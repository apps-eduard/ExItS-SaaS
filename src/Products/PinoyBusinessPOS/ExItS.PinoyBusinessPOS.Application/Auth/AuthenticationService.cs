using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Auth;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Offline;
using ExItS.PinoyBusinessPOS.Application.Platform;

namespace ExItS.PinoyBusinessPOS.Application.Auth;

/// <summary>
/// Platform password grant authentication plus Development/Testing GUID fallback via
/// <c>X-Dev-Platform-User-Id</c>. Password/bearer path is available outside Development/Testing.
/// </summary>
public sealed class AuthenticationService(
    IAppInfoService appInfo,
    ISessionStore sessionStore,
    ICurrentUserContext currentUser,
    IOnboardingPreferenceStore preferences,
    IPlatformAccessClient accessClient,
    IAuthEventSink events,
    ILocalContextManager? localContext = null,
    IProtectedShellAccessPolicy? accessPolicy = null,
    TimeProvider? timeProvider = null,
    IOfflineOperatingGrantService? offlineGrant = null,
    IDeviceIdentityProvider? deviceIdentity = null,
    OfflineSessionUxState? offlineSessionUx = null) : IAuthenticationService
{
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(12);
    private readonly TimeProvider _clock = timeProvider ?? TimeProvider.System;
    private readonly ILocalContextManager? _localContext = localContext;
    private readonly IProtectedShellAccessPolicy? _accessPolicy = accessPolicy;
    private readonly IOfflineOperatingGrantService? _offlineGrant = offlineGrant;
    private readonly IDeviceIdentityProvider? _deviceIdentity = deviceIdentity;
    private readonly OfflineSessionUxState? _offlineSessionUx = offlineSessionUx;

    public bool IsDevelopmentAuthenticationEnabled =>
        string.Equals(appInfo.EnvironmentName, "Development", StringComparison.OrdinalIgnoreCase)
        || string.Equals(appInfo.EnvironmentName, "Testing", StringComparison.OrdinalIgnoreCase);

    public async Task<AuthResult> SignInAsync(SignInRequest request, CancellationToken ct = default)
    {
        var hasPassword = !string.IsNullOrWhiteSpace(request.UsernameOrEmail)
                          && !string.IsNullOrWhiteSpace(request.Password);
        if (hasPassword)
        {
            return await SignInWithPasswordAsync(
                    request.UsernameOrEmail!,
                    request.Password!,
                    request.AccountProfileId,
                    ct)
                .ConfigureAwait(false);
        }

        if (request.PlatformUserId is Guid userId && userId != Guid.Empty)
        {
            return await SignInWithDevUserIdAsync(userId, ct).ConfigureAwait(false);
        }

        events.Record("signin_failure", Dict(("reason", "invalid_credentials")));
        return new AuthResult(false, AuthFailureReason.InvalidCredentials, SafeMessageKey: "Auth_InvalidCredentials");
    }

    private async Task<AuthResult> SignInWithPasswordAsync(
        string usernameOrEmail,
        string password,
        Guid? preferredAccountProfileId,
        CancellationToken ct)
    {
        // Dual auth: Platform session (Personal/Org Owner APIs) + POS bearer access token.
        var loginResult = await accessClient
            .LoginAsync(new PlatformLoginRequest(usernameOrEmail.Trim(), password), ct)
            .ConfigureAwait(false);

        string? platformSessionToken = null;
        Guid? accountProfileId = null;
        string? accountClass = null;
        if (loginResult.IsSuccess && loginResult.Data is not null)
        {
            platformSessionToken = loginResult.Data.SessionToken;
            accountProfileId = loginResult.Data.AccountProfileId;
            accountClass = loginResult.Data.AccountClass;

            if (preferredAccountProfileId is Guid profileId
                && profileId != Guid.Empty
                && !string.IsNullOrWhiteSpace(platformSessionToken)
                && accountProfileId != profileId)
            {
                // Seed session header for profile select (Quick Login identity).
                currentUser.Set(new AuthSession(
                    loginResult.Data.UserId,
                    loginResult.Data.DisplayName,
                    loginResult.Data.Username,
                    loginResult.Data.Email,
                    OrganizationId: null,
                    OrganizationDisplayName: null,
                    IssuedAtUtc: _clock.GetUtcNow(),
                    ExpiresAtUtc: loginResult.Data.ExpiresAtUtc,
                    HasPosAccess: false,
                    AccessReasonCode: null,
                    PlatformSessionToken: platformSessionToken,
                    AccountClass: accountClass,
                    AccountProfileId: accountProfileId));

                var selected = await accessClient
                    .SelectAccountProfileAsync(new SelectAccountProfileRequest(profileId), ct)
                    .ConfigureAwait(false);
                if (selected.IsSuccess && selected.Data is not null)
                {
                    platformSessionToken = selected.Data.SessionToken;
                    accountProfileId = selected.Data.AccountProfileId ?? profileId;
                    accountClass = selected.Data.AccountClass ?? accountClass;

                    // Keep Platform session org context aligned with Organization profile selection.
                    if (string.Equals(accountClass, "Organization", StringComparison.OrdinalIgnoreCase)
                        && selected.Data.SelectedOrganizationId is Guid selectedOrg
                        && selectedOrg != Guid.Empty)
                    {
                        currentUser.Set(currentUser.Session! with
                        {
                            PlatformSessionToken = platformSessionToken,
                            AccountClass = accountClass,
                            AccountProfileId = accountProfileId
                        });
                        _ = await accessClient
                            .SetOrganizationContextAsync(new SetOrganizationContextRequest(selectedOrg), ct)
                            .ConfigureAwait(false);
                    }
                }
            }
        }

        var tokenResult = await accessClient
            .IssueTokenAsync(
                new IssuePlatformAccessTokenRequest(
                    GrantType: "password",
                    UsernameOrEmail: usernameOrEmail.Trim(),
                    Password: password,
                    OrganizationId: null,
                    ProductCode: null),
                ct)
            .ConfigureAwait(false);

        // Local Validation / profile login may succeed while password grant fails (hash vs shared
        // password). Fall back to session grant so Mobile still receives a bearer access token.
        if ((!tokenResult.IsSuccess || tokenResult.Data is null)
            && !string.IsNullOrWhiteSpace(platformSessionToken))
        {
            currentUser.Set(new AuthSession(
                loginResult.Data!.UserId,
                loginResult.Data.DisplayName,
                loginResult.Data.Username,
                loginResult.Data.Email,
                OrganizationId: loginResult.Data.SelectedOrganizationId,
                OrganizationDisplayName: loginResult.Data.SelectedOrganizationDisplayName,
                IssuedAtUtc: _clock.GetUtcNow(),
                ExpiresAtUtc: loginResult.Data.ExpiresAtUtc,
                HasPosAccess: false,
                AccessReasonCode: null,
                AccessToken: null,
                PlatformSessionToken: platformSessionToken,
                AccountClass: accountClass,
                AccountProfileId: accountProfileId,
                OrganizationContextLocked: loginResult.Data.OrganizationContextLocked));

            tokenResult = await accessClient
                .IssueTokenAsync(
                    new IssuePlatformAccessTokenRequest(
                        GrantType: "session",
                        UsernameOrEmail: null,
                        Password: null,
                        OrganizationId: null,
                        ProductCode: null),
                    ct)
                .ConfigureAwait(false);
        }

        if (!tokenResult.IsSuccess || tokenResult.Data is null)
        {
            // Prefer Platform session login when bearer issue fails (personal-only accounts).
            if (loginResult.IsSuccess && loginResult.Data is not null)
            {
                var login = loginResult.Data;
                var personalNow = _clock.GetUtcNow();
                var personalSession = new AuthSession(
                    login.UserId,
                    login.DisplayName,
                    login.Username,
                    login.Email,
                    OrganizationId: login.SelectedOrganizationId,
                    OrganizationDisplayName: login.SelectedOrganizationDisplayName,
                    IssuedAtUtc: personalNow,
                    ExpiresAtUtc: login.ExpiresAtUtc,
                    HasPosAccess: false,
                    AccessReasonCode: null,
                    AccessToken: null,
                    PlatformSessionToken: login.SessionToken,
                    AccountClass: login.AccountClass,
                    AccountProfileId: login.AccountProfileId,
                    OrganizationContextLocked: login.OrganizationContextLocked);

                try
                {
                    await sessionStore.SaveAsync(personalSession, Guid.NewGuid().ToString("N"), ct).ConfigureAwait(false);
                }
                catch
                {
                    events.Record("secure_storage_failure", Dict(("operation", "signin_session_save")));
                    return new AuthResult(false, AuthFailureReason.SecureStorageFailure, SafeMessageKey: "Auth_SecureStorageFailure");
                }

                currentUser.Set(personalSession);
                events.Record("signin_success", Dict(("userId", personalSession.UserId.ToString("D")), ("grant", "platform_session")));
                return new AuthResult(true, AuthFailureReason.None, personalSession);
            }

            var failure = MapTransport(tokenResult.Status);
            if (!loginResult.IsSuccess)
            {
                failure = MapTransport(loginResult.Status);
            }

            events.Record("signin_failure", Dict(("reason", failure.FailureReason.ToString())));
            return failure;
        }

        var issued = tokenResult.Data;
        var now = _clock.GetUtcNow();
        var marker = Guid.NewGuid().ToString("N");
        var session = new AuthSession(
            issued.UserId,
            issued.DisplayName,
            issued.Username,
            issued.Email,
            OrganizationId: issued.OrganizationId,
            OrganizationDisplayName: issued.OrganizationDisplayName,
            IssuedAtUtc: now,
            ExpiresAtUtc: issued.ExpiresAtUtc,
            HasPosAccess: issued.ProductAccessAllowed == true && issued.OrganizationId is not null,
            AccessReasonCode: issued.ProductAccessReasonCode,
            AccessToken: issued.AccessToken,
            PlatformSessionToken: platformSessionToken,
            AccountClass: accountClass,
            AccountProfileId: accountProfileId,
            OrganizationContextLocked: loginResult.Data?.OrganizationContextLocked == true);

        try
        {
            await sessionStore.SaveAsync(session, marker, ct).ConfigureAwait(false);
        }
        catch
        {
            events.Record("secure_storage_failure", Dict(("operation", "signin_password_save")));
            return new AuthResult(false, AuthFailureReason.SecureStorageFailure, SafeMessageKey: "Auth_SecureStorageFailure");
        }

        currentUser.Set(session);
        events.Record("signin_success", Dict(("userId", session.UserId.ToString("D")), ("grant", "password")));
        return new AuthResult(true, AuthFailureReason.None, session);
    }

    public async Task<AuthResult> SignInWithPlatformSessionTokenAsync(string sessionToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            events.Record("signin_failure", Dict(("reason", "invalid_session_token")));
            return new AuthResult(false, AuthFailureReason.InvalidCredentials, SafeMessageKey: "Auth_InvalidCredentials");
        }

        var now = _clock.GetUtcNow();
        // Seed Platform session header so /auth/me and session-grant token issue can authenticate.
        currentUser.Set(new AuthSession(
            Guid.Empty,
            DisplayName: string.Empty,
            Username: string.Empty,
            Email: string.Empty,
            OrganizationId: null,
            OrganizationDisplayName: null,
            IssuedAtUtc: now,
            ExpiresAtUtc: now.Add(SessionLifetime),
            HasPosAccess: false,
            AccessReasonCode: null,
            PlatformSessionToken: sessionToken.Trim()));

        var meResult = await accessClient.GetAuthMeAsync(ct).ConfigureAwait(false);
        if (!meResult.IsSuccess || meResult.Data is null)
        {
            currentUser.Clear();
            var failure = MapTransport(meResult.Status);
            events.Record("signin_failure", Dict(("reason", failure.FailureReason.ToString()), ("grant", "external_session")));
            return failure;
        }

        var me = meResult.Data;
        currentUser.Set(new AuthSession(
            me.UserId,
            me.DisplayName,
            me.Username,
            me.Email,
            OrganizationId: me.SelectedOrganizationId,
            OrganizationDisplayName: me.SelectedOrganizationDisplayName,
            IssuedAtUtc: now,
            ExpiresAtUtc: me.ExpiresAtUtc,
            HasPosAccess: false,
            AccessReasonCode: null,
            PlatformSessionToken: sessionToken.Trim(),
            AccountClass: me.AccountClass,
            AccountProfileId: me.AccountProfileId));

        var tokenResult = await accessClient
            .IssueTokenAsync(
                new IssuePlatformAccessTokenRequest(
                    GrantType: "session",
                    UsernameOrEmail: null,
                    Password: null,
                    OrganizationId: null,
                    ProductCode: null),
                ct)
            .ConfigureAwait(false);

        if (!tokenResult.IsSuccess || tokenResult.Data is null)
        {
            // Personal-only accounts may not receive a POS bearer; keep Platform session.
            var personalSession = currentUser.Session!;
            try
            {
                await sessionStore.SaveAsync(personalSession, Guid.NewGuid().ToString("N"), ct).ConfigureAwait(false);
            }
            catch
            {
                currentUser.Clear();
                events.Record("secure_storage_failure", Dict(("operation", "signin_external_session_save")));
                return new AuthResult(false, AuthFailureReason.SecureStorageFailure, SafeMessageKey: "Auth_SecureStorageFailure");
            }

            events.Record("signin_success", Dict(("userId", personalSession.UserId.ToString("D")), ("grant", "external_platform_session")));
            return new AuthResult(true, AuthFailureReason.None, personalSession);
        }

        var issued = tokenResult.Data;
        var marker = Guid.NewGuid().ToString("N");
        var session = new AuthSession(
            issued.UserId,
            issued.DisplayName,
            issued.Username,
            issued.Email,
            OrganizationId: issued.OrganizationId,
            OrganizationDisplayName: issued.OrganizationDisplayName,
            IssuedAtUtc: now,
            ExpiresAtUtc: issued.ExpiresAtUtc,
            HasPosAccess: issued.ProductAccessAllowed == true && issued.OrganizationId is not null,
            AccessReasonCode: issued.ProductAccessReasonCode,
            AccessToken: issued.AccessToken,
            PlatformSessionToken: sessionToken.Trim(),
            AccountClass: me.AccountClass,
            AccountProfileId: me.AccountProfileId);

        try
        {
            await sessionStore.SaveAsync(session, marker, ct).ConfigureAwait(false);
        }
        catch
        {
            currentUser.Clear();
            events.Record("secure_storage_failure", Dict(("operation", "signin_external_token_save")));
            return new AuthResult(false, AuthFailureReason.SecureStorageFailure, SafeMessageKey: "Auth_SecureStorageFailure");
        }

        currentUser.Set(session);
        events.Record("signin_success", Dict(("userId", session.UserId.ToString("D")), ("grant", "external_session")));
        return new AuthResult(true, AuthFailureReason.None, session);
    }

    private async Task<AuthResult> SignInWithDevUserIdAsync(Guid platformUserId, CancellationToken ct)
    {
        if (!IsDevelopmentAuthenticationEnabled)
        {
            events.Record("signin_blocked_production", EmptyProps());
            return new AuthResult(false, AuthFailureReason.ProductionAuthUnavailable, SafeMessageKey: "Auth_ProductionUnavailable");
        }

        var userResult = await accessClient.GetUserAsync(platformUserId, ct).ConfigureAwait(false);
        if (!userResult.IsSuccess || userResult.Data is null)
        {
            var failure = MapTransport(userResult.Status);
            events.Record("signin_failure", Dict(("reason", failure.FailureReason.ToString())));
            return failure;
        }

        if (!string.Equals(userResult.Data.Status, "Active", StringComparison.OrdinalIgnoreCase))
        {
            events.Record("signin_failure", Dict(("reason", "user_inactive")));
            return new AuthResult(false, AuthFailureReason.UserInactive, SafeMessageKey: "Access_UserInactive");
        }

        var now = _clock.GetUtcNow();
        var marker = Guid.NewGuid().ToString("N");
        var session = new AuthSession(
            userResult.Data.Id,
            userResult.Data.DisplayName,
            userResult.Data.Username,
            userResult.Data.Email,
            OrganizationId: null,
            OrganizationDisplayName: null,
            IssuedAtUtc: now,
            ExpiresAtUtc: now.Add(SessionLifetime),
            HasPosAccess: false,
            AccessReasonCode: null);

        try
        {
            await sessionStore.SaveAsync(session, marker, ct).ConfigureAwait(false);
        }
        catch
        {
            events.Record("secure_storage_failure", Dict(("operation", "signin_save")));
            return new AuthResult(false, AuthFailureReason.SecureStorageFailure, SafeMessageKey: "Auth_SecureStorageFailure");
        }

        currentUser.Set(session);
        events.Record("signin_success", Dict(("userId", session.UserId.ToString("D")), ("grant", "dev_user_id")));
        return new AuthResult(true, AuthFailureReason.None, session);
    }

    public async Task<AuthResult> RestoreSessionAsync(CancellationToken ct = default)
    {
        AuthSession? shell;
        string? marker;
        try
        {
            (shell, marker) = await sessionStore.LoadAsync(ct).ConfigureAwait(false);
        }
        catch
        {
            events.Record("secure_storage_failure", Dict(("operation", "restore_load")));
            return new AuthResult(false, AuthFailureReason.SecureStorageFailure, SafeMessageKey: "Auth_SecureStorageFailure");
        }

        if (shell is null || string.IsNullOrWhiteSpace(marker))
        {
            currentUser.Clear();
            return new AuthResult(false, AuthFailureReason.SessionExpired, SafeMessageKey: "Auth_SessionExpired");
        }

        var hasAccessToken = !string.IsNullOrWhiteSpace(shell.AccessToken);
        if (!IsDevelopmentAuthenticationEnabled && !hasAccessToken)
        {
            await ClearLocalAsync(ct).ConfigureAwait(false);
            return new AuthResult(false, AuthFailureReason.ProductionAuthUnavailable, SafeMessageKey: "Auth_ProductionUnavailable");
        }

        if (shell.ExpiresAtUtc <= _clock.GetUtcNow())
        {
            await LogoutAsync(ct).ConfigureAwait(false);
            events.Record("session_expired", EmptyProps());
            return new AuthResult(false, AuthFailureReason.SessionExpired, SafeMessageKey: "Auth_SessionExpired");
        }

        if (hasAccessToken)
        {
            return await RestoreBearerSessionAsync(shell, ct).ConfigureAwait(false);
        }

        await CloseLocalContextAsync(ct).ConfigureAwait(false);
        return await RebuildSessionAsync(shell.UserId, shell.IssuedAtUtc, shell.ExpiresAtUtc, accessToken: null, ct)
            .ConfigureAwait(false);
    }

    private async Task<AuthResult> RestoreBearerSessionAsync(AuthSession shell, CancellationToken ct)
    {
        // Seed context so PlatformBearerHandler can attach the token for introspect/org calls.
        currentUser.Set(shell);

        try
        {
            var introspect = await accessClient.IntrospectTokenAsync(shell.AccessToken, ct).ConfigureAwait(false);
            if (introspect.IsSuccess && introspect.Data is { Active: false })
            {
                await LogoutAsync(ct).ConfigureAwait(false);
                events.Record("session_expired", Dict(("reason", "token_inactive")));
                return new AuthResult(false, AuthFailureReason.SessionExpired, SafeMessageKey: "Auth_SessionExpired");
            }

            if (introspect.IsSuccess && introspect.Data is { Active: true } active)
            {
                var orgId = active.OrganizationId ?? await preferences.GetSelectedOrganizationIdAsync(ct).ConfigureAwait(false);
                var hasAccess = active.ProductAccessAllowed == true && orgId is not null;
                var subscriptionStatus = active.SubscriptionStatus;
                var enabledFeatureCodes = active.EnabledFeatureCodes;
                // Mirror org-bind: Local Validation introspect often returns a partial grant
                // snapshot; without merge, Inventory/Shifts stay denied until a fresh Owner login.
                if (IsDevelopmentAuthenticationEnabled && hasAccess)
                {
                    subscriptionStatus = string.IsNullOrWhiteSpace(subscriptionStatus)
                        ? PosSubscriptionStatuses.Active
                        : subscriptionStatus;
                    enabledFeatureCodes = UtangCapabilityPolicy.MergeWithDevelopmentDefaults(enabledFeatureCodes);
                }

                var restored = new AuthSession(
                    active.UserId ?? shell.UserId,
                    active.DisplayName ?? shell.DisplayName,
                    active.Username ?? shell.Username,
                    shell.Email,
                    orgId,
                    active.OrganizationDisplayName,
                    shell.IssuedAtUtc,
                    active.ExpiresAtUtc ?? shell.ExpiresAtUtc,
                    hasAccess,
                    active.ProductAccessReasonCode,
                    subscriptionStatus,
                    enabledFeatureCodes,
                    shell.AccessToken,
                    shell.PlatformSessionToken,
                    shell.AccountClass,
                    shell.AccountProfileId);

                try
                {
                    var (_, marker) = await sessionStore.LoadAsync(ct).ConfigureAwait(false);
                    marker ??= Guid.NewGuid().ToString("N");
                    await sessionStore.SaveAsync(restored, marker, ct).ConfigureAwait(false);
                }
                catch
                {
                    events.Record("secure_storage_failure", Dict(("operation", "restore_bearer_save")));
                    return new AuthResult(false, AuthFailureReason.SecureStorageFailure, SafeMessageKey: "Auth_SecureStorageFailure");
                }

                currentUser.Set(restored);
                if (orgId is Guid restoredOrg)
                {
                    await AlignPlatformOrganizationContextAsync(restored, restoredOrg, ct).ConfigureAwait(false);
                }

                if (hasAccess && orgId is Guid validatedOrg)
                {
                    await OpenLocalContextAsync(restored.UserId, validatedOrg, ct).ConfigureAwait(false);
                    _accessPolicy?.NotifySessionAccessChanged();
                    if (_offlineGrant is not null)
                    {
                        var deviceId = _deviceIdentity is null
                            ? string.Empty
                            : await _deviceIdentity.GetOrCreateDeviceIdAsync(ct).ConfigureAwait(false);
                        await _offlineGrant
                            .EstablishFromOnlineSessionAsync(restored, deviceId, roleCode: null, ct)
                            .ConfigureAwait(false);
                        _offlineSessionUx?.ResetSession();
                    }
                }
                else
                {
                    await CloseLocalContextAsync(ct).ConfigureAwait(false);
                    // Explicit server denial of product access — do not keep a stale offline grant.
                    if (_offlineGrant is not null && !hasAccess)
                    {
                        await _offlineGrant.ClearAsync(ct).ConfigureAwait(false);
                    }
                }

                return new AuthResult(true, AuthFailureReason.None, restored);
            }
        }
        catch
        {
            // Fall through: server unreachable is not an authorization denial.
        }

        // Introspect unavailable / transport failure — attempt offline grant fallback.
        // Explicit Active:false above already logged out; do not treat network failure as revoke.
        return await RestoreOfflineOperatingFallbackAsync(shell, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Cold-start path when Platform introspect is unreachable. Offers PIN unlock when a valid
    /// offline grant exists; otherwise keeps the shell and requires reconnect (not a hard revoke).
    /// </summary>
    private async Task<AuthResult> RestoreOfflineOperatingFallbackAsync(AuthSession shell, CancellationToken ct)
    {
        var selectedOrg = await preferences.GetSelectedOrganizationIdAsync(ct).ConfigureAwait(false);
        var orgId = selectedOrg ?? shell.OrganizationId;

        // Already unlocked with PIN in this process — restore operate context without re-prompt.
        if (_offlineGrant is { IsUnlockedThisProcess: true, ActiveUnlockedGrant: { } active }
            && active.UserId == shell.UserId
            && !active.IsExpired(_clock.GetUtcNow()))
        {
            var unlocked = shell with
            {
                OrganizationId = active.OrganizationId,
                OrganizationDisplayName = active.OrganizationDisplayName,
                DisplayName = string.IsNullOrWhiteSpace(shell.DisplayName) ? active.DisplayName ?? shell.DisplayName : shell.DisplayName,
                HasPosAccess = true,
                AccessReasonCode = "offline_grant",
                SubscriptionStatus = active.SubscriptionStatus ?? shell.SubscriptionStatus,
                EnabledFeatureCodes = active.EnabledFeatureCodes.Count > 0 ? active.EnabledFeatureCodes : shell.EnabledFeatureCodes
            };
            currentUser.Set(unlocked);
            await OpenLocalContextAsync(unlocked.UserId, active.OrganizationId, ct).ConfigureAwait(false);
            _accessPolicy?.NotifyOfflineUnlock(unlocked.UserId, active.OrganizationId);
            return new AuthResult(true, AuthFailureReason.None, unlocked);
        }

        if (_offlineGrant is not null)
        {
            var offer = await _offlineGrant.EvaluateColdStartOfferAsync(ct).ConfigureAwait(false);
            if (offer.CanOfferPinUnlock && offer.Grant is not null)
            {
                if (orgId is Guid boundOrg && offer.Grant.OrganizationId != boundOrg)
                {
                    // Preference/org mismatch — fail closed to reconnect.
                    offer = offer with { CanOfferPinUnlock = false, DenialReasonCode = "offline_org_mismatch" };
                }
                else if (offer.Grant.UserId != shell.UserId)
                {
                    offer = offer with { CanOfferPinUnlock = false, DenialReasonCode = "offline_user_mismatch" };
                }
            }

            if (offer is { CanOfferPinUnlock: true, Grant: not null })
            {
                var pinPending = shell with
                {
                    OrganizationId = offer.Grant.OrganizationId,
                    OrganizationDisplayName = offer.Grant.OrganizationDisplayName,
                    HasPosAccess = false,
                    AccessReasonCode = "offline_pin_required",
                    SubscriptionStatus = offer.Grant.SubscriptionStatus,
                    EnabledFeatureCodes = offer.Grant.EnabledFeatureCodes
                };
                currentUser.Set(pinPending);
                events.Record("offline_pin_required", Dict(
                    ("userId", shell.UserId.ToString("D")),
                    ("organizationId", offer.Grant.OrganizationId.ToString("D"))));
                return new AuthResult(
                    false,
                    AuthFailureReason.Offline,
                    pinPending,
                    SafeMessageKey: "Offline_PinRequired");
            }
        }

        var offlineShell = shell with
        {
            OrganizationId = orgId,
            HasPosAccess = false,
            AccessReasonCode = "reconnect_required"
        };
        currentUser.Set(offlineShell);
        await CloseLocalContextAsync(ct).ConfigureAwait(false);
        return new AuthResult(false, AuthFailureReason.Offline, offlineShell, SafeMessageKey: "SyncStatus_Reconnect");
    }

    public async Task<AuthResult> UnlockOfflineWithPinAsync(string pin, CancellationToken ct = default)
    {
        if (_offlineGrant is null)
        {
            return new AuthResult(false, AuthFailureReason.AccessDenied, SafeMessageKey: "Offline_GrantMissing");
        }

        var unlock = await _offlineGrant.UnlockWithPinAsync(pin, ct).ConfigureAwait(false);
        if (unlock.Status != OfflinePinUnlockStatus.Succeeded || unlock.Grant is null)
        {
            return new AuthResult(
                false,
                AuthFailureReason.AccessDenied,
                SafeMessageKey: unlock.SafeMessageKey ?? "Offline_PinWrong");
        }

        var grant = unlock.Grant;
        var shell = currentUser.Session;
        AuthSession? markerSession = null;
        string? marker = null;
        try
        {
            (markerSession, marker) = await sessionStore.LoadAsync(ct).ConfigureAwait(false);
        }
        catch
        {
            // Fall through with in-memory shell.
        }

        var baseSession = shell ?? markerSession;
        if (baseSession is null || baseSession.UserId != grant.UserId)
        {
            return new AuthResult(false, AuthFailureReason.SessionExpired, SafeMessageKey: "Auth_SessionExpired");
        }

        var restored = baseSession with
        {
            OrganizationId = grant.OrganizationId,
            OrganizationDisplayName = grant.OrganizationDisplayName,
            DisplayName = grant.DisplayName ?? baseSession.DisplayName,
            Username = grant.Username ?? baseSession.Username,
            Email = grant.Email ?? baseSession.Email,
            HasPosAccess = true,
            AccessReasonCode = "offline_grant",
            SubscriptionStatus = grant.SubscriptionStatus ?? baseSession.SubscriptionStatus,
            EnabledFeatureCodes = grant.EnabledFeatureCodes
        };

        try
        {
            marker ??= Guid.NewGuid().ToString("N");
            await sessionStore.SaveAsync(restored, marker, ct).ConfigureAwait(false);
        }
        catch
        {
            events.Record("secure_storage_failure", Dict(("operation", "offline_unlock_save")));
            return new AuthResult(false, AuthFailureReason.SecureStorageFailure, SafeMessageKey: "Auth_SecureStorageFailure");
        }

        currentUser.Set(restored);
        await preferences.SetSelectedOrganizationIdAsync(grant.OrganizationId, ct).ConfigureAwait(false);
        await OpenLocalContextAsync(restored.UserId, grant.OrganizationId, ct).ConfigureAwait(false);
        _accessPolicy?.NotifyOfflineUnlock(restored.UserId, grant.OrganizationId);
        _offlineSessionUx?.NotifyOfflinePinUnlocked();
        events.Record("offline_pin_unlock_succeeded", Dict(
            ("userId", restored.UserId.ToString("D")),
            ("organizationId", grant.OrganizationId.ToString("D"))));
        return new AuthResult(true, AuthFailureReason.None, restored);
    }

    public Task<OfflinePinSetupResult> SetOfflinePinAsync(string pin, CancellationToken ct = default) =>
        _offlineGrant is null
            ? Task.FromResult(new OfflinePinSetupResult(false, "Offline_GrantMissing"))
            : _offlineGrant.SetPinAsync(pin, ct);

    public Task<bool> HasOfflinePinConfiguredAsync(CancellationToken ct = default) =>
        _offlineGrant is null
            ? Task.FromResult(false)
            : _offlineGrant.HasPinConfiguredAsync(ct);

    public Task<OfflineColdStartOffer> EvaluateOfflineColdStartOfferAsync(CancellationToken ct = default) =>
        _offlineGrant is null
            ? Task.FromResult(new OfflineColdStartOffer(false, null, "offline_grant_missing"))
            : _offlineGrant.EvaluateColdStartOfferAsync(ct);

    public async Task<AuthResult> RefreshSessionAsync(CancellationToken ct = default)
    {
        var existing = currentUser.Session;
        if (existing is null)
        {
            return await RestoreSessionAsync(ct).ConfigureAwait(false);
        }

        if (!IsDevelopmentAuthenticationEnabled && string.IsNullOrWhiteSpace(existing.AccessToken))
        {
            return new AuthResult(false, AuthFailureReason.ProductionAuthUnavailable, SafeMessageKey: "Auth_ProductionUnavailable");
        }

        if (existing.ExpiresAtUtc <= _clock.GetUtcNow())
        {
            await LogoutAsync(ct).ConfigureAwait(false);
            events.Record("refresh_failure", Dict(("reason", "expired")));
            return new AuthResult(false, AuthFailureReason.SessionExpired, SafeMessageKey: "Auth_SessionExpired");
        }

        if (!string.IsNullOrWhiteSpace(existing.AccessToken))
        {
            return await RestoreBearerSessionAsync(existing, ct).ConfigureAwait(false);
        }

        var now = _clock.GetUtcNow();
        var refreshedExpiry = now.Add(SessionLifetime);
        var result = await RebuildSessionAsync(existing.UserId, now, refreshedExpiry, accessToken: null, ct)
            .ConfigureAwait(false);
        if (!result.Succeeded)
        {
            events.Record("refresh_failure", Dict(("reason", result.FailureReason.ToString())));
            await LogoutAsync(ct).ConfigureAwait(false);
            return result with { FailureReason = AuthFailureReason.RefreshFailed };
        }

        return result;
    }

    public async Task LogoutAsync(CancellationToken ct = default)
    {
        events.Record("logout", Dict(("userId", currentUser.Session?.UserId.ToString("D"))));
        var session = currentUser.Session;
        if (session is not null)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(session.AccessToken))
                {
                    await accessClient.RevokeAccessTokenAsync(ct).ConfigureAwait(false);
                }
            }
            catch
            {
                // Best-effort remote revoke; local clear still proceeds.
            }

            try
            {
                if (!string.IsNullOrWhiteSpace(session.PlatformSessionToken))
                {
                    await accessClient.LogoutSessionAsync(ct).ConfigureAwait(false);
                }
            }
            catch
            {
                // Best-effort remote logout; local clear still proceeds.
            }
        }

        await ClearLocalAsync(ct).ConfigureAwait(false);
    }

    public async Task LockAsync(CancellationToken ct = default)
    {
        events.Record("lock", Dict(("userId", currentUser.Session?.UserId.ToString("D"))));
        _offlineGrant?.LockThisProcess();
        _accessPolicy?.ClearProcessValidation();
        _offlineSessionUx?.ResetSession();

        var session = currentUser.Session;
        if (session is null)
        {
            return;
        }

        var locked = session with
        {
            HasPosAccess = false,
            AccessReasonCode = "offline_pin_required"
        };

        try
        {
            var (_, marker) = await sessionStore.LoadAsync(ct).ConfigureAwait(false);
            marker ??= Guid.NewGuid().ToString("N");
            await sessionStore.SaveAsync(locked, marker, ct).ConfigureAwait(false);
        }
        catch
        {
            events.Record("secure_storage_failure", Dict(("operation", "lock_save")));
        }

        currentUser.Set(locked);
        // Do not clear grant, PIN verifier, or durable session identity — Lock ≠ Sign out.
        await Task.CompletedTask.ConfigureAwait(false);
    }

    public async Task<AuthResult> SwitchToPersonalAsync(CancellationToken ct = default)
    {
        var session = currentUser.Session;
        if (session is null)
        {
            return new AuthResult(false, AuthFailureReason.SessionExpired, SafeMessageKey: "Auth_SessionExpired");
        }

        try
        {
            await accessClient
                .SetOrganizationContextAsync(new SetOrganizationContextRequest(null), ct)
                .ConfigureAwait(false);
        }
        catch
        {
            // Best-effort Platform context clear; local Personal switch still proceeds.
        }

        await CloseLocalContextAsync(ct).ConfigureAwait(false);
        _accessPolicy?.ClearProcessValidation();
        await preferences.ClearOrganizationPreferenceAsync(ct).ConfigureAwait(false);

        var updated = session with
        {
            OrganizationId = null,
            OrganizationDisplayName = null,
            HasPosAccess = false,
            AccessReasonCode = null,
            SubscriptionStatus = null,
            EnabledFeatureCodes = null
        };

        try
        {
            var (_, marker) = await sessionStore.LoadAsync(ct).ConfigureAwait(false);
            marker ??= Guid.NewGuid().ToString("N");
            await sessionStore.SaveAsync(updated, marker, ct).ConfigureAwait(false);
        }
        catch
        {
            events.Record("secure_storage_failure", Dict(("operation", "switch_personal_save")));
            return new AuthResult(false, AuthFailureReason.SecureStorageFailure, SafeMessageKey: "Auth_SecureStorageFailure");
        }

        currentUser.Set(updated);
        events.Record("switched_to_personal", Dict(("userId", session.UserId.ToString("D"))));

        // Bind Personal account class so Utang APIs under /api/v1/personal are allowed.
        return await EnsurePersonalAccountProfileAsync(ct).ConfigureAwait(false);
    }

    public async Task<AuthResult> EnsurePersonalAccountProfileAsync(CancellationToken ct = default)
    {
        var session = currentUser.Session;
        if (session is null)
        {
            return new AuthResult(false, AuthFailureReason.SessionExpired, SafeMessageKey: "Auth_SessionExpired");
        }

        if (string.Equals(session.AccountClass, "Personal", StringComparison.OrdinalIgnoreCase)
            && session.OrganizationId is null)
        {
            return new AuthResult(true, AuthFailureReason.None, session);
        }

        if (string.IsNullOrWhiteSpace(session.PlatformSessionToken))
        {
            var localOnly = session with
            {
                OrganizationId = null,
                OrganizationDisplayName = null,
                HasPosAccess = false,
                AccessReasonCode = null,
                SubscriptionStatus = null,
                EnabledFeatureCodes = null,
                AccountClass = "Personal"
            };
            currentUser.Set(localOnly);
            return new AuthResult(true, AuthFailureReason.None, localOnly);
        }

        var profiles = await accessClient.GetAccountProfilesAsync(ct).ConfigureAwait(false);
        if (!profiles.IsSuccess || profiles.Data is null)
        {
            return new AuthResult(true, AuthFailureReason.None, session with
            {
                OrganizationId = null,
                OrganizationDisplayName = null,
                HasPosAccess = false
            });
        }

        var personalProfile = profiles.Data.FirstOrDefault(p =>
            string.Equals(p.AccountClass, "Personal", StringComparison.OrdinalIgnoreCase)
            && string.Equals(p.Status, "Active", StringComparison.OrdinalIgnoreCase));
        if (personalProfile is null)
        {
            return new AuthResult(true, AuthFailureReason.None, session with
            {
                OrganizationId = null,
                OrganizationDisplayName = null,
                HasPosAccess = false
            });
        }

        var selected = await accessClient
            .SelectAccountProfileAsync(new SelectAccountProfileRequest(personalProfile.Id), ct)
            .ConfigureAwait(false);
        if (!selected.IsSuccess || selected.Data is null)
        {
            return new AuthResult(
                false,
                AuthFailureReason.AccessDenied,
                SafeMessageKey: "Access_Denied");
        }

        var updated = session with
        {
            OrganizationId = null,
            OrganizationDisplayName = null,
            HasPosAccess = false,
            AccessReasonCode = null,
            SubscriptionStatus = null,
            EnabledFeatureCodes = null,
            PlatformSessionToken = selected.Data.SessionToken ?? session.PlatformSessionToken,
            AccountClass = selected.Data.AccountClass ?? "Personal",
            AccountProfileId = selected.Data.AccountProfileId ?? personalProfile.Id,
            ExpiresAtUtc = selected.Data.ExpiresAtUtc == default ? session.ExpiresAtUtc : selected.Data.ExpiresAtUtc
        };

        try
        {
            var (_, marker) = await sessionStore.LoadAsync(ct).ConfigureAwait(false);
            marker ??= Guid.NewGuid().ToString("N");
            await sessionStore.SaveAsync(updated, marker, ct).ConfigureAwait(false);
        }
        catch
        {
            events.Record("secure_storage_failure", Dict(("operation", "ensure_personal_profile_save")));
            return new AuthResult(false, AuthFailureReason.SecureStorageFailure, SafeMessageKey: "Auth_SecureStorageFailure");
        }

        currentUser.Set(updated);
        events.Record("ensured_personal_profile", Dict(("userId", session.UserId.ToString("D"))));
        return new AuthResult(true, AuthFailureReason.None, updated);
    }

    public async Task<AuthResult> EnsureOrganizationAccountProfileAsync(CancellationToken ct = default)
    {
        var session = currentUser.Session;
        if (session is null)
        {
            return new AuthResult(false, AuthFailureReason.SessionExpired, SafeMessageKey: "Auth_SessionExpired");
        }

        if (string.Equals(session.AccountClass, "Organization", StringComparison.OrdinalIgnoreCase))
        {
            return new AuthResult(true, AuthFailureReason.None, session);
        }

        if (string.IsNullOrWhiteSpace(session.PlatformSessionToken))
        {
            return new AuthResult(true, AuthFailureReason.None, session);
        }

        var profiles = await accessClient.GetAccountProfilesAsync(ct).ConfigureAwait(false);
        if (!profiles.IsSuccess || profiles.Data is null)
        {
            return new AuthResult(true, AuthFailureReason.None, session);
        }

        var orgProfile = profiles.Data.FirstOrDefault(p =>
            string.Equals(p.AccountClass, "Organization", StringComparison.OrdinalIgnoreCase)
            && string.Equals(p.Status, "Active", StringComparison.OrdinalIgnoreCase));
        if (orgProfile is null)
        {
            return new AuthResult(true, AuthFailureReason.None, session);
        }

        var selected = await accessClient
            .SelectAccountProfileAsync(new SelectAccountProfileRequest(orgProfile.Id), ct)
            .ConfigureAwait(false);
        if (!selected.IsSuccess || selected.Data is null)
        {
            return new AuthResult(
                false,
                AuthFailureReason.AccessDenied,
                SafeMessageKey: "Access_Denied");
        }

        var updated = session with
        {
            PlatformSessionToken = selected.Data.SessionToken ?? session.PlatformSessionToken,
            AccountClass = selected.Data.AccountClass ?? "Organization",
            AccountProfileId = selected.Data.AccountProfileId ?? orgProfile.Id,
            ExpiresAtUtc = selected.Data.ExpiresAtUtc == default ? session.ExpiresAtUtc : selected.Data.ExpiresAtUtc
        };

        try
        {
            var (_, marker) = await sessionStore.LoadAsync(ct).ConfigureAwait(false);
            marker ??= Guid.NewGuid().ToString("N");
            await sessionStore.SaveAsync(updated, marker, ct).ConfigureAwait(false);
        }
        catch
        {
            events.Record("secure_storage_failure", Dict(("operation", "ensure_org_profile_save")));
            return new AuthResult(false, AuthFailureReason.SecureStorageFailure, SafeMessageKey: "Auth_SecureStorageFailure");
        }

        currentUser.Set(updated);
        events.Record("ensured_organization_profile", Dict(("userId", session.UserId.ToString("D"))));
        return new AuthResult(true, AuthFailureReason.None, updated);
    }

    public async Task<AuthResult> SelectOrganizationAsync(Guid organizationId, CancellationToken ct = default)
    {
        var profileReady = await EnsureOrganizationAccountProfileAsync(ct).ConfigureAwait(false);
        if (!profileReady.Succeeded)
        {
            return profileReady;
        }

        var session = currentUser.Session;
        if (session is null)
        {
            return new AuthResult(false, AuthFailureReason.SessionExpired, SafeMessageKey: "Auth_SessionExpired");
        }

        // Drop prior org/POS process cache before binding a different organization.
        _accessPolicy?.ClearProcessValidation();
        await CloseLocalContextAsync(ct).ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(session.AccessToken))
        {
            return await SelectOrganizationWithBindAsync(session, organizationId, ct).ConfigureAwait(false);
        }

        var accessResult = await accessClient
            .EvaluateAccessAsync(session.UserId, organizationId, PosProductCodes.PinoyBusinessPos, ct)
            .ConfigureAwait(false);
        if (!accessResult.IsSuccess || accessResult.Data is null)
        {
            var transport = MapTransport(accessResult.Status);
            events.Record("access_denial", Dict(
                ("userId", session.UserId.ToString("D")),
                ("organizationId", organizationId.ToString("D")),
                ("reason", transport.SafeMessageKey)));
            return transport;
        }

        if (!accessResult.Data.Allowed)
        {
            var denial = new AuthResult(
                false,
                AuthFailureReason.AccessDenied,
                SafeMessageKey: ProductAccessResolver.MapReasonKey(accessResult.Data.ReasonCode));
            events.Record("access_denial", Dict(
                ("userId", session.UserId.ToString("D")),
                ("organizationId", organizationId.ToString("D")),
                ("reason", denial.SafeMessageKey)));
            return denial;
        }

        var org = await accessClient.GetOrganizationAsync(organizationId, ct).ConfigureAwait(false);
        var displayName = org.IsSuccess && org.Data is not null ? org.Data.DisplayName : organizationId.ToString("D");

        await preferences.SetSelectedOrganizationIdAsync(organizationId, ct).ConfigureAwait(false);

        var subscriptionStatus = accessResult.Data.SubscriptionStatus;
        var enabledFeatureCodes = accessResult.Data.EnabledFeatureCodes;
        if (IsDevelopmentAuthenticationEnabled)
        {
            subscriptionStatus = string.IsNullOrWhiteSpace(subscriptionStatus)
                ? PosSubscriptionStatuses.Active
                : subscriptionStatus;
            enabledFeatureCodes = UtangCapabilityPolicy.MergeWithDevelopmentDefaults(enabledFeatureCodes);
        }

        var updated = session with
        {
            OrganizationId = organizationId,
            OrganizationDisplayName = displayName,
            HasPosAccess = true,
            AccessReasonCode = accessResult.Data.ReasonCode ?? "allowed",
            SubscriptionStatus = subscriptionStatus,
            EnabledFeatureCodes = enabledFeatureCodes
        };

        return await PersistOrganizationSelectionAsync(session, updated, organizationId, ct).ConfigureAwait(false);
    }

    private async Task<AuthResult> SelectOrganizationWithBindAsync(
        AuthSession session,
        Guid organizationId,
        CancellationToken ct)
    {
        var bindResult = await accessClient
            .BindTokenAsync(
                new BindPlatformAccessTokenRequest(
                    AccessToken: session.AccessToken,
                    OrganizationId: organizationId,
                    ProductCode: PosProductCodes.PinoyBusinessPos),
                ct)
            .ConfigureAwait(false);

        // After Personal → Organization profile switch, an older bearer can 401. Re-issue from
        // Platform session and retry bind once before surfacing a transport failure.
        if ((!bindResult.IsSuccess || bindResult.Data is null)
            && bindResult.Status == ApiCallStatus.Unauthorized
            && !string.IsNullOrWhiteSpace(session.PlatformSessionToken))
        {
            currentUser.Set(session);
            var reissue = await accessClient
                .IssueTokenAsync(
                    new IssuePlatformAccessTokenRequest(
                        GrantType: "session",
                        UsernameOrEmail: null,
                        Password: null,
                        OrganizationId: null,
                        ProductCode: null),
                    ct)
                .ConfigureAwait(false);
            if (reissue.IsSuccess && reissue.Data is not null && !string.IsNullOrWhiteSpace(reissue.Data.AccessToken))
            {
                session = session with { AccessToken = reissue.Data.AccessToken, ExpiresAtUtc = reissue.Data.ExpiresAtUtc };
                currentUser.Set(session);
                bindResult = await accessClient
                    .BindTokenAsync(
                        new BindPlatformAccessTokenRequest(
                            AccessToken: session.AccessToken,
                            OrganizationId: organizationId,
                            ProductCode: PosProductCodes.PinoyBusinessPos),
                        ct)
                    .ConfigureAwait(false);
            }
        }

        if (!bindResult.IsSuccess || bindResult.Data is null)
        {
            // Web Admin can select an organization with membership alone. Mobile bind also requires
            // POS operate (entitlement + product-local role). Fall back to org essentials when bind
            // is denied for product entry so Organization Owners are not stranded on Access Denied.
            if (IsProductEntryDenied(bindResult))
            {
                return await SelectOrganizationWithoutPosOperateAsync(
                        session,
                        organizationId,
                        InferDeniedReasonCode(bindResult.Error),
                        ct)
                    .ConfigureAwait(false);
            }

            var transport = MapTransport(bindResult.Status);
            events.Record("access_denial", Dict(
                ("userId", session.UserId.ToString("D")),
                ("organizationId", organizationId.ToString("D")),
                ("reason", transport.SafeMessageKey),
                ("status", bindResult.Status.ToString()),
                ("errorCode", bindResult.Error?.ErrorCode)));
            return transport;
        }

        var issued = bindResult.Data;
        await preferences.SetSelectedOrganizationIdAsync(organizationId, ct).ConfigureAwait(false);

        var accessToken = issued.AccessToken ?? session.AccessToken;
        string? subscriptionStatus = null;
        IReadOnlyList<string>? enabledFeatureCodes = null;
        if (issued.ProductAccessAllowed == true && !string.IsNullOrWhiteSpace(accessToken))
        {
            // Bind response omits commercial grants; catalog ManageCatalog and POS commercial
            // headers require SubscriptionStatus + EnabledFeatureCodes on the session.
            (subscriptionStatus, enabledFeatureCodes) = await ResolveCommercialGrantsAsync(
                    session,
                    accessToken,
                    issued.OrganizationId ?? organizationId,
                    ct)
                .ConfigureAwait(false);
        }

        var updated = session with
        {
            AccessToken = accessToken,
            OrganizationId = issued.OrganizationId ?? organizationId,
            OrganizationDisplayName = issued.OrganizationDisplayName,
            ExpiresAtUtc = issued.ExpiresAtUtc,
            HasPosAccess = issued.ProductAccessAllowed == true,
            AccessReasonCode = issued.ProductAccessReasonCode
                ?? (issued.ProductAccessAllowed == true ? "allowed" : "product_local_role_missing"),
            DisplayName = issued.DisplayName ?? session.DisplayName,
            Username = issued.Username ?? session.Username,
            Email = issued.Email ?? session.Email,
            SubscriptionStatus = subscriptionStatus,
            EnabledFeatureCodes = enabledFeatureCodes
        };

        if (issued.ProductAccessAllowed == false)
        {
            events.Record("organization_selected_without_pos", Dict(
                ("userId", session.UserId.ToString("D")),
                ("organizationId", organizationId.ToString("D")),
                ("reason", updated.AccessReasonCode)));
        }

        return await PersistOrganizationSelectionAsync(session, updated, organizationId, ct).ConfigureAwait(false);
    }

    private async Task<(string? SubscriptionStatus, IReadOnlyList<string>? EnabledFeatureCodes)> ResolveCommercialGrantsAsync(
        AuthSession session,
        string accessToken,
        Guid organizationId,
        CancellationToken ct)
    {
        // Seed bearer so Introspect/Evaluate handlers can attach the bound token.
        currentUser.Set(session with { AccessToken = accessToken, OrganizationId = organizationId });

        string? subscriptionStatus = null;
        IReadOnlyList<string>? enabledFeatureCodes = null;

        try
        {
            var introspect = await accessClient.IntrospectTokenAsync(accessToken, ct).ConfigureAwait(false);
            if (introspect.IsSuccess && introspect.Data is { Active: true } active)
            {
                subscriptionStatus = active.SubscriptionStatus;
                enabledFeatureCodes = active.EnabledFeatureCodes;
            }
        }
        catch
        {
            // Fall through to evaluate.
        }

        // Status alone is not enough — ManageCatalog requires feature grant codes.
        if (enabledFeatureCodes is not { Count: > 0 })
        {
            try
            {
                var accessResult = await accessClient
                    .EvaluateAccessAsync(session.UserId, organizationId, PosProductCodes.PinoyBusinessPos, ct)
                    .ConfigureAwait(false);
                if (accessResult.IsSuccess && accessResult.Data is not null)
                {
                    subscriptionStatus ??= accessResult.Data.SubscriptionStatus;
                    if (accessResult.Data.EnabledFeatureCodes is { Count: > 0 })
                    {
                        enabledFeatureCodes = accessResult.Data.EnabledFeatureCodes;
                    }
                }
            }
            catch
            {
                // Leave grants empty unless Development fallback applies below.
            }
        }

        if (IsDevelopmentAuthenticationEnabled)
        {
            // Local Validation / PhysicalDevice: introspect/evaluate may return a partial
            // grant snapshot (e.g. catalog only). After Personal → org re-bind that strands
            // Inventory/Registers/Shifts behind capability checks that bounce to Owner home.
            // Always merge the full Dev grant set so ops UIs stay reachable for validation.
            subscriptionStatus = string.IsNullOrWhiteSpace(subscriptionStatus)
                ? PosSubscriptionStatuses.Active
                : subscriptionStatus;
            enabledFeatureCodes = UtangCapabilityPolicy.MergeWithDevelopmentDefaults(enabledFeatureCodes);
        }

        return (subscriptionStatus, enabledFeatureCodes);
    }

    private async Task<AuthResult> SelectOrganizationWithoutPosOperateAsync(
        AuthSession session,
        Guid organizationId,
        string? reasonCode,
        CancellationToken ct)
    {
        var eligible = await accessClient.GetAuthEligibleOrganizationsAsync(ct).ConfigureAwait(false);
        var membershipOk = eligible.IsSuccess
            && eligible.Data is not null
            && eligible.Data.Any(o => o.OrganizationId == organizationId);

        if (!membershipOk)
        {
            var memberships = await accessClient.GetUserMembershipsAsync(session.UserId, ct).ConfigureAwait(false);
            membershipOk = memberships.IsSuccess
                && memberships.Data is not null
                && memberships.Data.Items.Any(m =>
                    m.OrganizationId == organizationId
                    && string.Equals(m.Status, "Active", StringComparison.OrdinalIgnoreCase));
        }

        if (!membershipOk)
        {
            var denial = new AuthResult(
                false,
                AuthFailureReason.AccessDenied,
                SafeMessageKey: ProductAccessResolver.MapReasonKey(reasonCode));
            events.Record("access_denial", Dict(
                ("userId", session.UserId.ToString("D")),
                ("organizationId", organizationId.ToString("D")),
                ("reason", denial.SafeMessageKey)));
            return denial;
        }

        var org = await accessClient.GetOrganizationAsync(organizationId, ct).ConfigureAwait(false);
        var displayName = org.IsSuccess && org.Data is not null
            ? org.Data.DisplayName
            : organizationId.ToString("D");

        await preferences.SetSelectedOrganizationIdAsync(organizationId, ct).ConfigureAwait(false);

        var updated = session with
        {
            OrganizationId = organizationId,
            OrganizationDisplayName = displayName,
            HasPosAccess = false,
            AccessReasonCode = reasonCode ?? "product_local_role_missing"
        };

        events.Record("organization_selected_without_pos", Dict(
            ("userId", session.UserId.ToString("D")),
            ("organizationId", organizationId.ToString("D")),
            ("reason", updated.AccessReasonCode)));

        return await PersistOrganizationSelectionAsync(session, updated, organizationId, ct).ConfigureAwait(false);
    }

    private static bool IsProductEntryDenied(ApiResult<PlatformAccessTokenIssueDto> result) =>
        result.Status == ApiCallStatus.Forbidden;

    private static string InferDeniedReasonCode(ApiError? error)
    {
        var detail = error?.Detail ?? string.Empty;
        if (detail.Contains("Product-local role", StringComparison.OrdinalIgnoreCase))
        {
            return "product_local_role_missing";
        }

        // Platform bind/issue embeds the effective reason as "(reason_code)".
        foreach (var code in KnownProductEntryReasonCodes)
        {
            if (detail.Contains(code, StringComparison.OrdinalIgnoreCase))
            {
                return code;
            }
        }

        if (detail.Contains("stale", StringComparison.OrdinalIgnoreCase))
        {
            return "entitlement_stale";
        }

        return "product_assignment_missing";
    }

    private static readonly string[] KnownProductEntryReasonCodes =
    [
        "entitlement_stale",
        "entitlement_missing",
        "entitlement_denied",
        "subscription_ineligible",
        "product_assignment_inactive",
        "product_assignment_missing",
        "product_inactive",
        "membership_inactive",
        "membership_missing",
        "organization_inactive",
        "user_inactive",
        "product_local_role_missing"
    ];

    private async Task<AuthResult> PersistOrganizationSelectionAsync(
        AuthSession previous,
        AuthSession updated,
        Guid organizationId,
        CancellationToken ct)
    {
        try
        {
            var (_, marker) = await sessionStore.LoadAsync(ct).ConfigureAwait(false);
            marker ??= Guid.NewGuid().ToString("N");
            await sessionStore.SaveAsync(updated, marker, ct).ConfigureAwait(false);
        }
        catch
        {
            events.Record("secure_storage_failure", Dict(("operation", "select_org_save")));
            return new AuthResult(false, AuthFailureReason.SecureStorageFailure, SafeMessageKey: "Auth_SecureStorageFailure");
        }

        currentUser.Set(updated);
        await AlignPlatformOrganizationContextAsync(updated, organizationId, ct).ConfigureAwait(false);
        await OpenLocalContextAsync(updated.UserId, organizationId, ct).ConfigureAwait(false);
        // SelectOrganization clears process validation first; re-arm once POS access is bound.
        _accessPolicy?.NotifySessionAccessChanged();
        if (updated.HasPosAccess && _offlineGrant is not null)
        {
            var deviceId = _deviceIdentity is null
                ? string.Empty
                : await _deviceIdentity.GetOrCreateDeviceIdAsync(ct).ConfigureAwait(false);
            await _offlineGrant
                .EstablishFromOnlineSessionAsync(updated, deviceId, roleCode: null, ct)
                .ConfigureAwait(false);
            _offlineSessionUx?.ResetSession();
        }

        events.Record("organization_selected", Dict(
            ("userId", previous.UserId.ToString("D")),
            ("organizationId", organizationId.ToString("D"))));
        return new AuthResult(true, AuthFailureReason.None, updated);
    }

    /// <summary>
    /// Keeps Platform session SelectedOrganizationId aligned with Mobile's bound org so
    /// PlatformSession-authenticated org/subscription/entitlement reads authorize correctly.
    /// </summary>
    private async Task AlignPlatformOrganizationContextAsync(
        AuthSession session,
        Guid? organizationId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(session.PlatformSessionToken))
        {
            return;
        }

        try
        {
            await accessClient
                .SetOrganizationContextAsync(new SetOrganizationContextRequest(organizationId), ct)
                .ConfigureAwait(false);
        }
        catch
        {
            // Best-effort; membership-based view auth remains the fallback.
        }
    }

    private async Task<AuthResult> RebuildSessionAsync(
        Guid userId,
        DateTimeOffset issuedAt,
        DateTimeOffset expiresAt,
        string? accessToken,
        CancellationToken ct)
    {
        var userResult = await accessClient.GetUserAsync(userId, ct).ConfigureAwait(false);
        if (!userResult.IsSuccess || userResult.Data is null)
        {
            if (userResult.Status is ApiCallStatus.Offline or ApiCallStatus.Timeout or ApiCallStatus.Cancelled)
            {
                var offlineSession = await BuildUnvalidatedShellAsync(userId, issuedAt, expiresAt, accessToken, ct)
                    .ConfigureAwait(false);
                if (offlineSession is not null)
                {
                    currentUser.Set(offlineSession);
                    await CloseLocalContextAsync(ct).ConfigureAwait(false);
                    return new AuthResult(
                        false,
                        AuthFailureReason.Offline,
                        offlineSession,
                        SafeMessageKey: "SyncStatus_Reconnect");
                }
            }

            await ClearLocalAsync(ct).ConfigureAwait(false);
            return MapTransport(userResult.Status) with { FailureReason = AuthFailureReason.RefreshFailed };
        }

        if (!string.Equals(userResult.Data.Status, "Active", StringComparison.OrdinalIgnoreCase))
        {
            await ClearLocalAsync(ct).ConfigureAwait(false);
            events.Record("access_denial", Dict(("reason", "user_inactive")));
            return new AuthResult(false, AuthFailureReason.UserInactive, SafeMessageKey: "Access_UserInactive");
        }

        var selectedOrg = await preferences.GetSelectedOrganizationIdAsync(ct).ConfigureAwait(false);
        Guid? organizationId = selectedOrg;
        string? organizationName = null;
        var hasAccess = false;
        string? reason = null;

        string? subscriptionStatus = null;
        IReadOnlyList<string>? enabledFeatureCodes = null;

        if (organizationId is Guid orgId)
        {
            var accessResult = await accessClient
                .EvaluateAccessAsync(userId, orgId, PosProductCodes.PinoyBusinessPos, ct)
                .ConfigureAwait(false);
            if (!accessResult.IsSuccess || accessResult.Data is null || !accessResult.Data.Allowed)
            {
                await preferences.ClearOrganizationPreferenceAsync(ct).ConfigureAwait(false);
                organizationId = null;
                reason = accessResult.Data is not null && !accessResult.Data.Allowed
                    ? ProductAccessResolver.MapReasonKey(accessResult.Data.ReasonCode)
                    : MapTransport(accessResult.Status).SafeMessageKey;
            }
            else
            {
                hasAccess = true;
                reason = accessResult.Data.ReasonCode ?? "allowed";
                subscriptionStatus = accessResult.Data.SubscriptionStatus;
                enabledFeatureCodes = accessResult.Data.EnabledFeatureCodes;
                if (IsDevelopmentAuthenticationEnabled)
                {
                    subscriptionStatus = string.IsNullOrWhiteSpace(subscriptionStatus)
                        ? PosSubscriptionStatuses.Active
                        : subscriptionStatus;
                    enabledFeatureCodes = UtangCapabilityPolicy.MergeWithDevelopmentDefaults(enabledFeatureCodes);
                }

                var org = await accessClient.GetOrganizationAsync(orgId, ct).ConfigureAwait(false);
                organizationName = org.IsSuccess && org.Data is not null ? org.Data.DisplayName : orgId.ToString("D");
            }
        }

        var existingShell = await sessionStore.LoadAsync(ct).ConfigureAwait(false);
        var session = new AuthSession(
            userResult.Data.Id,
            userResult.Data.DisplayName,
            userResult.Data.Username,
            userResult.Data.Email,
            organizationId,
            organizationName,
            issuedAt,
            expiresAt,
            hasAccess,
            reason,
            subscriptionStatus,
            enabledFeatureCodes,
            accessToken,
            existingShell.Session?.PlatformSessionToken,
            existingShell.Session?.AccountClass,
            existingShell.Session?.AccountProfileId);

        try
        {
            var (_, marker) = await sessionStore.LoadAsync(ct).ConfigureAwait(false);
            marker ??= Guid.NewGuid().ToString("N");
            await sessionStore.SaveAsync(session, marker, ct).ConfigureAwait(false);
        }
        catch
        {
            events.Record("secure_storage_failure", Dict(("operation", "rebuild_save")));
            return new AuthResult(false, AuthFailureReason.SecureStorageFailure, SafeMessageKey: "Auth_SecureStorageFailure");
        }

        currentUser.Set(session);
        if (hasAccess && organizationId is Guid validatedOrg)
        {
            await OpenLocalContextAsync(userId, validatedOrg, ct).ConfigureAwait(false);
        }
        else
        {
            await CloseLocalContextAsync(ct).ConfigureAwait(false);
        }

        return new AuthResult(true, AuthFailureReason.None, session);
    }

    private async Task<AuthSession?> BuildUnvalidatedShellAsync(
        Guid userId,
        DateTimeOffset issuedAt,
        DateTimeOffset expiresAt,
        string? accessToken,
        CancellationToken ct)
    {
        try
        {
            var (shell, _) = await sessionStore.LoadAsync(ct).ConfigureAwait(false);
            if (shell is null || shell.UserId != userId)
            {
                return new AuthSession(
                    userId,
                    DisplayName: string.Empty,
                    Username: string.Empty,
                    Email: string.Empty,
                    OrganizationId: await preferences.GetSelectedOrganizationIdAsync(ct).ConfigureAwait(false),
                    OrganizationDisplayName: null,
                    issuedAt,
                    expiresAt,
                    HasPosAccess: false,
                    AccessReasonCode: "reconnect_required",
                    AccessToken: accessToken);
            }

            return shell with
            {
                HasPosAccess = false,
                AccessReasonCode = "reconnect_required",
                SubscriptionStatus = null,
                EnabledFeatureCodes = null,
                AccessToken = accessToken ?? shell.AccessToken
            };
        }
        catch
        {
            return null;
        }
    }

    private async Task OpenLocalContextAsync(Guid userId, Guid organizationId, CancellationToken ct)
    {
        if (_localContext is null)
        {
            return;
        }

        try
        {
            await _localContext
                .OpenAsync(userId, organizationId, PosProductCodes.PinoyBusinessPos, ct)
                .ConfigureAwait(false);
        }
        catch
        {
            events.Record("local_context_open_failure", Dict(
                ("userId", userId.ToString("D")),
                ("organizationId", organizationId.ToString("D"))));
        }
    }

    private async Task CloseLocalContextAsync(CancellationToken ct)
    {
        if (_localContext is null)
        {
            return;
        }

        try
        {
            await _localContext.CloseAsync(ct).ConfigureAwait(false);
        }
        catch
        {
            events.Record("local_context_close_failure", EmptyProps());
        }
    }

    public async Task<AuthResult> ContinueAfterStartBusinessAsync(
        StartBusinessResultDto result,
        CancellationToken ct = default)
    {
        var existing = currentUser.Session;
        if (existing is null)
        {
            return new AuthResult(false, AuthFailureReason.SessionExpired, SafeMessageKey: "Auth_SessionExpired");
        }

        var now = _clock.GetUtcNow();
        var seeded = existing with
        {
            PlatformSessionToken = result.SessionToken,
            AccountClass = result.AccountClass,
            AccountProfileId = result.OrganizationAccountProfileId,
            OrganizationId = result.SelectedOrganizationId ?? result.OrganizationId,
            HasPosAccess = false,
            AccessReasonCode = null
        };

        try
        {
            var (_, marker) = await sessionStore.LoadAsync(ct).ConfigureAwait(false);
            marker ??= Guid.NewGuid().ToString("N");
            await sessionStore.SaveAsync(seeded, marker, ct).ConfigureAwait(false);
        }
        catch
        {
            events.Record("secure_storage_failure", Dict(("operation", "start_business_session")));
            return new AuthResult(false, AuthFailureReason.SecureStorageFailure, SafeMessageKey: "Auth_SecureStorageFailure");
        }

        currentUser.Set(seeded);

        if (result.PosEntitlementActivated || result.PosOwnerRoleGranted)
        {
            return await SelectOrganizationAsync(result.OrganizationId, ct).ConfigureAwait(false);
        }

        await preferences.SetSelectedOrganizationIdAsync(result.OrganizationId, ct).ConfigureAwait(false);
        var org = await accessClient.GetOrganizationAsync(result.OrganizationId, ct).ConfigureAwait(false);
        var updated = seeded with
        {
            OrganizationId = result.OrganizationId,
            OrganizationDisplayName = org.IsSuccess && org.Data is not null
                ? org.Data.DisplayName
                : result.OrganizationId.ToString("D"),
            IssuedAtUtc = now
        };

        try
        {
            var (_, marker) = await sessionStore.LoadAsync(ct).ConfigureAwait(false);
            marker ??= Guid.NewGuid().ToString("N");
            await sessionStore.SaveAsync(updated, marker, ct).ConfigureAwait(false);
        }
        catch
        {
            return new AuthResult(false, AuthFailureReason.SecureStorageFailure, SafeMessageKey: "Auth_SecureStorageFailure");
        }

        currentUser.Set(updated);
        events.Record("start_business_continued", Dict(("organizationId", result.OrganizationId.ToString("D"))));
        return new AuthResult(true, AuthFailureReason.None, updated);
    }

    private async Task ClearLocalAsync(CancellationToken ct)
    {
        await CloseLocalContextAsync(ct).ConfigureAwait(false);
        _accessPolicy?.ClearProcessValidation();
        _offlineSessionUx?.ResetSession();
        if (_offlineGrant is not null)
        {
            try
            {
                // Sign out clears the operate grant so the next auth requires internet.
                // PIN verifier is retained for reuse after the next online establish.
                await _offlineGrant.ClearAsync(ct).ConfigureAwait(false);
            }
            catch
            {
                // Best-effort grant clear.
            }
        }

        try
        {
            await sessionStore.ClearAsync(ct).ConfigureAwait(false);
        }
        catch
        {
            events.Record("secure_storage_failure", Dict(("operation", "clear")));
        }

        // Keep SelectedOrganizationId so the next successful sign-in can safely restore
        // the last valid organization context (or fall through to Personal / chooser).
        currentUser.Clear();
    }

    private static AuthResult MapTransport(ApiCallStatus status) => status switch
    {
        ApiCallStatus.Offline => new AuthResult(false, AuthFailureReason.Offline, SafeMessageKey: "Auth_Offline"),
        ApiCallStatus.Timeout => new AuthResult(false, AuthFailureReason.Timeout, SafeMessageKey: "Auth_Timeout"),
        ApiCallStatus.RateLimited => new AuthResult(false, AuthFailureReason.RateLimited, SafeMessageKey: "Auth_RateLimited"),
        ApiCallStatus.NotFound => new AuthResult(false, AuthFailureReason.InvalidCredentials, SafeMessageKey: "Auth_InvalidCredentials"),
        ApiCallStatus.Unauthorized => new AuthResult(false, AuthFailureReason.InvalidCredentials, SafeMessageKey: "Auth_InvalidCredentials"),
        ApiCallStatus.Forbidden => new AuthResult(false, AuthFailureReason.AccessDenied, SafeMessageKey: "Access_Denied"),
        ApiCallStatus.Cancelled => new AuthResult(false, AuthFailureReason.Cancelled, SafeMessageKey: "Auth_Cancelled"),
        _ => new AuthResult(false, AuthFailureReason.ApiUnavailable, SafeMessageKey: "Auth_ApiUnavailable")
    };

    private static IReadOnlyDictionary<string, string?> EmptyProps() => new Dictionary<string, string?>();

    private static IReadOnlyDictionary<string, string?> Dict(params (string Key, string? Value)[] pairs)
    {
        var map = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var (key, value) in pairs)
        {
            map[key] = value;
        }

        return map;
    }
}
