using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExItS.Platform.Infrastructure.Identity;

internal sealed class EfPublicUserIdGenerator(PlatformDbContext db) : IPublicUserIdGenerator
{
    private const int MaxAttempts = 32;

    public async Task<string> GenerateUniqueAsync(CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var candidate = PublicUserIdRules.GenerateRandom();
            var exists = await db.PlatformUsers.AsNoTracking()
                .AnyAsync(u => u.PublicUserId == candidate, cancellationToken)
                .ConfigureAwait(false);
            if (!exists)
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("Unable to allocate a unique ExItS public user ID.");
    }
}
