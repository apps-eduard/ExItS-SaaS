using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExItS.Platform.Infrastructure.Organizations;

internal sealed class EfPublicOrganizationIdGenerator(PlatformDbContext db) : IPublicOrganizationIdGenerator
{
    private const int MaxAttempts = 64;

    public async Task<string> GenerateUniqueAsync(CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var candidate = PublicOrganizationIdRules.GenerateRandom();
            var exists = await db.Organizations.AsNoTracking()
                .AnyAsync(o => o.PublicOrganizationId == candidate, cancellationToken)
                .ConfigureAwait(false);
            if (!exists)
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("Unable to allocate a unique public organization ID.");
    }
}
