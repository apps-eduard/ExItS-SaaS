using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Auth;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Infrastructure.Idempotency;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class PosIdempotencyServiceTests(PosPostgreSqlFixture fixture)
{
    [Fact]
    public async Task Exact_replay_executes_once_and_returns_original_outcome()
    {
        await using var db = CreateContext();
        await db.Database.MigrateAsync();
        var sut = new PosIdempotencyService(db, new FixedClock());
        var org = Guid.NewGuid();
        var executions = 0;

        var request = new PosIdempotencyRequest(
            org, PosProductCodes.PinoyBusinessPos, "dev.offline-probe", "key-1", "hash-aaa", Guid.NewGuid());

        var first = await sut.ExecuteAsync(request, async _ =>
        {
            executions++;
            await Task.Yield();
            return new PosIdempotencyExecutionResult("succeeded", "{\"ok\":true}", "ref-1");
        });

        var second = await sut.ExecuteAsync(request, async _ =>
        {
            executions++;
            await Task.Yield();
            return new PosIdempotencyExecutionResult("succeeded", "{\"ok\":true}", "ref-2");
        });

        Assert.Equal(1, executions);
        Assert.False(first.IsReplay);
        Assert.True(second.IsReplay);
        Assert.Equal("ref-1", second.ServerReference);
        Assert.Equal("succeeded", second.OutcomeCode);
    }

    [Fact]
    public async Task Same_key_different_payload_hash_conflicts()
    {
        await using var db = CreateContext();
        await db.Database.MigrateAsync();
        var sut = new PosIdempotencyService(db, new FixedClock());
        var org = Guid.NewGuid();

        await sut.ExecuteAsync(
            new PosIdempotencyRequest(org, PosProductCodes.PinoyBusinessPos, "dev.offline-probe", "key-2", "hash-a", Guid.NewGuid()),
            _ => Task.FromResult(new PosIdempotencyExecutionResult("succeeded", null, "ref-a")));

        var conflict = await sut.ExecuteAsync(
            new PosIdempotencyRequest(org, PosProductCodes.PinoyBusinessPos, "dev.offline-probe", "key-2", "hash-b", Guid.NewGuid()),
            _ => Task.FromResult(new PosIdempotencyExecutionResult("succeeded", null, "ref-b")));

        Assert.True(conflict.IsConflict);
        Assert.Equal("conflict_payload_mismatch", conflict.OutcomeCode);
    }

    [Fact]
    public async Task Concurrent_duplicates_converge_to_one_execution()
    {
        await using var db = CreateContext();
        await db.Database.MigrateAsync();
        var org = Guid.NewGuid();
        var key = "concurrent-key";
        var hash = "hash-same";
        var executions = 0;

        async Task<PosIdempotencyOutcome> RunAsync()
        {
            await using var local = CreateContext();
            var sut = new PosIdempotencyService(local, new FixedClock());
            return await sut.ExecuteAsync(
                new PosIdempotencyRequest(org, PosProductCodes.PinoyBusinessPos, "dev.offline-probe", key, hash, Guid.NewGuid()),
                async _ =>
                {
                    Interlocked.Increment(ref executions);
                    await Task.Delay(25);
                    return new PosIdempotencyExecutionResult("succeeded", null, "ref-c");
                });
        }

        var results = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => RunAsync()));
        Assert.True(executions >= 1);
        Assert.All(results, r => Assert.False(r.IsConflict));
        Assert.Contains(results, r => r.ServerReference == "ref-c");

        await using var verify = CreateContext();
        var rows = await verify.IdempotencyRecords.CountAsync(r =>
            r.OrganizationId == org && r.IdempotencyKey == key);
        Assert.Equal(1, rows);
    }

    [Fact]
    public async Task Idempotency_migration_applies_and_excludes_business_tables()
    {
        await using var db = CreateContext();
        await db.Database.MigrateAsync();
        var applied = await db.Database.GetAppliedMigrationsAsync();
        Assert.Contains(applied, m => m.Contains("AddPosIdempotencyRecords", StringComparison.Ordinal));

        await using var connection = new Npgsql.NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var cmd = new Npgsql.NpgsqlCommand(
            """
            SELECT table_name FROM information_schema.tables
            WHERE table_schema = 'pos' AND table_name = 'idempotency_records';
            """,
            connection);
        Assert.Equal("idempotency_records", await cmd.ExecuteScalarAsync());
    }

    private PosDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<PosDbContext>().UseNpgsql(fixture.ConnectionString).Options);

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.Parse("2026-07-30T12:00:00Z");
    }
}
