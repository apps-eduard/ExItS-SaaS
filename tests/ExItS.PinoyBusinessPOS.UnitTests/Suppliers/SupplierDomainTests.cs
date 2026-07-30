using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Suppliers;

namespace ExItS.PinoyBusinessPOS.UnitTests.Suppliers;

public sealed class SupplierDomainTests
{
    private static readonly PosOrganizationId OrgA =
        PosOrganizationId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));

    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-07-30T08:00:00Z");

    [Fact]
    public void Create_defaults_to_active_and_normalizes_name()
    {
        var supplier = Create("SUP-000001", "  Acme Trading  ");

        Assert.Equal("Acme Trading", supplier.Name);
        Assert.Equal("ACME TRADING", supplier.NormalizedName);
        Assert.Equal(SupplierStatus.Active, supplier.Status);
        Assert.Equal("SUP-000001", supplier.SupplierCode);
        Assert.Equal(OrgA, supplier.OrganizationId);
    }

    [Fact]
    public void Create_requires_name()
    {
        var ex = Assert.Throws<DomainException>(() =>
            Supplier.Create(OrgA, "SUP-000001", "   ", Now));
        Assert.Equal(DomainErrorCodes.InvalidSupplierName, ex.ErrorCode);
    }

    [Fact]
    public void Name_contact_and_notes_enforce_length_bounds()
    {
        var longName = new string('x', Supplier.NameMaxLength + 1);
        var nameError = Assert.Throws<DomainException>(() =>
            Supplier.Create(OrgA, "SUP-000001", longName, Now));
        Assert.Equal(DomainErrorCodes.InvalidSupplierName, nameError.ErrorCode);

        var longContact = new string('x', Supplier.ContactPersonMaxLength + 1);
        var contactError = Assert.Throws<DomainException>(() =>
            Supplier.Create(OrgA, "SUP-000001", "Acme", Now, contactPerson: longContact));
        Assert.Equal(DomainErrorCodes.InvalidSupplierContactPerson, contactError.ErrorCode);

        var longNotes = new string('x', Supplier.NotesMaxLength + 1);
        var notesError = Assert.Throws<DomainException>(() =>
            Supplier.Create(OrgA, "SUP-000001", "Acme", Now, notes: longNotes));
        Assert.Equal(DomainErrorCodes.InvalidSupplierNotes, notesError.ErrorCode);
    }

    [Fact]
    public void Supplier_codes_format_sup_nnnnnn_and_normalize()
    {
        Assert.Equal("SUP-000001", SupplierCodes.Format(1));
        Assert.Equal("SUP-000042", SupplierCodes.Format(42));
        Assert.Equal("SUP-999999", SupplierCodes.Format(SupplierCodes.MaxSequence));

        Assert.Equal("SUP-000001", SupplierCodes.Normalize(" sup-000001 "));

        var invalid = Assert.Throws<DomainException>(() => SupplierCodes.Normalize("SUP-1"));
        Assert.Equal(DomainErrorCodes.InvalidSupplierCode, invalid.ErrorCode);

        var outOfRange = Assert.Throws<DomainException>(() => SupplierCodes.Format(0));
        Assert.Equal(DomainErrorCodes.InvalidSupplierCode, outOfRange.ErrorCode);
    }

    [Fact]
    public void UpdateProfile_changes_contact_fields_but_not_supplier_code()
    {
        var supplier = Create(
            "SUP-000010",
            "Acme",
            email: "buyer@acme.test",
            mobileNumber: "0917-111-2222",
            taxOrRegistrationNumber: "123-456-789");

        supplier.UpdateProfile(
            "Acme Corp",
            Now.AddMinutes(1),
            contactPerson: "Maria",
            mobileNumber: "0917-333-4444",
            telephoneNumber: "02-1234",
            email: "orders@acme.test",
            addressLine1: "123 Main",
            cityMunicipality: "Quezon City",
            province: "Metro Manila",
            postalCode: "1100",
            taxOrRegistrationNumber: "987-654-321",
            notes: "Preferred vendor");

        Assert.Equal("Acme Corp", supplier.Name);
        Assert.Equal("ACME CORP", supplier.NormalizedName);
        Assert.Equal("Maria", supplier.ContactPerson);
        Assert.Equal("0917-333-4444", supplier.MobileNumber);
        Assert.Equal("orders@acme.test", supplier.Email);
        Assert.Equal("SUP-000010", supplier.SupplierCode);
        Assert.Equal(Now.AddMinutes(1), supplier.UpdatedAtUtc);
    }

    [Fact]
    public void Deactivate_and_reactivate_guard_repeat_transitions()
    {
        var supplier = Create("SUP-000002", "Beta Supply");

        supplier.Deactivate(Now.AddMinutes(1));
        Assert.Equal(SupplierStatus.Inactive, supplier.Status);

        var repeat = Assert.Throws<DomainException>(() => supplier.Deactivate(Now.AddMinutes(2)));
        Assert.Equal(DomainErrorCodes.InvalidSupplierStatusTransition, repeat.ErrorCode);

        supplier.Reactivate(Now.AddMinutes(3));
        Assert.Equal(SupplierStatus.Active, supplier.Status);

        var repeatReactivate = Assert.Throws<DomainException>(() => supplier.Reactivate(Now.AddMinutes(4)));
        Assert.Equal(DomainErrorCodes.InvalidSupplierStatusTransition, repeatReactivate.ErrorCode);
    }

    [Fact]
    public void Inactive_supplier_cannot_be_edited_before_reactivation()
    {
        var supplier = Create("SUP-000003", "Gamma");
        supplier.Deactivate(Now.AddMinutes(1));

        var ex = Assert.Throws<DomainException>(() =>
            supplier.UpdateProfile("Gamma Updated", Now.AddMinutes(2)));
        Assert.Equal(DomainErrorCodes.SupplierNotActive, ex.ErrorCode);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("@missing-local.com")]
    public void Email_validation_rejects_invalid_formats(string email)
    {
        var ex = Assert.Throws<DomainException>(() =>
            Supplier.Create(OrgA, "SUP-000004", "Delta", Now, email: email));
        Assert.Equal(DomainErrorCodes.InvalidSupplierEmail, ex.ErrorCode);
    }

    [Fact]
    public void Email_accepts_valid_address_and_normalizes()
    {
        var supplier = Create("SUP-000005", "Epsilon", email: " Buyer@Example.COM ");
        Assert.Equal("Buyer@Example.COM", supplier.Email);
        Assert.Equal("BUYER@EXAMPLE.COM", supplier.NormalizedEmail);
    }

    [Theory]
    [InlineData("---")]
    public void Tax_number_requires_alphanumeric_content(string tax)
    {
        var ex = Assert.Throws<DomainException>(() =>
            Supplier.Create(OrgA, "SUP-000006", "Zeta", Now, taxOrRegistrationNumber: tax));
        Assert.Equal(DomainErrorCodes.InvalidSupplierTaxNumber, ex.ErrorCode);
    }

    [Fact]
    public void Tax_number_strips_separators_for_normalization()
    {
        var supplier = Create("SUP-000007", "Eta", taxOrRegistrationNumber: "123-456-789");
        Assert.Equal("123-456-789", supplier.TaxOrRegistrationNumber);
        Assert.Equal("123456789", supplier.NormalizedTaxOrRegistrationNumber);
    }

    [Fact]
    public void Supplier_code_property_has_no_public_setter()
    {
        var codeProperty = typeof(Supplier).GetProperty(nameof(Supplier.SupplierCode));
        Assert.NotNull(codeProperty);
        Assert.Null(codeProperty!.SetMethod);
    }

    [Fact]
    public void Supplier_id_rejects_empty_guid()
    {
        Assert.Throws<DomainException>(() => SupplierId.From(Guid.Empty));
    }

    private static Supplier Create(
        string code,
        string name,
        string? email = null,
        string? mobileNumber = null,
        string? taxOrRegistrationNumber = null) =>
        Supplier.Create(
            OrgA,
            code,
            name,
            Now,
            email: email,
            mobileNumber: mobileNumber,
            taxOrRegistrationNumber: taxOrRegistrationNumber);

}
