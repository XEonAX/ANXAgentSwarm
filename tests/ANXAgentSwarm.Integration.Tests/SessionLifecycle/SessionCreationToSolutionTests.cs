using ANXAgentSwarm.Core.Enums;
using ANXAgentSwarm.Integration.Tests.Mocks;
using FluentAssertions;
using Moq;

namespace ANXAgentSwarm.Integration.Tests.SessionLifecycle;

/// <summary>
/// Integration tests for full session lifecycle - creation to solution.
/// </summary>
public class SessionCreationToSolutionTests : IntegrationTestBase
{
    [Fact]
    public async Task StartSessionAsync_WithSimpleDelegationChain_CompletesSuccessfully()
    {
        // Arrange
        var mockLlm = MockLlmScenarios.SimpleDelegationChain();
        MockLlm.Reset();
        
        // Coordinator delegates to Technical Architect
        MockLlm.EnqueueDelegation(
            PersonaType.TechnicalArchitect,
            "Please design the system architecture.");

        // Technical Architect delegates to Senior Developer
        MockLlm.EnqueueDelegation(
            PersonaType.SeniorDeveloper,
            "Architecture designed. Please implement.");

        // Senior Developer provides solution
        MockLlm.EnqueueSolution("Here is the implementation code.");

        // Coordinator compiles final solution
        MockLlm.EnqueueSolution("## Final Solution\n\nComplete implementation provided.");

        // Act
        var session = await Orchestrator.StartSessionAsync("Build a REST API for products");

        // Assert
        session.Should().NotBeNull();
        session.Status.Should().Be(SessionStatus.Completed);
        session.FinalSolution.Should().NotBeNullOrEmpty();
        session.FinalSolution.Should().Contain("Final Solution");
    }

    [Fact]
    public async Task StartSessionAsync_CreatesSessionWithCorrectProblemStatement()
    {
        // Arrange
        var problemStatement = "Create a user authentication system";
        MockLlm.Reset();
        MockLlm.EnqueueSolution("Authentication system designed.");

        // Act
        var session = await Orchestrator.StartSessionAsync(problemStatement);

        // Assert
        session.ProblemStatement.Should().Be(problemStatement);
    }

    [Fact]
    public async Task StartSessionAsync_GeneratesTitle()
    {
        // Arrange
        MockLlm.Reset();
        MockLlm.EnqueueSolution("Solution provided.");

        // Act
        var session = await Orchestrator.StartSessionAsync("Build a todo app with React");

        // Assert
        session.Title.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task StartSessionAsync_CreatesInitialUserMessage()
    {
        // Arrange
        var problemStatement = "Test problem statement";
        MockLlm.Reset();
        MockLlm.EnqueueSolution("Solution.");

        // Act
        var session = await Orchestrator.StartSessionAsync(problemStatement);

        // Assert
        var messages = await MessageRepository.GetBySessionIdAsync(session.Id);
        messages.Should().ContainSingle(m => 
            m.FromPersona == PersonaType.User && 
            m.Content == problemStatement);
    }

    [Fact]
    public async Task StartSessionAsync_BroadcastsMessagesViaSignalR()
    {
        // Arrange
        MockLlm.Reset();
        MockLlm.EnqueueSolution("Quick solution.");

        // Act
        var session = await Orchestrator.StartSessionAsync("Simple problem");

        // Assert - at minimum user message + coordinator response
        MockHubBroadcaster.Verify(
            x => x.BroadcastMessageReceivedAsync(
                session.Id,
                It.IsAny<Core.DTOs.MessageDto>(),
                It.IsAny<CancellationToken>()),
            Times.AtLeast(2));
    }

    [Fact]
    public async Task StartSessionAsync_BroadcastsSolutionReady()
    {
        // Arrange
        MockLlm.Reset();
        MockLlm.EnqueueSolution("Final solution ready.");

        // Act
        var session = await Orchestrator.StartSessionAsync("Problem to solve");

        // Assert
        VerifySolutionBroadcast(session.Id);
    }

    [Fact]
    public async Task StartSessionAsync_WithMultipleDelegations_TracksAllMessages()
    {
        // Arrange
        MockLlm.Reset();
        MockLlm.EnqueueDelegation(PersonaType.BusinessAnalyst, "Analyze requirements");
        MockLlm.EnqueueDelegation(PersonaType.TechnicalArchitect, "Design system");
        MockLlm.EnqueueDelegation(PersonaType.SeniorDeveloper, "Implement");
        MockLlm.EnqueueSolution("Implementation complete.");
        MockLlm.EnqueueSolution("## Final Solution");

        // Act
        var session = await Orchestrator.StartSessionAsync("Complex multi-step problem");

        // Assert
        var messages = (await MessageRepository.GetBySessionIdAsync(session.Id)).ToList();
        
        // Should have: user message + coordinator + BA + TA + SrDev + Coordinator (compile)
        messages.Count.Should().BeGreaterThanOrEqualTo(5);
        
        // Verify delegation chain
        messages.Should().Contain(m => m.FromPersona == PersonaType.Coordinator);
        messages.Should().Contain(m => m.FromPersona == PersonaType.BusinessAnalyst);
        messages.Should().Contain(m => m.FromPersona == PersonaType.TechnicalArchitect);
        messages.Should().Contain(m => m.FromPersona == PersonaType.SeniorDeveloper);
    }

    [Fact]
    public async Task StartSessionAsync_WithInstantSolution_CompletesImmediately()
    {
        // Arrange
        MockLlm.Reset();
        MockLlm.EnqueueSolution("This is a simple problem. Here's the solution.");

        // Act
        var session = await Orchestrator.StartSessionAsync("What is 2+2?");

        // Assert
        session.Status.Should().Be(SessionStatus.Completed);
        session.FinalSolution.Should().Contain("solution");
        
        var messages = (await MessageRepository.GetBySessionIdAsync(session.Id)).ToList();
        // Just user message + coordinator solution
        messages.Count.Should().Be(2);
    }

    [Fact]
    public async Task StartSessionAsync_RecordsTimestamps()
    {
        // Arrange
        var beforeTest = DateTime.UtcNow;
        MockLlm.Reset();
        MockLlm.EnqueueSolution("Solution.");

        // Act
        var session = await Orchestrator.StartSessionAsync("Test");
        var afterTest = DateTime.UtcNow;

        // Assert
        session.CreatedAt.Should().BeOnOrAfter(beforeTest);
        session.CreatedAt.Should().BeOnOrBefore(afterTest);
        session.UpdatedAt.Should().BeOnOrAfter(session.CreatedAt);
    }

    [Fact]
    public async Task StartSessionAsync_SetsCurrentPersonaToNull_WhenCompleted()
    {
        // Arrange
        MockLlm.Reset();
        MockLlm.EnqueueSolution("Done.");

        // Act
        var session = await Orchestrator.StartSessionAsync("Problem");

        // Assert
        session.CurrentPersona.Should().BeNull();
    }

    [Fact]
    public async Task StartSessionAsync_WithDelegationToAllPersonas_HandlesFullChain()
    {
        // Arrange
        MockLlm.Reset();
        MockLlm.EnqueueDelegation(PersonaType.BusinessAnalyst, "Requirements");
        MockLlm.EnqueueDelegation(PersonaType.UXEngineer, "User flows");
        MockLlm.EnqueueDelegation(PersonaType.UIEngineer, "Visual design");
        MockLlm.EnqueueDelegation(PersonaType.TechnicalArchitect, "Architecture");
        MockLlm.EnqueueDelegation(PersonaType.SeniorDeveloper, "Implementation");
        MockLlm.EnqueueDelegation(PersonaType.JuniorDeveloper, "Basic tasks");
        MockLlm.EnqueueDelegation(PersonaType.SeniorQA, "Testing strategy");
        MockLlm.EnqueueDelegation(PersonaType.JuniorQA, "Execute tests");
        MockLlm.EnqueueDelegation(PersonaType.DocumentWriter, "Documentation");
        MockLlm.EnqueueSolution("All documentation complete.");
        MockLlm.EnqueueSolution("## Complete Solution\n\nFull team collaboration complete.");

        // Act
        var session = await Orchestrator.StartSessionAsync("Full team project");

        // Assert
        session.Status.Should().Be(SessionStatus.Completed);
        
        var messages = await MessageRepository.GetBySessionIdAsync(session.Id);
        
        // Verify all personas participated
        var participatingPersonas = messages.Select(m => m.FromPersona).Distinct().ToList();
        participatingPersonas.Should().Contain(PersonaType.Coordinator);
        participatingPersonas.Should().Contain(PersonaType.BusinessAnalyst);
        participatingPersonas.Should().Contain(PersonaType.TechnicalArchitect);
        participatingPersonas.Should().Contain(PersonaType.SeniorDeveloper);
    }

    [Fact]
    public async Task StartSessionAsync_WithAnswerResponse_ReturnsToCoordinator()
    {
        // Arrange
        MockLlm.Reset();
        MockLlm.EnqueueDelegation(PersonaType.TechnicalArchitect, "Design");
        // Answer must be > 100 characters to trigger return to Coordinator
        MockLlm.EnqueueAnswer("Architecture design: Use microservices pattern with separate services for authentication, user management, and core business logic. Each service should communicate via REST APIs with proper rate limiting and circuit breakers for resilience.");
        MockLlm.EnqueueSolution("## Final Solution based on architecture");

        // Act
        var session = await Orchestrator.StartSessionAsync("Design a scalable system");

        // Assert
        session.Status.Should().Be(SessionStatus.Completed);
    }
}
