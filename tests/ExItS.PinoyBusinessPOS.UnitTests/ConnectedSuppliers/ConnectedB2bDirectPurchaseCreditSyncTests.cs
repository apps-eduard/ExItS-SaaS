using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Application.SupplierPayables;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Sales;
using ExItS.PinoyBusinessPOS.Domain.SupplierPayables;
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
        var (sync, credits, payables, relationship, supplier) = await CreateHarnessAsync();
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

        var payable = await payables.FindBySourceAsync(
            Buyer,
            SupplierPayableSourceType.DirectPurchaseReceipt,
            receipt.Id.Value);
        Assert.NotNull(payable);
        Assert.Equal(747m, payable!.OriginalAmount);
        Assert.Equal(0m, payable.PaidAtReceiptAmount);
        Assert.Equal(747m, payable.Balance);
    }

    [Fact]
    public async Task PostFromReceipt_creates_payable_when_missing()
    {
        var (sync, _, payables, _, supplier) = await CreateHarnessAsync();
        var receipt = CreateReceipt(supplier.Id, totalCost: 500m, paidActor: Actor);

        await sync.PostFromReceiptAsync(receipt, paidNow: 100m, Now, dueDate: new DateOnly(2026, 10, 1));

        var payable = await payables.FindBySourceAsync(
            Buyer,
            SupplierPayableSourceType.DirectPurchaseReceipt,
            receipt.Id.Value);
        Assert.NotNull(payable);
        Assert.Equal(500m, payable!.OriginalAmount);
        Assert.Equal(100m, payable.PaidAtReceiptAmount);
        Assert.Equal(400m, payable.Balance);
        Assert.Equal(new DateOnly(2026, 10, 1), payable.DueDate);
        Assert.Equal(supplier.Id, payable.SupplierId);
    }

    [Fact]
    public async Task PostFromReceipt_is_idempotent_for_credit_and_payable()
    {
        var (sync, credits, payables, _, supplier) = await CreateHarnessAsync();
        var receipt = CreateReceipt(supplier.Id, totalCost: 747m);

        await sync.PostFromReceiptAsync(receipt, paidNow: 0m, Now);
        await sync.PostFromReceiptAsync(receipt, paidNow: 0m, Now);

        var entries = await credits.ListChronologicalForBuyerAsync(Seller, Buyer);
        Assert.Single(entries);
        Assert.Equal(747m, await credits.SumActiveAmountAsync(Seller, Buyer));
        Assert.Equal(1, payables.Count);
    }

    [Fact]
    public async Task ReconcileMissingPayables_creates_once_second_call_noop()
    {
        var (sync, credits, payables, relationship, supplier) = await CreateHarnessAsync();
        var receiptId = Guid.Parse("cccccccc-cccc-4ccc-8ccc-cccccccccccc");
        var entry = BusinessCreditEntry.Create(
            Seller,
            Buyer,
            321m,
            ConnectedPoUtangObligationProjection.BuildDirectPurchaseRemark(receiptId, "DPR-1"),
            Now,
            relationship.Id.Value);
        entry.ApplyCurrentDueDate(new DateOnly(2026, 11, 1));
        await credits.AddAsync(entry);

        var first = await sync.ReconcileMissingPayablesForRelationshipAsync(
            Seller,
            Buyer,
            relationship.Id.Value,
            Actor,
            Now);
        var second = await sync.ReconcileMissingPayablesForRelationshipAsync(
            Seller,
            Buyer,
            relationship.Id.Value,
            Actor,
            Now.AddMinutes(1));

        Assert.Equal(1, first);
        Assert.Equal(0, second);
        Assert.Equal(1, payables.Count);
        var payable = await payables.FindBySourceAsync(
            Buyer,
            SupplierPayableSourceType.DirectPurchaseReceipt,
            receiptId);
        Assert.NotNull(payable);
        Assert.Equal(321m, payable!.OriginalAmount);
        Assert.Equal(0m, payable.PaidAtReceiptAmount);
        Assert.Equal(new DateOnly(2026, 11, 1), payable.DueDate);
        Assert.Equal(supplier.Id, payable.SupplierId);
    }

    [Fact]
    public async Task PostFromReceipt_fully_paid_does_not_post_credit()
    {
        var (sync, credits, payables, _, supplier) = await CreateHarnessAsync();
        var receipt = CreateReceipt(supplier.Id, totalCost: 747m);

        await sync.PostFromReceiptAsync(receipt, paidNow: 747m, Now);

        Assert.Equal(0m, await credits.SumActiveAmountAsync(Seller, Buyer));
        Assert.Equal(0, payables.Count);
    }

    [Fact]
    public async Task PostFromReceipt_unconnected_supplier_skips()
    {
        var credits = new InMemoryBusinessCredits();
        var payables = new InMemoryPayables();
        var suppliers = new InMemorySuppliers();
        var relationships = new InMemoryRelationships();
        var localSupplier = Supplier.Create(Buyer, "SUP-000099", "Local Supplier", Now);
        await suppliers.AddAsync(localSupplier);
        var sync = new ConnectedB2bDirectPurchaseCreditSync(suppliers, relationships, credits, payables);
        var receipt = CreateReceipt(localSupplier.Id, totalCost: 100m);

        await sync.PostFromReceiptAsync(receipt, paidNow: 0m, Now);

        Assert.Equal(0m, await credits.SumActiveAmountAsync(Seller, Buyer));
        Assert.Equal(0, payables.Count);
    }

    [Fact]
    public async Task EnsurePayableForSale_creates_once_second_call_noop()
    {
        var (sync, _, payables, relationship, supplier) = await CreateHarnessAsync();
        var saleId = Guid.Parse("eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee");

        await sync.EnsurePayableForSaleAsync(
            Seller,
            Buyer,
            relationship.Id.Value,
            saleId,
            amount: 815m,
            dueDate: new DateOnly(2026, 10, 15),
            Actor,
            Now);
        await sync.EnsurePayableForSaleAsync(
            Seller,
            Buyer,
            relationship.Id.Value,
            saleId,
            amount: 815m,
            dueDate: new DateOnly(2026, 10, 15),
            Actor,
            Now.AddMinutes(1));

        Assert.Equal(1, payables.Count);
        var payable = await payables.FindBySourceAsync(Buyer, SupplierPayableSourceType.Sale, saleId);
        Assert.NotNull(payable);
        Assert.Equal(815m, payable!.OriginalAmount);
        Assert.Equal(815m, payable.Balance);
        Assert.Equal(SupplierPayableStatus.Open, payable.Status);
        Assert.Equal(supplier.Id, payable.SupplierId);
        Assert.Equal(new DateOnly(2026, 10, 15), payable.DueDate);
    }

    [Fact]
    public async Task ReconcileMissingPayables_heals_sale_sourced_credit_without_remark_prefix()
    {
        var (sync, credits, payables, relationship, supplier) = await CreateHarnessAsync();
        var saleId = Guid.Parse("ffffffff-ffff-4fff-8fff-ffffffffffff");
        var entry = BusinessCreditEntry.Create(
            Seller,
            Buyer,
            815m,
            ProductBasedUtangRemarks.ForSaleNumber("SALE-20260917-000815"),
            Now,
            relationship.Id.Value,
            sourceSaleId: SaleId.From(saleId));
        entry.ApplyCurrentDueDate(new DateOnly(2026, 11, 1));
        await credits.AddAsync(entry);

        var first = await sync.ReconcileMissingPayablesForRelationshipAsync(
            Seller,
            Buyer,
            relationship.Id.Value,
            Actor,
            Now);
        var second = await sync.ReconcileMissingPayablesForRelationshipAsync(
            Seller,
            Buyer,
            relationship.Id.Value,
            Actor,
            Now.AddMinutes(1));

        Assert.Equal(1, first);
        Assert.Equal(0, second);
        var payable = await payables.FindBySourceAsync(Buyer, SupplierPayableSourceType.Sale, saleId);
        Assert.NotNull(payable);
        Assert.Equal(815m, payable!.OriginalAmount);
        Assert.Equal(supplier.Id, payable.SupplierId);
    }

    [Fact]
    public async Task ReversePayableForSale_voids_matching_payable()
    {
        var (sync, _, payables, relationship, _) = await CreateHarnessAsync();
        var saleId = Guid.Parse("aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee");
        await sync.EnsurePayableForSaleAsync(
            Seller,
            Buyer,
            relationship.Id.Value,
            saleId,
            815m,
            dueDate: null,
            Actor,
            Now);

        await sync.ReversePayableForSaleAsync(Buyer, saleId, "void sale", Actor, Now.AddMinutes(1));

        var payable = await payables.FindBySourceAsync(Buyer, SupplierPayableSourceType.Sale, saleId);
        Assert.NotNull(payable);
        Assert.Equal(SupplierPayableStatus.Voided, payable!.Status);
    }

    [Fact]
    public async Task ReversePayableForSale_blocks_when_payable_has_posted_payments()
    {
        var (sync, _, payables, relationship, _) = await CreateHarnessAsync();
        var saleId = Guid.Parse("bbbbbbbb-cccc-4ddd-8eee-ffffffffffff");
        await sync.EnsurePayableForSaleAsync(
            Seller,
            Buyer,
            relationship.Id.Value,
            saleId,
            815m,
            dueDate: null,
            Actor,
            Now);
        var payable = await payables.FindBySourceAsync(Buyer, SupplierPayableSourceType.Sale, saleId);
        Assert.NotNull(payable);
        payable!.ApplyPayment(100m, SupplierPayablePaymentMethod.Cash, Actor, Now.AddMinutes(1));
        await payables.UpdateAsync(payable);

        await Assert.ThrowsAsync<DomainException>(() =>
            sync.ReversePayableForSaleAsync(Buyer, saleId, "void sale", Actor, Now.AddMinutes(2)));

        Assert.Equal(SupplierPayableStatus.PartiallyPaid, payable.Status);
    }

    [Fact]
    public async Task ReverseForReceipt_reverses_matching_credit_entry()
    {
        var (sync, credits, payables, _, supplier) = await CreateHarnessAsync();
        var receipt = CreateReceipt(supplier.Id, totalCost: 747m);
        await sync.PostFromReceiptAsync(receipt, paidNow: 0m, Now);

        await sync.ReverseForReceiptAsync(receipt, "void receipt", Now.AddMinutes(1), actorId: Actor);

        Assert.Equal(0m, await credits.SumActiveAmountAsync(Seller, Buyer));
        var entries = await credits.ListChronologicalForBuyerAsync(Seller, Buyer);
        Assert.Equal(CreditEntryStatus.Reversed, entries[0].Status);
        var payable = await payables.FindBySourceAsync(
            Buyer,
            SupplierPayableSourceType.DirectPurchaseReceipt,
            receipt.Id.Value);
        Assert.NotNull(payable);
        Assert.Equal(SupplierPayableStatus.Voided, payable!.Status);
    }

    [Fact]
    public async Task ReverseForReceipt_blocks_when_payable_has_posted_payments()
    {
        var (sync, credits, payables, _, supplier) = await CreateHarnessAsync();
        var receipt = CreateReceipt(supplier.Id, totalCost: 747m);
        await sync.PostFromReceiptAsync(receipt, paidNow: 0m, Now);
        var payable = await payables.FindBySourceAsync(
            Buyer,
            SupplierPayableSourceType.DirectPurchaseReceipt,
            receipt.Id.Value);
        Assert.NotNull(payable);
        payable!.ApplyPayment(
            50m,
            SupplierPayablePaymentMethod.Cash,
            Actor,
            Now.AddMinutes(1),
            reference: "partial");
        await payables.UpdateAsync(payable);

        await Assert.ThrowsAsync<DomainException>(() =>
            sync.ReverseForReceiptAsync(receipt, "void receipt", Now.AddMinutes(2), actorId: Actor));

        Assert.Equal(747m, await credits.SumActiveAmountAsync(Seller, Buyer));
        Assert.Equal(SupplierPayableStatus.PartiallyPaid, payable.Status);
    }

    [Fact]
    public async Task Mixed_PO_remark_and_Direct_remark_sum_to_1390()
    {
        var (sync, credits, _, relationship, supplier) = await CreateHarnessAsync();
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
        InMemoryPayables Payables,
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
        var payables = new InMemoryPayables();
        var sync = new ConnectedB2bDirectPurchaseCreditSync(suppliers, relationships, credits, payables);
        return (sync, credits, payables, relationship, supplier);
    }

    private static DirectPurchaseReceipt CreateReceipt(
        SupplierId supplierId,
        decimal totalCost,
        Guid? paidActor = null)
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
            paidActor ?? Actor,
            Now,
            supplierId,
            sourceName: "Connected Seller",
            receivingBranchId: Branch);
    }

    private sealed class InMemoryPayables : ISupplierPayableRepository
    {
        private readonly List<SupplierPayable> _items = [];

        public int Count => _items.Count;

        public Task<SupplierPayable?> GetByIdAsync(
            PosOrganizationId organizationId,
            SupplierPayableId payableId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(p =>
                p.OrganizationId == organizationId && p.Id == payableId));

        public Task<SupplierPayable?> FindBySourceAsync(
            PosOrganizationId organizationId,
            SupplierPayableSourceType sourceType,
            Guid sourceId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(p =>
                p.OrganizationId == organizationId
                && p.SourceType == sourceType
                && p.SourceId == sourceId));

        public Task<(IReadOnlyList<SupplierPayable> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId,
            SupplierPayableFilter filter,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<SupplierPayable>, int)>((_items, _items.Count));

        public Task<IReadOnlyList<SupplierPayablePayment>> ListPaymentsAsync(
            PosOrganizationId organizationId,
            SupplierPayableId payableId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SupplierPayablePayment>>([]);

        public Task AddAsync(SupplierPayable payable, CancellationToken cancellationToken = default)
        {
            _items.Add(payable);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(SupplierPayable payable, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<SupplierPayableSummaryTotals> GetSupplierSummaryAsync(
            PosOrganizationId organizationId,
            SupplierId supplierId,
            DateOnly asOfDate,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new SupplierPayableSummaryTotals(0m, 0m, 0));
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
