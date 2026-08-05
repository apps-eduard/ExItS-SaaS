namespace ExItS.PinoyBusinessPOS.Maui.Services;

/// <summary>
/// Lets organization-select (and similar AuthShell pages) drive the dual top-bar identity
/// without cascading upward from page body into the layout.
/// </summary>
public sealed class AuthShellIdentityState
{
    public Guid? OrganizationId { get; private set; }
    public string? OrganizationName { get; private set; }
    public string? MembershipRoleHint { get; private set; }

    public event Func<Task>? Changed;

    public void SetOrganizationPreview(Guid? organizationId, string? displayName, string? membershipRole)
    {
        OrganizationId = organizationId;
        OrganizationName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
        MembershipRoleHint = string.IsNullOrWhiteSpace(membershipRole) ? null : membershipRole.Trim();
        _ = NotifyAsync();
    }

    public void Clear()
    {
        OrganizationId = null;
        OrganizationName = null;
        MembershipRoleHint = null;
        _ = NotifyAsync();
    }

    private async Task NotifyAsync()
    {
        var handlers = Changed;
        if (handlers is null)
        {
            return;
        }

        foreach (var handler in handlers.GetInvocationList().Cast<Func<Task>>())
        {
            await handler().ConfigureAwait(false);
        }
    }
}
