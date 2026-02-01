namespace ANXAgentSwarm.Infrastructure.Configuration;

/// <summary>
/// Configuration options for Ollama LLM provider.
/// </summary>
public class OllamaOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Ollama";

    /// <summary>
    /// Base URL for Ollama API (default: http://localhost:11434).
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:11434";

    /// <summary>
    /// Default model to use (default: gemma3:27b).
    /// </summary>
    public string DefaultModel { get; set; } = "gemma3:27b";

    /// <summary>
    /// Request timeout in seconds (default: 120).
    /// </summary>
    public int TimeoutSeconds { get; set; } = 120;
}

/// <summary>
/// Configuration options for the workspace.
/// </summary>
public class WorkspaceOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Workspace";

    /// <summary>
    /// Root path for workspace files.
    /// </summary>
    public string RootPath { get; set; } = "./workspace";
}

/// <summary>
/// Configuration options for memory constraints.
/// </summary>
public class MemoryOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Memory";

    /// <summary>
    /// Maximum words allowed per memory content.
    /// </summary>
    public int MaxWordsPerMemory { get; set; } = 2000;

    /// <summary>
    /// Maximum words allowed in memory identifier.
    /// </summary>
    public int MaxIdentifierWords { get; set; } = 10;

    /// <summary>
    /// Maximum memories per persona per session.
    /// </summary>
    public int MaxMemoriesPerPersonaPerSession { get; set; } = 10;
}
