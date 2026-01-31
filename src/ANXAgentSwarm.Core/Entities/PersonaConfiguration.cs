using ANXAgentSwarm.Core.Enums;

namespace ANXAgentSwarm.Core.Entities;

/// <summary>
/// Configuration for a specific persona including LLM settings and system prompt.
/// </summary>
public class PersonaConfiguration
{
    /// <summary>
    /// Unique identifier for the configuration.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The persona type this configuration applies to.
    /// </summary>
    public PersonaType PersonaType { get; set; }

    /// <summary>
    /// Display name for the persona.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// The LLM model to use for this persona.
    /// </summary>
    public string ModelName { get; set; } = "gemma3";

    /// <summary>
    /// The system prompt that defines this persona's behavior.
    /// </summary>
    public string SystemPrompt { get; set; } = string.Empty;

    /// <summary>
    /// Temperature setting for LLM generation (0.0-1.0).
    /// </summary>
    public double Temperature { get; set; } = 0.7;

    /// <summary>
    /// Maximum tokens for LLM response.
    /// </summary>
    public int MaxTokens { get; set; } = 4096;

    /// <summary>
    /// Whether this persona is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Brief description of the persona's role.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Order in which personas are typically consulted.
    /// </summary>
    public int SortOrder { get; set; }
}
