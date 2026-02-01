using ANXAgentSwarm.Core.Enums;
using ANXAgentSwarm.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ANXAgentSwarm.Infrastructure.Services;

/// <summary>
/// Background service that runs on startup to recover sessions that were
/// interrupted (e.g., due to server restart while processing).
/// Marks any Active sessions as Interrupted so users can manually resume them.
/// </summary>
public class SessionRecoveryService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SessionRecoveryService> _logger;

    public SessionRecoveryService(
        IServiceProvider serviceProvider,
        ILogger<SessionRecoveryService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Session recovery service starting...");

        try
        {
            // Create a scope to get scoped services
            using var scope = _serviceProvider.CreateScope();
            var sessionRepository = scope.ServiceProvider.GetRequiredService<ISessionRepository>();

            // Get all active sessions (sessions that were being processed when server stopped)
            var activeSessions = (await sessionRepository.GetAllAsync(SessionStatus.Active, cancellationToken))
                .ToList();

            if (activeSessions.Count == 0)
            {
                _logger.LogInformation("No interrupted sessions found");
                return;
            }

            _logger.LogWarning(
                "Found {Count} session(s) that were interrupted during processing",
                activeSessions.Count);

            // Mark each as Interrupted
            foreach (var session in activeSessions)
            {
                session.Status = SessionStatus.Interrupted;
                session.UpdatedAt = DateTime.UtcNow;
                await sessionRepository.UpdateAsync(session, cancellationToken);

                _logger.LogInformation(
                    "Marked session {SessionId} ({Title}) as Interrupted",
                    session.Id,
                    session.Title ?? session.ProblemStatement[..Math.Min(50, session.ProblemStatement.Length)]);
            }

            _logger.LogInformation(
                "Session recovery complete. {Count} session(s) marked as Interrupted and can be resumed from the UI.",
                activeSessions.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during session recovery");
            // Don't throw - we don't want to prevent the app from starting
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
