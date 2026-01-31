using ANXAgentSwarm.Core.Entities;
using ANXAgentSwarm.Core.Enums;
using ANXAgentSwarm.Core.Models;

namespace ANXAgentSwarm.Core.Interfaces;

/// <summary>
/// Interface for the persona engine that processes messages through LLM.
/// </summary>
public interface IPersonaEngine
{
    /// <summary>
    /// Processes an incoming message for a specific persona.
    /// </summary>
    /// <param name="persona">The persona to process as.</param>
    /// <param name="incomingMessage">The message to respond to.</param>
    /// <param name="session">The current session context.</param>
    /// <param name="relevantMemories">Relevant memories for context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The persona's response.</returns>
    Task<PersonaResponse> ProcessAsync(
        PersonaType persona,
        Message incomingMessage,
        Session session,
        IEnumerable<Memory> relevantMemories,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the configuration for a specific persona.
    /// </summary>
    /// <param name="persona">The persona type.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The persona configuration.</returns>
    Task<PersonaConfiguration?> GetPersonaConfigurationAsync(
        PersonaType persona,
        CancellationToken cancellationToken = default);
}
