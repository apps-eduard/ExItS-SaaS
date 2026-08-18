using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Application.Platform;
using Microsoft.Extensions.Options;

namespace ExItS.PinoyBusinessPOS.Api.CustomerOrdering;

/// <summary>
/// Loads Platform branch fulfillment snapshots for customer ordering. Testing allows synthetic branches.
/// </summary>
internal sealed class PosCustomerOrderBranchDirectory(
    HttpClient client,
    IHttpContextAccessor httpContextAccessor,
    IOptions<PlatformAuthOptions> options,
    IHostEnvironment environment) : ICustomerOrderBranchDirectory
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<CustomerOrderBranchSnapshot?> GetBranchAsync(
        Guid sellerOrganizationId,
        Guid branchId,
        CancellationToken cancellationToken = default)
    {
        if (branchId == Guid.Empty)
        {
            return null;
        }

        var branches = await ListBranchesAsync(sellerOrganizationId, cancellationToken).ConfigureAwait(false);
        return branches.FirstOrDefault(b => b.BranchId == branchId);
    }

    public async Task<IReadOnlyList<CustomerOrderBranchSnapshot>> ListBranchesAsync(
        Guid sellerOrganizationId,
        CancellationToken cancellationToken = default)
    {
        if (sellerOrganizationId == Guid.Empty)
        {
            return [];
        }

        if (environment.IsEnvironment("Testing"))
        {
            return
            [
                new CustomerOrderBranchSnapshot(
                    Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                    "Test Branch",
                    CustomerOrderingEnabled: true,
                    PickupEnabled: true,
                    DeliveryEnabled: true,
                    CustomerOrderingOperational: true,
                    PickupOperational: true,
                    DeliveryOperational: true,
                    OnlineOrdersPaused: false,
                    StoreStatusMessage: "Open",
                    Latitude: 14.5995m,
                    Longitude: 120.9842m,
                    new CustomerOrderBranchDeliveryPolicySnapshot(
                        MinimumOrderAmount: 0m,
                        BaseDeliveryFee: 49m,
                        IncludedDistanceKm: 2m,
                        AdditionalFeePerKm: 10m,
                        MaximumDeliveryDistanceKm: 15m,
                        FreeDeliveryThreshold: 500m))
            ];
        }

        EnsureBaseAddress();
        if (client.BaseAddress is null)
        {
            return [];
        }

        using var platformRequest = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/v1/platform/organizations/{sellerOrganizationId:D}/branches");
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
                return [];
            }

            var branches = await response.Content
                .ReadFromJsonAsync<IReadOnlyList<OrganizationBranchDto>>(JsonOptions, cancellationToken)
                .ConfigureAwait(false)
                ?? [];

            return branches
                .Where(b => string.Equals(b.Status, "Active", StringComparison.OrdinalIgnoreCase))
                .Select(MapBranch)
                .ToList();
        }
        catch (HttpRequestException)
        {
            return [];
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private void EnsureBaseAddress()
    {
        if (client.BaseAddress is not null)
        {
            return;
        }

        var baseUrl = options.Value.BaseUrl;
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return;
        }

        client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/", UriKind.Absolute);
    }

    private static CustomerOrderBranchSnapshot MapBranch(OrganizationBranchDto branch)
    {
        CustomerOrderBranchDeliveryPolicySnapshot? policy = null;
        if (branch.DeliveryPolicy is not null)
        {
            policy = new CustomerOrderBranchDeliveryPolicySnapshot(
                branch.DeliveryPolicy.MinimumOrderAmount,
                branch.DeliveryPolicy.BaseDeliveryFee,
                branch.DeliveryPolicy.IncludedDistanceKm,
                branch.DeliveryPolicy.AdditionalFeePerKm,
                branch.DeliveryPolicy.MaximumDeliveryDistanceKm,
                branch.DeliveryPolicy.FreeDeliveryThreshold);
        }

        return new CustomerOrderBranchSnapshot(
            branch.Id,
            string.IsNullOrWhiteSpace(branch.Name) ? branch.Code : branch.Name,
            branch.CustomerOrderingEnabled,
            branch.PickupEnabled,
            branch.DeliveryEnabled,
            branch.CustomerOrderingOperational,
            branch.PickupOperational,
            branch.DeliveryOperational,
            branch.OnlineOrdersPaused,
            branch.StoreStatusMessage,
            branch.Latitude,
            branch.Longitude,
            policy,
            branch.IsPrimary);
    }
}
