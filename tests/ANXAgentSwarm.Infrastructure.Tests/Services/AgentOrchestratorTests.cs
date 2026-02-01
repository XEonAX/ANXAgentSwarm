using ANXAgentSwarm.Core.DTOs;
using ANXAgentSwarm.Core.Entities;
using ANXAgentSwarm.Core.Enums;
using ANXAgentSwarm.Core.Interfaces;
using ANXAgentSwarm.Core.Models;
using ANXAgentSwarm.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace ANXAgentSwarm.Infrastructure.Tests.Services;

/// <summary>
/// Unit tests for the AgentOrchestrator service.
/// </summary>
public class AgentOrchestratorTests
{
    private readonly Mock<ISessionRepository> _sessionRepoMock;
    private readonly Mock<IMessageRepository> _messageRepoMock;
    private readonly Mock<IMemoryService> _memoryServiceMock;
    private readonly Mock<IPersonaEngine> _personaEngineMock;
    private readonly Mock<ISessionHubBroadcaster> _hubBroadcasterMock;
    private readonly Mock<ILogger<AgentOrchestrator>> _loggerMock;
    private readonly AgentOrchestrator _sut;

    public AgentOrchestratorTests()
    {
        _sessionRepoMock = new Mock<ISessionRepository>();
        _messageRepoMock = new Mock<IMessageRepository>();
        _memoryServiceMock = new Mock<IMemoryService>();
        _personaEngineMock = new Mock<IPersonaEngine>();
        _hubBroadcasterMock = new Mock<ISessionHubBroadcaster>();
        _loggerMock = new Mock<ILogger<AgentOrchestrator>>();

        _sut = new AgentOrchestrator(
            _sessionRepoMock.Object,
            _messageRepoMock.Object,
            _memoryServiceMock.Object,
            _personaEngineMock.Object,
            _hubBroadcasterMock.Object,
            _loggerMock.Object);
    }

    private void SetupDefaultMocks()
    {
        _sessionRepoMock
            .Setup(x => x.CreateAsync(It.IsAny<Session>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Session s, CancellationToken _) => s);

        _sessionRepoMock
            .Setup(x => x.UpdateAsync(It.IsAny<Session>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Session s, CancellationToken _) => s);

        _messageRepoMock
            .Setup(x => x.CreateAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Message m, CancellationToken _) => m);

        _memoryServiceMock
            .Setup(x => x.GetRecentMemoriesAsync(
                It.IsAny<Guid>(),
                It.IsAny<PersonaType>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<Memory>());
    }

    #region StartSessionAsync Tests

    [Fact]
    public async Task StartSessionAsync_CreatesSessionWithCorrectData()
    {
        // Arrange
        var problemStatement = "Build a REST API for managing products";
        SetupDefaultMocks();

        Session? capturedSession = null;
        _sessionRepoMock
            .Setup(x => x.CreateAsync(It.IsAny<Session>(), It.IsAny<CancellationToken>()))
            .Callback<Session, CancellationToken>((s, _) => capturedSession = s)
            .ReturnsAsync((Session s, CancellationToken _) => s);

        _sessionRepoMock
            .Setup(x => x.GetByIdWithMessagesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => capturedSession);

        _personaEngineMock
            .Setup(x => x.ProcessAsync(
                PersonaType.Coordinator,
                It.IsAny<Message>(),
                It.IsAny<Session>(),
                It.IsAny<IEnumerable<Memory>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(PersonaResponse.Answer("I'll help you build this API."));

        // Act
        var result = await _sut.StartSessionAsync(problemStatement);

        // Assert
        capturedSession.Should().NotBeNull();
        capturedSession!.ProblemStatement.Should().Be(problemStatement);
        capturedSession.Status.Should().Be(SessionStatus.Active);
        capturedSession.CurrentPersona.Should().Be(PersonaType.Coordinator);
    }

    [Fact]
    public async Task StartSessionAsync_GeneratesTitle()
    {
        // Arrange
        var problemStatement = "Build a REST API for managing products with CRUD operations";
        SetupDefaultMocks();

        Session? capturedSession = null;
        _sessionRepoMock
            .Setup(x => x.CreateAsync(It.IsAny<Session>(), It.IsAny<CancellationToken>()))
            .Callback<Session, CancellationToken>((s, _) => capturedSession = s)
            .ReturnsAsync((Session s, CancellationToken _) => s);

        _sessionRepoMock
            .Setup(x => x.GetByIdWithMessagesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => capturedSession);

        _personaEngineMock
            .Setup(x => x.ProcessAsync(
                It.IsAny<PersonaType>(),
                It.IsAny<Message>(),
                It.IsAny<Session>(),
                It.IsAny<IEnumerable<Memory>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(PersonaResponse.Answer("OK"));

        // Act
        await _sut.StartSessionAsync(problemStatement);

        // Assert
        capturedSession.Should().NotBeNull();
        capturedSession!.Title.Should().NotBeNullOrEmpty();
        capturedSession.Title.Length.Should().BeLessThanOrEqualTo(53); // 50 + "..."
    }

    [Fact]
    public async Task StartSessionAsync_CreatesInitialUserMessage()
    {
        // Arrange
        var problemStatement = "Create a todo app";
        SetupDefaultMocks();

        var createdMessages = new List<Message>();
        _messageRepoMock
            .Setup(x => x.CreateAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .Callback<Message, CancellationToken>((m, _) => createdMessages.Add(m))
            .ReturnsAsync((Message m, CancellationToken _) => m);

        _sessionRepoMock
            .Setup(x => x.GetByIdWithMessagesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => new Session
            {
                Id = id,
                Messages = createdMessages
            });

        _personaEngineMock
            .Setup(x => x.ProcessAsync(
                It.IsAny<PersonaType>(),
                It.IsAny<Message>(),
                It.IsAny<Session>(),
                It.IsAny<IEnumerable<Memory>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(PersonaResponse.Answer("OK"));

        // Act
        await _sut.StartSessionAsync(problemStatement);

        // Assert
        var userMessage = createdMessages.FirstOrDefault(m => m.FromPersona == PersonaType.User);
        userMessage.Should().NotBeNull();
        userMessage!.MessageType.Should().Be(MessageType.ProblemStatement);
        userMessage.Content.Should().Be(problemStatement);
        userMessage.ToPersona.Should().Be(PersonaType.Coordinator);
    }

    [Fact]
    public async Task StartSessionAsync_BroadcastsMessageReceived()
    {
        // Arrange
        SetupDefaultMocks();

        _sessionRepoMock
            .Setup(x => x.GetByIdWithMessagesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Session { Id = Guid.NewGuid() });

        _personaEngineMock
            .Setup(x => x.ProcessAsync(
                It.IsAny<PersonaType>(),
                It.IsAny<Message>(),
                It.IsAny<Session>(),
                It.IsAny<IEnumerable<Memory>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(PersonaResponse.Answer("OK"));

        // Act
        await _sut.StartSessionAsync("Test problem");

        // Assert
        _hubBroadcasterMock.Verify(
            x => x.BroadcastMessageReceivedAsync(
                It.IsAny<Guid>(),
                It.IsAny<MessageDto>(),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region Delegation Flow Tests

    [Fact]
    public async Task ProcessDelegationAsync_ProcessesWithDelegatedPersona()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        var session = new Session
        {
            Id = sessionId,
            Status = SessionStatus.Active,
            Messages = new List<Message>()
        };

        var delegationMessage = new Message
        {
            Id = messageId,
            SessionId = sessionId,
            FromPersona = PersonaType.Coordinator,
            MessageType = MessageType.Delegation,
            DelegateToPersona = PersonaType.TechnicalArchitect,
            Content = "Please design the architecture",
            DelegationContext = "System design needed"
        };

        SetupDefaultMocks();

        _sessionRepoMock
            .Setup(x => x.GetByIdWithMessagesAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        _messageRepoMock
            .Setup(x => x.GetByIdAsync(messageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(delegationMessage);

        _personaEngineMock
            .Setup(x => x.ProcessAsync(
                PersonaType.TechnicalArchitect,
                It.IsAny<Message>(),
                It.IsAny<Session>(),
                It.IsAny<IEnumerable<Memory>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(PersonaResponse.Answer("Here is the architecture design."));

        // Act
        var result = await _sut.ProcessDelegationAsync(sessionId, messageId);

        // Assert
        result.Should().NotBeNull();
        _personaEngineMock.Verify(
            x => x.ProcessAsync(
                PersonaType.TechnicalArchitect,
                It.IsAny<Message>(),
                It.IsAny<Session>(),
                It.IsAny<IEnumerable<Memory>>(),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ProcessDelegationAsync_WithNonDelegationMessage_Throws()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        var session = new Session { Id = sessionId };
        var answerMessage = new Message
        {
            Id = messageId,
            MessageType = MessageType.Answer
        };

        _sessionRepoMock
            .Setup(x => x.GetByIdWithMessagesAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        _messageRepoMock
            .Setup(x => x.GetByIdAsync(messageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(answerMessage);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.ProcessDelegationAsync(sessionId, messageId));
    }

    #endregion

    #region Clarification Handling Tests

    [Fact]
    public async Task HandleUserClarificationAsync_UpdatesSessionStatus()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var clarificationMessage = new Message
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            FromPersona = PersonaType.BusinessAnalyst,
            MessageType = MessageType.Clarification,
            Content = "What features do you need?",
            Timestamp = DateTime.UtcNow
        };

        var session = new Session
        {
            Id = sessionId,
            Status = SessionStatus.WaitingForClarification,
            Messages = new List<Message> { clarificationMessage }
        };

        SetupDefaultMocks();

        _sessionRepoMock
            .Setup(x => x.GetByIdWithMessagesAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        _personaEngineMock
            .Setup(x => x.ProcessAsync(
                PersonaType.BusinessAnalyst,
                It.IsAny<Message>(),
                It.IsAny<Session>(),
                It.IsAny<IEnumerable<Memory>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(PersonaResponse.Answer("Great, I understand your requirements."));

        Session? updatedSession = null;
        _sessionRepoMock
            .Setup(x => x.UpdateAsync(It.IsAny<Session>(), It.IsAny<CancellationToken>()))
            .Callback<Session, CancellationToken>((s, _) => updatedSession = s)
            .ReturnsAsync((Session s, CancellationToken _) => s);

        // Act
        await _sut.HandleUserClarificationAsync(sessionId, "I need CRUD operations and user auth");

        // Assert
        _sessionRepoMock.Verify(
            x => x.UpdateAsync(
                It.Is<Session>(s => s.Status == SessionStatus.Active),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task HandleUserClarificationAsync_CreatesUserResponseMessage()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var clarificationMessage = new Message
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            FromPersona = PersonaType.BusinessAnalyst,
            MessageType = MessageType.Clarification,
            Timestamp = DateTime.UtcNow
        };

        var session = new Session
        {
            Id = sessionId,
            Status = SessionStatus.WaitingForClarification,
            Messages = new List<Message> { clarificationMessage }
        };

        SetupDefaultMocks();

        _sessionRepoMock
            .Setup(x => x.GetByIdWithMessagesAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var createdMessages = new List<Message>();
        _messageRepoMock
            .Setup(x => x.CreateAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .Callback<Message, CancellationToken>((m, _) => createdMessages.Add(m))
            .ReturnsAsync((Message m, CancellationToken _) => m);

        _personaEngineMock
            .Setup(x => x.ProcessAsync(
                It.IsAny<PersonaType>(),
                It.IsAny<Message>(),
                It.IsAny<Session>(),
                It.IsAny<IEnumerable<Memory>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(PersonaResponse.Answer("OK"));

        // Act
        await _sut.HandleUserClarificationAsync(sessionId, "Here is my clarification");

        // Assert
        var userResponse = createdMessages.FirstOrDefault(m =>
            m.FromPersona == PersonaType.User &&
            m.MessageType == MessageType.UserResponse);
        userResponse.Should().NotBeNull();
        userResponse!.Content.Should().Be("Here is my clarification");
        userResponse.ParentMessageId.Should().Be(clarificationMessage.Id);
    }

    [Fact]
    public async Task HandleUserClarificationAsync_WhenNotWaiting_Throws()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var session = new Session
        {
            Id = sessionId,
            Status = SessionStatus.Active
        };

        _sessionRepoMock
            .Setup(x => x.GetByIdWithMessagesAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.HandleUserClarificationAsync(sessionId, "Response"));
    }

    #endregion

    #region Solution Handling Tests

    [Fact]
    public async Task ProcessWithPersona_WithSolution_CompletesSession()
    {
        // Arrange
        SetupDefaultMocks();

        Session? updatedSession = null;
        _sessionRepoMock
            .Setup(x => x.UpdateAsync(It.IsAny<Session>(), It.IsAny<CancellationToken>()))
            .Callback<Session, CancellationToken>((s, _) => updatedSession = s)
            .ReturnsAsync((Session s, CancellationToken _) => s);

        _sessionRepoMock
            .Setup(x => x.GetByIdWithMessagesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Session { Id = Guid.NewGuid(), Messages = new List<Message>() });

        // First call (Coordinator) returns solution
        _personaEngineMock
            .Setup(x => x.ProcessAsync(
                PersonaType.Coordinator,
                It.IsAny<Message>(),
                It.IsAny<Session>(),
                It.IsAny<IEnumerable<Memory>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(PersonaResponse.Solution("Here is the complete solution."));

        // Act
        await _sut.StartSessionAsync("Build something");

        // Assert
        _sessionRepoMock.Verify(
            x => x.UpdateAsync(
                It.Is<Session>(s => s.Status == SessionStatus.Completed),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);

        _hubBroadcasterMock.Verify(
            x => x.BroadcastSolutionReadyAsync(
                It.IsAny<Guid>(),
                It.IsAny<SessionDto>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Stuck Handling Tests

    [Fact]
    public async Task ProcessWithPersona_WithStuck_HandlesGracefully()
    {
        // Arrange
        SetupDefaultMocks();

        _sessionRepoMock
            .Setup(x => x.GetByIdWithMessagesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Session { Id = Guid.NewGuid(), Messages = new List<Message>() });

        // Return stuck from Coordinator
        _personaEngineMock
            .Setup(x => x.ProcessAsync(
                PersonaType.Coordinator,
                It.IsAny<Message>(),
                It.IsAny<Session>(),
                It.IsAny<IEnumerable<Memory>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(PersonaResponse.Stuck("I cannot proceed with this request."));

        // Act
        await _sut.StartSessionAsync("Impossible task");

        // Assert
        _hubBroadcasterMock.Verify(
            x => x.BroadcastSessionStuckAsync(
                It.IsAny<Guid>(),
                It.IsAny<SessionDto>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region Resume Session Tests

    [Fact]
    public async Task ResumeSessionAsync_WithDelegation_ContinuesProcessing()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var delegationMessage = new Message
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            MessageType = MessageType.Delegation,
            DelegateToPersona = PersonaType.SeniorDeveloper,
            Timestamp = DateTime.UtcNow
        };

        var session = new Session
        {
            Id = sessionId,
            Status = SessionStatus.Stuck,
            Messages = new List<Message> { delegationMessage }
        };

        SetupDefaultMocks();

        _sessionRepoMock
            .Setup(x => x.GetByIdWithMessagesAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        _personaEngineMock
            .Setup(x => x.ProcessAsync(
                PersonaType.SeniorDeveloper,
                It.IsAny<Message>(),
                It.IsAny<Session>(),
                It.IsAny<IEnumerable<Memory>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(PersonaResponse.Answer("Implementation complete."));

        // Act
        await _sut.ResumeSessionAsync(sessionId);

        // Assert
        _personaEngineMock.Verify(
            x => x.ProcessAsync(
                PersonaType.SeniorDeveloper,
                It.IsAny<Message>(),
                It.IsAny<Session>(),
                It.IsAny<IEnumerable<Memory>>(),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ResumeSessionAsync_WhenCompleted_Throws()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var session = new Session
        {
            Id = sessionId,
            Status = SessionStatus.Completed
        };

        _sessionRepoMock
            .Setup(x => x.GetByIdWithMessagesAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.ResumeSessionAsync(sessionId));
    }

    #endregion

    #region Cancel Session Tests

    [Fact]
    public async Task CancelSessionAsync_UpdatesStatusToCancelled()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var session = new Session
        {
            Id = sessionId,
            Status = SessionStatus.Active
        };

        _sessionRepoMock
            .Setup(x => x.GetByIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        _sessionRepoMock
            .Setup(x => x.UpdateAsync(It.IsAny<Session>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Session s, CancellationToken _) => s);

        // Act
        await _sut.CancelSessionAsync(sessionId);

        // Assert
        _sessionRepoMock.Verify(
            x => x.UpdateAsync(
                It.Is<Session>(s => s.Status == SessionStatus.Cancelled),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CancelSessionAsync_WithNonExistentSession_Throws()
    {
        // Arrange
        var sessionId = Guid.NewGuid();

        _sessionRepoMock
            .Setup(x => x.GetByIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Session?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.CancelSessionAsync(sessionId));
    }

    #endregion

    #region Delegation Chain Tests

    [Fact]
    public async Task ProcessWithPersona_WithMultipleDelegations_ProcessesChain()
    {
        // Arrange
        SetupDefaultMocks();

        _sessionRepoMock
            .Setup(x => x.GetByIdWithMessagesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Session { Id = Guid.NewGuid(), Messages = new List<Message>() });

        var callCount = 0;
        _personaEngineMock
            .Setup(x => x.ProcessAsync(
                It.IsAny<PersonaType>(),
                It.IsAny<Message>(),
                It.IsAny<Session>(),
                It.IsAny<IEnumerable<Memory>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount switch
                {
                    1 => PersonaResponse.Delegate(PersonaType.TechnicalArchitect, "Analyzing", "Design needed"),
                    2 => PersonaResponse.Delegate(PersonaType.SeniorDeveloper, "Designed", "Implementation needed"),
                    _ => PersonaResponse.Solution("Complete implementation")
                };
            });

        // Act
        await _sut.StartSessionAsync("Build a feature");

        // Assert
        callCount.Should().BeGreaterThanOrEqualTo(3);
        _personaEngineMock.Verify(
            x => x.ProcessAsync(
                PersonaType.Coordinator,
                It.IsAny<Message>(),
                It.IsAny<Session>(),
                It.IsAny<IEnumerable<Memory>>(),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);

        _personaEngineMock.Verify(
            x => x.ProcessAsync(
                PersonaType.TechnicalArchitect,
                It.IsAny<Message>(),
                It.IsAny<Session>(),
                It.IsAny<IEnumerable<Memory>>(),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    #endregion
}
