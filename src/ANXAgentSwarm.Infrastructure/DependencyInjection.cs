using ANXAgentSwarm.Core.Interfaces;
using ANXAgentSwarm.Infrastructure.Configuration;
using ANXAgentSwarm.Infrastructure.Data;
using ANXAgentSwarm.Infrastructure.FileSystem;
using ANXAgentSwarm.Infrastructure.LlmProviders;
using ANXAgentSwarm.Infrastructure.Repositories;
using ANXAgentSwarm.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ANXAgentSwarm.Infrastructure;

/// <summary>
/// Extension methods for registering Infrastructure services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds Infrastructure services to the DI container.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure options
        services.Configure<OllamaOptions>(configuration.GetSection(OllamaOptions.SectionName));
        services.Configure<WorkspaceOptions>(configuration.GetSection(WorkspaceOptions.SectionName));
        services.Configure<MemoryOptions>(configuration.GetSection(MemoryOptions.SectionName));

        // Register DbContext
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=anxagentswarm.db";
        
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(connectionString));

        // Register repositories
        services.AddScoped<ISessionRepository, SessionRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<IMemoryRepository, MemoryRepository>();
        services.AddScoped<IPersonaConfigurationRepository, PersonaConfigurationRepository>();

        // Register services
        services.AddScoped<IWorkspaceService, WorkspaceService>();
        services.AddScoped<IMemoryService, MemoryService>();
        services.AddScoped<IPersonaEngine, PersonaEngine>();
        services.AddScoped<IAgentOrchestrator, AgentOrchestrator>();

        // Register Ollama provider with HttpClient
        services.AddHttpClient<ILlmProvider, OllamaProvider>();

        // Register startup recovery service to handle interrupted sessions
        services.AddHostedService<SessionRecoveryService>();

        return services;
    }

    /// <summary>
    /// Ensures the database is created and seeds default data.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var personaRepo = scope.ServiceProvider.GetRequiredService<IPersonaConfigurationRepository>();

        // Create database if it doesn't exist
        await context.Database.EnsureCreatedAsync();

        // Seed default persona configurations
        await personaRepo.SeedDefaultsAsync();
    }
}
