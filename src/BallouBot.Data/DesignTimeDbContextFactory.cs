using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BallouBot.Data;

/// <summary>
/// Factory for creating BotDbContext instances at design time (for EF Core migrations CLI).
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<BotDbContext>
{
    /// <inheritdoc />
    public BotDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<BotDbContext>();
        optionsBuilder.UseSqlite("Data Source=balloubot.db");

        return new BotDbContext(optionsBuilder.Options);
    }
}
