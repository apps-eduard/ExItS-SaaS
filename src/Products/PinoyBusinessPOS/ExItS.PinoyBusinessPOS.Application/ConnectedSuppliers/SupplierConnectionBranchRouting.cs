namespace ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;

/// <summary>
/// Branch routing for supplier-side connection requests. Relationships stay organization-anchored;
/// <c>SupplierBranchId</c> is the exact target operational location.
/// </summary>
public static class SupplierConnectionBranchRouting
{
    /// <summary>
    /// Branch workspace: exact <paramref name="relationshipSupplierBranchId"/> match only.
    /// Legacy null-branch relationships are never silently exposed to ordinary branch workspaces.
    /// Organization-wide inbox (Owner/Admin global): every pending request including legacy null.
    /// </summary>
    public static bool IsVisibleInSupplierInbox(
        Guid? relationshipSupplierBranchId,
        Guid? workspaceBranchId,
        bool organizationWideInbox)
    {
        if (organizationWideInbox)
        {
            return true;
        }

        if (workspaceBranchId is null)
        {
            return false;
        }

        return relationshipSupplierBranchId == workspaceBranchId;
    }

    /// <summary>
    /// Accept/Decline authority for the relationship's target supplier branch.
    /// Owner/Admin organization-wide access may manage any target; Area/explicit staff only when
    /// the central branch access resolver includes the target. Legacy null fails closed for non-global actors.
    /// </summary>
    public static bool CanRespondForSupplierBranch(
        Guid? relationshipSupplierBranchId,
        bool organizationWideAccess,
        IEnumerable<Guid> accessibleBranchIds)
    {
        if (organizationWideAccess)
        {
            return true;
        }

        if (relationshipSupplierBranchId is null || relationshipSupplierBranchId == Guid.Empty)
        {
            return false;
        }

        foreach (var branchId in accessibleBranchIds)
        {
            if (branchId == relationshipSupplierBranchId)
            {
                return true;
            }
        }

        return false;
    }
}
