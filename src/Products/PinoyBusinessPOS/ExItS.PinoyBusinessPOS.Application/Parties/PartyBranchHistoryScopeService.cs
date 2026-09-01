namespace ExItS.PinoyBusinessPOS.Application.Parties;

/// <summary>PRIVACY-04: branch-scoped transaction history visibility for party credit/ledger reads.</summary>
public sealed class PartyBranchHistoryScopeService
{
    private readonly PartyBranchAccessGovernanceAuthority _governance;
    private readonly IPartyBranchAccessActorAccessor _actorAccessor;

    public PartyBranchHistoryScopeService(
        PartyBranchAccessGovernanceAuthority governance,
        IPartyBranchAccessActorAccessor actorAccessor)
    {
        _governance = governance;
        _actorAccessor = actorAccessor;
    }

    /// <summary>
    /// Null = all branches (org governance). Otherwise only credits tied to sales at those branches are visible.
    /// Branch staff receive a single acting-branch id set.
    /// </summary>
    public IReadOnlySet<Guid>? GetPermittedHistoryBranchIds(Guid organizationId)
    {
        _ = organizationId;
        var actor = _actorAccessor.GetActor();
        if (_governance.CanBypassBranchFilter(actor))
        {
            return null;
        }

        if (actor.ActingBranchId is null || actor.ActingBranchId == Guid.Empty)
        {
            return new HashSet<Guid>();
        }

        return new HashSet<Guid> { actor.ActingBranchId.Value };
    }

    /// <summary>When branch-scoped, repayments/write-offs are hidden from ledger and summary surfaces.</summary>
    public bool ShouldHideOrgWideLedgerAdjustments() =>
        GetPermittedHistoryBranchIds(Guid.Empty) is not null;
}
