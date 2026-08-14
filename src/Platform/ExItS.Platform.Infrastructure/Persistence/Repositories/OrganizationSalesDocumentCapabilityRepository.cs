using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Organizations;
using Microsoft.EntityFrameworkCore;

namespace ExItS.Platform.Infrastructure.Persistence.Repositories;

internal sealed class OrganizationSalesDocumentCapabilityRepository(PlatformDbContext db)
    : IOrganizationSalesDocumentCapabilityRepository
{
    public async Task<OrganizationSalesDocumentCapability?> GetByOrganizationIdAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var record = await db.OrganizationSalesDocumentCapabilities
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrganizationId == organizationId.Value, cancellationToken)
            .ConfigureAwait(false);

        return record is null
            ? null
            : OrganizationSalesDocumentCapability.Rehydrate(
                PlatformOrganizationId.From(record.OrganizationId),
                record.TaxDocumentIssuanceEnabled,
                record.UpdatedAtUtc,
                record.UpdatedByActorReference);
    }

    public Task AddAsync(
        OrganizationSalesDocumentCapability capability,
        CancellationToken cancellationToken = default)
    {
        db.OrganizationSalesDocumentCapabilities.Add(new OrganizationSalesDocumentCapabilityRecord
        {
            OrganizationId = capability.OrganizationId.Value,
            TaxDocumentIssuanceEnabled = capability.TaxDocumentIssuanceEnabled,
            UpdatedAtUtc = capability.UpdatedAtUtc,
            UpdatedByActorReference = capability.UpdatedByActorReference
        });
        return Task.CompletedTask;
    }
}
