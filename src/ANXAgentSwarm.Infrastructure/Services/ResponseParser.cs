using System.Text.RegularExpressions;
using ANXAgentSwarm.Core.Enums;
using ANXAgentSwarm.Core.Models;

namespace ANXAgentSwarm.Infrastructure.Services;

/// <summary>
/// Parses LLM responses to extract structured data like delegations, clarifications, etc.
/// </summary>
public static partial class ResponseParser
{
    // Regex patterns for parsing response tags
    private static readonly Regex DelegatePattern = DelegateRegex();
    private static readonly Regex ClarifyPattern = ClarifyRegex();
    private static readonly Regex SolutionPattern = SolutionRegex();
    private static readonly Regex StuckPattern = StuckRegex();
    private static readonly Regex StorePattern = StoreRegex();
    private static readonly Regex RememberPattern = RememberRegex();
    private static readonly Regex DeclinePattern = DeclineRegex();
    private static readonly Regex ReasoningPattern = ReasoningRegex();

    /// <summary>
    /// Parses an LLM response and extracts structured data.
    /// </summary>
    /// <param name="rawResponse">The raw LLM response text.</param>
    /// <returns>A parsed PersonaResponse object.</returns>
    public static PersonaResponse Parse(string rawResponse)
    {
        if (string.IsNullOrWhiteSpace(rawResponse))
        {
            return new PersonaResponse
            {
                Content = string.Empty,
                ResponseType = MessageType.Answer,
                RawResponse = rawResponse
            };
        }

        var response = new PersonaResponse
        {
            RawResponse = rawResponse
        };

        // Extract internal reasoning if present
        var reasoningMatch = ReasoningPattern.Match(rawResponse);
        if (reasoningMatch.Success)
        {
            response.InternalReasoning = reasoningMatch.Groups["reasoning"].Value.Trim();
            // Remove reasoning from content
            rawResponse = ReasoningPattern.Replace(rawResponse, "").Trim();
        }

        // Check for delegation
        var delegateMatch = DelegatePattern.Match(rawResponse);
        if (delegateMatch.Success)
        {
            var personaName = delegateMatch.Groups["persona"].Value;
            var context = delegateMatch.Groups["context"].Value.Trim();

            response.ResponseType = MessageType.Delegation;
            response.DelegateToPersona = ParsePersonaType(personaName);
            response.DelegationContext = context;
            response.Content = GetContentBeforeTag(rawResponse, "[DELEGATE:");
            
            if (string.IsNullOrWhiteSpace(response.Content))
            {
                response.Content = context;
            }

            return response;
        }

        // Check for clarification
        var clarifyMatch = ClarifyPattern.Match(rawResponse);
        if (clarifyMatch.Success)
        {
            var question = clarifyMatch.Groups["question"].Value.Trim();
            response.ResponseType = MessageType.Clarification;
            response.ClarificationQuestion = question;
            response.Content = GetContentBeforeTag(rawResponse, "[CLARIFY]");
            
            if (string.IsNullOrWhiteSpace(response.Content))
            {
                response.Content = question;
            }

            return response;
        }

        // Check for solution
        var solutionMatch = SolutionPattern.Match(rawResponse);
        if (solutionMatch.Success)
        {
            var solution = solutionMatch.Groups["solution"].Value.Trim();
            response.ResponseType = MessageType.Solution;
            response.Content = GetContentBeforeTag(rawResponse, "[SOLUTION]");
            
            if (string.IsNullOrWhiteSpace(response.Content))
            {
                response.Content = solution;
            }
            else
            {
                response.Content = $"{response.Content}\n\n{solution}";
            }

            return response;
        }

        // Check for stuck
        var stuckMatch = StuckPattern.Match(rawResponse);
        if (stuckMatch.Success)
        {
            var reason = stuckMatch.Groups["reason"].Value.Trim();
            response.ResponseType = MessageType.Stuck;
            response.IsStuck = true;
            response.Content = GetContentBeforeTag(rawResponse, "[STUCK]");
            
            if (string.IsNullOrWhiteSpace(response.Content))
            {
                response.Content = reason;
            }
            else
            {
                response.Content = $"{response.Content}\n\n{reason}";
            }

            return response;
        }

        // Check for decline
        var declineMatch = DeclinePattern.Match(rawResponse);
        if (declineMatch.Success)
        {
            var reason = declineMatch.Groups["reason"].Value.Trim();
            response.ResponseType = MessageType.Decline;
            response.Content = GetContentBeforeTag(rawResponse, "[DECLINE]");
            
            if (string.IsNullOrWhiteSpace(response.Content))
            {
                response.Content = reason;
            }

            return response;
        }

        // Default to answer type
        response.ResponseType = MessageType.Answer;
        response.Content = rawResponse.Trim();

        return response;
    }

    /// <summary>
    /// Extracts memory store commands from the response.
    /// </summary>
    /// <param name="rawResponse">The raw LLM response text.</param>
    /// <returns>A list of (identifier, content) tuples for memories to store.</returns>
    public static List<(string Identifier, string Content)> ExtractMemoryStores(string rawResponse)
    {
        var memories = new List<(string Identifier, string Content)>();
        
        if (string.IsNullOrWhiteSpace(rawResponse))
        {
            return memories;
        }

        var matches = StorePattern.Matches(rawResponse);
        foreach (Match match in matches)
        {
            var identifier = match.Groups["identifier"].Value.Trim();
            var content = match.Groups["content"].Value.Trim();
            memories.Add((identifier, content));
        }

        return memories;
    }

    /// <summary>
    /// Extracts memory recall requests from the response.
    /// </summary>
    /// <param name="rawResponse">The raw LLM response text.</param>
    /// <returns>A list of memory identifiers to recall.</returns>
    public static List<string> ExtractMemoryRecalls(string rawResponse)
    {
        var recalls = new List<string>();
        
        if (string.IsNullOrWhiteSpace(rawResponse))
        {
            return recalls;
        }

        var matches = RememberPattern.Matches(rawResponse);
        foreach (Match match in matches)
        {
            var identifier = match.Groups["identifier"].Value.Trim();
            recalls.Add(identifier);
        }

        return recalls;
    }

    /// <summary>
    /// Gets the content before a specific tag.
    /// </summary>
    private static string GetContentBeforeTag(string rawResponse, string tag)
    {
        var index = rawResponse.IndexOf(tag, StringComparison.OrdinalIgnoreCase);
        if (index <= 0)
        {
            return string.Empty;
        }

        return rawResponse[..index].Trim();
    }

    /// <summary>
    /// Parses a persona name string to PersonaType enum.
    /// </summary>
    public static PersonaType? ParsePersonaType(string personaName)
    {
        if (string.IsNullOrWhiteSpace(personaName))
        {
            return null;
        }

        // Normalize the name for matching
        var normalized = personaName
            .Replace(" ", "")
            .Replace("-", "")
            .Replace("_", "")
            .Trim();

        // Try exact match first
        if (Enum.TryParse<PersonaType>(normalized, ignoreCase: true, out var result))
        {
            return result;
        }

        // Try common variations
        return normalized.ToLowerInvariant() switch
        {
            "coordinator" or "coord" => PersonaType.Coordinator,
            "businessanalyst" or "ba" => PersonaType.BusinessAnalyst,
            "technicalarchitect" or "ta" or "architect" => PersonaType.TechnicalArchitect,
            "seniordeveloper" or "seniordev" or "srdev" or "srdeveloper" => PersonaType.SeniorDeveloper,
            "juniordeveloper" or "juniordev" or "jrdev" or "jrdeveloper" => PersonaType.JuniorDeveloper,
            "seniorqa" or "srqa" => PersonaType.SeniorQA,
            "juniorqa" or "jrqa" => PersonaType.JuniorQA,
            "uxengineer" or "ux" => PersonaType.UXEngineer,
            "uiengineer" or "ui" => PersonaType.UIEngineer,
            "documentwriter" or "docwriter" or "doc" or "docs" => PersonaType.DocumentWriter,
            _ => null
        };
    }

    /// <summary>
    /// Removes all parsing tags from content for clean display.
    /// </summary>
    public static string CleanContent(string rawResponse)
    {
        if (string.IsNullOrWhiteSpace(rawResponse))
        {
            return string.Empty;
        }

        var cleaned = rawResponse;

        // Remove all known tags
        cleaned = DelegatePattern.Replace(cleaned, "");
        cleaned = ClarifyPattern.Replace(cleaned, "");
        cleaned = SolutionPattern.Replace(cleaned, "");
        cleaned = StuckPattern.Replace(cleaned, "");
        cleaned = StorePattern.Replace(cleaned, "");
        cleaned = RememberPattern.Replace(cleaned, "");
        cleaned = DeclinePattern.Replace(cleaned, "");
        cleaned = ReasoningPattern.Replace(cleaned, "");

        // Clean up extra whitespace
        cleaned = Regex.Replace(cleaned, @"\n{3,}", "\n\n");
        
        return cleaned.Trim();
    }

    // Generated regex patterns for performance
    [GeneratedRegex(@"\[DELEGATE:(?<persona>[^\]]+)\]\s*(?<context>[\s\S]*?)(?=\[(?:DELEGATE|CLARIFY|SOLUTION|STUCK|STORE|REMEMBER|DECLINE)\]|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex DelegateRegex();

    [GeneratedRegex(@"\[CLARIFY\]\s*(?<question>[\s\S]*?)(?=\[(?:DELEGATE|CLARIFY|SOLUTION|STUCK|STORE|REMEMBER|DECLINE)\]|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex ClarifyRegex();

    [GeneratedRegex(@"\[SOLUTION\]\s*(?<solution>[\s\S]*?)(?=\[(?:DELEGATE|CLARIFY|SOLUTION|STUCK|STORE|REMEMBER|DECLINE)\]|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex SolutionRegex();

    [GeneratedRegex(@"\[STUCK\]\s*(?<reason>[\s\S]*?)(?=\[(?:DELEGATE|CLARIFY|SOLUTION|STUCK|STORE|REMEMBER|DECLINE)\]|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex StuckRegex();

    [GeneratedRegex(@"\[STORE:(?<identifier>[^\]]+)\]\s*(?<content>[\s\S]*?)(?=\[(?:DELEGATE|CLARIFY|SOLUTION|STUCK|STORE|REMEMBER|DECLINE)\]|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex StoreRegex();

    [GeneratedRegex(@"\[REMEMBER:(?<identifier>[^\]]+)\]", RegexOptions.IgnoreCase)]
    private static partial Regex RememberRegex();

    [GeneratedRegex(@"\[DECLINE\]\s*(?<reason>[\s\S]*?)(?=\[(?:DELEGATE|CLARIFY|SOLUTION|STUCK|STORE|REMEMBER|DECLINE)\]|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex DeclineRegex();

    [GeneratedRegex(@"\[REASONING\]\s*(?<reasoning>[\s\S]*?)\[/REASONING\]", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex ReasoningRegex();
}
