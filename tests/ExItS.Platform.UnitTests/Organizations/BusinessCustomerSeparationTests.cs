using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.UnitTests.Organizations;

public sealed class BusinessCustomerSeparationTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 2, 6, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Business_customer_is_never_organization_staff()
    {
        var customer = BusinessCustomer.Create(
            PlatformOrganizationId.New(),
            "Walk-in Customer",
            T0,
            email: "customer@example.com");

        Assert.False(customer.IsOrganizationStaff);
        CustomerStaffSeparationGuard.EnsureNotTreatedAsStaff(customer);
    }

    [Fact]
    public void Customer_to_staff_conversion_is_hard_denied()
    {
        var ex = Assert.Throws<DomainException>(CustomerStaffSeparationGuard.RejectCustomerToStaffConversion);
        Assert.Equal(DomainErrorCodes.CustomerToStaffConversionDenied, ex.ErrorCode);
    }

    [Fact]
    public void Customer_link_accept_must_not_create_staff()
    {
        var ex = Assert.Throws<DomainException>(() =>
            CustomerStaffSeparationGuard.EnsureCustomerLinkDoesNotGrantStaff(
                createdOrganizationMembership: true,
                grantedStaffRole: false));
        Assert.Equal(DomainErrorCodes.CustomerLinkMustNotCreateStaff, ex.ErrorCode);
    }

    [Fact]
    public void Customer_link_request_accept_creates_linked_app_user_without_staff_flags()
    {
        var orgId = PlatformOrganizationId.New();
        var customer = BusinessCustomer.Create(orgId, "Linked Customer", T0, email: "link@example.com");
        var (request, token) = CustomerLinkRequest.Create(
            orgId,
            customer.Id,
            "link@example.com",
            T0);
        var userId = PlatformUserId.New();

        request.Accept(userId, "link@example.com", T0.AddMinutes(1));
        customer.LinkAppUser(userId, T0.AddMinutes(1));
        var link = LinkedCustomerAppUser.CreateFromAcceptedLink(
            orgId,
            customer.Id,
            userId,
            request.Id,
            T0.AddMinutes(1));

        Assert.Equal(CustomerLinkRequestStatus.Active, request.Status);
        Assert.Equal(userId, customer.LinkedUserIdentityId);
        Assert.False(link.IsOrganizationStaff);
        Assert.False(link.GrantsProductRole);
        Assert.Equal(CustomerLinkRequest.InvitationType, InvitationKinds.CustomerLinkRequest);
        Assert.Equal(64, OrganizationInvitation.HashToken(token).Length);
    }

    [Fact]
    public void Credit_customer_is_not_staff()
    {
        var orgId = PlatformOrganizationId.New();
        var customer = BusinessCustomer.Create(orgId, "Credit Buyer", T0);
        var credit = CreditCustomer.Create(orgId, customer.Id, T0);
        Assert.False(credit.IsOrganizationStaff);
        Assert.Equal(CreditCustomerStatus.Active, credit.Status);
    }

    [Fact]
    public void Staff_invitation_type_is_distinct_from_customer_link()
    {
        Assert.Equal(InvitationKinds.OrganizationStaffInvitation, OrganizationInvitation.InvitationType);
        Assert.NotEqual(OrganizationInvitation.InvitationType, CustomerLinkRequest.InvitationType);
    }
}
