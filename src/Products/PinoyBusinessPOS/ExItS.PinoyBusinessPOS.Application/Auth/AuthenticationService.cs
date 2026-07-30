using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Auth;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Platform;

namespace ExItS.PinoyBusinessPOS.Application.Auth;

/// <summary>
/// Development/Testing authentication using Platform User Id selection and
/// <c>X-Dev-Platform-User-Id</c>. Disabled outside Development/Testing. Not production authentication.
/// </summary>
public sealed class AuthenticationService(
    IAppInfoService appInfo,
    ISessionStore sessionStore,
    ICurrentUserContext currentUser,
    IOnboardingPreferenceStore preferences,
    IPlatformAccessClient accessClient,
    IAuthEventSink events,
    ILocalContextManager? localContext = null,
    TimeProvider? timeProvider = null) : IAuthenticationService
{
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(12);
    private readonly TimeProvider _clock = timeProvider ?? TimeProvider.System;
    private readonly ILocalContextManager? _localContext = localContext;

    public bool IsDevelopmentAuthenticationEnabled =>
        string.Equals(appInfo.EnvironmentName, "Development", StringComparison.OrdinalIgnoreCase)
        || string.Equals(appInfo.EnvironmentName, "Testing", StringComparison.OrdinalIgnoreCase);

    public async Task<AuthResult> SignInAsync(SignInRequest request, CancellationToken ct = default)
    {
        if (!IsDevelopmentAuthenticationEnabled)
        {
            events.Record("signin_blocked_production", EmptyProps());
            return new AuthResult(false, AuthFailureReason.ProductionAuthUnavailable, SafeMessageKey: "Auth_ProductionUnavailable");
        }

        if (request.PlatformUserId == Guid.Empty)
        {
            events.Record("signin_failure", Dict(("reason", "invalid_user_id")));
            return new AuthResult(false, AuthFailureReason.InvalidCredentials, SafeMessageKey: "Auth_InvalidCredentials");
        }

        var userResult = await accessClient.GetUserAsync(request.PlatformUserId, ct).ConfigureAwait(false);
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
        events.Record("signin_success", Dict(("userId", session.UserId.ToString("D"))));
        return new AuthResult(true, AuthFailureReason.None, session);
    }

    public async Task<AuthResult> RestoreSessionAsync(CancellationToken ct = default)
    {
        if (!IsDevelopmentAuthenticationEnabled)
        {
            await ClearLocalAsync(ct).ConfigureAwait(false);
            return new AuthResult(false, AuthFailureReason.ProductionAuthUnavailable, SafeMessageKey: "Auth_ProductionUnavailable");
        }

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

        if (shell.ExpiresAtUtc <= _clock.GetUtcNow())
        {
            await LogoutAsync(ct).ConfigureAwait(false);
            events.Record("session_expired", EmptyProps());
            return new AuthResult(false, AuthFailureReason.SessionExpired, SafeMessageKey: "Auth_SessionExpired");
        }

        // Close any leftover in-memory local context; reopen only after online access validation.
        await CloseLocalContextAsync(ct).ConfigureAwait(false);
        return await RebuildSessionAsync(shell.UserId, shell.IssuedAtUtc, shell.ExpiresAtUtc, ct).ConfigureAwait(false);
    }

    public async Task<AuthResult> RefreshSessionAsync(CancellationToken ct = default)
    {
        if (!IsDevelopmentAuthenticationEnabled)
        {
            return new AuthResult(false, AuthFailureReason.ProductionAuthUnavailable, SafeMessageKey: "Auth_ProductionUnavailable");
        }

        var existing = currentUser.Session;
        if (existing is null)
        {
            return await RestoreSessionAsync(ct).ConfigureAwait(false);
        }

        if (existing.ExpiresAtUtc <= _clock.GetUtcNow())
        {
            await LogoutAsync(ct).ConfigureAwait(false);
            events.Record("refresh_failure", Dict(("reason", "expired")));
            return new AuthResult(false, AuthFailureReason.SessionExpired, SafeMessageKey: "Auth_SessionExpired");
        }

        var now = _clock.GetUtcNow();
        var refreshedExpiry = now.Add(SessionLifetime);
        var result = await RebuildSessionAsync(existing.UserId, now, refreshedExpiry, ct).ConfigureAwait(false);
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
        await ClearLocalAsync(ct).ConfigureAwait(false);
    }

    public async Task<AuthResult> SelectOrganizationAsync(Guid organizationId, CancellationToken ct = default)
    {
        var session = currentUser.Session;
        if (session is null)
        {
            return new AuthResult(false, AuthFailureReason.SessionExpired, SafeMessageKey: "Auth_SessionExpired");
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
        events.Record("organization_selected", Dict(
            ("userId", session.UserId.ToString("D")),
            ("organizationId", organizationId.ToString("D"))));
        return new AuthResult(true, AuthFailureReason.None, updated);
    }

    private async Task<AuthResult> RebuildSessionAsync(
        Guid userId,
        DateTimeOffset issuedAt,
        DateTimeOffset expiresAt,
        CancellationToken ct)
    {
        var userResult = await accessClient.GetUserAsync(userId, ct).ConfigureAwait(false);
        if (!userResult.IsSuccess || userResult.Data is null)
        {
            // Offline / transport failure: keep durable session secrets but deny protected access
            // until online revalidation. Do not open local context from SQLite alone.
            if (userResult.Status is ApiCallStatus.Offline or ApiCallStatus.Timeout or ApiCallStatus.Cancelled)
            {
                var offlineSession = await BuildUnvalidatedShellAsync(userId, issuedAt, expiresAt, ct)
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
            enabledFeatureCodes);

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
                    AccessReasonCode: "reconnect_required");
            }

            return shell with
            {
                HasPosAccess = false,
                AccessReasonCode = "reconnect_required",
                SubscriptionStatus = null,
                EnabledFeatureCodes = null
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

    private async Task ClearLocalAsync(CancellationToken ct)
    {
        await CloseLocalContextAsync(ct).ConfigureAwait(false);

        try
        {
            await sessionStore.ClearAsync(ct).ConfigureAwait(false);
        }
        catch
        {
            events.Record("secure_storage_failure", Dict(("operation", "clear")));
        }

        await preferences.ClearOrganizationPreferenceAsync(ct).ConfigureAwait(false);
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
