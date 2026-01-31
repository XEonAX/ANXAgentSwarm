using ANXAgentSwarm.Core.Entities;
using ANXAgentSwarm.Core.Enums;
using ANXAgentSwarm.Core.Interfaces;
using ANXAgentSwarm.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ANXAgentSwarm.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Memory entities.
/// </summary>
public class MemoryRepository : IMemoryRepository
{
    private readonly AppDbContext _context;

    public MemoryRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Memory> CreateAsync(Memory memory, CancellationToken cancellationToken = default)
    {
        memory.Id = memory.Id == Guid.Empty ? Guid.NewGuid() : memory.Id;
        memory.CreatedAt = DateTime.UtcNow;

        _context.Memories.Add(memory);
        await _context.SaveChangesAsync(cancellationToken);

        return memory;
    }

    public async Task<Memory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Memories
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Memory>> GetBySessionAndPersonaAsync(
        Guid sessionId,
        PersonaType persona,
        CancellationToken cancellationToken = default)
    {
        return await _context.Memories
            .Where(m => m.SessionId == sessionId && m.PersonaType == persona)
            .OrderByDescending(m => m.CreatedAt)
            .Take(10) // Max 10 memories per persona per session
            .ToListAsync(cancellationToken);
    }

    public async Task<Memory?> GetByIdentifierAsync(
        Guid sessionId,
        PersonaType persona,
        string identifier,
        CancellationToken cancellationToken = default)
    {
        return await _context.Memories
            .FirstOrDefaultAsync(m => 
                m.SessionId == sessionId && 
                m.PersonaType == persona && 
                m.Identifier == identifier, 
                cancellationToken);
    }

    public async Task<IEnumerable<Memory>> SearchAsync(
        Guid sessionId,
        PersonaType persona,
        string query,
        CancellationToken cancellationToken = default)
    {
        var lowerQuery = query.ToLowerInvariant();
        
        return await _context.Memories
            .Where(m => 
                m.SessionId == sessionId && 
                m.PersonaType == persona &&
                (m.Identifier.ToLower().Contains(lowerQuery) || 
                 m.Content.ToLower().Contains(lowerQuery)))
            .OrderByDescending(m => m.AccessCount)
            .ThenByDescending(m => m.CreatedAt)
            .Take(10)
            .ToListAsync(cancellationToken);
    }

    public async Task<Memory> UpdateAsync(Memory memory, CancellationToken cancellationToken = default)
    {
        _context.Memories.Update(memory);
        await _context.SaveChangesAsync(cancellationToken);

        return memory;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var memory = await _context.Memories.FindAsync(new object[] { id }, cancellationToken);
        if (memory != null)
        {
            _context.Memories.Remove(memory);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<int> GetCountAsync(
        Guid sessionId,
        PersonaType persona,
        CancellationToken cancellationToken = default)
    {
        return await _context.Memories
            .CountAsync(m => m.SessionId == sessionId && m.PersonaType == persona, cancellationToken);
    }
}
