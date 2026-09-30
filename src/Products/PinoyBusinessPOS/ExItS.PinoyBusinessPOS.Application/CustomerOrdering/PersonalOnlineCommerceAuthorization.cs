using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Application.CustomerOrdering;

/// <summary>
/// Effective Personal online shopping authorization:
/// seller master (entitlement + Connected Commerce Online) → active Platform link →
/// POSCustomer OnlineOrderingAccess. Storefront also fails closed when no branch Online is ON.
/// Branch open-now / place readiness remain caller-specific (place/quote).
/// </summary>
public sealed class PersonalOnlineCommerceAuthorization
{
    private readonly ISellerCustomerOrderingCapability _sellerCapability;
    private readonly ILinkedCustomerPlatformAuthorization _linkedCustomerAuth;
    private readonly IPOSCustomerRepository _customers;

    public PersonalOnlineCommerceAuthorization(
        ISellerCustomerOrderingCapability sellerCapability,
        ILinkedCustomerPlatformAuthorization linkedCustomerAuth,
        IPOSCustomerRepository customers)
    {
        _sellerCapability = sellerCapability;
        _linkedCustomerAuth = linkedCustomerAuth;
        _customers = customers;
    }

    public sealed record Context(
        POSCustomer PosCustomer,
        Guid PlatformBusinessCustomerId,
        Guid PersonalUserId,
        CustomerOnlineOrderingAccess Access,
        bool MasterAcceptingOrders,
        bool AllowDeliveryBeyondNormalDistance);

    public async Task<ApplicationResult<Context>> AuthorizeShoppingAsync(
        Guid sellerOrganizationId,
        Guid personalPlatformUserId,
        Guid platformBusinessCustomerId,
        CancellationToken cancellationToken = default)
    {
        if (sellerOrganizationId == Guid.Empty
            || personalPlatformUserId == Guid.Empty
            || platformBusinessCustomerId == Guid.Empty)
        {
            return ApplicationResult<Context>.Failure(
                ApplicationErrorCodes.LinkedCustomerNotFound,
                "Linked customer was not found.");
        }

        var capability = await _sellerCapability
            .ResolveAsync(sellerOrganizationId, cancellationToken)
            .ConfigureAwait(false);
        var masterOn = capability.CanCustomerOrder;

        var platform = await _linkedCustomerAuth
            .VerifyAsync(sellerOrganizationId, platformBusinessCustomerId, cancellationToken)
            .ConfigureAwait(false);
        if (platform.Outcome != LinkedCustomerPlatformAuthorizationOutcome.Authorized
            || platform.Proof is null
            || platform.Proof.PersonalUserId != personalPlatformUserId
            || platform.Proof.OrganizationId != sellerOrganizationId
            || platform.Proof.PlatformBusinessCustomerId != platformBusinessCustomerId)
        {
            return ApplicationResult<Context>.Failure(
                ApplicationErrorCodes.LinkedCustomerNotFound,
                "Linked customer was not found.");
        }

        var orgId = PosOrganizationId.From(sellerOrganizationId);
        var posCustomer = await _customers
            .FindByPlatformBusinessCustomerIdAsync(orgId, platformBusinessCustomerId, cancellationToken)
            .ConfigureAwait(false);
        if (posCustomer is null || posCustomer.Status != CustomerStatus.Active)
        {
            return ApplicationResult<Context>.Failure(
                ApplicationErrorCodes.LinkedCustomerNotFound,
                "Linked customer was not found.");
        }

        var access = posCustomer.OnlineOrderingAccess;
        if (!CustomerOnlineOrderingAccessRules.IsShoppingAllowed(masterOn, access))
        {
            var message = CustomerOnlineOrderingAccessRules.DenialMessage(masterOn, access)
                ?? CustomerOnlineOrderingAccessRules.StoreNotAcceptingMessage;
            var code = !masterOn
                ? ApplicationErrorCodes.CustomerOrderOrderingUnavailable
                : ApplicationErrorCodes.CustomerOrderCustomerBlocked;
            return ApplicationResult<Context>.Failure(code, message);
        }

        return ApplicationResult<Context>.Success(
            new Context(
                posCustomer,
                platformBusinessCustomerId,
                personalPlatformUserId,
                access,
                masterOn,
                platform.Proof.AllowDeliveryBeyondNormalDistance));
    }
}
