using ExItS.PinoyBusinessPOS.Application.CustomerOrdering;

namespace ExItS.PinoyBusinessPOS.IntegrationTests.Support;

/// <summary>Exposes Main/Mica branches for customer-order API proofs in Testing.</summary>
internal sealed class H1ProofCustomerOrderBranchDirectory : ICustomerOrderBranchDirectory
{
    private static readonly Guid Main = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid MicaA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid MicaB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private static readonly IReadOnlyList<Guid> Branches = [Main, MicaA, MicaB];

    public Task<CustomerOrderBranchSnapshot?> GetBranchAsync(
        Guid sellerOrganizationId,
        Guid branchId,
        CancellationToken cancellationToken = default)
    {
        if (branchId == Guid.Empty || !Branches.Contains(branchId))
        {
            return Task.FromResult<CustomerOrderBranchSnapshot?>(null);
        }

        return Task.FromResult<CustomerOrderBranchSnapshot?>(CreateSnapshot(branchId));
    }

    public Task<IReadOnlyList<CustomerOrderBranchSnapshot>> ListBranchesAsync(
        Guid sellerOrganizationId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CustomerOrderBranchSnapshot>>(
            Branches.Select(CreateSnapshot).ToList());

    private static CustomerOrderBranchSnapshot CreateSnapshot(Guid branchId) =>
        new(
            branchId,
            branchId == Main ? "Main" : branchId == MicaA ? "Mica A" : "Mica B",
            CustomerOrderingEnabled: true,
            PickupEnabled: true,
            DeliveryEnabled: true,
            CustomerOrderingOperational: true,
            PickupOperational: true,
            DeliveryOperational: true,
            OnlineOrdersPaused: false,
            StoreStatusMessage: "Open",
            Latitude: 14.5995m,
            Longitude: 120.9842m,
            DeliveryPolicy: null,
            IsPrimary: branchId == Main,
            DeliveryServiceAreas: null);
}
