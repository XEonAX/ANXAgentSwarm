using ANXAgentSwarm.Core.DTOs;
using ANXAgentSwarm.Core.Interfaces;
using ANXAgentSwarm.Infrastructure.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ANXAgentSwarm.Api.Controllers;

/// <summary>
/// Controller for system status and LLM information.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class StatusController : ControllerBase
{
    private readonly ILlmProvider _llmProvider;
    private readonly OllamaOptions _ollamaOptions;
    private readonly ILogger<StatusController> _logger;

    public StatusController(
        ILlmProvider llmProvider,
        IOptions<OllamaOptions> ollamaOptions,
        ILogger<StatusController> logger)
    {
        _llmProvider = llmProvider;
        _ollamaOptions = ollamaOptions.Value;
        _logger = logger;
    }

    /// <summary>
    /// Gets the LLM provider status and available models.
    /// </summary>
    [HttpGet("llm")]
    public async Task<ActionResult<LlmStatusDto>> GetLlmStatus(CancellationToken cancellationToken)
    {
        var isAvailable = await _llmProvider.IsAvailableAsync(cancellationToken);
        var models = isAvailable
            ? await _llmProvider.GetAvailableModelsAsync(cancellationToken)
            : Enumerable.Empty<string>();

        return Ok(new LlmStatusDto(
            isAvailable,
            models,
            _ollamaOptions.DefaultModel
        ));
    }

    /// <summary>
    /// Gets system health status.
    /// </summary>
    [HttpGet("health")]
    public async Task<IActionResult> GetHealth(CancellationToken cancellationToken)
    {
        var llmAvailable = await _llmProvider.IsAvailableAsync(cancellationToken);

        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            services = new
            {
                database = true, // If we got here, database is working
                llm = llmAvailable
            }
        });
    }
}
