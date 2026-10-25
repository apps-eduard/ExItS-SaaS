namespace ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;

/// <summary>One seller line adjustment: reduce fulfillable quantity or cancel the line (qty → 0).</summary>
public sealed record CustomerOrderLineAdjustment(
    Guid LineId,
    decimal NewQuantity,
    CustomerOrderLineAdjustmentReason Reason,
    string? Note = null);
