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
/// Unit tests for the PersonaEngine service.
/// </summary>
public class PersonaEngineTests
{
    private readonly Mock<ILlmProvider> _llmProviderMock;
    private readonly Mock<IPersonaConfigurationRepository> _personaConfigRepoMock;
    private readonly Mock<IMemoryService> _memoryServiceMock;
    private readonly Mock<ILogger<PersonaEngine>> _loggerMock;
    private readonly PersonaEngine _sut;

    public PersonaEngineTests()
    {
        _llmProviderMock = new Mock<ILlmProvider>();
        _personaConfigRepoMock = new Mock<IPersonaConfigurationRepository>();
        _memoryServiceMock = new Mock<IMemoryService>();
        _loggerMock = new Mock<ILogger<PersonaEngine>>();

        _sut = new PersonaEngine(
            _llmProviderMock.Object,
            _personaConfigRepoMock.Object,
            _memoryServiceMock.Object,
            _loggerMock.Object);
    }

    private static Session CreateTestSession() => new()
    {
        Id = Guid.NewGuid(),
        Title = "Test Session",
        Status = SessionStatus.Active,
        ProblemStatement = "Build a todo app",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        Messages = new List<Message>()
    };

    private static Message CreateTestMessage(Session session) => new()
    {
        Id = Guid.NewGuid(),
        SessionId = session.Id,
        FromPersona = PersonaType.User,
        Content = "Please help me build a todo app",
        MessageType = MessageType.ProblemStatement,
        Timestamp = DateTime.UtcNow
    };

    private static PersonaConfiguration CreateTestConfig(PersonaType persona) => new()
    {
        Id = Guid.NewGuid(),
        PersonaType = persona,
        DisplayName = persona.ToString(),
        ModelName = "gemma3:27b",
        SystemPrompt = "You are a helpful assistant.",
        Temperature = 0.7,
        MaxTokens = 4096,
        IsEnabled = true
    };

    #region ProcessAsync Tests

    [Fact]
    public async Task ProcessAsync_WithValidRequest_ReturnsResponse()
    {
        // Arrange
        var session = CreateTestSession();
        var message = CreateTestMessage(session);
        var config = CreateTestConfig(PersonaType.Coordinator);
        var memories = Enumerable.Empty<Memory>();

        _personaConfigRepoMock
            .Setup(x => x.GetByPersonaTypeAsync(PersonaType.Coordinator, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        _llmProviderMock
            .Setup(x => x.GenerateAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LlmResponse
            {
                Success = true,
                Content = "I've analyzed your request. Let me help you build a todo app."
            });

        // Act
        var result = await _sut.ProcessAsync(PersonaType.Coordinator, message, session, memories);

        // Assert
        result.Should().NotBeNull();
        result.ResponseType.Should().Be(MessageType.Answer);
        result.Content.Should().Contain("todo app");
    }

    [Fact]
    public async Task ProcessAsync_WithDelegationResponse_ReturnsDelegationType()
    {
        // Arrange
        var session = CreateTestSession();
        var message = CreateTestMessage(session);
        var config = CreateTestConfig(PersonaType.Coordinator);
        var memories = Enumerable.Empty<Memory>();

        _personaConfigRepoMock
            .Setup(x => x.GetByPersonaTypeAsync(PersonaType.Coordinator, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        _llmProviderMock
            .Setup(x => x.GenerateAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LlmResponse
            {
                Success = true,
                Content = "[DELEGATE:TechnicalArchitect] Please design the architecture for this todo app."
            });

        // Act
        var result = await _sut.ProcessAsync(PersonaType.Coordinator, message, session, memories);

        // Assert
        result.ResponseType.Should().Be(MessageType.Delegation);
        result.DelegateToPersona.Should().Be(PersonaType.TechnicalArchitect);
    }

    [Fact]
    public async Task ProcessAsync_WithClarificationResponse_ReturnsClarificationType()
    {
        // Arrange
        var session = CreateTestSession();
        var message = CreateTestMessage(session);
        var config = CreateTestConfig(PersonaType.BusinessAnalyst);
        var memories = Enumerable.Empty<Memory>();

        _personaConfigRepoMock
            .Setup(x => x.GetByPersonaTypeAsync(PersonaType.BusinessAnalyst, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        _llmProviderMock
            .Setup(x => x.GenerateAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LlmResponse
            {
                Success = true,
                Content = "[CLARIFY] What features would you like in your todo app? Do you need user authentication?"
            });

        // Act
        var result = await _sut.ProcessAsync(PersonaType.BusinessAnalyst, message, session, memories);

        // Assert
        result.ResponseType.Should().Be(MessageType.Clarification);
        result.ClarificationQuestion.Should().Contain("authentication");
    }

    [Fact]
    public async Task ProcessAsync_WithSolutionResponse_ReturnsSolutionType()
    {
        // Arrange
        var session = CreateTestSession();
        var message = CreateTestMessage(session);
        var config = CreateTestConfig(PersonaType.SeniorDeveloper);
        var memories = Enumerable.Empty<Memory>();

        _personaConfigRepoMock
            .Setup(x => x.GetByPersonaTypeAsync(PersonaType.SeniorDeveloper, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        _llmProviderMock
            .Setup(x => x.GenerateAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LlmResponse
            {
                Success = true,
                Content = "[SOLUTION] Here is the complete implementation of the TodoService class..."
            });

        // Act
        var result = await _sut.ProcessAsync(PersonaType.SeniorDeveloper, message, session, memories);

        // Assert
        result.ResponseType.Should().Be(MessageType.Solution);
    }

    [Fact]
    public async Task ProcessAsync_WithStuckResponse_ReturnsStuckType()
    {
        // Arrange
        var session = CreateTestSession();
        var message = CreateTestMessage(session);
        var config = CreateTestConfig(PersonaType.JuniorDeveloper);
        var memories = Enumerable.Empty<Memory>();

        _personaConfigRepoMock
            .Setup(x => x.GetByPersonaTypeAsync(PersonaType.JuniorDeveloper, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        _llmProviderMock
            .Setup(x => x.GenerateAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LlmResponse
            {
                Success = true,
                Content = "[STUCK] I don't have enough information about the database schema to proceed."
            });

        // Act
        var result = await _sut.ProcessAsync(PersonaType.JuniorDeveloper, message, session, memories);

        // Assert
        result.ResponseType.Should().Be(MessageType.Stuck);
        result.IsStuck.Should().BeTrue();
    }

    [Fact]
    public async Task ProcessAsync_WithDeclineResponse_ReturnsDeclineType()
    {
        // Arrange
        var session = CreateTestSession();
        var message = CreateTestMessage(session);
        var config = CreateTestConfig(PersonaType.UIEngineer);
        var memories = Enumerable.Empty<Memory>();

        _personaConfigRepoMock
            .Setup(x => x.GetByPersonaTypeAsync(PersonaType.UIEngineer, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        _llmProviderMock
            .Setup(x => x.GenerateAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LlmResponse
            {
                Success = true,
                Content = "[DECLINE] This is a backend task. Please delegate to the Senior Developer."
            });

        // Act
        var result = await _sut.ProcessAsync(PersonaType.UIEngineer, message, session, memories);

        // Assert
        result.ResponseType.Should().Be(MessageType.Decline);
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public async Task ProcessAsync_WithMissingConfiguration_ReturnsStuckResponse()
    {
        // Arrange
        var session = CreateTestSession();
        var message = CreateTestMessage(session);

        _personaConfigRepoMock
            .Setup(x => x.GetByPersonaTypeAsync(PersonaType.Coordinator, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PersonaConfiguration?)null);

        // Act
        var result = await _sut.ProcessAsync(PersonaType.Coordinator, message, session, Enumerable.Empty<Memory>());

        // Assert
        result.ResponseType.Should().Be(MessageType.Stuck);
        result.Content.Should().Contain("Configuration error");
    }

    [Fact]
    public async Task ProcessAsync_WithDisabledPersona_ReturnsDeclineResponse()
    {
        // Arrange
        var session = CreateTestSession();
        var message = CreateTestMessage(session);
        var config = CreateTestConfig(PersonaType.JuniorQA);
        config.IsEnabled = false;

        _personaConfigRepoMock
            .Setup(x => x.GetByPersonaTypeAsync(PersonaType.JuniorQA, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        // Act
        var result = await _sut.ProcessAsync(PersonaType.JuniorQA, message, session, Enumerable.Empty<Memory>());

        // Assert
        result.ResponseType.Should().Be(MessageType.Decline);
        result.Content.Should().Contain("unavailable");
    }

    [Fact]
    public async Task GetPersonaConfigurationAsync_ReturnsCorrectConfig()
    {
        // Arrange
        var expectedConfig = CreateTestConfig(PersonaType.TechnicalArchitect);

        _personaConfigRepoMock
            .Setup(x => x.GetByPersonaTypeAsync(PersonaType.TechnicalArchitect, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedConfig);

        // Act
        var result = await _sut.GetPersonaConfigurationAsync(PersonaType.TechnicalArchitect);

        // Assert
        result.Should().NotBeNull();
        result!.PersonaType.Should().Be(PersonaType.TechnicalArchitect);
    }

    #endregion

    #region LLM Error Handling Tests

    [Fact]
    public async Task ProcessAsync_WithLlmFailure_ReturnsStuckResponse()
    {
        // Arrange
        var session = CreateTestSession();
        var message = CreateTestMessage(session);
        var config = CreateTestConfig(PersonaType.Coordinator);

        _personaConfigRepoMock
            .Setup(x => x.GetByPersonaTypeAsync(PersonaType.Coordinator, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        _llmProviderMock
            .Setup(x => x.GenerateAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LlmResponse
            {
                Success = false,
                Error = "Connection timeout"
            });

        // Act
        var result = await _sut.ProcessAsync(PersonaType.Coordinator, message, session, Enumerable.Empty<Memory>());

        // Assert
        result.ResponseType.Should().Be(MessageType.Stuck);
        result.Content.Should().Contain("error");
    }

    [Fact]
    public async Task ProcessAsync_WithException_ReturnsStuckResponse()
    {
        // Arrange
        var session = CreateTestSession();
        var message = CreateTestMessage(session);
        var config = CreateTestConfig(PersonaType.Coordinator);

        _personaConfigRepoMock
            .Setup(x => x.GetByPersonaTypeAsync(PersonaType.Coordinator, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        _llmProviderMock
            .Setup(x => x.GenerateAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act
        var result = await _sut.ProcessAsync(PersonaType.Coordinator, message, session, Enumerable.Empty<Memory>());

        // Assert
        result.ResponseType.Should().Be(MessageType.Stuck);
        result.Content.Should().Contain("unexpected error");
    }

    #endregion

    #region Memory Integration Tests

    [Fact]
    public async Task ProcessAsync_WithMemoryStoreCommand_StoresMemory()
    {
        // Arrange
        var session = CreateTestSession();
        var message = CreateTestMessage(session);
        var config = CreateTestConfig(PersonaType.BusinessAnalyst);

        _personaConfigRepoMock
            .Setup(x => x.GetByPersonaTypeAsync(PersonaType.BusinessAnalyst, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        _llmProviderMock
            .Setup(x => x.GenerateAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LlmResponse
            {
                Success = true,
                Content = "[STORE:user-requirements] User needs a todo app with CRUD operations."
            });

        _memoryServiceMock
            .Setup(x => x.StoreMemoryAsync(
                session.Id,
                PersonaType.BusinessAnalyst,
                "user-requirements",
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Memory());

        // Act
        await _sut.ProcessAsync(PersonaType.BusinessAnalyst, message, session, Enumerable.Empty<Memory>());

        // Assert
        _memoryServiceMock.Verify(
            x => x.StoreMemoryAsync(
                session.Id,
                PersonaType.BusinessAnalyst,
                "user-requirements",
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_WithMemories_IncludesInContext()
    {
        // Arrange
        var session = CreateTestSession();
        var message = CreateTestMessage(session);
        var config = CreateTestConfig(PersonaType.Coordinator);
        var memories = new List<Memory>
        {
            new()
            {
                Id = Guid.NewGuid(),
                SessionId = session.Id,
                PersonaType = PersonaType.Coordinator,
                Identifier = "previous-decision",
                Content = "We decided to use PostgreSQL.",
                CreatedAt = DateTime.UtcNow
            }
        };

        _personaConfigRepoMock
            .Setup(x => x.GetByPersonaTypeAsync(PersonaType.Coordinator, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        LlmRequest? capturedRequest = null;
        _llmProviderMock
            .Setup(x => x.GenerateAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .Callback<LlmRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new LlmResponse { Success = true, Content = "OK" });

        // Act
        await _sut.ProcessAsync(PersonaType.Coordinator, message, session, memories);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.SystemPrompt.Should().Contain("previous-decision");
        capturedRequest.SystemPrompt.Should().Contain("PostgreSQL");
    }

    #endregion

    #region Request Building Tests

    [Fact]
    public async Task ProcessAsync_BuildsCorrectLlmRequest()
    {
        // Arrange
        var session = CreateTestSession();
        var message = CreateTestMessage(session);
        var config = CreateTestConfig(PersonaType.Coordinator);
        config.Temperature = 0.5;
        config.MaxTokens = 2048;
        config.ModelName = "custom-model";

        _personaConfigRepoMock
            .Setup(x => x.GetByPersonaTypeAsync(PersonaType.Coordinator, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        LlmRequest? capturedRequest = null;
        _llmProviderMock
            .Setup(x => x.GenerateAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .Callback<LlmRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new LlmResponse { Success = true, Content = "Response" });

        // Act
        await _sut.ProcessAsync(PersonaType.Coordinator, message, session, Enumerable.Empty<Memory>());

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Model.Should().Be("custom-model");
        capturedRequest.Temperature.Should().Be(0.5);
        capturedRequest.MaxTokens.Should().Be(2048);
        capturedRequest.SystemPrompt.Should().Contain(config.SystemPrompt);
    }

    [Fact]
    public async Task ProcessAsync_IncludesSessionContextInPrompt()
    {
        // Arrange
        var session = CreateTestSession();
        session.ProblemStatement = "Build a todo management application";
        var message = CreateTestMessage(session);
        var config = CreateTestConfig(PersonaType.Coordinator);

        _personaConfigRepoMock
            .Setup(x => x.GetByPersonaTypeAsync(PersonaType.Coordinator, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        LlmRequest? capturedRequest = null;
        _llmProviderMock
            .Setup(x => x.GenerateAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .Callback<LlmRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new LlmResponse { Success = true, Content = "Response" });

        // Act
        await _sut.ProcessAsync(PersonaType.Coordinator, message, session, Enumerable.Empty<Memory>());

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.SystemPrompt.Should().Contain("Build a todo management application");
        capturedRequest.SystemPrompt.Should().Contain(session.Id.ToString());
    }

    #endregion
}
