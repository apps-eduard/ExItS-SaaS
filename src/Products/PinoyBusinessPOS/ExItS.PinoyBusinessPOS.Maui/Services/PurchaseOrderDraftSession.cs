namespace ExItS.PinoyBusinessPOS.Maui.Services;

/// <summary>
/// In-memory PO create draft held while navigating to connected-catalog setup and back.
/// Not persisted across app restarts.
/// </summary>
public sealed class PurchaseOrderDraftSession
{
    private readonly object _gate = new();
    private Draft? _draft;

    public sealed record LineDraft(
        Guid ProductId,
        string Name,
        decimal OrderedQty,
        decimal UnitPurchaseCost,
        Guid? SupplierProductId);

    public sealed record Draft(
        string? SupplierId,
        DateOnly? OrderDate,
        DateOnly? ExpectedDeliveryDate,
        string? SupplierReference,
        string? Notes,
        IReadOnlyList<LineDraft> Lines,
        string ProductSearch,
        Guid? CategoryFilterId,
        string DraftQtyText,
        decimal? DraftUnitCost,
        string SelectedProductId);

    public void Save(Draft draft)
    {
        lock (_gate)
        {
            _draft = draft;
        }
    }

    public bool TryTake(out Draft draft)
    {
        lock (_gate)
        {
            if (_draft is null)
            {
                draft = null!;
                return false;
            }

            draft = _draft;
            _draft = null;
            return true;
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            _draft = null;
        }
    }
}
