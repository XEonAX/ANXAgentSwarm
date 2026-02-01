using ANXAgentSwarm.Core.Enums;
using FluentAssertions;

namespace ANXAgentSwarm.Integration.Tests.SessionLifecycle;

/// <summary>
/// Integration tests for sessions that get stuck and partial solution handling.
/// </summary>
public class StuckSessionTests : IntegrationTestBase
{
    [Fact]
    public async Task StartSessionAsync_WithSingleStuck_SendsToCoordinator()
    {
        // Arrange
        MockLlm.Reset();
        MockLlm.EnqueueDelegation(PersonaType.TechnicalArchitect, "Design architecture");
        MockLlm.EnqueueStuck("I need more information about the API specification.");
        MockLlm.EnqueueSolution("Let me provide an alternative solution without the API.");

        // Act
        var session = await Orchestrator.StartSessionAsync("Integrate with external API");

        // Assert
        session.Status.Should().Be(SessionStatus.Completed);
    }

    [Fact]
    public async Task StartSessionAsync_WithConsecutiveStuck_MarksSessionAsStuck()
    {
        // Arrange
        MockLlm.Reset();
        MockLlm.EnqueueDelegation(PersonaType.TechnicalArchitect, "Design");
        
        // TA is stuck
        MockLlm.EnqueueStuck("Cannot proceed without hardware specs.");
        
        // Coordinator is also stuck
        MockLlm.EnqueueStuck("Team cannot proceed.");
        MockLlm.EnqueueStuck("Still stuck.");
        MockLlm.EnqueueStuck("Still stuck.");
        MockLlm.EnqueueStuck("Still stuck.");
        MockLlm.EnqueueStuck("Still stuck.");

        // Act
        var session = await Orchestrator.StartSessionAsync("Design hardware interface");

        // Assert
        session.Status.Should().Be(SessionStatus.Stuck);
    }

    [Fact]
    public async Task StartSessionAsync_WhenStuck_BroadcastsStuckEvent()
    {
        // Arrange
        MockLlm.Reset();
        // Immediate stuck from coordinator
        MockLlm.EnqueueStuck("Cannot help with this request.");
        MockLlm.EnqueueStuck("Still cannot proceed.");
        MockLlm.EnqueueStuck("Stuck.");
        MockLlm.EnqueueStuck("Stuck.");
        MockLlm.EnqueueStuck("Stuck.");

        // Act
        var session = await Orchestrator.StartSessionAsync("Impossible task");

        // Assert
        VerifySessionStuckBroadcast(session.Id);
    }

    [Fact]
    public async Task StartSessionAsync_WithStuckRecovery_CompletesSuccessfully()
    {
        // Arrange
        MockLlm.Reset();
        MockLlm.EnqueueDelegation(PersonaType.SeniorDeveloper, "Implement feature");
        MockLlm.EnqueueStuck("I'm stuck on the algorithm.");
        // Coordinator tries alternative
        MockLlm.EnqueueDelegation(PersonaType.TechnicalArchitect, "Help with algorithm");
        MockLlm.EnqueueSolution("Here's the algorithm design.");
        MockLlm.EnqueueSolution("## Final Solution with algorithm");

        // Act
        var session = await Orchestrator.StartSessionAsync("Implement sorting algorithm");

        // Assert
        session.Status.Should().Be(SessionStatus.Completed);
        session.FinalSolution.Should().Contain("algorithm");
    }

    [Fact]
    public async Task StartSessionAsync_WithDecline_TriesAlternative()
    {
        // Arrange
        MockLlm.Reset();
        MockLlm.EnqueueDelegation(PersonaType.JuniorDeveloper, "Implement complex feature");
        MockLlm.EnqueueDecline("This is too complex for me. Please assign to Senior Developer.");
        MockLlm.EnqueueDelegation(PersonaType.SeniorDeveloper, "Implement complex feature");
        MockLlm.EnqueueSolution("Complex feature implemented.");
        MockLlm.EnqueueSolution("## Final Solution");

        // Act
        var session = await Orchestrator.StartSessionAsync("Implement caching layer");

        // Assert
        session.Status.Should().Be(SessionStatus.Completed);
        
        var messages = await MessageRepository.GetBySessionIdAsync(session.Id);
        messages.Should().Contain(m => m.MessageType == MessageType.Decline);
        messages.Should().Contain(m => m.FromPersona == PersonaType.SeniorDeveloper);
    }

    [Fact]
    public async Task StartSessionAsync_StuckCreatesStuckMessage()
    {
        // Arrange
        MockLlm.Reset();
        MockLlm.EnqueueStuck("Cannot proceed.");
        MockLlm.EnqueueStuck("Still stuck.");
        MockLlm.EnqueueStuck("Stuck.");
        MockLlm.EnqueueStuck("Stuck.");
        MockLlm.EnqueueStuck("Stuck.");

        // Act
        var session = await Orchestrator.StartSessionAsync("Impossible task");

        // Assert
        var messages = await MessageRepository.GetBySessionIdAsync(session.Id);
        messages.Should().Contain(m => m.MessageType == MessageType.Stuck);
    }

    [Fact]
    public async Task StartSessionAsync_MultiplePersonasStuck_EventuallyMarksSessionStuck()
    {
        // Arrange
        MockLlm.Reset();
        // Coordinator tries different personas
        MockLlm.EnqueueDelegation(PersonaType.TechnicalArchitect, "Design");
        MockLlm.EnqueueStuck("TA stuck.");
        MockLlm.EnqueueDelegation(PersonaType.SeniorDeveloper, "Try implementing");
        MockLlm.EnqueueStuck("Dev stuck.");
        MockLlm.EnqueueDelegation(PersonaType.BusinessAnalyst, "Analyze");
        MockLlm.EnqueueStuck("BA stuck.");
        MockLlm.EnqueueStuck("Coordinator stuck.");
        MockLlm.EnqueueStuck("Still stuck.");

        // Act
        var session = await Orchestrator.StartSessionAsync("Unsolvable problem");

        // Assert
        session.Status.Should().Be(SessionStatus.Stuck);
    }

    [Fact]
    public async Task StartSessionAsync_StuckAfterProgress_PreservesMessages()
    {
        // Arrange
        MockLlm.Reset();
        MockLlm.EnqueueDelegation(PersonaType.BusinessAnalyst, "Gather requirements");
        MockLlm.EnqueueWithReasoning(
            "[DELEGATE:TechnicalArchitect] Design system",
            "Requirements gathered successfully.");
        MockLlm.EnqueueDelegation(PersonaType.SeniorDeveloper, "Implement");
        MockLlm.EnqueueStuck("Cannot implement without external service access.");
        MockLlm.EnqueueStuck("Still stuck.");
        MockLlm.EnqueueStuck("Stuck.");
        MockLlm.EnqueueStuck("Stuck.");
        MockLlm.EnqueueStuck("Stuck.");

        // Act
        var session = await Orchestrator.StartSessionAsync("Build with external dependencies");

        // Assert
        var messages = await MessageRepository.GetBySessionIdAsync(session.Id);
        
        // Verify progress was made before getting stuck
        messages.Should().Contain(m => m.FromPersona == PersonaType.BusinessAnalyst);
        messages.Should().Contain(m => m.FromPersona == PersonaType.TechnicalArchitect);
        messages.Should().Contain(m => m.FromPersona == PersonaType.SeniorDeveloper);
    }

    [Fact]
    public async Task StartSessionAsync_StuckWithInternalReasoning_PreservesReasoning()
    {
        // Arrange
        MockLlm.Reset();
        MockLlm.EnqueueWithReasoning(
            "[STUCK] Cannot proceed without database access.",
            "I analyzed the problem and determined that we need direct database access which is not available.");
        MockLlm.EnqueueStuck("Still stuck.");
        MockLlm.EnqueueStuck("Stuck.");
        MockLlm.EnqueueStuck("Stuck.");
        MockLlm.EnqueueStuck("Stuck.");

        // Act
        var session = await Orchestrator.StartSessionAsync("Direct database query");

        // Assert
        var messages = await MessageRepository.GetBySessionIdAsync(session.Id);
        var stuckMessage = messages.First(m => m.MessageType == MessageType.Stuck);
        stuckMessage.InternalReasoning.Should().Contain("database access");
    }

    [Fact]
    public async Task StartSessionAsync_LlmError_HandlesGracefully()
    {
        // Arrange
        MockLlm.Reset();
        MockLlm.EnqueueError("LLM service unavailable");

        // Act
        var session = await Orchestrator.StartSessionAsync("Test problem");

        // Assert - should handle error as stuck
        var messages = await MessageRepository.GetBySessionIdAsync(session.Id);
        messages.Should().Contain(m => m.IsStuck);
    }

    [Fact]
    public async Task StartSessionAsync_MaxDelegationDepth_MarksAsStuck()
    {
        // Arrange
        MockLlm.Reset();
        
        // Create a loop of delegations that exceeds max depth
        for (int i = 0; i < 60; i++) // Max is 50
        {
            var persona = (PersonaType)((i % 9) + 1); // Cycle through personas
            MockLlm.EnqueueDelegation(persona, $"Step {i}");
        }

        // Act
        var session = await Orchestrator.StartSessionAsync("Infinite loop problem");

        // Assert
        session.Status.Should().Be(SessionStatus.Stuck);
    }
}
