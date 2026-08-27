namespace ExItS.PinoyBuyNowPayLater.Application.Access;

/// <summary>
/// Branch authorization scope carried as trusted facts (opaque Platform org-branch Guids).
/// Organization-wide access must be explicitly asserted by the trusted transport — never inferred from role names.
/// </summary>
public sealed class BnplBranchScope
{
    private BnplBranchScope(bool isOrganizationWide, IReadOnlySet<Guid> allowedBranchIds)
    {
        IsOrganizationWide = isOrganizationWide;
        AllowedBranchIds = allowedBranchIds;
    }

    public bool IsOrganizationWide { get; }

    public IReadOnlySet<Guid> AllowedBranchIds { get; }

    public static BnplBranchScope None { get; } =
        new(false, new HashSet<Guid>());

    public static BnplBranchScope OrganizationWide() =>
        new(true, new HashSet<Guid>());

    public static BnplBranchScope Restricted(IEnumerable<Guid> allowedBranchIds)
    {
        ArgumentNullException.ThrowIfNull(allowedBranchIds);
        var set = new HashSet<Guid>(allowedBranchIds.Where(id => id != Guid.Empty));
        return new BnplBranchScope(false, set);
    }

    public bool Allows(Guid branchId)
    {
        if (branchId == Guid.Empty)
        {
            return false;
        }

        return IsOrganizationWide || AllowedBranchIds.Contains(branchId);
    }
}
