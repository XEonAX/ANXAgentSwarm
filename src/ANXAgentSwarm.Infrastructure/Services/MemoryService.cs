using ANXAgentSwarm.Core.Entities;
using ANXAgentSwarm.Core.Enums;
using ANXAgentSwarm.Core.Interfaces;
using ANXAgentSwarm.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ANXAgentSwarm.Infrastructure.Services;

/// <summary>
/// Service for managing persona memories within sessions.
/// </summary>
public class MemoryService : IMemoryService
{
    private readonly IMemoryRepository _memoryRepository;
    private readonly MemoryOptions _options;
    private readonly ILogger<MemoryService> _logger;

    public MemoryService(
        IMemoryRepository memoryRepository,
        IOptions<MemoryOptions> options,
        ILogger<MemoryService> logger)
    {
        _memoryRepository = memoryRepository;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Memory> StoreMemoryAsync(
        Guid sessionId,
        PersonaType persona,
        string identifier,
        string content,
        CancellationToken cancellationToken = default)
    {
        // Validate identifier word count
        var identifierWords = CountWords(identifier);
        if (identifierWords > _options.MaxIdentifierWords)
        {
            throw new ArgumentException(
                $"Identifier exceeds maximum word count. Max: {_options.MaxIdentifierWords}, Actual: {identifierWords}",
                nameof(identifier));
        }

        // Validate content word count
        var contentWords = CountWords(content);
        if (contentWords > _options.MaxWordsPerMemory)
        {
            throw new ArgumentException(
                $"Content exceeds maximum word count. Max: {_options.MaxWordsPerMemory}, Actual: {contentWords}",
                nameof(content));
        }

        // Check if we've reached the memory limit for this persona in this session
        var existingCount = await _memoryRepository.GetCountAsync(sessionId, persona, cancellationToken);
        if (existingCount >= _options.MaxMemoriesPerPersonaPerSession)
        {
            _logger.LogWarning(
                "Memory limit reached for persona {Persona} in session {SessionId}. " +
                "Max: {Max}, Current: {Current}. Oldest memory will be replaced.",
                persona, sessionId, _options.MaxMemoriesPerPersonaPerSession, existingCount);

            // Get existing memories and remove the oldest one
            var existingMemories = await _memoryRepository.GetBySessionAndPersonaAsync(
                sessionId, persona, cancellationToken);
            
            var oldest = existingMemories
                .OrderBy(m => m.CreatedAt)
                .First();
            
            await _memoryRepository.DeleteAsync(oldest.Id, cancellationToken);
        }

        // Check if a memory with this identifier already exists (update instead of create)
        var existing = await _memoryRepository.GetByIdentifierAsync(
            sessionId, persona, identifier, cancellationToken);

        if (existing != null)
        {
            _logger.LogInformation(
                "Updating existing memory with identifier '{Identifier}' for persona {Persona}",
                identifier, persona);

            existing.Content = content;
            existing.LastAccessedAt = DateTime.UtcNow;
            existing.AccessCount++;

            return await _memoryRepository.UpdateAsync(existing, cancellationToken);
        }

        // Create new memory
        var memory = new Memory
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            PersonaType = persona,
            Identifier = identifier.Trim(),
            Content = content.Trim(),
            CreatedAt = DateTime.UtcNow,
            AccessCount = 0
        };

        _logger.LogInformation(
            "Storing new memory with identifier '{Identifier}' for persona {Persona} in session {SessionId}",
            identifier, persona, sessionId);

        return await _memoryRepository.CreateAsync(memory, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Memory>> SearchMemoriesAsync(
        Guid sessionId,
        PersonaType persona,
        string query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Enumerable.Empty<Memory>();
        }

        _logger.LogDebug(
            "Searching memories for persona {Persona} in session {SessionId} with query '{Query}'",
            persona, sessionId, query);

        var memories = await _memoryRepository.SearchAsync(sessionId, persona, query, cancellationToken);
        
        // Update access count for retrieved memories
        foreach (var memory in memories)
        {
            memory.AccessCount++;
            memory.LastAccessedAt = DateTime.UtcNow;
            await _memoryRepository.UpdateAsync(memory, cancellationToken);
        }

        return memories;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Memory>> GetRecentMemoriesAsync(
        Guid sessionId,
        PersonaType persona,
        int count = 10,
        CancellationToken cancellationToken = default)
    {
        // Cap at max allowed
        var actualCount = Math.Min(count, _options.MaxMemoriesPerPersonaPerSession);

        var memories = await _memoryRepository.GetBySessionAndPersonaAsync(
            sessionId, persona, cancellationToken);

        var recentMemories = memories
            .OrderByDescending(m => m.CreatedAt)
            .Take(actualCount)
            .ToList();

        // Update access count for retrieved memories
        foreach (var memory in recentMemories)
        {
            memory.AccessCount++;
            memory.LastAccessedAt = DateTime.UtcNow;
            await _memoryRepository.UpdateAsync(memory, cancellationToken);
        }

        return recentMemories;
    }

    /// <inheritdoc />
    public async Task<Memory?> GetMemoryByIdentifierAsync(
        Guid sessionId,
        PersonaType persona,
        string identifier,
        CancellationToken cancellationToken = default)
    {
        var memory = await _memoryRepository.GetByIdentifierAsync(
            sessionId, persona, identifier, cancellationToken);

        if (memory != null)
        {
            memory.AccessCount++;
            memory.LastAccessedAt = DateTime.UtcNow;
            await _memoryRepository.UpdateAsync(memory, cancellationToken);
        }

        return memory;
    }

    /// <inheritdoc />
    public async Task DeleteMemoryAsync(Guid memoryId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting memory {MemoryId}", memoryId);
        await _memoryRepository.DeleteAsync(memoryId, cancellationToken);
    }

    /// <summary>
    /// Counts the number of words in a string.
    /// </summary>
    private static int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        return text.Split(
            new[] { ' ', '\t', '\n', '\r' },
            StringSplitOptions.RemoveEmptyEntries).Length;
    }
}
