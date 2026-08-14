using System.Text.RegularExpressions;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Domain.Sales;

/// <summary>
/// Immutable buyer/counterparty snapshot recorded on a POS sale.
/// Seller organization ownership is never inferred from these fields.
/// </summary>
public sealed class SaleBuyerParty
{
    public const int DisplayNameMaxLength = 128;
    public const int PersonalPublicUserIdMaxLength = 12;
    public const int PublicOrganizationIdMaxLength = 9;

    private static readonly Regex PersonalPublicIdPattern = new(
        @"^EX-\d{4}-\d{4}$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex PublicOrganizationIdPattern = new(
        @"^ORG\d{6}$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public SaleBuyerPartyKind Kind { get; }
    public string? DisplayNameSnapshot { get; }
    public string? PersonalPublicUserId { get; }
    public Guid? BuyerOrganizationId { get; }
    public string? BuyerPublicOrganizationId { get; }

    private SaleBuyerParty(
        SaleBuyerPartyKind kind,
        string? displayNameSnapshot,
        string? personalPublicUserId,
        Guid? buyerOrganizationId,
        string? buyerPublicOrganizationId)
    {
        Kind = kind;
        DisplayNameSnapshot = displayNameSnapshot;
        PersonalPublicUserId = personalPublicUserId;
        BuyerOrganizationId = buyerOrganizationId;
        BuyerPublicOrganizationId = buyerPublicOrganizationId;
    }

    public static SaleBuyerParty WalkIn(string? displayNameSnapshot = null) =>
        new(SaleBuyerPartyKind.WalkIn, NormalizeOptionalDisplayName(displayNameSnapshot), null, null, null);

    public static SaleBuyerParty ExternalCustomer(string? displayNameSnapshot) =>
        new(
            SaleBuyerPartyKind.ExternalCustomer,
            NormalizeOptionalDisplayName(displayNameSnapshot)
            ?? throw new DomainException(
                DomainErrorCodes.InvalidSaleBuyerParty,
                "External customer sales require a buyer display name snapshot."),
            null,
            null,
            null);

    public static SaleBuyerParty Personal(string personalPublicUserId, string displayNameSnapshot) =>
        new(
            SaleBuyerPartyKind.Personal,
            NormalizeRequiredDisplayName(displayNameSnapshot),
            NormalizePersonalPublicUserId(personalPublicUserId),
            null,
            null);

    public static SaleBuyerParty Organization(
        Guid buyerOrganizationId,
        string buyerPublicOrganizationId,
        string displayNameSnapshot)
    {
        if (buyerOrganizationId == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSaleBuyerParty,
                "Buyer organization id cannot be empty.");
        }

        return new(
            SaleBuyerPartyKind.Organization,
            NormalizeRequiredDisplayName(displayNameSnapshot),
            null,
            buyerOrganizationId,
            NormalizePublicOrganizationId(buyerPublicOrganizationId));
    }

    /// <summary>
    /// Safe historical default: null customer ⇒ Walk-in; otherwise ExternalCustomer.
    /// Never guesses Personal/Organization identity from names or phones.
    /// </summary>
    public static SaleBuyerParty FromLegacyCustomer(POSCustomerId? customerId, string? displayNameSnapshot = null) =>
        customerId is null
            ? WalkIn(displayNameSnapshot)
            : new(
                SaleBuyerPartyKind.ExternalCustomer,
                NormalizeOptionalDisplayName(displayNameSnapshot) ?? "Customer",
                null,
                null,
                null);

    public static SaleBuyerParty Rehydrate(
        SaleBuyerPartyKind kind,
        string? displayNameSnapshot,
        string? personalPublicUserId,
        Guid? buyerOrganizationId,
        string? buyerPublicOrganizationId) =>
        new(kind, displayNameSnapshot, personalPublicUserId, buyerOrganizationId, buyerPublicOrganizationId);

    public static SaleBuyerPartyKind ParseKind(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return SaleBuyerPartyKind.WalkIn;
        }

        if (Enum.TryParse<SaleBuyerPartyKind>(value.Trim(), ignoreCase: true, out var kind))
        {
            return kind;
        }

        throw new DomainException(
            DomainErrorCodes.InvalidSaleBuyerParty,
            "Buyer party kind is invalid.");
    }

    public static string ToCode(SaleBuyerPartyKind kind) => kind.ToString();

    /// <summary>
    /// Ensures party fields are consistent with an optional seller-owned customer id.
    /// Utang still requires a customer id at payment validation — that is separate.
    /// </summary>
    public void EnsureConsistentWith(POSCustomerId? customerId)
    {
        switch (Kind)
        {
            case SaleBuyerPartyKind.WalkIn:
                if (customerId is not null)
                {
                    throw new DomainException(
                        DomainErrorCodes.InvalidSaleBuyerParty,
                        "Walk-in sales must not attach a customer id.");
                }

                if (PersonalPublicUserId is not null
                    || BuyerOrganizationId is not null
                    || BuyerPublicOrganizationId is not null)
                {
                    throw new DomainException(
                        DomainErrorCodes.InvalidSaleBuyerParty,
                        "Walk-in sales cannot carry ExItS buyer identity fields.");
                }

                break;

            case SaleBuyerPartyKind.ExternalCustomer:
                if (customerId is null)
                {
                    throw new DomainException(
                        DomainErrorCodes.InvalidSaleBuyerParty,
                        "External customer sales require a seller-owned customer.");
                }

                if (PersonalPublicUserId is not null
                    || BuyerOrganizationId is not null
                    || BuyerPublicOrganizationId is not null)
                {
                    throw new DomainException(
                        DomainErrorCodes.InvalidSaleBuyerParty,
                        "External customer sales cannot carry ExItS buyer identity fields on the sale.");
                }

                break;

            case SaleBuyerPartyKind.Personal:
                if (string.IsNullOrWhiteSpace(PersonalPublicUserId))
                {
                    throw new DomainException(
                        DomainErrorCodes.InvalidSaleBuyerParty,
                        "Personal buyer sales require a Personal public user id.");
                }

                if (BuyerOrganizationId is not null || BuyerPublicOrganizationId is not null)
                {
                    throw new DomainException(
                        DomainErrorCodes.InvalidSaleBuyerParty,
                        "Personal buyer sales cannot carry Organization buyer identity.");
                }

                break;

            case SaleBuyerPartyKind.Organization:
                if (BuyerOrganizationId is null || BuyerOrganizationId == Guid.Empty
                    || string.IsNullOrWhiteSpace(BuyerPublicOrganizationId))
                {
                    throw new DomainException(
                        DomainErrorCodes.InvalidSaleBuyerParty,
                        "Organization buyer sales require buyer organization identity.");
                }

                if (PersonalPublicUserId is not null)
                {
                    throw new DomainException(
                        DomainErrorCodes.InvalidSaleBuyerParty,
                        "Organization buyer sales cannot carry Personal buyer identity.");
                }

                break;

            default:
                throw new DomainException(
                    DomainErrorCodes.InvalidSaleBuyerParty,
                    "Unknown buyer party kind.");
        }
    }

    private static string NormalizePersonalPublicUserId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSaleBuyerParty,
                "Personal public user id is required.");
        }

        var trimmed = value.Trim().ToUpperInvariant();
        if (trimmed.Length > PersonalPublicUserIdMaxLength || !PersonalPublicIdPattern.IsMatch(trimmed))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSaleBuyerParty,
                "Personal public user id format is invalid.");
        }

        return trimmed;
    }

    private static string NormalizePublicOrganizationId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSaleBuyerParty,
                "Buyer public organization id is required.");
        }

        var trimmed = value.Trim().ToUpperInvariant();
        if (trimmed.Length > PublicOrganizationIdMaxLength || !PublicOrganizationIdPattern.IsMatch(trimmed))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSaleBuyerParty,
                "Buyer public organization id must match ORG######.");
        }

        return trimmed;
    }

    private static string NormalizeRequiredDisplayName(string value)
    {
        var normalized = NormalizeOptionalDisplayName(value);
        if (normalized is null)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSaleBuyerParty,
                "Buyer display name snapshot is required.");
        }

        return normalized;
    }

    private static string? NormalizeOptionalDisplayName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > DisplayNameMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSaleBuyerParty,
                $"Buyer display name must be at most {DisplayNameMaxLength} characters.");
        }

        return trimmed;
    }
}
