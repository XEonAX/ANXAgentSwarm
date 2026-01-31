using ANXAgentSwarm.Core.Entities;
using ANXAgentSwarm.Core.Enums;

namespace ANXAgentSwarm.Core.Interfaces;

/// <summary>
/// Repository interface for Memory entities.
/// </summary>
public interface IMemoryRepository
{
    /// <summary>
    /// Creates a new memory.
    /// </summary>
    Task<Memory> CreateAsync(Memory memory, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a memory by ID.
    /// </summary>
    Task<Memory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets memories for a persona in a session.
    /// </summary>
    Task<IEnumerable<Memory>> GetBySessionAndPersonaAsync(
        Guid sessionId,
        PersonaType persona,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a memory by identifier for a persona in a session.
    /// </summary>
    Task<Memory?> GetByIdentifierAsync(
        Guid sessionId,
        PersonaType persona,
        string identifier,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches memories by content.
    /// </summary>
    Task<IEnumerable<Memory>> SearchAsync(
        Guid sessionId,
        PersonaType persona,
        string query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a memory.
    /// </summary>
    Task<Memory> UpdateAsync(Memory memory, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a memory.
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of memories for a persona in a session.
    /// </summary>
    Task<int> GetCountAsync(
        Guid sessionId,
        PersonaType persona,
        CancellationToken cancellationToken = default);
}
