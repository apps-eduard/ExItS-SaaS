using System.Net.Http.Json;

namespace ExItS.Web.UI;

public sealed record WebHandoffCreatedResponse(
    string Ticket,
    string TargetApp,
    string ReturnPath,
    DateTimeOffset ExpiresAtUtc);

public sealed record WebHandoffRedeemedResponse(
    string SessionToken,
    string TargetApp,
    string ReturnPath,
    string AccountClass,
    Guid? OrganizationId,
    DateTimeOffset SessionExpiresAtUtc);

public sealed record WebWorkspaceListResponse(
    IReadOnlyList<WebWorkspaceItemResponse> Workspaces,
    string? CurrentApp,
    Guid? CurrentOrganizationId);

public sealed record WebWorkspaceItemResponse(
    string App,
    string Label,
    Guid? AccountProfileId,
    Guid? OrganizationId,
    string? OrganizationName,
    string? RoleLabel);

public static class WebHandoffHttp
{
    public static async Task<WebHandoffCreatedResponse?> CreateAsync(
        HttpClient client,
        string sessionToken,
        string targetApp,
        Guid? organizationId,
        string? returnPath,
        CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/platform/auth/web-handoff")
        {
            Content = JsonContent.Create(new
            {
                targetApp,
                organizationId,
                returnPath
            })
        };
        request.Headers.TryAddWithoutValidation("X-ExItS-Session-Token", sessionToken);
        using var response = await client.SendAsync(request, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<WebHandoffCreatedResponse>(ct).ConfigureAwait(false);
    }

    public static async Task<WebHandoffRedeemedResponse?> RedeemAsync(
        HttpClient client,
        string ticket,
        CancellationToken ct = default)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/v1/platform/auth/web-handoff/redeem",
            new { ticket },
            ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<WebHandoffRedeemedResponse>(ct).ConfigureAwait(false);
    }

    public static string EstablishUrl(string appOrigin, string ticket, string? returnPath = null)
    {
        var url = appOrigin.TrimEnd('/') + "/session/establish?ticket=" + Uri.EscapeDataString(ticket);
        if (!string.IsNullOrWhiteSpace(returnPath))
        {
            url += "&returnPath=" + Uri.EscapeDataString(returnPath);
        }

        return url;
    }
}
