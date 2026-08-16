using System.Text.RegularExpressions;
using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;

/// <summary>
/// Immutable customer/counterparty snapshot on a customer order.
/// Exactly one party identity is required: Personal (Platform user) or Organization buyer.
/// Seller organization ownership is never inferred from these fields.
/// </summary>
public sealed class CustomerOrderParty
{
    public const int DisplayNameMaxLength = 128;
    public const int PublicOrganizationIdMaxLength = 9;

    private static readonly Regex PublicOrganizationIdPattern = new(
        @"^ORG\d{6}$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public CustomerPartyType PartyType { get; }
    public string DisplayNameSnapshot { get; }
    public Guid? PlatformUserId { get; }
    public Guid? BuyerOrganizationId { get; }
    public string? BuyerPublicOrganizationId { get; }

    private CustomerOrderParty(
        CustomerPartyType partyType,
        string displayNameSnapshot,
        Guid? platformUserId,
        Guid? buyerOrganizationId,
        string? buyerPublicOrganizationId)
    {
        PartyType = partyType;
        DisplayNameSnapshot = displayNameSnapshot;
        PlatformUserId = platformUserId;
        BuyerOrganizationId = buyerOrganizationId;
        BuyerPublicOrganizationId = buyerPublicOrganizationId;
    }

    public static CustomerOrderParty Personal(Guid platformUserId, string displayNameSnapshot)
    {
        if (platformUserId == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderParty,
                "Personal customer orders require a Platform user id.");
        }

        return new(
            CustomerPartyType.Personal,
            NormalizeRequiredDisplayName(displayNameSnapshot),
            platformUserId,
            null,
            null);
    }

    public static CustomerOrderParty Organization(
        Guid buyerOrganizationId,
        string buyerPublicOrganizationId,
        string displayNameSnapshot)
    {
        if (buyerOrganizationId == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderParty,
                "Buyer organization id cannot be empty.");
        }

        return new(
            CustomerPartyType.Organization,
            NormalizeRequiredDisplayName(displayNameSnapshot),
            null,
            buyerOrganizationId,
            NormalizePublicOrganizationId(buyerPublicOrganizationId));
    }

    public static CustomerOrderParty Rehydrate(
        CustomerPartyType partyType,
        string displayNameSnapshot,
        Guid? platformUserId,
        Guid? buyerOrganizationId,
        string? buyerPublicOrganizationId) =>
        new(partyType, displayNameSnapshot, platformUserId, buyerOrganizationId, buyerPublicOrganizationId);

    /// <summary>Ensures party fields are mutually exclusive and complete for the party type.</summary>
    public void EnsureConsistent()
    {
        switch (PartyType)
        {
            case CustomerPartyType.Personal:
                if (PlatformUserId is null || PlatformUserId == Guid.Empty)
                {
                    throw new DomainException(
                        DomainErrorCodes.InvalidCustomerOrderParty,
                        "Personal customer orders require a Platform user id.");
                }

                if (BuyerOrganizationId is not null || BuyerPublicOrganizationId is not null)
                {
                    throw new DomainException(
                        DomainErrorCodes.InvalidCustomerOrderParty,
                        "Personal customer orders cannot carry Organization buyer identity.");
                }

                break;

            case CustomerPartyType.Organization:
                if (BuyerOrganizationId is null || BuyerOrganizationId == Guid.Empty
                    || string.IsNullOrWhiteSpace(BuyerPublicOrganizationId))
                {
                    throw new DomainException(
                        DomainErrorCodes.InvalidCustomerOrderParty,
                        "Organization customer orders require buyer organization identity.");
                }

                if (PlatformUserId is not null)
                {
                    throw new DomainException(
                        DomainErrorCodes.InvalidCustomerOrderParty,
                        "Organization customer orders cannot carry Personal buyer identity.");
                }

                break;

            default:
                throw new DomainException(
                    DomainErrorCodes.InvalidCustomerOrderParty,
                    "Unknown or unsupported customer party type.");
        }

        if (string.IsNullOrWhiteSpace(DisplayNameSnapshot))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderParty,
                "Customer display name snapshot is required.");
        }
    }

    private static string NormalizePublicOrganizationId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderParty,
                "Buyer public organization id is required.");
        }

        var trimmed = value.Trim().ToUpperInvariant();
        if (trimmed.Length > PublicOrganizationIdMaxLength || !PublicOrganizationIdPattern.IsMatch(trimmed))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderParty,
                "Buyer public organization id must match ORG######.");
        }

        return trimmed;
    }

    private static string NormalizeRequiredDisplayName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderParty,
                "Customer display name snapshot is required.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length > DisplayNameMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderParty,
                $"Customer display name must be at most {DisplayNameMaxLength} characters.");
        }

        return trimmed;
    }
}
