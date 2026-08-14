using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Infrastructure.Persistence.Organizations;
using Microsoft.EntityFrameworkCore;

namespace ExItS.Platform.Infrastructure.Persistence.Repositories;

internal sealed class OrganizationOwnershipTransferRepository : IOrganizationOwnershipTransferRepository
{
    private readonly PlatformDbContext _db;

    public OrganizationOwnershipTransferRepository(PlatformDbContext db) => _db = db;

    public async Task<OrganizationOwnershipTransfer?> GetByIdAsync(
        OrganizationOwnershipTransferId id,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.OrganizationOwnershipTransfers.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id.Value, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public async Task<OrganizationOwnershipTransfer?> FindPendingByOrganizationAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var pending = nameof(OrganizationOwnershipTransferStatus.Pending);
        var record = await _db.OrganizationOwnershipTransfers.AsNoTracking()
            .FirstOrDefaultAsync(
                t => t.OrganizationId == organizationId.Value && t.Status == pending,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public async Task<IReadOnlyList<OrganizationOwnershipTransfer>> ListPendingByRecipientAsync(
        PlatformUserId toUserId,
        CancellationToken cancellationToken = default)
    {
        var pending = nameof(OrganizationOwnershipTransferStatus.Pending);
        var records = await _db.OrganizationOwnershipTransfers.AsNoTracking()
            .Where(t => t.ToUserId == toUserId.Value && t.Status == pending)
            .OrderByDescending(t => t.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(ToDomain).ToList();
    }

    public Task AddAsync(OrganizationOwnershipTransfer transfer, CancellationToken cancellationToken = default)
    {
        _db.OrganizationOwnershipTransfers.Add(ToRecord(transfer));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(OrganizationOwnershipTransfer transfer, CancellationToken cancellationToken = default)
    {
        var record = await _db.OrganizationOwnershipTransfers
            .FirstOrDefaultAsync(t => t.Id == transfer.Id.Value, cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            throw new InvalidOperationException(
                $"Organization ownership transfer '{transfer.Id}' was not found for update.");
        }

        Apply(transfer, record);
    }

    private static OrganizationOwnershipTransfer ToDomain(OrganizationOwnershipTransferRecord record) =>
        OrganizationOwnershipTransfer.Rehydrate(
            OrganizationOwnershipTransferId.From(record.Id),
            PlatformOrganizationId.From(record.OrganizationId),
            PlatformUserId.From(record.FromOwnerUserId),
            PlatformUserId.From(record.ToUserId),
            Enum.Parse<OrganizationOwnershipTransferStatus>(record.Status),
            record.CreatedAtUtc,
            record.ExpiresAtUtc,
            record.AcceptedAtUtc,
            record.DeclinedAtUtc,
            record.CancelledAtUtc,
            record.CompletedAtUtc,
            record.UpdatedAtUtc);

    private static OrganizationOwnershipTransferRecord ToRecord(OrganizationOwnershipTransfer transfer) =>
        new()
        {
            Id = transfer.Id.Value,
            OrganizationId = transfer.OrganizationId.Value,
            FromOwnerUserId = transfer.FromOwnerUserId.Value,
            ToUserId = transfer.ToUserId.Value,
            Status = transfer.Status.ToString(),
            CreatedAtUtc = transfer.CreatedAtUtc,
            ExpiresAtUtc = transfer.ExpiresAtUtc,
            AcceptedAtUtc = transfer.AcceptedAtUtc,
            DeclinedAtUtc = transfer.DeclinedAtUtc,
            CancelledAtUtc = transfer.CancelledAtUtc,
            CompletedAtUtc = transfer.CompletedAtUtc,
            UpdatedAtUtc = transfer.UpdatedAtUtc
        };

    private static void Apply(OrganizationOwnershipTransfer transfer, OrganizationOwnershipTransferRecord record)
    {
        record.Status = transfer.Status.ToString();
        record.ExpiresAtUtc = transfer.ExpiresAtUtc;
        record.AcceptedAtUtc = transfer.AcceptedAtUtc;
        record.DeclinedAtUtc = transfer.DeclinedAtUtc;
        record.CancelledAtUtc = transfer.CancelledAtUtc;
        record.CompletedAtUtc = transfer.CompletedAtUtc;
        record.UpdatedAtUtc = transfer.UpdatedAtUtc;
    }
}
