using ANXAgentSwarm.Core.Interfaces;
using ANXAgentSwarm.Infrastructure.Configuration;
using ANXAgentSwarm.Infrastructure.Data;
using ANXAgentSwarm.Infrastructure.Repositories;
using ANXAgentSwarm.Infrastructure.Services;
using ANXAgentSwarm.Integration.Tests.Mocks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace ANXAgentSwarm.Integration.Tests;

/// <summary>
/// Base class for integration tests providing common setup and utilities.
/// </summary>
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected IServiceProvider ServiceProvider { get; private set; } = null!;
    protected IServiceScope Scope { get; private set; } = null!;
    protected AppDbContext DbContext { get; private set; } = null!;
    protected MockLlmProvider MockLlm { get; private set; } = null!;
    protected Mock<ISessionHubBroadcaster> MockHubBroadcaster { get; private set; } = null!;

    // Core services
    protected IAgentOrchestrator Orchestrator { get; private set; } = null!;
    protected ISessionRepository SessionRepository { get; private set; } = null!;
    protected IMessageRepository MessageRepository { get; private set; } = null!;
    protected IMemoryRepository MemoryRepository { get; private set; } = null!;
    protected IMemoryService MemoryService { get; private set; } = null!;
    protected IPersonaEngine PersonaEngine { get; private set; } = null!;

    private readonly string _databaseName = $"IntegrationTest_{Guid.NewGuid()}";

    /// <summary>
    /// Override to configure the mock LLM before tests run.
    /// </summary>
    protected virtual void ConfigureMockLlm(MockLlmProvider mockLlm)
    {
        // Default: set a simple answer response
        mockLlm.SetDefaultResponse(new Core.Models.LlmResponse
        {
            Success = true,
            Content = "Default mock response"
        });
    }

    /// <summary>
    /// Override to configure additional services.
    /// </summary>
    protected virtual void ConfigureServices(IServiceCollection services)
    {
    }

    public async Task InitializeAsync()
    {
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddDebug();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        // Add in-memory database
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(_databaseName)
                   .EnableSensitiveDataLogging()
                   .EnableDetailedErrors());

        // Configure options
        services.Configure<MemoryOptions>(options =>
        {
            options.MaxWordsPerMemory = 2000;
            options.MaxIdentifierWords = 10;
            options.MaxMemoriesPerPersonaPerSession = 10;
        });

        // Add repositories
        services.AddScoped<ISessionRepository, SessionRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<IMemoryRepository, MemoryRepository>();
        services.AddScoped<IPersonaConfigurationRepository, PersonaConfigurationRepository>();

        // Create mocks
        MockLlm = new MockLlmProvider();
        ConfigureMockLlm(MockLlm);

        MockHubBroadcaster = new Mock<ISessionHubBroadcaster>();
        SetupHubBroadcasterMock(MockHubBroadcaster);

        // Register mock LLM
        services.AddSingleton<ILlmProvider>(MockLlm);
        services.AddSingleton(MockHubBroadcaster.Object);

        // Add services
        services.AddScoped<IMemoryService, MemoryService>();
        services.AddScoped<IPersonaEngine, PersonaEngine>();
        services.AddScoped<IAgentOrchestrator, AgentOrchestrator>();

        // Allow derived classes to configure additional services
        ConfigureServices(services);

        ServiceProvider = services.BuildServiceProvider();
        Scope = ServiceProvider.CreateScope();

        // Initialize database
        DbContext = Scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await DbContext.Database.EnsureCreatedAsync();

        // Seed persona configurations
        var personaRepo = Scope.ServiceProvider.GetRequiredService<IPersonaConfigurationRepository>();
        await personaRepo.SeedDefaultsAsync();

        // Get services
        Orchestrator = Scope.ServiceProvider.GetRequiredService<IAgentOrchestrator>();
        SessionRepository = Scope.ServiceProvider.GetRequiredService<ISessionRepository>();
        MessageRepository = Scope.ServiceProvider.GetRequiredService<IMessageRepository>();
        MemoryRepository = Scope.ServiceProvider.GetRequiredService<IMemoryRepository>();
        MemoryService = Scope.ServiceProvider.GetRequiredService<IMemoryService>();
        PersonaEngine = Scope.ServiceProvider.GetRequiredService<IPersonaEngine>();
    }

    public async Task DisposeAsync()
    {
        await DbContext.Database.EnsureDeletedAsync();
        Scope.Dispose();

        if (ServiceProvider is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else if (ServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    /// <summary>
    /// Sets up the hub broadcaster mock with default behavior.
    /// </summary>
    protected virtual void SetupHubBroadcasterMock(Mock<ISessionHubBroadcaster> mock)
    {
        mock.Setup(x => x.BroadcastMessageReceivedAsync(
                It.IsAny<Guid>(),
                It.IsAny<Core.DTOs.MessageDto>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mock.Setup(x => x.BroadcastSessionStatusChangedAsync(
                It.IsAny<Guid>(),
                It.IsAny<Core.DTOs.SessionDto>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mock.Setup(x => x.BroadcastClarificationRequestedAsync(
                It.IsAny<Guid>(),
                It.IsAny<Core.DTOs.MessageDto>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mock.Setup(x => x.BroadcastSolutionReadyAsync(
                It.IsAny<Guid>(),
                It.IsAny<Core.DTOs.SessionDto>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mock.Setup(x => x.BroadcastSessionStuckAsync(
                It.IsAny<Guid>(),
                It.IsAny<Core.DTOs.SessionDto>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    /// <summary>
    /// Resets the mock LLM and reconfigures it.
    /// </summary>
    protected void ResetMockLlm()
    {
        MockLlm.Reset();
        ConfigureMockLlm(MockLlm);
    }

    /// <summary>
    /// Gets a new scope with fresh services.
    /// </summary>
    protected IServiceScope CreateNewScope()
    {
        return ServiceProvider.CreateScope();
    }

    /// <summary>
    /// Verifies that a specific number of messages were broadcast.
    /// </summary>
    protected void VerifyMessageBroadcastCount(int expectedCount)
    {
        MockHubBroadcaster.Verify(
            x => x.BroadcastMessageReceivedAsync(
                It.IsAny<Guid>(),
                It.IsAny<Core.DTOs.MessageDto>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(expectedCount));
    }

    /// <summary>
    /// Verifies that solution ready was broadcast.
    /// </summary>
    protected void VerifySolutionBroadcast(Guid sessionId)
    {
        MockHubBroadcaster.Verify(
            x => x.BroadcastSolutionReadyAsync(
                sessionId,
                It.IsAny<Core.DTOs.SessionDto>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Verifies that clarification was requested.
    /// </summary>
    protected void VerifyClarificationBroadcast(Guid sessionId)
    {
        MockHubBroadcaster.Verify(
            x => x.BroadcastClarificationRequestedAsync(
                sessionId,
                It.IsAny<Core.DTOs.MessageDto>(),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    /// <summary>
    /// Verifies that session stuck was broadcast.
    /// </summary>
    protected void VerifySessionStuckBroadcast(Guid sessionId)
    {
        MockHubBroadcaster.Verify(
            x => x.BroadcastSessionStuckAsync(
                sessionId,
                It.IsAny<Core.DTOs.SessionDto>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }
}
