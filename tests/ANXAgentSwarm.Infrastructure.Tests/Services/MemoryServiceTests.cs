using ANXAgentSwarm.Core.Entities;
using ANXAgentSwarm.Core.Enums;
using ANXAgentSwarm.Core.Interfaces;
using ANXAgentSwarm.Infrastructure.Configuration;
using ANXAgentSwarm.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace ANXAgentSwarm.Infrastructure.Tests.Services;

/// <summary>
/// Unit tests for the MemoryService.
/// </summary>
public class MemoryServiceTests
{
    private readonly Mock<IMemoryRepository> _memoryRepoMock;
    private readonly Mock<ILogger<MemoryService>> _loggerMock;
    private readonly MemoryOptions _options;
    private readonly MemoryService _sut;

    public MemoryServiceTests()
    {
        _memoryRepoMock = new Mock<IMemoryRepository>();
        _loggerMock = new Mock<ILogger<MemoryService>>();
        _options = new MemoryOptions
        {
            MaxWordsPerMemory = 2000,
            MaxIdentifierWords = 10,
            MaxMemoriesPerPersonaPerSession = 10
        };

        var optionsMock = new Mock<IOptions<MemoryOptions>>();
        optionsMock.Setup(x => x.Value).Returns(_options);

        _sut = new MemoryService(
            _memoryRepoMock.Object,
            optionsMock.Object,
            _loggerMock.Object);
    }

    #region StoreMemoryAsync Tests

    [Fact]
    public async Task StoreMemoryAsync_WithValidData_CreatesMemory()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var persona = PersonaType.BusinessAnalyst;
        var identifier = "user-requirements";
        var content = "The user needs a REST API";

        _memoryRepoMock
            .Setup(x => x.GetCountAsync(sessionId, persona, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _memoryRepoMock
            .Setup(x => x.GetByIdentifierAsync(sessionId, persona, identifier, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Memory?)null);

        Memory? capturedMemory = null;
        _memoryRepoMock
            .Setup(x => x.CreateAsync(It.IsAny<Memory>(), It.IsAny<CancellationToken>()))
            .Callback<Memory, CancellationToken>((m, _) => capturedMemory = m)
            .ReturnsAsync((Memory m, CancellationToken _) => m);

        // Act
        var result = await _sut.StoreMemoryAsync(sessionId, persona, identifier, content);

        // Assert
        capturedMemory.Should().NotBeNull();
        capturedMemory!.SessionId.Should().Be(sessionId);
        capturedMemory.PersonaType.Should().Be(persona);
        capturedMemory.Identifier.Should().Be(identifier);
        capturedMemory.Content.Should().Be(content);
    }

    [Fact]
    public async Task StoreMemoryAsync_WithExistingIdentifier_UpdatesMemory()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var persona = PersonaType.Coordinator;
        var identifier = "decision";
        var existingMemory = new Memory
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            PersonaType = persona,
            Identifier = identifier,
            Content = "Old content",
            AccessCount = 1
        };

        _memoryRepoMock
            .Setup(x => x.GetCountAsync(sessionId, persona, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _memoryRepoMock
            .Setup(x => x.GetByIdentifierAsync(sessionId, persona, identifier, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingMemory);

        _memoryRepoMock
            .Setup(x => x.UpdateAsync(It.IsAny<Memory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Memory m, CancellationToken _) => m);

        // Act
        await _sut.StoreMemoryAsync(sessionId, persona, identifier, "New content");

        // Assert
        _memoryRepoMock.Verify(
            x => x.UpdateAsync(
                It.Is<Memory>(m => m.Content == "New content" && m.AccessCount == 2),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task StoreMemoryAsync_WhenAtLimit_RemovesOldestMemory()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var persona = PersonaType.TechnicalArchitect;
        var oldestMemoryId = Guid.NewGuid();

        var existingMemories = new List<Memory>
        {
            new() { Id = oldestMemoryId, CreatedAt = DateTime.UtcNow.AddHours(-2) },
            new() { Id = Guid.NewGuid(), CreatedAt = DateTime.UtcNow.AddHours(-1) }
        };

        _options.MaxMemoriesPerPersonaPerSession = 2;

        _memoryRepoMock
            .Setup(x => x.GetCountAsync(sessionId, persona, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_options.MaxMemoriesPerPersonaPerSession);

        _memoryRepoMock
            .Setup(x => x.GetBySessionAndPersonaAsync(sessionId, persona, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingMemories);

        _memoryRepoMock
            .Setup(x => x.GetByIdentifierAsync(sessionId, persona, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Memory?)null);

        _memoryRepoMock
            .Setup(x => x.CreateAsync(It.IsAny<Memory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Memory m, CancellationToken _) => m);

        // Act
        await _sut.StoreMemoryAsync(sessionId, persona, "new-identifier", "New content");

        // Assert
        _memoryRepoMock.Verify(
            x => x.DeleteAsync(oldestMemoryId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task StoreMemoryAsync_WithTooLongIdentifier_ThrowsArgumentException()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var persona = PersonaType.Coordinator;
        var longIdentifier = "this is a very long identifier that has way more than ten words in it";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.StoreMemoryAsync(sessionId, persona, longIdentifier, "content"));
    }

    [Fact]
    public async Task StoreMemoryAsync_WithTooLongContent_ThrowsArgumentException()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var persona = PersonaType.Coordinator;
        var longContent = string.Join(" ", Enumerable.Repeat("word", 2001));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.StoreMemoryAsync(sessionId, persona, "identifier", longContent));
    }

    #endregion

    #region SearchMemoriesAsync Tests

    [Fact]
    public async Task SearchMemoriesAsync_WithValidQuery_ReturnsMatchingMemories()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var persona = PersonaType.BusinessAnalyst;
        var query = "requirements";
        var expectedMemories = new List<Memory>
        {
            new() { Id = Guid.NewGuid(), Identifier = "user-requirements", Content = "Requirements content" }
        };

        _memoryRepoMock
            .Setup(x => x.SearchAsync(sessionId, persona, query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedMemories);

        _memoryRepoMock
            .Setup(x => x.UpdateAsync(It.IsAny<Memory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Memory m, CancellationToken _) => m);

        // Act
        var result = await _sut.SearchMemoriesAsync(sessionId, persona, query);

        // Assert
        result.Should().HaveCount(1);
        result.First().Identifier.Should().Be("user-requirements");
    }

    [Fact]
    public async Task SearchMemoriesAsync_UpdatesAccessCount()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var persona = PersonaType.Coordinator;
        var memory = new Memory
        {
            Id = Guid.NewGuid(),
            AccessCount = 5
        };

        _memoryRepoMock
            .Setup(x => x.SearchAsync(sessionId, persona, "query", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Memory> { memory });

        _memoryRepoMock
            .Setup(x => x.UpdateAsync(It.IsAny<Memory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Memory m, CancellationToken _) => m);

        // Act
        await _sut.SearchMemoriesAsync(sessionId, persona, "query");

        // Assert
        _memoryRepoMock.Verify(
            x => x.UpdateAsync(
                It.Is<Memory>(m => m.AccessCount == 6),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SearchMemoriesAsync_WithEmptyQuery_ReturnsEmpty()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var persona = PersonaType.Coordinator;

        // Act
        var result = await _sut.SearchMemoriesAsync(sessionId, persona, "");

        // Assert
        result.Should().BeEmpty();
        _memoryRepoMock.Verify(
            x => x.SearchAsync(It.IsAny<Guid>(), It.IsAny<PersonaType>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region GetRecentMemoriesAsync Tests

    [Fact]
    public async Task GetRecentMemoriesAsync_ReturnsRecentMemories()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var persona = PersonaType.SeniorDeveloper;
        var memories = new List<Memory>
        {
            new() { Id = Guid.NewGuid(), CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), CreatedAt = DateTime.UtcNow.AddMinutes(-5) }
        };

        _memoryRepoMock
            .Setup(x => x.GetBySessionAndPersonaAsync(sessionId, persona, It.IsAny<CancellationToken>()))
            .ReturnsAsync(memories);

        // Act
        var result = await _sut.GetRecentMemoriesAsync(sessionId, persona, 5);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetRecentMemoriesAsync_CapsAtMaxLimit()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var persona = PersonaType.Coordinator;
        var memories = Enumerable.Range(0, 15)
            .Select(i => new Memory { Id = Guid.NewGuid() })
            .ToList();

        _memoryRepoMock
            .Setup(x => x.GetBySessionAndPersonaAsync(sessionId, persona, It.IsAny<CancellationToken>()))
            .ReturnsAsync(memories);

        // Act - request more than max
        var result = await _sut.GetRecentMemoriesAsync(sessionId, persona, 100);

        // Assert - should be capped at MaxMemoriesPerPersonaPerSession
        result.Count().Should().BeLessThanOrEqualTo(_options.MaxMemoriesPerPersonaPerSession);
    }

    #endregion

    #region GetMemoryByIdentifierAsync Tests

    [Fact]
    public async Task GetMemoryByIdentifierAsync_WithExistingMemory_ReturnsMemory()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var persona = PersonaType.TechnicalArchitect;
        var identifier = "architecture-decision";
        var expectedMemory = new Memory
        {
            Id = Guid.NewGuid(),
            Identifier = identifier,
            Content = "Use microservices"
        };

        _memoryRepoMock
            .Setup(x => x.GetByIdentifierAsync(sessionId, persona, identifier, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedMemory);

        _memoryRepoMock
            .Setup(x => x.UpdateAsync(It.IsAny<Memory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Memory m, CancellationToken _) => m);

        // Act
        var result = await _sut.GetMemoryByIdentifierAsync(sessionId, persona, identifier);

        // Assert
        result.Should().NotBeNull();
        result!.Identifier.Should().Be(identifier);
    }

    [Fact]
    public async Task GetMemoryByIdentifierAsync_WithNonExistingMemory_ReturnsNull()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var persona = PersonaType.Coordinator;

        _memoryRepoMock
            .Setup(x => x.GetByIdentifierAsync(sessionId, persona, "nonexistent", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Memory?)null);

        // Act
        var result = await _sut.GetMemoryByIdentifierAsync(sessionId, persona, "nonexistent");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region DeleteMemoryAsync Tests

    [Fact]
    public async Task DeleteMemoryAsync_CallsRepository()
    {
        // Arrange
        var memoryId = Guid.NewGuid();

        // Act
        await _sut.DeleteMemoryAsync(memoryId);

        // Assert
        _memoryRepoMock.Verify(
            x => x.DeleteAsync(memoryId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}
