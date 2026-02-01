using ANXAgentSwarm.Core.DTOs;
using ANXAgentSwarm.Core.Entities;

namespace ANXAgentSwarm.Core.Interfaces;

/// <summary>
/// Interface for broadcasting session events via SignalR.
/// Allows services in Infrastructure to broadcast without direct SignalR dependency.
/// </summary>
public interface ISessionHubBroadcaster
{
    /// <summary>
    /// Broadcasts that a new message was received in a session.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="message">The message DTO.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task BroadcastMessageReceivedAsync(
        Guid sessionId,
        MessageDto message,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Broadcasts that a session's status has changed.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="session">The session DTO with updated status.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task BroadcastSessionStatusChangedAsync(
        Guid sessionId,
        SessionDto session,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Broadcasts that a persona has requested clarification from the user.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="message">The clarification request message DTO.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task BroadcastClarificationRequestedAsync(
        Guid sessionId,
        MessageDto message,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Broadcasts that a solution is ready for the session.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="session">The completed session DTO with the solution.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task BroadcastSolutionReadyAsync(
        Guid sessionId,
        SessionDto session,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Broadcasts that the session is stuck and cannot proceed.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="session">The stuck session DTO.</param>
    /// <param name="partialResults">A summary of partial results achieved before getting stuck.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task BroadcastSessionStuckAsync(
        Guid sessionId,
        SessionDto session,
        string partialResults,
        CancellationToken cancellationToken = default);
}
