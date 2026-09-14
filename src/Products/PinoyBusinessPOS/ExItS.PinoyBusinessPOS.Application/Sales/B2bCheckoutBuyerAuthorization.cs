using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Application.Sales;

public sealed record B2bCheckoutBuyerResolution(
    SaleBuyerParty BuyerParty,
    Guid ConnectionId);

/// <summary>
/// Derives Organization buyer identity for Direct Sale checkout from a B2B relationship.
/// Pending relationships may attach for immediate (non-receivable) payments only;
/// Utang/credit requires an Active relationship.
/// </summary>
public static class B2bCheckoutBuyerAuthorization
{
    public static async Task<ApplicationResult<B2bCheckoutBuyerResolution>> ResolveAsync(
        PosOrganizationId sellerOrganizationId,
        IConnectedSupplierRelationshipRepository relationships,
        Guid? buyerConnectionId,
        Guid? requestedBuyerOrganizationId,
        Guid? customerId,
        bool isUtang,
        CancellationToken cancellationToken = default)
    {
        var createsReceivable = isUtang;

        if (customerId is Guid linked && linked != Guid.Empty)
        {
            return ApplicationResult<B2bCheckoutBuyerResolution>.Failure(
                DomainErrorCodes.InvalidSaleBuyerParty,
                "B2B Organization checkout must not attach a POS customer id.");
        }

        ConnectedSupplierRelationship? relationship = null;
        if (buyerConnectionId is Guid connectionId && connectionId != Guid.Empty)
        {
            relationship = await relationships
                .GetAsync(ConnectedSupplierRelationshipId.From(connectionId), cancellationToken)
                .ConfigureAwait(false);
            if (relationship is null
                || relationship.SupplierOrganizationId != sellerOrganizationId)
            {
                return ApplicationResult<B2bCheckoutBuyerResolution>.Failure(
                    DomainErrorCodes.SaleB2bRelationshipRequired,
                    "B2B relationship was not found.");
            }
        }
        else if (requestedBuyerOrganizationId is Guid buyerOrg && buyerOrg != Guid.Empty)
        {
            relationship = await relationships
                .FindOpenAsync(
                    PosOrganizationId.From(buyerOrg),
                    sellerOrganizationId,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        if (relationship is null)
        {
            return ApplicationResult<B2bCheckoutBuyerResolution>.Failure(
                DomainErrorCodes.SaleB2bRelationshipRequired,
                "B2B relationship was not found.");
        }

        if (relationship.Status == ConnectedSupplierRelationshipStatus.Pending)
        {
            if (createsReceivable)
            {
                return ApplicationResult<B2bCheckoutBuyerResolution>.Failure(
                    DomainErrorCodes.SaleB2bCreditRequiresAcceptedRelationship,
                    "Credit is unavailable while this relationship is pending. The buyer must accept the connection before Utang can be used.");
            }
        }
        else if (relationship.Status != ConnectedSupplierRelationshipStatus.Active)
        {
            return ApplicationResult<B2bCheckoutBuyerResolution>.Failure(
                DomainErrorCodes.SaleB2bRelationshipRequired,
                "Active B2B relationship was not found.");
        }

        if (relationship.BuyerOrganizationId == sellerOrganizationId)
        {
            return ApplicationResult<B2bCheckoutBuyerResolution>.Failure(
                DomainErrorCodes.InvalidSaleBuyerParty,
                "You can't sell to your own business as a B2B Organization buyer.");
        }

        var displayName = string.IsNullOrWhiteSpace(relationship.BuyerDisplayNameSnapshot)
            ? relationship.BuyerPublicOrganizationIdSnapshot
            : relationship.BuyerDisplayNameSnapshot;
        if (string.IsNullOrWhiteSpace(displayName)
            || string.IsNullOrWhiteSpace(relationship.BuyerPublicOrganizationIdSnapshot))
        {
            return ApplicationResult<B2bCheckoutBuyerResolution>.Failure(
                DomainErrorCodes.InvalidSaleBuyerParty,
                "B2B Organization buyer identity is incomplete on the relationship.");
        }

        try
        {
            var party = SaleBuyerParty.Organization(
                relationship.BuyerOrganizationId.Value,
                relationship.BuyerPublicOrganizationIdSnapshot!,
                displayName!);
            return ApplicationResult<B2bCheckoutBuyerResolution>.Success(
                new B2bCheckoutBuyerResolution(party, relationship.Id.Value));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<B2bCheckoutBuyerResolution>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}
