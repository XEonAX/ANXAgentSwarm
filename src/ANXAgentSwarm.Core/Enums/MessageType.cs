namespace ANXAgentSwarm.Core.Enums;

/// <summary>
/// Represents the type of message in a conversation.
/// </summary>
public enum MessageType
{
    /// <summary>
    /// A question being asked (to another persona or to the user).
    /// </summary>
    Question = 0,

    /// <summary>
    /// An answer to a previous question.
    /// </summary>
    Answer = 1,

    /// <summary>
    /// A delegation to another persona with context.
    /// </summary>
    Delegation = 2,

    /// <summary>
    /// A request for clarification from the user.
    /// </summary>
    Clarification = 3,

    /// <summary>
    /// A complete or partial solution.
    /// </summary>
    Solution = 4,

    /// <summary>
    /// The persona is stuck and cannot proceed.
    /// </summary>
    Stuck = 5,

    /// <summary>
    /// Initial problem statement from the user.
    /// </summary>
    ProblemStatement = 6,

    /// <summary>
    /// User's response to a clarification request.
    /// </summary>
    UserResponse = 7,

    /// <summary>
    /// A decline to handle a delegated task.
    /// </summary>
    Decline = 8
}
