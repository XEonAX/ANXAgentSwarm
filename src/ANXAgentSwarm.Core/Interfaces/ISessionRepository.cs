using ANXAgentSwarm.Core.Entities;
using ANXAgentSwarm.Core.Enums;

namespace ANXAgentSwarm.Core.Interfaces;

/// <summary>
/// Repository interface for Session entities.
/// </summary>
public interface ISessionRepository
{
    /// <summary>
    /// Creates a new session.
    /// </summary>
    Task<Session> CreateAsync(Session session, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a session by ID.
    /// </summary>
    Task<Session?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a session by ID including all messages.
    /// </summary>
    Task<Session?> GetByIdWithMessagesAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all sessions, optionally filtered by status.
    /// </summary>
    Task<IEnumerable<Session>> GetAllAsync(
        SessionStatus? status = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a session.
    /// </summary>
    Task<Session> UpdateAsync(Session session, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a session.
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a session exists.
    /// </summary>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}
