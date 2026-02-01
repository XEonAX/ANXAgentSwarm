using ANXAgentSwarm.Core.DTOs;
using ANXAgentSwarm.Core.Entities;

namespace ANXAgentSwarm.Core.Extensions;

/// <summary>
/// Extension methods for mapping entities to DTOs.
/// </summary>
public static class MappingExtensions
{
    /// <summary>
    /// Maps a Session entity to SessionDto.
    /// </summary>
    public static SessionDto ToDto(this Session session)
    {
        return new SessionDto(
            session.Id,
            session.Title,
            session.Status,
            session.ProblemStatement,
            session.FinalSolution,
            session.CreatedAt,
            session.UpdatedAt,
            session.Messages?.Count ?? 0,
            session.CurrentPersona
        );
    }

    /// <summary>
    /// Maps a Session entity to SessionDetailDto.
    /// </summary>
    public static SessionDetailDto ToDetailDto(this Session session)
    {
        return new SessionDetailDto(
            session.Id,
            session.Title,
            session.Status,
            session.ProblemStatement,
            session.FinalSolution,
            session.CreatedAt,
            session.UpdatedAt,
            session.CurrentPersona,
            session.Messages?.Select(m => m.ToDto()).ToList() ?? new List<MessageDto>()
        );
    }

    /// <summary>
    /// Maps a Message entity to MessageDto.
    /// </summary>
    public static MessageDto ToDto(this Message message)
    {
        return new MessageDto(
            message.Id,
            message.FromPersona,
            message.ToPersona,
            message.Content,
            message.MessageType,
            message.InternalReasoning,
            message.Timestamp,
            message.ParentMessageId,
            message.DelegateToPersona,
            message.DelegationContext,
            message.IsStuck
        );
    }

    /// <summary>
    /// Maps a Memory entity to MemoryDto.
    /// </summary>
    public static MemoryDto ToDto(this Memory memory)
    {
        return new MemoryDto(
            memory.Id,
            memory.PersonaType,
            memory.Identifier,
            memory.Content,
            memory.CreatedAt,
            memory.AccessCount
        );
    }

    /// <summary>
    /// Maps a PersonaConfiguration entity to PersonaConfigurationDto.
    /// </summary>
    public static PersonaConfigurationDto ToDto(this PersonaConfiguration config)
    {
        return new PersonaConfigurationDto(
            config.Id,
            config.PersonaType,
            config.DisplayName,
            config.ModelName,
            config.Temperature,
            config.MaxTokens,
            config.IsEnabled,
            config.Description
        );
    }
}
