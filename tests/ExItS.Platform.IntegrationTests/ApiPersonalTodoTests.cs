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
