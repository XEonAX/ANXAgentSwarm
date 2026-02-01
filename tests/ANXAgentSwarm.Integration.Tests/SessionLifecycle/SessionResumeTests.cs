using ANXAgentSwarm.Core.Enums;
using FluentAssertions;

namespace ANXAgentSwarm.Integration.Tests.SessionLifecycle;

/// <summary>
/// Integration tests for resuming paused or stuck sessions.
/// </summary>
public class SessionResumeTests : IntegrationTestBase
{
    [Fact]
    public async Task ResumeSessionAsync_ResumesStuckSession()
    {
        // Arrange
        MockLlm.Reset();
        // Only 1 stuck response needed - Coordinator being stuck immediately ends session
        MockLlm.EnqueueStuck("Stuck on initial attempt.");
        
        var session = await Orchestrator.StartSessionAsync("Difficult problem");
        session.Status.Should().Be(SessionStatus.Stuck);

        // Setup responses for resume - Coordinator will try again
        MockLlm.EnqueueDelegation(PersonaType.BusinessAnalyst, "Try different approach");
        MockLlm.EnqueueSolution("Alternative solution found.");
        MockLlm.EnqueueSolution("## Final Solution"); // For Coordinator compilation

        // Act
        var resumedSession = await Orchestrator.ResumeSessionAsync(session.Id);

        // Assert
        resumedSession.Status.Should().Be(SessionStatus.Completed);
    }

    [Fact]
    public async Task ResumeSessionAsync_ResumesFromLastDelegation()
    {
        // Arrange
        MockLlm.Reset();
        MockLlm.EnqueueDelegation(PersonaType.TechnicalArchitect, "Design");
        MockLlm.EnqueueClarification("Need clarification"); // Pauses
        
        var session = await Orchestrator.StartSessionAsync("Problem");
        session.Status.Should().Be(SessionStatus.WaitingForClarification);

        // User provides clarification, continues with delegation
        MockLlm.EnqueueDelegation(PersonaType.SeniorDeveloper, "Implement");
        MockLlm.EnqueueSolution("Done.");
        MockLlm.EnqueueSolution("## Final");

        // Act - Resume by providing clarification
        await Orchestrator.HandleUserClarificationAsync(session.Id, "Details provided");

        // Assert
        var finalSession = await SessionRepository.GetByIdAsync(session.Id);
        finalSession!.Status.Should().Be(SessionStatus.Completed);
    }

    [Fact]
    public async Task ResumeSessionAsync_ThrowsForCompletedSession()
    {
        // Arrange
        MockLlm.Reset();
        MockLlm.EnqueueSolution("Immediate solution.");
        
        var session = await Orchestrator.StartSessionAsync("Simple problem");
        session.Status.Should().Be(SessionStatus.Completed);

        // Act & Assert
        var act = () => Orchestrator.ResumeSessionAsync(session.Id);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cannot resume*Completed*");
    }

    [Fact]
    public async Task ResumeSessionAsync_ThrowsForNonExistentSession()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        var act = () => Orchestrator.ResumeSessionAsync(nonExistentId);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{nonExistentId}*not found*");
    }

    [Fact]
    public async Task ResumeSessionAsync_UpdatesSessionStatus()
    {
        // Arrange
        MockLlm.Reset();
        // Only 1 stuck response - Coordinator stuck immediately ends session
        MockLlm.EnqueueStuck("Stuck.");
        
        var session = await Orchestrator.StartSessionAsync("Problem");

        // Setup for resume - Coordinator retries and provides solution
        MockLlm.EnqueueSolution("Now I can solve it!");

        // Act
        await Orchestrator.ResumeSessionAsync(session.Id);

        // Assert
        var resumedSession = await SessionRepository.GetByIdAsync(session.Id);
        resumedSession!.Status.Should().Be(SessionStatus.Completed);
    }

    [Fact]
    public async Task ResumeSessionAsync_AddsNewMessages()
    {
        // Arrange
        MockLlm.Reset();
        // Only 1 stuck response - Coordinator stuck immediately ends session
        MockLlm.EnqueueStuck("Stuck.");
        
        var session = await Orchestrator.StartSessionAsync("Problem");
        var messagesBeforeResume = (await MessageRepository.GetBySessionIdAsync(session.Id)).Count();

        // Setup for resume
        MockLlm.EnqueueDelegation(PersonaType.BusinessAnalyst, "New approach");
        MockLlm.EnqueueSolution("Solution found.");
        MockLlm.EnqueueSolution("## Final");

        // Act
        await Orchestrator.ResumeSessionAsync(session.Id);

        // Assert
        var messagesAfterResume = await MessageRepository.GetBySessionIdAsync(session.Id);
        messagesAfterResume.Count().Should().BeGreaterThan(messagesBeforeResume);
    }

    [Fact]
    public async Task ResumeSessionAsync_CanResumeMultipleTimes()
    {
        // Arrange
        MockLlm.Reset();
        // Coordinator stuck immediately ends session
        MockLlm.EnqueueStuck("First stuck.");
        
        var session = await Orchestrator.StartSessionAsync("Hard problem");
        session.Status.Should().Be(SessionStatus.Stuck);

        // First resume - Coordinator still stuck
        MockLlm.EnqueueStuck("Stuck again.");

        await Orchestrator.ResumeSessionAsync(session.Id);
        var stillStuckSession = await SessionRepository.GetByIdAsync(session.Id);
        stillStuckSession!.Status.Should().Be(SessionStatus.Stuck);

        // Second resume - succeeds
        MockLlm.EnqueueSolution("Finally solved!");

        // Act
        await Orchestrator.ResumeSessionAsync(session.Id);

        // Assert
        var finalSession = await SessionRepository.GetByIdAsync(session.Id);
        finalSession!.Status.Should().Be(SessionStatus.Completed);
    }

    [Fact]
    public async Task ResumeSessionAsync_SendsToCoordinatorForStuck()
    {
        // Arrange
        MockLlm.Reset();
        // Coordinator delegates to Dev, Dev gets stuck, returns to Coordinator, Coordinator gets stuck
        MockLlm.EnqueueDelegation(PersonaType.SeniorDeveloper, "Implement");
        MockLlm.EnqueueStuck("Dev stuck.");
        MockLlm.EnqueueStuck("Coordinator stuck too.");
        
        var session = await Orchestrator.StartSessionAsync("Problem");
        session.Status.Should().Be(SessionStatus.Stuck);

        // Resume should try with Coordinator
        MockLlm.EnqueueDelegation(PersonaType.TechnicalArchitect, "Try different approach");
        MockLlm.EnqueueSolution("TA solved it.");
        MockLlm.EnqueueSolution("## Final");

        // Act
        await Orchestrator.ResumeSessionAsync(session.Id);

        // Assert
        var messages = await MessageRepository.GetBySessionIdAsync(session.Id);
        var messagesAfterStuck = messages
            .OrderBy(m => m.Timestamp)
            .SkipWhile(m => m.MessageType != MessageType.Stuck)
            .Skip(1)
            .ToList();

        // Coordinator should have processed after resume
        messagesAfterStuck.Should().Contain(m => m.FromPersona == PersonaType.Coordinator);
    }

    [Fact]
    public async Task ResumeSessionAsync_ProcessesPendingDelegation()
    {
        // Arrange
        // This test simulates a scenario where session was interrupted mid-delegation
        MockLlm.Reset();
        MockLlm.EnqueueDelegation(PersonaType.TechnicalArchitect, "Design");
        MockLlm.EnqueueClarification("Question"); // Pause for clarification
        
        var session = await Orchestrator.StartSessionAsync("Problem");

        // Respond to clarification with a delegation
        MockLlm.EnqueueDelegation(PersonaType.SeniorDeveloper, "Implement after clarification");
        MockLlm.EnqueueSolution("Implementation done.");
        MockLlm.EnqueueSolution("## Final");

        // Act
        await Orchestrator.HandleUserClarificationAsync(session.Id, "Answer");

        // Assert
        var finalSession = await SessionRepository.GetByIdAsync(session.Id);
        finalSession!.Status.Should().Be(SessionStatus.Completed);
        
        var messages = await MessageRepository.GetBySessionIdAsync(session.Id);
        messages.Should().Contain(m => m.FromPersona == PersonaType.SeniorDeveloper);
    }

    [Fact]
    public async Task ResumeSessionAsync_UpdatesTimestamp()
    {
        // Arrange
        MockLlm.Reset();
        // Coordinator stuck immediately ends session
        MockLlm.EnqueueStuck("Stuck.");
        
        var session = await Orchestrator.StartSessionAsync("Problem");
        var stuckTime = session.UpdatedAt;

        await Task.Delay(10);

        MockLlm.EnqueueSolution("Solved!");

        // Act
        await Orchestrator.ResumeSessionAsync(session.Id);

        // Assert
        var resumedSession = await SessionRepository.GetByIdAsync(session.Id);
        resumedSession!.UpdatedAt.Should().BeAfter(stuckTime);
    }

    [Fact]
    public async Task ResumeSessionAsync_PreservesExistingMemories()
    {
        // Arrange
        MockLlm.Reset();
        // Coordinator stores memory then gets stuck
        MockLlm.EnqueueWithMemoryStore(
            "[STUCK] Stuck but saved progress.",
            "progress-saved",
            "Completed initial analysis");
        
        var session = await Orchestrator.StartSessionAsync("Problem");
        session.Status.Should().Be(SessionStatus.Stuck);

        MockLlm.EnqueueSolution("Resumed and completed.");

        // Act
        await Orchestrator.ResumeSessionAsync(session.Id);

        // Assert
        var memories = await MemoryService.GetRecentMemoriesAsync(session.Id, PersonaType.Coordinator, 10);
        memories.Should().NotBeEmpty();
    }
}
