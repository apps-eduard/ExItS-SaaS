using ExItS.Platform.Application.Access;
using ExItS.Platform.Application.Audit;
using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Entitlements;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Application.Subscriptions;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Payments;

namespace ExItS.Platform.Application.Payments;

public sealed class HostedCheckoutSubscriptionActivator(
    IPlanRepository plans,
    ActivatePaidSubscription activatePaid,
    GenerateEntitlementSnapshot generateSnapshot,
    RecordLinkedSuccessfulProviderPayment recordLinkedPayment,
    GrantProductAccess grantProductAccess,
    IProductLocalRoleGrantRepository roleGrants,
    IAuditWriter auditWriter,
    IPaymentProvider paymentProvider,
    IClock clock) : IHostedCheckoutSubscriptionActivator
{
    public Task<ApplicationResult> ActivateAsync(
        SubscriptionPaymentTransaction payment,
        CancellationToken cancellationToken = default) =>
        SubscriptionPaymentActivation.ActivateForAttachedOrganizationAsync(
            payment,
            payment.InitiatedByUserId,
            plans,
            activatePaid,
            generateSnapshot,
            recordLinkedPayment,
            grantProductAccess,
            roleGrants,
            auditWriter,
            paymentProvider,
            clock.UtcNow,
            cancellationToken);
}
