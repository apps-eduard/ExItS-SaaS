using ExItS.PinoyBusinessPOS.Domain.Suppliers;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Suppliers;

internal sealed class SupplierRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string SupplierCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? MobileNumber { get; set; }
    public string? NormalizedMobile { get; set; }
    public string? TelephoneNumber { get; set; }
    public string? Email { get; set; }
    public string? NormalizedEmail { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? CityMunicipality { get; set; }
    public string? Province { get; set; }
    public string? PostalCode { get; set; }
    public string? TaxOrRegistrationNumber { get; set; }
    public string? NormalizedTaxOrRegistrationNumber { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = string.Empty;
    public int ConnectionType { get; set; }
    public Guid? ConnectedRelationshipId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public uint Xmin { get; set; }
}

internal sealed class SupplierCodeSequenceRecord
{
    public Guid OrganizationId { get; set; }
    public long NextValue { get; set; }
}

internal static class SupplierEntityMapper
{
    public static Supplier ToDomain(SupplierRecord record) =>
        Supplier.Rehydrate(
            SupplierId.From(record.Id),
            Domain.Customers.PosOrganizationId.From(record.OrganizationId),
            record.SupplierCode,
            record.Name,
            record.NormalizedName,
            record.ContactPerson,
            record.MobileNumber,
            record.NormalizedMobile,
            record.TelephoneNumber,
            record.Email,
            record.NormalizedEmail,
            record.AddressLine1,
            record.AddressLine2,
            record.CityMunicipality,
            record.Province,
            record.PostalCode,
            record.TaxOrRegistrationNumber,
            record.NormalizedTaxOrRegistrationNumber,
            record.Notes,
            Enum.Parse<SupplierStatus>(record.Status),
            record.CreatedAtUtc,
            record.UpdatedAtUtc,
            (SupplierConnectionType)record.ConnectionType,
            record.ConnectedRelationshipId is Guid relationshipId
                ? ConnectedSupplierRelationshipId.From(relationshipId)
                : null);

    public static SupplierRecord ToRecord(Supplier supplier) =>
        new()
        {
            Id = supplier.Id.Value,
            OrganizationId = supplier.OrganizationId.Value,
            SupplierCode = supplier.SupplierCode,
            Name = supplier.Name,
            NormalizedName = supplier.NormalizedName,
            ContactPerson = supplier.ContactPerson,
            MobileNumber = supplier.MobileNumber,
            NormalizedMobile = supplier.NormalizedMobile,
            TelephoneNumber = supplier.TelephoneNumber,
            Email = supplier.Email,
            NormalizedEmail = supplier.NormalizedEmail,
            AddressLine1 = supplier.AddressLine1,
            AddressLine2 = supplier.AddressLine2,
            CityMunicipality = supplier.CityMunicipality,
            Province = supplier.Province,
            PostalCode = supplier.PostalCode,
            TaxOrRegistrationNumber = supplier.TaxOrRegistrationNumber,
            NormalizedTaxOrRegistrationNumber = supplier.NormalizedTaxOrRegistrationNumber,
            Notes = supplier.Notes,
            Status = supplier.Status.ToString(),
            ConnectionType = (int)supplier.ConnectionType,
            ConnectedRelationshipId = supplier.ConnectedRelationshipId?.Value,
            CreatedAtUtc = supplier.CreatedAtUtc,
            UpdatedAtUtc = supplier.UpdatedAtUtc
        };

    public static void ApplyToRecord(Supplier supplier, SupplierRecord record)
    {
        record.Name = supplier.Name;
        record.NormalizedName = supplier.NormalizedName;
        record.ContactPerson = supplier.ContactPerson;
        record.MobileNumber = supplier.MobileNumber;
        record.NormalizedMobile = supplier.NormalizedMobile;
        record.TelephoneNumber = supplier.TelephoneNumber;
        record.Email = supplier.Email;
        record.NormalizedEmail = supplier.NormalizedEmail;
        record.AddressLine1 = supplier.AddressLine1;
        record.AddressLine2 = supplier.AddressLine2;
        record.CityMunicipality = supplier.CityMunicipality;
        record.Province = supplier.Province;
        record.PostalCode = supplier.PostalCode;
        record.TaxOrRegistrationNumber = supplier.TaxOrRegistrationNumber;
        record.NormalizedTaxOrRegistrationNumber = supplier.NormalizedTaxOrRegistrationNumber;
        record.Notes = supplier.Notes;
        record.Status = supplier.Status.ToString();
        record.ConnectionType = (int)supplier.ConnectionType;
        record.ConnectedRelationshipId = supplier.ConnectedRelationshipId?.Value;
        record.UpdatedAtUtc = supplier.UpdatedAtUtc;
    }
}
