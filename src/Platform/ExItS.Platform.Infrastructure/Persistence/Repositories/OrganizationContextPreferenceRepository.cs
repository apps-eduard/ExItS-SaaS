using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Infrastructure.Persistence.Identity;
using Microsoft.EntityFrameworkCore;

namespace ExItS.Platform.Infrastructure.Persistence.Repositories;

internal sealed class OrganizationContextPreferenceRepository(PlatformDbContext db)
    : IOrganizationContextPreferenceRepository
{
    public async Task<PlatformOrganizationId?> GetLastActiveOrganizationIdAsync(
        PlatformUserId userIdentityId,
        CancellationToken cancellationToken = default)
    {
        var id = await db.OrganizationContextPreferences.AsNoTracking()
            .Where(x => x.UserIdentityId == userIdentityId.Value)
            .Select(x => x.LastActiveOrganizationId)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        return id is null ? null : PlatformOrganizationId.From(id.Value);
    }

    public async Task UpsertLastActiveOrganizationAsync(
        PlatformUserId userIdentityId,
        PlatformOrganizationId? organizationId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default)
    {
        var record = await db.OrganizationContextPreferences
            .FirstOrDefaultAsync(x => x.UserIdentityId == userIdentityId.Value, cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            db.OrganizationContextPreferences.Add(new OrganizationContextPreferenceRecord
            {
                UserIdentityId = userIdentityId.Value,
                LastActiveOrganizationId = organizationId?.Value,
                UpdatedAtUtc = utcNow
            });
            return;
        }

        record.LastActiveOrganizationId = organizationId?.Value;
        record.UpdatedAtUtc = utcNow;
    }
}
