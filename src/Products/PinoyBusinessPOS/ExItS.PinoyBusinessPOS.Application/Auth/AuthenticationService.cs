using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Auth;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
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
    TimeProvider? timeProvider = null) : IAuthenticationService
{
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(12);
    private readonly TimeProvider _clock = timeProvider ?? TimeProvider.System;
    private readonly ILocalContextManager? _localContext = localContext;
    private readonly IProtectedShellAccessPolicy? _accessPolicy = accessPolicy;

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
                    AccountProfileId: login.AccountProfileId);

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
            AccountProfileId: accountProfileId);

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
                    active.SubscriptionStatus,
                    active.EnabledFeatureCodes,
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
                if (hasAccess && orgId is Guid validatedOrg)
                {
                    await OpenLocalContextAsync(restored.UserId, validatedOrg, ct).ConfigureAwait(false);
                }
                else
                {
                    await CloseLocalContextAsync(ct).ConfigureAwait(false);
                }

                return new AuthResult(true, AuthFailureReason.None, restored);
            }
        }
        catch
        {
            // Fall through to expiry-only restore when introspect is unreachable.
        }

        // Offline / introspect unavailable: keep durable bearer session shell (expiry already checked).
        var selectedOrg = await preferences.GetSelectedOrganizationIdAsync(ct).ConfigureAwait(false);
        var offlineShell = shell with
        {
            OrganizationId = selectedOrg ?? shell.OrganizationId,
            HasPosAccess = false,
            AccessReasonCode = "reconnect_required"
        };
        currentUser.Set(offlineShell);
        await CloseLocalContextAsync(ct).ConfigureAwait(false);
        return new AuthResult(false, AuthFailureReason.Offline, offlineShell, SafeMessageKey: "SyncStatus_Reconnect");
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
        return new AuthResult(true, AuthFailureReason.None, updated);
    }

    public async Task<AuthResult> SelectOrganizationAsync(Guid organizationId, CancellationToken ct = default)
    {
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

        var updated = session with
        {
            OrganizationId = organizationId,
            OrganizationDisplayName = displayName,
            HasPosAccess = true,
            AccessReasonCode = accessResult.Data.ReasonCode ?? "allowed",
            SubscriptionStatus = accessResult.Data.SubscriptionStatus,
            EnabledFeatureCodes = accessResult.Data.EnabledFeatureCodes
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
                ("reason", transport.SafeMessageKey)));
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
            enabledFeatureCodes = MergeDevelopmentGrants(enabledFeatureCodes);
        }

        return (subscriptionStatus, enabledFeatureCodes);
    }

    private static IReadOnlyList<string> MergeDevelopmentGrants(IReadOnlyList<string>? existing)
    {
        if (existing is not { Count: > 0 })
        {
            return UtangCapabilityPolicy.DefaultDevelopmentGrants;
        }

        var merged = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);
        foreach (var code in UtangCapabilityPolicy.DefaultDevelopmentGrants)
        {
            merged.Add(code);
        }

        return merged.OrderBy(c => c, StringComparer.OrdinalIgnoreCase).ToArray();
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
        if (error?.Detail?.Contains("Product-local role", StringComparison.OrdinalIgnoreCase) == true)
        {
            return "product_local_role_missing";
        }

        return "product_assignment_missing";
    }

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
        await OpenLocalContextAsync(updated.UserId, organizationId, ct).ConfigureAwait(false);
        // SelectOrganization clears process validation first; re-arm once POS access is bound.
        _accessPolicy?.NotifySessionAccessChanged();
        events.Record("organization_selected", Dict(
            ("userId", previous.UserId.ToString("D")),
            ("organizationId", organizationId.ToString("D"))));
        return new AuthResult(true, AuthFailureReason.None, updated);
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
