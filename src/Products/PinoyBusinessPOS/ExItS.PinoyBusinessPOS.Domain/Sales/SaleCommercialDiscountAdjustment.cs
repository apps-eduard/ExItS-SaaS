using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Domain.Sales;

/// <summary>
/// Immutable audit snapshot of one commercial discount that was applied at checkout: what the
/// operator asked for (<see cref="Method"/> + <see cref="RequestedValue"/> + <see cref="Reason"/>),
/// what the server computed (<see cref="CalculatedAmount"/>), and who applied it. One row per
/// requested intent, so a cart with two line discounts and one sale discount records three rows.
///
/// This is history, never a recalculation input: replaying a sale reads the stored line amounts.
/// </summary>
public sealed class SaleCommercialDiscountAdjustment
{
    public SaleCommercialDiscountAdjustmentId Id { get; }
    public SaleId SaleId { get; }
    public PosOrganizationId OrganizationId { get; }
    public SaleDiscountScope Scope { get; }
    public SaleDiscountMethod Method { get; }
    public SaleDiscountSource Source { get; }

    /// <summary>Percentage or peso amount exactly as requested, before the server computed pesos.</summary>
    public decimal RequestedValue { get; }

    /// <summary>Server-computed peso amount actually taken off the sale by this adjustment.</summary>
    public decimal CalculatedAmount { get; }

    public string Reason { get; }

    /// <summary>Line this adjustment attached to; null for sale-scoped adjustments.</summary>
    public SaleLineId? SaleLineId { get; }

    public Guid AppliedBy { get; }
    public DateTimeOffset RecordedAtUtc { get; }

    private SaleCommercialDiscountAdjustment(
        SaleCommercialDiscountAdjustmentId id,
        SaleId saleId,
        PosOrganizationId organizationId,
        SaleDiscountScope scope,
        SaleDiscountMethod method,
        SaleDiscountSource source,
        decimal requestedValue,
        decimal calculatedAmount,
        string reason,
        SaleLineId? saleLineId,
        Guid appliedBy,
        DateTimeOffset recordedAtUtc)
    {
        Id = id;
        SaleId = saleId;
        OrganizationId = organizationId;
        Scope = scope;
        Method = method;
        Source = source;
        RequestedValue = requestedValue;
        CalculatedAmount = calculatedAmount;
        Reason = reason;
        SaleLineId = saleLineId;
        AppliedBy = appliedBy;
        RecordedAtUtc = recordedAtUtc;
    }

    internal static SaleCommercialDiscountAdjustment Create(
        SaleId saleId,
        PosOrganizationId organizationId,
        SaleCommercialDiscountAdjustmentDraft draft,
        SaleLineId? saleLineId,
        Guid appliedBy,
        DateTimeOffset utcNow,
        SaleCommercialDiscountAdjustmentId? id = null)
    {
        ArgumentNullException.ThrowIfNull(draft);
        SaleMoney.EnsureUtc(utcNow);
        SaleMoney.EnsureActor(appliedBy);

        if (draft.Scope == SaleDiscountScope.Line && saleLineId is null)
        {
            throw new DomainException(
                DomainErrorCodes.SaleDiscountLineUnmatched,
                "A line-scoped commercial discount adjustment requires its sale line.");
        }

        return new SaleCommercialDiscountAdjustment(
            id ?? SaleCommercialDiscountAdjustmentId.New(),
            saleId,
            organizationId,
            draft.Scope,
            draft.Method,
            SaleDiscountSource.Manual,
            draft.RequestedValue,
            SaleMoney.RoundMoney(draft.CalculatedAmount),
            SaleCommercialDiscountRules.NormalizeReason(draft.Reason),
            draft.Scope == SaleDiscountScope.Line ? saleLineId : null,
            appliedBy,
            utcNow);
    }

    public static SaleCommercialDiscountAdjustment Rehydrate(
        SaleCommercialDiscountAdjustmentId id,
        SaleId saleId,
        PosOrganizationId organizationId,
        SaleDiscountScope scope,
        SaleDiscountMethod method,
        SaleDiscountSource source,
        decimal requestedValue,
        decimal calculatedAmount,
        string reason,
        SaleLineId? saleLineId,
        Guid appliedBy,
        DateTimeOffset recordedAtUtc) =>
        new(
            id,
            saleId,
            organizationId,
            scope,
            method,
            source,
            requestedValue,
            calculatedAmount,
            reason,
            saleLineId,
            appliedBy,
            recordedAtUtc);
}
