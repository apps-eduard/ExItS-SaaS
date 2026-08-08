using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.UnitTests.Support;

namespace ExItS.Platform.UnitTests.TestSupport;

internal sealed class FakeStaffLoginNameAllocator(InMemoryPlatformUserRepository users) : IStaffLoginNameAllocator
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
            if (await users.GetByNormalizedEmailAsync(normalized, cancellationToken).ConfigureAwait(false) is null)
            {
                return StaffLoginNameRules.FormatForDisplay(normalized);
            }
        }

        throw new InvalidOperationException("Unable to allocate a unique staff login name.");
    }
}
