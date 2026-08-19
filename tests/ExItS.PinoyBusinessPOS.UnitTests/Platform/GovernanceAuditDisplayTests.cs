using ExItS.PinoyBusinessPOS.Application.Platform;

namespace ExItS.PinoyBusinessPOS.UnitTests.Platform;

public sealed class GovernanceAuditDisplayTests
{
    [Fact]
    public void FormatMobileRow_uses_summary_when_present()
    {
        var utc = new DateTimeOffset(2026, 8, 19, 7, 42, 0, TimeSpan.Zero);
        var row = new PlatformGovernanceAuditRecordDto(
            Guid.NewGuid(),
            utc,
            "platform-user:abc",
            "PlatformUser",
            "platform.organization.branch.hours_updated",
            "OrganizationBranch",
            Guid.NewGuid().ToString("D"),
            Guid.NewGuid(),
            "Succeeded",
            null,
            "Maria changed Branch A hours");

        var formatted = GovernanceAuditDisplay.FormatMobileRow(row);
        Assert.StartsWith("Maria changed Branch A hours", formatted, StringComparison.Ordinal);
        Assert.Contains("·", formatted, StringComparison.Ordinal);
    }

    [Fact]
    public void FormatWebAction_maps_known_branch_codes()
    {
        Assert.Equal(
            "Branch created",
            GovernanceAuditDisplay.FormatWebAction("platform.organization.branch.created"));
    }
}
