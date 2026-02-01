using ANXAgentSwarm.Core.DTOs;
using ANXAgentSwarm.Core.Enums;
using FluentAssertions;
using Moq;

namespace ANXAgentSwarm.Integration.Tests.SignalR;

/// <summary>
/// Integration tests for SignalR hub communication and message broadcasting.
/// </summary>
public class SignalRBroadcastTests : IntegrationTestBase
{
    [Fact]
    public async Task StartSessionAsync_BroadcastsUserMessage()
    {
        // Arrange
        MockLlm.Reset();
        MockLlm.EnqueueSolution("Solution.");

        // Act
        var session = await Orchestrator.StartSessionAsync("Test problem");

        // Assert
        MockHubBroadcaster.Verify(
            x => x.BroadcastMessageReceivedAsync(
                session.Id,
                It.Is<MessageDto>(m => m.FromPersona == PersonaType.User),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task StartSessionAsync_BroadcastsCoordinatorResponse()
    {
        // Arrange
        MockLlm.Reset();
        MockLlm.EnqueueSolution("Coordinator solution.");

        // Act
        var session = await Orchestrator.StartSessionAsync("Problem");

        // Assert
        MockHubBroadcaster.Verify(
            x => x.BroadcastMessageReceivedAsync(
                session.Id,
                It.Is<MessageDto>(m => m.FromPersona == PersonaType.Coordinator),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task StartSessionAsync_BroadcastsAllDelegationMessages()
    {
        // Arrange
        MockLlm.Reset();
        MockLlm.EnqueueDelegation(PersonaType.TechnicalArchitect, "Design");
        MockLlm.EnqueueDelegation(PersonaType.SeniorDeveloper, "Implement");
        MockLlm.EnqueueSolution("Done.");
        MockLlm.EnqueueSolution("## Final");

        // Act
        var session = await Orchestrator.StartSessionAsync("Multi-step problem");

        // Assert
        MockHubBroadcaster.Verify(
            x => x.BroadcastMessageReceivedAsync(
                session.Id,
                It.Is<MessageDto>(m => m.FromPersona == PersonaType.TechnicalArchitect),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);

        MockHubBroadcaster.Verify(
            x => x.BroadcastMessageReceivedAsync(
                session.Id,
                It.Is<MessageDto>(m => m.FromPersona == PersonaType.SeniorDeveloper),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task StartSessionAsync_BroadcastsSolutionReady_OnCompletion()
    {
        // Arrange
        MockLlm.Reset();
        MockLlm.EnqueueSolution("Complete solution.");

        // Act
        var session = await Orchestrator.StartSessionAsync("Problem");

        // Assert
        MockHubBroadcaster.Verify(
            x => x.BroadcastSolutionReadyAsync(
                session.Id,
                It.Is<SessionDto>(s => s.Status == SessionStatus.Completed),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task StartSessionAsync_BroadcastsClarificationRequested()
    {
        // Arrange
        MockLlm.Reset();
        MockLlm.EnqueueClarification("What framework do you prefer?");

        // Act
        var session = await Orchestrator.StartSessionAsync("Build an app");

        // Assert
        MockHubBroadcaster.Verify(
            x => x.BroadcastClarificationRequestedAsync(
                session.Id,
                It.Is<MessageDto>(m => m.MessageType == MessageType.Clarification),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task StartSessionAsync_BroadcastsSessionStatusChanged_OnClarification()
    {
        // Arrange
        MockLlm.Reset();
        MockLlm.EnqueueClarification("Clarify please");

        // Act
        var session = await Orchestrator.StartSessionAsync("Problem");

        // Assert
        MockHubBroadcaster.Verify(
            x => x.BroadcastSessionStatusChangedAsync(
                session.Id,
                It.Is<SessionDto>(s => s.Status == SessionStatus.WaitingForClarification),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task StartSessionAsync_BroadcastsSessionStuck()
    {
        // Arrange
        MockLlm.Reset();
        MockLlm.EnqueueStuck("Cannot proceed.");
        MockLlm.EnqueueStuck("Still stuck.");
        MockLlm.EnqueueStuck("Stuck.");
        MockLlm.EnqueueStuck("Stuck.");
        MockLlm.EnqueueStuck("Stuck.");

        // Act
        var session = await Orchestrator.StartSessionAsync("Impossible problem");

        // Assert
        MockHubBroadcaster.Verify(
            x => x.BroadcastSessionStuckAsync(
                session.Id,
                It.Is<SessionDto>(s => s.Status == SessionStatus.Stuck),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task HandleUserClarificationAsync_BroadcastsUserResponse()
    {
        // Arrange
        MockLlm.Reset();
        MockLlm.EnqueueClarification("Question");
        var session = await Orchestrator.StartSessionAsync("Problem");

        MockLlm.EnqueueSolution("Answer processed.");
        MockHubBroadcaster.Invocations.Clear();

        // Act
        await Orchestrator.HandleUserClarificationAsync(session.Id, "My answer");

        // Assert
        MockHubBroadcaster.Verify(
            x => x.BroadcastMessageReceivedAsync(
                session.Id,
                It.Is<MessageDto>(m => 
                    m.FromPersona == PersonaType.User && 
                    m.MessageType == MessageType.UserResponse),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleUserClarificationAsync_BroadcastsStatusChange()
    {
        // Arrange
        MockLlm.Reset();
        MockLlm.EnqueueClarification("Question");
        var session = await Orchestrator.StartSessionAsync("Problem");

        MockLlm.EnqueueSolution("Done.");
        MockHubBroadcaster.Invocations.Clear();

        // Act
        await Orchestrator.HandleUserClarificationAsync(session.Id, "Answer");

        // Assert
        MockHubBroadcaster.Verify(
            x => x.BroadcastSessionStatusChangedAsync(
                session.Id,
                It.Is<SessionDto>(s => s.Status == SessionStatus.Active),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task BroadcastMessages_ContainCorrectMessageType()
    {
        // Arrange
        MockLlm.Reset();
        MockLlm.EnqueueDelegation(PersonaType.TechnicalArchitect, "Design");
        MockLlm.EnqueueSolution("Done.");
        MockLlm.EnqueueSolution("## Final");

        var capturedMessages = new List<MessageDto>();
        MockHubBroadcaster
            .Setup(x => x.BroadcastMessageReceivedAsync(
                It.IsAny<Guid>(),
                It.IsAny<MessageDto>(),
                It.IsAny<CancellationToken>()))
            .Callback<Guid, MessageDto, CancellationToken>((_, msg, _) => capturedMessages.Add(msg))
            .Returns(Task.CompletedTask);

        // Act
        await Orchestrator.StartSessionAsync("Problem");

        // Assert
        capturedMessages.Should().Contain(m => m.MessageType == MessageType.ProblemStatement);
        capturedMessages.Should().Contain(m => m.MessageType == MessageType.Delegation);
        capturedMessages.Should().Contain(m => m.MessageType == MessageType.Solution);
    }

    [Fact]
    public async Task BroadcastMessages_ContainInternalReasoning()
    {
        // Arrange
        MockLlm.Reset();
        MockLlm.EnqueueWithReasoning(
            "[SOLUTION] Here's the solution.",
            "I analyzed the problem and determined the best approach.");

        var capturedMessages = new List<MessageDto>();
        MockHubBroadcaster
            .Setup(x => x.BroadcastMessageReceivedAsync(
                It.IsAny<Guid>(),
                It.IsAny<MessageDto>(),
                It.IsAny<CancellationToken>()))
            .Callback<Guid, MessageDto, CancellationToken>((_, msg, _) => capturedMessages.Add(msg))
            .Returns(Task.CompletedTask);

        // Act
        await Orchestrator.StartSessionAsync("Problem");

        // Assert
        capturedMessages.Should().Contain(m => 
            m.InternalReasoning != null && 
            m.InternalReasoning.Contains("analyzed the problem"));
    }

    [Fact]
    public async Task BroadcastMessages_ContainTimestamps()
    {
        // Arrange
        MockLlm.Reset();
        MockLlm.EnqueueSolution("Solution.");

        var capturedMessages = new List<MessageDto>();
        MockHubBroadcaster
            .Setup(x => x.BroadcastMessageReceivedAsync(
                It.IsAny<Guid>(),
                It.IsAny<MessageDto>(),
                It.IsAny<CancellationToken>()))
            .Callback<Guid, MessageDto, CancellationToken>((_, msg, _) => capturedMessages.Add(msg))
            .Returns(Task.CompletedTask);

        var beforeTest = DateTime.UtcNow;

        // Act
        await Orchestrator.StartSessionAsync("Problem");

        // Assert
        foreach (var msg in capturedMessages)
        {
            msg.Timestamp.Should().BeOnOrAfter(beforeTest);
        }
    }

    [Fact]
    public async Task BroadcastMessages_HaveUniqueIds()
    {
        // Arrange
        MockLlm.Reset();
        MockLlm.EnqueueDelegation(PersonaType.TechnicalArchitect, "Design");
        MockLlm.EnqueueDelegation(PersonaType.SeniorDeveloper, "Implement");
        MockLlm.EnqueueSolution("Done.");
        MockLlm.EnqueueSolution("## Final");

        var capturedMessages = new List<MessageDto>();
        MockHubBroadcaster
            .Setup(x => x.BroadcastMessageReceivedAsync(
                It.IsAny<Guid>(),
                It.IsAny<MessageDto>(),
                It.IsAny<CancellationToken>()))
            .Callback<Guid, MessageDto, CancellationToken>((_, msg, _) => capturedMessages.Add(msg))
            .Returns(Task.CompletedTask);

        // Act
        await Orchestrator.StartSessionAsync("Problem");

        // Assert
        var ids = capturedMessages.Select(m => m.Id).ToList();
        ids.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task BroadcastMessages_InChronologicalOrder()
    {
        // Arrange
        MockLlm.Reset();
        MockLlm.EnqueueDelegation(PersonaType.TechnicalArchitect, "Design");
        MockLlm.EnqueueSolution("Done.");
        MockLlm.EnqueueSolution("## Final");

        var capturedMessages = new List<MessageDto>();
        MockHubBroadcaster
            .Setup(x => x.BroadcastMessageReceivedAsync(
                It.IsAny<Guid>(),
                It.IsAny<MessageDto>(),
                It.IsAny<CancellationToken>()))
            .Callback<Guid, MessageDto, CancellationToken>((_, msg, _) => capturedMessages.Add(msg))
            .Returns(Task.CompletedTask);

        // Act
        await Orchestrator.StartSessionAsync("Problem");

        // Assert
        for (int i = 1; i < capturedMessages.Count; i++)
        {
            capturedMessages[i].Timestamp.Should().BeOnOrAfter(capturedMessages[i - 1].Timestamp);
        }
    }
}
