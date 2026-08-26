using System.Net;

namespace ExItS.Platform.Application.Identity;

/// <summary>
/// Builds auth outbound email subject/body. Callers must never place passwords, invitation tokens
/// (after acceptance), or session/access secrets into <see cref="PlatformAuthOutboundMessage"/>.
/// </summary>
public static class PlatformAuthOutboundEmailComposer
{
    public static (string Subject, string HtmlBody) Compose(
        PlatformAuthOutboundMessage message,
        string adminPublicBaseUrl,
        string? pinoyLoanManagerPublicBaseUrl = null,
        bool allowHttpLoopbackPublicUrls = false,
        string? linkGuidanceHtml = null)
    {
        var baseUrl = adminPublicBaseUrl.TrimEnd('/');
        var encodedToken = WebUtility.UrlEncode(message.OpaqueToken ?? string.Empty);
        var guidance = AppendGuidance(linkGuidanceHtml);

        return message.Kind switch
        {
            PlatformAuthOutboundMessageKinds.EmailVerification => (
                "Verify your ExItS account",
                ComposeTokenLinkBody(
                    message,
                    adminPublicBaseUrl,
                    pinoyLoanManagerPublicBaseUrl,
                    allowHttpLoopbackPublicUrls,
                    """
                    <p>Welcome to ExItS.</p>
                    <p>Confirm your email and create your password to activate your account.</p>
                    """,
                    "Activate your account") + guidance),
            PlatformAuthOutboundMessageKinds.OrganizationStaffInvitation =>
                ComposeStaffInvitation(message, baseUrl, encodedToken, guidance),
            PlatformAuthOutboundMessageKinds.OrganizationStaffInvitationAccepted =>
                ComposeStaffInvitationAccepted(message),
            PlatformAuthOutboundMessageKinds.PasswordReset => (
                "Reset your ExItS password",
                ComposeTokenLinkBody(
                    message,
                    adminPublicBaseUrl,
                    pinoyLoanManagerPublicBaseUrl,
                    allowHttpLoopbackPublicUrls,
                    "<p>A password reset was requested for your ExItS account.</p>",
                    "Reset password") + guidance),
            PlatformAuthOutboundMessageKinds.RecoveryEmailVerification => (
                "Confirm your ExItS recovery email",
                $"""
                 <p>Confirm your recovery email for ExItS.</p>
                 <p><a href="{baseUrl}/admin/confirm-recovery-email?token={encodedToken}">Confirm recovery email</a></p>
                 <p>This link expires at {message.ExpiresAtUtc:u} (UTC).</p>
                 {guidance}
                 """),
            _ => (
                "ExItS account message",
                $"<p>ExItS account message ({WebUtility.HtmlEncode(message.Kind)}).</p><p>Token expires at {message.ExpiresAtUtc:u} (UTC).</p>{guidance}")
        };
    }

    private static string AppendGuidance(string? linkGuidanceHtml)
    {
        if (string.IsNullOrWhiteSpace(linkGuidanceHtml))
        {
            return string.Empty;
        }

        return $"<p><em>{linkGuidanceHtml.Trim()}</em></p>";
    }

    private static string ComposeTokenLinkBody(
        PlatformAuthOutboundMessage message,
        string adminPublicBaseUrl,
        string? pinoyLoanManagerPublicBaseUrl,
        bool allowHttpLoopbackPublicUrls,
        string introHtml,
        string linkText)
    {
        if (!PlatformAuthCallbackResolver.TryCreateLink(
                message,
                adminPublicBaseUrl,
                pinoyLoanManagerPublicBaseUrl,
                allowHttpLoopbackPublicUrls,
                out var href))
        {
            return $"""
                    {introHtml}
                    <p>This message could not include an activation or reset link because the public origin is not configured.</p>
                    <p>This link expires at {message.ExpiresAtUtc:u} (UTC).</p>
                    <p>If you did not expect this message, you can ignore it.</p>
                    """;
        }

        var encodedHref = WebUtility.HtmlEncode(href);
        return $"""
                {introHtml}
                <p><a href="{encodedHref}">{linkText}</a></p>
                <p>This link expires at {message.ExpiresAtUtc:u} (UTC).</p>
                <p>If you did not expect this message, you can ignore it.</p>
                """;
    }

    private static (string Subject, string HtmlBody) ComposeStaffInvitation(
        PlatformAuthOutboundMessage message,
        string baseUrl,
        string encodedToken,
        string guidance)
    {
        var orgName = WebUtility.HtmlEncode(
            string.IsNullOrWhiteSpace(message.OrganizationName) ? "an organization" : message.OrganizationName);
        var contactEmail = WebUtility.HtmlEncode(
            string.IsNullOrWhiteSpace(message.ContactEmail) ? message.Email : message.ContactEmail);
        var role = string.IsNullOrWhiteSpace(message.RoleDisplay)
            ? null
            : WebUtility.HtmlEncode(message.RoleDisplay);

        var roleLine = role is null
            ? string.Empty
            : $"<p><strong>Role:</strong> {role}</p>";

        return (
            $"You are invited to join {orgName} on ExItS",
            $"""
             <p>You have been invited to join <strong>{orgName}</strong> as organization staff on ExItS.</p>
             <p><strong>Contact email:</strong> {contactEmail}</p>
             {roleLine}
             <p>Your organization-specific staff username will be created when you accept this invitation.</p>
             <p>The contact email above is for invitation and recovery only — it is not your staff login.</p>
             <p><a href="{baseUrl}/admin/accept-organization-invitation?token={encodedToken}">Accept invitation</a></p>
             <p>This link expires at {message.ExpiresAtUtc:u} (UTC).</p>
             {guidance}
             """);
    }

    private static (string Subject, string HtmlBody) ComposeStaffInvitationAccepted(
        PlatformAuthOutboundMessage message)
    {
        var orgName = WebUtility.HtmlEncode(
            string.IsNullOrWhiteSpace(message.OrganizationName) ? "your organization" : message.OrganizationName);
        var contactEmail = WebUtility.HtmlEncode(
            string.IsNullOrWhiteSpace(message.ContactEmail) ? message.Email : message.ContactEmail);
        var staffLogin = WebUtility.HtmlEncode(message.StaffLogin ?? string.Empty);

        return (
            $"Your ExItS staff login for {orgName}",
            $"""
             <p>Your staff account for <strong>{orgName}</strong> is ready.</p>
             <p><strong>Staff username (sign in with this):</strong> {staffLogin}</p>
             <p><strong>Contact / recovery email:</strong> {contactEmail}</p>
             <p>Use your staff username to sign in. Your contact email is for recovery only and is not your login.</p>
             """);
    }
}
