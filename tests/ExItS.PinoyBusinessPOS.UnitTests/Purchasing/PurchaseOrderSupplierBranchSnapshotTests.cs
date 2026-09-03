using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Application.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Suppliers;

namespace ExItS.PinoyBusinessPOS.UnitTests.Purchasing;

/// <summary>POBRANCH-01..08 — supplier source-branch snapshot on purchase orders.</summary>
public sealed class PurchaseOrderSupplierBranchSnapshotTests
{
    private static readonly Guid BuyerOrg = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid SupplierOrg = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ProductA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid SupplierA = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid ReceivingBranch = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private static readonly Guid Iloilo = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid Cebu = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly DateTimeOffset Now = new(2026, 9, 3, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void POBRANCH_01_connected_iloilo_supplier_po_snapshots_iloilo()
    {
        var po = CreateDraftWithSupplierBranch(Iloilo, "Iloilo Branch");
        Assert.Equal(Iloilo, po.SupplierBranchId);
        Assert.Equal("Iloilo Branch", po.SupplierBranchNameSnapshot);
    }

    [Fact]
    public void POBRANCH_02_relationship_change_to_cebu_does_not_rewrite_old_po()
    {
        var relationship = ActiveRelationship(Iloilo, "Iloilo Branch");
        var po = CreateDraftWithSupplierBranch(
            relationship.SupplierBranchId,
            relationship.SupplierBranchNameSnapshot);

        relationship.SetSupplierLocation(Cebu, "Cebu Branch", Now.AddMinutes(5));

        Assert.Equal(Cebu, relationship.SupplierBranchId);
        Assert.Equal("Cebu Branch", relationship.SupplierBranchNameSnapshot);
        Assert.Equal(Iloilo, po.SupplierBranchId);
        Assert.Equal("Iloilo Branch", po.SupplierBranchNameSnapshot);
    }

    [Fact]
    public void POBRANCH_03_new_po_after_change_snapshots_cebu()
    {
        var relationship = ActiveRelationship(Iloilo, "Iloilo Branch");
        relationship.SetSupplierLocation(Cebu, "Cebu Branch", Now.AddMinutes(1));

        var oldPo = CreateDraftWithSupplierBranch(Iloilo, "Iloilo Branch");
        var newPo = CreateDraftWithSupplierBranch(
            relationship.SupplierBranchId,
            relationship.SupplierBranchNameSnapshot);

        Assert.Equal(Iloilo, oldPo.SupplierBranchId);
        Assert.Equal(Cebu, newPo.SupplierBranchId);
        Assert.Equal("Cebu Branch", newPo.SupplierBranchNameSnapshot);
    }

    [Fact]
    public void POBRANCH_04_manual_supplier_remains_null()
    {
        var po = PurchaseOrder.CreateDraft(
            PosOrganizationId.From(BuyerOrg),
            SupplierId.From(SupplierA),
            DateOnly.FromDateTime(Now.Date),
            [new PurchaseOrderLineDraft(CatalogProductId.From(ProductA), 1m, 10m)],
            Now);

        Assert.Null(po.SupplierBranchId);
        Assert.Null(po.SupplierBranchNameSnapshot);

        var dto = PurchaseMapper.Map(po, supplierName: "Manual Co");
        Assert.Null(dto.SupplierBranchId);
        Assert.Null(dto.SupplierBranchName);
    }

    [Fact]
    public void POBRANCH_05_receiving_branch_remains_buyer_branch()
    {
        var po = Submit(CreateDraftWithSupplierBranch(Iloilo, "Iloilo Branch"));
        var grn = GoodsReceipt.Create(
            PosOrganizationId.From(BuyerOrg),
            po.Id,
            "GRN-20260903-000001",
            po,
            [new PurchaseOrderReceiveLineDraft(CatalogProductId.From(ProductA), 1m)],
            Guid.NewGuid(),
            Now,
            receivingBranchId: PosBranchId.From(ReceivingBranch));

        Assert.Equal(Iloilo, po.SupplierBranchId);
        Assert.Equal(ReceivingBranch, grn.ReceivingBranchId!.Value);
        Assert.NotEqual(po.SupplierBranchId, grn.ReceivingBranchId.Value);
    }

    [Fact]
    public void POBRANCH_06_inventory_receipt_posts_only_to_buyer_receiving_branch()
    {
        var po = Submit(CreateDraftWithSupplierBranch(Iloilo, "Iloilo Branch"));
        var grn = GoodsReceipt.Create(
            PosOrganizationId.From(BuyerOrg),
            po.Id,
            "GRN-20260903-000002",
            po,
            [new PurchaseOrderReceiveLineDraft(CatalogProductId.From(ProductA), 1m)],
            Guid.NewGuid(),
            Now,
            receivingBranchId: PosBranchId.From(ReceivingBranch));

        // Goods receipt owns receiving branch only — supplier source is on the PO, never the GRN destination.
        Assert.Equal(ReceivingBranch, grn.ReceivingBranchId!.Value);
        Assert.Null(typeof(GoodsReceipt).GetProperty("SupplierBranchId"));
    }

    [Fact]
    public async Task POBRANCH_07_connected_supplier_routing_uses_po_snapshot()
    {
        var relationship = ActiveRelationship(Cebu, "Cebu Branch");
        var buyerPo = Submit(CreateDraftWithSupplierBranch(Iloilo, "Iloilo Branch"));
        var line = ConnectedPurchaseOrderLine.Create(
            CatalogProductId.From(ProductA), "Item", null, 1m, 10m, "Piece");
        var incoming = ConnectedPurchaseOrder.CreateFromBuyerSubmission(
            relationship,
            buyerPo.Id,
            buyerPo.PoNumber,
            buyerPo.OrderDate,
            null,
            [line],
            Now);

        var buyerOrders = new InMemoryBuyerOrders();
        buyerOrders.Items.Add(buyerPo);
        var connectedOrders = new InMemoryConnectedOrders();
        connectedOrders.Items.Add(incoming);
        var relationships = new InMemoryRelationships { Stored = relationship };

        var get = new GetIncomingOrder(
            connectedOrders,
            new FakeAccess(),
            relationships,
            buyerOrders);

        var result = await get.ExecuteAsync(SupplierOrg, incoming.Id.Value);
        Assert.True(result.IsSuccess);
        Assert.Equal(Iloilo, result.Value!.SupplierBranchId);
        Assert.Equal("Iloilo Branch", result.Value.SupplierBranchName);
        Assert.NotEqual(relationship.SupplierBranchId, result.Value.SupplierBranchId);
    }

    [Fact]
    public void POBRANCH_08_legacy_po_null_remains_valid()
    {
        var legacy = PurchaseOrder.CreateDraft(
            PosOrganizationId.From(BuyerOrg),
            SupplierId.From(SupplierA),
            DateOnly.FromDateTime(Now.Date),
            [new PurchaseOrderLineDraft(CatalogProductId.From(ProductA), 1m, 10m)],
            Now);
        Submit(legacy);

        Assert.Null(legacy.SupplierBranchId);
        Assert.Null(legacy.SupplierBranchNameSnapshot);

        var dto = PurchaseMapper.Map(legacy, supplierName: "ABC Wholesale");
        Assert.Null(dto.SupplierBranchId);
        Assert.Null(dto.SupplierBranchName);
    }

    [Fact]
    public void Submit_preserves_supplier_branch_snapshot()
    {
        var po = CreateDraftWithSupplierBranch(Iloilo, "Iloilo Branch");
        Submit(po);
        Assert.Equal(Iloilo, po.SupplierBranchId);
        Assert.Equal("Iloilo Branch", po.SupplierBranchNameSnapshot);
        Assert.Equal(PurchaseOrderStatus.Ordered, po.Status);
    }

    [Fact]
    public void Draft_update_can_refresh_snapshot_only_while_draft()
    {
        var po = CreateDraftWithSupplierBranch(Iloilo, "Iloilo Branch");
        po.UpdateDraft(
            SupplierId.From(SupplierA),
            DateOnly.FromDateTime(Now.Date),
            [new PurchaseOrderLineDraft(CatalogProductId.From(ProductA), 1m, 10m)],
            Now.AddMinutes(1),
            supplierBranchId: Cebu,
            supplierBranchName: "Cebu Branch",
            updateSupplierSourceBranch: true);
        Assert.Equal(Cebu, po.SupplierBranchId);

        Submit(po);
        Assert.Throws<DomainException>(() =>
            po.UpdateDraft(
                SupplierId.From(SupplierA),
                DateOnly.FromDateTime(Now.Date),
                [new PurchaseOrderLineDraft(CatalogProductId.From(ProductA), 1m, 10m)],
                Now.AddMinutes(2),
                supplierBranchId: Iloilo,
                supplierBranchName: "Iloilo Branch",
                updateSupplierSourceBranch: true));
        Assert.Equal(Cebu, po.SupplierBranchId);
    }

    private static ConnectedSupplierRelationship ActiveRelationship(Guid branchId, string branchName)
    {
        var relationship = ConnectedSupplierRelationship.Request(
            PosOrganizationId.From(BuyerOrg),
            PosOrganizationId.From(SupplierOrg),
            Now,
            supplierBranchId: branchId,
            supplierBranchName: branchName);
        relationship.Approve(Now.AddSeconds(1));
        return relationship;
    }

    private static PurchaseOrder CreateDraftWithSupplierBranch(Guid? branchId, string? branchName) =>
        PurchaseOrder.CreateDraft(
            PosOrganizationId.From(BuyerOrg),
            SupplierId.From(SupplierA),
            DateOnly.FromDateTime(Now.Date),
            [new PurchaseOrderLineDraft(CatalogProductId.From(ProductA), 1m, 10m)],
            Now,
            supplierBranchId: branchId,
            supplierBranchName: branchName);

    private static PurchaseOrder Submit(PurchaseOrder po)
    {
        po.Submit(
            "PO-20260903-000001",
            [
                new PurchaseOrderLineSnapshotInput(
                    CatalogProductId.From(ProductA),
                    "Item",
                    UnitOfMeasure.Piece,
                    1m,
                    10m)
            ],
            Guid.NewGuid(),
            Now);
        return po;
    }

    private sealed class FakeAccess : IPosCommercialAccessAccessor
    {
        public PosCommercialAccess Current { get; set; } = PosCommercialAccess.DevelopmentDefault;
    }

    private sealed class InMemoryBuyerOrders : IPurchaseOrderRepository
    {
        public List<PurchaseOrder> Items { get; } = [];

        public Task AddAsync(PurchaseOrder purchaseOrder, CancellationToken cancellationToken = default)
        {
            Items.Add(purchaseOrder);
            return Task.CompletedTask;
        }

        public Task<PurchaseOrder?> GetByIdAsync(
            PosOrganizationId organizationId,
            PurchaseOrderId purchaseOrderId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(x => x.OrganizationId == organizationId && x.Id == purchaseOrderId));

        public Task<GoodsReceipt?> GetGoodsReceiptByIdAsync(
            PosOrganizationId organizationId,
            GoodsReceiptId goodsReceiptId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<GoodsReceipt?>(null);

        public Task UpdateGoodsReceiptAsync(GoodsReceipt receipt, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<(IReadOnlyList<PurchaseOrder> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId,
            PurchaseOrderFilter filter,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<PurchaseOrder>, int)>((Items, Items.Count));

        public Task<IReadOnlyList<GoodsReceipt>> ListGoodsReceiptsForPurchaseOrderAsync(
            PosOrganizationId organizationId,
            PurchaseOrderId purchaseOrderId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<GoodsReceipt>>([]);

        public Task<(PurchaseOrder PurchaseOrder, GoodsReceipt GoodsReceipt)> ReceiveAsync(
            PosOrganizationId organizationId,
            PurchaseOrderId purchaseOrderId,
            DateOnly businessDateUtc,
            Func<string, (PurchaseOrder UpdatedPo, GoodsReceipt Receipt)> applyReceive,
            Func<GoodsReceipt, PurchaseOrder, CancellationToken, Task>? afterReceiptCreated = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<PurchaseOrder> SubmitAsync(
            PosOrganizationId organizationId,
            PurchaseOrderId purchaseOrderId,
            DateOnly businessDateUtc,
            Func<string, PurchaseOrder> applySubmit,
            Func<PurchaseOrder, CancellationToken, Task>? beforeCommit = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task UpdateAsync(PurchaseOrder purchaseOrder, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class InMemoryConnectedOrders : IConnectedPurchaseOrderRepository
    {
        public List<ConnectedPurchaseOrder> Items { get; } = [];

        public Task AddAsync(ConnectedPurchaseOrder order, CancellationToken ct = default)
        {
            Items.Add(order);
            return Task.CompletedTask;
        }

        public Task<ConnectedPurchaseOrder?> GetAsync(ConnectedPurchaseOrderId id, CancellationToken ct = default) =>
            Task.FromResult(Items.FirstOrDefault(x => x.Id == id));

        public Task<ConnectedPurchaseOrder?> GetByBuyerPurchaseOrderAsync(PurchaseOrderId id, CancellationToken ct = default) =>
            Task.FromResult(Items.FirstOrDefault(x => x.BuyerPurchaseOrderId == id));

        public Task<IReadOnlyList<ConnectedPurchaseOrder>> ListIncomingAsync(
            PosOrganizationId supplierOrganizationId,
            CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ConnectedPurchaseOrder>>(
                Items.Where(x => x.SupplierOrganizationId == supplierOrganizationId).ToList());

        public Task UpdateAsync(ConnectedPurchaseOrder order, CancellationToken ct = default) =>
            Task.CompletedTask;
    }

    private sealed class InMemoryRelationships : IConnectedSupplierRelationshipRepository
    {
        public ConnectedSupplierRelationship? Stored { get; set; }

        public Task AddAsync(ConnectedSupplierRelationship relationship, CancellationToken ct = default)
        {
            Stored = relationship;
            return Task.CompletedTask;
        }

        public Task<ConnectedSupplierRelationship?> FindOpenAsync(
            PosOrganizationId buyer,
            PosOrganizationId supplier,
            CancellationToken ct = default) =>
            Task.FromResult(Stored);

        public Task<ConnectedSupplierRelationship?> GetAsync(
            ConnectedSupplierRelationshipId id,
            CancellationToken ct = default) =>
            Task.FromResult(Stored is not null && Stored.Id == id ? Stored : null);

        public Task<IReadOnlyList<ConnectedSupplierRelationship>> ListAsync(
            PosOrganizationId organizationId,
            bool supplierView,
            CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ConnectedSupplierRelationship>>(Stored is null ? [] : [Stored]);

        public Task UpdateAsync(ConnectedSupplierRelationship relationship, CancellationToken ct = default)
        {
            Stored = relationship;
            return Task.CompletedTask;
        }
    }
}
