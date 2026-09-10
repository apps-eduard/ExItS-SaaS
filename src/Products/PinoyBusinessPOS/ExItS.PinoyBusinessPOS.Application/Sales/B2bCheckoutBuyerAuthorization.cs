using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Application.Sales;

/// <summary>
/// Derives Organization buyer identity for Direct Sale checkout from an Active B2B relationship.
/// </summary>
public static class B2bCheckoutBuyerAuthorization
{
    public static async Task<ApplicationResult<SaleBuyerParty>> ResolveAsync(
        PosOrganizationId sellerOrganizationId,
        IConnectedSupplierRelationshipRepository relationships,
        Guid? buyerConnectionId,
        Guid? requestedBuyerOrganizationId,
        Guid? customerId,
        bool isUtang,
        CancellationToken cancellationToken = default)
    {
        if (isUtang)
        {
            return ApplicationResult<SaleBuyerParty>.Failure(
                DomainErrorCodes.SaleB2bUtangNotSupported,
                "Direct credit is not configured for this business. Use Cash, GCash, or the Purchase Order payment terms.");
        }

        if (customerId is Guid linked && linked != Guid.Empty)
        {
            return ApplicationResult<SaleBuyerParty>.Failure(
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
                return ApplicationResult<SaleBuyerParty>.Failure(
                    DomainErrorCodes.SaleB2bRelationshipRequired,
                    "Active B2B relationship was not found.");
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

        if (relationship is null
            || relationship.Status != ConnectedSupplierRelationshipStatus.Active)
        {
            return ApplicationResult<SaleBuyerParty>.Failure(
                DomainErrorCodes.SaleB2bRelationshipRequired,
                "Active B2B relationship was not found.");
        }

        if (relationship.BuyerOrganizationId == sellerOrganizationId)
        {
            return ApplicationResult<SaleBuyerParty>.Failure(
                DomainErrorCodes.InvalidSaleBuyerParty,
                "You can't sell to your own business as a B2B Organization buyer.");
        }

        var displayName = string.IsNullOrWhiteSpace(relationship.BuyerDisplayNameSnapshot)
            ? relationship.BuyerPublicOrganizationIdSnapshot
            : relationship.BuyerDisplayNameSnapshot;
        if (string.IsNullOrWhiteSpace(displayName)
            || string.IsNullOrWhiteSpace(relationship.BuyerPublicOrganizationIdSnapshot))
        {
            return ApplicationResult<SaleBuyerParty>.Failure(
                DomainErrorCodes.InvalidSaleBuyerParty,
                "B2B Organization buyer identity is incomplete on the relationship.");
        }

        try
        {
            return ApplicationResult<SaleBuyerParty>.Success(
                SaleBuyerParty.Organization(
                    relationship.BuyerOrganizationId.Value,
                    relationship.BuyerPublicOrganizationIdSnapshot!,
                    displayName!));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<SaleBuyerParty>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}
