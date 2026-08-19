using ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;

internal sealed class CustomerOrderRecord
{
    public Guid Id { get; set; }
    public Guid SellerOrganizationId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string FulfillmentStatus { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = nameof(CustomerOrderPaymentMethod.Cash);
    public string FulfillmentType { get; set; } = string.Empty;
    public Guid FulfillmentBranchId { get; set; }
    public string BranchNameSnapshot { get; set; } = string.Empty;

    public string CustomerPartyType { get; set; } = string.Empty;
    public string CustomerDisplayNameSnapshot { get; set; } = string.Empty;
    public Guid? CustomerPlatformUserId { get; set; }
    public Guid? CustomerBuyerOrganizationId { get; set; }
    public string? CustomerBuyerPublicOrganizationId { get; set; }

    public decimal MerchandiseSubtotal { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal Total { get; set; }

    public string? DeliveryRecipientName { get; set; }
    public string? DeliveryRecipientPhone { get; set; }
    public string? DeliveryAddressLine1 { get; set; }
    public string? DeliveryAddressLine2 { get; set; }
    public string? DeliveryCity { get; set; }
    public string? DeliveryNotes { get; set; }
    public decimal? DeliveryDestinationLatitude { get; set; }
    public decimal? DeliveryDestinationLongitude { get; set; }
    public decimal? DeliveryBranchLatitudeSnapshot { get; set; }
    public decimal? DeliveryBranchLongitudeSnapshot { get; set; }
    public decimal? DeliveryDistanceKm { get; set; }
    public decimal? DeliveryMinimumOrderAmountSnapshot { get; set; }
    public decimal? DeliveryBaseFeeSnapshot { get; set; }
    public decimal? DeliveryIncludedDistanceKmSnapshot { get; set; }
    public decimal? DeliveryAdditionalFeePerKmSnapshot { get; set; }
    public decimal? DeliveryMaximumDistanceKmSnapshot { get; set; }
    public decimal? DeliveryFreeThresholdSnapshot { get; set; }
    public decimal? DeliveryDistanceCharge { get; set; }
    public decimal? DeliveryFinalFee { get; set; }
    public bool? DeliveryFreeApplied { get; set; }

    public string StockReservationState { get; set; } = string.Empty;
    public string? RejectReason { get; set; }
    public string? RejectNotes { get; set; }
    public string? IdempotencyKey { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? SubmittedAtUtc { get; set; }
    public Guid? SubmittedBy { get; set; }
    public DateTimeOffset? AcceptedAtUtc { get; set; }
    public Guid? AcceptedBy { get; set; }
    public DateTimeOffset? RejectedAtUtc { get; set; }
    public Guid? RejectedBy { get; set; }
    public DateTimeOffset? CancelledAtUtc { get; set; }
    public Guid? CancelledBy { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public Guid? CompletedBy { get; set; }
    public DateTimeOffset? ReadyAtUtc { get; set; }
    public Guid? ReadyBy { get; set; }
    public DateTimeOffset? OutForDeliveryAtUtc { get; set; }
    public Guid? OutForDeliveryBy { get; set; }
    public DateTimeOffset? DeliveredAtUtc { get; set; }
    public Guid? DeliveredBy { get; set; }
    public DateTimeOffset? CollectedAtUtc { get; set; }
    public Guid? CollectedBy { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public uint Xmin { get; set; }
}
