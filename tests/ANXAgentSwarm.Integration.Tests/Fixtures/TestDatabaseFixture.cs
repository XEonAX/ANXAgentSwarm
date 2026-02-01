using ANXAgentSwarm.Core.Interfaces;
using ANXAgentSwarm.Infrastructure.Data;
using ANXAgentSwarm.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ANXAgentSwarm.Integration.Tests.Fixtures;

/// <summary>
/// Provides an in-memory database fixture for integration testing.
/// Each test gets a fresh database instance.
/// </summary>
public class TestDatabaseFixture : IDisposable
{
    private readonly string _databaseName;
    private bool _disposed;

    /// <summary>
    /// The DbContext for testing.
    /// </summary>
    public AppDbContext Context { get; }

    /// <summary>
    /// Service provider for accessing repositories.
    /// </summary>
    public IServiceProvider ServiceProvider { get; }

    public TestDatabaseFixture()
    {
        _databaseName = $"TestDb_{Guid.NewGuid()}";

        var services = new ServiceCollection();

        // Add in-memory database
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(_databaseName)
                   .EnableSensitiveDataLogging()
                   .EnableDetailedErrors());

        // Add repositories
        services.AddScoped<ISessionRepository, SessionRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<IMemoryRepository, MemoryRepository>();
        services.AddScoped<IPersonaConfigurationRepository, PersonaConfigurationRepository>();

        ServiceProvider = services.BuildServiceProvider();

        Context = ServiceProvider.GetRequiredService<AppDbContext>();
        Context.Database.EnsureCreated();
    }

    /// <summary>
    /// Seeds the database with default persona configurations.
    /// </summary>
    public async Task SeedDefaultsAsync()
    {
        var personaRepo = ServiceProvider.GetRequiredService<IPersonaConfigurationRepository>();
        await personaRepo.SeedDefaultsAsync();
    }

    /// <summary>
    /// Clears all data from the database.
    /// </summary>
    public async Task ClearDataAsync()
    {
        Context.Memories.RemoveRange(Context.Memories);
        Context.Messages.RemoveRange(Context.Messages);
        Context.Sessions.RemoveRange(Context.Sessions);
        await Context.SaveChangesAsync();
    }

    /// <summary>
    /// Creates a new scope for the service provider.
    /// </summary>
    public IServiceScope CreateScope()
    {
        return ServiceProvider.CreateScope();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            Context.Database.EnsureDeleted();
            Context.Dispose();

            if (ServiceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        _disposed = true;
    }
}

/// <summary>
/// Shared database fixture for tests that need to share state.
/// </summary>
public class SharedDatabaseFixture : IAsyncLifetime
{
    public AppDbContext Context { get; private set; } = null!;
    public IServiceProvider ServiceProvider { get; private set; } = null!;
    private readonly string _databaseName = $"SharedTestDb_{Guid.NewGuid()}";

    public async Task InitializeAsync()
    {
        var services = new ServiceCollection();

        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(_databaseName)
                   .EnableSensitiveDataLogging()
                   .EnableDetailedErrors());

        services.AddScoped<ISessionRepository, SessionRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<IMemoryRepository, MemoryRepository>();
        services.AddScoped<IPersonaConfigurationRepository, PersonaConfigurationRepository>();

        ServiceProvider = services.BuildServiceProvider();
        Context = ServiceProvider.GetRequiredService<AppDbContext>();

        await Context.Database.EnsureCreatedAsync();

        // Seed defaults
        var personaRepo = ServiceProvider.GetRequiredService<IPersonaConfigurationRepository>();
        await personaRepo.SeedDefaultsAsync();
    }

    public async Task DisposeAsync()
    {
        await Context.Database.EnsureDeletedAsync();
        await Context.DisposeAsync();

        if (ServiceProvider is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else if (ServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}

/// <summary>
/// Collection definition for tests that share the database fixture.
/// </summary>
[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<SharedDatabaseFixture>
{
}
