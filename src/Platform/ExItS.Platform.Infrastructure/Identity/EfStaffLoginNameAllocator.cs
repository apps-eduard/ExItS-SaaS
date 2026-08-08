using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExItS.Platform.Infrastructure.Identity;

internal sealed class EfStaffLoginNameAllocator(PlatformDbContext db) : IStaffLoginNameAllocator
{
    public async Task<string> AllocateAsync(
        string contactEmail,
        string publicOrganizationId,
        CancellationToken cancellationToken = default)
    {
        var local = StaffLoginNameRules.NormalizeLocalPartFromEmail(contactEmail);
        for (var suffix = 0; suffix < 10_000; suffix++)
        {
            var candidate = StaffLoginNameRules.Build(local, publicOrganizationId, suffix);
            var normalized = PlatformUser.NormalizeEmail(candidate);
            var exists = await db.PlatformUsers.AsNoTracking()
                .AnyAsync(u => u.NormalizedEmail == normalized, cancellationToken)
                .ConfigureAwait(false);
            if (!exists)
            {
                return StaffLoginNameRules.FormatForDisplay(normalized);
            }
        }

        throw new InvalidOperationException("Unable to allocate a unique staff login name.");
    }
}
