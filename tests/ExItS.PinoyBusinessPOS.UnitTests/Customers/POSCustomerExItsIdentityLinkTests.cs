using ExItS.PinoyBusinessPOS.Application.Identity;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.UnitTests.Customers;

public sealed class POSCustomerExItsIdentityLinkTests
{
    private static readonly PosOrganizationId Org = PosOrganizationId.From(Guid.NewGuid());
    private static readonly DateTimeOffset Now = new(2026, 8, 14, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Link_personal_identity_does_not_change_organization_ownership()
    {
        var customer = POSCustomer.Create(Org, "Eduard", Now);
        customer.LinkPersonalExItsIdentity("EX-4827-1936", Now);

        Assert.Equal(Org, customer.OrganizationId);
        Assert.Equal("EX-4827-1936", customer.LinkedPersonalPublicUserId);
        Assert.Null(customer.LinkedBuyerOrganizationId);
    }

    [Fact]
    public void Link_organization_identity_uses_org_ids_not_owner_user()
    {
        var buyerOrg = Guid.NewGuid();
        var customer = POSCustomer.Create(Org, "ABC Trading", Now);
        customer.LinkOrganizationExItsIdentity(buyerOrg, "ORG001234", Now);

        Assert.Equal(Org, customer.OrganizationId);
        Assert.Equal(buyerOrg, customer.LinkedBuyerOrganizationId);
        Assert.Equal("ORG001234", customer.LinkedBuyerPublicOrganizationId);
        Assert.Null(customer.LinkedPersonalPublicUserId);
    }

    [Fact]
    public void Cannot_link_both_personal_and_organization()
    {
        var customer = POSCustomer.Create(Org, "Eduard", Now);
        customer.LinkPersonalExItsIdentity("EX-4827-1936", Now);
        var ex = Assert.Throws<DomainException>(() =>
            customer.LinkOrganizationExItsIdentity(Guid.NewGuid(), "ORG001234", Now));
        Assert.Equal(DomainErrorCodes.CustomerExItsIdentityLinkConflict, ex.ErrorCode);
    }
}
