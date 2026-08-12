using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace ExItS.Platform.Infrastructure.Persistence.Repositories;

internal sealed class PlatformUnitOfWork : IPlatformUnitOfWork
{
    private readonly PlatformDbContext _db;

    public PlatformUnitOfWork(PlatformDbContext db) => _db = db;

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.ConcurrencyConflict,
                "A concurrency conflict occurred while saving changes.");
        }
        catch (DbUpdateException ex) when (PersistenceExceptionMapper.TryMapUniqueViolation(ex, out var errorCode, out var message))
        {
            throw new PersistenceConflictException(errorCode, message);
        }
    }

    public async Task ExecuteWithOrganizationLockAsync(
        Guid organizationId,
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (organizationId == Guid.Empty)
        {
            await action(cancellationToken).ConfigureAwait(false);
            return;
        }

        var provider = _db.Database.ProviderName ?? string.Empty;
        if (!provider.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
        {
            await action(cancellationToken).ConfigureAwait(false);
            return;
        }

        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database
                .BeginTransactionAsync(cancellationToken)
                .ConfigureAwait(false);

            // Two-key advisory lock derived from organization Guid (transaction-scoped).
            var bytes = organizationId.ToByteArray();
            var key1 = BitConverter.ToInt32(bytes, 0);
            var key2 = BitConverter.ToInt32(bytes, 4);
            await _db.Database
                .ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({key1}, {key2})", cancellationToken)
                .ConfigureAwait(false);

            await action(cancellationToken).ConfigureAwait(false);
            await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }
}
