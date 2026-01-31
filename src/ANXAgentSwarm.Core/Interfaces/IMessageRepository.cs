using ANXAgentSwarm.Core.Entities;

namespace ANXAgentSwarm.Core.Interfaces;

/// <summary>
/// Repository interface for Message entities.
/// </summary>
public interface IMessageRepository
{
    /// <summary>
    /// Creates a new message.
    /// </summary>
    Task<Message> CreateAsync(Message message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a message by ID.
    /// </summary>
    Task<Message?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all messages for a session in chronological order.
    /// </summary>
    Task<IEnumerable<Message>> GetBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the last message in a session.
    /// </summary>
    Task<Message?> GetLastMessageAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a message.
    /// </summary>
    Task<Message> UpdateAsync(Message message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a message.
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of messages in a session.
    /// </summary>
    Task<int> GetCountBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default);
}
