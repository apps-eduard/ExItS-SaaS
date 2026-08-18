using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Auth;

namespace ExItS.PinoyBusinessPOS.ApiClient;

/// <summary>
/// Adds <c>X-Pos-Organization-Id</c> from the current authenticated organization session.
/// Required for POS customer API organization isolation.
/// </summary>
public sealed class PosOrganizationHeaderHandler(ICurrentUserContext currentUser) : DelegatingHandler
{
    public const string HeaderName = "X-Pos-Organization-Id";
    public const string BranchHeaderName = "X-Pos-Branch-Id";

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (currentUser.Session?.OrganizationId is { } organizationId && organizationId != Guid.Empty)
        {
            request.Headers.Remove(HeaderName);
            request.Headers.TryAddWithoutValidation(HeaderName, organizationId.ToString("D"));
        }

        if (AuthSessionBranchContext.GetSelectedBranchId(currentUser.Session) is { } branchId)
        {
            request.Headers.Remove(BranchHeaderName);
            request.Headers.TryAddWithoutValidation(BranchHeaderName, branchId.ToString("D"));
        }

        return base.SendAsync(request, cancellationToken);
    }
}
