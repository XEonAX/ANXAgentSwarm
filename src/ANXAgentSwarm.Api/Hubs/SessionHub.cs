using Microsoft.AspNetCore.SignalR;

namespace ANXAgentSwarm.Api.Hubs;

/// <summary>
/// SignalR hub for real-time session updates.
/// Clients can join/leave session rooms to receive targeted updates.
/// </summary>
public class SessionHub : Hub
{
    private readonly ILogger<SessionHub> _logger;

    public SessionHub(ILogger<SessionHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Allows a client to join a session room to receive updates for that session.
    /// </summary>
    /// <param name="sessionId">The session ID to join.</param>
    public async Task JoinSession(string sessionId)
    {
        _logger.LogInformation(
            "Client {ConnectionId} joining session {SessionId}",
            Context.ConnectionId, sessionId);

        await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);

        await Clients.Caller.SendAsync("JoinedSession", new
        {
            SessionId = sessionId,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Allows a client to leave a session room.
    /// </summary>
    /// <param name="sessionId">The session ID to leave.</param>
    public async Task LeaveSession(string sessionId)
    {
        _logger.LogInformation(
            "Client {ConnectionId} leaving session {SessionId}",
            Context.ConnectionId, sessionId);

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId);

        await Clients.Caller.SendAsync("LeftSession", new
        {
            SessionId = sessionId,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Called when a client connects to the hub.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects from the hub.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception != null)
        {
            _logger.LogWarning(
                exception,
                "Client {ConnectionId} disconnected with error",
                Context.ConnectionId);
        }
        else
        {
            _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }
}

/// <summary>
/// Strongly-typed interface for client methods callable from the hub.
/// This is optional but provides type safety for hub-to-client calls.
/// </summary>
public interface ISessionHubClient
{
    /// <summary>
    /// Called when a new message is received in the session.
    /// </summary>
    Task MessageReceived(object message);

    /// <summary>
    /// Called when the session status changes.
    /// </summary>
    Task SessionStatusChanged(object session);

    /// <summary>
    /// Called when clarification is requested from the user.
    /// </summary>
    Task ClarificationRequested(object message);

    /// <summary>
    /// Called when the final solution is ready.
    /// </summary>
    Task SolutionReady(object session);

    /// <summary>
    /// Called when the session is stuck.
    /// </summary>
    Task SessionStuck(object data);

    /// <summary>
    /// Called when the client successfully joins a session.
    /// </summary>
    Task JoinedSession(object data);

    /// <summary>
    /// Called when the client leaves a session.
    /// </summary>
    Task LeftSession(object data);
}
