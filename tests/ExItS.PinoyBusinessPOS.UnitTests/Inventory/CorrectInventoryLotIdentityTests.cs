using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.UnitTests.Inventory;

public sealed class CorrectInventoryLotIdentityTests
{
    private static readonly Guid OrgGuid = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid BranchGuid = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid OtherBranchGuid = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid Actor = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly PosOrganizationId Org = PosOrganizationId.From(OrgGuid);
    private static readonly PosBranchId Branch = PosBranchId.From(BranchGuid);
    private static readonly DateTimeOffset Utc = new(2026, 9, 27, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Pristine_lot_can_update_identity_without_changing_quantity()
    {
        var product = CatalogProduct.Create(Org, "Apple", UnitOfMeasure.Kilogram, 50m, Utc);
        var lot = InventoryLot.Create(Org, product.Id, new DateOnly(2026, 12, 31), 50m, Utc, Branch);
        var lots = new InMemoryLots([lot]);
        var corrections = new CapturingCorrections();
        var useCase = CreateUseCase(lots, new EditableSupport(), corrections, product);

        var result = await useCase.ExecuteAsync(
            OrgGuid,
            BranchGuid,
            product.Id.Value,
            lot.Id.Value,
            new CorrectInventoryLotIdentityRequest(new DateOnly(2027, 1, 15), "LOT-A", "Corrected supplier label", lot.UpdatedAtUtc),
            Actor,
            7);

        Assert.True(result.IsSuccess);
        Assert.Equal(50m, lot.QuantityOnHand);
        Assert.Equal(new DateOnly(2027, 1, 15), lot.ExpirationDate);
        Assert.Equal("LOT-A", lot.LotNumber);
        Assert.Single(corrections.Items);
        Assert.True(result.Value!.CanEditIdentity);
    }

    [Fact]
    public async Task Used_lot_is_locked_including_blank_lot_number()
    {
        var product = CatalogProduct.Create(Org, "Apple", UnitOfMeasure.Kilogram, 50m, Utc);
        var lot = InventoryLot.Create(Org, product.Id, new DateOnly(2026, 12, 31), 35m, Utc, Branch);
        var support = new EditableSupport
        {
            Movements =
            {
                [lot.Id.Value] = [StockMovementType.PurchaseReceipt, StockMovementType.SaleDeduction]
            }
        };
        var useCase = CreateUseCase(new InMemoryLots([lot]), support, new CapturingCorrections(), product);

        var result = await useCase.ExecuteAsync(
            OrgGuid,
            BranchGuid,
            product.Id.Value,
            lot.Id.Value,
            new CorrectInventoryLotIdentityRequest(new DateOnly(2027, 1, 15), "LOT-X", "Should fail", lot.UpdatedAtUtc),
            Actor,
            7);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.InventoryLotIdentityLocked, result.ErrorCode);
        Assert.Equal("Used", result.ErrorDetails!["identityLockReason"]);
        Assert.Null(lot.LotNumber);
        Assert.Equal(new DateOnly(2026, 12, 31), lot.ExpirationDate);
    }

    [Fact]
    public async Task Other_branch_lot_is_not_found()
    {
        var product = CatalogProduct.Create(Org, "Apple", UnitOfMeasure.Kilogram, 50m, Utc);
        var lot = InventoryLot.Create(
            Org,
            product.Id,
            new DateOnly(2026, 12, 31),
            50m,
            Utc,
            PosBranchId.From(OtherBranchGuid));
        var useCase = CreateUseCase(new InMemoryLots([lot]), new EditableSupport(), new CapturingCorrections(), product);

        var result = await useCase.ExecuteAsync(
            OrgGuid,
            BranchGuid,
            product.Id.Value,
            lot.Id.Value,
            new CorrectInventoryLotIdentityRequest(new DateOnly(2027, 1, 15), "LOT-A", "Nope", lot.UpdatedAtUtc),
            Actor,
            7);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.InventoryLotNotFound, result.ErrorCode);
    }

    [Fact]
    public async Task Identity_collision_is_rejected()
    {
        var product = CatalogProduct.Create(Org, "Apple", UnitOfMeasure.Kilogram, 50m, Utc);
        var lot1 = InventoryLot.Create(Org, product.Id, new DateOnly(2026, 12, 31), 20m, Utc, Branch, "LOT-A");
        var lot2 = InventoryLot.Create(Org, product.Id, new DateOnly(2027, 1, 31), 30m, Utc, Branch, "LOT-B");
        var useCase = CreateUseCase(new InMemoryLots([lot1, lot2]), new EditableSupport(), new CapturingCorrections(), product);

        var result = await useCase.ExecuteAsync(
            OrgGuid,
            BranchGuid,
            product.Id.Value,
            lot2.Id.Value,
            new CorrectInventoryLotIdentityRequest(new DateOnly(2026, 12, 31), "LOT-A", "Merge attempt", lot2.UpdatedAtUtc),
            Actor,
            7);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.InventoryLotIdentityConflict, result.ErrorCode);
        Assert.Equal(30m, lot2.QuantityOnHand);
        Assert.Equal("LOT-B", lot2.LotNumber);
    }

    [Fact]
    public async Task Stale_expected_updated_at_is_rejected()
    {
        var product = CatalogProduct.Create(Org, "Apple", UnitOfMeasure.Kilogram, 50m, Utc);
        var lot = InventoryLot.Create(Org, product.Id, new DateOnly(2026, 12, 31), 50m, Utc, Branch);
        var useCase = CreateUseCase(new InMemoryLots([lot]), new EditableSupport(), new CapturingCorrections(), product);

        var result = await useCase.ExecuteAsync(
            OrgGuid,
            BranchGuid,
            product.Id.Value,
            lot.Id.Value,
            new CorrectInventoryLotIdentityRequest(
                new DateOnly(2027, 1, 15),
                "LOT-A",
                "Stale",
                Utc.AddHours(-1)),
            Actor,
            7);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.InventoryLotChanged, result.ErrorCode);
    }

    [Fact]
    public async Task Active_transfer_draft_locks()
    {
        var product = CatalogProduct.Create(Org, "Apple", UnitOfMeasure.Kilogram, 50m, Utc);
        var lot = InventoryLot.Create(Org, product.Id, new DateOnly(2026, 12, 31), 50m, Utc, Branch);
        var support = new EditableSupport { DraftLotIds = { lot.Id.Value } };
        var useCase = CreateUseCase(new InMemoryLots([lot]), support, new CapturingCorrections(), product);

        var result = await useCase.ExecuteAsync(
            OrgGuid,
            BranchGuid,
            product.Id.Value,
            lot.Id.Value,
            new CorrectInventoryLotIdentityRequest(new DateOnly(2027, 1, 15), "LOT-A", "Draft", lot.UpdatedAtUtc),
            Actor,
            7);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.InventoryLotIdentityLocked, result.ErrorCode);
        Assert.Equal("ActiveTransferDraft", result.ErrorDetails!["identityLockReason"]);
    }

    [Fact]
    public async Task Transfer_in_lot_is_locked()
    {
        var product = CatalogProduct.Create(Org, "Apple", UnitOfMeasure.Kilogram, 50m, Utc);
        var lot = InventoryLot.Create(Org, product.Id, new DateOnly(2026, 12, 31), 10m, Utc, Branch, "LOT-A");
        var support = new EditableSupport
        {
            Movements = { [lot.Id.Value] = [StockMovementType.TransferIn] }
        };
        var useCase = CreateUseCase(new InMemoryLots([lot]), support, new CapturingCorrections(), product);

        var result = await useCase.ExecuteAsync(
            OrgGuid,
            BranchGuid,
            product.Id.Value,
            lot.Id.Value,
            new CorrectInventoryLotIdentityRequest(new DateOnly(2027, 1, 15), "LOT-B", "Trace break", lot.UpdatedAtUtc),
            Actor,
            7);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.InventoryLotIdentityLocked, result.ErrorCode);
        Assert.Equal("ReceivedFromTransfer", result.ErrorDetails!["identityLockReason"]);
    }

    private static CorrectInventoryLotIdentity CreateUseCase(
        InMemoryLots lots,
        EditableSupport support,
        CapturingCorrections corrections,
        CatalogProduct product) =>
        new(
            lots,
            support,
            corrections,
            new FixedProducts(product),
            new ImmediateUnitOfWork(),
            new FixedClock());

    private sealed class EditableSupport : IInventoryLotIdentityEditSupport
    {
        public Dictionary<Guid, IReadOnlyList<StockMovementType>> Movements { get; } = new();
        public HashSet<Guid> DraftLotIds { get; } = new();

        public Task<IReadOnlyDictionary<Guid, IReadOnlyList<StockMovementType>>> ListDistinctMovementTypesByLotIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<Guid> lotIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, IReadOnlyList<StockMovementType>>>(Movements);

        public Task<IReadOnlySet<Guid>> ListLotIdsReferencedByActiveTransferDraftAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<Guid> lotIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlySet<Guid>>(DraftLotIds);
    }

    private sealed class CapturingCorrections : IInventoryLotIdentityCorrectionRepository
    {
        public List<InventoryLotIdentityCorrection> Items { get; } = [];

        public Task AddAsync(InventoryLotIdentityCorrection correction, CancellationToken cancellationToken = default)
        {
            Items.Add(correction);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryLots : IInventoryLotRepository
    {
        private readonly List<InventoryLot> _lots;

        public InMemoryLots(IEnumerable<InventoryLot> lots) => _lots = lots.ToList();

        public Task<InventoryLot?> GetByIdAsync(
            PosOrganizationId organizationId,
            InventoryLotId lotId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_lots.FirstOrDefault(l => l.OrganizationId == organizationId && l.Id == lotId));

        public Task<InventoryLot?> FindAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            DateOnly expirationDate,
            string normalizedLotNumber,
            PosBranchId? branchId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_lots.FirstOrDefault(l =>
                l.OrganizationId == organizationId
                && l.ProductId == productId
                && l.ExpirationDate == expirationDate
                && l.NormalizedLotNumber == normalizedLotNumber
                && Equals(l.BranchId, branchId)));

        public Task UpdateAsync(InventoryLot lot, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddAsync(InventoryLot lot, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddMovementAsync(InventoryLotMovement movement, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AdoptOrgLevelLotsForBranchAsync(PosOrganizationId organizationId, CatalogProductId productId, PosBranchId branchId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<(int ExpiredCount, int NearExpiryCount)> CountExpiryAsync(PosOrganizationId organizationId, DateOnly today, PosBranchId? branchId = null, CancellationToken cancellationToken = default) => Task.FromResult((0, 0));
        public Task<bool> HasMovementAsync(PosOrganizationId organizationId, Guid sourceId, InventoryLotId lotId, StockMovementType movementType, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<IReadOnlyList<InventoryLotMovement>> ListBySourceAsync(PosOrganizationId organizationId, Guid sourceId, StockMovementType movementType, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<InventoryLotMovement>>([]);
        public Task<(IReadOnlyList<InventoryLot> Items, int TotalCount)> ListExpiringPagedAsync(PosOrganizationId organizationId, PosBranchId? branchId, DateOnly expireOnOrBefore, DateOnly? expireOnOrAfter, string? search, int skip, int take, CancellationToken cancellationToken = default) => Task.FromResult<(IReadOnlyList<InventoryLot>, int)>(([], 0));
        public Task<IReadOnlyList<InventoryLot>> ListOnHandAsync(PosOrganizationId organizationId, CatalogProductId productId, PosBranchId? branchId, bool includeDepleted, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<InventoryLot>>(_lots);
        public Task<IReadOnlyList<InventoryLot>> ListOrgLevelOnHandAsync(PosOrganizationId organizationId, CatalogProductId productId, bool includeDepleted, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<InventoryLot>>([]);
        public Task<(IReadOnlyList<InventoryLot> Items, int TotalCount)> ListPagedAsync(PosOrganizationId organizationId, CatalogProductId productId, PosBranchId? branchId, bool includeDepleted, int skip, int take, CancellationToken cancellationToken = default) => Task.FromResult<(IReadOnlyList<InventoryLot>, int)>((_lots, _lots.Count));
    }

    private sealed class FixedProducts(CatalogProduct product) : ICatalogProductRepository
    {
        public Task<CatalogProduct?> GetByIdAsync(PosOrganizationId organizationId, CatalogProductId productId, CancellationToken cancellationToken = default) =>
            Task.FromResult(product.Id == productId && product.OrganizationId == organizationId ? product : null);

        public Task<CatalogProduct?> FindByNormalizedSkuAsync(PosOrganizationId organizationId, string normalizedSku, CancellationToken cancellationToken = default) =>
            Task.FromResult<CatalogProduct?>(null);

        public Task<CatalogProduct?> FindByBarcodeAsync(PosOrganizationId organizationId, string barcode, CancellationToken cancellationToken = default) =>
            Task.FromResult<CatalogProduct?>(null);

        public Task<IReadOnlyList<CatalogProduct>> ListByIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<CatalogProductId> productIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CatalogProduct>>([product]);

        public Task<(IReadOnlyList<CatalogProduct> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId,
            CatalogProductFilter filter,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<CatalogProduct>, int)>(([product], 1));

        public Task<IReadOnlyList<Guid>> ListIdsAsync(
            PosOrganizationId organizationId,
            CatalogProductFilter filter,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Guid>>([product.Id.Value]);

        public Task<(int TotalCount, int AvailableCount, int NotAvailableCount)> CountConnectedBuyerAvailabilityAsync(
            PosOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult((1, 1, 0));

        public Task<IReadOnlyList<(Guid? CategoryId, int Count)>> ListConnectedBuyerAvailabilityCategoryFacetsAsync(
            PosOrganizationId organizationId,
            CatalogProductFilter filter,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<(Guid?, int)>>([]);

        public Task<CatalogProduct?> FindByPlatformGlobalProductIdAsync(
            PosOrganizationId organizationId,
            Guid platformGlobalProductId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<CatalogProduct?>(null);

        public Task<IReadOnlySet<Guid>> ListPlatformGlobalProductIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<Guid> platformGlobalProductIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlySet<Guid>>(new HashSet<Guid>());

        public Task AddAsync(CatalogProduct catalogProduct, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateAsync(CatalogProduct catalogProduct, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class ImmediateUnitOfWork : IPosUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<T> ExecuteInSerializableTransactionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default) => action(cancellationToken);
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow => Utc.AddMinutes(10);
    }
}
