using ANXAgentSwarm.Core.Enums;
using FluentAssertions;

namespace ANXAgentSwarm.Integration.Tests.Concurrency;

/// <summary>
/// Integration tests for concurrent session handling.
/// </summary>
public class ConcurrentSessionTests : IntegrationTestBase
{
    [Fact]
    public async Task MultipleSessions_CanBeCreatedConcurrently()
    {
        // Arrange
        MockLlm.Reset();
        // Queue enough responses for multiple sessions
        for (int i = 0; i < 10; i++)
        {
            MockLlm.EnqueueSolution($"Solution for session {i}");
        }

        // Act - Create multiple sessions concurrently
        var tasks = Enumerable.Range(0, 5)
            .Select(i => Orchestrator.StartSessionAsync($"Problem {i}"))
            .ToList();

        var sessions = await Task.WhenAll(tasks);

        // Assert
        sessions.Should().HaveCount(5);
        sessions.Select(s => s.Id).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task MultipleSessions_HaveIndependentState()
    {
        // Arrange
        MockLlm.Reset();
        MockLlm.EnqueueClarification("Question for session 1");
        MockLlm.EnqueueSolution("Solution for session 2");

        // Act
        var session1 = await Orchestrator.StartSessionAsync("Problem 1");
        var session2 = await Orchestrator.StartSessionAsync("Problem 2");

        // Assert
        session1.Status.Should().Be(SessionStatus.WaitingForClarification);
        session2.Status.Should().Be(SessionStatus.Completed);
    }

    [Fact]
    public async Task MultipleSessions_HaveIndependentMessages()
    {
        // Arrange
        MockLlm.Reset();
        MockLlm.EnqueueSolution("Solution A");
        MockLlm.EnqueueSolution("Solution B");

        // Act
        var sessionA = await Orchestrator.StartSessionAsync("Problem A");
        var sessionB = await Orchestrator.StartSessionAsync("Problem B");

        // Assert
        var messagesA = await MessageRepository.GetBySessionIdAsync(sessionA.Id);
        var messagesB = await MessageRepository.GetBySessionIdAsync(sessionB.Id);

        messagesA.Should().OnlyContain(m => m.SessionId == sessionA.Id);
        messagesB.Should().OnlyContain(m => m.SessionId == sessionB.Id);
        
        // IDs should be unique across sessions
        var allMessageIds = messagesA.Select(m => m.Id).Concat(messagesB.Select(m => m.Id));
        allMessageIds.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task MultipleSessions_HaveIndependentMemories()
    {
        // Arrange
        MockLlm.Reset();
        MockLlm.EnqueueWithMemoryStore("[SOLUTION] Done", "memory-1", "Content for session 1");
        MockLlm.EnqueueWithMemoryStore("[SOLUTION] Done", "memory-2", "Content for session 2");

        // Act
        var session1 = await Orchestrator.StartSessionAsync("Problem 1");
        var session2 = await Orchestrator.StartSessionAsync("Problem 2");

        // Assert
        var memories1 = await MemoryService.GetRecentMemoriesAsync(session1.Id, PersonaType.Coordinator, 10);
        var memories2 = await MemoryService.GetRecentMemoriesAsync(session2.Id, PersonaType.Coordinator, 10);

        memories1.Should().OnlyContain(m => m.SessionId == session1.Id);
        memories2.Should().OnlyContain(m => m.SessionId == session2.Id);
    }

    [Fact]
    public async Task ClarificationInOneSession_DoesNotAffectAnother()
    {
        // Arrange
        MockLlm.Reset();
        MockLlm.EnqueueClarification("Question");
        MockLlm.EnqueueSolution("Solution for session 2");

        // Act
        var session1 = await Orchestrator.StartSessionAsync("Problem 1");
        var session2 = await Orchestrator.StartSessionAsync("Problem 2");

        // Session 1 is waiting, session 2 is complete
        session1.Status.Should().Be(SessionStatus.WaitingForClarification);
        session2.Status.Should().Be(SessionStatus.Completed);

        // Answer clarification for session 1
        MockLlm.EnqueueSolution("Session 1 now complete");
        await Orchestrator.HandleUserClarificationAsync(session1.Id, "Answer");

        // Assert - both should now be complete
        var updated1 = await SessionRepository.GetByIdAsync(session1.Id);
        var updated2 = await SessionRepository.GetByIdAsync(session2.Id);

        updated1!.Status.Should().Be(SessionStatus.Completed);
        updated2!.Status.Should().Be(SessionStatus.Completed);
    }

    [Fact]
    public async Task CancelOneSession_DoesNotAffectOthers()
    {
        // Arrange
        MockLlm.Reset();
        MockLlm.EnqueueClarification("Question 1");
        MockLlm.EnqueueClarification("Question 2");
        MockLlm.EnqueueClarification("Question 3");

        // Act
        var session1 = await Orchestrator.StartSessionAsync("Problem 1");
        var session2 = await Orchestrator.StartSessionAsync("Problem 2");
        var session3 = await Orchestrator.StartSessionAsync("Problem 3");

        await Orchestrator.CancelSessionAsync(session2.Id);

        // Assert
        var updated1 = await SessionRepository.GetByIdAsync(session1.Id);
        var updated2 = await SessionRepository.GetByIdAsync(session2.Id);
        var updated3 = await SessionRepository.GetByIdAsync(session3.Id);

        updated1!.Status.Should().Be(SessionStatus.WaitingForClarification);
        updated2!.Status.Should().Be(SessionStatus.Cancelled);
        updated3!.Status.Should().Be(SessionStatus.WaitingForClarification);
    }

    [Fact]
    public async Task ManySessionsSimultaneously_AllComplete()
    {
        // Arrange
        MockLlm.Reset();
        const int sessionCount = 10;
        
        for (int i = 0; i < sessionCount; i++)
        {
            MockLlm.EnqueueSolution($"Solution {i}");
        }

        // Act
        var tasks = Enumerable.Range(0, sessionCount)
            .Select(i => Orchestrator.StartSessionAsync($"Problem {i}"))
            .ToList();

        var sessions = await Task.WhenAll(tasks);

        // Assert
        sessions.Should().HaveCount(sessionCount);
        sessions.Should().OnlyContain(s => s.Status == SessionStatus.Completed);
    }

    [Fact]
    public async Task SequentialSessionCreation_MaintainsOrder()
    {
        // Arrange
        MockLlm.Reset();
        MockLlm.EnqueueSolution("First");
        MockLlm.EnqueueSolution("Second");
        MockLlm.EnqueueSolution("Third");

        // Act
        var session1 = await Orchestrator.StartSessionAsync("First problem");
        var session2 = await Orchestrator.StartSessionAsync("Second problem");
        var session3 = await Orchestrator.StartSessionAsync("Third problem");

        // Assert
        session1.CreatedAt.Should().BeBefore(session2.CreatedAt);
        session2.CreatedAt.Should().BeBefore(session3.CreatedAt);
    }

    [Fact]
    public async Task SessionsWithDifferentComplexity_HandleCorrectly()
    {
        // Arrange
        MockLlm.Reset();
        
        // Simple session - immediate solution
        MockLlm.EnqueueSolution("Simple solution");
        
        // Complex session - multiple delegations
        MockLlm.EnqueueDelegation(PersonaType.TechnicalArchitect, "Design");
        MockLlm.EnqueueDelegation(PersonaType.SeniorDeveloper, "Implement");
        MockLlm.EnqueueSolution("Complex implementation");
        MockLlm.EnqueueSolution("## Final Complex Solution");
        
        // Another simple session
        MockLlm.EnqueueSolution("Another simple solution");

        // Act
        var simple1 = await Orchestrator.StartSessionAsync("Simple 1");
        var complex = await Orchestrator.StartSessionAsync("Complex problem");
        var simple2 = await Orchestrator.StartSessionAsync("Simple 2");

        // Assert
        simple1.Status.Should().Be(SessionStatus.Completed);
        complex.Status.Should().Be(SessionStatus.Completed);
        simple2.Status.Should().Be(SessionStatus.Completed);

        // Complex should have more messages
        var simpleMessages = await MessageRepository.GetBySessionIdAsync(simple1.Id);
        var complexMessages = await MessageRepository.GetBySessionIdAsync(complex.Id);

        complexMessages.Count().Should().BeGreaterThan(simpleMessages.Count());
    }

    [Fact]
    public async Task MixedSessionStates_HandleCorrectly()
    {
        // Arrange
        MockLlm.Reset();
        MockLlm.EnqueueSolution("Completed");
        MockLlm.EnqueueClarification("Waiting");
        MockLlm.EnqueueStuck("Stuck");
        MockLlm.EnqueueStuck("Still stuck");
        MockLlm.EnqueueStuck("Stuck");
        MockLlm.EnqueueStuck("Stuck");
        MockLlm.EnqueueStuck("Stuck");

        // Act
        var completedSession = await Orchestrator.StartSessionAsync("Will complete");
        var waitingSession = await Orchestrator.StartSessionAsync("Will wait");
        var stuckSession = await Orchestrator.StartSessionAsync("Will get stuck");

        // Assert
        completedSession.Status.Should().Be(SessionStatus.Completed);
        waitingSession.Status.Should().Be(SessionStatus.WaitingForClarification);
        stuckSession.Status.Should().Be(SessionStatus.Stuck);
    }

    [Fact]
    public async Task SessionIsolation_MessagesNotMixed()
    {
        // Arrange
        MockLlm.Reset();
        MockLlm.EnqueueDelegation(PersonaType.BusinessAnalyst, "Analyze A");
        MockLlm.EnqueueSolution("Analysis A complete");
        MockLlm.EnqueueSolution("## Solution A");
        
        MockLlm.EnqueueDelegation(PersonaType.TechnicalArchitect, "Design B");
        MockLlm.EnqueueSolution("Design B complete");
        MockLlm.EnqueueSolution("## Solution B");

        // Act
        var sessionA = await Orchestrator.StartSessionAsync("Problem A - needs BA");
        var sessionB = await Orchestrator.StartSessionAsync("Problem B - needs TA");

        // Assert
        var messagesA = (await MessageRepository.GetBySessionIdAsync(sessionA.Id)).ToList();
        var messagesB = (await MessageRepository.GetBySessionIdAsync(sessionB.Id)).ToList();

        // Session A should have BA, not TA
        messagesA.Should().Contain(m => m.FromPersona == PersonaType.BusinessAnalyst);
        messagesA.Should().NotContain(m => m.FromPersona == PersonaType.TechnicalArchitect);

        // Session B should have TA, not BA
        messagesB.Should().Contain(m => m.FromPersona == PersonaType.TechnicalArchitect);
        messagesB.Should().NotContain(m => m.FromPersona == PersonaType.BusinessAnalyst);
    }

    [Fact]
    public async Task ResumeOneSession_DoesNotAffectOthers()
    {
        // Arrange
        MockLlm.Reset();
        // Only 1 stuck response per session - Coordinator stuck immediately ends session
        MockLlm.EnqueueStuck("Stuck 1");
        MockLlm.EnqueueStuck("Stuck 2");

        var session1 = await Orchestrator.StartSessionAsync("Problem 1");
        var session2 = await Orchestrator.StartSessionAsync("Problem 2");

        // Both should be stuck
        session1.Status.Should().Be(SessionStatus.Stuck);
        session2.Status.Should().Be(SessionStatus.Stuck);

        // Resume only session 1
        MockLlm.EnqueueSolution("Session 1 recovered");

        await Orchestrator.ResumeSessionAsync(session1.Id);

        // Assert
        var updated1 = await SessionRepository.GetByIdAsync(session1.Id);
        var updated2 = await SessionRepository.GetByIdAsync(session2.Id);

        updated1!.Status.Should().Be(SessionStatus.Completed);
        updated2!.Status.Should().Be(SessionStatus.Stuck);
    }
}
