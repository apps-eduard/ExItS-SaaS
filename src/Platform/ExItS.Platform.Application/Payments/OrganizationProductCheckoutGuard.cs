using ExItS.Platform.Application.Access;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Subscriptions;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Domain.Subscriptions;

namespace ExItS.Platform.Application.Payments;

/// <summary>
/// Resolves the organization for a subscription checkout and refuses a second checkout
/// when that organization already has an active or trialing subscription.
/// </summary>
public sealed record CheckoutAffiliationDecision(
    PlatformOrganizationId? OrganizationId,
    string? ErrorCode,
    string? ErrorMessage)
{
    public bool IsBlocked => ErrorCode is not null;

    public static CheckoutAffiliationDecision Allow(PlatformOrganizationId? organizationId) =>
        new(organizationId, null, null);

    public static CheckoutAffiliationDecision Block(string errorCode, string errorMessage) =>
        new(null, errorCode, errorMessage);
}

public sealed class OrganizationProductCheckoutGuard(
    ISubscriptionRepository subscriptions,
    UserProductAffiliationGuard affiliations)
{
    public const string ManagedByOrganizationMessage =
        "Subscription is managed by the organization.";

    public const string ActiveSubscriptionMessage =
        "This organization already has a subscription for this product.";

    public async Task<CheckoutAffiliationDecision> ResolveAsync(
        PlatformUserId userId,
        ProductCode productCode,
        PlatformOrganizationId? requestedOrganizationId,
        CancellationToken cancellationToken = default)
    {
        var affiliation = await affiliations
            .FindAsync(userId, productCode, cancellationToken)
            .ConfigureAwait(false);

        if (requestedOrganizationId is not null)
        {
            if (affiliation is null || affiliation.OrganizationId != requestedOrganizationId.Value)
            {
                return CheckoutAffiliationDecision.Block(
                    DomainErrorCodes.AuthorizationDenied,
                    "You do not have an active affiliation with this organization for this product.");
            }
        }

        var organizationId = requestedOrganizationId
            ?? (affiliation is null ? null : PlatformOrganizationId.From(affiliation.OrganizationId));

        if (affiliation is not null && !affiliation.CanManageBilling)
        {
            return CheckoutAffiliationDecision.Block(
                DomainErrorCodes.AuthorizationDenied,
                ManagedByOrganizationMessage);
        }

        if (organizationId is not null)
        {
            var current = await subscriptions
                .GetCurrentForOrganizationProductAsync(organizationId, productCode, cancellationToken)
                .ConfigureAwait(false);
            if (current is not null
                && current.Status is SubscriptionStatus.Active or SubscriptionStatus.Trialing)
            {
                return CheckoutAffiliationDecision.Block(
                    ApplicationErrorCodes.ActiveSubscriptionConflict,
                    ActiveSubscriptionMessage);
            }
        }

        return CheckoutAffiliationDecision.Allow(organizationId);
    }
}
