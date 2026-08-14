using System.Net.Mail;
using System.Text.RegularExpressions;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;

namespace ExItS.PinoyBusinessPOS.Domain.Suppliers;

/// <summary>
/// Organization-owned supplier master-data aggregate (P10-WP01 Option A).
/// Reference data only — no purchasing, receiving, payables, cost, or stock state.
/// </summary>
public sealed class Supplier
{
    public const int NameMaxLength = 128;
    public const int ContactPersonMaxLength = 128;
    public const int EmailMaxLength = 256;
    public const int AddressLineMaxLength = 128;
    public const int CityMaxLength = 64;
    public const int ProvinceMaxLength = 64;
    public const int PostalCodeMaxLength = 16;
    public const int TaxMaxLength = 64;
    public const int NotesMaxLength = 512;
    public const int TelephoneMaxLength = 32;
    public const int MobileMaxLength = 32;

    private static readonly Regex NamePattern = new(
        @"^[\p{L}\p{N}][\p{L}\p{N} .'\-&/]{0,126}[\p{L}\p{N}.]?$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public SupplierId Id { get; }
    public PosOrganizationId OrganizationId { get; }
    public string SupplierCode { get; }
    public string Name { get; private set; }
    public string NormalizedName { get; private set; }
    public string? ContactPerson { get; private set; }
    public string? MobileNumber { get; private set; }
    public string? NormalizedMobile { get; private set; }
    public string? TelephoneNumber { get; private set; }
    public string? Email { get; private set; }
    public string? NormalizedEmail { get; private set; }
    public string? AddressLine1 { get; private set; }
    public string? AddressLine2 { get; private set; }
    public string? CityMunicipality { get; private set; }
    public string? Province { get; private set; }
    public string? PostalCode { get; private set; }
    public string? TaxOrRegistrationNumber { get; private set; }
    public string? NormalizedTaxOrRegistrationNumber { get; private set; }
    public string? Notes { get; private set; }
    public SupplierStatus Status { get; private set; }
    public SupplierConnectionType ConnectionType { get; private set; }
    public ConnectedSupplierRelationshipId? ConnectedRelationshipId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private Supplier(
        SupplierId id,
        PosOrganizationId organizationId,
        string supplierCode,
        string name,
        string normalizedName,
        string? contactPerson,
        string? mobileNumber,
        string? normalizedMobile,
        string? telephoneNumber,
        string? email,
        string? normalizedEmail,
        string? addressLine1,
        string? addressLine2,
        string? cityMunicipality,
        string? province,
        string? postalCode,
        string? taxOrRegistrationNumber,
        string? normalizedTax,
        string? notes,
        SupplierStatus status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        SupplierConnectionType connectionType = SupplierConnectionType.External,
        ConnectedSupplierRelationshipId? connectedRelationshipId = null)
    {
        Id = id;
        OrganizationId = organizationId;
        SupplierCode = supplierCode;
        Name = name;
        NormalizedName = normalizedName;
        ContactPerson = contactPerson;
        MobileNumber = mobileNumber;
        NormalizedMobile = normalizedMobile;
        TelephoneNumber = telephoneNumber;
        Email = email;
        NormalizedEmail = normalizedEmail;
        AddressLine1 = addressLine1;
        AddressLine2 = addressLine2;
        CityMunicipality = cityMunicipality;
        Province = province;
        PostalCode = postalCode;
        TaxOrRegistrationNumber = taxOrRegistrationNumber;
        NormalizedTaxOrRegistrationNumber = normalizedTax;
        Notes = notes;
        Status = status;
        ConnectionType = connectionType;
        ConnectedRelationshipId = connectedRelationshipId;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static Supplier Create(
        PosOrganizationId organizationId,
        string supplierCode,
        string name,
        DateTimeOffset utcNow,
        string? contactPerson = null,
        string? mobileNumber = null,
        string? telephoneNumber = null,
        string? email = null,
        string? addressLine1 = null,
        string? addressLine2 = null,
        string? cityMunicipality = null,
        string? province = null,
        string? postalCode = null,
        string? taxOrRegistrationNumber = null,
        string? notes = null,
        SupplierId? id = null)
    {
        EnsureUtc(utcNow);
        var code = SupplierCodes.Normalize(supplierCode);
        var displayName = NormalizeName(name);
        var (displayMobile, normalizedMobile) = POSCustomer.NormalizeOptionalMobile(mobileNumber);
        var (displayEmail, normalizedEmail) = NormalizeOptionalEmail(email);
        var (displayTax, normalizedTax) = NormalizeOptionalTax(taxOrRegistrationNumber);

        return new Supplier(
            id ?? SupplierId.New(),
            organizationId,
            code,
            displayName,
            Normalize(displayName),
            NormalizeOptionalText(contactPerson, ContactPersonMaxLength, DomainErrorCodes.InvalidSupplierContactPerson, "Contact person"),
            displayMobile,
            normalizedMobile,
            NormalizeOptionalText(telephoneNumber, TelephoneMaxLength, DomainErrorCodes.InvalidSupplierTelephone, "Telephone"),
            displayEmail,
            normalizedEmail,
            NormalizeOptionalText(addressLine1, AddressLineMaxLength, DomainErrorCodes.InvalidSupplierAddress, "Address line 1"),
            NormalizeOptionalText(addressLine2, AddressLineMaxLength, DomainErrorCodes.InvalidSupplierAddress, "Address line 2"),
            NormalizeOptionalText(cityMunicipality, CityMaxLength, DomainErrorCodes.InvalidSupplierAddress, "City/municipality"),
            NormalizeOptionalText(province, ProvinceMaxLength, DomainErrorCodes.InvalidSupplierAddress, "Province"),
            NormalizeOptionalText(postalCode, PostalCodeMaxLength, DomainErrorCodes.InvalidSupplierAddress, "Postal code"),
            displayTax,
            normalizedTax,
            NormalizeOptionalText(notes, NotesMaxLength, DomainErrorCodes.InvalidSupplierNotes, "Notes"),
            SupplierStatus.Active,
            utcNow,
            utcNow,
            SupplierConnectionType.External,
            null);
    }

    public static Supplier Rehydrate(
        SupplierId id,
        PosOrganizationId organizationId,
        string supplierCode,
        string name,
        string normalizedName,
        string? contactPerson,
        string? mobileNumber,
        string? normalizedMobile,
        string? telephoneNumber,
        string? email,
        string? normalizedEmail,
        string? addressLine1,
        string? addressLine2,
        string? cityMunicipality,
        string? province,
        string? postalCode,
        string? taxOrRegistrationNumber,
        string? normalizedTax,
        string? notes,
        SupplierStatus status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        SupplierConnectionType connectionType = SupplierConnectionType.External,
        ConnectedSupplierRelationshipId? connectedRelationshipId = null) =>
        new(
            id, organizationId, supplierCode, name, normalizedName, contactPerson,
            mobileNumber, normalizedMobile, telephoneNumber, email, normalizedEmail,
            addressLine1, addressLine2, cityMunicipality, province, postalCode,
            taxOrRegistrationNumber, normalizedTax, notes, status, createdAtUtc, updatedAtUtc,
            connectionType, connectedRelationshipId);

    public void AttachConnectedRelationship(ConnectedSupplierRelationshipId relationshipId, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(relationshipId);
        EnsureUtc(utcNow);
        ConnectionType = SupplierConnectionType.ConnectedOrganization;
        ConnectedRelationshipId = relationshipId;
        UpdatedAtUtc = utcNow;
    }

    public void AttachConnectedRelationship(ConnectedSupplierRelationshipId relationshipId) =>
        AttachConnectedRelationship(relationshipId, UpdatedAtUtc);

    public void ClearConnectedRelationship(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        ConnectionType = SupplierConnectionType.External;
        ConnectedRelationshipId = null;
        UpdatedAtUtc = utcNow;
    }

    public void ClearConnectedRelationship() => ClearConnectedRelationship(UpdatedAtUtc);

    public void UpdateProfile(
        string name,
        DateTimeOffset utcNow,
        string? contactPerson = null,
        string? mobileNumber = null,
        string? telephoneNumber = null,
        string? email = null,
        string? addressLine1 = null,
        string? addressLine2 = null,
        string? cityMunicipality = null,
        string? province = null,
        string? postalCode = null,
        string? taxOrRegistrationNumber = null,
        string? notes = null)
    {
        EnsureUtc(utcNow);
        EnsureActive();
        var displayName = NormalizeName(name);
        var (displayMobile, normalizedMobile) = POSCustomer.NormalizeOptionalMobile(mobileNumber);
        var (displayEmail, normalizedEmail) = NormalizeOptionalEmail(email);
        var (displayTax, normalizedTax) = NormalizeOptionalTax(taxOrRegistrationNumber);

        Name = displayName;
        NormalizedName = Normalize(displayName);
        ContactPerson = NormalizeOptionalText(contactPerson, ContactPersonMaxLength, DomainErrorCodes.InvalidSupplierContactPerson, "Contact person");
        MobileNumber = displayMobile;
        NormalizedMobile = normalizedMobile;
        TelephoneNumber = NormalizeOptionalText(telephoneNumber, TelephoneMaxLength, DomainErrorCodes.InvalidSupplierTelephone, "Telephone");
        Email = displayEmail;
        NormalizedEmail = normalizedEmail;
        AddressLine1 = NormalizeOptionalText(addressLine1, AddressLineMaxLength, DomainErrorCodes.InvalidSupplierAddress, "Address line 1");
        AddressLine2 = NormalizeOptionalText(addressLine2, AddressLineMaxLength, DomainErrorCodes.InvalidSupplierAddress, "Address line 2");
        CityMunicipality = NormalizeOptionalText(cityMunicipality, CityMaxLength, DomainErrorCodes.InvalidSupplierAddress, "City/municipality");
        Province = NormalizeOptionalText(province, ProvinceMaxLength, DomainErrorCodes.InvalidSupplierAddress, "Province");
        PostalCode = NormalizeOptionalText(postalCode, PostalCodeMaxLength, DomainErrorCodes.InvalidSupplierAddress, "Postal code");
        TaxOrRegistrationNumber = displayTax;
        NormalizedTaxOrRegistrationNumber = normalizedTax;
        Notes = NormalizeOptionalText(notes, NotesMaxLength, DomainErrorCodes.InvalidSupplierNotes, "Notes");
        UpdatedAtUtc = utcNow;
    }

    public void Deactivate(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (Status == SupplierStatus.Inactive)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSupplierStatusTransition,
                "Supplier is already inactive.");
        }

        Status = SupplierStatus.Inactive;
        UpdatedAtUtc = utcNow;
    }

    public void Reactivate(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (Status == SupplierStatus.Active)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSupplierStatusTransition,
                "Supplier is already active.");
        }

        Status = SupplierStatus.Active;
        UpdatedAtUtc = utcNow;
    }

    public static string Normalize(string displayName) => displayName.Trim().ToUpperInvariant();

    public static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException(DomainErrorCodes.InvalidSupplierName, "Supplier name is required.");
        }

        var trimmed = name.Trim();
        if (trimmed.Length > NameMaxLength || !NamePattern.IsMatch(trimmed))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSupplierName,
                $"Supplier name must be 1–{NameMaxLength} characters using letters, digits, spaces, and .'-&/.");
        }

        return trimmed;
    }

    public static (string? Display, string? Normalized) NormalizeOptionalEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return (null, null);
        }

        var trimmed = email.Trim();
        if (trimmed.Length > EmailMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSupplierEmail,
                $"Email must be at most {EmailMaxLength} characters.");
        }

        try
        {
            var parsed = new MailAddress(trimmed);
            if (!string.Equals(parsed.Address, trimmed, StringComparison.OrdinalIgnoreCase)
                && !trimmed.Contains('@', StringComparison.Ordinal))
            {
                throw new FormatException();
            }
        }
        catch (FormatException)
        {
            throw new DomainException(DomainErrorCodes.InvalidSupplierEmail, "Email format is invalid.");
        }

        return (trimmed, trimmed.ToUpperInvariant());
    }

    public static (string? Display, string? Normalized) NormalizeOptionalTax(string? tax)
    {
        if (string.IsNullOrWhiteSpace(tax))
        {
            return (null, null);
        }

        var trimmed = tax.Trim();
        if (trimmed.Length > TaxMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSupplierTaxNumber,
                $"Tax/registration number must be at most {TaxMaxLength} characters.");
        }

        var normalized = Regex.Replace(trimmed.ToUpperInvariant(), @"[^A-Z0-9]", string.Empty);
        if (normalized.Length == 0)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSupplierTaxNumber,
                "Tax/registration number must contain letters or digits.");
        }

        return (trimmed, normalized);
    }

    private void EnsureActive()
    {
        if (Status != SupplierStatus.Active)
        {
            throw new DomainException(
                DomainErrorCodes.SupplierNotActive,
                "Inactive suppliers cannot be edited. Reactivate first.");
        }
    }

    private static void EnsureUtc(DateTimeOffset utcNow)
    {
        if (utcNow.Offset != TimeSpan.Zero)
        {
            throw new DomainException(DomainErrorCodes.InvalidUtcTimestamp, "Timestamp must be UTC.");
        }
    }

    private static string? NormalizeOptionalText(string? value, int maxLength, string errorCode, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new DomainException(errorCode, $"{label} must be at most {maxLength} characters.");
        }

        if (trimmed.Contains('<', StringComparison.Ordinal) || trimmed.Contains('>', StringComparison.Ordinal))
        {
            throw new DomainException(errorCode, $"{label} must be plain text.");
        }

        return trimmed;
    }
}
