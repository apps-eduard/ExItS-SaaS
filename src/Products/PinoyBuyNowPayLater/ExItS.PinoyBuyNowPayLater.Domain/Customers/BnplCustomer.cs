using System.Net.Mail;
using System.Text.RegularExpressions;

namespace ExItS.PinoyBuyNowPayLater.Domain.Customers;

/// <summary>
/// Organization-scoped BNPL customer profile. Not staff, not financing eligibility.
/// Optional Platform Personal and Commerce customer links are opaque external identifiers only.
/// </summary>
public sealed class BnplCustomer
{
    public const int DisplayNameMaxLength = 128;
    public const int MobileMaxLength = 32;
    public const int EmailMaxLength = 256;

    private static readonly Regex DisplayNamePattern = new(
        @"^[\p{L}\p{N}][\p{L}\p{N} .'\-]{0,126}[\p{L}\p{N}.]?$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public BnplCustomerId Id { get; }
    public Guid OrganizationId { get; }
    public string DisplayName { get; private set; }
    public string? Mobile { get; private set; }
    public string? NormalizedMobile { get; private set; }
    public string? Email { get; private set; }
    public string? NormalizedEmail { get; private set; }
    public BnplCustomerStatus Status { get; private set; }

    /// <summary>Optional Platform Personal public user id (EX-####-####). Not a cross-DB FK.</summary>
    public string? LinkedPersonalPublicUserId { get; private set; }

    /// <summary>Optional Commerce/POS merchant-local customer Guid. Not a cross-DB FK.</summary>
    public Guid? LinkedCommerceCustomerId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private BnplCustomer(
        BnplCustomerId id,
        Guid organizationId,
        string displayName,
        string? mobile,
        string? normalizedMobile,
        string? email,
        string? normalizedEmail,
        BnplCustomerStatus status,
        string? linkedPersonalPublicUserId,
        Guid? linkedCommerceCustomerId,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        DisplayName = displayName;
        Mobile = mobile;
        NormalizedMobile = normalizedMobile;
        Email = email;
        NormalizedEmail = normalizedEmail;
        Status = status;
        LinkedPersonalPublicUserId = linkedPersonalPublicUserId;
        LinkedCommerceCustomerId = linkedCommerceCustomerId;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static BnplCustomer Create(
        Guid organizationId,
        string displayName,
        DateTimeOffset utcNow,
        BnplCustomerId? customerId = null,
        string? mobile = null,
        string? email = null,
        string? linkedPersonalPublicUserId = null,
        Guid? linkedCommerceCustomerId = null)
    {
        EnsureUtc(utcNow);
        EnsureOrganizationId(organizationId);
        var (displayMobile, normalizedMobile) = NormalizeOptionalMobile(mobile);
        var (displayEmail, normalizedEmail) = NormalizeOptionalEmail(email);

        return new BnplCustomer(
            customerId ?? BnplCustomerId.New(),
            organizationId,
            NormalizeDisplayName(displayName),
            displayMobile,
            normalizedMobile,
            displayEmail,
            normalizedEmail,
            BnplCustomerStatus.Active,
            BnplPersonalPublicUserIdRules.NormalizeOptional(linkedPersonalPublicUserId),
            NormalizeOptionalCommerceCustomerId(linkedCommerceCustomerId),
            utcNow,
            utcNow);
    }

    public static BnplCustomer Reconstitute(
        BnplCustomerId id,
        Guid organizationId,
        string displayName,
        string? mobile,
        string? normalizedMobile,
        string? email,
        string? normalizedEmail,
        BnplCustomerStatus status,
        string? linkedPersonalPublicUserId,
        Guid? linkedCommerceCustomerId,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc) =>
        new(
            id,
            organizationId,
            displayName,
            mobile,
            normalizedMobile,
            email,
            normalizedEmail,
            status,
            linkedPersonalPublicUserId,
            linkedCommerceCustomerId,
            createdAtUtc,
            updatedAtUtc);

    public void UpdateProfile(
        string displayName,
        string? mobile,
        string? email,
        DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        var (displayMobile, normalizedMobile) = NormalizeOptionalMobile(mobile);
        var (displayEmail, normalizedEmail) = NormalizeOptionalEmail(email);
        DisplayName = NormalizeDisplayName(displayName);
        Mobile = displayMobile;
        NormalizedMobile = normalizedMobile;
        Email = displayEmail;
        NormalizedEmail = normalizedEmail;
        UpdatedAtUtc = utcNow;
    }

    public void LinkPersonalPublicUserId(string personalPublicUserId, DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        LinkedPersonalPublicUserId = BnplPersonalPublicUserIdRules.Normalize(personalPublicUserId);
        UpdatedAtUtc = utcNow;
    }

    public void ClearPersonalPublicUserId(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        LinkedPersonalPublicUserId = null;
        UpdatedAtUtc = utcNow;
    }

    public void LinkCommerceCustomerId(Guid commerceCustomerId, DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        LinkedCommerceCustomerId = NormalizeOptionalCommerceCustomerId(commerceCustomerId)
            ?? throw new BnplDomainException(
                BnplCustomerErrorCodes.InvalidCommerceCustomerId,
                "Commerce customer id must be a non-empty Guid.");
        UpdatedAtUtc = utcNow;
    }

    public void ClearCommerceCustomerId(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        LinkedCommerceCustomerId = null;
        UpdatedAtUtc = utcNow;
    }

    public void Deactivate(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        Status = BnplCustomerStatus.Inactive;
        UpdatedAtUtc = utcNow;
    }

    public void Reactivate(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        Status = BnplCustomerStatus.Active;
        UpdatedAtUtc = utcNow;
    }

    /// <summary>
    /// True when create-retry payload matches this persisted customer (idempotent converge).
    /// Email/mobile are contact fields only — never authorization identity.
    /// </summary>
    public bool IsCompatibleCreatePayload(
        string displayName,
        string? mobile,
        string? email,
        string? linkedPersonalPublicUserId,
        Guid? linkedCommerceCustomerId)
    {
        var expectedName = NormalizeDisplayName(displayName);
        var (_, expectedMobile) = NormalizeOptionalMobile(mobile);
        var (_, expectedEmail) = NormalizeOptionalEmail(email);
        var expectedPersonal = BnplPersonalPublicUserIdRules.NormalizeOptional(linkedPersonalPublicUserId);
        var expectedCommerce = NormalizeOptionalCommerceCustomerId(linkedCommerceCustomerId);

        return string.Equals(DisplayName, expectedName, StringComparison.Ordinal)
               && string.Equals(NormalizedMobile, expectedMobile, StringComparison.Ordinal)
               && string.Equals(NormalizedEmail, expectedEmail, StringComparison.Ordinal)
               && string.Equals(LinkedPersonalPublicUserId, expectedPersonal, StringComparison.Ordinal)
               && LinkedCommerceCustomerId == expectedCommerce;
    }

    private static void EnsureOrganizationId(Guid organizationId)
    {
        if (organizationId == Guid.Empty)
        {
            throw new BnplDomainException(
                BnplCustomerErrorCodes.InvalidOrganizationId,
                "OrganizationId must be a non-empty Guid.");
        }
    }

    private static void EnsureUtc(DateTimeOffset utcNow)
    {
        if (utcNow.Offset != TimeSpan.Zero)
        {
            throw new BnplDomainException(
                BnplCustomerErrorCodes.InvalidDisplayName,
                "Timestamps must be UTC.");
        }
    }

    private static string NormalizeDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new BnplDomainException(
                BnplCustomerErrorCodes.InvalidDisplayName,
                "DisplayName is required.");
        }

        var trimmed = displayName.Trim();
        if (trimmed.Length > DisplayNameMaxLength || !DisplayNamePattern.IsMatch(trimmed))
        {
            throw new BnplDomainException(
                BnplCustomerErrorCodes.InvalidDisplayName,
                "DisplayName is invalid.");
        }

        return trimmed;
    }

    private static (string? Display, string? Normalized) NormalizeOptionalMobile(string? mobile)
    {
        if (string.IsNullOrWhiteSpace(mobile))
        {
            return (null, null);
        }

        var trimmed = mobile.Trim();
        if (trimmed.Length > MobileMaxLength)
        {
            throw new BnplDomainException(
                BnplCustomerErrorCodes.InvalidMobile,
                "Mobile exceeds maximum length.");
        }

        var digits = new string(trimmed.Where(char.IsDigit).ToArray());
        if (digits.Length < 7)
        {
            throw new BnplDomainException(
                BnplCustomerErrorCodes.InvalidMobile,
                "Mobile must contain at least 7 digits.");
        }

        return (trimmed, digits);
    }

    private static (string? Display, string? Normalized) NormalizeOptionalEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return (null, null);
        }

        var trimmed = email.Trim();
        if (trimmed.Length > EmailMaxLength)
        {
            throw new BnplDomainException(
                BnplCustomerErrorCodes.InvalidEmail,
                "Email exceeds maximum length.");
        }

        try
        {
            _ = new MailAddress(trimmed);
        }
        catch (FormatException)
        {
            throw new BnplDomainException(
                BnplCustomerErrorCodes.InvalidEmail,
                "Email format is invalid.");
        }

        return (trimmed, trimmed.ToLowerInvariant());
    }

    private static Guid? NormalizeOptionalCommerceCustomerId(Guid? commerceCustomerId)
    {
        if (commerceCustomerId is null)
        {
            return null;
        }

        if (commerceCustomerId.Value == Guid.Empty)
        {
            throw new BnplDomainException(
                BnplCustomerErrorCodes.InvalidCommerceCustomerId,
                "Commerce customer id must be a non-empty Guid.");
        }

        return commerceCustomerId.Value;
    }
}
