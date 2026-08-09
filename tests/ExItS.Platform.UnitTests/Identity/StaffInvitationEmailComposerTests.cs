using ExItS.Platform.Application.Identity;

namespace ExItS.Platform.UnitTests.Identity;

public sealed class StaffInvitationEmailComposerTests
{
    [Fact]
    public void Invitation_email_distinguishes_contact_email_from_future_staff_login()
    {
        var message = new PlatformAuthOutboundMessage(
            PlatformAuthOutboundMessageKinds.OrganizationStaffInvitation,
            Guid.Empty,
            "maria@gmail.com",
            "invite-token-abc",
            DateTimeOffset.Parse("2026-08-10T12:00:00Z"),
            OrganizationName: "ABC Sari-Sari Store",
            RoleDisplay: "Staff",
            ContactEmail: "maria@gmail.com");

        var (subject, body) = PlatformAuthOutboundEmailComposer.Compose(message, "https://admin.example");

        Assert.Contains("ABC Sari-Sari Store", subject, StringComparison.Ordinal);
        Assert.Contains("maria@gmail.com", body, StringComparison.Ordinal);
        Assert.Contains("Staff", body, StringComparison.Ordinal);
        Assert.Contains(
            "Your organization-specific staff username will be created when you accept this invitation.",
            body,
            StringComparison.Ordinal);
        Assert.Contains("not your staff login", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("accept-organization-invitation?token=invite-token-abc", body, StringComparison.Ordinal);
        Assert.DoesNotContain("maria@ORG", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Acceptance_email_includes_actual_staff_login_without_secrets()
    {
        var message = new PlatformAuthOutboundMessage(
            PlatformAuthOutboundMessageKinds.OrganizationStaffInvitationAccepted,
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "maria@gmail.com",
            OpaqueToken: string.Empty,
            ExpiresAtUtc: DateTimeOffset.Parse("2026-08-10T12:00:00Z"),
            OrganizationName: "ABC Sari-Sari Store",
            RoleDisplay: "Staff",
            ContactEmail: "maria@gmail.com",
            StaffLogin: "maria2@ORG001842");

        var (_, body) = PlatformAuthOutboundEmailComposer.Compose(message, "https://admin.example");

        Assert.Contains("maria2@ORG001842", body, StringComparison.Ordinal);
        Assert.Contains("ABC Sari-Sari Store", body, StringComparison.Ordinal);
        Assert.Contains("maria@gmail.com", body, StringComparison.Ordinal);
        Assert.Contains("Staff username", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token=", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Bearer", body, StringComparison.Ordinal);
        Assert.DoesNotContain("refresh", body, StringComparison.OrdinalIgnoreCase);
    }
}
