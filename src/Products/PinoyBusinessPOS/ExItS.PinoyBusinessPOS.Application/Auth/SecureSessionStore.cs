using System.Globalization;
using ExItS.PinoyBusinessPOS.Application.Abstractions;

namespace ExItS.PinoyBusinessPOS.Application.Auth;

public sealed class SecureSessionStore(ISecureTokenStore tokens) : ISessionStore
{
    public async Task SaveAsync(AuthSession session, string sessionMarker, CancellationToken ct = default)
    {
        await tokens.SetAsync(SecureTokenKeys.UserId, session.UserId.ToString("D"), ct).ConfigureAwait(false);
        await tokens.SetAsync(SecureTokenKeys.SessionMarker, sessionMarker, ct).ConfigureAwait(false);
        await tokens.SetAsync(SecureTokenKeys.IssuedAtUtc, session.IssuedAtUtc.UtcDateTime.ToString("O", CultureInfo.InvariantCulture), ct).ConfigureAwait(false);
        await tokens.SetAsync(SecureTokenKeys.ExpiresAtUtc, session.ExpiresAtUtc.UtcDateTime.ToString("O", CultureInfo.InvariantCulture), ct).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(session.AccessToken))
        {
            await tokens.ClearAsync(SecureTokenKeys.AccessToken, ct).ConfigureAwait(false);
        }
        else
        {
            await tokens.SetAsync(SecureTokenKeys.AccessToken, session.AccessToken, ct).ConfigureAwait(false);
        }

        if (string.IsNullOrWhiteSpace(session.PlatformSessionToken))
        {
            await tokens.ClearAsync(SecureTokenKeys.PlatformSessionToken, ct).ConfigureAwait(false);
        }
        else
        {
            await tokens.SetAsync(SecureTokenKeys.PlatformSessionToken, session.PlatformSessionToken, ct).ConfigureAwait(false);
        }

        if (string.IsNullOrWhiteSpace(session.SubscriptionStatus))
        {
            await tokens.ClearAsync(SecureTokenKeys.SubscriptionStatus, ct).ConfigureAwait(false);
        }
        else
        {
            await tokens.SetAsync(SecureTokenKeys.SubscriptionStatus, session.SubscriptionStatus.Trim(), ct).ConfigureAwait(false);
        }

        if (session.EnabledFeatureCodes is null || session.EnabledFeatureCodes.Count == 0)
        {
            await tokens.ClearAsync(SecureTokenKeys.FeatureGrants, ct).ConfigureAwait(false);
        }
        else
        {
            var grants = string.Join(',', session.EnabledFeatureCodes.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim()));
            if (string.IsNullOrWhiteSpace(grants))
            {
                await tokens.ClearAsync(SecureTokenKeys.FeatureGrants, ct).ConfigureAwait(false);
            }
            else
            {
                await tokens.SetAsync(SecureTokenKeys.FeatureGrants, grants, ct).ConfigureAwait(false);
            }
        }

        if (string.IsNullOrWhiteSpace(session.AccountClass))
        {
            await tokens.ClearAsync(SecureTokenKeys.AccountClass, ct).ConfigureAwait(false);
        }
        else
        {
            await tokens.SetAsync(SecureTokenKeys.AccountClass, session.AccountClass.Trim(), ct).ConfigureAwait(false);
        }

        if (session.AccountProfileId is Guid profileId && profileId != Guid.Empty)
        {
            await tokens.SetAsync(SecureTokenKeys.AccountProfileId, profileId.ToString("D"), ct).ConfigureAwait(false);
        }
        else
        {
            await tokens.ClearAsync(SecureTokenKeys.AccountProfileId, ct).ConfigureAwait(false);
        }

        if (session.OrganizationContextLocked)
        {
            await tokens.SetAsync(SecureTokenKeys.OrganizationContextLocked, "true", ct).ConfigureAwait(false);
        }
        else
        {
            await tokens.ClearAsync(SecureTokenKeys.OrganizationContextLocked, ct).ConfigureAwait(false);
        }

        if (session.BranchId is Guid branchId && branchId != Guid.Empty)
        {
            await tokens.SetAsync(SecureTokenKeys.BranchId, branchId.ToString("D"), ct).ConfigureAwait(false);
        }
        else
        {
            await tokens.ClearAsync(SecureTokenKeys.BranchId, ct).ConfigureAwait(false);
        }

        if (session.PosDeviceId is Guid posDeviceId && posDeviceId != Guid.Empty)
        {
            await tokens.SetAsync(SecureTokenKeys.PosDeviceId, posDeviceId.ToString("D"), ct).ConfigureAwait(false);
        }
        else
        {
            await tokens.ClearAsync(SecureTokenKeys.PosDeviceId, ct).ConfigureAwait(false);
        }

        if (session.SelectedBranchId is Guid selectedBranchId && selectedBranchId != Guid.Empty)
        {
            await tokens.SetAsync(SecureTokenKeys.SelectedBranchId, selectedBranchId.ToString("D"), ct).ConfigureAwait(false);
        }
        else
        {
            await tokens.ClearAsync(SecureTokenKeys.SelectedBranchId, ct).ConfigureAwait(false);
        }
    }

    public async Task<(AuthSession? Session, string? Marker)> LoadAsync(CancellationToken ct = default)
    {
        try
        {
            var userIdText = await tokens.GetAsync(SecureTokenKeys.UserId, ct).ConfigureAwait(false);
            var marker = await tokens.GetAsync(SecureTokenKeys.SessionMarker, ct).ConfigureAwait(false);
            var issuedText = await tokens.GetAsync(SecureTokenKeys.IssuedAtUtc, ct).ConfigureAwait(false);
            var expiresText = await tokens.GetAsync(SecureTokenKeys.ExpiresAtUtc, ct).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(userIdText)
                || string.IsNullOrWhiteSpace(marker)
                || !Guid.TryParse(userIdText, out var userId)
                || !DateTimeOffset.TryParse(issuedText, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var issued)
                || !DateTimeOffset.TryParse(expiresText, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var expires))
            {
                return (null, null);
            }

            var accessToken = await tokens.GetAsync(SecureTokenKeys.AccessToken, ct).ConfigureAwait(false);
            var platformSessionToken = await tokens.GetAsync(SecureTokenKeys.PlatformSessionToken, ct).ConfigureAwait(false);
            var subscriptionStatus = await tokens.GetAsync(SecureTokenKeys.SubscriptionStatus, ct).ConfigureAwait(false);
            var grantsText = await tokens.GetAsync(SecureTokenKeys.FeatureGrants, ct).ConfigureAwait(false);
            var accountClass = await tokens.GetAsync(SecureTokenKeys.AccountClass, ct).ConfigureAwait(false);
            var accountProfileText = await tokens.GetAsync(SecureTokenKeys.AccountProfileId, ct).ConfigureAwait(false);
            var orgLockedText = await tokens.GetAsync(SecureTokenKeys.OrganizationContextLocked, ct).ConfigureAwait(false);
            IReadOnlyList<string>? grants = null;
            if (!string.IsNullOrWhiteSpace(grantsText))
            {
                grants = grantsText
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }

            Guid? accountProfileId = null;
            if (Guid.TryParse(accountProfileText, out var parsedProfile) && parsedProfile != Guid.Empty)
            {
                accountProfileId = parsedProfile;
            }

            var organizationContextLocked = string.Equals(orgLockedText, "true", StringComparison.OrdinalIgnoreCase);

            Guid? branchId = null;
            var branchText = await tokens.GetAsync(SecureTokenKeys.BranchId, ct).ConfigureAwait(false);
            if (Guid.TryParse(branchText, out var parsedBranch) && parsedBranch != Guid.Empty)
            {
                branchId = parsedBranch;
            }

            Guid? posDeviceId = null;
            var posDeviceText = await tokens.GetAsync(SecureTokenKeys.PosDeviceId, ct).ConfigureAwait(false);
            if (Guid.TryParse(posDeviceText, out var parsedDevice) && parsedDevice != Guid.Empty)
            {
                posDeviceId = parsedDevice;
            }

            Guid? selectedBranchId = null;
            var selectedBranchText = await tokens.GetAsync(SecureTokenKeys.SelectedBranchId, ct).ConfigureAwait(false);
            if (Guid.TryParse(selectedBranchText, out var parsedSelected) && parsedSelected != Guid.Empty)
            {
                selectedBranchId = parsedSelected;
            }

            // Partial session shell — display fields filled by AuthenticationService after restore.
            var shell = new AuthSession(
                userId,
                DisplayName: string.Empty,
                Username: string.Empty,
                Email: string.Empty,
                OrganizationId: null,
                OrganizationDisplayName: null,
                IssuedAtUtc: issued,
                ExpiresAtUtc: expires,
                HasPosAccess: false,
                AccessReasonCode: null,
                SubscriptionStatus: string.IsNullOrWhiteSpace(subscriptionStatus) ? null : subscriptionStatus.Trim(),
                EnabledFeatureCodes: grants,
                AccessToken: string.IsNullOrWhiteSpace(accessToken) ? null : accessToken,
                PlatformSessionToken: string.IsNullOrWhiteSpace(platformSessionToken) ? null : platformSessionToken,
                AccountClass: string.IsNullOrWhiteSpace(accountClass) ? null : accountClass.Trim(),
                AccountProfileId: accountProfileId,
                OrganizationContextLocked: organizationContextLocked,
                BranchId: branchId,
                PosDeviceId: posDeviceId,
                SelectedBranchId: selectedBranchId);

            return (shell, marker);
        }
        catch
        {
            return (null, null);
        }
    }

    public Task ClearAsync(CancellationToken ct = default) => tokens.ClearAllSessionKeysAsync(ct);
}
