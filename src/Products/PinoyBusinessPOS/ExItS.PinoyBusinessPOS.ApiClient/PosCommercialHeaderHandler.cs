using ExItS.PinoyBusinessPOS.Application.Abstractions;

namespace ExItS.PinoyBusinessPOS.ApiClient;

/// <summary>
/// Adds commercial entitlement headers from the current session for POS business API authorization.
/// Development-stage headers only — not production authentication.
/// </summary>
public sealed class PosCommercialHeaderHandler(ICurrentUserContext currentUser) : DelegatingHandler
{
    public const string SubscriptionStatusHeaderName = "X-Pos-Subscription-Status";
    public const string FeatureGrantsHeaderName = "X-Pos-Feature-Grants";

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var session = currentUser.Session;
        if (session is not null)
        {
            if (!string.IsNullOrWhiteSpace(session.SubscriptionStatus))
            {
                request.Headers.Remove(SubscriptionStatusHeaderName);
                request.Headers.TryAddWithoutValidation(SubscriptionStatusHeaderName, session.SubscriptionStatus.Trim());
            }

            if (session.EnabledFeatureCodes is { Count: > 0 })
            {
                var grants = string.Join(',', session.EnabledFeatureCodes
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .Select(c => c.Trim()));
                if (!string.IsNullOrWhiteSpace(grants))
                {
                    request.Headers.Remove(FeatureGrantsHeaderName);
                    request.Headers.TryAddWithoutValidation(FeatureGrantsHeaderName, grants);
                }
            }
        }

        return base.SendAsync(request, cancellationToken);
    }
}
