using ANXAgentSwarm.Core.Enums;

namespace ANXAgentSwarm.Core.Entities;

/// <summary>
/// Represents a problem-solving session containing all messages and state.
/// </summary>
public class Session
{
    /// <summary>
    /// Unique identifier for the session.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Title of the session (auto-generated or user-provided).
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the session.
    /// </summary>
    public SessionStatus Status { get; set; }

    /// <summary>
    /// The original problem statement from the user.
    /// </summary>
    public string ProblemStatement { get; set; } = string.Empty;

    /// <summary>
    /// The final compiled solution (null until completed).
    /// </summary>
    public string? FinalSolution { get; set; }

    /// <summary>
    /// When the session was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the session was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// The persona that is currently processing (for tracking active work).
    /// </summary>
    public PersonaType? CurrentPersona { get; set; }

    /// <summary>
    /// All messages in this session.
    /// </summary>
    public ICollection<Message> Messages { get; set; } = new List<Message>();

    /// <summary>
    /// All memories stored during this session.
    /// </summary>
    public ICollection<Memory> Memories { get; set; } = new List<Memory>();

    /// <summary>
    /// Check if the session is in a terminal state.
    /// </summary>
    public bool IsTerminated => Status is SessionStatus.Completed 
                                        or SessionStatus.Stuck 
                                        or SessionStatus.Cancelled 
                                        or SessionStatus.Error;

    /// <summary>
    /// Check if the session is waiting for user input.
    /// </summary>
    public bool IsWaitingForUser => Status == SessionStatus.WaitingForClarification;
}
