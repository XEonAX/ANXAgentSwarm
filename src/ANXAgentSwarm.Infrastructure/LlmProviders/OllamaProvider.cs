using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ANXAgentSwarm.Core.Interfaces;
using ANXAgentSwarm.Core.Models;
using ANXAgentSwarm.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ANXAgentSwarm.Infrastructure.LlmProviders;

/// <summary>
/// Ollama LLM provider implementation.
/// </summary>
public class OllamaProvider : ILlmProvider
{
    private readonly HttpClient _httpClient;
    private readonly OllamaOptions _options;
    private readonly ILogger<OllamaProvider> _logger;

    public OllamaProvider(
        HttpClient httpClient,
        IOptions<OllamaOptions> options,
        ILogger<OllamaProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(_options.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
    }

    public async Task<LlmResponse> GenerateAsync(LlmRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var model = string.IsNullOrEmpty(request.Model) ? _options.DefaultModel : request.Model;

            // Build messages array with system prompt
            var messages = new List<OllamaMessage>();

            if (!string.IsNullOrEmpty(request.SystemPrompt))
            {
                messages.Add(new OllamaMessage { Role = "system", Content = request.SystemPrompt });
            }

            foreach (var msg in request.Messages)
            {
                messages.Add(new OllamaMessage { Role = msg.Role, Content = msg.Content });
            }

            var ollamaRequest = new OllamaChatRequest
            {
                Model = model,
                Messages = messages,
                Stream = false,
                Options = new OllamaRequestOptions
                {
                    Temperature = request.Temperature,
                    NumPredict = request.MaxTokens
                }
            };

            _logger.LogDebug("Sending request to Ollama: Model={Model}, Messages={MessageCount}",
                model, messages.Count);

            var response = await _httpClient.PostAsJsonAsync(
                "/api/chat",
                ollamaRequest,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Ollama request failed: {StatusCode} - {Error}",
                    response.StatusCode, errorContent);

                return new LlmResponse
                {
                    Success = false,
                    Error = $"Ollama request failed: {response.StatusCode} - {errorContent}"
                };
            }

            var ollamaResponse = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(
                cancellationToken: cancellationToken);

            stopwatch.Stop();

            if (ollamaResponse?.Message == null)
            {
                return new LlmResponse
                {
                    Success = false,
                    Error = "Empty response from Ollama"
                };
            }

            _logger.LogDebug("Received response from Ollama: {ContentLength} chars in {Duration}ms",
                ollamaResponse.Message.Content?.Length ?? 0, stopwatch.ElapsedMilliseconds);

            return new LlmResponse
            {
                Success = true,
                Content = ollamaResponse.Message.Content ?? string.Empty,
                Model = ollamaResponse.Model,
                TotalTokens = ollamaResponse.PromptEvalCount + ollamaResponse.EvalCount,
                DurationMs = stopwatch.ElapsedMilliseconds
            };
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Ollama request was cancelled or timed out");
            return new LlmResponse
            {
                Success = false,
                Error = "Request was cancelled or timed out"
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error communicating with Ollama");
            return new LlmResponse
            {
                Success = false,
                Error = $"HTTP error: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling Ollama");
            return new LlmResponse
            {
                Success = false,
                Error = $"Unexpected error: {ex.Message}"
            };
        }
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/tags", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ollama availability check failed");
            return false;
        }
    }

    public async Task<IEnumerable<string>> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/tags", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return Enumerable.Empty<string>();
            }

            var tagsResponse = await response.Content.ReadFromJsonAsync<OllamaTagsResponse>(
                cancellationToken: cancellationToken);

            return tagsResponse?.Models?.Select(m => m.Name) ?? Enumerable.Empty<string>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get available models from Ollama");
            return Enumerable.Empty<string>();
        }
    }

    #region Ollama API Models

    private class OllamaChatRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("messages")]
        public List<OllamaMessage> Messages { get; set; } = new();

        [JsonPropertyName("stream")]
        public bool Stream { get; set; }

        [JsonPropertyName("options")]
        public OllamaRequestOptions? Options { get; set; }
    }

    private class OllamaMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    private class OllamaRequestOptions
    {
        [JsonPropertyName("temperature")]
        public double Temperature { get; set; }

        [JsonPropertyName("num_predict")]
        public int NumPredict { get; set; }
    }

    private class OllamaChatResponse
    {
        [JsonPropertyName("model")]
        public string? Model { get; set; }

        [JsonPropertyName("message")]
        public OllamaMessage? Message { get; set; }

        [JsonPropertyName("done")]
        public bool Done { get; set; }

        [JsonPropertyName("total_duration")]
        public long TotalDuration { get; set; }

        [JsonPropertyName("prompt_eval_count")]
        public int PromptEvalCount { get; set; }

        [JsonPropertyName("eval_count")]
        public int EvalCount { get; set; }
    }

    private class OllamaTagsResponse
    {
        [JsonPropertyName("models")]
        public List<OllamaModelInfo>? Models { get; set; }
    }

    private class OllamaModelInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("size")]
        public long Size { get; set; }

        [JsonPropertyName("modified_at")]
        public DateTime ModifiedAt { get; set; }
    }

    #endregion
}
