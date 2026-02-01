using ANXAgentSwarm.Core.DTOs;
using ANXAgentSwarm.Core.Entities;
using ANXAgentSwarm.Core.Enums;
using ANXAgentSwarm.Core.Extensions;
using ANXAgentSwarm.Core.Interfaces;
using ANXAgentSwarm.Core.Models;
using Microsoft.Extensions.Logging;

namespace ANXAgentSwarm.Infrastructure.Services;

/// <summary>
/// Orchestrates the flow of messages between personas in a problem-solving session.
/// </summary>
public class AgentOrchestrator : IAgentOrchestrator
{
    private readonly ISessionRepository _sessionRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly IMemoryService _memoryService;
    private readonly IPersonaEngine _personaEngine;
    private readonly ISessionHubBroadcaster _hubBroadcaster;
    private readonly ILogger<AgentOrchestrator> _logger;

    /// <summary>
    /// Maximum number of delegation steps to prevent infinite loops.
    /// </summary>
    private const int MaxDelegationDepth = 50;

    /// <summary>
    /// Maximum number of consecutive stuck responses before declaring session stuck.
    /// </summary>
    private const int MaxConsecutiveStuck = 5;

    public AgentOrchestrator(
        ISessionRepository sessionRepository,
        IMessageRepository messageRepository,
        IMemoryService memoryService,
        IPersonaEngine personaEngine,
        ISessionHubBroadcaster hubBroadcaster,
        ILogger<AgentOrchestrator> logger)
    {
        _sessionRepository = sessionRepository;
        _messageRepository = messageRepository;
        _memoryService = memoryService;
        _personaEngine = personaEngine;
        _hubBroadcaster = hubBroadcaster;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Session> StartSessionAsync(
        string problemStatement,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting new session with problem statement");

        // Create the session
        var session = new Session
        {
            Id = Guid.NewGuid(),
            Title = GenerateTitle(problemStatement),
            Status = SessionStatus.Active,
            ProblemStatement = problemStatement,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CurrentPersona = PersonaType.Coordinator
        };

        session = await _sessionRepository.CreateAsync(session, cancellationToken);

        // Create the initial problem statement message from the user
        var userMessage = new Message
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            FromPersona = PersonaType.User,
            ToPersona = PersonaType.Coordinator,
            Content = problemStatement,
            MessageType = MessageType.ProblemStatement,
            Timestamp = DateTime.UtcNow
        };

        await _messageRepository.CreateAsync(userMessage, cancellationToken);

        // Broadcast the initial message
        await _hubBroadcaster.BroadcastMessageReceivedAsync(
            session.Id, userMessage.ToDto(), cancellationToken);

        // Process with the Coordinator
        await ProcessWithPersonaAsync(session, userMessage, PersonaType.Coordinator, cancellationToken);

        // Reload session to get updated state
        session = await _sessionRepository.GetByIdWithMessagesAsync(session.Id, cancellationToken)
            ?? throw new InvalidOperationException("Session not found after creation");

        return session;
    }

    /// <inheritdoc />
    public async Task<Message> ProcessDelegationAsync(
        Guid sessionId,
        Guid messageId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Processing delegation for session {SessionId}, message {MessageId}",
            sessionId, messageId);

        var session = await _sessionRepository.GetByIdWithMessagesAsync(sessionId, cancellationToken)
            ?? throw new InvalidOperationException($"Session {sessionId} not found");

        var message = await _messageRepository.GetByIdAsync(messageId, cancellationToken)
            ?? throw new InvalidOperationException($"Message {messageId} not found");

        if (message.MessageType != MessageType.Delegation || message.DelegateToPersona == null)
        {
            throw new InvalidOperationException("Message is not a delegation");
        }

        // Process with the delegated persona
        var responseMessage = await ProcessWithPersonaAsync(
            session, message, message.DelegateToPersona.Value, cancellationToken);

        return responseMessage;
    }

    /// <inheritdoc />
    public async Task<Message> HandleUserClarificationAsync(
        Guid sessionId,
        string response,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling user clarification for session {SessionId}", sessionId);

        var session = await _sessionRepository.GetByIdWithMessagesAsync(sessionId, cancellationToken)
            ?? throw new InvalidOperationException($"Session {sessionId} not found");

        if (session.Status != SessionStatus.WaitingForClarification)
        {
            throw new InvalidOperationException(
                $"Session is not waiting for clarification. Current status: {session.Status}");
        }

        // Find the last clarification request to know which persona to respond to
        var lastClarification = session.Messages
            .OrderByDescending(m => m.Timestamp)
            .FirstOrDefault(m => m.MessageType == MessageType.Clarification)
            ?? throw new InvalidOperationException("No clarification request found in session");

        // Create the user's response message
        var userMessage = new Message
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            FromPersona = PersonaType.User,
            ToPersona = lastClarification.FromPersona, // Respond to the persona that asked
            Content = response,
            MessageType = MessageType.UserResponse,
            Timestamp = DateTime.UtcNow,
            ParentMessageId = lastClarification.Id
        };

        await _messageRepository.CreateAsync(userMessage, cancellationToken);

        // Broadcast the user response message
        await _hubBroadcaster.BroadcastMessageReceivedAsync(
            session.Id, userMessage.ToDto(), cancellationToken);

        // Update session status back to active
        session.Status = SessionStatus.Active;
        session.UpdatedAt = DateTime.UtcNow;
        await _sessionRepository.UpdateAsync(session, cancellationToken);

        // Broadcast status change
        await _hubBroadcaster.BroadcastSessionStatusChangedAsync(
            session.Id, session.ToDto(), cancellationToken);

        // Continue processing with the persona that requested clarification
        var responseMessage = await ProcessWithPersonaAsync(
            session, userMessage, lastClarification.FromPersona, cancellationToken);

        return responseMessage;
    }

    /// <inheritdoc />
    public async Task<Session> ResumeSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Resuming session {SessionId}", sessionId);

        var session = await _sessionRepository.GetByIdWithMessagesAsync(sessionId, cancellationToken)
            ?? throw new InvalidOperationException($"Session {sessionId} not found");

        if (session.Status == SessionStatus.Completed || session.Status == SessionStatus.Cancelled)
        {
            throw new InvalidOperationException(
                $"Cannot resume a {session.Status} session");
        }

        // Find the last message
        var lastMessage = session.Messages
            .OrderByDescending(m => m.Timestamp)
            .FirstOrDefault();

        if (lastMessage == null)
        {
            throw new InvalidOperationException("Session has no messages to resume from");
        }

        // Determine the next step based on the last message
        if (lastMessage.MessageType == MessageType.Delegation && lastMessage.DelegateToPersona != null)
        {
            // Process the delegation
            session.Status = SessionStatus.Active;
            session.UpdatedAt = DateTime.UtcNow;
            await _sessionRepository.UpdateAsync(session, cancellationToken);

            await ProcessWithPersonaAsync(
                session, lastMessage, lastMessage.DelegateToPersona.Value, cancellationToken);
        }
        else if (lastMessage.MessageType == MessageType.Stuck)
        {
            // Try to recover - send back to Coordinator for alternative approach
            session.Status = SessionStatus.Active;
            session.UpdatedAt = DateTime.UtcNow;
            await _sessionRepository.UpdateAsync(session, cancellationToken);

            await ProcessWithPersonaAsync(
                session, lastMessage, PersonaType.Coordinator, cancellationToken);
        }

        // Reload session to get updated state
        return await _sessionRepository.GetByIdWithMessagesAsync(sessionId, cancellationToken)
            ?? throw new InvalidOperationException("Session not found after resume");
    }

    /// <inheritdoc />
    public async Task CancelSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Cancelling session {SessionId}", sessionId);

        var session = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken)
            ?? throw new InvalidOperationException($"Session {sessionId} not found");

        session.Status = SessionStatus.Cancelled;
        session.UpdatedAt = DateTime.UtcNow;

        await _sessionRepository.UpdateAsync(session, cancellationToken);
    }

    /// <summary>
    /// Processes a message with a specific persona and continues the delegation chain.
    /// </summary>
    private async Task<Message> ProcessWithPersonaAsync(
        Session session,
        Message incomingMessage,
        PersonaType persona,
        CancellationToken cancellationToken)
    {
        int delegationDepth = 0;
        int consecutiveStuckCount = 0;
        var stuckPersonas = new HashSet<PersonaType>();
        var currentMessage = incomingMessage;
        var currentPersona = persona;

        while (delegationDepth < MaxDelegationDepth)
        {
            delegationDepth++;

            _logger.LogDebug(
                "Processing with {Persona}, depth {Depth}",
                currentPersona, delegationDepth);

            // Update session's current persona
            session.CurrentPersona = currentPersona;
            session.UpdatedAt = DateTime.UtcNow;
            await _sessionRepository.UpdateAsync(session, cancellationToken);

            // Get relevant memories for the persona
            var memories = await _memoryService.GetRecentMemoriesAsync(
                session.Id, currentPersona, 10, cancellationToken);

            // Process with the persona engine
            var response = await _personaEngine.ProcessAsync(
                currentPersona,
                currentMessage,
                session,
                memories,
                cancellationToken);

            // Create the response message
            var responseMessage = CreateMessageFromResponse(
                session.Id,
                currentPersona,
                currentMessage,
                response);

            await _messageRepository.CreateAsync(responseMessage, cancellationToken);

            // Broadcast the message received
            await _hubBroadcaster.BroadcastMessageReceivedAsync(
                session.Id, responseMessage.ToDto(), cancellationToken);

            // Handle the response based on its type
            switch (response.ResponseType)
            {
                case MessageType.Solution:
                    // Solution found - complete the session
                    await HandleSolutionAsync(session, responseMessage, response, cancellationToken);
                    return responseMessage;

                case MessageType.Clarification:
                    // Need user input - pause the session
                    await HandleClarificationAsync(session, responseMessage, cancellationToken);
                    return responseMessage;

                case MessageType.Delegation:
                    // Continue with the delegated persona
                    if (response.DelegateToPersona == null)
                    {
                        _logger.LogWarning("Delegation response missing target persona");
                        break;
                    }
                    consecutiveStuckCount = 0; // Reset stuck counter
                    currentMessage = responseMessage;
                    currentPersona = response.DelegateToPersona.Value;
                    continue;

                case MessageType.Decline:
                    // Persona declined - try alternative
                    return await HandleDeclineAsync(
                        session, responseMessage, currentMessage, cancellationToken);

                case MessageType.Stuck:
                    // Persona is stuck
                    stuckPersonas.Add(currentPersona);
                    consecutiveStuckCount++;

                    if (consecutiveStuckCount >= MaxConsecutiveStuck ||
                        stuckPersonas.Count >= GetActivePersonaCount())
                    {
                        // All personas are stuck - compile partial solution
                        await HandleAllStuckAsync(session, cancellationToken);
                        return responseMessage;
                    }

                    // Try to recover via Coordinator
                    if (currentPersona != PersonaType.Coordinator)
                    {
                        currentMessage = responseMessage;
                        currentPersona = PersonaType.Coordinator;
                        continue;
                    }

                    // Coordinator is also stuck - session is stuck
                    await HandleAllStuckAsync(session, cancellationToken);
                    return responseMessage;

                case MessageType.Answer:
                default:
                    // Check if this is a return to Coordinator with a compiled answer
                    if (ShouldReturnToCoordinator(session, currentPersona, response))
                    {
                        currentMessage = responseMessage;
                        currentPersona = PersonaType.Coordinator;
                        continue;
                    }

                    // Regular answer - the flow might be complete
                    return responseMessage;
            }
        }

        // Max delegation depth reached
        _logger.LogWarning(
            "Max delegation depth ({MaxDepth}) reached for session {SessionId}",
            MaxDelegationDepth, session.Id);

        session.Status = SessionStatus.Stuck;
        session.UpdatedAt = DateTime.UtcNow;
        await _sessionRepository.UpdateAsync(session, cancellationToken);

        return await CreateStuckMessageAsync(
            session,
            "Maximum delegation depth reached. Please try rephrasing your problem or breaking it into smaller parts.",
            cancellationToken);
    }

    /// <summary>
    /// Handles a solution response by completing the session.
    /// </summary>
    private async Task HandleSolutionAsync(
        Session session,
        Message solutionMessage,
        PersonaResponse response,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Solution found for session {SessionId}", session.Id);

        // If the solution is not from the Coordinator, send it back for compilation
        if (solutionMessage.FromPersona != PersonaType.Coordinator)
        {
            _logger.LogDebug("Sending solution back to Coordinator for final compilation");

            // Let Coordinator compile the final solution
            var memories = await _memoryService.GetRecentMemoriesAsync(
                session.Id, PersonaType.Coordinator, 10, cancellationToken);

            var coordinatorResponse = await _personaEngine.ProcessAsync(
                PersonaType.Coordinator,
                solutionMessage,
                session,
                memories,
                cancellationToken);

            var compiledMessage = CreateMessageFromResponse(
                session.Id,
                PersonaType.Coordinator,
                solutionMessage,
                coordinatorResponse);

            await _messageRepository.CreateAsync(compiledMessage, cancellationToken);

            // Broadcast the compiled message
            await _hubBroadcaster.BroadcastMessageReceivedAsync(
                session.Id, compiledMessage.ToDto(), cancellationToken);

            session.FinalSolution = coordinatorResponse.Content;
        }
        else
        {
            session.FinalSolution = response.Content;
        }

        session.Status = SessionStatus.Completed;
        session.CurrentPersona = null;
        session.UpdatedAt = DateTime.UtcNow;
        await _sessionRepository.UpdateAsync(session, cancellationToken);

        // Broadcast solution ready
        await _hubBroadcaster.BroadcastSolutionReadyAsync(
            session.Id, session.ToDto(), cancellationToken);
    }

    /// <summary>
    /// Handles a clarification request by pausing the session.
    /// </summary>
    private async Task HandleClarificationAsync(
        Session session,
        Message clarificationMessage,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Clarification requested by {Persona} in session {SessionId}",
            clarificationMessage.FromPersona, session.Id);

        session.Status = SessionStatus.WaitingForClarification;
        session.UpdatedAt = DateTime.UtcNow;
        await _sessionRepository.UpdateAsync(session, cancellationToken);

        // Broadcast status change
        await _hubBroadcaster.BroadcastSessionStatusChangedAsync(
            session.Id, session.ToDto(), cancellationToken);

        // Broadcast clarification request
        await _hubBroadcaster.BroadcastClarificationRequestedAsync(
            session.Id, clarificationMessage.ToDto(), cancellationToken);
    }

    /// <summary>
    /// Handles a decline response by finding an alternative persona.
    /// </summary>
    private async Task<Message> HandleDeclineAsync(
        Session session,
        Message declineMessage,
        Message originalRequest,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Delegation declined by {Persona} in session {SessionId}",
            declineMessage.FromPersona, session.Id);

        // Send back to Coordinator to find an alternative
        var memories = await _memoryService.GetRecentMemoriesAsync(
            session.Id, PersonaType.Coordinator, 10, cancellationToken);

        var response = await _personaEngine.ProcessAsync(
            PersonaType.Coordinator,
            declineMessage,
            session,
            memories,
            cancellationToken);

        var coordinatorMessage = CreateMessageFromResponse(
            session.Id,
            PersonaType.Coordinator,
            declineMessage,
            response);

        await _messageRepository.CreateAsync(coordinatorMessage, cancellationToken);

        // Broadcast the coordinator's response
        await _hubBroadcaster.BroadcastMessageReceivedAsync(
            session.Id, coordinatorMessage.ToDto(), cancellationToken);

        // If Coordinator delegated to someone else, continue processing
        if (response.ResponseType == MessageType.Delegation && response.DelegateToPersona != null)
        {
            return await ProcessWithPersonaAsync(
                session, coordinatorMessage, response.DelegateToPersona.Value, cancellationToken);
        }

        return coordinatorMessage;
    }

    /// <summary>
    /// Handles the case when all personas are stuck.
    /// </summary>
    private async Task HandleAllStuckAsync(
        Session session,
        CancellationToken cancellationToken)
    {
        _logger.LogWarning("All personas stuck in session {SessionId}", session.Id);

        // Compile partial results from the session
        var partialSolution = await CompilePartialSolutionAsync(session, cancellationToken);

        session.Status = SessionStatus.Stuck;
        session.FinalSolution = partialSolution;
        session.CurrentPersona = null;
        session.UpdatedAt = DateTime.UtcNow;
        await _sessionRepository.UpdateAsync(session, cancellationToken);

        // Broadcast session stuck
        await _hubBroadcaster.BroadcastSessionStuckAsync(
            session.Id, session.ToDto(), partialSolution, cancellationToken);
    }

    /// <summary>
    /// Compiles a partial solution from all contributions in the session.
    /// </summary>
    private async Task<string> CompilePartialSolutionAsync(
        Session session,
        CancellationToken cancellationToken)
    {
        var messages = await _messageRepository.GetBySessionIdAsync(session.Id, cancellationToken);
        var contributions = new List<string>();

        foreach (var message in messages.Where(m =>
            m.FromPersona != PersonaType.User &&
            m.MessageType != MessageType.Stuck &&
            m.MessageType != MessageType.Decline &&
            !string.IsNullOrWhiteSpace(m.Content)))
        {
            contributions.Add($"**{message.FromPersona}:**\n{message.Content}");
        }

        if (!contributions.Any())
        {
            return "Unfortunately, no progress was made on this problem. Please try rephrasing your request or breaking it into smaller parts.";
        }

        return $"""
            # Partial Solution (Session Incomplete)
            
            The team was unable to complete the solution, but here is what was accomplished:
            
            {string.Join("\n\n---\n\n", contributions)}
            
            ---
            
            ## What's Missing
            
            The team encountered difficulties and was unable to proceed further. You may:
            1. Provide additional clarification or context
            2. Break the problem into smaller, more specific tasks
            3. Try a different approach to the problem
            """;
    }

    /// <summary>
    /// Creates a message entity from a persona response.
    /// </summary>
    private static Message CreateMessageFromResponse(
        Guid sessionId,
        PersonaType fromPersona,
        Message parentMessage,
        PersonaResponse response)
    {
        return new Message
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            FromPersona = fromPersona,
            ToPersona = response.DelegateToPersona,
            Content = response.Content,
            MessageType = response.ResponseType,
            InternalReasoning = response.InternalReasoning,
            Timestamp = DateTime.UtcNow,
            ParentMessageId = parentMessage.Id,
            DelegateToPersona = response.DelegateToPersona,
            DelegationContext = response.DelegationContext,
            IsStuck = response.IsStuck,
            RawResponse = response.RawResponse
        };
    }

    /// <summary>
    /// Creates a stuck message for error scenarios.
    /// </summary>
    private async Task<Message> CreateStuckMessageAsync(
        Session session,
        string reason,
        CancellationToken cancellationToken)
    {
        var message = new Message
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            FromPersona = PersonaType.Coordinator,
            Content = reason,
            MessageType = MessageType.Stuck,
            IsStuck = true,
            Timestamp = DateTime.UtcNow
        };

        return await _messageRepository.CreateAsync(message, cancellationToken);
    }

    /// <summary>
    /// Determines if the flow should return to the Coordinator.
    /// </summary>
    private static bool ShouldReturnToCoordinator(
        Session session,
        PersonaType currentPersona,
        PersonaResponse response)
    {
        // If we're already the Coordinator, don't return to ourselves
        if (currentPersona == PersonaType.Coordinator)
            return false;

        // Check if the response seems complete (no further delegation)
        // The Coordinator should compile final results
        if (response.ResponseType == MessageType.Answer &&
            response.DelegateToPersona == null &&
            response.Content.Length > 100) // Substantial answer
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets the count of active (non-User) personas.
    /// </summary>
    private static int GetActivePersonaCount()
    {
        // All personas except User
        return Enum.GetValues<PersonaType>().Length - 1;
    }

    /// <summary>
    /// Generates a title from the problem statement.
    /// </summary>
    private static string GenerateTitle(string problemStatement)
    {
        if (string.IsNullOrWhiteSpace(problemStatement))
            return "Untitled Session";

        // Take the first 50 characters or first sentence, whichever is shorter
        var firstSentenceEnd = problemStatement.IndexOfAny(['.', '?', '!', '\n']);
        var truncateAt = firstSentenceEnd > 0 && firstSentenceEnd < 50
            ? firstSentenceEnd
            : Math.Min(50, problemStatement.Length);

        var title = problemStatement[..truncateAt].Trim();

        if (truncateAt < problemStatement.Length)
            title += "...";

        return title;
    }
}
