using ANXAgentSwarm.Core.Enums;

namespace ANXAgentSwarm.Core.Entities;

/// <summary>
/// Represents a single message in a session conversation.
/// </summary>
public class Message
{
    /// <summary>
    /// Unique identifier for the message.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The session this message belongs to.
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// Navigation property to the session.
    /// </summary>
    public Session? Session { get; set; }

    /// <summary>
    /// The persona that sent this message.
    /// </summary>
    public PersonaType FromPersona { get; set; }

    /// <summary>
    /// The persona this message is directed to (null if to user or broadcast).
    /// </summary>
    public PersonaType? ToPersona { get; set; }

    /// <summary>
    /// The visible content of the message.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// The type of this message.
    /// </summary>
    public MessageType MessageType { get; set; }

    /// <summary>
    /// The internal reasoning/thinking process (shown in expandable UI).
    /// </summary>
    public string? InternalReasoning { get; set; }

    /// <summary>
    /// When the message was created.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Optional parent message ID for threading.
    /// </summary>
    public Guid? ParentMessageId { get; set; }

    /// <summary>
    /// Navigation property to parent message.
    /// </summary>
    public Message? ParentMessage { get; set; }

    /// <summary>
    /// Child messages (replies to this message).
    /// </summary>
    public ICollection<Message> Replies { get; set; } = new List<Message>();

    /// <summary>
    /// If this is a delegation, which persona to delegate to.
    /// </summary>
    public PersonaType? DelegateToPersona { get; set; }

    /// <summary>
    /// Additional context for delegation.
    /// </summary>
    public string? DelegationContext { get; set; }

    /// <summary>
    /// Indicates if the persona is stuck on this message.
    /// </summary>
    public bool IsStuck { get; set; }

    /// <summary>
    /// The raw LLM response (for debugging).
    /// </summary>
    public string? RawResponse { get; set; }
}
