using ANXAgentSwarm.Core.Models;

namespace ANXAgentSwarm.Core.Interfaces;

/// <summary>
/// Interface for LLM provider implementations (Ollama, OpenAI, etc.).
/// </summary>
public interface ILlmProvider
{
    /// <summary>
    /// Generates a response from the LLM.
    /// </summary>
    /// <param name="request">The request containing model, prompt, and messages.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The LLM response.</returns>
    Task<LlmResponse> GenerateAsync(LlmRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the LLM provider is available and responding.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if available, false otherwise.</returns>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the list of available models.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of model names.</returns>
    Task<IEnumerable<string>> GetAvailableModelsAsync(CancellationToken cancellationToken = default);
}
