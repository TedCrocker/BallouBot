using BallouBot.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace BallouBot.Data;

/// <summary>
/// The Entity Framework Core database context for BallouBot.
/// Provides access to all entity sets and configures the database schema.
/// </summary>
public class BotDbContext : DbContext
{
    /// <summary>
    /// Gets or sets the guild settings table.
    /// </summary>
    public DbSet<GuildSettings> GuildSettings => Set<GuildSettings>();

    /// <summary>
    /// Gets or sets the welcome configuration table.
    /// </summary>
    public DbSet<WelcomeConfig> WelcomeConfigs => Set<WelcomeConfig>();

    /// <summary>
    /// Initializes a new instance of the <see cref="BotDbContext"/> class.
    /// </summary>
    /// <param name="options">The database context options.</param>
    public BotDbContext(DbContextOptions<BotDbContext> options) : base(options)
    {
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<GuildSettings>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.GuildId).IsUnique();

            entity.Property(e => e.GuildId)
                .IsRequired();

            entity.Property(e => e.GuildName)
                .HasMaxLength(100);

            entity.Property(e => e.Prefix)
                .HasMaxLength(10)
                .HasDefaultValue("!");

            entity.HasOne(e => e.WelcomeConfig)
                .WithOne(w => w.GuildSettings)
                .HasForeignKey<WelcomeConfig>(w => w.GuildId)
                .HasPrincipalKey<GuildSettings>(g => g.GuildId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WelcomeConfig>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.GuildId).IsUnique();

            entity.Property(e => e.GuildId)
                .IsRequired();

            entity.Property(e => e.ChannelId)
                .IsRequired();

            entity.Property(e => e.Message)
                .HasMaxLength(2000)
                .HasDefaultValue("Welcome to {server}, {user}! You are member #{membercount}.");

            entity.Property(e => e.IsEnabled)
                .HasDefaultValue(true);

            entity.Property(e => e.UseEmbed)
                .HasDefaultValue(false);

            entity.Property(e => e.EmbedColor)
                .HasMaxLength(6)
                .HasDefaultValue("5865F2");

            entity.Property(e => e.EmbedTitle)
                .HasMaxLength(256);
        });
    }
}
