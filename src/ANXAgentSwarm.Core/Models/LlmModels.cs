namespace ANXAgentSwarm.Core.Models;

/// <summary>
/// Represents a chat message for LLM communication.
/// </summary>
public class ChatMessage
{
    /// <summary>
    /// The role of the message sender (system, user, assistant).
    /// </summary>
    public string Role { get; set; } = "user";

    /// <summary>
    /// The content of the message.
    /// </summary>
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// Request to send to the LLM provider.
/// </summary>
public class LlmRequest
{
    /// <summary>
    /// The model to use for generation.
    /// </summary>
    public string Model { get; set; } = "gemma3:27b";

    /// <summary>
    /// The system prompt to set the context.
    /// </summary>
    public string SystemPrompt { get; set; } = string.Empty;

    /// <summary>
    /// The conversation history.
    /// </summary>
    public List<ChatMessage> Messages { get; set; } = new();

    /// <summary>
    /// Temperature for generation (0.0-1.0).
    /// </summary>
    public double Temperature { get; set; } = 0.7;

    /// <summary>
    /// Maximum tokens to generate.
    /// </summary>
    public int MaxTokens { get; set; } = 4096;
}

/// <summary>
/// Response from the LLM provider.
/// </summary>
public class LlmResponse
{
    /// <summary>
    /// Whether the request was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The generated content.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Error message if the request failed.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// The model used for generation.
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// Total tokens used in the request.
    /// </summary>
    public int? TotalTokens { get; set; }

    /// <summary>
    /// Time taken for generation in milliseconds.
    /// </summary>
    public long? DurationMs { get; set; }
}
