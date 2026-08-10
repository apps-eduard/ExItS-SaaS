using System.Net.Http.Json;
using System.Text.Json;
using ExItS.Platform.Application.Integration.Pos;
using Microsoft.Extensions.Options;

namespace ExItS.Platform.Infrastructure.Integration.Pos;

/// <summary>HttpClient proxy to POS platform-support catalog read API.</summary>
public sealed class PosOrganizationCatalogReadClient : IPosOrganizationCatalogReadClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly PosProductApiOptions _options;

    public PosOrganizationCatalogReadClient(HttpClient httpClient, IOptions<PosProductApiOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<PosOrganizationCatalogSummaryDto> GetOrganizationCatalogAsync(
        Guid organizationId,
        int? page = null,
        int? pageSize = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            throw new InvalidOperationException("PosProductApi:BaseUrl is not configured.");
        }

        if (string.IsNullOrEmpty(_options.SupportApiKey))
        {
            throw new InvalidOperationException("PosProductApi:SupportApiKey is not configured.");
        }

        var query = new List<string>();
        if (page is not null)
        {
            query.Add($"page={page.Value}");
        }

        if (pageSize is not null)
        {
            query.Add($"pageSize={pageSize.Value}");
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query.Add($"search={Uri.EscapeDataString(search.Trim())}");
        }

        var path =
            $"api/v1/pos/platform-support/organizations/{organizationId:D}/catalog"
            + (query.Count == 0 ? string.Empty : "?" + string.Join("&", query));

        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.TryAddWithoutValidation(
            "X-ExItS-Platform-Support-Key",
            _options.SupportApiKey);

        using var response = await _httpClient
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content
            .ReadFromJsonAsync<PosOrganizationCatalogSummaryDto>(JsonOptions, cancellationToken)
            .ConfigureAwait(false);

        return payload
            ?? throw new InvalidOperationException("POS organization catalog response was empty.");
    }
}
