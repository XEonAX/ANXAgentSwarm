using ANXAgentSwarm.Core.Entities;
using ANXAgentSwarm.Core.Enums;

namespace ANXAgentSwarm.Core.Interfaces;

/// <summary>
/// Repository interface for PersonaConfiguration entities.
/// </summary>
public interface IPersonaConfigurationRepository
{
    /// <summary>
    /// Gets configuration for a specific persona.
    /// </summary>
    Task<PersonaConfiguration?> GetByPersonaTypeAsync(
        PersonaType personaType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all persona configurations.
    /// </summary>
    Task<IEnumerable<PersonaConfiguration>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all enabled persona configurations.
    /// </summary>
    Task<IEnumerable<PersonaConfiguration>> GetEnabledAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates a persona configuration.
    /// </summary>
    Task<PersonaConfiguration> UpsertAsync(
        PersonaConfiguration configuration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a persona configuration.
    /// </summary>
    Task DeleteAsync(PersonaType personaType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Seeds default persona configurations if none exist.
    /// </summary>
    Task SeedDefaultsAsync(CancellationToken cancellationToken = default);
}
