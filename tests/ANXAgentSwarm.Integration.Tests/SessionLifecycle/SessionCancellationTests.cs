using ANXAgentSwarm.Core.Enums;
using FluentAssertions;

namespace ANXAgentSwarm.Integration.Tests.SessionLifecycle;

/// <summary>
/// Integration tests for session cancellation functionality.
/// </summary>
public class SessionCancellationTests : IntegrationTestBase
{
    [Fact]
    public async Task CancelSessionAsync_MarksSessionAsCancelled()
    {
        // Arrange
        MockLlm.Reset();
        MockLlm.EnqueueClarification("What framework?"); // Pause session
        
        var session = await Orchestrator.StartSessionAsync("Build an app");
        session.Status.Should().Be(SessionStatus.WaitingForClarification);

        // Act
        await Orchestrator.CancelSessionAsync(session.Id);

        // Assert
        var cancelledSession = await SessionRepository.GetByIdAsync(session.Id);
        cancelledSession!.Status.Should().Be(SessionStatus.Cancelled);
    }

    [Fact]
    public async Task CancelSessionAsync_UpdatesTimestamp()
    {
        // Arrange
        MockLlm.Reset();
        MockLlm.EnqueueClarification("Question");
        
        var session = await Orchestrator.StartSessionAsync("Problem");
        var originalUpdatedAt = session.UpdatedAt;

        await Task.Delay(10); // Ensure time difference

        // Act
        await Orchestrator.CancelSessionAsync(session.Id);

        // Assert
        var cancelledSession = await SessionRepository.GetByIdAsync(session.Id);
        cancelledSession!.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public async Task CancelSessionAsync_ThrowsForNonExistentSession()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        var act = () => Orchestrator.CancelSessionAsync(nonExistentId);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{nonExistentId}*not found*");
    }

    [Fact]
    public async Task CancelSessionAsync_PreservesExistingMessages()
    {
        // Arrange
        MockLlm.Reset();
        MockLlm.EnqueueDelegation(PersonaType.TechnicalArchitect, "Design");
        MockLlm.EnqueueClarification("Need more details");
        
        var session = await Orchestrator.StartSessionAsync("Build something");
        
        var messagesBeforeCancel = await MessageRepository.GetBySessionIdAsync(session.Id);
        var countBefore = messagesBeforeCancel.Count();

        // Act
        await Orchestrator.CancelSessionAsync(session.Id);

        // Assert
        var messagesAfterCancel = await MessageRepository.GetBySessionIdAsync(session.Id);
        messagesAfterCancel.Count().Should().Be(countBefore);
    }

    [Fact]
    public async Task CancelSessionAsync_CanCancelActiveSession()
    {
        // Arrange - Create a session but don't wait for completion
        // This simulates cancelling mid-processing
        MockLlm.Reset();
        MockLlm.EnqueueClarification("Question"); // Will pause

        var session = await Orchestrator.StartSessionAsync("Problem");

        // Act
        await Orchestrator.CancelSessionAsync(session.Id);

        // Assert
        var cancelledSession = await SessionRepository.GetByIdAsync(session.Id);
        cancelledSession!.Status.Should().Be(SessionStatus.Cancelled);
    }

    [Fact]
    public async Task CancelSessionAsync_CanCancelStuckSession()
    {
        // Arrange
        MockLlm.Reset();
        MockLlm.EnqueueStuck("Stuck.");
        MockLlm.EnqueueStuck("Still stuck.");
        MockLlm.EnqueueStuck("Stuck.");
        MockLlm.EnqueueStuck("Stuck.");
        MockLlm.EnqueueStuck("Stuck.");
        
        var session = await Orchestrator.StartSessionAsync("Impossible");
        session.Status.Should().Be(SessionStatus.Stuck);

        // Act
        await Orchestrator.CancelSessionAsync(session.Id);

        // Assert
        var cancelledSession = await SessionRepository.GetByIdAsync(session.Id);
        cancelledSession!.Status.Should().Be(SessionStatus.Cancelled);
    }

    [Fact]
    public async Task HandleUserClarificationAsync_ThrowsForCancelledSession()
    {
        // Arrange
        MockLlm.Reset();
        MockLlm.EnqueueClarification("Question");
        
        var session = await Orchestrator.StartSessionAsync("Problem");
        await Orchestrator.CancelSessionAsync(session.Id);

        // Act & Assert
        var act = () => Orchestrator.HandleUserClarificationAsync(session.Id, "Answer");
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not waiting for clarification*");
    }

    [Fact]
    public async Task ResumeSessionAsync_ThrowsForCancelledSession()
    {
        // Arrange
        MockLlm.Reset();
        MockLlm.EnqueueClarification("Question");
        
        var session = await Orchestrator.StartSessionAsync("Problem");
        await Orchestrator.CancelSessionAsync(session.Id);

        // Act & Assert
        var act = () => Orchestrator.ResumeSessionAsync(session.Id);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cannot resume*Cancelled*");
    }

    [Fact]
    public async Task CancelSessionAsync_DoesNotAffectOtherSessions()
    {
        // Arrange
        MockLlm.Reset();
        MockLlm.EnqueueClarification("Question 1");
        var session1 = await Orchestrator.StartSessionAsync("Problem 1");
        
        MockLlm.EnqueueClarification("Question 2");
        var session2 = await Orchestrator.StartSessionAsync("Problem 2");

        // Act
        await Orchestrator.CancelSessionAsync(session1.Id);

        // Assert
        var cancelledSession = await SessionRepository.GetByIdAsync(session1.Id);
        var otherSession = await SessionRepository.GetByIdAsync(session2.Id);
        
        cancelledSession!.Status.Should().Be(SessionStatus.Cancelled);
        otherSession!.Status.Should().Be(SessionStatus.WaitingForClarification);
    }

    [Fact]
    public async Task CancelSessionAsync_IsIdempotent()
    {
        // Arrange
        MockLlm.Reset();
        MockLlm.EnqueueClarification("Question");
        
        var session = await Orchestrator.StartSessionAsync("Problem");
        await Orchestrator.CancelSessionAsync(session.Id);

        // Act - Cancel again
        await Orchestrator.CancelSessionAsync(session.Id);

        // Assert
        var cancelledSession = await SessionRepository.GetByIdAsync(session.Id);
        cancelledSession!.Status.Should().Be(SessionStatus.Cancelled);
    }

    [Fact]
    public async Task CancelSessionAsync_PreservesMemories()
    {
        // Arrange
        MockLlm.Reset();
        MockLlm.EnqueueWithMemoryStore(
            "[CLARIFY] What's your preference?",
            "initial-requirements",
            "User wants to build an app");
        
        var session = await Orchestrator.StartSessionAsync("Build an app");

        // Act
        await Orchestrator.CancelSessionAsync(session.Id);

        // Assert
        var memories = await MemoryService.GetRecentMemoriesAsync(session.Id, PersonaType.Coordinator, 10);
        memories.Should().NotBeEmpty();
    }
}
