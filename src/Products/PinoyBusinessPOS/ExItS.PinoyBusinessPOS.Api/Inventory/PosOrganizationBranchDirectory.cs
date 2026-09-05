using System.Net.Http.Headers;
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

        var branches = await FetchBranchesAsync(organizationId, cancellationToken).ConfigureAwait(false);
        return branches
            .Where(b => wanted.Contains(b.Id))
            .ToDictionary(b => b.Id, b => string.IsNullOrWhiteSpace(b.Name) ? b.Code : b.Name);
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

    public async Task<string> GetBranchTypeAsync(
        Guid organizationId,
        Guid branchId,
        CancellationToken cancellationToken = default)
    {
        if (branchId == Guid.Empty)
        {
            return "Retail";
        }

        if (environment.IsEnvironment("Testing"))
        {
            return "Retail";
        }

        var branch = await GetBranchAsync(organizationId, branchId, cancellationToken).ConfigureAwait(false);
        if (branch is null || string.IsNullOrWhiteSpace(branch.BranchType))
        {
            return "Retail";
        }

        return string.Equals(branch.BranchType, "Warehouse", StringComparison.OrdinalIgnoreCase)
            ? "Warehouse"
            : "Retail";
    }

    /// <summary>
    /// Branch grouping for hierarchical reads. Platform List Branches already filters by caller
    /// branch access, so an inaccessible branch never reaches an area subtotal. Platform also owns the
    /// organization-wide decision — POS never infers it from how many branches came back.
    /// </summary>
    public async Task<AuthorizedBranchScope> ListAuthorizedAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        var branches = await FetchBranchesAsync(organizationId, cancellationToken).ConfigureAwait(false);
        var organizationWide = await FetchOrganizationWideAccessAsync(organizationId, cancellationToken)
            .ConfigureAwait(false);
        return new AuthorizedBranchScope(
            organizationWide,
            branches
                .Select(b => new AuthorizedBranchGrouping(
                    b.Id,
                    string.IsNullOrWhiteSpace(b.Name) ? b.Code : b.Name,
                    b.AreaId,
                    b.AreaName))
                .ToList());
    }

    /// <summary>Fails closed: an unreadable scope is treated as partial branch access.</summary>
    private async Task<bool> FetchOrganizationWideAccessAsync(
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        if (environment.IsEnvironment("Testing"))
        {
            return true;
        }

        if (!TryEnsureBaseAddress())
        {
            return false;
        }

        using var platformRequest = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/v1/platform/organizations/{organizationId:D}/branches/my-access");
        ApplyCallerCredentials(platformRequest);

        try
        {
            using var response = await client.SendAsync(platformRequest, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (document.RootElement.TryGetProperty("organizationWide", out var orgWide)
                || document.RootElement.TryGetProperty("OrganizationWide", out orgWide))
            {
                return orgWide.ValueKind == JsonValueKind.True;
            }

            return false;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
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
        if (!TryEnsureBaseAddress())
        {
            return [];
        }

        using var platformRequest = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/v1/platform/organizations/{organizationId:D}/branches");
        ApplyCallerCredentials(platformRequest);

        try
        {
            using var response = await client.SendAsync(platformRequest, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return [];
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return ParseBranchList(document.RootElement);
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
        if (!TryEnsureBaseAddress())
        {
            return null;
        }

        using var platformRequest = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/v1/platform/organizations/{organizationId:D}/primary-branch");
        ApplyCallerCredentials(platformRequest);

        try
        {
            using var response = await client.SendAsync(platformRequest, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (TryReadGuid(document.RootElement, "branchId", out var branchId)
                || TryReadGuid(document.RootElement, "BranchId", out branchId))
            {
                return branchId;
            }

            return null;
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

    private bool TryEnsureBaseAddress()
    {
        if (client.BaseAddress is not null)
        {
            return true;
        }

        var baseUrl = options.Value.BaseUrl;
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return false;
        }

        client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/", UriKind.Absolute);
        return true;
    }

    /// <summary>
    /// Cookie alone can be missing on POS←Vite proxied calls; always attach PlatformSession when known.
    /// </summary>
    private void ApplyCallerCredentials(HttpRequestMessage platformRequest)
    {
        var httpRequest = httpContextAccessor.HttpContext?.Request;
        PlatformCallerCredentialForwarder.CopyTo(httpRequest, platformRequest);

        var token = PlatformCallerCredentialForwarder.ResolvePlatformSessionToken(httpRequest);
        if (string.IsNullOrWhiteSpace(token))
        {
            return;
        }

        platformRequest.Headers.Remove("Authorization");
        platformRequest.Headers.Authorization = new AuthenticationHeaderValue("PlatformSession", token);
        if (!platformRequest.Headers.Contains("X-ExItS-Session-Token"))
        {
            platformRequest.Headers.TryAddWithoutValidation("X-ExItS-Session-Token", token);
        }
    }

    /// <summary>
    /// Tolerant parse: Platform branch payloads grow frequently; full DTO deserialize must not
    /// fail-closed report/transfer branch checks into "Branch was not found".
    /// </summary>
    internal static IReadOnlyList<OrganizationBranchDto> ParseBranchList(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var result = new List<OrganizationBranchDto>();
        foreach (var element in root.EnumerateArray())
        {
            if (element.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if (!TryReadGuid(element, "id", out var id) && !TryReadGuid(element, "Id", out id))
            {
                continue;
            }

            _ = TryReadGuid(element, "organizationId", out var organizationId)
                || TryReadGuid(element, "OrganizationId", out organizationId);

            var code = ReadString(element, "code") ?? ReadString(element, "Code") ?? string.Empty;
            var name = ReadString(element, "name") ?? ReadString(element, "Name") ?? code;
            var status = ReadString(element, "status") ?? ReadString(element, "Status") ?? "Active";
            var isPrimary = ReadBoolean(element, "isPrimary") || ReadBoolean(element, "IsPrimary");
            var branchType = ReadString(element, "branchType") ?? ReadString(element, "BranchType") ?? "Retail";
            Guid? areaId = TryReadGuid(element, "areaId", out var parsedArea)
                || TryReadGuid(element, "AreaId", out parsedArea)
                ? parsedArea
                : null;
            var areaName = ReadString(element, "areaName") ?? ReadString(element, "AreaName");

            result.Add(
                new OrganizationBranchDto(
                    id,
                    organizationId,
                    code,
                    name,
                    isPrimary,
                    status,
                    AreaId: areaId,
                    AreaName: areaName,
                    BranchType: branchType));
        }

        return result;
    }

    private static bool TryReadGuid(JsonElement element, string propertyName, out Guid value)
    {
        value = Guid.Empty;
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return false;
        }

        if (property.ValueKind == JsonValueKind.String
            && Guid.TryParse(property.GetString(), out value))
        {
            return true;
        }

        return false;
    }

    private static string? ReadString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString(),
            JsonValueKind.Number => property.ToString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null
        };
    }

    private static bool ReadBoolean(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return false;
        }

        return property.ValueKind == JsonValueKind.True
            || (property.ValueKind == JsonValueKind.String
                && bool.TryParse(property.GetString(), out var parsed)
                && parsed);
    }
}
