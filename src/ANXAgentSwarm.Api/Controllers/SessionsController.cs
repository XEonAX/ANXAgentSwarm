using ANXAgentSwarm.Core.DTOs;
using ANXAgentSwarm.Core.Extensions;
using ANXAgentSwarm.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

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
    private readonly ILogger<SessionsController> _logger;

    public SessionsController(
        ISessionRepository sessionRepository,
        IMessageRepository messageRepository,
        ILogger<SessionsController> logger)
    {
        _sessionRepository = sessionRepository;
        _messageRepository = messageRepository;
        _logger = logger;
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
