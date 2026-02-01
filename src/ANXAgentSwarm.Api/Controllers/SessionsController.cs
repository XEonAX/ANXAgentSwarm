using ANXAgentSwarm.Core.DTOs;
using ANXAgentSwarm.Core.Extensions;
using ANXAgentSwarm.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace ANXAgentSwarm.Api.Controllers;

/// <summary>
/// Controller for managing sessions.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SessionsController : ControllerBase
{
    private readonly ISessionRepository _sessionRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly IAgentOrchestrator _orchestrator;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SessionsController> _logger;

    public SessionsController(
        ISessionRepository sessionRepository,
        IMessageRepository messageRepository,
        IAgentOrchestrator orchestrator,
        IServiceScopeFactory scopeFactory,
        ILogger<SessionsController> logger)
    {
        _sessionRepository = sessionRepository;
        _messageRepository = messageRepository;
        _orchestrator = orchestrator;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new problem-solving session and starts processing asynchronously.
    /// Returns immediately with the session ID so the client can subscribe to SignalR updates.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<SessionDetailDto>> Create(
        [FromBody] CreateSessionRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ProblemStatement))
        {
            return BadRequest(new { message = "Problem statement is required" });
        }

        try
        {
            // Initialize the session (creates it in DB with initial message)
            var session = await _orchestrator.InitializeSessionAsync(request.ProblemStatement, cancellationToken);
            var sessionId = session.Id;
            
            // Start processing in the background (fire and forget)
            // This allows the client to receive real-time updates via SignalR
            // We create a new scope because the original request's scope will be disposed
            _ = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var orchestrator = scope.ServiceProvider.GetRequiredService<IAgentOrchestrator>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<SessionsController>>();
                
                try
                {
                    await orchestrator.ProcessSessionAsync(sessionId, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing session {SessionId}", sessionId);
                }
            });
            
            return CreatedAtAction(nameof(GetById), new { id = session.Id }, session.ToDetailDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating session");
            return StatusCode(500, new { message = "An error occurred while processing your request" });
        }
    }

    /// <summary>
    /// Gets all sessions.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SessionDto>>> GetAll(CancellationToken cancellationToken)
    {
        var sessions = await _sessionRepository.GetAllAsync(cancellationToken: cancellationToken);
        return Ok(sessions.Select(s => s.ToDto()));
    }

    /// <summary>
    /// Gets a session by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SessionDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetByIdWithMessagesAsync(id, cancellationToken);
        
        if (session == null)
        {
            return NotFound(new { message = $"Session with ID {id} not found" });
        }

        return Ok(session.ToDetailDto());
    }

    /// <summary>
    /// Submits a user clarification response.
    /// </summary>
    [HttpPost("{id:guid}/clarify")]
    public async Task<ActionResult<MessageDto>> SubmitClarification(
        Guid id,
        [FromBody] ClarificationResponse request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Response))
        {
            return BadRequest(new { message = "Response is required" });
        }

        try
        {
            var message = await _orchestrator.HandleUserClarificationAsync(
                id, request.Response, cancellationToken);
            return Ok(message.ToDto());
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting clarification for session {SessionId}", id);
            return StatusCode(500, new { message = "An error occurred while processing your request" });
        }
    }

    /// <summary>
    /// Resumes a paused or stuck session.
    /// </summary>
    [HttpPost("{id:guid}/resume")]
    public async Task<ActionResult<SessionDetailDto>> Resume(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await _orchestrator.ResumeSessionAsync(id, cancellationToken);
            return Ok(session.ToDetailDto());
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resuming session {SessionId}", id);
            return StatusCode(500, new { message = "An error occurred while processing your request" });
        }
    }

    /// <summary>
    /// Cancels a session.
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _orchestrator.CancelSessionAsync(id, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling session {SessionId}", id);
            return StatusCode(500, new { message = "An error occurred while processing your request" });
        }
    }

    /// <summary>
    /// Gets all messages for a session.
    /// </summary>
    [HttpGet("{id:guid}/messages")]
    public async Task<ActionResult<IEnumerable<MessageDto>>> GetMessages(
        Guid id,
        CancellationToken cancellationToken)
    {
        var exists = await _sessionRepository.ExistsAsync(id, cancellationToken);
        if (!exists)
        {
            return NotFound(new { message = $"Session with ID {id} not found" });
        }

        var messages = await _messageRepository.GetBySessionIdAsync(id, cancellationToken);
        return Ok(messages.Select(m => m.ToDto()));
    }

    /// <summary>
    /// Deletes a session.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var exists = await _sessionRepository.ExistsAsync(id, cancellationToken);
        
        if (!exists)
        {
            return NotFound(new { message = $"Session with ID {id} not found" });
        }

        await _sessionRepository.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
