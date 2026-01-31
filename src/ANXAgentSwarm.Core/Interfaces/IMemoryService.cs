using ANXAgentSwarm.Core.Entities;
using ANXAgentSwarm.Core.Enums;

namespace ANXAgentSwarm.Core.Interfaces;

/// <summary>
/// Interface for managing persona memories within sessions.
/// </summary>
public interface IMemoryService
{
    /// <summary>
    /// Stores a new memory for a persona in a session.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="persona">The persona storing the memory.</param>
    /// <param name="identifier">Short identifier (max 10 words).</param>
    /// <param name="content">Memory content (max 2000 words).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created memory.</returns>
    Task<Memory> StoreMemoryAsync(
        Guid sessionId,
        PersonaType persona,
        string identifier,
        string content,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches memories by keywords.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="persona">The persona whose memories to search.</param>
    /// <param name="query">Search query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Matching memories.</returns>
    Task<IEnumerable<Memory>> SearchMemoriesAsync(
        Guid sessionId,
        PersonaType persona,
        string query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the most recent memories for a persona in a session.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="persona">The persona.</param>
    /// <param name="count">Number of memories to retrieve (default 10).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Recent memories.</returns>
    Task<IEnumerable<Memory>> GetRecentMemoriesAsync(
        Guid sessionId,
        PersonaType persona,
        int count = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a memory by its identifier.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="persona">The persona.</param>
    /// <param name="identifier">The memory identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The memory if found.</returns>
    Task<Memory?> GetMemoryByIdentifierAsync(
        Guid sessionId,
        PersonaType persona,
        string identifier,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a memory.
    /// </summary>
    /// <param name="memoryId">The memory ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteMemoryAsync(Guid memoryId, CancellationToken cancellationToken = default);
}
