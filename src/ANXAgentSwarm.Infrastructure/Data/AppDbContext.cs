using ANXAgentSwarm.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace ANXAgentSwarm.Infrastructure.Data;

/// <summary>
/// Entity Framework Core database context for ANXAgentSwarm.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Sessions table.
    /// </summary>
    public DbSet<Session> Sessions => Set<Session>();

    /// <summary>
    /// Messages table.
    /// </summary>
    public DbSet<Message> Messages => Set<Message>();

    /// <summary>
    /// Memories table.
    /// </summary>
    public DbSet<Memory> Memories => Set<Memory>();

    /// <summary>
    /// PersonaConfigurations table.
    /// </summary>
    public DbSet<PersonaConfiguration> PersonaConfigurations => Set<PersonaConfiguration>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureSession(modelBuilder);
        ConfigureMessage(modelBuilder);
        ConfigureMemory(modelBuilder);
        ConfigurePersonaConfiguration(modelBuilder);
    }

    private static void ConfigureSession(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Session>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.ProblemStatement)
                .IsRequired();

            entity.Property(e => e.FinalSolution);

            entity.Property(e => e.Status)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.Property(e => e.UpdatedAt)
                .IsRequired();

            entity.HasMany(e => e.Messages)
                .WithOne(m => m.Session)
                .HasForeignKey(m => m.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Memories)
                .WithOne(m => m.Session)
                .HasForeignKey(m => m.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
        });
    }

    private static void ConfigureMessage(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Content)
                .IsRequired();

            entity.Property(e => e.MessageType)
                .IsRequired();

            entity.Property(e => e.FromPersona)
                .IsRequired();

            entity.Property(e => e.Timestamp)
                .IsRequired();

            entity.Property(e => e.InternalReasoning);
            entity.Property(e => e.DelegationContext);
            entity.Property(e => e.RawResponse);

            entity.HasOne(e => e.ParentMessage)
                .WithMany(m => m.Replies)
                .HasForeignKey(e => e.ParentMessageId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => e.SessionId);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => new { e.SessionId, e.Timestamp });
        });
    }

    private static void ConfigureMemory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Memory>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Identifier)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Content)
                .IsRequired();

            entity.Property(e => e.PersonaType)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.HasIndex(e => e.SessionId);
            entity.HasIndex(e => new { e.SessionId, e.PersonaType });
            entity.HasIndex(e => new { e.SessionId, e.PersonaType, e.Identifier })
                .IsUnique();
        });
    }

    private static void ConfigurePersonaConfiguration(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PersonaConfiguration>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.PersonaType)
                .IsRequired();

            entity.Property(e => e.DisplayName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.ModelName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.SystemPrompt)
                .IsRequired();

            entity.Property(e => e.Temperature)
                .IsRequired();

            entity.Property(e => e.MaxTokens)
                .IsRequired();

            entity.Property(e => e.Description)
                .HasMaxLength(500);

            entity.HasIndex(e => e.PersonaType)
                .IsUnique();
        });
    }
}
