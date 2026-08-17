using ExItS.PinoyBusinessPOS.Application.Purchasing;

namespace ExItS.PinoyBusinessPOS.UnitTests.Purchasing;

public sealed class PurchaseOrderCreateUiTests
{
    private sealed record Product(Guid ProductId, string Name, Guid? CategoryId);
    private sealed record Line(Guid ProductId, string Name, decimal OrderedQty, decimal UnitPurchaseCost);
    private sealed record ConnectedLine(Guid BuyerProductId, Guid SupplierProductId, decimal Price, decimal Qty);

    [Fact]
    public void Supplier_selection_gates_product_picker()
    {
        Assert.False(PurchaseOrderCreateUi.HasSupplierSelected(null));
        Assert.False(PurchaseOrderCreateUi.HasSupplierSelected(""));
        Assert.False(PurchaseOrderCreateUi.HasSupplierSelected(Guid.Empty.ToString("D")));
        Assert.True(PurchaseOrderCreateUi.HasSupplierSelected(Guid.NewGuid().ToString("D")));
    }

    [Fact]
    public void Supplier_change_with_lines_requires_confirmation()
    {
        var a = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa").ToString("D");
        var b = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb").ToString("D");

        Assert.False(PurchaseOrderCreateUi.RequiresSupplierChangeConfirmation(a, b, lineCount: 0));
        Assert.False(PurchaseOrderCreateUi.RequiresSupplierChangeConfirmation(a, a, lineCount: 3));
        Assert.True(PurchaseOrderCreateUi.RequiresSupplierChangeConfirmation(a, b, lineCount: 2));
        Assert.False(PurchaseOrderCreateUi.RequiresSupplierChangeConfirmation(null, b, lineCount: 2));
    }

    [Fact]
    public void Connected_organization_connection_type_is_detected()
    {
        Assert.True(PurchaseOrderCreateUi.IsConnectedOrganizationSupplier("ConnectedOrganization"));
        Assert.False(PurchaseOrderCreateUi.IsConnectedOrganizationSupplier("External"));
        Assert.False(PurchaseOrderCreateUi.IsConnectedOrganizationSupplier(null));
    }

    [Fact]
    public void Filter_matches_product_name_case_insensitively()
    {
        var products = SampleProducts();

        var lower = PurchaseOrderCreateUi.FilterEligibleProducts(
            products, p => p.ProductId, p => p.Name, p => p.CategoryId,
            excludedProductIds: new HashSet<Guid>(), editingProductId: null,
            searchText: "chicken", selectedCategoryId: null);

        var upper = PurchaseOrderCreateUi.FilterEligibleProducts(
            products, p => p.ProductId, p => p.Name, p => p.CategoryId,
            excludedProductIds: new HashSet<Guid>(), editingProductId: null,
            searchText: "CHICKEN", selectedCategoryId: null);

        Assert.Single(lower);
        Assert.Equal("Frozen Chicken", lower[0].Name);
        Assert.Equal(lower.Select(p => p.ProductId), upper.Select(p => p.ProductId));
    }

    [Fact]
    public void Filter_category_and_search_work_together()
    {
        var frozenId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var products = SampleProducts();

        var filtered = PurchaseOrderCreateUi.FilterEligibleProducts(
            products, p => p.ProductId, p => p.Name, p => p.CategoryId,
            excludedProductIds: new HashSet<Guid>(), editingProductId: null,
            searchText: "pack", selectedCategoryId: frozenId);

        Assert.Empty(filtered);

        var drinksId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var drinks = PurchaseOrderCreateUi.FilterEligibleProducts(
            products, p => p.ProductId, p => p.Name, p => p.CategoryId,
            excludedProductIds: new HashSet<Guid>(), editingProductId: null,
            searchText: "cola", selectedCategoryId: drinksId);

        Assert.Single(drinks);
        Assert.Equal("Cola Pack", drinks[0].Name);
    }

    [Fact]
    public void Filter_all_shows_eligible_products_and_excludes_added_lines()
    {
        var products = SampleProducts();
        var excluded = new HashSet<Guid> { products[0].ProductId };

        var all = PurchaseOrderCreateUi.FilterEligibleProducts(
            products, p => p.ProductId, p => p.Name, p => p.CategoryId,
            excludedProductIds: excluded, editingProductId: null,
            searchText: null, selectedCategoryId: null);

        Assert.Equal(3, all.Count);
        Assert.DoesNotContain(all, p => p.ProductId == products[0].ProductId);
    }

    [Fact]
    public void Filter_keeps_editing_product_visible_even_when_already_on_lines()
    {
        var products = SampleProducts();
        var editing = products[0].ProductId;

        var filtered = PurchaseOrderCreateUi.FilterEligibleProducts(
            products, p => p.ProductId, p => p.Name, p => p.CategoryId,
            excludedProductIds: new HashSet<Guid> { editing },
            editingProductId: editing,
            searchText: null, selectedCategoryId: null);

        Assert.Contains(filtered, p => p.ProductId == editing);
    }

    [Fact]
    public void Uncategorized_products_are_handled_safely()
    {
        var products = SampleProducts();
        var chips = PurchaseOrderCreateUi.RelevantCategories(
            products.Select(p => (p.CategoryId, p.CategoryId is null ? null : CategoryName(p.CategoryId.Value))),
            uncategorizedLabel: "No category");

        Assert.Contains(chips, c => c.CategoryId == PurchaseOrderCreateUi.UncategorizedFilterId);
        Assert.Equal("No category", chips.Single(c => c.CategoryId == PurchaseOrderCreateUi.UncategorizedFilterId).Name);

        var uncategorized = PurchaseOrderCreateUi.FilterEligibleProducts(
            products, p => p.ProductId, p => p.Name, p => p.CategoryId,
            excludedProductIds: new HashSet<Guid>(), editingProductId: null,
            searchText: null, selectedCategoryId: PurchaseOrderCreateUi.UncategorizedFilterId);

        Assert.Single(uncategorized);
        Assert.Equal("Loose Candy", uncategorized[0].Name);
    }

    [Fact]
    public void Line_and_order_totals_use_money_rounding()
    {
        Assert.Equal(900.00m, PurchaseOrderCreateUi.LineTotal(5m, 180.00m));
        Assert.Equal(200.00m, PurchaseOrderCreateUi.LineTotal(1m, 200.00m));

        var total = PurchaseOrderCreateUi.OrderTotal(
        [
            (5m, 180.00m),
            (1m, 200.00m)
        ]);

        Assert.Equal(1100.00m, total);
    }

    [Fact]
    public void Upsert_replaces_same_line_instead_of_duplicating()
    {
        var productId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        IReadOnlyList<Line> lines =
        [
            new(productId, "Frozen Chicken", 5m, 180.00m)
        ];

        var updated = PurchaseOrderCreateUi.UpsertLine(
            lines,
            l => l.ProductId,
            new Line(productId, "Frozen Chicken", 2m, 190.00m),
            replaceExisting: true);

        Assert.Single(updated);
        Assert.Equal(2m, updated[0].OrderedQty);
        Assert.Equal(190.00m, updated[0].UnitPurchaseCost);
        Assert.Equal(380.00m, PurchaseOrderCreateUi.LineTotal(updated[0].OrderedQty, updated[0].UnitPurchaseCost));
    }

    [Fact]
    public void Upsert_without_replace_does_not_duplicate_existing_product()
    {
        var productId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        IReadOnlyList<Line> lines =
        [
            new(productId, "Frozen Chicken", 5m, 180.00m)
        ];

        var next = PurchaseOrderCreateUi.UpsertLine(
            lines,
            l => l.ProductId,
            new Line(productId, "Frozen Chicken", 1m, 100.00m),
            replaceExisting: false);

        Assert.Same(lines, next);
        Assert.Single(next);
        Assert.Equal(5m, next[0].OrderedQty);
    }

    [Fact]
    public void Delete_removes_correct_line_and_recalculates_total()
    {
        var chicken = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var cola = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        IReadOnlyList<Line> lines =
        [
            new(chicken, "Frozen Chicken", 5m, 180.00m),
            new(cola, "Cola Pack", 1m, 200.00m)
        ];

        var remaining = PurchaseOrderCreateUi.RemoveLine(lines, l => l.ProductId, chicken);

        Assert.Single(remaining);
        Assert.Equal(cola, remaining[0].ProductId);
        Assert.Equal(200.00m, PurchaseOrderCreateUi.OrderTotal(remaining.Select(l => (l.OrderedQty, l.UnitPurchaseCost))));
    }

    [Fact]
    public void Online_reconcile_keeps_all_server_ready_products_when_local_cache_is_partial()
    {
        var server = NineReadyProducts();
        var local = new List<PurchaseOrderCreateUi.LinkedReadyProduct> { server[0] };

        var ready = PurchaseOrderCreateUi.ReconcileOnlineReadyProducts(server, local);

        Assert.Equal(9, ready.Count);
        Assert.Equal(server.Select(p => p.BuyerProductId).OrderBy(id => id), ready.Select(p => p.BuyerProductId).OrderBy(id => id));
    }

    [Fact]
    public void Online_reconcile_does_not_let_partial_local_cache_hide_valid_server_products()
    {
        var server = NineReadyProducts();
        var stale = server[0] with { ProductName = "Cached Apple", LastKnownOrderPrice = 175m };
        var unrelatedLocal = SampleReadyProduct(Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"), "Ghost");

        var ready = PurchaseOrderCreateUi.ReconcileOnlineReadyProducts(server, [stale, unrelatedLocal]);

        Assert.Equal(9, ready.Count);
        Assert.DoesNotContain(ready, p => p.BuyerProductId == unrelatedLocal.BuyerProductId);
        Assert.Equal("Cached Apple", ready.Single(p => p.BuyerProductId == stale.BuyerProductId).ProductName);
        Assert.Equal(80m, ready.Single(p => p.BuyerProductId == stale.BuyerProductId).LastKnownOrderPrice);
    }

    [Fact]
    public void Offline_ready_list_uses_local_cache_only()
    {
        var local = NineReadyProducts().Take(2).ToList();
        local[1] = local[1] with { IsOrderable = false };

        var ready = PurchaseOrderCreateUi.FilterOfflineReadyProducts(local);

        Assert.Single(ready);
        Assert.Equal(local[0].BuyerProductId, ready[0].BuyerProductId);
    }

    [Fact]
    public void Connected_all_category_shows_every_ready_product()
    {
        var ready = NineReadyProducts();

        var filtered = PurchaseOrderCreateUi.FilterConnectedReadyProducts(
            ready,
            p => p.ProductName,
            p => new[] { p.SupplierSku },
            _ => (Guid?)null,
            searchText: null,
            selectedCategoryId: null);

        Assert.Equal(9, filtered.Count);
    }

    [Fact]
    public void Connected_category_and_search_combine_and_keep_added_products_visible()
    {
        var fruits = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var drinks = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var products = new[]
        {
            SampleReadyProduct(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "Apple", "APL-1"),
            SampleReadyProduct(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), "Banana Lakatan", "BAN-1"),
            SampleReadyProduct(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), "Cola Pack", "COLA-1")
        };
        Guid? Category(PurchaseOrderCreateUi.LinkedReadyProduct p) =>
            p.ProductName.Contains("Cola", StringComparison.Ordinal) ? drinks : fruits;

        var chips = PurchaseOrderCreateUi.RelevantCategories(
            products.Select(p => (
                (Guid?)Category(p),
                (string?)(Category(p) == fruits ? "Fruits" : "Beverages"))),
            uncategorizedLabel: "No category");

        Assert.Equal(2, chips.Count);
        Assert.Contains(chips, c => c.Name == "Fruits");
        Assert.Contains(chips, c => c.Name == "Beverages");
        Assert.DoesNotContain(chips, c => c.CategoryId == PurchaseOrderCreateUi.UncategorizedFilterId);

        var filtered = PurchaseOrderCreateUi.FilterConnectedReadyProducts(
            products,
            p => p.ProductName,
            p => new[] { p.SupplierSku },
            Category,
            searchText: "ban",
            selectedCategoryId: fruits);

        Assert.Single(filtered);
        Assert.Equal("Banana Lakatan", filtered[0].ProductName);
    }

    [Fact]
    public void Connected_search_matches_sku_and_uncategorized_uses_existing_sentinel()
    {
        var products = new[]
        {
            SampleReadyProduct(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "Apple", "APL-99"),
            SampleReadyProduct(Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"), "Loose Candy", null)
        };

        var bySku = PurchaseOrderCreateUi.FilterConnectedReadyProducts(
            products,
            p => p.ProductName,
            p => new[] { p.SupplierSku },
            p => p.ProductName == "Loose Candy" ? null : Guid.Parse("11111111-1111-1111-1111-111111111111"),
            searchText: "apl-99",
            selectedCategoryId: null);
        Assert.Single(bySku);
        Assert.Equal("Apple", bySku[0].ProductName);

        var chips = PurchaseOrderCreateUi.RelevantCategories(
            products.Select(p => (
                p.ProductName == "Loose Candy" ? (Guid?)null : Guid.Parse("11111111-1111-1111-1111-111111111111"),
                p.ProductName == "Loose Candy" ? null : "Fruits")),
            uncategorizedLabel: "No category");
        Assert.Contains(chips, c => c.CategoryId == PurchaseOrderCreateUi.UncategorizedFilterId);

        var uncategorized = PurchaseOrderCreateUi.FilterConnectedReadyProducts(
            products,
            p => p.ProductName,
            p => new[] { p.SupplierSku },
            p => p.ProductName == "Loose Candy" ? null : Guid.Parse("11111111-1111-1111-1111-111111111111"),
            searchText: null,
            selectedCategoryId: PurchaseOrderCreateUi.UncategorizedFilterId);
        Assert.Single(uncategorized);
        Assert.Equal("Loose Candy", uncategorized[0].ProductName);
    }

    [Fact]
    public void Connected_upsert_retains_buyer_and_supplier_product_ids_and_po_price()
    {
        var buyerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var supplierProductId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        IReadOnlyList<ConnectedLine> lines = [];

        var added = PurchaseOrderCreateUi.UpsertLine(
            lines,
            l => l.BuyerProductId,
            new ConnectedLine(buyerId, supplierProductId, 10.80m, 3m),
            replaceExisting: false);
        var duplicate = PurchaseOrderCreateUi.UpsertLine(
            added,
            l => l.BuyerProductId,
            new ConnectedLine(buyerId, supplierProductId, 99m, 1m),
            replaceExisting: false);

        Assert.Same(added, duplicate);
        Assert.Single(added);
        Assert.Equal(buyerId, added[0].BuyerProductId);
        Assert.Equal(supplierProductId, added[0].SupplierProductId);
        Assert.Equal(10.80m, added[0].Price);
        Assert.Equal(3m, added[0].Qty);
    }

    [Fact]
    public void Connected_plus_on_unadded_product_creates_qty_one()
    {
        var buyerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var supplierProductId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        IReadOnlyList<ConnectedLine> lines = [];

        var added = Step(lines, buyerId, supplierProductId, 10.80m, 1);

        Assert.Single(added);
        Assert.Equal(1m, added[0].Qty);
        Assert.Equal(buyerId, added[0].BuyerProductId);
        Assert.Equal(supplierProductId, added[0].SupplierProductId);
        Assert.Equal(10.80m, added[0].Price);
    }

    [Fact]
    public void Connected_repeated_plus_increments_same_line_without_duplicate()
    {
        var buyerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var supplierProductId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        IReadOnlyList<ConnectedLine> lines = [];

        var once = Step(lines, buyerId, supplierProductId, 10.80m, 1);
        var twice = Step(once, buyerId, supplierProductId, 99m, 1);
        var thrice = Step(twice, buyerId, supplierProductId, 99m, 1);

        Assert.Single(thrice);
        Assert.Equal(3m, thrice[0].Qty);
        Assert.Equal(10.80m, thrice[0].Price);
        Assert.Equal(supplierProductId, thrice[0].SupplierProductId);
        Assert.Equal(32.40m, PurchaseOrderCreateUi.OrderTotal(thrice.Select(l => (l.Qty, l.Price))));
    }

    [Fact]
    public void Connected_minus_decrements_and_qty_one_removes_line()
    {
        var buyerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var supplierProductId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        IReadOnlyList<ConnectedLine> lines = [];

        var qtyTwo = Step(Step(lines, buyerId, supplierProductId, 10.80m, 1), buyerId, supplierProductId, 10.80m, 1);
        var qtyOne = Step(qtyTwo, buyerId, supplierProductId, 10.80m, -1);
        Assert.Single(qtyOne);
        Assert.Equal(1m, qtyOne[0].Qty);

        var removed = Step(qtyOne, buyerId, supplierProductId, 10.80m, -1);
        Assert.Empty(removed);

        var stillEmpty = Step(removed, buyerId, supplierProductId, 10.80m, -1);
        Assert.Empty(stillEmpty);
    }

    [Fact]
    public void Connected_trash_removes_whole_line_and_recalculates_totals()
    {
        var water = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var soy = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        IReadOnlyList<ConnectedLine> lines =
        [
            new(water, Guid.Parse("11111111-1111-1111-1111-111111111111"), 10.80m, 2m),
            new(soy, Guid.Parse("22222222-2222-2222-2222-222222222222"), 25.20m, 1m)
        ];

        Assert.Equal(2, lines.Count);
        Assert.Equal("Purchasing_DraftSummaryMany", PurchaseOrderCreateUi.DraftProductSummaryKey(lines.Count));
        Assert.Equal(46.80m, PurchaseOrderCreateUi.OrderTotal(lines.Select(l => (l.Qty, l.Price))));

        var afterTrash = PurchaseOrderCreateUi.RemoveLine(lines, l => l.BuyerProductId, water);
        Assert.Single(afterTrash);
        Assert.Equal(soy, afterTrash[0].BuyerProductId);
        Assert.Equal("Purchasing_DraftSummaryOne", PurchaseOrderCreateUi.DraftProductSummaryKey(afterTrash.Count));
        Assert.Equal(25.20m, PurchaseOrderCreateUi.OrderTotal(afterTrash.Select(l => (l.Qty, l.Price))));
        Assert.Empty(PurchaseOrderCreateUi.RemoveLine(afterTrash, l => l.BuyerProductId, soy));
    }

    [Fact]
    public void External_upsert_does_not_auto_increment_or_change_purchase_cost()
    {
        var productId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        IReadOnlyList<Line> lines = [new(productId, "Cola Pack", 1m, 12.50m)];

        var duplicateAdd = PurchaseOrderCreateUi.UpsertLine(
            lines,
            l => l.ProductId,
            new Line(productId, "Cola Pack", 1m, 99m),
            replaceExisting: false);
        Assert.Same(lines, duplicateAdd);
        Assert.Equal(1m, duplicateAdd[0].OrderedQty);
        Assert.Equal(12.50m, duplicateAdd[0].UnitPurchaseCost);

        var edited = PurchaseOrderCreateUi.UpsertLine(
            lines,
            l => l.ProductId,
            new Line(productId, "Cola Pack", 4m, 12.50m),
            replaceExisting: true);
        Assert.Equal(4m, edited[0].OrderedQty);
        Assert.Equal(12.50m, edited[0].UnitPurchaseCost);
        Assert.Equal(50.00m, PurchaseOrderCreateUi.OrderTotal(edited.Select(l => (l.OrderedQty, l.UnitPurchaseCost))));
    }

    [Fact]
    public void Draft_summary_uses_singular_and_plural_product_keys()
    {
        Assert.Equal("Purchasing_DraftSummaryOne", PurchaseOrderCreateUi.DraftProductSummaryKey(1));
        Assert.Equal("Purchasing_DraftSummaryMany", PurchaseOrderCreateUi.DraftProductSummaryKey(3));
        Assert.Equal("Purchasing_DraftSummaryMany", PurchaseOrderCreateUi.DraftProductSummaryKey(0));
    }

    private static List<PurchaseOrderCreateUi.LinkedReadyProduct> NineReadyProducts()
    {
        var products = new List<PurchaseOrderCreateUi.LinkedReadyProduct>(9);
        for (var i = 1; i <= 9; i++)
        {
            var n = i.ToString("D2");
            products.Add(SampleReadyProduct(
                Guid.Parse($"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa{n}"),
                $"Ready {n}",
                $"SKU-{n}"));
        }

        return products;
    }

    private static PurchaseOrderCreateUi.LinkedReadyProduct SampleReadyProduct(
        Guid buyerProductId,
        string name,
        string? sku = null) =>
        new(
            Guid.NewGuid(),
            buyerProductId,
            Guid.NewGuid(),
            name,
            "Kilogram",
            80.00m,
            IsOrderable: true,
            IsActive: true,
            sku);

    private static List<Product> SampleProducts()
    {
        var frozen = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var drinks = Guid.Parse("22222222-2222-2222-2222-222222222222");
        return
        [
            new(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "Frozen Chicken", frozen),
            new(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), "Cola Pack", drinks),
            new(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), "Ice Cubes", frozen),
            new(Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"), "Loose Candy", null)
        ];
    }

    private static string CategoryName(Guid categoryId) =>
        categoryId == Guid.Parse("11111111-1111-1111-1111-111111111111")
            ? "Frozen"
            : "Drinks";

    private static IReadOnlyList<ConnectedLine> Step(
        IReadOnlyList<ConnectedLine> lines,
        Guid buyerId,
        Guid supplierProductId,
        decimal catalogPrice,
        int delta) =>
        PurchaseOrderCreateUi.ApplyConnectedQuantityDelta(
            lines,
            l => l.BuyerProductId,
            l => l.Qty,
            (l, qty) => l with { Qty = qty },
            () => new ConnectedLine(buyerId, supplierProductId, catalogPrice, 1m),
            buyerId,
            delta);
}
