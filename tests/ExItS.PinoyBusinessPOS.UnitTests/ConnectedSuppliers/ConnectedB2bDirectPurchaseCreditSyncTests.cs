using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Suppliers;

namespace ExItS.PinoyBusinessPOS.UnitTests.ConnectedSuppliers;

public sealed class ConnectedB2bDirectPurchaseCreditSyncTests
{
    private static readonly PosOrganizationId Buyer = PosOrganizationId.From(
        Guid.Parse("11111111-1111-4111-8111-111111111111"));
    private static readonly PosOrganizationId Seller = PosOrganizationId.From(
        Guid.Parse("22222222-2222-4222-8222-222222222222"));
    private static readonly Guid Actor = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa");
    private static readonly PosBranchId Branch = PosBranchId.From(
        Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb"));
    private static readonly DateTimeOffset Now = new(2026, 9, 16, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task PostFromReceipt_connected_Utang_posts_seller_BusinessCreditEntry()
    {
        var (sync, credits, relationship, supplier) = await CreateHarnessAsync();
        var receipt = CreateReceipt(supplier.Id, totalCost: 747m);

        await sync.PostFromReceiptAsync(receipt, paidNow: 0m, Now);

        var outstanding = await credits.SumActiveAmountAsync(Seller, Buyer);
        Assert.Equal(747m, outstanding);
        var entries = await credits.ListChronologicalForBuyerAsync(Seller, Buyer);
        Assert.Single(entries);
        Assert.True(
            ConnectedPoUtangObligationProjection.TryParseDirectPurchaseReceiptId(
                entries[0].Remarks,
                out var receiptId));
        Assert.Equal(receipt.Id.Value, receiptId);
        Assert.Equal(relationship.Id.Value, entries[0].ConnectionId);
    }

    [Fact]
    public async Task PostFromReceipt_fully_paid_does_not_post_credit()
    {
        var (sync, credits, _, supplier) = await CreateHarnessAsync();
        var receipt = CreateReceipt(supplier.Id, totalCost: 747m);

        await sync.PostFromReceiptAsync(receipt, paidNow: 747m, Now);

        Assert.Equal(0m, await credits.SumActiveAmountAsync(Seller, Buyer));
    }

    [Fact]
    public async Task PostFromReceipt_unconnected_supplier_skips()
    {
        var credits = new InMemoryBusinessCredits();
        var suppliers = new InMemorySuppliers();
        var relationships = new InMemoryRelationships();
        var localSupplier = Supplier.Create(Buyer, "SUP-000099", "Local Supplier", Now);
        await suppliers.AddAsync(localSupplier);
        var sync = new ConnectedB2bDirectPurchaseCreditSync(suppliers, relationships, credits);
        var receipt = CreateReceipt(localSupplier.Id, totalCost: 100m);

        await sync.PostFromReceiptAsync(receipt, paidNow: 0m, Now);

        Assert.Equal(0m, await credits.SumActiveAmountAsync(Seller, Buyer));
    }

    [Fact]
    public async Task ReverseForReceipt_reverses_matching_credit_entry()
    {
        var (sync, credits, _, supplier) = await CreateHarnessAsync();
        var receipt = CreateReceipt(supplier.Id, totalCost: 747m);
        await sync.PostFromReceiptAsync(receipt, paidNow: 0m, Now);

        await sync.ReverseForReceiptAsync(receipt, "void receipt", Now.AddMinutes(1));

        Assert.Equal(0m, await credits.SumActiveAmountAsync(Seller, Buyer));
        var entries = await credits.ListChronologicalForBuyerAsync(Seller, Buyer);
        Assert.Equal(CreditEntryStatus.Reversed, entries[0].Status);
    }

    [Fact]
    public async Task Mixed_PO_remark_and_Direct_remark_sum_to_1390()
    {
        var (sync, credits, relationship, supplier) = await CreateHarnessAsync();
        var receipt = CreateReceipt(supplier.Id, totalCost: 747m);
        await sync.PostFromReceiptAsync(receipt, paidNow: 0m, Now);

        var poCredit = BusinessCreditEntry.Create(
            Seller,
            Buyer,
            643m,
            ConnectedPoUtangObligationProjection.BuildGoodsReceiptRemark(
                Guid.Parse("cccccccc-cccc-4ccc-8ccc-cccccccccccc"),
                "PO-100",
                Guid.Parse("dddddddd-dddd-4ddd-8ddd-dddddddddddd")),
            Now,
            relationship.Id.Value);
        await credits.AddAsync(poCredit);

        Assert.Equal(1_390m, await credits.SumActiveAmountAsync(Seller, Buyer));
    }

    private static async Task<(
        ConnectedB2bDirectPurchaseCreditSync Sync,
        InMemoryBusinessCredits Credits,
        ConnectedSupplierRelationship Relationship,
        Supplier Supplier)> CreateHarnessAsync()
    {
        var relationship = ConnectedSupplierRelationship.Request(Buyer, Seller, Now);
        relationship.Approve(Now.AddMinutes(1));
        var relationships = new InMemoryRelationships();
        await relationships.AddAsync(relationship);

        var supplier = Supplier.Create(Buyer, "SUP-000001", "Connected Seller", Now);
        supplier.AttachConnectedRelationship(relationship.Id, Now);
        var suppliers = new InMemorySuppliers();
        await suppliers.AddAsync(supplier);

        var credits = new InMemoryBusinessCredits();
        var sync = new ConnectedB2bDirectPurchaseCreditSync(suppliers, relationships, credits);
        return (sync, credits, relationship, supplier);
    }

    private static DirectPurchaseReceipt CreateReceipt(SupplierId supplierId, decimal totalCost)
    {
        var unitCost = totalCost;
        return DirectPurchaseReceipt.Create(
            Buyer,
            "DPR-20260916-000001",
            DateOnly.FromDateTime(Now.UtcDateTime),
            [
                new DirectPurchaseReceiptLineDraft(
                    CatalogProductId.New(),
                    "Coke",
                    "COKE",
                    UnitOfMeasure.Piece,
                    1m,
                    unitCost,
                    SellingMode.PerItem)
            ],
            Actor,
            Now,
            supplierId,
            sourceName: "Connected Seller",
            receivingBranchId: Branch);
    }

    private sealed class InMemoryBusinessCredits : IBusinessCreditEntryRepository
    {
        private readonly List<BusinessCreditEntry> _entries = [];

        public Task<BusinessCreditEntry?> GetByIdAsync(
            PosOrganizationId sellerOrganizationId,
            BusinessCreditEntryId entryId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_entries.FirstOrDefault(e =>
                e.SellerOrganizationId == sellerOrganizationId && e.Id == entryId));

        public Task AddAsync(BusinessCreditEntry entry, CancellationToken cancellationToken = default)
        {
            _entries.Add(entry);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(BusinessCreditEntry entry, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<decimal> SumActiveAmountAsync(
            PosOrganizationId sellerOrganizationId,
            PosOrganizationId buyerOrganizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                _entries
                    .Where(e =>
                        e.SellerOrganizationId == sellerOrganizationId
                        && e.BuyerOrganizationId == buyerOrganizationId
                        && e.Status == CreditEntryStatus.Active)
                    .Sum(e => e.Amount));

        public Task<IReadOnlyDictionary<Guid, decimal>> SumActiveAmountsByBuyerIdsAsync(
            PosOrganizationId sellerOrganizationId,
            IReadOnlyCollection<Guid> buyerOrganizationIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, decimal>>(
                _entries
                    .Where(e =>
                        e.SellerOrganizationId == sellerOrganizationId
                        && buyerOrganizationIds.Contains(e.BuyerOrganizationId.Value)
                        && e.Status == CreditEntryStatus.Active)
                    .GroupBy(e => e.BuyerOrganizationId.Value)
                    .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount)));

        public Task<IReadOnlyList<BusinessCreditEntry>> ListChronologicalForBuyerAsync(
            PosOrganizationId sellerOrganizationId,
            PosOrganizationId buyerOrganizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<BusinessCreditEntry>>(
                _entries
                    .Where(e =>
                        e.SellerOrganizationId == sellerOrganizationId
                        && e.BuyerOrganizationId == buyerOrganizationId)
                    .OrderBy(e => e.CreatedAtUtc)
                    .ThenBy(e => e.Id.Value)
                    .ToList());

        public Task AcquireBusinessCreditLockAsync(
            PosOrganizationId sellerOrganizationId,
            PosOrganizationId buyerOrganizationId,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class InMemoryRelationships : IConnectedSupplierRelationshipRepository
    {
        private readonly List<ConnectedSupplierRelationship> _items = [];

        public Task AddAsync(ConnectedSupplierRelationship relationship, CancellationToken ct = default)
        {
            _items.Add(relationship);
            return Task.CompletedTask;
        }

        public Task<ConnectedSupplierRelationship?> GetAsync(
            ConnectedSupplierRelationshipId id,
            CancellationToken ct = default) =>
            Task.FromResult(_items.FirstOrDefault(x => x.Id == id));

        public Task<ConnectedSupplierRelationship?> FindOpenAsync(
            PosOrganizationId buyer,
            PosOrganizationId supplier,
            CancellationToken ct = default) =>
            Task.FromResult(_items.FirstOrDefault(x =>
                x.BuyerOrganizationId == buyer && x.SupplierOrganizationId == supplier));

        public Task<IReadOnlyList<ConnectedSupplierRelationship>> ListAsync(
            PosOrganizationId organizationId,
            bool supplierView,
            CancellationToken ct = default) =>
            Task.FromResult((IReadOnlyList<ConnectedSupplierRelationship>)_items);

        public Task UpdateAsync(ConnectedSupplierRelationship relationship, CancellationToken ct = default) =>
            Task.CompletedTask;
    }

    private sealed class InMemorySuppliers : ISupplierRepository
    {
        private readonly List<Supplier> _items = [];

        public Task AddAsync(Supplier supplier, CancellationToken cancellationToken = default)
        {
            _items.Add(supplier);
            return Task.CompletedTask;
        }

        public Task<Supplier?> GetByIdAsync(
            PosOrganizationId organizationId,
            SupplierId supplierId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(s =>
                s.OrganizationId == organizationId && s.Id == supplierId));

        public Task<(IReadOnlyList<Supplier> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId,
            SupplierFilter filter,
            int skip,
            int take,
            IReadOnlyCollection<Guid>? restrictToSupplierIds = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<Supplier>, int)>((_items, _items.Count));

        public Task<Supplier?> FindActiveByNormalizedNameAsync(
            PosOrganizationId organizationId,
            string normalizedName,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Supplier?>(null);

        public Task<Supplier?> FindActiveByNormalizedEmailAsync(
            PosOrganizationId organizationId,
            string normalizedEmail,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Supplier?>(null);

        public Task<Supplier?> FindActiveByNormalizedMobileAsync(
            PosOrganizationId organizationId,
            string normalizedMobile,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Supplier?>(null);

        public Task<Supplier?> FindActiveByNormalizedTaxAsync(
            PosOrganizationId organizationId,
            string normalizedTax,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Supplier?>(null);

        public Task<Supplier?> FindByConnectedRelationshipIdAsync(
            PosOrganizationId organizationId,
            ConnectedSupplierRelationshipId relationshipId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(s =>
                s.OrganizationId == organizationId && s.ConnectedRelationshipId == relationshipId));

        public Task<string> AllocateNextSupplierCodeAsync(
            PosOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult("SUP-1");

        public Task UpdateAsync(Supplier supplier, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesByIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<Guid> supplierIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string>>(
                _items
                    .Where(s => s.OrganizationId == organizationId && supplierIds.Contains(s.Id.Value))
                    .ToDictionary(s => s.Id.Value, s => s.Name));
    }
}
