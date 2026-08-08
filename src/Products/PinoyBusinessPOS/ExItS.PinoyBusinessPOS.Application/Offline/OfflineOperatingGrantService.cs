using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Auth;
using Microsoft.Extensions.Options;

namespace ExItS.PinoyBusinessPOS.Application.Offline;

/// <summary>
/// Offline operate grant lifecycle. Online bind establishes/refreshes the grant; PIN only unlocks
/// an already-valid grant and never extends <see cref="OfflineOperatingGrant.ExpiresAtUtc"/>.
/// Supports Organization (POS) and Personal (Utang) scopes with mutual isolation.
/// </summary>
public sealed class OfflineOperatingGrantService(
    IOfflineOperatingGrantStore store,
    IDeviceIdentityProvider deviceIdentity,
    IOptions<OfflineOperatingGrantOptions> options,
    TimeProvider? timeProvider = null) : IOfflineOperatingGrantService
{
    private readonly TimeProvider _clock = timeProvider ?? TimeProvider.System;
    private readonly OfflineOperatingGrantOptions _options = options.Value;

    public bool IsUnlockedThisProcess { get; private set; }

    public OfflineOperatingGrant? ActiveUnlockedGrant { get; private set; }

    public async Task EstablishFromOnlineSessionAsync(
        AuthSession session,
        string deviceId,
        string? roleCode,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        OfflineOperatingGrant? grant = null;
        var now = _clock.GetUtcNow();
        var duration = TimeSpan.FromHours(Math.Clamp(_options.DurationHours, 1, 168));

        if (string.IsNullOrWhiteSpace(deviceId))
        {
            deviceId = await deviceIdentity.GetOrCreateDeviceIdAsync(ct).ConfigureAwait(false);
        }

        deviceId = deviceId.Trim();

        if (session.HasPosAccess && session.OrganizationId is Guid orgId)
        {
            // Organization / POS operate grant — requires org + POS access.
            grant = new OfflineOperatingGrant(
                SchemaVersion: OfflineOperatingGrant.CurrentSchemaVersion,
                UserId: session.UserId,
                OrganizationId: orgId,
                OrganizationDisplayName: session.OrganizationDisplayName ?? orgId.ToString("D"),
                DeviceId: deviceId,
                RoleCode: roleCode,
                EnabledFeatureCodes: session.EnabledFeatureCodes?.ToArray() ?? Array.Empty<string>(),
                SubscriptionStatus: session.SubscriptionStatus,
                DisplayName: session.DisplayName,
                Username: session.Username,
                Email: session.Email,
                IssuedAtUtc: now,
                LastOnlineValidatedAtUtc: now,
                ExpiresAtUtc: now.Add(duration),
                ScopeKind: OfflineGrantScopeKind.Organization);
        }
        else if (IsPersonalEligible(session))
        {
            // Personal Utang grant — never for staff/org-locked sessions with an organization bind.
            grant = new OfflineOperatingGrant(
                SchemaVersion: OfflineOperatingGrant.CurrentSchemaVersion,
                UserId: session.UserId,
                OrganizationId: null,
                OrganizationDisplayName: PersonalLocalScope.DisplayName,
                DeviceId: deviceId,
                RoleCode: null,
                EnabledFeatureCodes: Array.Empty<string>(),
                SubscriptionStatus: null,
                DisplayName: session.DisplayName,
                Username: session.Username,
                Email: session.Email,
                IssuedAtUtc: now,
                LastOnlineValidatedAtUtc: now,
                ExpiresAtUtc: now.Add(duration),
                ScopeKind: OfflineGrantScopeKind.Personal);
        }
        else
        {
            return;
        }

        await store.SaveGrantAsync(grant, ct).ConfigureAwait(false);

        // Online validation refreshes the grant window; do not require PIN again this process.
        IsUnlockedThisProcess = true;
        ActiveUnlockedGrant = grant;
    }

    public async Task ClearAsync(CancellationToken ct = default)
    {
        // Drop the operate grant on logout / explicit server denial. Keep the PIN verifier so the
        // same device user can reuse their PIN after the next online establish.
        await store.ClearGrantAsync(ct).ConfigureAwait(false);
        IsUnlockedThisProcess = false;
        ActiveUnlockedGrant = null;
    }

    public void LockThisProcess()
    {
        // Keep durable grant + PIN; only revoke process unlock so Lock ≠ Sign out.
        IsUnlockedThisProcess = false;
        ActiveUnlockedGrant = null;
    }

    public async Task<bool> HasPinConfiguredAsync(CancellationToken ct = default)
    {
        var verifier = await store.LoadPinVerifierAsync(ct).ConfigureAwait(false);
        return verifier is not null
               && !string.IsNullOrWhiteSpace(verifier.HashBase64)
               && !string.IsNullOrWhiteSpace(verifier.SaltBase64);
    }

    public async Task<OfflinePinSetupResult> SetPinAsync(string pin, CancellationToken ct = default)
    {
        if (!OfflinePinHasher.IsValidPinFormat(pin, _options.PinMinLength))
        {
            return new OfflinePinSetupResult(false, "Offline_PinInvalidFormat");
        }

        var grant = await LoadNormalizedGrantAsync(ct).ConfigureAwait(false);
        if (grant is null)
        {
            return new OfflinePinSetupResult(false, "Offline_GrantMissing");
        }

        var now = _clock.GetUtcNow();
        if (grant.IsExpired(now))
        {
            return new OfflinePinSetupResult(false, "Offline_GrantExpired");
        }

        var deviceId = await deviceIdentity.GetOrCreateDeviceIdAsync(ct).ConfigureAwait(false);
        if (!string.Equals(grant.DeviceId, deviceId, StringComparison.Ordinal))
        {
            return new OfflinePinSetupResult(false, "Offline_DeviceMismatch");
        }

        var verifier = OfflinePinHasher.Create(pin, Math.Max(10_000, _options.PinHashIterations));
        await store.SavePinVerifierAsync(verifier, ct).ConfigureAwait(false);
        return new OfflinePinSetupResult(true);
    }

    public async Task<OfflineColdStartOffer> EvaluateColdStartOfferAsync(CancellationToken ct = default)
    {
        var grant = await LoadNormalizedGrantAsync(ct).ConfigureAwait(false);
        if (grant is null)
        {
            return new OfflineColdStartOffer(false, null, "offline_grant_missing");
        }

        if (!grant.IsOrganizationScope && !grant.IsPersonalScope)
        {
            return new OfflineColdStartOffer(false, null, "offline_grant_invalid_scope");
        }

        var now = _clock.GetUtcNow();
        if (grant.IsExpired(now))
        {
            return new OfflineColdStartOffer(false, grant, "offline_grant_expired");
        }

        var deviceId = await deviceIdentity.GetOrCreateDeviceIdAsync(ct).ConfigureAwait(false);
        if (!string.Equals(grant.DeviceId, deviceId, StringComparison.Ordinal))
        {
            return new OfflineColdStartOffer(false, grant, "offline_device_mismatch");
        }

        if (!await HasPinConfiguredAsync(ct).ConfigureAwait(false))
        {
            return new OfflineColdStartOffer(false, grant, "offline_pin_not_configured");
        }

        return new OfflineColdStartOffer(true, grant, null);
    }

    public async Task<OfflinePinUnlockResult> UnlockWithPinAsync(string pin, CancellationToken ct = default)
    {
        var offer = await EvaluateColdStartOfferAsync(ct).ConfigureAwait(false);
        if (!offer.CanOfferPinUnlock || offer.Grant is null)
        {
            return offer.DenialReasonCode switch
            {
                "offline_grant_expired" => new OfflinePinUnlockResult(
                    OfflinePinUnlockStatus.GrantExpired, offer.Grant, "Offline_GrantExpired"),
                "offline_device_mismatch" => new OfflinePinUnlockResult(
                    OfflinePinUnlockStatus.DeviceMismatch, offer.Grant, "Offline_DeviceMismatch"),
                "offline_pin_not_configured" => new OfflinePinUnlockResult(
                    OfflinePinUnlockStatus.PinNotConfigured, offer.Grant, "Offline_PinNotConfigured"),
                "offline_grant_invalid_scope" => new OfflinePinUnlockResult(
                    OfflinePinUnlockStatus.ScopeMismatch, offer.Grant, "Offline_GrantMissing"),
                _ => new OfflinePinUnlockResult(
                    OfflinePinUnlockStatus.GrantMissing, null, "Offline_GrantMissing")
            };
        }

        if (!OfflinePinHasher.IsValidPinFormat(pin, _options.PinMinLength))
        {
            return new OfflinePinUnlockResult(
                OfflinePinUnlockStatus.InvalidPinFormat, offer.Grant, "Offline_PinInvalidFormat");
        }

        var verifier = await store.LoadPinVerifierAsync(ct).ConfigureAwait(false);
        if (verifier is null)
        {
            return new OfflinePinUnlockResult(
                OfflinePinUnlockStatus.PinNotConfigured, offer.Grant, "Offline_PinNotConfigured");
        }

        var now = _clock.GetUtcNow();
        if (verifier.LockedUntilUtc is DateTimeOffset locked && locked > now)
        {
            return new OfflinePinUnlockResult(
                OfflinePinUnlockStatus.Locked,
                offer.Grant,
                "Offline_PinLocked",
                locked);
        }

        if (!OfflinePinHasher.Verify(pin, verifier))
        {
            var failed = verifier.FailedAttempts + 1;
            DateTimeOffset? lockUntil = null;
            if (failed >= Math.Max(1, _options.MaxFailedPinAttempts))
            {
                lockUntil = now.AddMinutes(Math.Max(1, _options.PinLockoutMinutes));
                failed = 0;
            }

            await store.SavePinVerifierAsync(
                    verifier with { FailedAttempts = failed, LockedUntilUtc = lockUntil },
                    ct)
                .ConfigureAwait(false);

            return lockUntil is not null
                ? new OfflinePinUnlockResult(
                    OfflinePinUnlockStatus.Locked, offer.Grant, "Offline_PinLocked", lockUntil)
                : new OfflinePinUnlockResult(
                    OfflinePinUnlockStatus.WrongPin, offer.Grant, "Offline_PinWrong");
        }

        // Successful unlock: reset attempts. Do NOT change grant expiry.
        await store.SavePinVerifierAsync(
                verifier with { FailedAttempts = 0, LockedUntilUtc = null },
                ct)
            .ConfigureAwait(false);

        IsUnlockedThisProcess = true;
        ActiveUnlockedGrant = offer.Grant;
        return new OfflinePinUnlockResult(OfflinePinUnlockStatus.Succeeded, offer.Grant);
    }

    private async Task<OfflineOperatingGrant?> LoadNormalizedGrantAsync(CancellationToken ct)
    {
        var grant = await store.LoadGrantAsync(ct).ConfigureAwait(false);
        if (grant is null || !OfflineOperatingGrant.IsSupportedSchemaVersion(grant.SchemaVersion))
        {
            return null;
        }

        var normalized = grant.NormalizeForEvaluation();
        if (!normalized.IsOrganizationScope && !normalized.IsPersonalScope)
        {
            return null;
        }

        return normalized;
    }

    private static bool IsPersonalEligible(AuthSession session)
    {
        if (session.OrganizationContextLocked)
        {
            return false;
        }

        if (session.OrganizationId is not null)
        {
            return false;
        }

        if (session.HasPosAccess)
        {
            return false;
        }

        return string.Equals(session.AccountClass, "Personal", StringComparison.OrdinalIgnoreCase);
    }
}
