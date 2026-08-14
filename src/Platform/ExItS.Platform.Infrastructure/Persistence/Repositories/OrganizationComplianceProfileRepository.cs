using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Organizations;
using Microsoft.EntityFrameworkCore;

namespace ExItS.Platform.Infrastructure.Persistence.Repositories;

internal sealed class OrganizationComplianceProfileRepository(PlatformDbContext db)
    : IOrganizationComplianceProfileRepository
{
    public async Task<OrganizationComplianceProfile?> GetByOrganizationIdAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var record = await db.OrganizationComplianceProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrganizationId == organizationId.Value, cancellationToken)
            .ConfigureAwait(false);

        return record is null
            ? null
            : OrganizationComplianceProfile.Rehydrate(
                PlatformOrganizationId.From(record.OrganizationId),
                record.CreatedAtUtc,
                record.UpdatedAtUtc,
                record.UpdatedByActorReference);
    }

    public Task AddAsync(
        OrganizationComplianceProfile profile,
        CancellationToken cancellationToken = default)
    {
        db.OrganizationComplianceProfiles.Add(new OrganizationComplianceProfileRecord
        {
            OrganizationId = profile.OrganizationId.Value,
            CreatedAtUtc = profile.CreatedAtUtc,
            UpdatedAtUtc = profile.UpdatedAtUtc,
            UpdatedByActorReference = profile.UpdatedByActorReference
        });
        return Task.CompletedTask;
    }
}
