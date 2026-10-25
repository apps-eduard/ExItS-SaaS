using System.Security.Claims;

namespace ExItS.PinoyBusinessPOS.Api.Common;

/// <summary>
/// Maps bearer introspection items to <see cref="ClaimsPrincipal"/> for SignalR [Authorize] and hub user id.
/// </summary>
internal sealed class PosClaimsPrincipalMiddleware(RequestDelegate next)
{
    public const string AuthenticationType = "PosBearer";

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User?.Identity?.IsAuthenticated != true
            && context.Items.TryGetValue(PosAuthItems.UserId, out var value)
            && value is Guid userId
            && userId != Guid.Empty)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId.ToString("D")),
            };

            if (context.Items.TryGetValue(PosAuthItems.OrganizationId, out var orgValue)
                && orgValue is Guid orgId
                && orgId != Guid.Empty)
            {
                claims.Add(new Claim("pos_organization_id", orgId.ToString("D")));
            }

            context.User = new ClaimsPrincipal(
                new ClaimsIdentity(claims, AuthenticationType));
        }

        await next(context).ConfigureAwait(false);
    }
}
