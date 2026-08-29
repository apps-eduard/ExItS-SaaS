namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Sales;

internal sealed class SaleRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string SaleNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal Total { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal GrossSubtotal { get; set; }
    public decimal LineDiscountTotal { get; set; }
    public decimal SaleDiscountTotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal? AmountTendered { get; set; }
    public decimal? ChangeAmount { get; set; }
    public string? GcashReference { get; set; }
    public Guid? CustomerId { get; set; }
    public string BuyerPartyKind { get; set; } = "WalkIn";
    public string? BuyerDisplayNameSnapshot { get; set; }
    public string? BuyerPersonalPublicUserId { get; set; }
    public Guid? BuyerOrganizationId { get; set; }
    public string? BuyerPublicOrganizationId { get; set; }
    public Guid? LinkedCreditEntryId { get; set; }
    public Guid? CashierShiftId { get; set; }
    public Guid? RegisterId { get; set; }
    public Guid? BranchId { get; set; }
    public string StockReservationState { get; set; } = "None";
    public DateTimeOffset RecordedAtUtc { get; set; }
    public Guid RecordedBy { get; set; }
    public DateTimeOffset? VoidedAtUtc { get; set; }
    public Guid? VoidedBy { get; set; }
    public string? VoidReason { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public string? CostStatus { get; set; }
    public decimal? TotalCostSnapshot { get; set; }
    public uint Xmin { get; set; }
}
