using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ExItS.PinoyBuyNowPayLater.Infrastructure.Persistence;

/// <summary>Design-time factory so <c>dotnet ef</c> does not boot the API host.</summary>
internal sealed class BnplDbContextDesignTimeFactory : IDesignTimeDbContextFactory<BnplDbContext>
{
    public BnplDbContext CreateDbContext(string[] args)
    {
        var connectionString = args.Length > 0 && !string.IsNullOrWhiteSpace(args[0])
            ? args[0]
            : "Host=127.0.0.1;Port=5434;Database=ExItS_PinoyBuyNowPayLater_Design;Username=postgres;Password=exits_platform_dev_only";

        var options = new DbContextOptionsBuilder<BnplDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new BnplDbContext(options);
    }
}
