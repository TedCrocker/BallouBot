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
    /// Gets or sets the Random Richard configuration table.
    /// </summary>
    public DbSet<RichardConfig> RichardConfigs => Set<RichardConfig>();

    /// <summary>
    /// Gets or sets the Random Richard user entries (whitelist/blacklist) table.
    /// </summary>
    public DbSet<RichardUserEntry> RichardUserEntries => Set<RichardUserEntry>();

    /// <summary>
    /// Gets or sets the GIF module configuration table.
    /// </summary>
    public DbSet<GifConfig> GifConfigs => Set<GifConfig>();

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

        modelBuilder.Entity<RichardConfig>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.GuildId).IsUnique();

            entity.Property(e => e.GuildId)
                .IsRequired();

            entity.Property(e => e.IsEnabled)
                .HasDefaultValue(false);

            entity.Property(e => e.MinIntervalMinutes)
                .HasDefaultValue(480);

            entity.Property(e => e.MaxIntervalMinutes)
                .HasDefaultValue(480);

            entity.Property(e => e.UseWhitelistMode)
                .HasDefaultValue(true);

            entity.Property(e => e.FallbackChannelId)
                .IsRequired(false);

            entity.HasOne(e => e.GuildSettings)
                .WithOne(g => g.RichardConfig)
                .HasForeignKey<RichardConfig>(r => r.GuildId)
                .HasPrincipalKey<GuildSettings>(g => g.GuildId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.UserEntries)
                .WithOne(u => u.RichardConfig)
                .HasForeignKey(u => u.GuildId)
                .HasPrincipalKey(r => r.GuildId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RichardUserEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.GuildId, e.UserId, e.ListType }).IsUnique();

            entity.Property(e => e.GuildId)
                .IsRequired();

            entity.Property(e => e.UserId)
                .IsRequired();

            entity.Property(e => e.ListType)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);
        });

        modelBuilder.Entity<GifConfig>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.GuildId).IsUnique();

            entity.Property(e => e.GuildId)
                .IsRequired();

            entity.Property(e => e.IsEnabled)
                .HasDefaultValue(true);

            entity.Property(e => e.Provider)
                .HasMaxLength(50)
                .HasDefaultValue("Tenor");

            entity.Property(e => e.ApiKey)
                .HasMaxLength(500);

            entity.Property(e => e.PreviewCount)
                .HasDefaultValue(5);

            entity.HasOne(e => e.GuildSettings)
                .WithOne(g => g.GifConfig)
                .HasForeignKey<GifConfig>(gc => gc.GuildId)
                .HasPrincipalKey<GuildSettings>(g => g.GuildId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
