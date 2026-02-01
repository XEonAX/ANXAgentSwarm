using System.Globalization;
using System.Text;
using ANXAgentSwarm.Core.Entities;
using ANXAgentSwarm.Core.Enums;
using ANXAgentSwarm.Core.Interfaces;
using ANXAgentSwarm.Core.Models;
using Microsoft.Extensions.Logging;

namespace ANXAgentSwarm.Infrastructure.Services;

#pragma warning disable CA1848 // Use LoggerMessage delegates for performance
#pragma warning disable CA1305 // Specify IFormatProvider

/// <summary>
/// Engine for processing messages through personas using the LLM provider.
/// </summary>
public class PersonaEngine : IPersonaEngine
{
    private readonly ILlmProvider _llmProvider;
    private readonly IPersonaConfigurationRepository _personaConfigRepository;
    private readonly IMemoryService _memoryService;
    private readonly IWorkspaceService _workspaceService;
    private readonly ILogger<PersonaEngine> _logger;

    public PersonaEngine(
        ILlmProvider llmProvider,
        IPersonaConfigurationRepository personaConfigRepository,
        IMemoryService memoryService,
        IWorkspaceService workspaceService,
        ILogger<PersonaEngine> logger)
    {
        _llmProvider = llmProvider;
        _personaConfigRepository = personaConfigRepository;
        _memoryService = memoryService;
        _workspaceService = workspaceService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<PersonaResponse> ProcessAsync(
        PersonaType persona,
        Message incomingMessage,
        Session session,
        IEnumerable<Memory> relevantMemories,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Processing message for persona {Persona} in session {SessionId}",
            persona, session.Id);

        // Get persona configuration
        var config = await GetPersonaConfigurationAsync(persona, cancellationToken);
        if (config == null)
        {
            _logger.LogError("No configuration found for persona {Persona}", persona);
            return PersonaResponse.Stuck(
                $"Configuration error: No configuration found for {persona}",
                "Unable to find persona configuration in the database.");
        }

        if (!config.IsEnabled)
        {
            _logger.LogWarning("Persona {Persona} is disabled", persona);
            return PersonaResponse.Decline(
                $"I am currently unavailable. Please try another team member.",
                $"Persona {persona} is disabled in configuration.");
        }

        // Build the context for the LLM
        var systemPrompt = BuildSystemPrompt(config, session, relevantMemories);
        var messages = BuildMessageHistory(session, incomingMessage);

        // Create LLM request
        var request = new LlmRequest
        {
            Model = config.ModelName,
            SystemPrompt = systemPrompt,
            Messages = messages,
            Temperature = config.Temperature,
            MaxTokens = config.MaxTokens
        };

        try
        {
            // Call LLM
            var llmResponse = await _llmProvider.GenerateAsync(request, cancellationToken);

            if (!llmResponse.Success)
            {
                _logger.LogError(
                    "LLM request failed for persona {Persona}: {Error}",
                    persona, llmResponse.Error);

                return PersonaResponse.Stuck(
                    $"I encountered an error processing your request: {llmResponse.Error}",
                    $"LLM API call failed: {llmResponse.Error}");
            }

            // Parse the response
            var parsedResponse = ResponseParser.Parse(llmResponse.Content);
            parsedResponse.RawResponse = llmResponse.Content;

            // Process any memory store commands
            await ProcessMemoryCommands(session.Id, persona, llmResponse.Content, cancellationToken);

            // Process any file write commands
            await ProcessFileCommands(llmResponse.Content, cancellationToken);

            _logger.LogInformation(
                "Persona {Persona} responded with type {ResponseType}",
                persona, parsedResponse.ResponseType);

            return parsedResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message for persona {Persona}", persona);

            return PersonaResponse.Stuck(
                "I encountered an unexpected error. Please try again.",
                $"Exception during processing: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<PersonaConfiguration?> GetPersonaConfigurationAsync(
        PersonaType persona,
        CancellationToken cancellationToken = default)
    {
        return await _personaConfigRepository.GetByPersonaTypeAsync(persona, cancellationToken);
    }

    /// <summary>
    /// Builds the system prompt with session context and memories.
    /// </summary>
    private static string BuildSystemPrompt(
        PersonaConfiguration config,
        Session session,
        IEnumerable<Memory> memories)
    {
        var sb = new StringBuilder();
        
        // Add the base system prompt
        sb.AppendLine(config.SystemPrompt);
        sb.AppendLine();

        // Add response format instructions
        sb.AppendLine("## Response Format");
        sb.AppendLine("Use these tags to communicate:");
        sb.AppendLine("- [DELEGATE:PersonaName] context - Delegate to another team member");
        sb.AppendLine("- [CLARIFY] question - Ask the user for clarification");
        sb.AppendLine("- [SOLUTION] solution - Provide a solution");
        sb.AppendLine("- [STUCK] reason - Indicate you cannot proceed");
        sb.AppendLine("- [DECLINE] reason - Decline a delegation");
        sb.AppendLine("- [STORE:identifier] content - Store important information to memory");
        sb.AppendLine("- [REMEMBER:identifier] - Recall stored information");
        sb.AppendLine("- [REASONING] your thinking [/REASONING] - Show your internal reasoning");
        sb.AppendLine("- [FILE:relative/path/to/file.ext] file content here [/FILE] - Create or overwrite a file in the workspace");
        sb.AppendLine();
        sb.AppendLine("## File Creation");
        sb.AppendLine("When creating files (code, HTML, documents, etc.), use the [FILE:path] tag:");
        sb.AppendLine("Example: [FILE:hello.html]<!DOCTYPE html><html>...</html>[/FILE]");
        sb.AppendLine("- Use relative paths only (e.g., 'output/index.html', not '/output/index.html')");
        sb.AppendLine("- Directories will be created automatically");
        sb.AppendLine("- You can create multiple files in a single response");
        sb.AppendLine();

        // Add session context
        sb.AppendLine("## Current Session Context");
        sb.AppendLine($"Session ID: {session.Id}");
        sb.AppendLine($"Status: {session.Status}");
        sb.AppendLine($"Problem Statement: {session.ProblemStatement}");
        sb.AppendLine();

        // Add memories if available
        var memoryList = memories.ToList();
        if (memoryList.Count > 0)
        {
            sb.AppendLine("## Your Memories");
            foreach (var memory in memoryList)
            {
                sb.AppendLine($"### [{memory.Identifier}]");
                sb.AppendLine(memory.Content);
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Builds the message history for the LLM conversation.
    /// </summary>
    private static List<ChatMessage> BuildMessageHistory(Session session, Message incomingMessage)
    {
        var messages = new List<ChatMessage>();

        // Add recent message history (last 10 messages for context)
        var recentMessages = session.Messages
            .OrderBy(m => m.Timestamp)
            .TakeLast(10)
            .ToList();

        foreach (var msg in recentMessages)
        {
            var role = msg.FromPersona == PersonaType.User ? "user" : "assistant";
            messages.Add(new ChatMessage
            {
                Role = role,
                Content = msg.Content
            });
        }

        // Add the incoming message
        messages.Add(new ChatMessage
        {
            Role = incomingMessage.FromPersona == PersonaType.User ? "user" : "user",
            Content = BuildIncomingMessageContent(incomingMessage)
        });

        return messages;
    }

    /// <summary>
    /// Builds the content for an incoming message with context.
    /// </summary>
    private static string BuildIncomingMessageContent(Message message)
    {
        var sb = new StringBuilder();

        // Add sender context
        if (message.FromPersona != PersonaType.User)
        {
            sb.AppendLine($"[From: {message.FromPersona}]");
        }

        // Add delegation context if present
        if (!string.IsNullOrWhiteSpace(message.DelegationContext))
        {
            sb.AppendLine($"[Context: {message.DelegationContext}]");
        }

        sb.AppendLine(message.Content);

        return sb.ToString();
    }

    /// <summary>
    /// Processes any memory store/recall commands in the response.
    /// </summary>
    private async Task ProcessMemoryCommands(
        Guid sessionId,
        PersonaType persona,
        string rawResponse,
        CancellationToken cancellationToken)
    {
        // Extract and store any new memories
        var memoryStores = ResponseParser.ExtractMemoryStores(rawResponse);
        foreach (var (identifier, content) in memoryStores)
        {
            try
            {
                await _memoryService.StoreMemoryAsync(
                    sessionId, persona, identifier, content, cancellationToken);

                _logger.LogDebug(
                    "Stored memory '{Identifier}' for persona {Persona}",
                    identifier, persona);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(
                    "Failed to store memory '{Identifier}': {Error}",
                    identifier, ex.Message);
            }
        }
    }

    /// <summary>
    /// Processes any file write commands in the response.
    /// </summary>
    private async Task ProcessFileCommands(
        string rawResponse,
        CancellationToken cancellationToken)
    {
        var fileWrites = ResponseParser.ExtractFileWrites(rawResponse);
        foreach (var (filePath, content) in fileWrites)
        {
            try
            {
                await _workspaceService.WriteFileAsync(filePath, content, cancellationToken);

                _logger.LogInformation(
                    "Created file in workspace: {FilePath}",
                    filePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    "Failed to write file '{FilePath}': {Error}",
                    filePath, ex.Message);
            }
        }
    }
}
