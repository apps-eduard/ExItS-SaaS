using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.CustomerOrdering;

namespace ExItS.PinoyBusinessPOS.Application.Abstractions;

public interface IPosCustomerOrderClient
{
    Task<ApiResult<CustomerOrderPagedResult>> ListSellerOrdersAsync(
        Guid organizationId,
        string? status = null,
        string? fulfillmentType = null,
        Guid? branchId = null,
        string? orderNumber = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);

    Task<ApiResult<CustomerOrderDto>> GetSellerOrderAsync(
        Guid organizationId,
        Guid orderId,
        CancellationToken ct = default);

    Task<ApiResult<CustomerOrderDto>> PlaceSellerOrderAsync(
        Guid organizationId,
        PlaceCustomerOrderRequest request,
        CancellationToken ct = default);

    Task<ApiResult<QuoteCustomerOrderDeliveryDto>> QuoteDeliveryAsync(
        Guid organizationId,
        QuoteCustomerOrderDeliveryRequest request,
        CancellationToken ct = default);

    Task<ApiResult<CustomerOrderDto>> AcceptAsync(
        Guid organizationId,
        Guid orderId,
        CancellationToken ct = default);

    Task<ApiResult<CustomerOrderDto>> RejectAsync(
        Guid organizationId,
        Guid orderId,
        RejectCustomerOrderRequest request,
        CancellationToken ct = default);

    Task<ApiResult<CustomerOrderDto>> CompleteAsync(
        Guid organizationId,
        Guid orderId,
        CancellationToken ct = default);

    Task<ApiResult<CustomerOrderDto>> StartPreparingAsync(
        Guid organizationId,
        Guid orderId,
        CancellationToken ct = default);

    Task<ApiResult<CustomerOrderDto>> MarkReadyAsync(
        Guid organizationId,
        Guid orderId,
        CancellationToken ct = default);

    Task<ApiResult<CustomerOrderDto>> MarkOutForDeliveryAsync(
        Guid organizationId,
        Guid orderId,
        CancellationToken ct = default);

    Task<ApiResult<CustomerOrderDto>> MarkDeliveredAsync(
        Guid organizationId,
        Guid orderId,
        CancellationToken ct = default);

    Task<ApiResult<CustomerOrderDto>> MarkCollectedAsync(
        Guid organizationId,
        Guid orderId,
        CancellationToken ct = default);

    Task<ApiResult<CustomerOrderPagedResult>> ListMineAsync(
        string? partyType = null,
        Guid? buyerOrganizationId = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);

    Task<ApiResult<CustomerOrderDto>> GetMineAsync(
        Guid orderId,
        string? partyType = null,
        Guid? buyerOrganizationId = null,
        CancellationToken ct = default);

    Task<ApiResult<CustomerOrderDto>> PlaceAsCustomerAsync(
        Guid sellerOrganizationId,
        PlaceCustomerOrderRequest request,
        CancellationToken ct = default);

    Task<ApiResult<CustomerStorefrontDto>> GetStorefrontAsync(
        Guid sellerOrganizationId,
        string? search = null,
        Guid? categoryId = null,
        int page = 1,
        int pageSize = 40,
        CancellationToken ct = default);

    Task<ApiResult<ProductImageBytes>> GetStorefrontProductImageAsync(
        Guid sellerOrganizationId,
        Guid productId,
        string variant,
        CancellationToken ct = default);

    Task<ApiResult<QuoteCustomerOrderDeliveryDto>> QuoteDeliveryAsCustomerAsync(
        Guid sellerOrganizationId,
        QuoteCustomerOrderDeliveryRequest request,
        CancellationToken ct = default);
}
