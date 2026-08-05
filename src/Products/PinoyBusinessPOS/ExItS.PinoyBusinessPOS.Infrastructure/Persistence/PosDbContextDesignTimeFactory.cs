using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence;

/// <summary>Design-time factory so <c>dotnet ef</c> does not boot the API host (Production guards).</summary>
internal sealed class PosDbContextDesignTimeFactory : IDesignTimeDbContextFactory<PosDbContext>
{
    public PosDbContext CreateDbContext(string[] args)
    {
        var connectionString = args.Length > 0 && !string.IsNullOrWhiteSpace(args[0])
            ? args[0]
            : "Host=127.0.0.1;Port=5434;Database=ExItS_PinoyBusinessPOS_Design;Username=postgres;Password=exits_platform_dev_only";

        var options = new DbContextOptionsBuilder<PosDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new PosDbContext(options);
    }
}
