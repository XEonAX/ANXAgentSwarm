using ANXAgentSwarm.Core.Entities;
using ANXAgentSwarm.Core.Enums;
using ANXAgentSwarm.Core.Interfaces;
using ANXAgentSwarm.Core.Models;
using ANXAgentSwarm.Infrastructure.Configuration;
using ANXAgentSwarm.Infrastructure.Data;
using ANXAgentSwarm.Infrastructure.Repositories;
using ANXAgentSwarm.Infrastructure.Services;
using ANXAgentSwarm.Integration.Tests.Mocks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace ANXAgentSwarm.Integration.Tests.Benchmarks;

/// <summary>
/// Performance benchmarks for the agent orchestration loop.
/// Run with: dotnet run -c Release -- --filter "*OrchestrationBenchmarks*"
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class OrchestrationBenchmarks
{
    private IServiceProvider _serviceProvider = null!;
    private IServiceScope _scope = null!;
    private MockLlmProvider _mockLlm = null!;
    private IAgentOrchestrator _orchestrator = null!;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();

        // Add logging (null logger for benchmarks)
        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        // Add in-memory database
        var databaseName = $"BenchmarkDb_{Guid.NewGuid()}";
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));

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

        // Create mock LLM
        _mockLlm = new MockLlmProvider();

        // Mock hub broadcaster
        var hubBroadcasterMock = new Mock<ISessionHubBroadcaster>();
        hubBroadcasterMock
            .Setup(x => x.BroadcastMessageReceivedAsync(
                It.IsAny<Guid>(), It.IsAny<Core.DTOs.MessageDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        hubBroadcasterMock
            .Setup(x => x.BroadcastSessionStatusChangedAsync(
                It.IsAny<Guid>(), It.IsAny<Core.DTOs.SessionDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        hubBroadcasterMock
            .Setup(x => x.BroadcastClarificationRequestedAsync(
                It.IsAny<Guid>(), It.IsAny<Core.DTOs.MessageDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        hubBroadcasterMock
            .Setup(x => x.BroadcastSolutionReadyAsync(
                It.IsAny<Guid>(), It.IsAny<Core.DTOs.SessionDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        hubBroadcasterMock
            .Setup(x => x.BroadcastSessionStuckAsync(
                It.IsAny<Guid>(), It.IsAny<Core.DTOs.SessionDto>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        services.AddSingleton<ILlmProvider>(_mockLlm);
        services.AddSingleton(hubBroadcasterMock.Object);

        // Add services
        services.AddScoped<IMemoryService, MemoryService>();
        services.AddScoped<IPersonaEngine, PersonaEngine>();
        services.AddScoped<IAgentOrchestrator, AgentOrchestrator>();

        _serviceProvider = services.BuildServiceProvider();

        // Create scope and keep it alive for the benchmark run
        _scope = _serviceProvider.CreateScope();
        var context = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
        context.Database.EnsureCreated();

        var personaRepo = _scope.ServiceProvider.GetRequiredService<IPersonaConfigurationRepository>();
        personaRepo.SeedDefaultsAsync().GetAwaiter().GetResult();

        // Get orchestrator from the scope (keeps context alive)
        _orchestrator = _scope.ServiceProvider.GetRequiredService<IAgentOrchestrator>();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _scope?.Dispose();
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    [IterationSetup]
    public void IterationSetup()
    {
        _mockLlm.Reset();
    }

    [Benchmark(Description = "Simple session - instant solution")]
    public async Task<Session> SimpleSolution()
    {
        _mockLlm.EnqueueSolution("Simple solution content.");
        return await _orchestrator.StartSessionAsync("Simple problem statement");
    }

    [Benchmark(Description = "Single delegation chain")]
    public async Task<Session> SingleDelegation()
    {
        _mockLlm.EnqueueDelegation(PersonaType.TechnicalArchitect, "Design the solution");
        _mockLlm.EnqueueSolution("Architecture designed.");
        _mockLlm.EnqueueSolution("## Final Solution");
        
        return await _orchestrator.StartSessionAsync("Design a system");
    }

    [Benchmark(Description = "Three-step delegation chain")]
    public async Task<Session> ThreeStepDelegation()
    {
        _mockLlm.EnqueueDelegation(PersonaType.BusinessAnalyst, "Gather requirements");
        _mockLlm.EnqueueDelegation(PersonaType.TechnicalArchitect, "Design system");
        _mockLlm.EnqueueDelegation(PersonaType.SeniorDeveloper, "Implement");
        _mockLlm.EnqueueSolution("Implementation complete.");
        _mockLlm.EnqueueSolution("## Final Solution");
        
        return await _orchestrator.StartSessionAsync("Build complete system");
    }

    [Benchmark(Description = "Five-step delegation chain")]
    public async Task<Session> FiveStepDelegation()
    {
        _mockLlm.EnqueueDelegation(PersonaType.BusinessAnalyst, "Requirements");
        _mockLlm.EnqueueDelegation(PersonaType.UXEngineer, "User flows");
        _mockLlm.EnqueueDelegation(PersonaType.TechnicalArchitect, "Architecture");
        _mockLlm.EnqueueDelegation(PersonaType.SeniorDeveloper, "Implementation");
        _mockLlm.EnqueueDelegation(PersonaType.SeniorQA, "Testing");
        _mockLlm.EnqueueSolution("All tested.");
        _mockLlm.EnqueueSolution("## Final Solution");
        
        return await _orchestrator.StartSessionAsync("Complex project");
    }

    [Benchmark(Description = "Session with memory storage")]
    public async Task<Session> SessionWithMemory()
    {
        _mockLlm.EnqueueWithMemoryStore(
            "[DELEGATE:TechnicalArchitect] Design based on requirements",
            "requirements",
            "User needs a REST API with authentication");
        _mockLlm.EnqueueWithMemoryStore(
            "[SOLUTION] Implementation based on stored requirements",
            "architecture",
            "Using clean architecture pattern");
        _mockLlm.EnqueueSolution("## Final Solution");
        
        return await _orchestrator.StartSessionAsync("Build API with memory");
    }

    [Benchmark(Description = "Clarification and resume flow")]
    public async Task<Session> ClarificationFlow()
    {
        _mockLlm.EnqueueClarification("What database system?");
        
        var session = await _orchestrator.StartSessionAsync("Build data system");
        
        _mockLlm.EnqueueSolution("PostgreSQL implementation complete.");
        
        await _orchestrator.HandleUserClarificationAsync(session.Id, "PostgreSQL");
        
        return await _serviceProvider.CreateScope()
            .ServiceProvider.GetRequiredService<ISessionRepository>()
            .GetByIdAsync(session.Id) ?? session;
    }

    [Benchmark(Description = "Decline and retry flow")]
    public async Task<Session> DeclineAndRetry()
    {
        _mockLlm.EnqueueDelegation(PersonaType.JuniorDeveloper, "Implement feature");
        _mockLlm.EnqueueDecline("Too complex for me");
        _mockLlm.EnqueueDelegation(PersonaType.SeniorDeveloper, "Implement feature");
        _mockLlm.EnqueueSolution("Implementation complete.");
        _mockLlm.EnqueueSolution("## Final Solution");
        
        return await _orchestrator.StartSessionAsync("Complex feature");
    }

    [Benchmark(Description = "Ten delegations in sequence")]
    public async Task<Session> TenDelegations()
    {
        var personas = new[]
        {
            PersonaType.BusinessAnalyst,
            PersonaType.UXEngineer,
            PersonaType.UIEngineer,
            PersonaType.TechnicalArchitect,
            PersonaType.SeniorDeveloper,
            PersonaType.JuniorDeveloper,
            PersonaType.SeniorQA,
            PersonaType.JuniorQA,
            PersonaType.DocumentWriter,
            PersonaType.SeniorDeveloper
        };

        foreach (var persona in personas)
        {
            _mockLlm.EnqueueDelegation(persona, $"Step for {persona}");
        }
        _mockLlm.EnqueueSolution("All steps complete.");
        _mockLlm.EnqueueSolution("## Final Solution");
        
        return await _orchestrator.StartSessionAsync("Full team project");
    }
}

/// <summary>
/// Memory benchmarks for the orchestration system.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 2, iterationCount: 5)]
public class MemoryBenchmarks
{
    private IServiceProvider _serviceProvider = null!;
    private IServiceScope _scope = null!;
    private MockLlmProvider _mockLlm = null!;
    private IMemoryService _memoryService = null!;
    private Guid _sessionId;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();

        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        var databaseName = $"MemoryBenchmarkDb_{Guid.NewGuid()}";
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));

        services.Configure<MemoryOptions>(options =>
        {
            options.MaxWordsPerMemory = 2000;
            options.MaxIdentifierWords = 10;
            options.MaxMemoriesPerPersonaPerSession = 10;
        });

        services.AddScoped<ISessionRepository, SessionRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<IMemoryRepository, MemoryRepository>();
        services.AddScoped<IPersonaConfigurationRepository, PersonaConfigurationRepository>();

        _mockLlm = new MockLlmProvider();
        services.AddSingleton<ILlmProvider>(_mockLlm);

        services.AddScoped<IMemoryService, MemoryService>();

        _serviceProvider = services.BuildServiceProvider();

        // Create scope and keep it alive for the benchmark run
        _scope = _serviceProvider.CreateScope();
        var context = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
        context.Database.EnsureCreated();

        // Create a session for memory tests
        var session = new Session
        {
            Id = Guid.NewGuid(),
            Title = "Memory Benchmark Session",
            Status = SessionStatus.Active,
            ProblemStatement = "Benchmark",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Sessions.Add(session);
        context.SaveChanges();
        _sessionId = session.Id;

        _memoryService = _scope.ServiceProvider.GetRequiredService<IMemoryService>();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _scope?.Dispose();
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    [Benchmark(Description = "Store single memory")]
    public async Task<Memory> StoreMemory()
    {
        var identifier = $"memory-{Guid.NewGuid():N}".Substring(0, 20);
        return await _memoryService.StoreMemoryAsync(
            _sessionId,
            PersonaType.Coordinator,
            identifier,
            "This is the content of the memory being stored.");
    }

    [Benchmark(Description = "Retrieve recent memories")]
    public async Task<IEnumerable<Memory>> GetRecentMemories()
    {
        return await _memoryService.GetRecentMemoriesAsync(
            _sessionId,
            PersonaType.Coordinator,
            10);
    }

    [Benchmark(Description = "Search memories")]
    public async Task<IEnumerable<Memory>> SearchMemories()
    {
        return await _memoryService.SearchMemoriesAsync(
            _sessionId,
            PersonaType.Coordinator,
            "content");
    }
}

/// <summary>
/// Benchmark runner for integration tests.
/// This class can be run directly to execute benchmarks.
/// </summary>
public static class BenchmarkRunner
{
    /// <summary>
    /// Run all benchmarks. Use command: dotnet run -c Release
    /// </summary>
    public static void RunAll()
    {
        BenchmarkDotNet.Running.BenchmarkRunner.Run<OrchestrationBenchmarks>();
        BenchmarkDotNet.Running.BenchmarkRunner.Run<MemoryBenchmarks>();
    }

    /// <summary>
    /// Run orchestration benchmarks only.
    /// </summary>
    public static void RunOrchestration()
    {
        BenchmarkDotNet.Running.BenchmarkRunner.Run<OrchestrationBenchmarks>();
    }

    /// <summary>
    /// Run memory benchmarks only.
    /// </summary>
    public static void RunMemory()
    {
        BenchmarkDotNet.Running.BenchmarkRunner.Run<MemoryBenchmarks>();
    }
}

/// <summary>
/// Tests to verify benchmarks compile and run (not actual performance tests).
/// </summary>
public class BenchmarkSmokeTests
{
    [Fact]
    public async Task OrchestrationBenchmarks_SimpleSolution_Runs()
    {
        // Arrange
        var benchmarks = new OrchestrationBenchmarks();
        benchmarks.Setup();

        try
        {
            benchmarks.IterationSetup();

            // Act
            var result = await benchmarks.SimpleSolution();

            // Assert
            result.Should().NotBeNull();
            result.Status.Should().Be(SessionStatus.Completed);
        }
        finally
        {
            benchmarks.Cleanup();
        }
    }

    [Fact]
    public async Task OrchestrationBenchmarks_ThreeStepDelegation_Runs()
    {
        // Arrange
        var benchmarks = new OrchestrationBenchmarks();
        benchmarks.Setup();

        try
        {
            benchmarks.IterationSetup();

            // Act
            var result = await benchmarks.ThreeStepDelegation();

            // Assert
            result.Should().NotBeNull();
            result.Status.Should().Be(SessionStatus.Completed);
        }
        finally
        {
            benchmarks.Cleanup();
        }
    }

    [Fact]
    public async Task MemoryBenchmarks_StoreMemory_Runs()
    {
        // Arrange
        var benchmarks = new MemoryBenchmarks();
        benchmarks.Setup();

        try
        {
            // Act
            var result = await benchmarks.StoreMemory();

            // Assert
            result.Should().NotBeNull();
        }
        finally
        {
            benchmarks.Cleanup();
        }
    }
}
