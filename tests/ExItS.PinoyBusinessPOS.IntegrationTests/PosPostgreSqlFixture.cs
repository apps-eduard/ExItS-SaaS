using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

public sealed class PosPostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:18")
        .Build();

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await _container.StartAsync().ConfigureAwait(false);
        ConnectionString = _container.GetConnectionString();

        var options = new DbContextOptionsBuilder<PosDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        await using var context = new PosDbContext(options);
        await context.Database.MigrateAsync().ConfigureAwait(false);
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync().ConfigureAwait(false);
    }
}

[CollectionDefinition(Name)]
public sealed class PosPostgreSqlCollection : ICollectionFixture<PosPostgreSqlFixture>
{
    public const string Name = "PosPostgreSql";
}
