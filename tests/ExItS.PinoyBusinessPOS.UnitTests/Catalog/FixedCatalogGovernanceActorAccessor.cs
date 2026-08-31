using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Permissions;

namespace ExItS.PinoyBusinessPOS.UnitTests.Catalog;

/// <summary>Test actor accessor for mandatory catalog governance dependencies.</summary>
internal sealed class FixedCatalogGovernanceActorAccessor : ICatalogGovernanceActorAccessor
{
    public FixedCatalogGovernanceActorAccessor(CatalogGovernanceActor actor) => Actor = actor;

    public CatalogGovernanceActor Actor { get; set; }

    public CatalogGovernanceActor GetActor() => Actor;

    public static FixedCatalogGovernanceActorAccessor Owner(Guid? actingBranchId = null) =>
        new(new CatalogGovernanceActor(PosRole.Owner, false, actingBranchId));

    public static FixedCatalogGovernanceActorAccessor StoreManager(Guid actingBranchId) =>
        new(new CatalogGovernanceActor(PosRole.StoreManager, false, actingBranchId));
}
