using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.Platform.Application.Common;
using ExItS.Platform.IntegrationTests.Support;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ExItS.Platform.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class ApiPersonalTodoTests(PostgreSqlFixture fixture) : IAsyncLifetime
{
    private SessionApiFactory _factory = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _factory = new SessionApiFactory(fixture.ConnectionString);
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    private async Task<(string Token, Guid UserId)> SeedPersonalUserAsync(string prefix)
    {
        var (userId, email, password) = await PlatformIntegrationTestUsers.RegisterPersonalWithPasswordAsync(_client, prefix);
        var login = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/login",
            new { usernameOrEmail = email, password });
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessionToken").GetString()!;
        return (token, userId);
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

    [Fact]
    public async Task Personal_todo_lifecycle_is_owner_scoped()
    {
        var (token, userId) = await SeedPersonalUserAsync("todo");

        using var listEmpty = Authed(HttpMethod.Get, "/api/v1/personal/todos", token);
        var listEmptyResponse = await _client.SendAsync(listEmpty);
        Assert.Equal(HttpStatusCode.OK, listEmptyResponse.StatusCode);
        Assert.Equal(0, (await listEmptyResponse.Content.ReadFromJsonAsync<JsonElement>()).GetArrayLength());

        using var createRequest = Authed(
            HttpMethod.Post,
            "/api/v1/personal/todos",
            token,
            new { title = "Buy groceries", notes = "Milk and eggs", priority = "Normal" });
        var createResponse = await _client.SendAsync(createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var todoId = created.GetProperty("id").GetGuid();
        Assert.Equal(userId, created.GetProperty("ownerUserIdentityId").GetGuid());
        Assert.Equal("Open", created.GetProperty("status").GetString());
        Assert.Equal(1, created.GetProperty("version").GetInt32());

        using var listAfterCreate = Authed(HttpMethod.Get, "/api/v1/personal/todos", token);
        var listAfterCreateResponse = await _client.SendAsync(listAfterCreate);
        Assert.Equal(HttpStatusCode.OK, listAfterCreateResponse.StatusCode);
        var items = await listAfterCreateResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, items.GetArrayLength());
        Assert.Equal(todoId, items[0].GetProperty("id").GetGuid());

        using var updateRequest = Authed(
            HttpMethod.Put,
            $"/api/v1/personal/todos/{todoId}",
            token,
            new
            {
                title = "Buy groceries and bread",
                notes = "Updated",
                priority = "High",
                expectedVersion = created.GetProperty("version").GetInt32()
            });
        var updateResponse = await _client.SendAsync(updateRequest);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("High", updated.GetProperty("priority").GetString());
        Assert.Equal(2, updated.GetProperty("version").GetInt32());

        using var completeRequest = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/todos/{todoId}/complete",
            token,
            new { expectedVersion = updated.GetProperty("version").GetInt32() });
        var completeResponse = await _client.SendAsync(completeRequest);
        Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);
        var completed = await completeResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Completed", completed.GetProperty("status").GetString());

        using var reopenRequest = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/todos/{todoId}/reopen",
            token,
            new { expectedVersion = completed.GetProperty("version").GetInt32() });
        var reopenResponse = await _client.SendAsync(reopenRequest);
        Assert.Equal(HttpStatusCode.OK, reopenResponse.StatusCode);
        var reopened = await reopenResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Open", reopened.GetProperty("status").GetString());

        using var cancelRequest = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/todos/{todoId}/cancel",
            token,
            new { expectedVersion = reopened.GetProperty("version").GetInt32() });
        var cancelResponse = await _client.SendAsync(cancelRequest);
        Assert.Equal(HttpStatusCode.OK, cancelResponse.StatusCode);
        var cancelled = await cancelResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Cancelled", cancelled.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Unrelated_user_cannot_read_personal_todo()
    {
        var (ownerToken, _) = await SeedPersonalUserAsync("toda");
        var (otherToken, _) = await SeedPersonalUserAsync("todb");

        using var createRequest = Authed(
            HttpMethod.Post,
            "/api/v1/personal/todos",
            ownerToken,
            new { title = "Private task" });
        var createResponse = await _client.SendAsync(createRequest);
        createResponse.EnsureSuccessStatusCode();
        var todoId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        using var denied = Authed(HttpMethod.Get, $"/api/v1/personal/todos/{todoId}", otherToken);
        var response = await _client.SendAsync(denied);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(ApplicationErrorCodes.PersonalTodoUnauthorized, body.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Cancelled_todo_persists_and_reactivates_with_same_id()
    {
        var (token, _) = await SeedPersonalUserAsync("canc");

        using var createRequest = Authed(
            HttpMethod.Post,
            "/api/v1/personal/todos",
            token,
            new { title = "Persist cancel test" });
        var createResponse = await _client.SendAsync(createRequest);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var todoId = created.GetProperty("id").GetGuid();
        Assert.Equal("Open", created.GetProperty("status").GetString());

        using var cancelRequest = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/todos/{todoId}/cancel",
            token,
            new { expectedVersion = created.GetProperty("version").GetInt32() });
        var cancelResponse = await _client.SendAsync(cancelRequest);
        Assert.Equal(HttpStatusCode.OK, cancelResponse.StatusCode);
        var cancelled = await cancelResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Cancelled", cancelled.GetProperty("status").GetString());
        Assert.Equal(2, cancelled.GetProperty("version").GetInt32());

        using var getCancelled = Authed(HttpMethod.Get, $"/api/v1/personal/todos/{todoId}", token);
        var getCancelledResponse = await _client.SendAsync(getCancelled);
        getCancelledResponse.EnsureSuccessStatusCode();
        var fetchedCancelled = await getCancelledResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(todoId, fetchedCancelled.GetProperty("id").GetGuid());
        Assert.Equal("Cancelled", fetchedCancelled.GetProperty("status").GetString());

        using var listCancelled = Authed(HttpMethod.Get, "/api/v1/personal/todos?status=Cancelled", token);
        var listCancelledResponse = await _client.SendAsync(listCancelled);
        listCancelledResponse.EnsureSuccessStatusCode();
        var cancelledItems = await listCancelledResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(cancelledItems.EnumerateArray(), item => item.GetProperty("id").GetGuid() == todoId);

        using var freshList = Authed(HttpMethod.Get, "/api/v1/personal/todos", token);
        var freshListResponse = await _client.SendAsync(freshList);
        freshListResponse.EnsureSuccessStatusCode();
        Assert.Contains(
            (await freshListResponse.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray(),
            item => item.GetProperty("id").GetGuid() == todoId
                && item.GetProperty("status").GetString() == "Cancelled");

        using var reopenRequest = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/todos/{todoId}/reopen",
            token,
            new { expectedVersion = cancelled.GetProperty("version").GetInt32() });
        var reopenResponse = await _client.SendAsync(reopenRequest);
        Assert.Equal(HttpStatusCode.OK, reopenResponse.StatusCode);
        var reopened = await reopenResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(todoId, reopened.GetProperty("id").GetGuid());
        Assert.Equal("Open", reopened.GetProperty("status").GetString());
        Assert.Equal(3, reopened.GetProperty("version").GetInt32());

        using var getActive = Authed(HttpMethod.Get, $"/api/v1/personal/todos/{todoId}", token);
        var getActiveResponse = await _client.SendAsync(getActive);
        getActiveResponse.EnsureSuccessStatusCode();
        var fetchedActive = await getActiveResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Open", fetchedActive.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Unrelated_user_cannot_reactivate_cancelled_todo()
    {
        var (ownerToken, _) = await SeedPersonalUserAsync("cown");
        var (otherToken, _) = await SeedPersonalUserAsync("coth");

        using var createRequest = Authed(
            HttpMethod.Post,
            "/api/v1/personal/todos",
            ownerToken,
            new { title = "Owner cancelled" });
        var createResponse = await _client.SendAsync(createRequest);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var todoId = created.GetProperty("id").GetGuid();

        using var cancelRequest = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/todos/{todoId}/cancel",
            ownerToken,
            new { expectedVersion = created.GetProperty("version").GetInt32() });
        (await _client.SendAsync(cancelRequest)).EnsureSuccessStatusCode();

        using var deniedReopen = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/todos/{todoId}/reopen",
            otherToken,
            new { expectedVersion = 2 });
        var reopenResponse = await _client.SendAsync(deniedReopen);
        Assert.Equal(HttpStatusCode.Forbidden, reopenResponse.StatusCode);
    }

    [Fact]
    public async Task Stale_expected_version_returns_conflict()
    {
        var (token, _) = await SeedPersonalUserAsync("toconf");

        using var createRequest = Authed(
            HttpMethod.Post,
            "/api/v1/personal/todos",
            token,
            new { title = "Versioned task" });
        var createResponse = await _client.SendAsync(createRequest);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var todoId = created.GetProperty("id").GetGuid();
        var staleVersion = created.GetProperty("version").GetInt32() - 1;

        using var updateRequest = Authed(
            HttpMethod.Put,
            $"/api/v1/personal/todos/{todoId}",
            token,
            new
            {
                title = "Conflict",
                expectedVersion = staleVersion
            });
        var response = await _client.SendAsync(updateRequest);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(ApplicationErrorCodes.ConcurrencyConflict, body.GetProperty("errorCode").GetString());
    }
}
