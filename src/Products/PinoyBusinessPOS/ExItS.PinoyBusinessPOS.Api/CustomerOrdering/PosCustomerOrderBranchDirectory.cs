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
    private static readonly Guid TestingAreaId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

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
        var fromList = branches.FirstOrDefault(b => b.BranchId == branchId);
        if (fromList is not null)
        {
            return fromList;
        }

        // Staff-filtered org branch lists omit branches the actor cannot operate.
        // Customer-order place/propose/accept still need the order's fulfillment branch
        // (org view auth — not staff assignment). Load by id via readiness + hours.
        return await TryGetOrgBranchByIdAsync(sellerOrganizationId, branchId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<CustomerOrderBranchSnapshot?> GetCommerceBranchAsync(
        Guid sellerOrganizationId,
        Guid branchId,
        string? sellerPublicOrganizationId,
        CancellationToken cancellationToken = default)
    {
        var membershipBranch = await GetBranchAsync(sellerOrganizationId, branchId, cancellationToken)
            .ConfigureAwait(false);
        if (membershipBranch is not null)
        {
            return membershipBranch;
        }

        // Connected buyers are not seller-org members — ListBranches returns empty/403.
        // Fall back to the public Active-branch commerce fulfillment snapshot.
        return await TryGetPublicCommerceBranchAsync(sellerPublicOrganizationId, branchId, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<CustomerOrderBranchSnapshot?> TryGetPublicCommerceBranchAsync(
        string? sellerPublicOrganizationId,
        Guid branchId,
        CancellationToken cancellationToken)
    {
        if (branchId == Guid.Empty || string.IsNullOrWhiteSpace(sellerPublicOrganizationId))
        {
            return null;
        }

        EnsureBaseAddress();
        if (client.BaseAddress is null)
        {
            return null;
        }

        var encoded = Uri.EscapeDataString(sellerPublicOrganizationId.Trim());
        using var platformRequest = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/v1/public/stores/{encoded}/branches/{branchId:D}/commerce-fulfillment");

        try
        {
            using var response = await client.SendAsync(platformRequest, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var dto = await response.Content
                .ReadFromJsonAsync<PublicCommerceFulfillmentDto>(JsonOptions, cancellationToken)
                .ConfigureAwait(false);
            if (dto is null || dto.BranchId != branchId)
            {
                return null;
            }

            return new CustomerOrderBranchSnapshot(
                dto.BranchId,
                string.IsNullOrWhiteSpace(dto.Name) ? "Branch" : dto.Name,
                dto.CustomerOrderingEnabled,
                dto.PickupEnabled,
                dto.DeliveryEnabled,
                CustomerOrderingOperational: false,
                PickupOperational: false,
                DeliveryOperational: false,
                OnlineOrdersPaused: false,
                StoreStatusMessage: null,
                Latitude: null,
                Longitude: null,
                DeliveryPolicy: null,
                IsPrimary: false,
                DeliveryServiceAreas: null,
                PickupReady: dto.PickupReady,
                DeliveryReady: dto.DeliveryReady);
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed record PublicCommerceFulfillmentDto(
        Guid BranchId,
        string Name,
        bool PickupEnabled,
        bool DeliveryEnabled,
        bool PickupReady,
        bool DeliveryReady,
        bool CustomerOrderingEnabled);

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
                        FreeDeliveryThreshold: 500m),
                    IsPrimary: true,
                    DeliveryServiceAreas:
                    [
                        new CustomerOrderDeliveryServiceAreaSnapshot(
                            TestingAreaId,
                            "Manila",
                            "NCR")
                    ],
                    PickupReady: true,
                    DeliveryReady: true,
                    TimeZoneId: "Asia/Manila",
                    StoreIsOpenNow: true)
            ];
        }

        EnsureBaseAddress();
        if (client.BaseAddress is null)
        {
            return [];
        }

        // Personal linked buyers cannot call org membership branch APIs — use linked-merchant projection.
        var personalBranches = await TryListLinkedMerchantBranchesAsync(sellerOrganizationId, cancellationToken)
            .ConfigureAwait(false);
        if (personalBranches is { Count: > 0 })
        {
            return personalBranches;
        }

        var membershipBranches = await TryListOrganizationMembershipBranchesAsync(
                sellerOrganizationId,
                cancellationToken)
            .ConfigureAwait(false);
        if (membershipBranches.Count > 0)
        {
            return membershipBranches;
        }

        // Linked-merchant returned 200 with zero Active rows (or Status filter emptied it).
        return personalBranches ?? membershipBranches;
    }

    private async Task<IReadOnlyList<CustomerOrderBranchSnapshot>> TryListOrganizationMembershipBranchesAsync(
        Guid sellerOrganizationId,
        CancellationToken cancellationToken)
    {
        using var platformRequest = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/v1/platform/organizations/{sellerOrganizationId:D}/branches");
        ForwardPlatformCredentials(platformRequest);

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

    /// <summary>
    /// Loads one org branch for schedule/place without staff branch-assignment filtering.
    /// Uses fulfillment-readiness + operating-hours (EnsureCanViewOrganization).
    /// </summary>
    private async Task<CustomerOrderBranchSnapshot?> TryGetOrgBranchByIdAsync(
        Guid sellerOrganizationId,
        Guid branchId,
        CancellationToken cancellationToken)
    {
        EnsureBaseAddress();
        if (client.BaseAddress is null)
        {
            return null;
        }

        try
        {
            using var readinessRequest = new HttpRequestMessage(
                HttpMethod.Get,
                $"api/v1/platform/organizations/{sellerOrganizationId:D}/branches/{branchId:D}/fulfillment-readiness");
            ForwardPlatformCredentials(readinessRequest);
            using var readinessResponse = await client
                .SendAsync(readinessRequest, cancellationToken)
                .ConfigureAwait(false);
            if (!readinessResponse.IsSuccessStatusCode)
            {
                return null;
            }

            var readiness = await readinessResponse.Content
                .ReadFromJsonAsync<OrgBranchReadinessDto>(JsonOptions, cancellationToken)
                .ConfigureAwait(false);
            if (readiness is null || readiness.BranchId != branchId)
            {
                return null;
            }

            IReadOnlyList<CustomerOrderBranchHoursDaySnapshot>? hours = null;
            using var hoursRequest = new HttpRequestMessage(
                HttpMethod.Get,
                $"api/v1/platform/organizations/{sellerOrganizationId:D}/branches/{branchId:D}/operating-hours");
            ForwardPlatformCredentials(hoursRequest);
            using var hoursResponse = await client
                .SendAsync(hoursRequest, cancellationToken)
                .ConfigureAwait(false);
            if (hoursResponse.IsSuccessStatusCode)
            {
                var days = await hoursResponse.Content
                    .ReadFromJsonAsync<IReadOnlyList<BranchOperatingHoursDayDto>>(JsonOptions, cancellationToken)
                    .ConfigureAwait(false);
                if (days is { Count: > 0 })
                {
                    hours = days
                        .Select(day => new CustomerOrderBranchHoursDaySnapshot(
                            day.DayOfWeek,
                            day.IsClosed,
                            day.IsOpen24Hours,
                            day.OpenTime,
                            day.CloseTime))
                        .ToList();
                }
            }

            return new CustomerOrderBranchSnapshot(
                readiness.BranchId,
                "Branch",
                readiness.CustomerOrderingEnabled,
                readiness.PickupEnabled,
                readiness.DeliveryEnabled,
                readiness.CustomerOrderingOperational,
                readiness.PickupOperational,
                readiness.DeliveryOperational,
                readiness.OnlineOrdersPaused,
                readiness.StoreStatusMessage,
                Latitude: null,
                Longitude: null,
                DeliveryPolicy: null,
                IsPrimary: false,
                DeliveryServiceAreas: null,
                readiness.PickupReady,
                readiness.DeliveryReady,
                readiness.TimeZoneId,
                hours,
                readiness.AcceptScheduledOrdersWhileClosed,
                readiness.MinPreparationLeadMinutes,
                readiness.MaxAdvanceScheduleDays,
                StoreIsOpenNow: readiness.StoreIsOpenNow);
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed record OrgBranchReadinessDto(
        Guid BranchId,
        bool CustomerOrderingEnabled,
        bool PickupEnabled,
        bool DeliveryEnabled,
        bool OnlineOrdersPaused,
        bool CustomerOrderingOperational,
        bool PickupOperational,
        bool DeliveryOperational,
        bool PickupReady,
        bool DeliveryReady,
        bool StoreIsOpenNow,
        string? StoreStatusMessage = null,
        bool AcceptScheduledOrdersWhileClosed = false,
        int MinPreparationLeadMinutes = 0,
        int MaxAdvanceScheduleDays = 0,
        string? TimeZoneId = null);

    private async Task<IReadOnlyList<CustomerOrderBranchSnapshot>?> TryListLinkedMerchantBranchesAsync(
        Guid sellerOrganizationId,
        CancellationToken cancellationToken)
    {
        using var platformRequest = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/v1/personal/linked-merchants/{sellerOrganizationId:D}/fulfillment-branches");
        ForwardPlatformCredentials(platformRequest);

        try
        {
            using var response = await client.SendAsync(platformRequest, cancellationToken).ConfigureAwait(false);
            if (response.StatusCode is System.Net.HttpStatusCode.NotFound
                or System.Net.HttpStatusCode.Forbidden
                or System.Net.HttpStatusCode.Unauthorized)
            {
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                return null;
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
            return null;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private void ForwardPlatformCredentials(HttpRequestMessage platformRequest)
    {
        PlatformCallerCredentialForwarder.CopyTo(httpContextAccessor.HttpContext?.Request, platformRequest);
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

        IReadOnlyList<CustomerOrderDeliveryServiceAreaSnapshot>? areas = null;
        if (branch.ActiveDeliveryServiceAreas is { Count: > 0 })
        {
            areas = branch.ActiveDeliveryServiceAreas
                .Select(a => new CustomerOrderDeliveryServiceAreaSnapshot(
                    a.Id,
                    a.CityMunicipalityName,
                    a.RegionOrProvinceName))
                .ToList();
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
            branch.IsPrimary,
            areas,
            branch.PickupReady,
            branch.DeliveryReady,
            branch.TimeZoneId,
            branch.OperatingHours?
                .Select(day => new CustomerOrderBranchHoursDaySnapshot(
                    day.DayOfWeek,
                    day.IsClosed,
                    day.IsOpen24Hours,
                    day.OpenTime,
                    day.CloseTime))
                .ToList(),
            branch.AcceptScheduledOrdersWhileClosed,
            branch.MinPreparationLeadMinutes,
            branch.MaxAdvanceScheduleDays,
            StoreIsOpenNow: branch.StoreIsOpenNow);
    }
}
