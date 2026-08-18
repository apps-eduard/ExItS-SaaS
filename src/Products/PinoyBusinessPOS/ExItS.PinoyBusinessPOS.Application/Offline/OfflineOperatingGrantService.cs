using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Auth;
using Microsoft.Extensions.Options;

namespace ExItS.PinoyBusinessPOS.Application.Offline;

/// <summary>
/// Offline operate grant lifecycle for shared POS terminals.
/// Online bind establishes/refreshes one user's grant; PIN only unlocks an already-valid grant
/// and never extends <see cref="OfflineOperatingGrant.ExpiresAtUtc"/>.
/// Multiple cashiers may keep independent PIN verifiers and grants on one device.
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
        if (session.UserId == Guid.Empty)
        {
            return;
        }

        OfflineOperatingGrant? grant = null;
        var now = _clock.GetUtcNow();
        var maxHours = Math.Max(1, _options.MaxDurationHours);
        var duration = TimeSpan.FromHours(Math.Clamp(_options.DurationHours, 1, maxHours));

        if (string.IsNullOrWhiteSpace(deviceId))
        {
            deviceId = await deviceIdentity.GetOrCreateDeviceIdAsync(ct).ConfigureAwait(false);
        }

        deviceId = deviceId.Trim();

        if (session.HasPosAccess
            && session.OrganizationId is Guid orgId
            && session.BranchId is Guid branchId
            && session.PosDeviceId is Guid posDeviceId)
        {
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
                ScopeKind: OfflineGrantScopeKind.Organization,
                BranchId: branchId,
                PosDeviceId: posDeviceId);
        }
        else if (IsPersonalEligible(session))
        {
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
        // Per-user slots: never clear another cashier's PIN when this user establishes online.
        await BindUnboundPinForUserAsync(grant.UserId, ct).ConfigureAwait(false);

        IsUnlockedThisProcess = true;
        ActiveUnlockedGrant = grant;
    }

    public async Task ClearAsync(CancellationToken ct = default)
    {
        var userId = ActiveUnlockedGrant?.UserId;
        if (userId is null)
        {
            IsUnlockedThisProcess = false;
            ActiveUnlockedGrant = null;
            return;
        }

        await ClearUserGrantAsync(userId.Value, ct).ConfigureAwait(false);
    }

    public async Task ClearUserGrantAsync(Guid userId, CancellationToken ct = default)
    {
        await store.ClearGrantAsync(userId, ct).ConfigureAwait(false);
        if (ActiveUnlockedGrant?.UserId == userId)
        {
            IsUnlockedThisProcess = false;
            ActiveUnlockedGrant = null;
        }
    }

    public async Task RemoveEnrolledUserAsync(Guid userId, CancellationToken ct = default)
    {
        await store.RemoveUserAsync(userId, ct).ConfigureAwait(false);
        if (ActiveUnlockedGrant?.UserId == userId)
        {
            IsUnlockedThisProcess = false;
            ActiveUnlockedGrant = null;
        }
    }

    public void LockThisProcess()
    {
        IsUnlockedThisProcess = false;
        ActiveUnlockedGrant = null;
    }

    public Task<IReadOnlyList<OfflineEnrolledUserSummary>> GetEnrolledUsersAsync(
        CancellationToken ct = default) =>
        store.GetEnrolledUsersAsync(ct);

    public async Task<bool> HasPinConfiguredAsync(CancellationToken ct = default)
    {
        if (ActiveUnlockedGrant is { UserId: var active })
        {
            return await HasPinConfiguredAsync(active, ct).ConfigureAwait(false);
        }

        // Online enrollment gate: prefer evaluating against the sole enrolled user when one exists.
        var enrolled = await store.GetEnrolledUsersAsync(ct).ConfigureAwait(false);
        if (enrolled.Count == 1)
        {
            return await HasPinConfiguredAsync(enrolled[0].UserId, ct).ConfigureAwait(false);
        }

        return false;
    }

    public async Task<bool> HasPinConfiguredAsync(Guid userId, CancellationToken ct = default)
    {
        var verifier = await store.LoadPinVerifierAsync(userId, ct).ConfigureAwait(false);
        if (verifier is null
            || string.IsNullOrWhiteSpace(verifier.HashBase64)
            || string.IsNullOrWhiteSpace(verifier.SaltBase64))
        {
            return false;
        }

        if (verifier.UserId is Guid pinUser && pinUser != Guid.Empty && pinUser != userId)
        {
            return false;
        }

        return true;
    }

    public async Task<OfflinePinSetupResult> SetPinAsync(string pin, CancellationToken ct = default)
    {
        if (!OfflinePinHasher.IsValidPinFormat(pin, _options.PinMinLength))
        {
            return new OfflinePinSetupResult(false, "Offline_PinInvalidFormat");
        }

        var grant = ActiveUnlockedGrant ?? await ResolveSingleActiveGrantAsync(ct).ConfigureAwait(false);
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

        var verifier = OfflinePinHasher.Create(
            pin,
            Math.Max(10_000, _options.PinHashIterations),
            grant.UserId);
        try
        {
            await store.SavePinVerifierAsync(grant.UserId, verifier, ct).ConfigureAwait(false);
        }
        catch
        {
            return new OfflinePinSetupResult(false, "Auth_SecureStorageFailure");
        }

        return new OfflinePinSetupResult(true);
    }

    public async Task<OfflineColdStartOffer> EvaluateUserReadinessAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        if (userId == Guid.Empty)
        {
            return OfflineColdStartOffer.Denied("offline_grant_missing") with
            {
                EligibilityReason = OfflinePinEligibilityReason.NoStoredIdentity
            };
        }

        var enrolled = await store.GetEnrolledUsersAsync(ct).ConfigureAwait(false);
        var summary = enrolled.FirstOrDefault(u => u.UserId == userId);
        var grant = await LoadNormalizedGrantAsync(userId, ct).ConfigureAwait(false);
        var hasPin = await HasPinConfiguredAsync(userId, ct).ConfigureAwait(false);

        if (grant is null)
        {
            if (hasPin)
            {
                return OfflineColdStartOffer.Denied("offline_grant_missing");
            }

            if (summary is null)
            {
                return OfflineColdStartOffer.Denied("offline_grant_missing") with
                {
                    EligibilityReason = OfflinePinEligibilityReason.NoStoredIdentity
                };
            }

            return OfflineColdStartOffer.Denied("offline_grant_missing");
        }

        if (!grant.IsOrganizationScope && !grant.IsPersonalScope)
        {
            return OfflineColdStartOffer.Denied("offline_grant_invalid_scope");
        }

        var now = _clock.GetUtcNow();
        if (grant.IsExpired(now))
        {
            return OfflineColdStartOffer.Denied("offline_grant_expired");
        }

        var deviceId = await deviceIdentity.GetOrCreateDeviceIdAsync(ct).ConfigureAwait(false);
        if (!string.Equals(grant.DeviceId, deviceId, StringComparison.Ordinal))
        {
            return OfflineColdStartOffer.Denied("offline_device_mismatch");
        }

        if (!hasPin)
        {
            return OfflineColdStartOffer.Denied("offline_pin_not_configured");
        }

        var candidate = (summary ?? new OfflineEnrolledUserSummary(
            grant.UserId,
            grant.DisplayName ?? grant.Username ?? string.Empty,
            grant.Username,
            grant.ScopeKind,
            grant.OrganizationDisplayName,
            grant.ExpiresAtUtc,
            HasPinConfigured: true)) with { HasPinConfigured = true };

        return OfflineColdStartOffer.Allowed(grant, [candidate]);
    }

    public async Task<OfflineColdStartOffer> EvaluateColdStartOfferAsync(CancellationToken ct = default)
    {
        var enrolled = await store.GetEnrolledUsersAsync(ct).ConfigureAwait(false);
        if (enrolled.Count == 0)
        {
            return OfflineColdStartOffer.Denied("offline_grant_missing") with
            {
                EligibilityReason = OfflinePinEligibilityReason.NoStoredIdentity
            };
        }

        var candidates = new List<OfflineEnrolledUserSummary>();
        OfflineOperatingGrant? singleGrant = null;
        string? lastDenial = "offline_grant_missing";

        foreach (var summary in enrolled)
        {
            var readiness = await EvaluateUserReadinessAsync(summary.UserId, ct).ConfigureAwait(false);
            if (!readiness.CanOfferPinUnlock)
            {
                lastDenial = readiness.DenialReasonCode ?? lastDenial;
                continue;
            }

            if (readiness.UnlockCandidates is { Count: > 0 } ready)
            {
                candidates.AddRange(ready);
            }
            else if (readiness.Grant is not null)
            {
                candidates.Add(summary with { HasPinConfigured = true });
            }

            singleGrant = readiness.Grant;
        }

        if (candidates.Count == 0)
        {
            return OfflineColdStartOffer.Denied(lastDenial);
        }

        if (candidates.Count == 1)
        {
            return OfflineColdStartOffer.Allowed(singleGrant, candidates);
        }

        return OfflineColdStartOffer.Allowed(null, candidates);
    }

    public async Task<OfflinePinUnlockResult> UnlockWithPinAsync(string pin, CancellationToken ct = default)
    {
        var offer = await EvaluateColdStartOfferAsync(ct).ConfigureAwait(false);
        if (!offer.CanOfferPinUnlock)
        {
            return MapOfferDenial(offer);
        }

        if (offer.UnlockCandidates is { Count: > 1 })
        {
            return new OfflinePinUnlockResult(
                OfflinePinUnlockStatus.UserSelectionRequired,
                null,
                "Offline_SelectAccount");
        }

        var userId = offer.UnlockCandidates is { Count: 1 }
            ? offer.UnlockCandidates[0].UserId
            : offer.Grant?.UserId;
        if (userId is null)
        {
            return new OfflinePinUnlockResult(
                OfflinePinUnlockStatus.GrantMissing, null, "Offline_GrantMissing");
        }

        return await UnlockWithPinAsync(userId.Value, pin, ct).ConfigureAwait(false);
    }

    public async Task<OfflinePinUnlockResult> UnlockWithPinAsync(
        Guid userId,
        string pin,
        CancellationToken ct = default)
    {
        var grant = await LoadNormalizedGrantAsync(userId, ct).ConfigureAwait(false);
        if (grant is null)
        {
            return new OfflinePinUnlockResult(
                OfflinePinUnlockStatus.GrantMissing, null, "Offline_GrantMissing");
        }

        if (!grant.IsOrganizationScope && !grant.IsPersonalScope)
        {
            return new OfflinePinUnlockResult(
                OfflinePinUnlockStatus.ScopeMismatch, grant, "Offline_GrantMissing");
        }

        var now = _clock.GetUtcNow();
        if (grant.IsExpired(now))
        {
            return new OfflinePinUnlockResult(
                OfflinePinUnlockStatus.GrantExpired, grant, "Offline_GrantExpired");
        }

        var deviceId = await deviceIdentity.GetOrCreateDeviceIdAsync(ct).ConfigureAwait(false);
        if (!string.Equals(grant.DeviceId, deviceId, StringComparison.Ordinal))
        {
            return new OfflinePinUnlockResult(
                OfflinePinUnlockStatus.DeviceMismatch, grant, "Offline_DeviceMismatch");
        }

        if (!OfflinePinHasher.IsValidPinFormat(pin, _options.PinMinLength))
        {
            return new OfflinePinUnlockResult(
                OfflinePinUnlockStatus.InvalidPinFormat, grant, "Offline_PinInvalidFormat");
        }

        var verifier = await store.LoadPinVerifierAsync(userId, ct).ConfigureAwait(false);
        if (verifier is null
            || string.IsNullOrWhiteSpace(verifier.HashBase64)
            || string.IsNullOrWhiteSpace(verifier.SaltBase64))
        {
            return new OfflinePinUnlockResult(
                OfflinePinUnlockStatus.PinNotConfigured, grant, "Offline_PinNotConfigured");
        }

        if (verifier.UserId is Guid pinUser && pinUser != Guid.Empty && pinUser != userId)
        {
            return new OfflinePinUnlockResult(
                OfflinePinUnlockStatus.UserMismatch, grant, "Offline_GrantMissing");
        }

        if (verifier.LockedUntilUtc is DateTimeOffset locked && locked > now)
        {
            return new OfflinePinUnlockResult(
                OfflinePinUnlockStatus.Locked,
                grant,
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
                    userId,
                    verifier with { FailedAttempts = failed, LockedUntilUtc = lockUntil, UserId = userId },
                    ct)
                .ConfigureAwait(false);

            return lockUntil is not null
                ? new OfflinePinUnlockResult(
                    OfflinePinUnlockStatus.Locked, grant, "Offline_PinLocked", lockUntil)
                : new OfflinePinUnlockResult(
                    OfflinePinUnlockStatus.WrongPin, grant, "Offline_PinWrong");
        }

        // Successful unlock: reset attempts. Do NOT change grant expiry / issued / last-online.
        await store.SavePinVerifierAsync(
                userId,
                verifier with
                {
                    FailedAttempts = 0,
                    LockedUntilUtc = null,
                    UserId = userId
                },
                ct)
            .ConfigureAwait(false);

        IsUnlockedThisProcess = true;
        ActiveUnlockedGrant = grant;
        return new OfflinePinUnlockResult(OfflinePinUnlockStatus.Succeeded, grant);
    }

    public async Task<bool> ForceExpireGrantForDevelopmentAsync(Guid userId, CancellationToken ct = default)
    {
        if (!_options.AllowDevelopmentExpiryOverride)
        {
            return false;
        }

        var grant = await store.LoadGrantAsync(userId, ct).ConfigureAwait(false);
        if (grant is null)
        {
            return false;
        }

        var expired = grant with { ExpiresAtUtc = _clock.GetUtcNow().AddMinutes(-1) };
        await store.SaveGrantAsync(expired, ct).ConfigureAwait(false);
        if (ActiveUnlockedGrant?.UserId == userId)
        {
            ActiveUnlockedGrant = expired;
        }

        return true;
    }

    public async Task<OfflineOperatingGrant?> PeekStoredGrantAsync(CancellationToken ct = default)
    {
        if (ActiveUnlockedGrant is not null)
        {
            return ActiveUnlockedGrant.NormalizeForEvaluation();
        }

        return await ResolveSingleActiveGrantAsync(ct).ConfigureAwait(false);
    }

    public Task<OfflineOperatingGrant?> PeekStoredGrantAsync(Guid userId, CancellationToken ct = default) =>
        LoadNormalizedGrantAsync(userId, ct);

    private async Task BindUnboundPinForUserAsync(Guid userId, CancellationToken ct)
    {
        var verifier = await store.LoadPinVerifierAsync(userId, ct).ConfigureAwait(false);
        if (verifier is null)
        {
            return;
        }

        if (verifier.UserId is Guid pinUser && pinUser != Guid.Empty)
        {
            return;
        }

        await store.SavePinVerifierAsync(userId, verifier with { UserId = userId }, ct)
            .ConfigureAwait(false);
    }

    private async Task<OfflineOperatingGrant?> LoadNormalizedGrantAsync(Guid userId, CancellationToken ct)
    {
        var grant = await store.LoadGrantAsync(userId, ct).ConfigureAwait(false);
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

    private async Task<OfflineOperatingGrant?> ResolveSingleActiveGrantAsync(CancellationToken ct)
    {
        var enrolled = await store.GetEnrolledUsersAsync(ct).ConfigureAwait(false);
        if (enrolled.Count != 1)
        {
            return null;
        }

        return await LoadNormalizedGrantAsync(enrolled[0].UserId, ct).ConfigureAwait(false);
    }

    private static OfflinePinUnlockResult MapOfferDenial(OfflineColdStartOffer offer) =>
        offer.DenialReasonCode switch
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

        if (string.Equals(session.AccountClass, "Organization", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Personal class, or Local Validation / GUID login with no org context yet.
        return string.IsNullOrWhiteSpace(session.AccountClass)
               || string.Equals(session.AccountClass, "Personal", StringComparison.OrdinalIgnoreCase);
    }
}
