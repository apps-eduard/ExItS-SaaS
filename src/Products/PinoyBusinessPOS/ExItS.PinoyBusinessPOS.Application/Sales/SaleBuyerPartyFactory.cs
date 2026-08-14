using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Application.Sales;

/// <summary>
/// Builds a validated <see cref="SaleBuyerParty"/> from checkout request fields + optional seller customer.
/// Never treats buyer identity as sale ownership.
/// </summary>
public static class SaleBuyerPartyFactory
{
    public static ApplicationResult<SaleBuyerParty> TryCreate(
        string? buyerPartyKind,
        string? buyerDisplayNameSnapshot,
        string? buyerPersonalPublicUserId,
        Guid? buyerOrganizationId,
        string? buyerPublicOrganizationId,
        POSCustomer? customer)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(buyerPartyKind))
            {
                return ApplicationResult<SaleBuyerParty>.Success(
                    SaleBuyerParty.FromLegacyCustomer(
                        customer?.Id,
                        customer?.DisplayName ?? buyerDisplayNameSnapshot));
            }

            var kind = SaleBuyerParty.ParseKind(buyerPartyKind);
            var display = !string.IsNullOrWhiteSpace(buyerDisplayNameSnapshot)
                ? buyerDisplayNameSnapshot
                : customer?.DisplayName;

            SaleBuyerParty party = kind switch
            {
                SaleBuyerPartyKind.WalkIn => SaleBuyerParty.WalkIn(display),
                SaleBuyerPartyKind.ExternalCustomer => SaleBuyerParty.ExternalCustomer(
                    display ?? throw new DomainException(
                        DomainErrorCodes.InvalidSaleBuyerParty,
                        "External customer sales require a buyer display name.")),
                SaleBuyerPartyKind.Personal => SaleBuyerParty.Personal(
                    PreferLinkedPersonal(buyerPersonalPublicUserId, customer),
                    display ?? throw new DomainException(
                        DomainErrorCodes.InvalidSaleBuyerParty,
                        "Personal buyer sales require a display name.")),
                SaleBuyerPartyKind.Organization => SaleBuyerParty.Organization(
                    PreferLinkedBuyerOrgId(buyerOrganizationId, customer),
                    PreferLinkedBuyerPublicOrgId(buyerPublicOrganizationId, customer),
                    display ?? throw new DomainException(
                        DomainErrorCodes.InvalidSaleBuyerParty,
                        "Organization buyer sales require a display name.")),
                _ => throw new DomainException(
                    DomainErrorCodes.InvalidSaleBuyerParty,
                    "Unknown buyer party kind.")
            };

            return ApplicationResult<SaleBuyerParty>.Success(party);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<SaleBuyerParty>.Failure(ex.ErrorCode, ex.Message);
        }
    }

    private static string PreferLinkedPersonal(string? requested, POSCustomer? customer)
    {
        if (!string.IsNullOrWhiteSpace(requested))
        {
            return requested;
        }

        if (!string.IsNullOrWhiteSpace(customer?.LinkedPersonalPublicUserId))
        {
            return customer!.LinkedPersonalPublicUserId!;
        }

        throw new DomainException(
            DomainErrorCodes.InvalidSaleBuyerParty,
            "Personal buyer sales require a Personal public user id.");
    }

    private static Guid PreferLinkedBuyerOrgId(Guid? requested, POSCustomer? customer)
    {
        if (requested is not null && requested != Guid.Empty)
        {
            return requested.Value;
        }

        if (customer?.LinkedBuyerOrganizationId is not null)
        {
            return customer.LinkedBuyerOrganizationId.Value;
        }

        throw new DomainException(
            DomainErrorCodes.InvalidSaleBuyerParty,
            "Organization buyer sales require a buyer organization id.");
    }

    private static string PreferLinkedBuyerPublicOrgId(string? requested, POSCustomer? customer)
    {
        if (!string.IsNullOrWhiteSpace(requested))
        {
            return requested;
        }

        if (!string.IsNullOrWhiteSpace(customer?.LinkedBuyerPublicOrganizationId))
        {
            return customer!.LinkedBuyerPublicOrganizationId!;
        }

        throw new DomainException(
            DomainErrorCodes.InvalidSaleBuyerParty,
            "Organization buyer sales require a public organization id.");
    }
}
