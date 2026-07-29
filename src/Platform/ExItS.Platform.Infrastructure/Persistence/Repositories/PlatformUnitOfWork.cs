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
        catch (DbUpdateException ex) when (PersistenceExceptionMapper.TryMapUniqueViolation(ex, out var errorCode, out var message))
        {
            throw new PersistenceConflictException(errorCode, message);
        }
    }
}
