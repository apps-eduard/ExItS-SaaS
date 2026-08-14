using System.Net.Http.Json;
using System.Security.Claims;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Auth;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Offline;
using ExItS.PinoyBusinessPOS.Application.Platform;
using ExItS.Web.UI;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Server.Circuits;

namespace ExItS.PinoyBusinessPOS.Web.Services;

public sealed class WebAppInfoService(IHostEnvironment environment) : IAppInfoService
{
    public string AppName => "ExItS Organization Admin";
    public string Version => "0.1.0";
    public string EnvironmentName => environment.EnvironmentName;
}

public sealed class AlwaysOnlineConnectivity : IConnectivityService
{
    public Task<bool> IsConnectedAsync(CancellationToken ct = default) => Task.FromResult(true);

    public event EventHandler<ConnectivityStatus>? ConnectivityChanged
    {
        add { }
        remove { }
    }
}

public sealed class WebNoOpSyncStatus : IPosSyncStatusService
{
    public PosSyncStatusSnapshot Current { get; } = new(PosSyncStatusKind.Online);

    public event Func<Task>? Changed
    {
        add { }
        remove { }
    }

    public Task InitializeAsync(CancellationToken ct = default) => Task.CompletedTask;

    public void SetReconnectRequired(bool required)
    {
    }

    public void SetRecoveryRequired(bool required)
    {
    }

    public void NotifyApiReachability(bool reachable)
    {
    }

    public void Refresh()
    {
    }
}

public sealed class WebDeviceIdentityProvider : IDeviceIdentityProvider
{
    /// <summary>
    /// Browser management clients are not POS terminals. This id is not a registered POS device
    /// and must not be treated as offline or checkout authorization.
    /// </summary>
    public Task<string> GetOrCreateDeviceIdAsync(CancellationToken ct = default) =>
        Task.FromResult("aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee");
}

public sealed class MemorySecureTokenStore : ISecureTokenStore
{
    private readonly Dictionary<string, string> _values = new(StringComparer.Ordinal);

    public Task<string?> GetAsync(string key, CancellationToken ct = default) =>
        Task.FromResult(_values.TryGetValue(key, out var value) ? value : null);

    public Task SetAsync(string key, string value, CancellationToken ct = default)
    {
        _values[key] = value;
        return Task.CompletedTask;
    }

    public Task ClearAsync(string key, CancellationToken ct = default)
    {
        _values.Remove(key);
        return Task.CompletedTask;
    }

    public Task ClearAllSessionKeysAsync(CancellationToken ct = default)
    {
        _values.Clear();
        return Task.CompletedTask;
    }
}

public sealed class OrgWebCircuitSession
{
    public string? SessionToken { get; set; }
}

public sealed class OrgWebSessionCircuitHandler(
    OrgWebCircuitSession circuitSession,
    IHttpContextAccessor httpContextAccessor) : CircuitHandler
{
    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        try
        {
            var http = httpContextAccessor.HttpContext;
            if (http is null)
            {
                return Task.CompletedTask;
            }

            var token = OrgWebBrowserSessionService.ResolveSessionToken(http);
            if (!string.IsNullOrWhiteSpace(token))
            {
                circuitSession.SessionToken = token;
            }
        }
        catch
        {
            // Never fail circuit open.
        }

        return Task.CompletedTask;
    }
}

public sealed class OrgWebBrowserSessionService(
    IHttpClientFactory httpClientFactory,
    IHttpContextAccessor httpContextAccessor,
    IHostEnvironment environment)
{
    public const string SessionTokenClaimType = "exits_session_token";
    public const string SessionTokenCookieName = ".ExItS.OrgWeb.Session";
    public const string CookieScheme = CookieAuthenticationDefaults.AuthenticationScheme;

    public static string? ResolveSessionToken(HttpContext http) =>
        http.User.FindFirstValue(SessionTokenClaimType)
        ?? (http.Request.Cookies.TryGetValue(SessionTokenCookieName, out var cookie) ? cookie : null);

    public async Task<(bool Ok, string? Error)> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var http = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HTTP context is required for login.");

        var client = httpClientFactory.CreateClient("PlatformApiUnauthenticated");
        using var response = await client.PostAsJsonAsync(
            "/api/v1/platform/auth/login",
            new PlatformLoginRequest(email, password),
            ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return (false, "Invalid email or password.");
        }

        var login = await response.Content.ReadFromJsonAsync<PlatformLoginResultDto>(ct).ConfigureAwait(false);
        if (login is null || string.IsNullOrWhiteSpace(login.SessionToken))
        {
            return (false, "Login response was invalid.");
        }

        await EstablishBrowserSessionAsync(
            http,
            login.UserId,
            login.Username,
            login.Email,
            login.SessionToken,
            login.ExpiresAtUtc).ConfigureAwait(false);
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> EstablishFromSessionTokenAsync(
        string sessionToken,
        CancellationToken ct = default)
    {
        var http = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HTTP context is required to establish the browser session.");
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            return (false, "Session token is missing.");
        }

        var client = httpClientFactory.CreateClient("PlatformApiUnauthenticated");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/platform/auth/me");
        request.Headers.TryAddWithoutValidation("X-ExItS-Session-Token", sessionToken);
        using var response = await client.SendAsync(request, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return (false, "Session is invalid.");
        }

        var me = await response.Content.ReadFromJsonAsync<PlatformLoginResultDto>(ct).ConfigureAwait(false);
        if (me is null)
        {
            return (false, "Session response was invalid.");
        }

        await EstablishBrowserSessionAsync(
            http,
            me.UserId,
            me.Username,
            me.Email,
            sessionToken,
            me.ExpiresAtUtc).ConfigureAwait(false);
        return (true, null);
    }

    public async Task<(bool Ok, string? Error, string? ReturnPath)> RedeemHandoffAsync(
        string ticket,
        CancellationToken ct = default)
    {
        var client = httpClientFactory.CreateClient("PlatformApiUnauthenticated");
        var redeemed = await WebHandoffHttp.RedeemAsync(client, ticket, ct).ConfigureAwait(false);
        if (redeemed is null || string.IsNullOrWhiteSpace(redeemed.SessionToken))
        {
            return (false, "Handoff ticket is invalid or expired.", null);
        }

        if (!string.Equals(redeemed.TargetApp, WebApps.Organization, StringComparison.OrdinalIgnoreCase))
        {
            return (false, "Handoff ticket is not for Organization Web.", null);
        }

        var established = await EstablishFromSessionTokenAsync(redeemed.SessionToken, ct).ConfigureAwait(false);
        return (established.Ok, established.Error, redeemed.ReturnPath);
    }

    public async Task LogoutAsync(CancellationToken ct = default)
    {
        var http = httpContextAccessor.HttpContext;
        if (http is null)
        {
            return;
        }

        var token = ResolveSessionToken(http);
        if (!string.IsNullOrWhiteSpace(token))
        {
            var client = httpClientFactory.CreateClient("PlatformApiUnauthenticated");
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/platform/auth/logout");
            request.Headers.TryAddWithoutValidation("X-ExItS-Session-Token", token);
            try
            {
                await client.SendAsync(request, ct).ConfigureAwait(false);
            }
            catch
            {
                // Local cookie sign-out still proceeds.
            }
        }

        http.Response.Cookies.Delete(SessionTokenCookieName);
        await http.SignOutAsync(CookieScheme).ConfigureAwait(false);
    }

    private async Task EstablishBrowserSessionAsync(
        HttpContext http,
        Guid userId,
        string username,
        string? email,
        string sessionToken,
        DateTimeOffset expiresAtUtc)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString("D")),
            new(ClaimTypes.Name, username),
            new(ClaimTypes.Email, email ?? string.Empty),
            new(SessionTokenClaimType, sessionToken)
        };

        var identity = new ClaimsIdentity(claims, CookieScheme);
        var principal = new ClaimsPrincipal(identity);
        await http.SignInAsync(
            CookieScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = expiresAtUtc,
                AllowRefresh = true
            }).ConfigureAwait(false);

        http.Response.Cookies.Append(
            SessionTokenCookieName,
            sessionToken,
            new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = !(environment.IsDevelopment() || environment.IsEnvironment("Testing")),
                Expires = expiresAtUtc
            });
    }
}

public sealed class OrgWebShellState
{
    public bool Ready { get; set; }
    public string? Error { get; set; }
    public string? MembershipRole { get; set; }
    public IReadOnlyList<PlatformAuthEligibleOrganizationDto> Organizations { get; set; } = [];
    public IReadOnlyList<string> AllowedCapabilities { get; set; } = [];
    public int UnreadNotificationCount { get; set; }
    public bool SidebarCollapsed { get; set; }
    public bool DrawerOpen { get; set; }

    public bool IsOrgOwner =>
        OrganizationMembershipRoles.IsOwnerRole(MembershipRole);

    public bool Can(UtangCapability capability) =>
        AllowedCapabilities.Contains(capability.ToString(), StringComparer.OrdinalIgnoreCase);

    public bool CanSee(string section) => section switch
    {
        "overview" => IsOrgOwner || Can(UtangCapability.ViewDashboard),
        "profile" or "branches" or "staff" or "subscription" or "notifications" or "ownership-transfer" => IsOrgOwner,
        "roles" => IsOrgOwner,
        "products" => Can(UtangCapability.ViewCatalog),
        "inventory" => Can(UtangCapability.ViewInventory),
        "customers" => Can(UtangCapability.ViewCustomersAndHistory),
        "devices" or "registers" => Can(UtangCapability.ViewRegisters) || IsOrgOwner,
        "shifts" => Can(UtangCapability.ViewShifts),
        "reports" => Can(UtangCapability.ViewReports) || Can(UtangCapability.ViewDashboard),
        "settings" => Can(UtangCapability.ViewOperationalSetup) || IsOrgOwner,
        _ => false
    };
}

public sealed class OrgWebSessionHydrator(
    ICurrentUserContext currentUser,
    OrgWebCircuitSession circuitSession,
    OrgWebShellState shell,
    IPlatformAccessClient platform,
    IPosPermissionClient permissions)
{
    public async Task HydrateAsync(CancellationToken ct = default)
    {
        shell.Error = null;
        var token = circuitSession.SessionToken;
        if (string.IsNullOrWhiteSpace(token))
        {
            currentUser.Clear();
            shell.Ready = true;
            return;
        }

        if (currentUser.Session is null ||
            !string.Equals(currentUser.Session.PlatformSessionToken, token, StringComparison.Ordinal))
        {
            currentUser.Set(new AuthSession(
                Guid.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                OrganizationId: null,
                OrganizationDisplayName: null,
                IssuedAtUtc: DateTimeOffset.UtcNow,
                ExpiresAtUtc: DateTimeOffset.UtcNow.AddHours(1),
                HasPosAccess: false,
                AccessReasonCode: null,
                PlatformSessionToken: token));
        }

        var me = await platform.GetAuthMeAsync(ct).ConfigureAwait(false);
        if (!me.IsSuccess || me.Data is null)
        {
            currentUser.Clear();
            shell.Error = me.Error?.Detail ?? "Session expired.";
            shell.Ready = true;
            return;
        }

        var orgs = await platform.GetAuthEligibleOrganizationsAsync(ct).ConfigureAwait(false);
        shell.Organizations = orgs.IsSuccess && orgs.Data is not null ? orgs.Data : [];

        var selected = me.Data.SelectedOrganizationId
            ?? shell.Organizations.FirstOrDefault()?.OrganizationId;
        if (selected is Guid orgId && orgId != Guid.Empty)
        {
            if (me.Data.SelectedOrganizationId != orgId)
            {
                await platform.SetOrganizationContextAsync(new SetOrganizationContextRequest(orgId), ct)
                    .ConfigureAwait(false);
            }

            var org = shell.Organizations.FirstOrDefault(o => o.OrganizationId == orgId);
            shell.MembershipRole = org?.MembershipRole;

            var tokenResult = await platform.IssueTokenAsync(
                new IssuePlatformAccessTokenRequest(
                    GrantType: "session",
                    UsernameOrEmail: null,
                    Password: null,
                    OrganizationId: orgId,
                    ProductCode: PosProductCodes.PinoyBusinessPos),
                ct).ConfigureAwait(false);

            var access = await platform.EvaluateAccessAsync(
                me.Data.UserId,
                orgId,
                PosProductCodes.PinoyBusinessPos,
                ct).ConfigureAwait(false);

            var hasPos = tokenResult.IsSuccess
                && tokenResult.Data is not null
                && access.IsSuccess
                && access.Data?.Allowed == true;

            currentUser.Set(new AuthSession(
                me.Data.UserId,
                me.Data.DisplayName,
                me.Data.Username,
                me.Data.Email,
                orgId,
                org?.DisplayName ?? me.Data.SelectedOrganizationDisplayName,
                DateTimeOffset.UtcNow,
                me.Data.ExpiresAtUtc,
                hasPos,
                access.Data?.ReasonCode,
                access.Data?.SubscriptionStatus,
                EnabledFeatureCodes: null,
                AccessToken: tokenResult.Data?.AccessToken,
                PlatformSessionToken: token,
                AccountClass: me.Data.AccountClass,
                AccountProfileId: me.Data.AccountProfileId,
                OrganizationContextLocked: false));

            if (hasPos)
            {
                var effective = await permissions.GetEffectiveAsync(ct).ConfigureAwait(false);
                if (effective.IsSuccess && effective.Data is not null)
                {
                    shell.AllowedCapabilities = effective.Data.AllowedCapabilities;
                    currentUser.Set(currentUser.Session! with
                    {
                        EnabledFeatureCodes = effective.Data.AllowedFeatureCodes
                    });
                }
            }
            else if (shell.IsOrgOwner)
            {
                shell.AllowedCapabilities = [];
            }

            var notes = await platform.GetOrganizationNotificationsAsync(orgId, ct).ConfigureAwait(false);
            if (notes.IsSuccess && notes.Data is not null)
            {
                shell.UnreadNotificationCount = notes.Data.Count(n => !n.IsRead);
            }
        }
        else
        {
            currentUser.Set(new AuthSession(
                me.Data.UserId,
                me.Data.DisplayName,
                me.Data.Username,
                me.Data.Email,
                OrganizationId: null,
                OrganizationDisplayName: null,
                IssuedAtUtc: DateTimeOffset.UtcNow,
                ExpiresAtUtc: me.Data.ExpiresAtUtc,
                HasPosAccess: false,
                AccessReasonCode: "organization_required",
                PlatformSessionToken: token,
                AccountClass: me.Data.AccountClass,
                AccountProfileId: me.Data.AccountProfileId));
        }

        shell.Ready = true;
    }
}
