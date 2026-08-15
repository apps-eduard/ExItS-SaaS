using ExItS.PinoyBusinessPOS.Application.Privacy;

namespace ExItS.PinoyBusinessPOS.UnitTests.Privacy;

public sealed class OrganizationPrivacyReadinessProjectionTests
{
    [Fact]
    public void Projection_defaults_legal_and_npc_to_not_verified()
    {
        Assert.Equal("NotVerified", OrganizationPrivacyReadinessStatuses.NotVerified);
        Assert.DoesNotContain("Compliant", OrganizationPrivacyReadinessStatuses.NotVerified, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Technical_safeguards_are_platform_managed_implemented_codes()
    {
        // Guard the published codes remain intentional (no marketing compliance claims).
        var codes = new[]
        {
            "ORG_ISOLATION", "RBAC", "AUTH_SESSION", "PERSONAL_ORG_BOUNDARY", "DEVICE_SECRETS", "SUPPLIER_SCOPE"
        };
        Assert.Equal(6, codes.Length);
        Assert.All(codes, c => Assert.DoesNotContain("COMPLIANT", c, StringComparison.OrdinalIgnoreCase));
    }
}
