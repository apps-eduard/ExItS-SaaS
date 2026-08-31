using System.Text.RegularExpressions;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.Domain.Organizations;

/// <summary>
/// Organization-owned commercial customer relationship. Never Organization Staff and never a Platform User by default.
/// </summary>
public sealed class BusinessCustomer
{
    public const int DisplayNameMaxLength = 128;
    public const int NotesMaxLength = 512;

    private static readonly Regex DisplayNamePattern = new(
        @"^[\p{L}\p{N}][\p{L}\p{N} .'\-]{0,126}[\p{L}\p{N}.]?$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public BusinessCustomerId Id { get; }
    public PlatformOrganizationId OrganizationId { get; }
    public string DisplayName { get; private set; }
    public string? NormalizedEmail { get; private set; }
    public string? Phone { get; private set; }
    public string? Notes { get; private set; }
    /// <summary>Optional owning product code (stable string; no cross-DB FK).</summary>
    public string? OwningProductCode { get; private set; }
    public BusinessCustomerStatus Status { get; private set; }
    public PlatformUserId? LinkedUserIdentityId { get; private set; }
    /// <summary>
    /// Seller preference: when true, linked personal checkout may place delivery beyond
    /// <c>MaximumDeliveryDistanceKm</c>. Does not bypass service area, readiness, or min order.
    /// </summary>
    public bool AllowDeliveryBeyondNormalDistance { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private BusinessCustomer(
        BusinessCustomerId id,
        PlatformOrganizationId organizationId,
        string displayName,
        string? normalizedEmail,
        string? phone,
        string? notes,
        string? owningProductCode,
        BusinessCustomerStatus status,
        PlatformUserId? linkedUserIdentityId,
        bool allowDeliveryBeyondNormalDistance,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        DisplayName = displayName;
        NormalizedEmail = normalizedEmail;
        Phone = phone;
        Notes = notes;
        OwningProductCode = owningProductCode;
        Status = status;
        LinkedUserIdentityId = linkedUserIdentityId;
        AllowDeliveryBeyondNormalDistance = allowDeliveryBeyondNormalDistance;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static BusinessCustomer Create(
        PlatformOrganizationId organizationId,
        string displayName,
        DateTimeOffset utcNow,
        string? email = null,
        string? phone = null,
        string? notes = null,
        string? owningProductCode = null,
        BusinessCustomerId? id = null)
    {
        ArgumentNullException.ThrowIfNull(organizationId);
        EnsureUtc(utcNow);

        return new BusinessCustomer(
            id ?? BusinessCustomerId.New(),
            organizationId,
            NormalizeDisplayName(displayName),
            NormalizeOptionalEmail(email),
            NormalizeOptionalPhone(phone),
            NormalizeOptionalNotes(notes),
            NormalizeOptionalProductCode(owningProductCode),
            BusinessCustomerStatus.Active,
            null,
            allowDeliveryBeyondNormalDistance: false,
            utcNow,
            utcNow);
    }

    public static BusinessCustomer Rehydrate(
        BusinessCustomerId id,
        PlatformOrganizationId organizationId,
        string displayName,
        string? normalizedEmail,
        string? phone,
        string? notes,
        string? owningProductCode,
        BusinessCustomerStatus status,
        PlatformUserId? linkedUserIdentityId,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        bool allowDeliveryBeyondNormalDistance = false) =>
        new(
            id,
            organizationId,
            displayName,
            normalizedEmail,
            phone,
            notes,
            owningProductCode,
            status,
            linkedUserIdentityId,
            allowDeliveryBeyondNormalDistance,
            createdAtUtc,
            updatedAtUtc);

    public void UpdateProfile(
        string displayName,
        string? email,
        string? phone,
        string? notes,
        DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (Status == BusinessCustomerStatus.Archived)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidBusinessCustomerStatusTransition,
                "Archived business customers cannot be edited.");
        }

        DisplayName = NormalizeDisplayName(displayName);
        NormalizedEmail = NormalizeOptionalEmail(email);
        Phone = NormalizeOptionalPhone(phone);
        Notes = NormalizeOptionalNotes(notes);
        UpdatedAtUtc = utcNow;
    }

    /// <summary>
    /// Seller-only delivery distance exception preference. Kept separate from
    /// <see cref="UpdateProfile"/> so profile edits never clear this flag.
    /// </summary>
    public void SetAllowDeliveryBeyondNormalDistance(bool allow, DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (Status == BusinessCustomerStatus.Archived)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidBusinessCustomerStatusTransition,
                "Archived business customers cannot be edited.");
        }

        AllowDeliveryBeyondNormalDistance = allow;
        UpdatedAtUtc = utcNow;
    }

    public void MarkInactive(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (Status == BusinessCustomerStatus.Archived)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidBusinessCustomerStatusTransition,
                "Archived business customers cannot be inactivated.");
        }

        Status = BusinessCustomerStatus.Inactive;
        UpdatedAtUtc = utcNow;
    }

    public void Archive(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        Status = BusinessCustomerStatus.Archived;
        UpdatedAtUtc = utcNow;
    }

    /// <summary>
    /// Links a Platform User Identity after explicit Customer Link acceptance.
    /// Does not create Organization membership or staff roles.
    /// </summary>
    public void LinkAppUser(PlatformUserId userIdentityId, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(userIdentityId);
        EnsureUtc(utcNow);
        if (Status == BusinessCustomerStatus.Archived)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidBusinessCustomerStatusTransition,
                "Archived business customers cannot be linked.");
        }

        if (LinkedUserIdentityId is not null && LinkedUserIdentityId != userIdentityId)
        {
            throw new DomainException(
                DomainErrorCodes.BusinessCustomerAlreadyLinked,
                "Business customer is already linked to a different user.");
        }

        LinkedUserIdentityId = userIdentityId;
        UpdatedAtUtc = utcNow;
    }

    public void UnlinkAppUser(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        LinkedUserIdentityId = null;
        UpdatedAtUtc = utcNow;
    }

    /// <summary>Hard guard: a Business Customer record is never Organization Staff.</summary>
    public bool IsOrganizationStaff => false;

    private static string NormalizeDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidBusinessCustomerDisplayName,
                "Display name is required.");
        }

        var trimmed = displayName.Trim();
        if (trimmed.Length > DisplayNameMaxLength || !DisplayNamePattern.IsMatch(trimmed))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidBusinessCustomerDisplayName,
                "Display name is invalid.");
        }

        return trimmed;
    }

    private static string? NormalizeOptionalEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        return PlatformUser.NormalizeEmail(email);
    }

    private static string? NormalizeOptionalPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return null;
        }

        var trimmed = phone.Trim();
        return trimmed.Length > 32 ? trimmed[..32] : trimmed;
    }

    private static string? NormalizeOptionalNotes(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            return null;
        }

        var trimmed = notes.Trim();
        return trimmed.Length > NotesMaxLength ? trimmed[..NotesMaxLength] : trimmed;
    }

    private static string? NormalizeOptionalProductCode(string? productCode)
    {
        if (string.IsNullOrWhiteSpace(productCode))
        {
            return null;
        }

        var trimmed = productCode.Trim().ToLowerInvariant();
        if (trimmed.Length > 64)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductCode,
                "Owning product code is invalid.");
        }

        return trimmed;
    }

    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainException(DomainErrorCodes.InvalidUtcTimestamp, "Timestamps must be UTC.");
        }
    }
}
