using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Organizations;
using Microsoft.EntityFrameworkCore;

namespace ExItS.Platform.Infrastructure.Persistence.Repositories;

internal sealed class OrganizationOnlineSupplierPaymentsCapabilityRepository(PlatformDbContext db)
    : IOrganizationOnlineSupplierPaymentsCapabilityRepository
{
    public async Task<OrganizationOnlineSupplierPaymentsCapability?> GetByOrganizationIdAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var record = await db.OrganizationOnlineSupplierPaymentsCapabilities
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrganizationId == organizationId.Value, cancellationToken)
            .ConfigureAwait(false);

        return record is null
            ? null
            : OrganizationOnlineSupplierPaymentsCapability.Rehydrate(
                PlatformOrganizationId.From(record.OrganizationId),
                record.Status,
                record.UpdatedAtUtc,
                record.UpdatedByActorReference,
                record.Reason);
    }

    public Task AddAsync(
        OrganizationOnlineSupplierPaymentsCapability capability,
        CancellationToken cancellationToken = default)
    {
        db.OrganizationOnlineSupplierPaymentsCapabilities.Add(
            new OrganizationOnlineSupplierPaymentsCapabilityRecord
            {
                OrganizationId = capability.OrganizationId.Value,
                Status = capability.Status,
                UpdatedAtUtc = capability.UpdatedAtUtc,
                UpdatedByActorReference = capability.UpdatedByActorReference,
                Reason = capability.Reason
            });
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(
        OrganizationOnlineSupplierPaymentsCapability capability,
        CancellationToken cancellationToken = default)
    {
        var record = await db.OrganizationOnlineSupplierPaymentsCapabilities
            .FirstOrDefaultAsync(x => x.OrganizationId == capability.OrganizationId.Value, cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            throw new InvalidOperationException(
                $"Online supplier payments capability for organization '{capability.OrganizationId.Value}' was not found.");
        }

        record.Status = capability.Status;
        record.UpdatedAtUtc = capability.UpdatedAtUtc;
        record.UpdatedByActorReference = capability.UpdatedByActorReference;
        record.Reason = capability.Reason;
    }
}
