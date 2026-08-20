using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.IntegrationTests.Support;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ExItS.Platform.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class ApiOrganizationStaffCustomerSeparationTests(PostgreSqlFixture fixture) : IAsyncLifetime
{
    private SessionApiFactory _factory = null!;
    private HttpClient _admin = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _factory = new SessionApiFactory(fixture.ConnectionString);
        _admin = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _admin.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    private static string Unique(string prefix) =>
        $"{prefix}{Guid.NewGuid():N}"[..Math.Min(20, prefix.Length + 32)].ToLowerInvariant();

    private async Task<(Guid UserId, string Username, string Password, string Email, string Token)> SeedOrgOwnerAsync(
        string prefix)
    {
        var (userId, email, password) = await PlatformIntegrationTestUsers.RegisterPersonalWithPasswordAsync(_client, prefix);

        var org = await _admin.PostAsJsonAsync(
            "/api/v1/platform/organizations",
            new { displayName = $"{prefix} Org", slug = Unique(prefix + "o") });
        org.EnsureSuccessStatusCode();
        var organizationId = (await org.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        (await _admin.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId}/members",
            new { userId, role = "OrganizationOwner", reason = "integration-test-link" })).EnsureSuccessStatusCode();

        var login = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/login",
            new { usernameOrEmail = email, password });
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessionToken").GetString()!;
        return (userId, email, password, email, token);
    }

    private async Task<(Guid UserId, string Email, string Token)> SeedPersonalUserAsync(string prefix)
    {
        var (userId, email, password) = await PlatformIntegrationTestUsers.RegisterPersonalWithPasswordAsync(_client, prefix);
        var login = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/login",
            new { usernameOrEmail = email, password });
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessionToken").GetString()!;
        return (userId, email, token);
    }

    private static HttpRequestMessage Authed(HttpMethod method, string url, string token, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("X-ExItS-Session-Token", token);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return request;
    }

    private async Task<Guid> ResolveSelectedOrganizationAsync(string token)
    {
        using var me = Authed(HttpMethod.Get, "/api/v1/platform/auth/me", token);
        var response = await _client.SendAsync(me);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("selectedOrganizationId").GetGuid();
    }

    [Fact]
    public async Task Customer_link_accept_creates_no_staff_membership_or_product_role()
    {
        var (_, _, _, _, ownerToken) = await SeedOrgOwnerAsync("own");
        var organizationId = await ResolveSelectedOrganizationAsync(ownerToken);
        var (customerUserId, customerEmail, customerToken) = await SeedPersonalUserAsync("cust");

        using var createCustomer = Authed(
            HttpMethod.Post,
            $"/api/v1/organizations/{organizationId}/customers",
            ownerToken,
            new { displayName = "Store Customer", email = customerEmail, owningProductCode = "pinoybusinesspos" });
        var created = await _client.SendAsync(createCustomer);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var customerBody = await created.Content.ReadFromJsonAsync<JsonElement>();
        var customerId = customerBody.GetProperty("id").GetGuid();
        Assert.False(customerBody.GetProperty("isOrganizationStaff").GetBoolean());
        Assert.Equal("pinoybusinesspos", customerBody.GetProperty("owningProductCode").GetString());

        using var promote = Authed(
            HttpMethod.Post,
            $"/api/v1/organizations/{organizationId}/customers/{customerId}/promote-to-staff",
            ownerToken);
        var promoteResponse = await _client.SendAsync(promote);
        Assert.Equal(HttpStatusCode.Forbidden, promoteResponse.StatusCode);
        var promoteBody = await promoteResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(
            DomainErrorCodes.CustomerToStaffConversionDenied,
            promoteBody.GetProperty("errorCode").GetString());

        using var linkRequest = Authed(
            HttpMethod.Post,
            $"/api/v1/organizations/{organizationId}/customers/{customerId}/link-requests",
            ownerToken,
            new { email = customerEmail });
        var linkResponse = await _client.SendAsync(linkRequest);
        Assert.Equal(HttpStatusCode.Created, linkResponse.StatusCode);
        var linkBody = await linkResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(InvitationKinds.CustomerLinkRequest, linkBody.GetProperty("invitationType").GetString());
        var acceptToken = linkBody.GetProperty("acceptToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(acceptToken));

        using var accept = Authed(
            HttpMethod.Post,
            "/api/v1/organizations/customer-link-requests/accept",
            customerToken,
            new { token = acceptToken });
        var acceptResponse = await _client.SendAsync(accept);
        Assert.Equal(HttpStatusCode.OK, acceptResponse.StatusCode);
        var acceptBody = await acceptResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(customerUserId, acceptBody.GetProperty("linkedUserIdentityId").GetGuid());
        Assert.False(acceptBody.GetProperty("createdOrganizationMembership").GetBoolean());
        Assert.False(acceptBody.GetProperty("grantedStaffRole").GetBoolean());
        Assert.False(acceptBody.GetProperty("grantedProductRole").GetBoolean());

        // Prove linked customer is absent from staff membership directory.
        var membersAdmin = await _admin.GetAsync(
            $"/api/v1/platform/organizations/{organizationId}/members?page=1&pageSize=50");
        membersAdmin.EnsureSuccessStatusCode();
        var membersBody = await membersAdmin.Content.ReadFromJsonAsync<JsonElement>();
        var memberIds = membersBody.GetProperty("items").EnumerateArray()
            .Select(i => i.GetProperty("userId").GetGuid())
            .ToList();
        Assert.DoesNotContain(customerUserId, memberIds);

        using var linked = Authed(
            HttpMethod.Get,
            $"/api/v1/organizations/{organizationId}/linked-customer-app-users",
            ownerToken);
        var linkedResponse = await _client.SendAsync(linked);
        Assert.Equal(HttpStatusCode.OK, linkedResponse.StatusCode);
        var linkedBody = await linkedResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, linkedBody.GetProperty("totalCount").GetInt32());
        var linkedItem = linkedBody.GetProperty("items")[0];
        Assert.False(linkedItem.GetProperty("isOrganizationStaff").GetBoolean());
        Assert.False(linkedItem.GetProperty("grantedProductRole").GetBoolean());
        Assert.Equal(customerId, linkedItem.GetProperty("businessCustomerId").GetGuid());
    }

    [Fact]
    public async Task Staff_roles_cannot_expose_unrelated_personal_utang_records()
    {
        var (_, _, _, _, ownerToken) = await SeedOrgOwnerAsync("stf");
        var organizationId = await ResolveSelectedOrganizationAsync(ownerToken);
        var (personalUserId, personalEmail, personalToken) = await SeedPersonalUserAsync("pers");

        using var createCustomer = Authed(
            HttpMethod.Post,
            $"/api/v1/organizations/{organizationId}/products/pinoybusinesspos/customers",
            ownerToken,
            new { displayName = "Personal Also Customer", email = personalEmail });
        var created = await _client.SendAsync(createCustomer);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var customerId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        using var linkRequest = Authed(
            HttpMethod.Post,
            $"/api/v1/organizations/{organizationId}/customers/{customerId}/link-requests",
            ownerToken,
            new { email = personalEmail });
        var linkResponse = await _client.SendAsync(linkRequest);
        linkResponse.EnsureSuccessStatusCode();
        var acceptToken = (await linkResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("acceptToken").GetString();

        using var accept = Authed(
            HttpMethod.Post,
            "/api/v1/organizations/customer-link-requests/accept",
            personalToken,
            new { token = acceptToken });
        (await _client.SendAsync(accept)).EnsureSuccessStatusCode();

        using var contactRequest = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/contacts",
            personalToken,
            new { displayName = "Friend", email = "friend@example.com" });
        var contactResponse = await _client.SendAsync(contactRequest);
        Assert.Equal(HttpStatusCode.Created, contactResponse.StatusCode);
        var contactId = (await contactResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        using var relationshipRequest = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/relationships",
            personalToken,
            new
            {
                creditorUserIdentityId = personalUserId,
                debtorContactId = contactId,
                currencyCode = "PHP",
                initialLoanAmount = 99m
            });
        var relationshipResponse = await _client.SendAsync(relationshipRequest);
        Assert.Equal(HttpStatusCode.Created, relationshipResponse.StatusCode);
        var relationshipId = (await relationshipResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        using var staffPersonal = Authed(
            HttpMethod.Get,
            $"/api/v1/personal/utang/relationships/{relationshipId}",
            ownerToken);
        var denied = await _client.SendAsync(staffPersonal);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        var deniedBody = await denied.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(ApplicationErrorCodes.AccountScopeDenied, deniedBody.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Staff_invitation_creates_membership_and_credit_customer_stays_non_staff()
    {
        var (ownerId, _, _, _, ownerToken) = await SeedOrgOwnerAsync("inv");
        _ = ownerId;
        var organizationId = await ResolveSelectedOrganizationAsync(ownerToken);
        var (personalUserId, contactEmail, personalToken) = await SeedPersonalUserAsync("staf");

        using var staffInvite = Authed(
            HttpMethod.Post,
            $"/api/v1/organizations/{organizationId}/staff-invitations",
            ownerToken,
            new { email = contactEmail, role = "OrganizationMember" });
        var inviteResponse = await _client.SendAsync(staffInvite);
        Assert.Equal(HttpStatusCode.Created, inviteResponse.StatusCode);
        var inviteBody = await inviteResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(
            InvitationKinds.OrganizationStaffInvitation,
            inviteBody.GetProperty("invitationType").GetString());
        var token = inviteBody.GetProperty("acceptToken").GetString();

        var anonymousAccept = await _client.PostAsJsonAsync(
            "/api/v1/platform/invitations/accept",
            new { token, password = "Correct-Horse-9!" });
        Assert.Equal(HttpStatusCode.Conflict, anonymousAccept.StatusCode);
        var anonymousBody = await anonymousAccept.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(
            ApplicationErrorCodes.InvitationRequiresAuthenticatedPersonal,
            anonymousBody.GetProperty("errorCode").GetString());

        using var acceptRequest = Authed(
            HttpMethod.Post,
            "/api/v1/platform/invitations/accept-as-personal",
            personalToken,
            new { token, password = "Correct-Horse-9!" });
        var acceptResponse = await _client.SendAsync(acceptRequest);
        Assert.Equal(HttpStatusCode.OK, acceptResponse.StatusCode);
        var acceptBody = await acceptResponse.Content.ReadFromJsonAsync<JsonElement>();
        var staffUserId = acceptBody.GetProperty("userId").GetGuid();
        Assert.NotEqual(personalUserId, staffUserId);
        Assert.Equal(personalUserId, acceptBody.GetProperty("linkedPersonalUserId").GetGuid());
        Assert.Contains("@ORG", acceptBody.GetProperty("staffLogin").GetString(), StringComparison.OrdinalIgnoreCase);

        var membersAdmin = await _admin.GetAsync(
            $"/api/v1/platform/organizations/{organizationId}/members?page=1&pageSize=50");
        membersAdmin.EnsureSuccessStatusCode();
        var memberIds = (await membersAdmin.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("items").EnumerateArray()
            .Select(i => i.GetProperty("userId").GetGuid())
            .ToList();
        Assert.Contains(staffUserId, memberIds);
        Assert.DoesNotContain(personalUserId, memberIds);

        using var createCustomer = Authed(
            HttpMethod.Post,
            $"/api/v1/organizations/{organizationId}/customers",
            ownerToken,
            new { displayName = "Credit Only", email = "creditonly@example.com" });
        var created = await _client.SendAsync(createCustomer);
        created.EnsureSuccessStatusCode();
        var customerId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        using var enableCredit = Authed(
            HttpMethod.Post,
            $"/api/v1/organizations/{organizationId}/customers/{customerId}/credit",
            ownerToken,
            new { currencyCode = "PHP" });
        var creditResponse = await _client.SendAsync(enableCredit);
        Assert.Equal(HttpStatusCode.Created, creditResponse.StatusCode);
        var creditBody = await creditResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(creditBody.GetProperty("isOrganizationStaff").GetBoolean());

        using var listCredit = Authed(
            HttpMethod.Get,
            $"/api/v1/organizations/{organizationId}/credit-customers",
            ownerToken);
        var listResponse = await _client.SendAsync(listCredit);
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.True(
            (await listResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("totalCount").GetInt32() >= 1);
    }

    [Fact]
    public async Task Product_local_customer_route_is_isolated_from_personal_utang()
    {
        var (_, _, _, _, ownerToken) = await SeedOrgOwnerAsync("prd");
        var organizationId = await ResolveSelectedOrganizationAsync(ownerToken);

        using var create = Authed(
            HttpMethod.Post,
            $"/api/v1/organizations/{organizationId}/products/pinoybusinesspos/customers",
            ownerToken,
            new { displayName = "POS Customer", phone = "+639171234567" });
        var created = await _client.SendAsync(create);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var body = await created.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("pinoybusinesspos", body.GetProperty("owningProductCode").GetString());
        Assert.False(body.GetProperty("isOrganizationStaff").GetBoolean());

        using var personalDenied = Authed(HttpMethod.Get, "/api/v1/personal/utang/contacts", ownerToken);
        var denied = await _client.SendAsync(personalDenied);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
    }

    [Fact]
    public async Task Organization_session_cannot_accept_customer_link()
    {
        var (_, _, _, _, ownerToken) = await SeedOrgOwnerAsync("oac");
        var organizationId = await ResolveSelectedOrganizationAsync(ownerToken);
        var (_, customerEmail, _) = await SeedPersonalUserAsync("oacc");

        using var createCustomer = Authed(
            HttpMethod.Post,
            $"/api/v1/organizations/{organizationId}/customers",
            ownerToken,
            new { displayName = "Store Customer", email = customerEmail });
        var created = await _client.SendAsync(createCustomer);
        created.EnsureSuccessStatusCode();
        var customerId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        using var linkRequest = Authed(
            HttpMethod.Post,
            $"/api/v1/organizations/{organizationId}/customers/{customerId}/link-requests",
            ownerToken,
            new { email = customerEmail });
        var linkResponse = await _client.SendAsync(linkRequest);
        linkResponse.EnsureSuccessStatusCode();
        var acceptToken = (await linkResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("acceptToken").GetString();

        using var accept = Authed(
            HttpMethod.Post,
            "/api/v1/organizations/customer-link-requests/accept",
            ownerToken,
            new { token = acceptToken });
        var acceptResponse = await _client.SendAsync(accept);
        Assert.Equal(HttpStatusCode.Forbidden, acceptResponse.StatusCode);
        var body = await acceptResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(ApplicationErrorCodes.AccountScopeDenied, body.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Unrelated_personal_user_cannot_accept_customer_link()
    {
        var (_, _, _, _, ownerToken) = await SeedOrgOwnerAsync("unr");
        var organizationId = await ResolveSelectedOrganizationAsync(ownerToken);
        var (_, customerEmail, _) = await SeedPersonalUserAsync("unrc");
        var (_, _, otherToken) = await SeedPersonalUserAsync("unro");

        using var createCustomer = Authed(
            HttpMethod.Post,
            $"/api/v1/organizations/{organizationId}/customers",
            ownerToken,
            new { displayName = "Store Customer", email = customerEmail });
        var created = await _client.SendAsync(createCustomer);
        created.EnsureSuccessStatusCode();
        var customerId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        using var linkRequest = Authed(
            HttpMethod.Post,
            $"/api/v1/organizations/{organizationId}/customers/{customerId}/link-requests",
            ownerToken,
            new { email = customerEmail });
        var linkResponse = await _client.SendAsync(linkRequest);
        linkResponse.EnsureSuccessStatusCode();
        var acceptToken = (await linkResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("acceptToken").GetString();

        using var accept = Authed(
            HttpMethod.Post,
            "/api/v1/organizations/customer-link-requests/accept",
            otherToken,
            new { token = acceptToken });
        var acceptResponse = await _client.SendAsync(accept);
        Assert.Equal(HttpStatusCode.BadRequest, acceptResponse.StatusCode);
        var body = await acceptResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(DomainErrorCodes.CustomerLinkRequestEmailMismatch, body.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Personal_linked_merchants_list_unlink_and_identifier_guessing()
    {
        var (_, _, _, _, ownerToken) = await SeedOrgOwnerAsync("lst");
        var organizationId = await ResolveSelectedOrganizationAsync(ownerToken);
        var (_, customerEmail, customerToken) = await SeedPersonalUserAsync("lstc");
        var (_, _, otherToken) = await SeedPersonalUserAsync("lsto");

        using var createCustomer = Authed(
            HttpMethod.Post,
            $"/api/v1/organizations/{organizationId}/customers",
            ownerToken,
            new { displayName = "Store Customer", email = customerEmail });
        var created = await _client.SendAsync(createCustomer);
        created.EnsureSuccessStatusCode();
        var customerId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        using var linkRequest = Authed(
            HttpMethod.Post,
            $"/api/v1/organizations/{organizationId}/customers/{customerId}/link-requests",
            ownerToken,
            new { email = customerEmail });
        var linkResponse = await _client.SendAsync(linkRequest);
        linkResponse.EnsureSuccessStatusCode();
        var acceptToken = (await linkResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("acceptToken").GetString();

        using var accept = Authed(
            HttpMethod.Post,
            "/api/v1/organizations/customer-link-requests/accept",
            customerToken,
            new { token = acceptToken });
        var acceptResponse = await _client.SendAsync(accept);
        Assert.Equal(HttpStatusCode.OK, acceptResponse.StatusCode);
        var linkedCustomerId = (await acceptResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("linkedCustomerAppUserId").GetGuid();

        using var listOwn = Authed(HttpMethod.Get, "/api/v1/personal/linked-merchants", customerToken);
        var listOwnResponse = await _client.SendAsync(listOwn);
        Assert.Equal(HttpStatusCode.OK, listOwnResponse.StatusCode);
        var ownBody = await listOwnResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, ownBody.GetProperty("totalCount").GetInt32());
        Assert.Equal(customerId, ownBody.GetProperty("items")[0].GetProperty("businessCustomerId").GetGuid());
        Assert.Equal(linkedCustomerId, ownBody.GetProperty("items")[0].GetProperty("linkedCustomerId").GetGuid());

        using var listOther = Authed(HttpMethod.Get, "/api/v1/personal/linked-merchants", otherToken);
        var listOtherResponse = await _client.SendAsync(listOther);
        Assert.Equal(HttpStatusCode.OK, listOtherResponse.StatusCode);
        Assert.Equal(0, (await listOtherResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("totalCount").GetInt32());

        using var guess = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/linked-merchants/{Guid.NewGuid():D}/unlink",
            otherToken);
        var guessResponse = await _client.SendAsync(guess);
        Assert.Equal(HttpStatusCode.NotFound, guessResponse.StatusCode);

        using var steal = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/linked-merchants/{linkedCustomerId:D}/unlink",
            otherToken);
        var stealResponse = await _client.SendAsync(steal);
        Assert.Equal(HttpStatusCode.NotFound, stealResponse.StatusCode);

        using var unlink = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/linked-merchants/{linkedCustomerId:D}/unlink",
            customerToken);
        var unlinkResponse = await _client.SendAsync(unlink);
        Assert.Equal(HttpStatusCode.OK, unlinkResponse.StatusCode);
        Assert.Equal(
            "Revoked",
            (await unlinkResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString());

        using var listAfter = Authed(HttpMethod.Get, "/api/v1/personal/linked-merchants", customerToken);
        var listAfterResponse = await _client.SendAsync(listAfter);
        Assert.Equal(HttpStatusCode.OK, listAfterResponse.StatusCode);
        Assert.Equal(0, (await listAfterResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task Personal_linked_customer_authorization_is_fail_closed()
    {
        var (_, _, _, _, ownerToken) = await SeedOrgOwnerAsync("atz");
        var organizationId = await ResolveSelectedOrganizationAsync(ownerToken);
        var (_, customerEmail, customerToken) = await SeedPersonalUserAsync("atzc");
        var (_, _, otherToken) = await SeedPersonalUserAsync("atzo");

        using var createCustomer = Authed(
            HttpMethod.Post,
            $"/api/v1/organizations/{organizationId}/customers",
            ownerToken,
            new { displayName = "Store Customer", email = customerEmail });
        var created = await _client.SendAsync(createCustomer);
        created.EnsureSuccessStatusCode();
        var customerId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        using var linkRequest = Authed(
            HttpMethod.Post,
            $"/api/v1/organizations/{organizationId}/customers/{customerId}/link-requests",
            ownerToken,
            new { email = customerEmail });
        var linkResponse = await _client.SendAsync(linkRequest);
        linkResponse.EnsureSuccessStatusCode();
        var acceptToken = (await linkResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("acceptToken").GetString();

        var authzUrl =
            $"/api/v1/personal/linked-merchants/authorization?organizationId={organizationId:D}&businessCustomerId={customerId:D}";

        using var pending = Authed(HttpMethod.Get, authzUrl, customerToken);
        var pendingResponse = await _client.SendAsync(pending);
        Assert.Equal(HttpStatusCode.NotFound, pendingResponse.StatusCode);
        Assert.Equal(
            ApplicationErrorCodes.LinkedCustomerAppUserNotFound,
            (await pendingResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errorCode").GetString());

        using var accept = Authed(
            HttpMethod.Post,
            "/api/v1/organizations/customer-link-requests/accept",
            customerToken,
            new { token = acceptToken });
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(accept)).StatusCode);

        using var success = Authed(HttpMethod.Get, authzUrl, customerToken);
        var successResponse = await _client.SendAsync(success);
        Assert.Equal(HttpStatusCode.OK, successResponse.StatusCode);
        var body = await successResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(
            new[] { "linkedCustomerAppUserId", "organizationId", "personalUserId", "platformBusinessCustomerId" },
            body.EnumerateObject().Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray());
        Assert.Equal(organizationId, body.GetProperty("organizationId").GetGuid());
        Assert.Equal(customerId, body.GetProperty("platformBusinessCustomerId").GetGuid());
        Assert.False(body.TryGetProperty("posCustomerId", out _));

        using var other = Authed(HttpMethod.Get, authzUrl, otherToken);
        var otherResponse = await _client.SendAsync(other);
        Assert.Equal(HttpStatusCode.NotFound, otherResponse.StatusCode);
        Assert.Equal(
            ApplicationErrorCodes.LinkedCustomerAppUserNotFound,
            (await otherResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errorCode").GetString());

        using var guessed = Authed(
            HttpMethod.Get,
            $"/api/v1/personal/linked-merchants/authorization?organizationId={Guid.NewGuid():D}&businessCustomerId={Guid.NewGuid():D}",
            customerToken);
        var guessedResponse = await _client.SendAsync(guessed);
        Assert.Equal(HttpStatusCode.NotFound, guessedResponse.StatusCode);
        Assert.Equal(
            ApplicationErrorCodes.LinkedCustomerAppUserNotFound,
            (await guessedResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errorCode").GetString());

        using var missing = Authed(HttpMethod.Get, "/api/v1/personal/linked-merchants/authorization", customerToken);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.SendAsync(missing)).StatusCode);

        var linkedCustomerId = body.GetProperty("linkedCustomerAppUserId").GetGuid();
        using var unlink = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/linked-merchants/{linkedCustomerId:D}/unlink",
            customerToken);
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(unlink)).StatusCode);

        using var afterUnlink = Authed(HttpMethod.Get, authzUrl, customerToken);
        var afterUnlinkResponse = await _client.SendAsync(afterUnlink);
        Assert.Equal(HttpStatusCode.NotFound, afterUnlinkResponse.StatusCode);
        Assert.Equal(
            ApplicationErrorCodes.LinkedCustomerAppUserNotFound,
            (await afterUnlinkResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Pending_customer_link_revoke_still_blocks_accept()
    {
        var (_, _, _, _, ownerToken) = await SeedOrgOwnerAsync("prv");
        var organizationId = await ResolveSelectedOrganizationAsync(ownerToken);
        var (_, customerEmail, customerToken) = await SeedPersonalUserAsync("prvc");

        using var createCustomer = Authed(
            HttpMethod.Post,
            $"/api/v1/organizations/{organizationId}/customers",
            ownerToken,
            new { displayName = "Store Customer", email = customerEmail });
        var created = await _client.SendAsync(createCustomer);
        created.EnsureSuccessStatusCode();
        var customerId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        using var linkRequest = Authed(
            HttpMethod.Post,
            $"/api/v1/organizations/{organizationId}/customers/{customerId}/link-requests",
            ownerToken,
            new { email = customerEmail });
        var linkResponse = await _client.SendAsync(linkRequest);
        linkResponse.EnsureSuccessStatusCode();
        var linkBody = await linkResponse.Content.ReadFromJsonAsync<JsonElement>();
        var requestId = linkBody.GetProperty("id").GetGuid();
        var acceptToken = linkBody.GetProperty("acceptToken").GetString();

        using var revoke = Authed(
            HttpMethod.Post,
            $"/api/v1/organizations/{organizationId}/customer-link-requests/{requestId}/revoke",
            ownerToken);
        var revokeResponse = await _client.SendAsync(revoke);
        Assert.Equal(HttpStatusCode.OK, revokeResponse.StatusCode);

        using var accept = Authed(
            HttpMethod.Post,
            "/api/v1/organizations/customer-link-requests/accept",
            customerToken,
            new { token = acceptToken });
        var acceptResponse = await _client.SendAsync(accept);
        Assert.Equal(HttpStatusCode.NotFound, acceptResponse.StatusCode);
        var acceptBody = await acceptResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(ApplicationErrorCodes.CustomerLinkRequestNotFound, acceptBody.GetProperty("errorCode").GetString());
    }
}
