namespace ANXAgentSwarm.Core.Enums;

/// <summary>
/// Represents the current status of a session.
/// </summary>
public enum SessionStatus
{
    /// <summary>
    /// Session is actively being processed.
    /// </summary>
    Active = 0,

    /// <summary>
    /// Session is waiting for user clarification.
    /// </summary>
    WaitingForClarification = 1,

    /// <summary>
    /// Session has been completed successfully with a solution.
    /// </summary>
    Completed = 2,

    /// <summary>
    /// All personas are stuck and cannot proceed.
    /// </summary>
    Stuck = 3,

    /// <summary>
    /// Session was cancelled by the user.
    /// </summary>
    Cancelled = 4,

    /// <summary>
    /// Session encountered an error.
    /// </summary>
    Error = 5,

    /// <summary>
    /// Session was interrupted (e.g., server restart while processing).
    /// </summary>
    Interrupted = 6
}
