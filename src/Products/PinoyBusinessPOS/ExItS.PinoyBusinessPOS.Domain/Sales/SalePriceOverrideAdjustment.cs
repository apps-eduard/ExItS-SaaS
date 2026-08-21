using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Domain.Sales;

/// <summary>
/// Immutable audit snapshot of one per-sale unit-price override applied at checkout: the resolved
/// catalog/unit baseline, the applied unit price, the operator reason, and who applied it.
/// Catalog <c>SellingPrice</c> / Today's Price is never rewritten by this row.
///
/// This is history, never a recalculation input: replaying a completed sale reads stored line amounts.
/// Legacy sales without override rows simply have none.
/// </summary>
public sealed class SalePriceOverrideAdjustment
{
    public SalePriceOverrideAdjustmentId Id { get; }
    public SaleId SaleId { get; }
    public PosOrganizationId OrganizationId { get; }
    public SaleLineId SaleLineId { get; }
    public decimal BaselineUnitPrice { get; }
    public decimal AppliedUnitPrice { get; }
    public string Reason { get; }
    public Guid AppliedBy { get; }
    public DateTimeOffset RecordedAtUtc { get; }

    private SalePriceOverrideAdjustment(
        SalePriceOverrideAdjustmentId id,
        SaleId saleId,
        PosOrganizationId organizationId,
        SaleLineId saleLineId,
        decimal baselineUnitPrice,
        decimal appliedUnitPrice,
        string reason,
        Guid appliedBy,
        DateTimeOffset recordedAtUtc)
    {
        Id = id;
        SaleId = saleId;
        OrganizationId = organizationId;
        SaleLineId = saleLineId;
        BaselineUnitPrice = baselineUnitPrice;
        AppliedUnitPrice = appliedUnitPrice;
        Reason = reason;
        AppliedBy = appliedBy;
        RecordedAtUtc = recordedAtUtc;
    }

    internal static SalePriceOverrideAdjustment Create(
        SaleId saleId,
        PosOrganizationId organizationId,
        SalePriceOverrideAdjustmentDraft draft,
        SaleLineId saleLineId,
        Guid appliedBy,
        DateTimeOffset utcNow,
        SalePriceOverrideAdjustmentId? id = null)
    {
        ArgumentNullException.ThrowIfNull(draft);
        SaleMoney.EnsureUtc(utcNow);
        SaleMoney.EnsureActor(appliedBy);

        return new SalePriceOverrideAdjustment(
            id ?? SalePriceOverrideAdjustmentId.New(),
            saleId,
            organizationId,
            saleLineId,
            SaleMoney.RoundMoney(draft.BaselineUnitPrice),
            SaleMoney.RoundMoney(draft.AppliedUnitPrice),
            SalePriceOverrideRules.NormalizeReason(draft.Reason),
            appliedBy,
            utcNow);
    }

    public static SalePriceOverrideAdjustment Rehydrate(
        SalePriceOverrideAdjustmentId id,
        SaleId saleId,
        PosOrganizationId organizationId,
        SaleLineId saleLineId,
        decimal baselineUnitPrice,
        decimal appliedUnitPrice,
        string reason,
        Guid appliedBy,
        DateTimeOffset recordedAtUtc) =>
        new(
            id,
            saleId,
            organizationId,
            saleLineId,
            baselineUnitPrice,
            appliedUnitPrice,
            reason,
            appliedBy,
            recordedAtUtc);
}
