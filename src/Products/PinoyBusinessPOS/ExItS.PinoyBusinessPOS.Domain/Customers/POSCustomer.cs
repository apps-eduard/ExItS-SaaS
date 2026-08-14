using System.Text.RegularExpressions;
using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Customers;

/// <summary>
/// Organization-owned POS customer aggregate. Profile and lifecycle only —
/// no credit, ledger, balance, repayment, sales, or inventory state.
/// Notes are general identification only and must never be treated as credit records.
/// </summary>
public sealed class POSCustomer
{
    public const int DisplayNameMaxLength = 128;
    public const int AddressMaxLength = 256;
    public const int NotesMaxLength = 512;
    public const int MobileMaxLength = 32;

    private static readonly Regex DisplayNamePattern = new(
        @"^[\p{L}\p{N}][\p{L}\p{N} .'\-]{0,126}[\p{L}\p{N}.]?$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public POSCustomerId Id { get; }
    public PosOrganizationId OrganizationId { get; }
    public string DisplayName { get; private set; }
    public string? MobileNumber { get; private set; }
    public string? NormalizedMobile { get; private set; }
    public string? Address { get; private set; }
    public string? Notes { get; private set; }
    public CustomerStatus Status { get; private set; }
    /// <summary>
    /// Optional Platform <c>BusinessCustomerId</c> correlation value (not a cross-database FK).
    /// Null for legacy POS customers that have no Platform BusinessCustomer.
    /// </summary>
    public Guid? PlatformBusinessCustomerId { get; private set; }

    /// <summary>
    /// Optional ExItS Personal public user id (EX-####-####) linked as counterparty identity.
    /// Seller-owned customer record remains owned by <see cref="OrganizationId"/>.
    /// Mutually exclusive with organization buyer link.
    /// </summary>
    public string? LinkedPersonalPublicUserId { get; private set; }

    /// <summary>
    /// Optional ExItS buyer Organization id (Platform/POS org Guid value correlation).
    /// Mutually exclusive with Personal link.
    /// </summary>
    public Guid? LinkedBuyerOrganizationId { get; private set; }

    /// <summary>Optional public organization id (ORG######) for the linked buyer organization.</summary>
    public string? LinkedBuyerPublicOrganizationId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private POSCustomer(
        POSCustomerId id,
        PosOrganizationId organizationId,
        string displayName,
        string? mobileNumber,
        string? normalizedMobile,
        string? address,
        string? notes,
        CustomerStatus status,
        Guid? platformBusinessCustomerId,
        string? linkedPersonalPublicUserId,
        Guid? linkedBuyerOrganizationId,
        string? linkedBuyerPublicOrganizationId,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        DisplayName = displayName;
        MobileNumber = mobileNumber;
        NormalizedMobile = normalizedMobile;
        Address = address;
        Notes = notes;
        Status = status;
        PlatformBusinessCustomerId = platformBusinessCustomerId;
        LinkedPersonalPublicUserId = linkedPersonalPublicUserId;
        LinkedBuyerOrganizationId = linkedBuyerOrganizationId;
        LinkedBuyerPublicOrganizationId = linkedBuyerPublicOrganizationId;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static POSCustomer Create(
        PosOrganizationId organizationId,
        string displayName,
        DateTimeOffset utcNow,
        string? mobileNumber = null,
        string? address = null,
        string? notes = null,
        POSCustomerId? id = null,
        Guid? platformBusinessCustomerId = null,
        string? linkedPersonalPublicUserId = null,
        Guid? linkedBuyerOrganizationId = null,
        string? linkedBuyerPublicOrganizationId = null)
    {
        EnsureUtc(utcNow);
        var (displayMobile, normalizedMobile) = NormalizeOptionalMobile(mobileNumber);
        var (personalLink, buyerOrgId, buyerPublicOrgId) = NormalizeExItsIdentityLinks(
            linkedPersonalPublicUserId,
            linkedBuyerOrganizationId,
            linkedBuyerPublicOrganizationId);

        return new POSCustomer(
            id ?? POSCustomerId.New(),
            organizationId,
            NormalizeDisplayName(displayName),
            displayMobile,
            normalizedMobile,
            NormalizeOptionalText(address, AddressMaxLength, DomainErrorCodes.InvalidAddress, "Address"),
            NormalizeOptionalText(notes, NotesMaxLength, DomainErrorCodes.InvalidNotes, "Notes"),
            CustomerStatus.Active,
            NormalizeOptionalPlatformBusinessCustomerId(platformBusinessCustomerId),
            personalLink,
            buyerOrgId,
            buyerPublicOrgId,
            utcNow,
            utcNow);
    }

    public static POSCustomer Rehydrate(
        POSCustomerId id,
        PosOrganizationId organizationId,
        string displayName,
        string? mobileNumber,
        string? normalizedMobile,
        string? address,
        string? notes,
        CustomerStatus status,
        Guid? platformBusinessCustomerId,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        string? linkedPersonalPublicUserId = null,
        Guid? linkedBuyerOrganizationId = null,
        string? linkedBuyerPublicOrganizationId = null) =>
        new(
            id,
            organizationId,
            displayName,
            mobileNumber,
            normalizedMobile,
            address,
            notes,
            status,
            platformBusinessCustomerId,
            linkedPersonalPublicUserId,
            linkedBuyerOrganizationId,
            linkedBuyerPublicOrganizationId,
            createdAtUtc,
            updatedAtUtc);

    /// <summary>Updates permitted profile fields. OrganizationId cannot change.</summary>
    public void UpdateProfile(
        string displayName,
        string? mobileNumber,
        string? address,
        string? notes,
        DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        EnsureNotInactiveForEdit();

        var (displayMobile, normalizedMobile) = NormalizeOptionalMobile(mobileNumber);
        DisplayName = NormalizeDisplayName(displayName);
        MobileNumber = displayMobile;
        NormalizedMobile = normalizedMobile;
        Address = NormalizeOptionalText(address, AddressMaxLength, DomainErrorCodes.InvalidAddress, "Address");
        Notes = NormalizeOptionalText(notes, NotesMaxLength, DomainErrorCodes.InvalidNotes, "Notes");
        UpdatedAtUtc = utcNow;
    }

    public void Deactivate(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (Status == CustomerStatus.Inactive)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerStatusTransition,
                "Customer is already inactive.");
        }

        Status = CustomerStatus.Inactive;
        UpdatedAtUtc = utcNow;
    }

    public void Reactivate(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (Status == CustomerStatus.Active)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerStatusTransition,
                "Customer is already active.");
        }

        Status = CustomerStatus.Active;
        UpdatedAtUtc = utcNow;
    }

    /// <summary>
    /// Binds this POS customer to a Platform BusinessCustomer id (value only).
    /// Idempotent when the same id is already set. Rejects a different id.
    /// </summary>
    public void CorrelateToPlatformBusinessCustomer(Guid platformBusinessCustomerId, DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        var normalized = NormalizeOptionalPlatformBusinessCustomerId(platformBusinessCustomerId)
            ?? throw new DomainException(
                DomainErrorCodes.InvalidPlatformBusinessCustomerId,
                "Platform BusinessCustomer id is required.");

        if (PlatformBusinessCustomerId is not null && PlatformBusinessCustomerId != normalized)
        {
            throw new DomainException(
                DomainErrorCodes.PlatformBusinessCustomerCorrelationConflict,
                "This POS customer is already correlated to a different Platform BusinessCustomer.");
        }

        PlatformBusinessCustomerId = normalized;
        UpdatedAtUtc = utcNow;
    }

    /// <summary>Clears the Platform BusinessCustomer correlation. Does not delete financial history.</summary>
    public void ClearPlatformBusinessCustomerCorrelation(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        PlatformBusinessCustomerId = null;
        UpdatedAtUtc = utcNow;
    }

    /// <summary>
    /// Links this seller-owned customer to a Personal ExItS identity. Does not grant access to Personal private data.
    /// </summary>
    public void LinkPersonalExItsIdentity(string personalPublicUserId, DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        EnsureNotInactiveForEdit();
        var normalized = NormalizePersonalPublicUserId(personalPublicUserId);
        if (LinkedBuyerOrganizationId is not null || LinkedBuyerPublicOrganizationId is not null)
        {
            throw new DomainException(
                DomainErrorCodes.CustomerExItsIdentityLinkConflict,
                "This customer is already linked to an ExItS business identity.");
        }

        if (LinkedPersonalPublicUserId is not null
            && !string.Equals(LinkedPersonalPublicUserId, normalized, StringComparison.Ordinal))
        {
            throw new DomainException(
                DomainErrorCodes.CustomerExItsIdentityLinkConflict,
                "This customer is already linked to a different Personal ExItS identity.");
        }

        LinkedPersonalPublicUserId = normalized;
        UpdatedAtUtc = utcNow;
    }

    /// <summary>
    /// Links this seller-owned customer to an ExItS Organization buyer identity (not the current owner user).
    /// </summary>
    public void LinkOrganizationExItsIdentity(
        Guid buyerOrganizationId,
        string buyerPublicOrganizationId,
        DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        EnsureNotInactiveForEdit();
        if (buyerOrganizationId == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerExItsIdentityLink,
                "Buyer organization id cannot be empty.");
        }

        var publicId = NormalizePublicOrganizationId(buyerPublicOrganizationId);
        if (LinkedPersonalPublicUserId is not null)
        {
            throw new DomainException(
                DomainErrorCodes.CustomerExItsIdentityLinkConflict,
                "This customer is already linked to a Personal ExItS identity.");
        }

        if (LinkedBuyerOrganizationId is not null && LinkedBuyerOrganizationId != buyerOrganizationId)
        {
            throw new DomainException(
                DomainErrorCodes.CustomerExItsIdentityLinkConflict,
                "This customer is already linked to a different ExItS business identity.");
        }

        LinkedBuyerOrganizationId = buyerOrganizationId;
        LinkedBuyerPublicOrganizationId = publicId;
        UpdatedAtUtc = utcNow;
    }

    public void ClearExItsIdentityLink(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        LinkedPersonalPublicUserId = null;
        LinkedBuyerOrganizationId = null;
        LinkedBuyerPublicOrganizationId = null;
        UpdatedAtUtc = utcNow;
    }

    public static string NormalizeDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new DomainException(DomainErrorCodes.InvalidDisplayName, "Display name is required.");
        }

        var trimmed = displayName.Trim();
        if (trimmed.Length > DisplayNameMaxLength || !DisplayNamePattern.IsMatch(trimmed))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidDisplayName,
                "Display name must be 1–128 characters using letters, numbers, spaces, periods, apostrophes, or hyphens.");
        }

        return trimmed;
    }

    /// <summary>
    /// Normalizes an optional mobile to digits-only E.164-ish form (10–15 digits).
    /// Philippine local numbers starting with 0 are rewritten to 63… when 11 digits.
    /// </summary>
    public static (string? Display, string? Normalized) NormalizeOptionalMobile(string? mobileNumber)
    {
        if (string.IsNullOrWhiteSpace(mobileNumber))
        {
            return (null, null);
        }

        var display = mobileNumber.Trim();
        if (display.Length > MobileMaxLength)
        {
            throw new DomainException(DomainErrorCodes.InvalidMobileNumber, "Mobile number is too long.");
        }

        var builder = new System.Text.StringBuilder(display.Length);
        foreach (var ch in display)
        {
            if (char.IsDigit(ch))
            {
                builder.Append(ch);
            }
            else if (ch is not (' ' or '-' or '(' or ')' or '+'))
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidMobileNumber,
                    "Mobile number may only contain digits, spaces, dashes, parentheses, or a leading +.");
            }
        }

        var digits = builder.ToString();
        if (digits.Length is < 10 or > 15)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidMobileNumber,
                "Mobile number must contain 10–15 digits after normalization.");
        }

        // Practical PH local form: 09XXXXXXXXX → 639XXXXXXXXX
        if (digits.Length == 11 && digits.StartsWith('0'))
        {
            digits = "63" + digits[1..];
        }

        return (display, digits);
    }

    private static string? NormalizeOptionalText(string? value, int maxLength, string errorCode, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new DomainException(errorCode, $"{fieldName} must be at most {maxLength} characters.");
        }

        return trimmed;
    }

    private static Guid? NormalizeOptionalPlatformBusinessCustomerId(Guid? platformBusinessCustomerId)
    {
        if (platformBusinessCustomerId is null)
        {
            return null;
        }

        if (platformBusinessCustomerId.Value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPlatformBusinessCustomerId,
                "Platform BusinessCustomer id cannot be empty.");
        }

        return platformBusinessCustomerId.Value;
    }

    private static (string? Personal, Guid? BuyerOrgId, string? BuyerPublicOrgId) NormalizeExItsIdentityLinks(
        string? linkedPersonalPublicUserId,
        Guid? linkedBuyerOrganizationId,
        string? linkedBuyerPublicOrganizationId)
    {
        var hasPersonal = !string.IsNullOrWhiteSpace(linkedPersonalPublicUserId);
        var hasOrg = linkedBuyerOrganizationId is not null
                     || !string.IsNullOrWhiteSpace(linkedBuyerPublicOrganizationId);
        if (hasPersonal && hasOrg)
        {
            throw new DomainException(
                DomainErrorCodes.CustomerExItsIdentityLinkConflict,
                "A customer cannot link both Personal and Organization ExItS identities.");
        }

        if (hasPersonal)
        {
            return (NormalizePersonalPublicUserId(linkedPersonalPublicUserId!), null, null);
        }

        if (!hasOrg)
        {
            return (null, null, null);
        }

        if (linkedBuyerOrganizationId is null || linkedBuyerOrganizationId == Guid.Empty
            || string.IsNullOrWhiteSpace(linkedBuyerPublicOrganizationId))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerExItsIdentityLink,
                "Organization buyer link requires both organization id and public organization id.");
        }

        return (
            null,
            linkedBuyerOrganizationId.Value,
            NormalizePublicOrganizationId(linkedBuyerPublicOrganizationId));
    }

    private static string NormalizePersonalPublicUserId(string value)
    {
        var trimmed = value.Trim().ToUpperInvariant();
        if (trimmed.Length != 12
            || !trimmed.StartsWith("EX-", StringComparison.Ordinal)
            || trimmed[7] != '-'
            || !trimmed[3..7].All(char.IsDigit)
            || !trimmed[8..].All(char.IsDigit))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerExItsIdentityLink,
                "Personal public user id format is invalid.");
        }

        return trimmed;
    }

    private static string NormalizePublicOrganizationId(string value)
    {
        var trimmed = value.Trim().ToUpperInvariant();
        if (trimmed.Length != 9 || !trimmed.StartsWith("ORG", StringComparison.Ordinal)
            || !trimmed.Skip(3).All(char.IsDigit))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerExItsIdentityLink,
                "Public organization id must match ORG######.");
        }

        return trimmed;
    }

    private void EnsureNotInactiveForEdit()
    {
        if (Status == CustomerStatus.Inactive)
        {
            throw new DomainException(
                DomainErrorCodes.CustomerNotActive,
                "Inactive customers cannot be edited. Reactivate first.");
        }
    }

    private static void EnsureUtc(DateTimeOffset utcNow)
    {
        if (utcNow.Offset != TimeSpan.Zero)
        {
            throw new DomainException(DomainErrorCodes.InvalidUtcTimestamp, "Timestamp must be UTC.");
        }
    }
}
