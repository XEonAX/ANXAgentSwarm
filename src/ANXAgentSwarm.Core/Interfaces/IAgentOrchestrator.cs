using ANXAgentSwarm.Core.Entities;

namespace ANXAgentSwarm.Core.Interfaces;

/// <summary>
/// Interface for the agent orchestrator that manages session flow.
/// </summary>
public interface IAgentOrchestrator
{
    /// <summary>
    /// Starts a new problem-solving session.
    /// </summary>
    /// <param name="problemStatement">The problem to solve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created session.</returns>
    Task<Session> StartSessionAsync(string problemStatement, CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes a delegation in a session.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="messageId">The message that triggered the delegation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response message.</returns>
    Task<Message> ProcessDelegationAsync(
        Guid sessionId,
        Guid messageId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Handles a user clarification response.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="response">The user's response.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The next message in the flow.</returns>
    Task<Message> HandleUserClarificationAsync(
        Guid sessionId,
        string response,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Initializes a new session without processing. Use this for real-time streaming,
    /// followed by ProcessSessionAsync.
    /// </summary>
    /// <param name="problemStatement">The problem to solve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created session (not yet processed).</returns>
    Task<Session> InitializeSessionAsync(string problemStatement, CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes an initialized session with the orchestration loop.
    /// This broadcasts messages in real-time via SignalR.
    /// </summary>
    /// <param name="sessionId">The session ID to process.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The processed session.</returns>
    Task<Session> ProcessSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resumes a paused or stuck session.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The session.</returns>
    Task<Session> ResumeSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a session.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CancelSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
}
