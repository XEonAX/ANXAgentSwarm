using ANXAgentSwarm.Core.DTOs;
using ANXAgentSwarm.Core.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace ANXAgentSwarm.Api.Hubs;

/// <summary>
/// Implementation of ISessionHubBroadcaster that uses SignalR to broadcast events.
/// This bridges the Infrastructure layer (AgentOrchestrator) with the API layer (SignalR hub).
/// </summary>
public class SessionHubBroadcaster : ISessionHubBroadcaster
{
    private readonly IHubContext<SessionHub> _hubContext;
    private readonly ILogger<SessionHubBroadcaster> _logger;

    public SessionHubBroadcaster(
        IHubContext<SessionHub> hubContext,
        ILogger<SessionHubBroadcaster> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task BroadcastMessageReceivedAsync(
        Guid sessionId,
        MessageDto message,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Broadcasting MessageReceived to session {SessionId} from {FromPersona}",
            sessionId, message.FromPersona);

        await _hubContext.Clients
            .Group(sessionId.ToString())
            .SendAsync("MessageReceived", new
            {
                SessionId = sessionId.ToString(),
                Message = message
            }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task BroadcastSessionStatusChangedAsync(
        Guid sessionId,
        SessionDto session,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Broadcasting SessionStatusChanged to session {SessionId}, new status: {Status}",
            sessionId, session.Status);

        await _hubContext.Clients
            .Group(sessionId.ToString())
            .SendAsync("SessionStatusChanged", new
            {
                SessionId = sessionId.ToString(),
                Status = session.Status,
                CurrentPersona = session.CurrentPersona
            }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task BroadcastClarificationRequestedAsync(
        Guid sessionId,
        MessageDto message,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Broadcasting ClarificationRequested to session {SessionId} from {FromPersona}",
            sessionId, message.FromPersona);

        await _hubContext.Clients
            .Group(sessionId.ToString())
            .SendAsync("ClarificationRequested", new
            {
                SessionId = sessionId.ToString(),
                Question = message.Content,
                FromPersona = message.FromPersona
            }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task BroadcastSolutionReadyAsync(
        Guid sessionId,
        SessionDto session,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Broadcasting SolutionReady to session {SessionId}",
            sessionId);

        await _hubContext.Clients
            .Group(sessionId.ToString())
            .SendAsync("SolutionReady", new
            {
                SessionId = sessionId.ToString(),
                Solution = session.FinalSolution
            }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task BroadcastSessionStuckAsync(
        Guid sessionId,
        SessionDto session,
        string partialResults,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(
            "Broadcasting SessionStuck to session {SessionId}",
            sessionId);

        await _hubContext.Clients
            .Group(sessionId.ToString())
            .SendAsync("SessionStuck", new
            {
                SessionId = sessionId.ToString(),
                PartialResults = partialResults,
                Reason = "Session is stuck and requires attention"
            }, cancellationToken);
    }
}
