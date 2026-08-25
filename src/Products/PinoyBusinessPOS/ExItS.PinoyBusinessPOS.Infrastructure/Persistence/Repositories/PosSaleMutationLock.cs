using ExItS.PinoyBusinessPOS.Application.Sales;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Sales;
using Microsoft.EntityFrameworkCore;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;

/// <summary>
/// PostgreSQL <c>pg_advisory_xact_lock</c> keyed by organization + sale, namespaced so it does not
/// collide with sale/return sequence locks, inventory reservation, or shift sequence locks.
/// </summary>
internal sealed class PosSaleMutationLock : ISaleMutationLock
{
    private const string LockSql = "SELECT pg_advisory_xact_lock({0})";

    /// <summary>ASCII "SALERET" packed into a 64-bit namespace XOR.</summary>
    private const ulong NamespaceXor = 0x53414C45524554UL;

    private readonly PosDbContext _db;

    public PosSaleMutationLock(PosDbContext db) => _db = db;

    public Task AcquireAsync(
        PosOrganizationId organizationId,
        SaleId saleId,
        CancellationToken cancellationToken = default) =>
        _db.Database.ExecuteSqlRawAsync(
            LockSql,
            [MutationLockKey(organizationId, saleId)],
            cancellationToken);

    private static long MutationLockKey(PosOrganizationId organizationId, SaleId saleId)
    {
        Span<byte> bytes = stackalloc byte[32];
        organizationId.Value.TryWriteBytes(bytes[..16]);
        saleId.Value.TryWriteBytes(bytes[16..]);

        unchecked
        {
            var hash = 0xcbf29ce484222325UL;
            foreach (var b in bytes)
            {
                hash = (hash ^ b) * 0x100000001b3UL;
            }

            return (long)(hash ^ NamespaceXor);
        }
    }
}
