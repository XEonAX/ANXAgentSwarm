using ANXAgentSwarm.Core.Enums;

namespace ANXAgentSwarm.Core.DTOs;

/// <summary>
/// Request to create a new session.
/// </summary>
public record CreateSessionRequest(string ProblemStatement);

/// <summary>
/// Request to submit a clarification response.
/// </summary>
public record ClarificationResponse(string Response);

/// <summary>
/// DTO for Session entity.
/// </summary>
public record SessionDto(
    Guid Id,
    string Title,
    SessionStatus Status,
    string ProblemStatement,
    string? FinalSolution,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int MessageCount
);

/// <summary>
/// DTO for Session with messages.
/// </summary>
public record SessionDetailDto(
    Guid Id,
    string Title,
    SessionStatus Status,
    string ProblemStatement,
    string? FinalSolution,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    PersonaType? CurrentPersona,
    List<MessageDto> Messages
);

/// <summary>
/// DTO for Message entity.
/// </summary>
public record MessageDto(
    Guid Id,
    PersonaType FromPersona,
    PersonaType? ToPersona,
    string Content,
    MessageType MessageType,
    string? InternalReasoning,
    DateTime Timestamp,
    Guid? ParentMessageId,
    PersonaType? DelegateToPersona,
    string? DelegationContext,
    bool IsStuck
);

/// <summary>
/// DTO for Memory entity.
/// </summary>
public record MemoryDto(
    Guid Id,
    PersonaType PersonaType,
    string Identifier,
    string Content,
    DateTime CreatedAt,
    int AccessCount
);

/// <summary>
/// DTO for PersonaConfiguration entity.
/// </summary>
public record PersonaConfigurationDto(
    Guid Id,
    PersonaType PersonaType,
    string DisplayName,
    string ModelName,
    double Temperature,
    int MaxTokens,
    bool IsEnabled,
    string? Description
);

/// <summary>
/// Response for LLM availability check.
/// </summary>
public record LlmStatusDto(
    bool IsAvailable,
    IEnumerable<string> AvailableModels,
    string DefaultModel
);
