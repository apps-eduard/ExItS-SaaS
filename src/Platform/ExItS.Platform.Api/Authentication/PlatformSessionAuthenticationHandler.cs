using System.Security.Claims;
using System.Text.Encodings.Web;
using ExItS.Platform.Application.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace ExItS.Platform.Api.Authentication;

public static class PlatformSessionDefaults
{
    public const string AuthenticationScheme = PlatformSessionClaimTypes.AuthenticationScheme;
    public const string SessionIdClaimType = PlatformSessionClaimTypes.SessionId;
    public const string AuthorizationScheme = PlatformSessionClaimTypes.AuthenticationScheme;
}

public sealed class PlatformSessionAuthenticationOptions : AuthenticationSchemeOptions
{
}

public sealed class PlatformSessionAuthenticationHandler : AuthenticationHandler<PlatformSessionAuthenticationOptions>
{
    private readonly ValidateAndRenewPlatformSession _validate;
    private readonly PlatformSessionOptions _sessionOptions;

    public PlatformSessionAuthenticationHandler(
        IOptionsMonitor<PlatformSessionAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ValidateAndRenewPlatformSession validate,
        IOptions<PlatformSessionOptions> sessionOptions)
        : base(options, logger, encoder)
    {
        _validate = validate;
        _sessionOptions = sessionOptions.Value;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var token = ExtractToken(Request);
        if (string.IsNullOrWhiteSpace(token))
        {
            return AuthenticateResult.NoResult();
        }

        var result = await _validate.ExecuteAsync(token, Context.RequestAborted).ConfigureAwait(false);
        if (!result.IsSuccess || result.Value is null)
        {
            return AuthenticateResult.NoResult();
        }

        var info = result.Value;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, info.UserId.ToString("D")),
            new(ClaimTypes.Name, info.Username),
            new(PlatformSessionDefaults.SessionIdClaimType, info.SessionId.ToString("D"))
        };
        if (info.AccountProfileId is Guid accountProfileId)
        {
            claims.Add(new Claim(PlatformSessionClaimTypes.AccountProfileId, accountProfileId.ToString("D")));
        }

        if (!string.IsNullOrWhiteSpace(info.AccountClass))
        {
            claims.Add(new Claim(PlatformSessionClaimTypes.AccountClass, info.AccountClass));
        }

        if (!string.IsNullOrWhiteSpace(info.AllowedScope))
        {
            claims.Add(new Claim(PlatformSessionClaimTypes.AllowedScope, info.AllowedScope));
        }

        if (info.SelectedOrganizationId is Guid organizationId)
        {
            claims.Add(new Claim(PlatformSessionClaimTypes.OrganizationId, organizationId.ToString("D")));
        }

        var identity = new ClaimsIdentity(claims, PlatformSessionDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, PlatformSessionDefaults.AuthenticationScheme);
        Context.Items[PlatformSessionClaimTypes.RequestTokenItemKey] = token;
        return AuthenticateResult.Success(ticket);
    }

    private string? ExtractToken(HttpRequest request)
    {
        if (request.Cookies.TryGetValue(_sessionOptions.CookieName, out var cookieToken)
            && !string.IsNullOrWhiteSpace(cookieToken))
        {
            return cookieToken;
        }

        if (request.Headers.TryGetValue(_sessionOptions.SessionTokenHeaderName, out var headerValues))
        {
            var headerToken = headerValues.ToString();
            if (!string.IsNullOrWhiteSpace(headerToken))
            {
                return headerToken;
            }
        }

        var authorization = request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(authorization)
            && authorization.StartsWith(PlatformSessionDefaults.AuthorizationScheme + " ", StringComparison.OrdinalIgnoreCase))
        {
            return authorization[(PlatformSessionDefaults.AuthorizationScheme.Length + 1)..].Trim();
        }

        return null;
    }
}
