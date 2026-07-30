using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.UnitTests.Customers;

public sealed class POSCustomerDomainTests
{
    private static readonly PosOrganizationId OrgA = PosOrganizationId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-07-30T08:00:00Z");

    [Fact]
    public void Create_requires_display_name_and_allows_missing_mobile()
    {
        var customer = POSCustomer.Create(OrgA, "Aling Nena", Now, mobileNumber: null, address: "Corner store", notes: "Regular");
        Assert.Equal("Aling Nena", customer.DisplayName);
        Assert.Null(customer.MobileNumber);
        Assert.Null(customer.NormalizedMobile);
        Assert.Equal(CustomerStatus.Active, customer.Status);
        Assert.Equal(OrgA, customer.OrganizationId);
    }

    [Fact]
    public void Create_normalizes_philippine_mobile()
    {
        var customer = POSCustomer.Create(OrgA, "Juan", Now, mobileNumber: "0917-123-4567");
        Assert.Equal("0917-123-4567", customer.MobileNumber);
        Assert.Equal("639171234567", customer.NormalizedMobile);
    }

    [Fact]
    public void Create_rejects_invalid_mobile()
    {
        var ex = Assert.Throws<DomainException>(() =>
            POSCustomer.Create(OrgA, "Juan", Now, mobileNumber: "123"));
        Assert.Equal(DomainErrorCodes.InvalidMobileNumber, ex.ErrorCode);
    }

    [Fact]
    public void Update_profile_does_not_change_organization()
    {
        var customer = POSCustomer.Create(OrgA, "Juan", Now);
        var orgBefore = customer.OrganizationId;
        customer.UpdateProfile("Juan Dela Cruz", "09171234567", "Barangay 1", "Neighbor", Now.AddMinutes(1));
        Assert.Equal(orgBefore, customer.OrganizationId);
        Assert.Equal("Juan Dela Cruz", customer.DisplayName);
        Assert.Equal("639171234567", customer.NormalizedMobile);
    }

    [Fact]
    public void Deactivate_and_reactivate_lifecycle()
    {
        var customer = POSCustomer.Create(OrgA, "Juan", Now);
        customer.Deactivate(Now.AddMinutes(1));
        Assert.Equal(CustomerStatus.Inactive, customer.Status);
        Assert.Throws<DomainException>(() => customer.UpdateProfile("X", null, null, null, Now.AddMinutes(2)));
        customer.Reactivate(Now.AddMinutes(3));
        Assert.Equal(CustomerStatus.Active, customer.Status);
    }

    [Fact]
    public void Notes_are_identification_only_and_bounded()
    {
        var longNotes = new string('x', 513);
        var ex = Assert.Throws<DomainException>(() =>
            POSCustomer.Create(OrgA, "Juan", Now, notes: longNotes));
        Assert.Equal(DomainErrorCodes.InvalidNotes, ex.ErrorCode);
    }
}
