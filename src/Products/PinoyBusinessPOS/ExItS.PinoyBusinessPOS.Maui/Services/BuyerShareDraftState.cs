namespace ExItS.PinoyBusinessPOS.Maui.Services;

/// <summary>
/// Temporary UI selection carried from post-Accept prompt into Manage Products.
/// Not persisted — Accept itself never writes shares.
/// </summary>
public sealed class BuyerShareDraftState
{
    private readonly object _gate = new();
    private Guid? _relationshipId;
    private HashSet<Guid>? _selectedProductIds;
    private bool _selectAllEligible;

    public void BeginAcceptDraft(Guid relationshipId, IEnumerable<Guid> selectedProductIds)
    {
        lock (_gate)
        {
            _relationshipId = relationshipId;
            _selectedProductIds = selectedProductIds.ToHashSet();
            _selectAllEligible = true;
        }
    }

    public bool TryTake(Guid relationshipId, out HashSet<Guid> selected, out bool selectAllEligible)
    {
        lock (_gate)
        {
            if (_relationshipId != relationshipId || _selectedProductIds is null)
            {
                selected = [];
                selectAllEligible = false;
                return false;
            }

            selected = _selectedProductIds;
            selectAllEligible = _selectAllEligible;
            _relationshipId = null;
            _selectedProductIds = null;
            _selectAllEligible = false;
            return true;
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            _relationshipId = null;
            _selectedProductIds = null;
            _selectAllEligible = false;
        }
    }
}
