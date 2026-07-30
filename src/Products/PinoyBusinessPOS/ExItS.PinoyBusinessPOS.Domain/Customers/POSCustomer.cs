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
        POSCustomerId? id = null)
    {
        EnsureUtc(utcNow);
        var (displayMobile, normalizedMobile) = NormalizeOptionalMobile(mobileNumber);

        return new POSCustomer(
            id ?? POSCustomerId.New(),
            organizationId,
            NormalizeDisplayName(displayName),
            displayMobile,
            normalizedMobile,
            NormalizeOptionalText(address, AddressMaxLength, DomainErrorCodes.InvalidAddress, "Address"),
            NormalizeOptionalText(notes, NotesMaxLength, DomainErrorCodes.InvalidNotes, "Notes"),
            CustomerStatus.Active,
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
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc) =>
        new(
            id,
            organizationId,
            displayName,
            mobileNumber,
            normalizedMobile,
            address,
            notes,
            status,
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
