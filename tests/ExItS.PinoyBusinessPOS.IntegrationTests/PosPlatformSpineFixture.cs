using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using ExItS.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

public sealed class PosPlatformSpineFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _platformContainer = new PostgreSqlBuilder()
        .WithImage("postgres:18")
        .Build();

    private readonly PostgreSqlContainer _posContainer = new PostgreSqlBuilder()
        .WithImage("postgres:18")
        .Build();

    public string PlatformConnectionString { get; private set; } = string.Empty;

    public string PosConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await _platformContainer.StartAsync().ConfigureAwait(false);
        await _posContainer.StartAsync().ConfigureAwait(false);

        PlatformConnectionString = _platformContainer.GetConnectionString();
        PosConnectionString = _posContainer.GetConnectionString();

        var platformOptions = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseNpgsql(PlatformConnectionString)
            .Options;
        await using (var platform = new PlatformDbContext(platformOptions))
        {
            await platform.Database.MigrateAsync().ConfigureAwait(false);
        }

        var posOptions = new DbContextOptionsBuilder<PosDbContext>()
            .UseNpgsql(PosConnectionString)
            .Options;
        await using (var pos = new PosDbContext(posOptions))
        {
            await pos.Database.MigrateAsync().ConfigureAwait(false);
        }
    }

    public async Task DisposeAsync()
    {
        await _platformContainer.DisposeAsync().ConfigureAwait(false);
        await _posContainer.DisposeAsync().ConfigureAwait(false);
    }
}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class PosPlatformSpineCollection : ICollectionFixture<PosPlatformSpineFixture>
{
    public const string Name = "PosPlatformSpine";
}
