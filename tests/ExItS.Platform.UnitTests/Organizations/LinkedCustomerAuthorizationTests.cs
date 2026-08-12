using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Personal;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.UnitTests.Organizations;

public sealed class LinkedCustomerAuthorizationTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 12, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Active_personal_link_authorizes_with_expected_context_only()
    {
        var harness = await CustomerLinkCompletenessTests.Harness.CreateAsync();
        var accepted = await AcceptAsync(harness);
        var result = await AuthorizeDefaultAsync(harness);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal(harness.Personal.Id.Value, result.Value!.PersonalUserId);
        Assert.Equal(harness.Org.Id.Value, result.Value.OrganizationId);
        Assert.Equal(harness.Customer.Id.Value, result.Value.PlatformBusinessCustomerId);
        Assert.Equal(accepted.LinkedCustomerAppUserId, result.Value.LinkedCustomerAppUserId);
        Assert.Equal(
            new[]
            {
                nameof(result.Value.LinkedCustomerAppUserId),
                nameof(result.Value.OrganizationId),
                nameof(result.Value.PersonalUserId),
                nameof(result.Value.PlatformBusinessCustomerId)
            },
            result.Value.GetType().GetProperties().Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public async Task Unrelated_personal_user_is_denied_as_not_found()
    {
        var harness = await CustomerLinkCompletenessTests.Harness.CreateAsync();
        await AcceptAsync(harness);
        var other = PlatformUser.Create("otheruser", "Other User", "other@example.com", T0);
        await harness.Users.AddAsync(other);

        var result = await harness.Authorize.ExecuteAsync(
            other.Id,
            AccountClass.Personal,
            harness.Org.Id,
            harness.Customer.Id);

        AssertNotFound(result);
    }

    [Fact]
    public async Task Inactive_personal_user_is_denied()
    {
        var harness = await CustomerLinkCompletenessTests.Harness.CreateAsync();
        await AcceptAsync(harness);
        harness.Personal.Suspend(T0.AddMinutes(1), "test");
        await harness.Users.UpdateAsync(harness.Personal);

        var result = await AuthorizeDefaultAsync(harness);
        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorCodes.UserNotActive, result.ErrorCode);
    }

    [Fact]
    public async Task Staff_identity_is_denied()
    {
        var harness = await CustomerLinkCompletenessTests.Harness.CreateAsync();
        var staff = PlatformUser.CreateOrganizationStaff(
            "maria_org001842",
            "maria@ORG001842",
            harness.Personal.NormalizedEmail,
            harness.Org.Id,
            "Maria Staff",
            T0);
        await harness.Users.AddAsync(staff);

        var result = await harness.Authorize.ExecuteAsync(
            staff.Id,
            AccountClass.Personal,
            harness.Org.Id,
            harness.Customer.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorCodes.CustomerLinkPersonalIdentityRequired, result.ErrorCode);
    }

    [Fact]
    public async Task Platform_admin_identity_is_denied()
    {
        var harness = await CustomerLinkCompletenessTests.Harness.CreateAsync();
        var admin = PlatformUser.CreatePlatformStaff(
            "olivia.staff",
            "Olivia",
            "Staff",
            "Olivia Staff",
            harness.Personal.NormalizedEmail,
            "STF-000001",
            T0);
        await harness.Users.AddAsync(admin);

        var result = await harness.Authorize.ExecuteAsync(
            admin.Id,
            AccountClass.Personal,
            harness.Org.Id,
            harness.Customer.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorCodes.CustomerLinkPersonalIdentityRequired, result.ErrorCode);
    }

    [Theory]
    [InlineData(AccountClass.Organization)]
    [InlineData(AccountClass.Platform)]
    public async Task Wrong_account_class_is_denied(AccountClass accountClass)
    {
        var harness = await CustomerLinkCompletenessTests.Harness.CreateAsync();
        await AcceptAsync(harness);

        var result = await harness.Authorize.ExecuteAsync(
            harness.Personal.Id,
            accountClass,
            harness.Org.Id,
            harness.Customer.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.AccountScopeDenied, result.ErrorCode);
    }

    [Fact]
    public async Task Missing_link_is_not_found()
    {
        var harness = await CustomerLinkCompletenessTests.Harness.CreateAsync();
        AssertNotFound(await AuthorizeDefaultAsync(harness));
    }

    [Fact]
    public async Task Pending_invitation_is_not_found()
    {
        var harness = await CustomerLinkCompletenessTests.Harness.CreateAsync();
        Assert.Equal(CustomerLinkRequestStatus.Pending, harness.Request.Status);
        AssertNotFound(await AuthorizeDefaultAsync(harness));
    }

    [Fact]
    public async Task Declined_invitation_is_not_found()
    {
        var harness = await CustomerLinkCompletenessTests.Harness.CreateAsync();
        Assert.True((await harness.Decline.ExecuteAsync(harness.AcceptToken)).IsSuccess);
        AssertNotFound(await AuthorizeDefaultAsync(harness));
    }

    [Fact]
    public async Task Expired_invitation_is_not_found()
    {
        var harness = await CustomerLinkCompletenessTests.Harness.CreateAsync();
        harness.Request.MarkExpired(T0.AddDays(8));
        await harness.Requests.UpdateAsync(harness.Request);
        AssertNotFound(await AuthorizeDefaultAsync(harness));
    }

    [Fact]
    public async Task Revoked_accepted_link_is_immediately_denied()
    {
        var harness = await CustomerLinkCompletenessTests.Harness.CreateAsync();
        var accepted = await AcceptAsync(harness);
        Assert.True((await AuthorizeDefaultAsync(harness)).IsSuccess);

        Assert.True((await harness.Unlink.ExecuteForOwnerAsync(
            LinkedCustomerAppUserId.From(accepted.LinkedCustomerAppUserId),
            harness.Personal.Id)).IsSuccess);

        AssertNotFound(await AuthorizeDefaultAsync(harness));
    }

    [Fact]
    public async Task Wrong_organization_is_not_found()
    {
        var harness = await CustomerLinkCompletenessTests.Harness.CreateAsync();
        await AcceptAsync(harness);
        var otherOrg = PlatformOrganization.Create("Other Store", "other-store", T0);
        await harness.Orgs.AddAsync(otherOrg);

        var result = await harness.Authorize.ExecuteAsync(
            harness.Personal.Id,
            AccountClass.Personal,
            otherOrg.Id,
            harness.Customer.Id);

        AssertNotFound(result);
    }

    [Fact]
    public async Task Wrong_business_customer_in_same_org_is_not_found()
    {
        var harness = await CustomerLinkCompletenessTests.Harness.CreateAsync();
        await AcceptAsync(harness);
        var otherCustomer = BusinessCustomer.Create(harness.Org.Id, "Other Customer", T0, email: "othercust@example.com");
        await harness.Customers.AddAsync(otherCustomer);

        var result = await harness.Authorize.ExecuteAsync(
            harness.Personal.Id,
            AccountClass.Personal,
            harness.Org.Id,
            otherCustomer.Id);

        AssertNotFound(result);
    }

    [Fact]
    public async Task Identifier_guessing_uses_the_same_not_found_code()
    {
        var harness = await CustomerLinkCompletenessTests.Harness.CreateAsync();
        await AcceptAsync(harness);
        var guessedOrg = PlatformOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        var guessedCustomer = BusinessCustomerId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));

        var missing = await AuthorizeDefaultAsync(await CustomerLinkCompletenessTests.Harness.CreateAsync());
        var guessed = await harness.Authorize.ExecuteAsync(
            harness.Personal.Id,
            AccountClass.Personal,
            guessedOrg,
            guessedCustomer);
        var otherCustomer = BusinessCustomer.Create(harness.Org.Id, "Other Customer", T0);
        await harness.Customers.AddAsync(otherCustomer);
        var sibling = await harness.Authorize.ExecuteAsync(
            harness.Personal.Id,
            AccountClass.Personal,
            harness.Org.Id,
            otherCustomer.Id);

        AssertNotFound(missing);
        AssertNotFound(guessed);
        AssertNotFound(sibling);
        Assert.Equal(missing.ErrorMessage, guessed.ErrorMessage);
        Assert.Equal(missing.ErrorMessage, sibling.ErrorMessage);
    }

    [Fact]
    public async Task Distinct_customers_in_same_org_authorize_independently()
    {
        var harness = await CustomerLinkCompletenessTests.Harness.CreateAsync();
        var first = await AcceptAsync(harness);
        var secondCustomer = BusinessCustomer.Create(harness.Org.Id, "Second Customer", T0, email: "rosa@example.com");
        await harness.Customers.AddAsync(secondCustomer);
        var created = await harness.CreateRequest.ExecuteAsync(
            harness.Org.Id,
            secondCustomer.Id,
            harness.Personal.NormalizedEmail,
            invitedByUserId: null);
        Assert.True(created.IsSuccess, created.ErrorMessage);
        var secondAccepted = await harness.Accept.ExecuteAsync(
            created.Value!.AcceptToken!,
            harness.Personal.Id,
            AccountClass.Personal);
        Assert.True(secondAccepted.IsSuccess, secondAccepted.ErrorMessage);

        var firstAuthz = await AuthorizeDefaultAsync(harness);
        var secondAuthz = await harness.Authorize.ExecuteAsync(
            harness.Personal.Id,
            AccountClass.Personal,
            harness.Org.Id,
            secondCustomer.Id);

        Assert.True(firstAuthz.IsSuccess);
        Assert.True(secondAuthz.IsSuccess);
        Assert.Equal(first.LinkedCustomerAppUserId, firstAuthz.Value!.LinkedCustomerAppUserId);
        Assert.Equal(secondAccepted.Value!.LinkedCustomerAppUserId, secondAuthz.Value!.LinkedCustomerAppUserId);
        Assert.NotEqual(firstAuthz.Value.PlatformBusinessCustomerId, secondAuthz.Value.PlatformBusinessCustomerId);
    }

    [Fact]
    public async Task Archived_business_customer_is_not_found()
    {
        var harness = await CustomerLinkCompletenessTests.Harness.CreateAsync();
        await AcceptAsync(harness);
        harness.Customer.Archive(T0.AddMinutes(1));
        await harness.Customers.UpdateAsync(harness.Customer);
        AssertNotFound(await AuthorizeDefaultAsync(harness));
    }

    [Fact]
    public async Task Customer_identity_mismatch_is_not_found()
    {
        var harness = await CustomerLinkCompletenessTests.Harness.CreateAsync();
        await AcceptAsync(harness);
        harness.Customer.UnlinkAppUser(T0.AddMinutes(1));
        await harness.Customers.UpdateAsync(harness.Customer);
        AssertNotFound(await AuthorizeDefaultAsync(harness));
    }

    [Fact]
    public async Task Wp02_accept_list_and_unlink_still_work()
    {
        var harness = await CustomerLinkCompletenessTests.Harness.CreateAsync();
        var accepted = await harness.Accept.ExecuteAsync(
            harness.AcceptToken,
            harness.Personal.Id,
            AccountClass.Personal);
        Assert.True(accepted.IsSuccess, accepted.ErrorMessage);
        Assert.Equal(1, (await harness.List.ExecuteAsync(harness.Personal.Id, 1, 20)).TotalCount);
        Assert.True((await harness.Unlink.ExecuteForOwnerAsync(
            LinkedCustomerAppUserId.From(accepted.Value!.LinkedCustomerAppUserId),
            harness.Personal.Id)).IsSuccess);
        Assert.Equal(0, (await harness.List.ExecuteAsync(harness.Personal.Id, 1, 20)).TotalCount);
    }

    private static async Task<ExItS.Platform.Application.Organizations.AcceptCustomerLinkResultDto> AcceptAsync(
        CustomerLinkCompletenessTests.Harness harness)
    {
        var accepted = await harness.Accept.ExecuteAsync(
            harness.AcceptToken,
            harness.Personal.Id,
            AccountClass.Personal);
        Assert.True(accepted.IsSuccess, accepted.ErrorMessage);
        return accepted.Value!;
    }

    private static Task<ApplicationResult<AuthorizedLinkedCustomerPlatformContext>> AuthorizeDefaultAsync(
        CustomerLinkCompletenessTests.Harness harness) =>
        harness.Authorize.ExecuteAsync(
            harness.Personal.Id,
            AccountClass.Personal,
            harness.Org.Id,
            harness.Customer.Id);

    private static void AssertNotFound(ApplicationResult<AuthorizedLinkedCustomerPlatformContext> result)
    {
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.LinkedCustomerAppUserNotFound, result.ErrorCode);
        Assert.Equal("Linked customer was not found.", result.ErrorMessage);
        Assert.DoesNotContain("org", result.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("guid", result.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }
}
