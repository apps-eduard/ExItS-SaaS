using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Application.Platform;
using Microsoft.Extensions.Options;

namespace ExItS.PinoyBusinessPOS.Api.Inventory;

/// <summary>
/// Resolves Platform organization branches for POS transfer authorization. Testing allows any
/// non-empty branch id so WebApplicationFactory suites do not require a live Platform.
/// </summary>
internal sealed class PosOrganizationBranchDirectory(
    HttpClient client,
    IHttpContextAccessor httpContextAccessor,
    IOptions<PlatformAuthOptions> options,
    IHostEnvironment environment) : IOrganizationBranchDirectory
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<bool> ExistsInOrganizationAsync(
        Guid organizationId,
        Guid branchId,
        CancellationToken cancellationToken = default)
    {
        if (branchId == Guid.Empty)
        {
            return false;
        }

        if (environment.IsEnvironment("Testing"))
        {
            return true;
        }

        var names = await GetNamesAsync(organizationId, [branchId], cancellationToken).ConfigureAwait(false);
        return names.ContainsKey(branchId);
    }

    public async Task<IReadOnlyDictionary<Guid, string>> GetNamesAsync(
        Guid organizationId,
        IReadOnlyCollection<Guid> branchIds,
        CancellationToken cancellationToken = default)
    {
        var wanted = branchIds.Where(id => id != Guid.Empty).Distinct().ToList();
        if (wanted.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        if (environment.IsEnvironment("Testing"))
        {
            return wanted.ToDictionary(id => id, id => "Branch");
        }

        if (client.BaseAddress is null)
        {
            var baseUrl = options.Value.BaseUrl;
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                return new Dictionary<Guid, string>();
            }

            client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/", UriKind.Absolute);
        }

        using var platformRequest = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/v1/platform/organizations/{organizationId:D}/branches");
        var source = httpContextAccessor.HttpContext?.Request;
        if (source is not null)
        {
            foreach (var name in new[] { "Authorization", "X-ExItS-Session-Token", "X-Dev-Platform-User-Id" })
            {
                if (source.Headers.TryGetValue(name, out var value))
                {
                    platformRequest.Headers.TryAddWithoutValidation(name, value.ToArray());
                }
            }
        }

        try
        {
            using var response = await client.SendAsync(platformRequest, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return new Dictionary<Guid, string>();
            }

            var branches = await response.Content
                .ReadFromJsonAsync<IReadOnlyList<OrganizationBranchDto>>(JsonOptions, cancellationToken)
                .ConfigureAwait(false)
                ?? [];
            return branches
                .Where(b => wanted.Contains(b.Id))
                .ToDictionary(b => b.Id, b => string.IsNullOrWhiteSpace(b.Name) ? b.Code : b.Name);
        }
        catch (HttpRequestException)
        {
            return new Dictionary<Guid, string>();
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new Dictionary<Guid, string>();
        }
    }

    public async Task<Guid?> GetPrimaryBranchIdAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        if (environment.IsEnvironment("Testing"))
        {
            return null;
        }

        if (client.BaseAddress is null)
        {
            var baseUrl = options.Value.BaseUrl;
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                return null;
            }

            client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/", UriKind.Absolute);
        }

        using var platformRequest = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/v1/platform/organizations/{organizationId:D}/branches");
        var source = httpContextAccessor.HttpContext?.Request;
        if (source is not null)
        {
            foreach (var name in new[] { "Authorization", "X-ExItS-Session-Token", "X-Dev-Platform-User-Id" })
            {
                if (source.Headers.TryGetValue(name, out var value))
                {
                    platformRequest.Headers.TryAddWithoutValidation(name, value.ToArray());
                }
            }
        }

        try
        {
            using var response = await client.SendAsync(platformRequest, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var branches = await response.Content
                .ReadFromJsonAsync<IReadOnlyList<OrganizationBranchDto>>(JsonOptions, cancellationToken)
                .ConfigureAwait(false)
                ?? [];
            return branches.FirstOrDefault(b => b.IsPrimary)?.Id;
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }
}
