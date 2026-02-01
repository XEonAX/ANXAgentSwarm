using ANXAgentSwarm.Core.Enums;
using FluentAssertions;
using Moq;

namespace ANXAgentSwarm.Integration.Tests.SessionLifecycle;

/// <summary>
/// Integration tests for session clarification request and response flow.
/// </summary>
public class ClarificationFlowTests : IntegrationTestBase
{
    [Fact]
    public async Task StartSessionAsync_WithClarificationRequest_PausesSession()
    {
        // Arrange
        MockLlm.Reset();
        MockLlm.EnqueueClarification("What database system do you prefer - SQL or NoSQL?");

        // Act
        var session = await Orchestrator.StartSessionAsync("Build a data storage solution");

        // Assert
        session.Status.Should().Be(SessionStatus.WaitingForClarification);
    }

    [Fact]
    public async Task StartSessionAsync_WithClarificationRequest_BroadcastsClarificationEvent()
    {
        // Arrange
        MockLlm.Reset();
        MockLlm.EnqueueClarification("Please clarify your requirements.");

        // Act
        var session = await Orchestrator.StartSessionAsync("Vague problem statement");

        // Assert
        VerifyClarificationBroadcast(session.Id);
    }

    [Fact]
    public async Task HandleUserClarificationAsync_ResumesSession()
    {
        // Arrange
        MockLlm.Reset();
        MockLlm.EnqueueClarification("What programming language?");
        
        var session = await Orchestrator.StartSessionAsync("Build an app");
        session.Status.Should().Be(SessionStatus.WaitingForClarification);

        // Setup response after clarification
        MockLlm.EnqueueSolution("Here's the Python implementation as requested.");

        // Act
        var responseMessage = await Orchestrator.HandleUserClarificationAsync(
            session.Id, "Python please");

        // Assert
        responseMessage.Should().NotBeNull();
        
        // Reload session
        var updatedSession = await SessionRepository.GetByIdAsync(session.Id);
        updatedSession!.Status.Should().Be(SessionStatus.Completed);
    }

    [Fact]
    public async Task HandleUserClarificationAsync_CreatesUserResponseMessage()
    {
        // Arrange
        MockLlm.Reset();
        MockLlm.EnqueueClarification("What's the target platform?");
        
        var session = await Orchestrator.StartSessionAsync("Build a mobile app");
        
        MockLlm.EnqueueSolution("iOS app implemented.");

        // Act
        await Orchestrator.HandleUserClarificationAsync(session.Id, "iOS");

        // Assert
        var messages = await MessageRepository.GetBySessionIdAsync(session.Id);
        messages.Should().Contain(m => 
            m.FromPersona == PersonaType.User && 
            m.MessageType == MessageType.UserResponse &&
            m.Content == "iOS");
    }

    [Fact]
    public async Task HandleUserClarificationAsync_LinksToParentClarificationMessage()
    {
        // Arrange
        MockLlm.Reset();
        MockLlm.EnqueueClarification("Clarify please");
        
        var session = await Orchestrator.StartSessionAsync("Problem");
        
        MockLlm.EnqueueSolution("Done.");

        // Act
        await Orchestrator.HandleUserClarificationAsync(session.Id, "Response");

        // Assert
        var messages = (await MessageRepository.GetBySessionIdAsync(session.Id)).ToList();
        
        var clarificationMessage = messages.First(m => m.MessageType == MessageType.Clarification);
        var userResponse = messages.First(m => m.MessageType == MessageType.UserResponse);
        
        userResponse.ParentMessageId.Should().Be(clarificationMessage.Id);
    }

    [Fact]
    public async Task HandleUserClarificationAsync_ContinuesWithPersonaThatAsked()
    {
        // Arrange
        MockLlm.Reset();
        MockLlm.EnqueueDelegation(PersonaType.BusinessAnalyst, "Gather requirements");
        MockLlm.EnqueueClarification("What's your budget?");
        
        var session = await Orchestrator.StartSessionAsync("Build enterprise software");
        
        // After clarification, BA continues and provides solution
        MockLlm.EnqueueSolution("Based on the $100k budget, here's the plan.");
        MockLlm.EnqueueSolution("## Final Solution");

        // Act
        await Orchestrator.HandleUserClarificationAsync(session.Id, "$100k");

        // Assert
        var messages = (await MessageRepository.GetBySessionIdAsync(session.Id)).ToList();
        
        // Find the message after user response - should be from BA
        var userResponse = messages.First(m => m.MessageType == MessageType.UserResponse);
        var responseAfterClarification = messages
            .Where(m => m.Timestamp > userResponse.Timestamp)
            .OrderBy(m => m.Timestamp)
            .FirstOrDefault();
        
        responseAfterClarification.Should().NotBeNull();
        responseAfterClarification!.FromPersona.Should().Be(PersonaType.BusinessAnalyst);
    }

    [Fact]
    public async Task HandleUserClarificationAsync_WithMultipleClarifications_HandlesSequentially()
    {
        // Arrange
        MockLlm.Reset();
        MockLlm.EnqueueClarification("What's the primary use case?");
        
        var session = await Orchestrator.StartSessionAsync("Build a tool");
        
        // After first clarification, ask another
        MockLlm.EnqueueClarification("What's the expected user count?");

        // Act - first clarification
        await Orchestrator.HandleUserClarificationAsync(session.Id, "Data analysis");
        
        // Session should be waiting for clarification again
        var midSession = await SessionRepository.GetByIdAsync(session.Id);
        midSession!.Status.Should().Be(SessionStatus.WaitingForClarification);
        
        // Setup final response
        MockLlm.EnqueueSolution("Data analysis tool for 1000 users.");

        // Act - second clarification
        await Orchestrator.HandleUserClarificationAsync(session.Id, "1000 users");

        // Assert
        var finalSession = await SessionRepository.GetByIdAsync(session.Id);
        finalSession!.Status.Should().Be(SessionStatus.Completed);
    }

    [Fact]
    public async Task HandleUserClarificationAsync_ThrowsForNonWaitingSession()
    {
        // Arrange
        MockLlm.Reset();
        MockLlm.EnqueueSolution("Immediate solution.");
        
        var session = await Orchestrator.StartSessionAsync("Simple problem");
        session.Status.Should().Be(SessionStatus.Completed);

        // Act & Assert
        var act = () => Orchestrator.HandleUserClarificationAsync(session.Id, "Some response");
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not waiting for clarification*");
    }

    [Fact]
    public async Task HandleUserClarificationAsync_ThrowsForNonExistentSession()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        var act = () => Orchestrator.HandleUserClarificationAsync(nonExistentId, "Response");
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{nonExistentId}*not found*");
    }

    [Fact]
    public async Task HandleUserClarificationAsync_BroadcastsUserResponseMessage()
    {
        // Arrange
        MockLlm.Reset();
        MockLlm.EnqueueClarification("Clarify");
        
        var session = await Orchestrator.StartSessionAsync("Problem");
        MockLlm.EnqueueSolution("Solution.");

        // Clear previous invocations
        MockHubBroadcaster.Invocations.Clear();

        // Act
        await Orchestrator.HandleUserClarificationAsync(session.Id, "User response");

        // Assert - should broadcast user response and subsequent messages
        MockHubBroadcaster.Verify(
            x => x.BroadcastMessageReceivedAsync(
                session.Id,
                It.Is<Core.DTOs.MessageDto>(m => m.Content == "User response"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleUserClarificationAsync_UpdatesSessionStatus()
    {
        // Arrange
        MockLlm.Reset();
        MockLlm.EnqueueClarification("Question");
        
        var session = await Orchestrator.StartSessionAsync("Problem");
        
        MockLlm.EnqueueDelegation(PersonaType.SeniorDeveloper, "Implement");
        MockLlm.EnqueueSolution("Done.");
        MockLlm.EnqueueSolution("## Final");

        // Act
        await Orchestrator.HandleUserClarificationAsync(session.Id, "Answer");

        // Assert - status should have been broadcast as changed
        MockHubBroadcaster.Verify(
            x => x.BroadcastSessionStatusChangedAsync(
                session.Id,
                It.IsAny<Core.DTOs.SessionDto>(),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ClarificationFlow_WithDelegationAfterClarification_Works()
    {
        // Arrange
        MockLlm.Reset();
        MockLlm.EnqueueClarification("Which cloud provider?");
        
        var session = await Orchestrator.StartSessionAsync("Deploy application");
        
        // After clarification, delegate to TA for architecture
        MockLlm.EnqueueDelegation(PersonaType.TechnicalArchitect, "Design AWS architecture");
        MockLlm.EnqueueDelegation(PersonaType.SeniorDeveloper, "Implement deployment");
        MockLlm.EnqueueSolution("Deployment scripts ready.");
        MockLlm.EnqueueSolution("## AWS Deployment Complete");

        // Act
        await Orchestrator.HandleUserClarificationAsync(session.Id, "AWS");

        // Assert
        var finalSession = await SessionRepository.GetByIdAsync(session.Id);
        finalSession!.Status.Should().Be(SessionStatus.Completed);
        
        var messages = await MessageRepository.GetBySessionIdAsync(session.Id);
        messages.Should().Contain(m => m.FromPersona == PersonaType.TechnicalArchitect);
        messages.Should().Contain(m => m.FromPersona == PersonaType.SeniorDeveloper);
    }
}
