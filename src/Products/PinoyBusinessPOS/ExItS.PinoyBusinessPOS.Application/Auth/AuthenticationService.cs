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
    OfflineSessionUxState? offlineSessionUx = null,
    SellingModeService? sellingMode = null,
    IConnectivityService? connectivity = null) : IAuthenticationService, IPlatformAccessTokenRecovery
{
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(12);
    private readonly TimeProvider _clock = timeProvider ?? TimeProvider.System;
    private readonly ILocalContextManager? _localContext = localContext;
    private readonly IProtectedShellAccessPolicy? _accessPolicy = accessPolicy;
    private readonly IOfflineOperatingGrantService? _offlineGrant = offlineGrant;
    private readonly IDeviceIdentityProvider? _deviceIdentity = deviceIdentity;
    private readonly OfflineSessionUxState? _offlineSessionUx = offlineSessionUx;
    private readonly SellingModeService? _sellingMode = sellingMode;
    private readonly IConnectivityService? _connectivity = connectivity;
    private readonly SemaphoreSlim _reissueGate = new(1, 1);

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

                personalSession = await NormalizePersonalDefaultSessionAsync(personalSession, ct).ConfigureAwait(false);
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
                await EnsureOfflineOperateGrantAsync(ct).ConfigureAwait(false);
                return new AuthResult(true, AuthFailureReason.None, currentUser.Session ?? personalSession);
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

        session = await NormalizePersonalDefaultSessionAsync(session, ct).ConfigureAwait(false);
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
        await EnsureOfflineOperateGrantAsync(ct).ConfigureAwait(false);
        return new AuthResult(true, AuthFailureReason.None, currentUser.Session ?? session);
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
            var personalSession = await NormalizePersonalDefaultSessionAsync(currentUser.Session!, ct)
                .ConfigureAwait(false);
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

            currentUser.Set(personalSession);
            events.Record("signin_success", Dict(("userId", personalSession.UserId.ToString("D")), ("grant", "external_platform_session")));
            await EnsureOfflineOperateGrantAsync(ct).ConfigureAwait(false);
            return new AuthResult(true, AuthFailureReason.None, currentUser.Session ?? personalSession);
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

        session = await NormalizePersonalDefaultSessionAsync(session, ct).ConfigureAwait(false);
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
        await EnsureOfflineOperateGrantAsync(ct).ConfigureAwait(false);
        return new AuthResult(true, AuthFailureReason.None, currentUser.Session ?? session);
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
            AccessReasonCode: null,
            AccountClass: "Personal");

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
        await EnsureOfflineOperateGrantAsync(ct).ConfigureAwait(false);
        return new AuthResult(true, AuthFailureReason.None, currentUser.Session ?? session);
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
            if (!await IsDeviceOnlineAsync(ct).ConfigureAwait(false))
            {
                // Nominal expiry while offline is not logout — keep stored identity for offline work.
                events.Record("session_nominal_expiry_while_offline", Dict(("userId", shell.UserId.ToString("D"))));
                return await RestoreOfflineOperatingFallbackAsync(shell, ct).ConfigureAwait(false);
            }

            currentUser.Set(shell);
            if (await TryReissueAccessTokenAsync(ct).ConfigureAwait(false)
                && currentUser.Session is { } renewed
                && !string.IsNullOrWhiteSpace(renewed.AccessToken))
            {
                return await RestoreBearerSessionAsync(renewed, ct).ConfigureAwait(false);
            }

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
                // Personal default must not inherit a device-level SelectedOrganizationId from a
                // previous Organization/staff session (logout intentionally keeps that preference).
                Guid? orgId;
                if (AuthSessionWorkspace.IsPersonalDefault(shell))
                {
                    orgId = null;
                    await preferences.ClearOrganizationPreferenceAsync(ct).ConfigureAwait(false);
                }
                else
                {
                    orgId = active.OrganizationId ?? await preferences.GetSelectedOrganizationIdAsync(ct).ConfigureAwait(false);
                }

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
                    shell.AccountProfileId,
                    shell.OrganizationContextLocked,
                    shell.BranchId,
                    shell.PosDeviceId);

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
                if (AuthSessionWorkspace.IsPersonalDefault(shell))
                {
                    restored = await NormalizePersonalDefaultSessionAsync(restored, ct).ConfigureAwait(false);
                    currentUser.Set(restored);
                }
                else if (orgId is Guid restoredOrg)
                {
                    await AlignPlatformOrganizationContextAsync(restored, restoredOrg, ct).ConfigureAwait(false);
                }

                if (hasAccess && orgId is Guid validatedOrg)
                {
                    restored = await EnsurePosDeviceBindingAsync(restored, ct).ConfigureAwait(false);
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
                    // Only wipe a durable grant on explicit server revocation. Personal accounts
                    // never have POS product access; Local Validation introspect often omits
                    // ProductAccessAllowed. Clearing on !hasAccess destroyed PIN eligibility.
                    if (_offlineGrant is not null
                        && ShouldRevokeOfflineGrantOnRestore(restored, active, hasAccess))
                    {
                        await ClearOfflineGrantForCurrentUserAsync(restored.UserId, ct)
                            .ConfigureAwait(false);
                    }
                }

                return new AuthResult(true, AuthFailureReason.None, restored);
            }

            if (IsExplicitOfflineGrantRevocation(introspect.Error?.ErrorCode))
            {
                await ClearOfflineGrantForExplicitRejectionAsync(ct).ConfigureAwait(false);
                events.Record("offline_grant_cleared", Dict(("reason", introspect.Error?.ErrorCode)));
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
        Guid? orgId;
        if (AuthSessionWorkspace.IsPersonalDefault(shell))
        {
            orgId = null;
            await preferences.ClearOrganizationPreferenceAsync(ct).ConfigureAwait(false);
        }
        else
        {
            var selectedOrg = await preferences.GetSelectedOrganizationIdAsync(ct).ConfigureAwait(false);
            orgId = selectedOrg ?? shell.OrganizationId;
        }

        // Already unlocked with PIN in this process — restore operate context without re-prompt.
        if (_offlineGrant is { IsUnlockedThisProcess: true, ActiveUnlockedGrant: { } active }
            && active.UserId == shell.UserId
            && !active.IsExpired(_clock.GetUtcNow()))
        {
            var unlocked = BuildSessionFromGrant(shell, active);
            currentUser.Set(unlocked);
            await OpenLocalContextForGrantAsync(unlocked.UserId, active, ct).ConfigureAwait(false);
            if (active.IsOrganizationScope && active.OrganizationId is Guid unlockedOrg)
            {
                _accessPolicy?.NotifyOfflineUnlock(unlocked.UserId, unlockedOrg);
            }

            return new AuthResult(true, AuthFailureReason.None, unlocked);
        }

        if (_offlineGrant is not null)
        {
            var offer = await _offlineGrant.EvaluateColdStartOfferAsync(ct).ConfigureAwait(false);
            if (offer.CanOfferPinUnlock && offer.Grant is not null)
            {
                if (offer.Grant.UserId != shell.UserId)
                {
                    offer = offer with { CanOfferPinUnlock = false, DenialReasonCode = "offline_user_mismatch" };
                }
                else if (offer.Grant.IsOrganizationScope
                         && orgId is Guid boundOrg
                         && offer.Grant.OrganizationId != boundOrg)
                {
                    // Preference/org mismatch — fail closed to reconnect.
                    offer = offer with { CanOfferPinUnlock = false, DenialReasonCode = "offline_org_mismatch" };
                }
                else if (offer.Grant.IsPersonalScope
                         && orgId is not null
                         && string.Equals(shell.AccountClass, "Organization", StringComparison.OrdinalIgnoreCase))
                {
                    // Do not offer a personal grant while the shell is org-bound.
                    offer = offer with { CanOfferPinUnlock = false, DenialReasonCode = "offline_scope_mismatch" };
                }
            }

            if (offer is { CanOfferPinUnlock: true, Grant: not null })
            {
                var pinPending = BuildSessionFromGrant(shell, offer.Grant) with
                {
                    HasPosAccess = false,
                    AccessReasonCode = "offline_pin_required"
                };
                currentUser.Set(pinPending);
                events.Record("offline_pin_required", Dict(
                    ("userId", shell.UserId.ToString("D")),
                    ("organizationId", offer.Grant.OrganizationId?.ToString("D") ?? string.Empty),
                    ("scopeKind", offer.Grant.ScopeKind.ToString())));
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

    public Task<AuthResult> UnlockOfflineWithPinAsync(string pin, CancellationToken ct = default) =>
        UnlockOfflineWithPinCoreAsync(userId: null, pin, ct);

    public Task<AuthResult> UnlockOfflineWithPinAsync(
        Guid userId,
        string pin,
        CancellationToken ct = default) =>
        UnlockOfflineWithPinCoreAsync(userId, pin, ct);

    public Task<IReadOnlyList<OfflineEnrolledUserSummary>> GetEnrolledOfflineUsersAsync(
        CancellationToken ct = default) =>
        _offlineGrant is null
            ? Task.FromResult<IReadOnlyList<OfflineEnrolledUserSummary>>(
                Array.Empty<OfflineEnrolledUserSummary>())
            : _offlineGrant.GetEnrolledUsersAsync(ct);

    public Task RemoveEnrolledOfflineUserAsync(Guid userId, CancellationToken ct = default) =>
        _offlineGrant is null
            ? Task.CompletedTask
            : _offlineGrant.RemoveEnrolledUserAsync(userId, ct);

    private async Task<AuthResult> UnlockOfflineWithPinCoreAsync(
        Guid? userId,
        string pin,
        CancellationToken ct)
    {
        if (_offlineGrant is null)
        {
            return new AuthResult(false, AuthFailureReason.AccessDenied, SafeMessageKey: "Offline_GrantMissing");
        }

        var unlock = userId is Guid id
            ? await _offlineGrant.UnlockWithPinAsync(id, pin, ct).ConfigureAwait(false)
            : await _offlineGrant.UnlockWithPinAsync(pin, ct).ConfigureAwait(false);
        if (unlock.Status != OfflinePinUnlockStatus.Succeeded || unlock.Grant is null)
        {
            return new AuthResult(
                false,
                AuthFailureReason.AccessDenied,
                SafeMessageKey: unlock.SafeMessageKey ?? "Offline_PinWrong");
        }

        var grant = unlock.Grant;
        if (!grant.IsOrganizationScope && !grant.IsPersonalScope)
        {
            return new AuthResult(false, AuthFailureReason.AccessDenied, SafeMessageKey: "Offline_GrantMissing");
        }

        var shell = currentUser.Session;
        AuthSession? markerSession = null;
        string? marker = null;
        try
        {
            (markerSession, marker) = await sessionStore.LoadAsync(ct).ConfigureAwait(false);
        }
        catch
        {
            // Fall through with in-memory / grant-built shell.
        }

        var baseSession = shell ?? markerSession;
        // Multi-cashier: after logout/lock, rebuild from the unlocked grant when session is missing
        // or belongs to a different enrolled user.
        if (baseSession is null || baseSession.UserId != grant.UserId)
        {
            baseSession = BuildShellFromGrant(grant);
        }

        // Reject cross-use: personal grant must not unlock while the shell is org-bound.
        if (grant.IsPersonalScope
            && baseSession.OrganizationId is not null
            && string.Equals(baseSession.AccountClass, "Organization", StringComparison.OrdinalIgnoreCase))
        {
            return new AuthResult(false, AuthFailureReason.AccessDenied, SafeMessageKey: "Offline_GrantMissing");
        }

        var restored = BuildSessionFromGrant(baseSession, grant);

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
        if (grant.IsOrganizationScope && grant.OrganizationId is Guid orgId)
        {
            await preferences.SetSelectedOrganizationIdAsync(orgId, ct).ConfigureAwait(false);
            await OpenLocalContextAsync(restored.UserId, orgId, ct).ConfigureAwait(false);
            _accessPolicy?.NotifyOfflineUnlock(restored.UserId, orgId);
        }
        else
        {
            await preferences.SetSelectedOrganizationIdAsync(null, ct).ConfigureAwait(false);
            await OpenPersonalLocalContextAsync(restored.UserId, ct).ConfigureAwait(false);
        }

        _offlineSessionUx?.NotifyOfflinePinUnlocked();
        events.Record("offline_pin_unlock_succeeded", Dict(
            ("userId", restored.UserId.ToString("D")),
            ("organizationId", grant.OrganizationId?.ToString("D") ?? string.Empty),
            ("scopeKind", grant.ScopeKind.ToString())));
        return new AuthResult(true, AuthFailureReason.None, restored);
    }

    public Task<OfflinePinSetupResult> SetOfflinePinAsync(string pin, CancellationToken ct = default)
    {
        if (_offlineGrant is null)
        {
            return Task.FromResult(new OfflinePinSetupResult(false, "Offline_GrantMissing"));
        }

        return SetOfflinePinCoreAsync(pin, ct);
    }

    private async Task<OfflinePinSetupResult> SetOfflinePinCoreAsync(string pin, CancellationToken ct)
    {
        await EnsureOfflineOperateGrantAsync(ct).ConfigureAwait(false);
        return await _offlineGrant!.SetPinAsync(pin, ct).ConfigureAwait(false);
    }

    public Task<bool> HasOfflinePinConfiguredAsync(CancellationToken ct = default)
    {
        if (_offlineGrant is null)
        {
            return Task.FromResult(false);
        }

        var userId = currentUser.Session?.UserId;
        if (userId is Guid id && id != Guid.Empty)
        {
            return _offlineGrant.HasPinConfiguredAsync(id, ct);
        }

        return _offlineGrant.HasPinConfiguredAsync(ct);
    }

    public async Task<OfflineColdStartOffer> EvaluateOfflineColdStartOfferAsync(CancellationToken ct = default)
    {
        if (_offlineGrant is null)
        {
            var missing = OfflineColdStartOffer.Denied("offline_grant_missing") with
            {
                EligibilityReason = OfflinePinEligibilityReason.NoStoredIdentity
            };
            RecordOfflinePinEligibility(missing);
            return missing;
        }

        var offer = await _offlineGrant.EvaluateColdStartOfferAsync(ct).ConfigureAwait(false);
        RecordOfflinePinEligibility(offer);
        return offer;
    }

    public async Task EnsureOfflineOperateGrantAsync(CancellationToken ct = default)
    {
        var session = currentUser.Session;
        if (session is null || _offlineGrant is null)
        {
            return;
        }

        if (session.HasPosAccess && session.OrganizationId is not null)
        {
            session = await EnsurePosDeviceBindingAsync(session, ct).ConfigureAwait(false);
            var deviceId = _deviceIdentity is null
                ? string.Empty
                : await _deviceIdentity.GetOrCreateDeviceIdAsync(ct).ConfigureAwait(false);
            await _offlineGrant
                .EstablishFromOnlineSessionAsync(session, deviceId, roleCode: null, ct)
                .ConfigureAwait(false);
            _offlineSessionUx?.ResetSession();
            return;
        }

        if (AuthSessionWorkspace.IsPersonalDefault(session) || session.OrganizationId is null)
        {
            await EstablishPersonalOfflineGrantAsync(session, ct).ConfigureAwait(false);
        }
    }

    private void RecordOfflinePinEligibility(OfflineColdStartOffer offer)
    {
        var pinConfigured = offer.UnlockCandidates?.Any(c => c.HasPinConfigured) == true
                            || offer.CanOfferPinUnlock;
        events.Record("offline_pin_eligibility", Dict(
            ("canOfferPinUnlock", offer.CanOfferPinUnlock ? "true" : "false"),
            ("eligibilityReason", offer.EligibilityReason.ToString()),
            ("denialReasonCode", offer.DenialReasonCode),
            ("hasGrant", offer.Grant is not null ? "true" : "false"),
            ("hasStoredIdentity", (offer.UnlockCandidates?.Count ?? 0) > 0 || offer.Grant is not null
                ? "true"
                : "false"),
            ("hasPinVerifier", pinConfigured ? "true" : "false"),
            ("deviceMatches", offer.EligibilityReason is OfflinePinEligibilityReason.DeviceMismatch
                ? "false"
                : offer.CanOfferPinUnlock ? "true" : null),
            ("grantExpired", offer.EligibilityReason is OfflinePinEligibilityReason.Expired
                ? "true"
                : "false"),
            ("grantRevoked", offer.EligibilityReason is OfflinePinEligibilityReason.Revoked
                ? "true"
                : "false"),
            ("candidateCount", (offer.UnlockCandidates?.Count ?? 0).ToString("D"))));
    }

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
            if (!await IsDeviceOnlineAsync(ct).ConfigureAwait(false))
            {
                events.Record("refresh_deferred_offline", Dict(("reason", "expired_while_offline")));
                return new AuthResult(true, AuthFailureReason.None, existing, SafeMessageKey: "SyncStatus_Offline");
            }

            if (await TryReissueAccessTokenAsync(ct).ConfigureAwait(false)
                && currentUser.Session is { } renewed
                && !string.IsNullOrWhiteSpace(renewed.AccessToken))
            {
                return await RestoreBearerSessionAsync(renewed, ct).ConfigureAwait(false);
            }

            await LogoutAsync(ct).ConfigureAwait(false);
            events.Record("refresh_failure", Dict(("reason", "expired")));
            return new AuthResult(false, AuthFailureReason.SessionExpired, SafeMessageKey: "Auth_SessionExpired");
        }

        if (!string.IsNullOrWhiteSpace(existing.AccessToken))
        {
            var bearer = await RestoreBearerSessionAsync(existing, ct).ConfigureAwait(false);
            if (bearer.Succeeded)
            {
                return bearer;
            }

            if (await IsDeviceOnlineAsync(ct).ConfigureAwait(false)
                && await TryReissueAccessTokenAsync(ct).ConfigureAwait(false)
                && currentUser.Session is { } reissued
                && !string.IsNullOrWhiteSpace(reissued.AccessToken))
            {
                return await RestoreBearerSessionAsync(reissued, ct).ConfigureAwait(false);
            }

            return bearer;
        }

        if (await TryReissueAccessTokenAsync(ct).ConfigureAwait(false)
            && currentUser.Session is { } issued
            && !string.IsNullOrWhiteSpace(issued.AccessToken))
        {
            return await RestoreBearerSessionAsync(issued, ct).ConfigureAwait(false);
        }

        var now = _clock.GetUtcNow();
        var refreshedExpiry = now.Add(SessionLifetime);
        var result = await RebuildSessionAsync(existing.UserId, now, refreshedExpiry, accessToken: null, ct)
            .ConfigureAwait(false);
        if (!result.Succeeded)
        {
            events.Record("refresh_failure", Dict(("reason", result.FailureReason.ToString())));
            if (await IsDeviceOnlineAsync(ct).ConfigureAwait(false))
            {
                await LogoutAsync(ct).ConfigureAwait(false);
            }

            return result with { FailureReason = AuthFailureReason.RefreshFailed };
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<bool> TryReissueAccessTokenAsync(CancellationToken ct = default)
    {
        await _reissueGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var session = currentUser.Session;
            if (session is null || string.IsNullOrWhiteSpace(session.PlatformSessionToken))
            {
                events.Record("token_reissue_skipped", Dict(("reason", "missing_platform_session")));
                return false;
            }

            if (!await IsDeviceOnlineAsync(ct).ConfigureAwait(false))
            {
                events.Record("token_reissue_skipped", Dict(("reason", "offline")));
                return false;
            }

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

            if (!reissue.IsSuccess
                || reissue.Data is null
                || string.IsNullOrWhiteSpace(reissue.Data.AccessToken))
            {
                events.Record("token_reissue_failure", Dict(
                    ("status", reissue.Status.ToString()),
                    ("errorCode", reissue.Error?.ErrorCode)));
                return false;
            }

            var updated = session with
            {
                AccessToken = reissue.Data.AccessToken,
                ExpiresAtUtc = reissue.Data.ExpiresAtUtc == default
                    ? _clock.GetUtcNow().Add(SessionLifetime)
                    : reissue.Data.ExpiresAtUtc
            };

            try
            {
                var (_, marker) = await sessionStore.LoadAsync(ct).ConfigureAwait(false);
                marker ??= Guid.NewGuid().ToString("N");
                await sessionStore.SaveAsync(updated, marker, ct).ConfigureAwait(false);
            }
            catch
            {
                events.Record("secure_storage_failure", Dict(("operation", "token_reissue_save")));
                return false;
            }

            currentUser.Set(updated);
            events.Record("token_reissue_success", Dict(("userId", updated.UserId.ToString("D"))));
            return true;
        }
        finally
        {
            _reissueGate.Release();
        }
    }

    private async Task<bool> IsDeviceOnlineAsync(CancellationToken ct)
    {
        if (_connectivity is null)
        {
            // Fail closed for expiry/reissue decisions when connectivity is unknown (unit tests).
            return true;
        }

        return await _connectivity.IsConnectedAsync(ct).ConfigureAwait(false);
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

        // Sign out clears cloud/session trust but keeps durable offline grant + PIN so the same
        // device can reopen limited offline work without internet.
        await ClearLocalSessionAsync(clearOfflineGrant: false, ct).ConfigureAwait(false);
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

        // Drop org-scoped in-memory UI state. SaleCartService clears itself when
        // ICurrentUserContext.OrganizationId changes (Maui singleton; not injectable here).
        ClearOrgScopedInMemoryState();
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
            EnabledFeatureCodes = null,
            BranchId = null,
            PosDeviceId = null,
            // Local Personal pages require AccountClass=Personal before opening SQLite context.
            // Clear AccountProfileId so EnsurePersonal rebinds the Platform Personal profile.
            AccountClass = "Personal",
            AccountProfileId = null
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
            // Tab switches call this on every Personal page. Do not rewrite SecureStorage
            // when a matching Personal grant is already unlocked in this process.
            if (!HasActivePersonalGrantFor(session.UserId))
            {
                await EstablishPersonalOfflineGrantAsync(session, ct).ConfigureAwait(false);
            }

            await OpenPersonalLocalContextAsync(session.UserId, ct).ConfigureAwait(false);

            // Best-effort Platform Personal profile bind (needed after org → personal).
            // Skip when already bound so Personal tab switches stay local-first.
            if (!string.IsNullOrWhiteSpace(session.PlatformSessionToken)
                && session.AccountProfileId is null)
            {
                var bound = await TryBindPersonalPlatformProfileAsync(session, ct).ConfigureAwait(false);
                if (bound is not null)
                {
                    return new AuthResult(true, AuthFailureReason.None, bound);
                }
            }

            return new AuthResult(true, AuthFailureReason.None, session);
        }

        if (string.IsNullOrWhiteSpace(session.PlatformSessionToken))
        {
            return await ActivateLocalPersonalSessionAsync(session, ct).ConfigureAwait(false);
        }

        var profiles = await accessClient.GetAccountProfilesAsync(ct).ConfigureAwait(false);
        if (!profiles.IsSuccess || profiles.Data is null)
        {
            return await ActivateLocalPersonalSessionAsync(session, ct).ConfigureAwait(false);
        }

        var personalProfile = profiles.Data.FirstOrDefault(p =>
            string.Equals(p.AccountClass, "Personal", StringComparison.OrdinalIgnoreCase)
            && string.Equals(p.Status, "Active", StringComparison.OrdinalIgnoreCase));
        if (personalProfile is null)
        {
            return await ActivateLocalPersonalSessionAsync(session, ct).ConfigureAwait(false);
        }

        var selected = await accessClient
            .SelectAccountProfileAsync(new SelectAccountProfileRequest(personalProfile.Id), ct)
            .ConfigureAwait(false);
        if (!selected.IsSuccess || selected.Data is null)
        {
            // Org → personal must still open local Personal home even when Platform select fails.
            return await ActivateLocalPersonalSessionAsync(session, ct).ConfigureAwait(false);
        }

        var updated = session with
        {
            OrganizationId = null,
            OrganizationDisplayName = null,
            HasPosAccess = false,
            AccessReasonCode = null,
            SubscriptionStatus = null,
            EnabledFeatureCodes = null,
            // Prefer Platform session token from SelectAccountProfile; never blank a valid token.
            PlatformSessionToken = string.IsNullOrWhiteSpace(selected.Data.SessionToken)
                ? session.PlatformSessionToken
                : selected.Data.SessionToken.Trim(),
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
        await EstablishPersonalOfflineGrantAsync(updated, ct).ConfigureAwait(false);
        await OpenPersonalLocalContextAsync(updated.UserId, ct).ConfigureAwait(false);
        events.Record("ensured_personal_profile", Dict(("userId", session.UserId.ToString("D"))));
        return new AuthResult(true, AuthFailureReason.None, updated);
    }

    /// <summary>
    /// Clears org fields, marks AccountClass=Personal, opens local Personal SQLite context.
    /// Used when Platform profile APIs are unavailable so Personal home still loads.
    /// </summary>
    private async Task<AuthResult> ActivateLocalPersonalSessionAsync(AuthSession session, CancellationToken ct)
    {
        var updated = session with
        {
            OrganizationId = null,
            OrganizationDisplayName = null,
            HasPosAccess = false,
            AccessReasonCode = null,
            SubscriptionStatus = null,
            EnabledFeatureCodes = null,
            AccountClass = "Personal"
        };

        try
        {
            var (_, marker) = await sessionStore.LoadAsync(ct).ConfigureAwait(false);
            marker ??= Guid.NewGuid().ToString("N");
            await sessionStore.SaveAsync(updated, marker, ct).ConfigureAwait(false);
        }
        catch
        {
            events.Record("secure_storage_failure", Dict(("operation", "activate_local_personal_save")));
        }

        currentUser.Set(updated);
        await EstablishPersonalOfflineGrantAsync(updated, ct).ConfigureAwait(false);
        await OpenPersonalLocalContextAsync(updated.UserId, ct).ConfigureAwait(false);
        return new AuthResult(true, AuthFailureReason.None, updated);
    }

    private async Task<AuthSession?> TryBindPersonalPlatformProfileAsync(AuthSession session, CancellationToken ct)
    {
        try
        {
            var profiles = await accessClient.GetAccountProfilesAsync(ct).ConfigureAwait(false);
            if (!profiles.IsSuccess || profiles.Data is null)
            {
                return null;
            }

            var personalProfile = profiles.Data.FirstOrDefault(p =>
                string.Equals(p.AccountClass, "Personal", StringComparison.OrdinalIgnoreCase)
                && string.Equals(p.Status, "Active", StringComparison.OrdinalIgnoreCase));
            if (personalProfile is null)
            {
                return null;
            }

            // Already bound — avoid SecureStorage rewrite on every tab.
            if (session.AccountProfileId == personalProfile.Id
                && string.Equals(session.AccountClass, "Personal", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var selected = await accessClient
                .SelectAccountProfileAsync(new SelectAccountProfileRequest(personalProfile.Id), ct)
                .ConfigureAwait(false);
            if (!selected.IsSuccess || selected.Data is null)
            {
                return null;
            }

            var updated = session with
            {
                OrganizationId = null,
                OrganizationDisplayName = null,
                HasPosAccess = false,
                AccessReasonCode = null,
                SubscriptionStatus = null,
                EnabledFeatureCodes = null,
                PlatformSessionToken = string.IsNullOrWhiteSpace(selected.Data.SessionToken)
                    ? session.PlatformSessionToken
                    : selected.Data.SessionToken.Trim(),
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
                events.Record("secure_storage_failure", Dict(("operation", "bind_personal_profile_save")));
                currentUser.Set(updated);
                return updated;
            }

            currentUser.Set(updated);
            events.Record("ensured_personal_profile", Dict(("userId", session.UserId.ToString("D"))));
            return updated;
        }
        catch
        {
            return null;
        }
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
            // Keep current session so auth/organizations can still list under Personal.
            return new AuthResult(true, AuthFailureReason.None, session);
        }

        // Prefer Active Organization; otherwise reactivate a deactivated Organization profile.
        var orgProfile = profiles.Data.FirstOrDefault(p =>
                             string.Equals(p.AccountClass, "Organization", StringComparison.OrdinalIgnoreCase)
                             && string.Equals(p.Status, "Active", StringComparison.OrdinalIgnoreCase))
                         ?? profiles.Data.FirstOrDefault(p =>
                             string.Equals(p.AccountClass, "Organization", StringComparison.OrdinalIgnoreCase));

        if (orgProfile is null)
        {
            // Create/reactivate Organization profile when the user already has memberships
            // (Personal → Organization after Start a Business).
            var memberships = await accessClient.GetUserMembershipsAsync(session.UserId, ct)
                .ConfigureAwait(false);
            var hasActiveMembership = memberships.IsSuccess
                && memberships.Data?.Items.Any(m =>
                    string.Equals(m.Status, "Active", StringComparison.OrdinalIgnoreCase)) == true;
            if (hasActiveMembership)
            {
                var ensured = await accessClient
                    .EnsureAccountProfileAsync(new EnsureAccountProfileRequest("Organization"), ct)
                    .ConfigureAwait(false);
                if (ensured.IsSuccess && ensured.Data is not null)
                {
                    orgProfile = ensured.Data;
                }
            }
        }

        if (orgProfile is null)
        {
            // No Organization profile and no memberships — Personal chooser stays empty honestly.
            return new AuthResult(true, AuthFailureReason.None, session);
        }

        var selected = await accessClient
            .SelectAccountProfileAsync(new SelectAccountProfileRequest(orgProfile.Id), ct)
            .ConfigureAwait(false);
        if (!selected.IsSuccess || selected.Data is null)
        {
            // Do not hard-fail Personal → Organization navigation; listing still works under Personal.
            return new AuthResult(true, AuthFailureReason.None, session);
        }

        var updated = session with
        {
            PlatformSessionToken = string.IsNullOrWhiteSpace(selected.Data.SessionToken)
                ? session.PlatformSessionToken
                : selected.Data.SessionToken.Trim(),
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
        // SaleCartService clears itself when ICurrentUserContext.OrganizationId changes.
        ClearOrgScopedInMemoryState();
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
            EnabledFeatureCodes = enabledFeatureCodes,
            // Never carry branch/device binding from Org A into Org B.
            BranchId = null,
            PosDeviceId = null
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
            EnabledFeatureCodes = enabledFeatureCodes,
            BranchId = null,
            PosDeviceId = null
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
            AccessReasonCode = reasonCode ?? "product_local_role_missing",
            BranchId = null,
            PosDeviceId = null
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
        if (updated.HasPosAccess)
        {
            await EnsureOfflineOperateGrantAsync(ct).ConfigureAwait(false);
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

        var existingShell = await sessionStore.LoadAsync(ct).ConfigureAwait(false);
        var personalDefault = AuthSessionWorkspace.IsPersonalDefault(existingShell.Session);

        Guid? organizationId = null;
        string? organizationName = null;
        var hasAccess = false;
        string? reason = null;

        string? subscriptionStatus = null;
        IReadOnlyList<string>? enabledFeatureCodes = null;

        if (personalDefault)
        {
            await preferences.ClearOrganizationPreferenceAsync(ct).ConfigureAwait(false);
        }
        else
        {
            organizationId = await preferences.GetSelectedOrganizationIdAsync(ct).ConfigureAwait(false);
        }

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
            existingShell.Session?.AccountProfileId,
            existingShell.Session?.OrganizationContextLocked == true,
            existingShell.Session?.BranchId,
            existingShell.Session?.PosDeviceId);

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
                    OrganizationId: null,
                    OrganizationDisplayName: null,
                    issuedAt,
                    expiresAt,
                    HasPosAccess: false,
                    AccessReasonCode: "reconnect_required",
                    AccessToken: accessToken);
            }

            if (AuthSessionWorkspace.IsPersonalDefault(shell))
            {
                return shell with
                {
                    OrganizationId = null,
                    OrganizationDisplayName = null,
                    HasPosAccess = false,
                    AccessReasonCode = "reconnect_required",
                    SubscriptionStatus = null,
                    EnabledFeatureCodes = null,
                    AccessToken = accessToken ?? shell.AccessToken
                };
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

        if (organizationId == PersonalLocalScope.PathIsolationMarker)
        {
            await OpenPersonalLocalContextAsync(userId, ct).ConfigureAwait(false);
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

    private async Task OpenPersonalLocalContextAsync(Guid userId, CancellationToken ct)
    {
        if (_localContext is null)
        {
            return;
        }

        try
        {
            await _localContext.OpenPersonalAsync(userId, ct).ConfigureAwait(false);
        }
        catch
        {
            events.Record("local_context_open_failure", Dict(
                ("userId", userId.ToString("D")),
                ("organizationId", string.Empty),
                ("scopeKind", nameof(OfflineGrantScopeKind.Personal))));
        }
    }

    private async Task OpenLocalContextForGrantAsync(
        Guid userId,
        OfflineOperatingGrant grant,
        CancellationToken ct)
    {
        if (grant.IsPersonalScope)
        {
            await OpenPersonalLocalContextAsync(userId, ct).ConfigureAwait(false);
            return;
        }

        if (grant.OrganizationId is Guid orgId)
        {
            await OpenLocalContextAsync(userId, orgId, ct).ConfigureAwait(false);
        }
    }

    private bool HasActivePersonalGrantFor(Guid userId) =>
        _offlineGrant is { IsUnlockedThisProcess: true, ActiveUnlockedGrant: { } grant }
        && grant.IsPersonalScope
        && grant.UserId == userId;

    private async Task EstablishPersonalOfflineGrantAsync(AuthSession session, CancellationToken ct)
    {
        if (_offlineGrant is null)
        {
            return;
        }

        var deviceId = _deviceIdentity is null
            ? string.Empty
            : await _deviceIdentity.GetOrCreateDeviceIdAsync(ct).ConfigureAwait(false);
        await _offlineGrant
            .EstablishFromOnlineSessionAsync(session, deviceId, roleCode: null, ct)
            .ConfigureAwait(false);
        _offlineSessionUx?.ResetSession();
    }

    private static AuthSession BuildShellFromGrant(OfflineOperatingGrant grant)
    {
        var display = string.IsNullOrWhiteSpace(grant.DisplayName)
            ? (grant.Username ?? "User")
            : grant.DisplayName!;
        var username = string.IsNullOrWhiteSpace(grant.Username)
            ? grant.UserId.ToString("D")
            : grant.Username!;

        if (grant.IsPersonalScope)
        {
            return new AuthSession(
                grant.UserId,
                display,
                username,
                grant.Email ?? string.Empty,
                OrganizationId: null,
                OrganizationDisplayName: PersonalLocalScope.DisplayName,
                IssuedAtUtc: grant.IssuedAtUtc,
                ExpiresAtUtc: grant.ExpiresAtUtc,
                HasPosAccess: false,
                AccessReasonCode: "offline_pin_required",
                SubscriptionStatus: null,
                EnabledFeatureCodes: null,
                AccountClass: "Personal");
        }

        return new AuthSession(
            grant.UserId,
            display,
            username,
            grant.Email ?? string.Empty,
            OrganizationId: grant.OrganizationId,
            OrganizationDisplayName: grant.OrganizationDisplayName,
            IssuedAtUtc: grant.IssuedAtUtc,
            ExpiresAtUtc: grant.ExpiresAtUtc,
            HasPosAccess: false,
            AccessReasonCode: "offline_pin_required",
            SubscriptionStatus: grant.SubscriptionStatus,
            EnabledFeatureCodes: grant.EnabledFeatureCodes,
            AccountClass: "Organization",
            BranchId: grant.BranchId,
            PosDeviceId: grant.PosDeviceId);
    }

    private static AuthSession BuildSessionFromGrant(AuthSession baseSession, OfflineOperatingGrant grant)
    {
        if (grant.IsPersonalScope)
        {
            return baseSession with
            {
                OrganizationId = null,
                OrganizationDisplayName = PersonalLocalScope.DisplayName,
                DisplayName = grant.DisplayName ?? baseSession.DisplayName,
                Username = grant.Username ?? baseSession.Username,
                Email = grant.Email ?? baseSession.Email,
                HasPosAccess = false,
                AccessReasonCode = "offline_grant",
                SubscriptionStatus = null,
                EnabledFeatureCodes = null,
                AccessToken = null,
                PlatformSessionToken = null,
                AccountClass = "Personal",
                BranchId = null,
                PosDeviceId = null
            };
        }

        return baseSession with
        {
            OrganizationId = grant.OrganizationId,
            OrganizationDisplayName = grant.OrganizationDisplayName,
            DisplayName = grant.DisplayName ?? baseSession.DisplayName,
            Username = grant.Username ?? baseSession.Username,
            Email = grant.Email ?? baseSession.Email,
            HasPosAccess = true,
            AccessToken = null,
            PlatformSessionToken = null,
            AccessReasonCode = "offline_grant",
            SubscriptionStatus = grant.SubscriptionStatus ?? baseSession.SubscriptionStatus,
            EnabledFeatureCodes = grant.EnabledFeatureCodes.Count > 0
                ? grant.EnabledFeatureCodes
                : baseSession.EnabledFeatureCodes,
            AccountClass = "Organization",
            BranchId = grant.BranchId ?? baseSession.BranchId,
            PosDeviceId = grant.PosDeviceId ?? baseSession.PosDeviceId
        };
    }

    private async Task<AuthSession> EnsurePosDeviceBindingAsync(AuthSession session, CancellationToken ct)
    {
        if (!session.HasPosAccess
            || session.OrganizationId is not Guid orgId
            || (session.BranchId is not null && session.PosDeviceId is not null)
            || _deviceIdentity is null)
        {
            return session;
        }

        try
        {
            var installationId = await _deviceIdentity.GetOrCreateDeviceIdAsync(ct).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(installationId))
            {
                return session;
            }

            var authorization = await accessClient.AuthorizePosDeviceAsync(
                orgId,
                new AuthorizePosDeviceRequest(installationId),
                ct).ConfigureAwait(false);
            if (!authorization.IsSuccess || authorization.Data is null)
            {
                return session;
            }

            var bound = session with
            {
                BranchId = authorization.Data.BranchId,
                PosDeviceId = authorization.Data.PosDeviceId
            };

            try
            {
                var (_, marker) = await sessionStore.LoadAsync(ct).ConfigureAwait(false);
                marker ??= Guid.NewGuid().ToString("N");
                await sessionStore.SaveAsync(bound, marker, ct).ConfigureAwait(false);
            }
            catch
            {
                events.Record("secure_storage_failure", Dict(("operation", "pos_device_bind_save")));
            }

            currentUser.Set(bound);
            return bound;
        }
        catch
        {
            return session;
        }
    }

    private void ClearOrgScopedInMemoryState()
    {
        // Preferred home / selling mode must not leak Org A → Org B (or into Personal).
        _sellingMode?.Clear();
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

    private Task ClearLocalAsync(CancellationToken ct) =>
        ClearLocalSessionAsync(clearOfflineGrant: true, ct);

    private async Task ClearOfflineGrantForExplicitRejectionAsync(CancellationToken ct)
    {
        if (_offlineGrant is null)
        {
            return;
        }

        try
        {
            var userId = currentUser.Session?.UserId ?? _offlineGrant.ActiveUnlockedGrant?.UserId;
            if (userId is Guid id && id != Guid.Empty)
            {
                await _offlineGrant.ClearUserGrantAsync(id, ct).ConfigureAwait(false);
            }
            else
            {
                await _offlineGrant.ClearAsync(ct).ConfigureAwait(false);
            }
        }
        catch
        {
            // The server rejection remains authoritative; secure-storage failure is handled by the
            // normal next-session path and must not be treated as a network fallback.
        }
    }

    private static bool ShouldRevokeOfflineGrantOnRestore(
        AuthSession restored,
        PlatformAccessTokenIntrospectionDto active,
        bool hasAccess)
    {
        if (hasAccess)
        {
            return false;
        }

        if (AuthSessionWorkspace.IsPersonalDefault(restored) || restored.OrganizationId is null)
        {
            return false;
        }

        if (active.ProductAccessAllowed is not false)
        {
            // Null/omitted ProductAccessAllowed is a partial snapshot, not a revocation.
            return false;
        }

        return IsExplicitProductAccessRevocation(active.ProductAccessReasonCode)
               || IsExplicitOfflineGrantRevocation(active.ProductAccessReasonCode);
    }

    private static bool IsExplicitProductAccessRevocation(string? reasonCode)
    {
        if (string.IsNullOrWhiteSpace(reasonCode))
        {
            return false;
        }

        return reasonCode.Equals("product_assignment_inactive", StringComparison.OrdinalIgnoreCase)
               || reasonCode.Equals("product_assignment_missing", StringComparison.OrdinalIgnoreCase)
               || reasonCode.Equals("entitlement_denied", StringComparison.OrdinalIgnoreCase)
               || reasonCode.Equals("entitlement_missing", StringComparison.OrdinalIgnoreCase)
               || reasonCode.Equals("membership_inactive", StringComparison.OrdinalIgnoreCase)
               || reasonCode.Equals("membership_missing", StringComparison.OrdinalIgnoreCase)
               || reasonCode.Equals("user_inactive", StringComparison.OrdinalIgnoreCase)
               || reasonCode.Equals("organization_inactive", StringComparison.OrdinalIgnoreCase)
               || reasonCode.Equals("product_inactive", StringComparison.OrdinalIgnoreCase);
    }

    private async Task ClearOfflineGrantForCurrentUserAsync(Guid userId, CancellationToken ct)
    {
        if (_offlineGrant is null || userId == Guid.Empty)
        {
            return;
        }

        try
        {
            await _offlineGrant.ClearUserGrantAsync(userId, ct).ConfigureAwait(false);
        }
        catch
        {
            // Best-effort.
        }
    }

    private static bool IsExplicitOfflineGrantRevocation(string? errorCode)
    {
        if (string.IsNullOrWhiteSpace(errorCode))
        {
            return false;
        }

        return errorCode.Equals("application.pos_device.revoked", StringComparison.OrdinalIgnoreCase)
            || errorCode.Equals("application.pos_device.not_authorized", StringComparison.OrdinalIgnoreCase)
            || errorCode.Equals("device_revoked", StringComparison.OrdinalIgnoreCase)
            || errorCode.Equals("PosDeviceNotAuthorized", StringComparison.OrdinalIgnoreCase)
            || errorCode.Contains("membership", StringComparison.OrdinalIgnoreCase)
                && (errorCode.Contains("removed", StringComparison.OrdinalIgnoreCase)
                    || errorCode.Contains("revoked", StringComparison.OrdinalIgnoreCase));
    }

    /// <param name="clearOfflineGrant">
    /// True for hard revoke (server denial / inactive user). False for Sign out — keep grant + PIN
    /// so cold-start PIN unlock remains available offline.
    /// </param>
    private async Task ClearLocalSessionAsync(bool clearOfflineGrant, CancellationToken ct)
    {
        await CloseLocalContextAsync(ct).ConfigureAwait(false);
        _accessPolicy?.ClearProcessValidation();
        _offlineSessionUx?.ResetSession();
        if (_offlineGrant is not null)
        {
            try
            {
                if (clearOfflineGrant)
                {
                    // Hard clear for the signed-in / active user only — other enrolled cashiers remain.
                    var userId = currentUser.Session?.UserId ?? _offlineGrant.ActiveUnlockedGrant?.UserId;
                    if (userId is Guid id && id != Guid.Empty)
                    {
                        await _offlineGrant.ClearUserGrantAsync(id, ct).ConfigureAwait(false);
                    }
                    else
                    {
                        await _offlineGrant.ClearAsync(ct).ConfigureAwait(false);
                    }
                }
                else
                {
                    // Sign out: drop process unlock only; all enrolled grants + PINs stay on device.
                    _offlineGrant.LockThisProcess();
                }
            }
            catch
            {
                // Best-effort grant handling.
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

        // Keep SelectedOrganizationId so the next Organization/staff sign-in can restore
        // the last valid organization context. Personal default sign-in/restore clears it.
        currentUser.Clear();
    }

    /// <summary>
    /// Personal default accounts never inherit a device-level SelectedOrganizationId or a forged
    /// OrganizationId. Organization Owners / staff bind org context explicitly (or via staff lock).
    /// </summary>
    private async Task<AuthSession> NormalizePersonalDefaultSessionAsync(AuthSession session, CancellationToken ct)
    {
        if (!AuthSessionWorkspace.IsPersonalDefault(session))
        {
            return session;
        }

        await preferences.ClearOrganizationPreferenceAsync(ct).ConfigureAwait(false);
        if (session.OrganizationId is null && !session.HasPosAccess)
        {
            return session;
        }

        return session with
        {
            OrganizationId = null,
            OrganizationDisplayName = null,
            HasPosAccess = false,
            AccessReasonCode = null,
            SubscriptionStatus = null,
            EnabledFeatureCodes = null
        };
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
