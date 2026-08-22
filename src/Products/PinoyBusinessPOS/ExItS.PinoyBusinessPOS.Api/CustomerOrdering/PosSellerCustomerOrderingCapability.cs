using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Application.Platform;
using Microsoft.Extensions.Options;

namespace ExItS.PinoyBusinessPOS.Api.CustomerOrdering;

/// <summary>
/// Resolves seller customer-ordering capability via Personal linked-merchant probe,
/// with commercial-access fallback for seller-staff place paths.
/// </summary>
internal sealed class PosSellerCustomerOrderingCapability(
    HttpClient client,
    IHttpContextAccessor httpContextAccessor,
    IOptions<PlatformAuthOptions> options,
    IHostEnvironment environment,
    IConfiguration configuration,
    IPosCommercialAccessAccessor commercialAccess) : ISellerCustomerOrderingCapability
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<SellerCustomerOrderingCapability> ResolveAsync(
        Guid sellerOrganizationId,
        CancellationToken cancellationToken = default)
    {
        if (sellerOrganizationId == Guid.Empty)
        {
            return new SellerCustomerOrderingCapability(sellerOrganizationId, false, false);
        }

        if (environment.IsEnvironment("Testing")
            && !PosCommercialValidation.IsStrict(configuration))
        {
            return new SellerCustomerOrderingCapability(
                sellerOrganizationId,
                CanCustomerOrder: true,
                CanCustomerDelivery: true,
                OrganizationDisplayName: "Test Merchant");
        }

        var personal = await TryPersonalCapabilityAsync(sellerOrganizationId, cancellationToken)
            .ConfigureAwait(false);
        if (personal is not null)
        {
            return personal;
        }

        var access = commercialAccess.Current;
        if (access.IsKnown)
        {
            var canOrder = access.Allows(UtangCapability.PlaceCustomerOrders);
            var canDelivery = canOrder
                && access.EnabledFeatureCodes.Any(g =>
                    string.Equals(g, PosFeatureCodes.StoreDeliveryOrders, StringComparison.OrdinalIgnoreCase));
            return new SellerCustomerOrderingCapability(
                sellerOrganizationId,
                canOrder,
                canDelivery);
        }

        return new SellerCustomerOrderingCapability(sellerOrganizationId, false, false);
    }

    private async Task<SellerCustomerOrderingCapability?> TryPersonalCapabilityAsync(
        Guid sellerOrganizationId,
        CancellationToken cancellationToken)
    {
        EnsureBaseAddress();
        if (client.BaseAddress is null)
        {
            return null;
        }

        using var platformRequest = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/v1/personal/linked-merchants/{sellerOrganizationId:D}/ordering-capability");
        ForwardAuthHeaders(platformRequest);

        try
        {
            using var response = await client.SendAsync(platformRequest, cancellationToken).ConfigureAwait(false);
            if (response.StatusCode is System.Net.HttpStatusCode.NotFound
                or System.Net.HttpStatusCode.Forbidden)
            {
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                return new SellerCustomerOrderingCapability(sellerOrganizationId, false, false);
            }

            var body = await response.Content
                .ReadFromJsonAsync<LinkedMerchantOrderingCapabilityDto>(JsonOptions, cancellationToken)
                .ConfigureAwait(false);
            if (body is null)
            {
                return new SellerCustomerOrderingCapability(sellerOrganizationId, false, false);
            }

            return new SellerCustomerOrderingCapability(
                sellerOrganizationId,
                body.CanCustomerOrder,
                body.CanCustomerDelivery,
                body.OrganizationDisplayName);
        }
        catch (HttpRequestException)
        {
            return new SellerCustomerOrderingCapability(sellerOrganizationId, false, false);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new SellerCustomerOrderingCapability(sellerOrganizationId, false, false);
        }
        catch (JsonException)
        {
            return new SellerCustomerOrderingCapability(sellerOrganizationId, false, false);
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

    private void ForwardAuthHeaders(HttpRequestMessage platformRequest)
    {
        var source = httpContextAccessor.HttpContext?.Request;
        if (source is null)
        {
            return;
        }

        foreach (var name in new[] { "Authorization", "X-ExItS-Session-Token", "X-Dev-Platform-User-Id" })
        {
            if (source.Headers.TryGetValue(name, out var value))
            {
                platformRequest.Headers.TryAddWithoutValidation(name, value.ToArray());
            }
        }
    }
}
