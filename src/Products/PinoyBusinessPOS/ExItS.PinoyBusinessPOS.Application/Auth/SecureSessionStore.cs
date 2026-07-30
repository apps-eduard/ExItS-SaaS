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

            var subscriptionStatus = await tokens.GetAsync(SecureTokenKeys.SubscriptionStatus, ct).ConfigureAwait(false);
            var grantsText = await tokens.GetAsync(SecureTokenKeys.FeatureGrants, ct).ConfigureAwait(false);
            IReadOnlyList<string>? grants = null;
            if (!string.IsNullOrWhiteSpace(grantsText))
            {
                grants = grantsText
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
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
                EnabledFeatureCodes: grants);

            return (shell, marker);
        }
        catch
        {
            return (null, null);
        }
    }

    public Task ClearAsync(CancellationToken ct = default) => tokens.ClearAllSessionKeysAsync(ct);
}
