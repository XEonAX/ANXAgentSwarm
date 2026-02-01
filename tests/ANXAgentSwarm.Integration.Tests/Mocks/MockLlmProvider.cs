using ANXAgentSwarm.Core.Enums;
using ANXAgentSwarm.Core.Interfaces;
using ANXAgentSwarm.Core.Models;

namespace ANXAgentSwarm.Integration.Tests.Mocks;

/// <summary>
/// Mock LLM provider for deterministic integration testing.
/// Allows configuring expected responses for specific personas and scenarios.
/// </summary>
public class MockLlmProvider : ILlmProvider
{
    private readonly Queue<LlmResponse> _responseQueue = new();
    private readonly Dictionary<PersonaType, Queue<LlmResponse>> _personaResponses = new();
    private readonly List<LlmRequest> _receivedRequests = new();
    private LlmResponse? _defaultResponse;
    private Func<LlmRequest, LlmResponse>? _dynamicResponseHandler;

    /// <summary>
    /// Gets all requests that were received by this mock.
    /// </summary>
    public IReadOnlyList<LlmRequest> ReceivedRequests => _receivedRequests.AsReadOnly();

    /// <summary>
    /// Gets the count of remaining queued responses.
    /// </summary>
    public int RemainingResponses => _responseQueue.Count;

    /// <summary>
    /// Enqueues a response to be returned by the next call to GenerateAsync.
    /// </summary>
    public MockLlmProvider EnqueueResponse(LlmResponse response)
    {
        _responseQueue.Enqueue(response);
        return this;
    }

    /// <summary>
    /// Enqueues a successful response with the given content.
    /// </summary>
    public MockLlmProvider EnqueueContent(string content)
    {
        return EnqueueResponse(new LlmResponse { Success = true, Content = content });
    }

    /// <summary>
    /// Enqueues a delegation response.
    /// </summary>
    public MockLlmProvider EnqueueDelegation(PersonaType toPersona, string context)
    {
        var content = $"[DELEGATE:{toPersona}] {context}";
        return EnqueueContent(content);
    }

    /// <summary>
    /// Enqueues a solution response.
    /// </summary>
    public MockLlmProvider EnqueueSolution(string solution)
    {
        return EnqueueContent($"[SOLUTION] {solution}");
    }

    /// <summary>
    /// Enqueues a clarification request.
    /// </summary>
    public MockLlmProvider EnqueueClarification(string question)
    {
        return EnqueueContent($"[CLARIFY] {question}");
    }

    /// <summary>
    /// Enqueues a stuck response.
    /// </summary>
    public MockLlmProvider EnqueueStuck(string reason)
    {
        return EnqueueContent($"[STUCK] {reason}");
    }

    /// <summary>
    /// Enqueues a decline response.
    /// </summary>
    public MockLlmProvider EnqueueDecline(string reason)
    {
        return EnqueueContent($"[DECLINE] {reason}");
    }

    /// <summary>
    /// Enqueues an answer response.
    /// </summary>
    public MockLlmProvider EnqueueAnswer(string answer)
    {
        return EnqueueContent(answer);
    }

    /// <summary>
    /// Enqueues an error response.
    /// </summary>
    public MockLlmProvider EnqueueError(string error)
    {
        return EnqueueResponse(new LlmResponse { Success = false, Error = error });
    }

    /// <summary>
    /// Enqueues a response with internal reasoning.
    /// </summary>
    public MockLlmProvider EnqueueWithReasoning(string content, string reasoning)
    {
        return EnqueueContent($"[REASONING]{reasoning}[/REASONING]\n{content}");
    }

    /// <summary>
    /// Enqueues a response with memory store command.
    /// </summary>
    public MockLlmProvider EnqueueWithMemoryStore(string content, string identifier, string memoryContent)
    {
        return EnqueueContent($"{content}\n[STORE:{identifier}] {memoryContent}");
    }

    /// <summary>
    /// Sets a response to be returned for a specific persona.
    /// </summary>
    public MockLlmProvider SetPersonaResponse(PersonaType persona, LlmResponse response)
    {
        if (!_personaResponses.ContainsKey(persona))
        {
            _personaResponses[persona] = new Queue<LlmResponse>();
        }
        _personaResponses[persona].Enqueue(response);
        return this;
    }

    /// <summary>
    /// Sets the default response when no queued responses are available.
    /// </summary>
    public MockLlmProvider SetDefaultResponse(LlmResponse response)
    {
        _defaultResponse = response;
        return this;
    }

    /// <summary>
    /// Sets a dynamic response handler for complex scenarios.
    /// </summary>
    public MockLlmProvider SetDynamicHandler(Func<LlmRequest, LlmResponse> handler)
    {
        _dynamicResponseHandler = handler;
        return this;
    }

    /// <summary>
    /// Clears all queued responses and recorded requests.
    /// </summary>
    public void Reset()
    {
        _responseQueue.Clear();
        _personaResponses.Clear();
        _receivedRequests.Clear();
        _defaultResponse = null;
        _dynamicResponseHandler = null;
    }

    /// <inheritdoc />
    public Task<LlmResponse> GenerateAsync(LlmRequest request, CancellationToken cancellationToken = default)
    {
        _receivedRequests.Add(request);

        // Check for dynamic handler first
        if (_dynamicResponseHandler != null)
        {
            return Task.FromResult(_dynamicResponseHandler(request));
        }

        // Try to get queued response
        if (_responseQueue.Count > 0)
        {
            return Task.FromResult(_responseQueue.Dequeue());
        }

        // Return default or error
        if (_defaultResponse != null)
        {
            return Task.FromResult(_defaultResponse);
        }

        return Task.FromResult(new LlmResponse
        {
            Success = false,
            Error = "No mock response configured"
        });
    }

    /// <inheritdoc />
    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<IEnumerable<string>> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<string>>(new[] { "mock-model", "gemma3:27b" });
    }
}

/// <summary>
/// Factory methods for common mock LLM scenarios.
/// </summary>
public static class MockLlmScenarios
{
    /// <summary>
    /// Creates a mock that simulates a simple delegation chain ending in a solution.
    /// Coordinator -> TechnicalArchitect -> SeniorDeveloper -> Solution
    /// </summary>
    public static MockLlmProvider SimpleDelegationChain()
    {
        var mock = new MockLlmProvider();

        // Coordinator delegates to Technical Architect
        mock.EnqueueDelegation(
            PersonaType.TechnicalArchitect,
            "Please design the system architecture for this problem.");

        // Technical Architect delegates to Senior Developer
        mock.EnqueueDelegation(
            PersonaType.SeniorDeveloper,
            "I've designed the architecture. Please implement the solution.");

        // Senior Developer provides solution
        mock.EnqueueSolution("Here is the complete implementation:\n```csharp\n// Implementation code\n```");

        // Coordinator compiles final solution
        mock.EnqueueSolution("## Final Solution\n\nBased on the team's work, here is the complete solution...");

        return mock;
    }

    /// <summary>
    /// Creates a mock that simulates a clarification request flow.
    /// </summary>
    public static MockLlmProvider ClarificationFlow()
    {
        var mock = new MockLlmProvider();

        // Coordinator asks for clarification
        mock.EnqueueClarification("What database system would you prefer - SQL or NoSQL?");

        // After user responds, Coordinator continues
        mock.EnqueueDelegation(
            PersonaType.TechnicalArchitect,
            "User wants SQL database. Please design the schema.");

        // Technical Architect provides solution
        mock.EnqueueSolution("Here is the SQL schema design...");

        // Coordinator compiles
        mock.EnqueueSolution("## Final Solution\n\nDatabase schema designed as requested.");

        return mock;
    }

    /// <summary>
    /// Creates a mock that simulates a session getting stuck.
    /// </summary>
    public static MockLlmProvider StuckFlow()
    {
        var mock = new MockLlmProvider();

        // Coordinator delegates
        mock.EnqueueDelegation(
            PersonaType.TechnicalArchitect,
            "Please analyze this complex problem.");

        // Technical Architect is stuck
        mock.EnqueueStuck("I need more information about the external API specification.");

        // Sent back to Coordinator
        mock.EnqueueStuck("The team cannot proceed without the API specification.");

        // More stuck responses to trigger stuck state
        mock.EnqueueStuck("Still stuck.");
        mock.EnqueueStuck("Still stuck.");
        mock.EnqueueStuck("Still stuck.");

        // Coordinator compiles partial solution
        mock.EnqueueSolution("## Partial Solution\n\nWe were unable to complete the request. Here's what we have so far...");

        return mock;
    }

    /// <summary>
    /// Creates a mock that simulates a decline and alternative path.
    /// </summary>
    public static MockLlmProvider DeclineFlow()
    {
        var mock = new MockLlmProvider();

        // Coordinator delegates to wrong persona
        mock.EnqueueDelegation(
            PersonaType.SeniorDeveloper,
            "Please design the user interface.");

        // Developer declines
        mock.EnqueueDecline("This is a UI design task, which should go to the UI Engineer.");

        // Coordinator redirects
        mock.EnqueueDelegation(
            PersonaType.UIEngineer,
            "Please design the user interface.");

        // UI Engineer provides solution
        mock.EnqueueSolution("Here is the UI design with Tailwind CSS...");

        // Coordinator compiles
        mock.EnqueueSolution("## Final Solution\n\nUI design completed.");

        return mock;
    }

    /// <summary>
    /// Creates a mock for a multi-step delegation chain.
    /// </summary>
    public static MockLlmProvider MultiStepChain(int steps)
    {
        var mock = new MockLlmProvider();
        var personas = new[]
        {
            PersonaType.TechnicalArchitect,
            PersonaType.SeniorDeveloper,
            PersonaType.SeniorQA,
            PersonaType.DocumentWriter
        };

        // Initial coordinator delegation
        mock.EnqueueDelegation(personas[0], "Step 1: Architecture");

        for (int i = 1; i < Math.Min(steps, personas.Length); i++)
        {
            mock.EnqueueDelegation(personas[i], $"Step {i + 1}: Continue with {personas[i]}");
        }

        // Final persona provides solution
        mock.EnqueueSolution("Multi-step solution complete.");
        mock.EnqueueSolution("## Final Solution\n\nAll steps completed.");

        return mock;
    }

    /// <summary>
    /// Creates a mock for instant solution (no delegation).
    /// </summary>
    public static MockLlmProvider InstantSolution(string solution)
    {
        var mock = new MockLlmProvider();
        mock.EnqueueSolution(solution);
        return mock;
    }

    /// <summary>
    /// Creates a mock for resume scenario.
    /// </summary>
    public static MockLlmProvider ResumeAfterStuck()
    {
        var mock = new MockLlmProvider();

        // After resume, Coordinator tries new approach
        mock.EnqueueDelegation(
            PersonaType.BusinessAnalyst,
            "Let's try a different approach. Please gather more requirements.");

        // Business Analyst provides insight
        mock.EnqueueDelegation(
            PersonaType.SeniorDeveloper,
            "I've clarified the requirements. Please implement.");

        // Developer provides solution
        mock.EnqueueSolution("Implementation complete based on new requirements.");

        // Coordinator compiles
        mock.EnqueueSolution("## Final Solution\n\nSession recovered and completed.");

        return mock;
    }
}
