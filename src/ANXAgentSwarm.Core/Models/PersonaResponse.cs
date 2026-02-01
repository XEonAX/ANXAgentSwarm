using ANXAgentSwarm.Core.Entities;
using ANXAgentSwarm.Core.Enums;

namespace ANXAgentSwarm.Core.Models;

/// <summary>
/// Response from the persona engine after processing a message.
/// </summary>
public class PersonaResponse
{
    /// <summary>
    /// The visible content of the response.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// The internal reasoning/thinking process.
    /// </summary>
    public string? InternalReasoning { get; set; }

    /// <summary>
    /// The type of response.
    /// </summary>
    public MessageType ResponseType { get; set; }

    /// <summary>
    /// If delegating, which persona to delegate to.
    /// </summary>
    public PersonaType? DelegateToPersona { get; set; }

    /// <summary>
    /// Context to provide to the delegated persona.
    /// </summary>
    public string? DelegationContext { get; set; }

    /// <summary>
    /// Whether the persona is stuck and cannot proceed.
    /// </summary>
    public bool IsStuck { get; set; }

    /// <summary>
    /// If the persona needs user clarification, the question to ask.
    /// </summary>
    public string? ClarificationQuestion { get; set; }

    /// <summary>
    /// A new memory to store (if any).
    /// </summary>
    public Memory? NewMemory { get; set; }

    /// <summary>
    /// The raw LLM response for debugging.
    /// </summary>
    public string? RawResponse { get; set; }

    /// <summary>
    /// Creates a delegation response.
    /// </summary>
    public static PersonaResponse Delegate(PersonaType toPersona, string content, string context, string? reasoning = null)
    {
        return new PersonaResponse
        {
            Content = content,
            ResponseType = MessageType.Delegation,
            DelegateToPersona = toPersona,
            DelegationContext = context,
            InternalReasoning = reasoning
        };
    }

    /// <summary>
    /// Creates a clarification request response.
    /// </summary>
    public static PersonaResponse Clarify(string question, string? reasoning = null)
    {
        return new PersonaResponse
        {
            Content = question,
            ResponseType = MessageType.Clarification,
            ClarificationQuestion = question,
            InternalReasoning = reasoning
        };
    }

    /// <summary>
    /// Creates a solution response.
    /// </summary>
    public static PersonaResponse Solution(string solution, string? reasoning = null)
    {
        return new PersonaResponse
        {
            Content = solution,
            ResponseType = MessageType.Solution,
            InternalReasoning = reasoning
        };
    }

    /// <summary>
    /// Creates a stuck response.
    /// </summary>
    public static PersonaResponse Stuck(string reason, string? reasoning = null)
    {
        return new PersonaResponse
        {
            Content = reason,
            ResponseType = MessageType.Stuck,
            IsStuck = true,
            InternalReasoning = reasoning
        };
    }

    /// <summary>
    /// Creates an answer response.
    /// </summary>
    public static PersonaResponse Answer(string answer, string? reasoning = null)
    {
        return new PersonaResponse
        {
            Content = answer,
            ResponseType = MessageType.Answer,
            InternalReasoning = reasoning
        };
    }

    /// <summary>
    /// Creates a decline response.
    /// </summary>
    public static PersonaResponse Decline(string reason, string? reasoning = null)
    {
        return new PersonaResponse
        {
            Content = reason,
            ResponseType = MessageType.Decline,
            InternalReasoning = reasoning
        };
    }
}
