using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;

namespace ExItS.PinoyBusinessPOS.Application.CustomerOrdering;

public sealed record CustomerOrderLineDto(
    Guid LineId,
    Guid ProductId,
    int LineNumber,
    string NameSnapshot,
    string? SkuSnapshot,
    string UnitSnapshot,
    decimal Quantity,
    decimal UnitPrice,
    decimal Discount,
    decimal LineTotal);

public sealed record CustomerOrderDeliveryDto(
    string RecipientName,
    string? RecipientPhone,
    string AddressLine1,
    string? AddressLine2,
    string? City,
    string? DeliveryNotes,
    decimal DestinationLatitude,
    decimal DestinationLongitude,
    decimal BranchLatitudeSnapshot,
    decimal BranchLongitudeSnapshot,
    decimal DistanceKm,
    decimal MinimumOrderAmountSnapshot,
    decimal BaseDeliveryFeeSnapshot,
    decimal IncludedDistanceKmSnapshot,
    decimal AdditionalFeePerKmSnapshot,
    decimal MaximumDeliveryDistanceKmSnapshot,
    decimal? FreeDeliveryThresholdSnapshot,
    decimal DistanceCharge,
    decimal FinalDeliveryFee,
    bool FreeDeliveryApplied);

public sealed record CustomerOrderDto(
    Guid OrderId,
    Guid SellerOrganizationId,
    string OrderNumber,
    string Status,
    string FulfillmentStatus,
    string PaymentStatus,
    string PaymentMethod,
    string FulfillmentType,
    Guid FulfillmentBranchId,
    string BranchNameSnapshot,
    string CustomerPartyType,
    string CustomerDisplayName,
    Guid? CustomerPlatformUserId,
    Guid? PlatformBusinessCustomerId,
    Guid? CustomerBuyerOrganizationId,
    string? CustomerBuyerPublicOrganizationId,
    decimal MerchandiseSubtotal,
    decimal DeliveryFee,
    decimal Total,
    string StockReservationState,
    string? RejectReason,
    string? RejectNotes,
    CustomerOrderDeliveryDto? Delivery,
    IReadOnlyList<CustomerOrderLineDto> Lines,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? SubmittedAtUtc,
    DateTimeOffset? AcceptedAtUtc,
    Guid? AcceptedBy,
    DateTimeOffset? RejectedAtUtc,
    Guid? RejectedBy,
    DateTimeOffset? CancelledAtUtc,
    Guid? CancelledBy,
    DateTimeOffset? ReadyAtUtc,
    Guid? ReadyBy,
    DateTimeOffset? OutForDeliveryAtUtc,
    Guid? OutForDeliveryBy,
    DateTimeOffset? DeliveredAtUtc,
    Guid? DeliveredBy,
    DateTimeOffset? CollectedAtUtc,
    Guid? CollectedBy,
    DateTimeOffset? CompletedAtUtc,
    Guid? CompletedBy,
    DateTimeOffset UpdatedAtUtc);

public sealed record CustomerOrderListItemDto(
    Guid OrderId,
    string OrderNumber,
    string Status,
    string FulfillmentStatus,
    string FulfillmentType,
    Guid FulfillmentBranchId,
    string BranchNameSnapshot,
    string CustomerDisplayName,
    decimal Total,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    int LineCount,
    Guid SellerOrganizationId);

public sealed record CustomerOrderPagedResult(
    IReadOnlyList<CustomerOrderListItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record PlaceCustomerOrderLineRequest(
    Guid ProductId,
    decimal Quantity,
    decimal Discount = 0m);

public sealed record PlaceCustomerOrderDeliveryRequest(
    string RecipientName,
    string? RecipientPhone,
    string AddressLine1,
    string? AddressLine2,
    string? City,
    string? DeliveryNotes,
    decimal DestinationLatitude,
    decimal DestinationLongitude,
    Guid? DeliveryServiceAreaId = null);

public sealed record PlaceCustomerOrderRequest(
    string FulfillmentType,
    Guid FulfillmentBranchId,
    string CustomerPartyType,
    string CustomerDisplayName,
    Guid? CustomerPlatformUserId,
    Guid? PlatformBusinessCustomerId,
    Guid? CustomerBuyerOrganizationId,
    string? CustomerBuyerPublicOrganizationId,
    IReadOnlyList<PlaceCustomerOrderLineRequest> Lines,
    PlaceCustomerOrderDeliveryRequest? Delivery = null,
    Guid? ClientOrderId = null,
    string? IdempotencyKey = null,
    string? PaymentMethod = null);

public sealed record QuoteCustomerOrderDeliveryRequest(
    Guid FulfillmentBranchId,
    decimal MerchandiseSubtotal,
    decimal DestinationLatitude,
    decimal DestinationLongitude,
    Guid? DeliveryServiceAreaId = null);

public sealed record QuoteCustomerOrderDeliveryDto(
    bool Available,
    string? UnavailableReason,
    decimal DistanceKm,
    decimal ExtraDistanceKm,
    decimal DistanceCharge,
    decimal DeliveryFee,
    bool FreeDeliveryApplied,
    decimal MinimumOrderAmount,
    decimal MaximumDeliveryDistanceKm);

public sealed record RejectCustomerOrderRequest(
    string Reason,
    string? Notes = null);

public static class CustomerOrderMaps
{
    public static CustomerOrderDto Map(CustomerOrder order) =>
        new(
            order.Id.Value,
            order.SellerOrganizationId.Value,
            order.OrderNumber,
            order.Status.ToString(),
            order.FulfillmentStatus.ToString(),
            order.PaymentStatus.ToString(),
            CustomerOrderPaymentMethods.ToCode(order.PaymentMethod),
            order.FulfillmentType.ToString(),
            order.FulfillmentBranchId,
            order.BranchNameSnapshot,
            order.CustomerParty.PartyType.ToString(),
            order.CustomerParty.DisplayNameSnapshot,
            order.CustomerParty.PlatformUserId,
            order.PlatformBusinessCustomerId,
            order.CustomerParty.BuyerOrganizationId,
            order.CustomerParty.BuyerPublicOrganizationId,
            order.MerchandiseSubtotal,
            order.DeliveryFee,
            order.Total,
            order.StockReservationState.ToString(),
            order.RejectReason?.ToString(),
            order.RejectNotes,
            order.DeliverySnapshot is null
                ? null
                : new CustomerOrderDeliveryDto(
                    order.DeliverySnapshot.RecipientName,
                    order.DeliverySnapshot.RecipientPhone,
                    order.DeliverySnapshot.AddressLine1,
                    order.DeliverySnapshot.AddressLine2,
                    order.DeliverySnapshot.City,
                    order.DeliverySnapshot.DeliveryNotes,
                    order.DeliverySnapshot.DestinationLatitude,
                    order.DeliverySnapshot.DestinationLongitude,
                    order.DeliverySnapshot.BranchLatitudeSnapshot,
                    order.DeliverySnapshot.BranchLongitudeSnapshot,
                    order.DeliverySnapshot.DistanceKm,
                    order.DeliverySnapshot.MinimumOrderAmountSnapshot,
                    order.DeliverySnapshot.BaseDeliveryFeeSnapshot,
                    order.DeliverySnapshot.IncludedDistanceKmSnapshot,
                    order.DeliverySnapshot.AdditionalFeePerKmSnapshot,
                    order.DeliverySnapshot.MaximumDeliveryDistanceKmSnapshot,
                    order.DeliverySnapshot.FreeDeliveryThresholdSnapshot,
                    order.DeliverySnapshot.DistanceCharge,
                    order.DeliverySnapshot.FinalDeliveryFee,
                    order.DeliverySnapshot.FreeDeliveryApplied),
            order.Lines.Select(l => new CustomerOrderLineDto(
                l.Id.Value,
                l.ProductId.Value,
                l.LineNumber,
                l.NameSnapshot,
                l.SkuSnapshot,
                UnitOfMeasures.ToCode(l.UnitSnapshot),
                l.Quantity,
                l.UnitPrice,
                l.Discount,
                l.LineTotal)).ToList(),
            order.CreatedAtUtc,
            order.SubmittedAtUtc,
            order.AcceptedAtUtc,
            order.AcceptedBy,
            order.RejectedAtUtc,
            order.RejectedBy,
            order.CancelledAtUtc,
            order.CancelledBy,
            order.ReadyAtUtc,
            order.ReadyBy,
            order.OutForDeliveryAtUtc,
            order.OutForDeliveryBy,
            order.DeliveredAtUtc,
            order.DeliveredBy,
            order.CollectedAtUtc,
            order.CollectedBy,
            order.CompletedAtUtc,
            order.CompletedBy,
            order.UpdatedAtUtc);

    public static CustomerOrderListItemDto MapListItem(CustomerOrder order) =>
        new(
            order.Id.Value,
            order.OrderNumber,
            order.Status.ToString(),
            order.FulfillmentStatus.ToString(),
            order.FulfillmentType.ToString(),
            order.FulfillmentBranchId,
            order.BranchNameSnapshot,
            order.CustomerParty.DisplayNameSnapshot,
            order.Total,
            order.CreatedAtUtc,
            order.UpdatedAtUtc,
            order.Lines.Count,
            order.SellerOrganizationId.Value);
}
