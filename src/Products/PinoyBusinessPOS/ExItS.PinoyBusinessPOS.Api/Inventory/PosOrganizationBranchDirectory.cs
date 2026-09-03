using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Application.Platform;
using Microsoft.Extensions.Options;

namespace ExItS.PinoyBusinessPOS.Api.Inventory;

/// <summary>
/// Platform organization branch directory for POS (transfers, inventory context, reporting).
/// Operational branch existence/active checks use caller-access-filtered branch lists.
/// <see cref="GetPrimaryBranchIdAsync"/> uses structural primary lookup (not assignment-filtered).
/// </summary>
internal sealed class PosOrganizationBranchDirectory(
    HttpClient client,
    IHttpContextAccessor httpContextAccessor,
    IOptions<PlatformAuthOptions> options,
    IHostEnvironment environment) : IOrganizationBranchDirectory, IAuthorizedBranchGroupingDirectory
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private Guid? _primaryOrganizationId;
    private Guid? _cachedPrimaryBranchId;
    private bool _primaryResolved;

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
        PlatformCallerCredentialForwarder.CopyTo(httpContextAccessor.HttpContext?.Request, platformRequest);

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
        catch (JsonException)
        {
            return new Dictionary<Guid, string>();
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new Dictionary<Guid, string>();
        }
    }

    public async Task<bool> IsActiveInOrganizationAsync(
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

        var branch = await GetBranchAsync(organizationId, branchId, cancellationToken).ConfigureAwait(false);
        return branch is not null
            && string.Equals(branch.Status, "Active", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Branch grouping for hierarchical reads. Platform List Branches already filters by caller
    /// branch access, so an inaccessible branch never reaches an area subtotal.
    /// </summary>
    public async Task<IReadOnlyList<AuthorizedBranchGrouping>> ListAuthorizedAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        var branches = await FetchBranchesAsync(organizationId, cancellationToken).ConfigureAwait(false);
        return branches
            .Select(b => new AuthorizedBranchGrouping(
                b.Id,
                string.IsNullOrWhiteSpace(b.Name) ? b.Code : b.Name,
                b.AreaId,
                b.AreaName))
            .ToList();
    }

    private async Task<OrganizationBranchDto?> GetBranchAsync(
        Guid organizationId,
        Guid branchId,
        CancellationToken cancellationToken)
    {
        var branches = await FetchBranchesAsync(organizationId, cancellationToken).ConfigureAwait(false);
        return branches.FirstOrDefault(b => b.Id == branchId);
    }

    private async Task<IReadOnlyList<OrganizationBranchDto>> FetchBranchesAsync(
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        if (client.BaseAddress is null)
        {
            var baseUrl = options.Value.BaseUrl;
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                return [];
            }

            client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/", UriKind.Absolute);
        }

        using var platformRequest = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/v1/platform/organizations/{organizationId:D}/branches");
        PlatformCallerCredentialForwarder.CopyTo(httpContextAccessor.HttpContext?.Request, platformRequest);

        try
        {
            using var response = await client.SendAsync(platformRequest, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return [];
            }

            return await response.Content
                .ReadFromJsonAsync<IReadOnlyList<OrganizationBranchDto>>(JsonOptions, cancellationToken)
                .ConfigureAwait(false)
                ?? [];
        }
        catch (HttpRequestException)
        {
            return [];
        }
        catch (JsonException)
        {
            return [];
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return [];
        }
    }

    public async Task<Guid?> GetPrimaryBranchIdAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        if (_primaryResolved && _primaryOrganizationId == organizationId)
        {
            return _cachedPrimaryBranchId;
        }

        Guid? primary;
        if (environment.IsEnvironment("Testing"))
        {
            primary = Guid.Parse("11111111-1111-1111-1111-111111111111");
        }
        else
        {
            primary = await FetchPrimaryBranchIdAsync(organizationId, cancellationToken).ConfigureAwait(false);
        }

        _primaryOrganizationId = organizationId;
        _cachedPrimaryBranchId = primary;
        _primaryResolved = true;
        return primary;
    }

    private async Task<Guid?> FetchPrimaryBranchIdAsync(
        Guid organizationId,
        CancellationToken cancellationToken)
    {
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
            $"api/v1/platform/organizations/{organizationId:D}/primary-branch");
        PlatformCallerCredentialForwarder.CopyTo(httpContextAccessor.HttpContext?.Request, platformRequest);

        try
        {
            using var response = await client.SendAsync(platformRequest, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var primary = await response.Content
                .ReadFromJsonAsync<OrganizationPrimaryBranchResponse>(JsonOptions, cancellationToken)
                .ConfigureAwait(false);
            return primary?.BranchId;
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }

    private sealed record OrganizationPrimaryBranchResponse(Guid BranchId);
}
