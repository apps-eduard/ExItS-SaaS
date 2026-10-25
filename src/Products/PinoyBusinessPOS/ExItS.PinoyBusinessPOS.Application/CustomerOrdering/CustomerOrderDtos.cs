using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Domain.Sales;

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
    decimal LineTotal,
    decimal OriginalQuantity = 0m,
    decimal CancelledQuantity = 0m,
    decimal OriginalLineTotal = 0m,
    bool IsCancelled = false,
    string? AdjustmentReason = null,
    string? AdjustmentNote = null);

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
    bool FreeDeliveryApplied,
    bool DistanceExceptionApplied = false);

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
    DateTimeOffset UpdatedAtUtc,
    string? PaymentReference = null,
    string? RequestedPickupLocal = null,
    string? RequestedPickupTimeZoneId = null,
    DateTimeOffset? RequestedPickupAtUtc = null,
    string? RequestedDeliveryLocal = null,
    string? RequestedDeliveryTimeZoneId = null,
    DateTimeOffset? RequestedDeliveryAtUtc = null,
    string? PreferredFulfillmentDate = null,
    string? PreferredFulfillmentPeriod = null,
    string? PreferredFulfillmentTimeZoneId = null,
    DateTimeOffset? PreferredFulfillmentWindowStartUtc = null,
    DateTimeOffset? PreferredFulfillmentWindowEndUtc = null,
    decimal? AmountReceived = null,
    decimal? ChangeAmount = null,
    DateTimeOffset? PaymentConfirmedAtUtc = null,
    Guid? PaymentConfirmedBy = null,
    string? ConfirmedFulfillmentLocal = null,
    string? ConfirmedFulfillmentTimeZoneId = null,
    DateTimeOffset? ConfirmedFulfillmentAtUtc = null,
    string? EffectiveFulfillmentLocal = null,
    string? EffectiveFulfillmentTimeZoneId = null,
    DateTimeOffset? EffectiveFulfillmentAtUtc = null,
    CustomerOrderScheduleProposalDto? PendingScheduleProposal = null,
    decimal OriginalMerchandiseSubtotal = 0m,
    decimal OriginalTotal = 0m,
    string? CancelReason = null,
    string? CancelNotes = null,
    IReadOnlyList<CustomerOrderActivityEventDto>? Activity = null,
    bool HasLineAdjustments = false);

public sealed record CustomerOrderScheduleProposalDto(
    Guid ProposalId,
    string Local,
    string TimeZoneId,
    DateTimeOffset AtUtc,
    string? Note,
    Guid ProposedBy,
    DateTimeOffset ProposedAtUtc);

public sealed record ProposeCustomerOrderScheduleRequest(
    string Date,
    string Time,
    string? Note = null,
    Guid? ClientProposalId = null);

public sealed record RespondCustomerOrderScheduleProposalRequest(
    Guid ProposalId);

public sealed record ConfirmCustomerOrderPaymentRequest(
    decimal? AmountReceived = null,
    string? PaymentReference = null);

public sealed record AdjustCustomerOrderLinesRequest(
    IReadOnlyList<AdjustCustomerOrderLineRequest> Lines);

public sealed record AdjustCustomerOrderLineRequest(
    Guid LineId,
    decimal NewQuantity,
    string Reason,
    string? Note = null);

public sealed record CancelCustomerOrderBySellerRequest(
    string Reason,
    string? Notes = null);

public sealed record CustomerOrderActivityEventDto(
    Guid EventId,
    DateTimeOffset AtUtc,
    Guid? ActorId,
    string Action,
    Guid? LineId = null,
    string? ProductName = null,
    decimal? PreviousQuantity = null,
    decimal? NewQuantity = null,
    string? ReasonCode = null,
    string? Note = null,
    decimal? PreviousTotal = null,
    decimal? NewTotal = null,
    string? Summary = null);

public sealed record CustomerOrderListItemDto(
    Guid OrderId,
    string OrderNumber,
    string Status,
    string FulfillmentStatus,
    string PaymentStatus,
    string PaymentMethod,
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
    string? PaymentMethod = null,
    string? PaymentReference = null,
    string? RequestedPickupDate = null,
    string? RequestedPickupTime = null,
    string? RequestedDeliveryDate = null,
    string? RequestedDeliveryTime = null,
    string? PreferredFulfillmentDate = null,
    string? PreferredFulfillmentPeriod = null);

public sealed record QuoteCustomerOrderDeliveryRequest(
    Guid FulfillmentBranchId,
    decimal MerchandiseSubtotal,
    decimal DestinationLatitude,
    decimal DestinationLongitude,
    Guid? DeliveryServiceAreaId = null,
    Guid? PlatformBusinessCustomerId = null);

public sealed record QuoteCustomerOrderDeliveryDto(
    bool Available,
    string? UnavailableReason,
    decimal DistanceKm,
    decimal ExtraDistanceKm,
    decimal DistanceCharge,
    decimal DeliveryFee,
    bool FreeDeliveryApplied,
    decimal MinimumOrderAmount,
    decimal MaximumDeliveryDistanceKm,
    bool DistanceExceptionApplied = false);

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
                    order.DeliverySnapshot.FreeDeliveryApplied,
                    order.DeliverySnapshot.DistanceExceptionApplied),
            order.Lines.Select(l =>
            {
                var originalGross = SaleMoney.RoundMoney(l.UnitPrice * l.OriginalQuantity);
                var originalLineTotal = SaleMoney.RoundMoney(originalGross - l.OriginalDiscount);
                return new CustomerOrderLineDto(
                    l.Id.Value,
                    l.ProductId.Value,
                    l.LineNumber,
                    l.NameSnapshot,
                    l.SkuSnapshot,
                    UnitOfMeasures.ToCode(l.UnitSnapshot),
                    l.Quantity,
                    l.UnitPrice,
                    l.Discount,
                    l.LineTotal,
                    l.OriginalQuantity,
                    l.CancelledQuantity,
                    originalLineTotal,
                    l.IsCancelled,
                    l.AdjustmentReason?.ToString(),
                    l.AdjustmentNote);
            }).ToList(),
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
            order.UpdatedAtUtc,
            order.PaymentReference,
            order.RequestedPickupLocal,
            order.RequestedPickupTimeZoneId,
            order.RequestedPickupAtUtc,
            order.RequestedDeliveryLocal,
            order.RequestedDeliveryTimeZoneId,
            order.RequestedDeliveryAtUtc,
            order.PreferredFulfillmentDate?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            order.PreferredFulfillmentPeriod?.ToString(),
            order.PreferredFulfillmentTimeZoneId,
            order.PreferredFulfillmentWindowStartUtc,
            order.PreferredFulfillmentWindowEndUtc,
            order.AmountReceived,
            order.ChangeAmount,
            order.PaymentConfirmedAtUtc,
            order.PaymentConfirmedBy,
            order.ConfirmedFulfillmentLocal,
            order.ConfirmedFulfillmentTimeZoneId,
            order.ConfirmedFulfillmentAtUtc,
            order.EffectiveFulfillmentLocal,
            order.EffectiveFulfillmentTimeZoneId,
            order.EffectiveFulfillmentAtUtc,
            order.PendingScheduleProposal is null
                ? null
                : new CustomerOrderScheduleProposalDto(
                    order.PendingScheduleProposal.ProposalId,
                    order.PendingScheduleProposal.Local,
                    order.PendingScheduleProposal.TimeZoneId,
                    order.PendingScheduleProposal.AtUtc,
                    order.PendingScheduleProposal.Note,
                    order.PendingScheduleProposal.ProposedBy,
                    order.PendingScheduleProposal.ProposedAtUtc),
            order.OriginalMerchandiseSubtotal,
            order.OriginalTotal,
            order.CancelReason?.ToString(),
            order.CancelNotes,
            order.ActivityEvents.Select(e => new CustomerOrderActivityEventDto(
                e.EventId,
                e.AtUtc,
                e.ActorId,
                e.Action,
                e.LineId,
                e.ProductName,
                e.PreviousQuantity,
                e.NewQuantity,
                e.ReasonCode,
                e.Note,
                e.PreviousTotal,
                e.NewTotal,
                e.Summary)).ToList(),
            order.Lines.Any(l => l.Quantity < l.OriginalQuantity));

    public static CustomerOrderListItemDto MapListItem(CustomerOrder order) =>
        new(
            order.Id.Value,
            order.OrderNumber,
            order.Status.ToString(),
            order.FulfillmentStatus.ToString(),
            order.PaymentStatus.ToString(),
            CustomerOrderPaymentMethods.ToCode(order.PaymentMethod),
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
