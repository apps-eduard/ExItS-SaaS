using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Organizations;
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
        var username = Unique(prefix);
        var password = "Correct-Horse-9!";
        var email = $"{username}@example.com";
        var create = await _admin.PostAsJsonAsync(
            "/api/v1/platform/users",
            new { username, displayName = "Org Owner", email });
        create.EnsureSuccessStatusCode();
        var userId = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        (await _admin.PutAsJsonAsync(
            $"/api/v1/platform/users/{userId}/credentials/password",
            new { password })).EnsureSuccessStatusCode();

        var org = await _admin.PostAsJsonAsync(
            "/api/v1/platform/organizations",
            new { displayName = $"{prefix} Org", slug = Unique(prefix + "o") });
        org.EnsureSuccessStatusCode();
        var organizationId = (await org.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        (await _admin.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId}/members",
            new { userId, role = "OrganizationOwner" })).EnsureSuccessStatusCode();

        var login = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/login",
            new { usernameOrEmail = username, password });
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessionToken").GetString()!;
        return (userId, username, password, email, token);
    }

    private async Task<(Guid UserId, string Email, string Token)> SeedPersonalUserAsync(string prefix)
    {
        var username = Unique(prefix);
        var password = "Correct-Horse-9!";
        var email = $"{username}@example.com";
        var create = await _admin.PostAsJsonAsync(
            "/api/v1/platform/users",
            new { username, displayName = "Personal User", email });
        create.EnsureSuccessStatusCode();
        var userId = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        (await _admin.PutAsJsonAsync(
            $"/api/v1/platform/users/{userId}/credentials/password",
            new { password })).EnsureSuccessStatusCode();
        var login = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/login",
            new { usernameOrEmail = username, password });
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
        var (staffUserId, staffEmail, staffToken) = await SeedPersonalUserAsync("staf");

        using var staffInvite = Authed(
            HttpMethod.Post,
            $"/api/v1/organizations/{organizationId}/staff-invitations",
            ownerToken,
            new { email = staffEmail, role = "OrganizationMember" });
        var inviteResponse = await _client.SendAsync(staffInvite);
        Assert.Equal(HttpStatusCode.Created, inviteResponse.StatusCode);
        var inviteBody = await inviteResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(
            InvitationKinds.OrganizationStaffInvitation,
            inviteBody.GetProperty("invitationType").GetString());
        var token = inviteBody.GetProperty("acceptToken").GetString();

        using var acceptStaff = Authed(
            HttpMethod.Post,
            "/api/v1/platform/invitations/accept",
            staffToken,
            new { token });
        var acceptResponse = await _client.SendAsync(acceptStaff);
        Assert.Equal(HttpStatusCode.OK, acceptResponse.StatusCode);

        var membersAdmin = await _admin.GetAsync(
            $"/api/v1/platform/organizations/{organizationId}/members?page=1&pageSize=50");
        membersAdmin.EnsureSuccessStatusCode();
        var memberIds = (await membersAdmin.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("items").EnumerateArray()
            .Select(i => i.GetProperty("userId").GetGuid())
            .ToList();
        Assert.Contains(staffUserId, memberIds);

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
}
