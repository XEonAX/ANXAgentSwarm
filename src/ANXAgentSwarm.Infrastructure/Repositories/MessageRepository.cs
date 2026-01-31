using ANXAgentSwarm.Core.Entities;
using ANXAgentSwarm.Core.Interfaces;
using ANXAgentSwarm.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ANXAgentSwarm.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Message entities.
/// </summary>
public class MessageRepository : IMessageRepository
{
    private readonly AppDbContext _context;

    public MessageRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Message> CreateAsync(Message message, CancellationToken cancellationToken = default)
    {
        message.Id = message.Id == Guid.Empty ? Guid.NewGuid() : message.Id;
        message.Timestamp = message.Timestamp == default ? DateTime.UtcNow : message.Timestamp;

        _context.Messages.Add(message);
        await _context.SaveChangesAsync(cancellationToken);

        return message;
    }

    public async Task<Message?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Messages
            .Include(m => m.ParentMessage)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Message>> GetBySessionIdAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Messages
            .Where(m => m.SessionId == sessionId)
            .OrderBy(m => m.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<Message?> GetLastMessageAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return await _context.Messages
            .Where(m => m.SessionId == sessionId)
            .OrderByDescending(m => m.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Message> UpdateAsync(Message message, CancellationToken cancellationToken = default)
    {
        _context.Messages.Update(message);
        await _context.SaveChangesAsync(cancellationToken);

        return message;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var message = await _context.Messages.FindAsync(new object[] { id }, cancellationToken);
        if (message != null)
        {
            _context.Messages.Remove(message);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<int> GetCountBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return await _context.Messages
            .CountAsync(m => m.SessionId == sessionId, cancellationToken);
    }
}
