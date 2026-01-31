using ANXAgentSwarm.Core.Enums;

namespace ANXAgentSwarm.Core.Entities;

/// <summary>
/// Represents a stored memory for a persona within a session.
/// </summary>
public class Memory
{
    /// <summary>
    /// Unique identifier for the memory.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The session this memory belongs to.
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// Navigation property to the session.
    /// </summary>
    public Session? Session { get; set; }

    /// <summary>
    /// The persona that owns this memory.
    /// </summary>
    public PersonaType PersonaType { get; set; }

    /// <summary>
    /// Short identifier for the memory (max 10 words).
    /// </summary>
    public string Identifier { get; set; } = string.Empty;

    /// <summary>
    /// The content of the memory (max 2000 words).
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// When the memory was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Number of times this memory has been accessed.
    /// </summary>
    public int AccessCount { get; set; }

    /// <summary>
    /// When the memory was last accessed.
    /// </summary>
    public DateTime? LastAccessedAt { get; set; }

    /// <summary>
    /// Maximum words allowed in the identifier.
    /// </summary>
    public const int MaxIdentifierWords = 10;

    /// <summary>
    /// Maximum words allowed in the content.
    /// </summary>
    public const int MaxContentWords = 2000;
}
