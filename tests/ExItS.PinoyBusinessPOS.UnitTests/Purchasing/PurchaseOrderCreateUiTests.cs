using ExItS.PinoyBusinessPOS.Application.Purchasing;

namespace ExItS.PinoyBusinessPOS.UnitTests.Purchasing;

public sealed class PurchaseOrderCreateUiTests
{
    private sealed record Product(Guid ProductId, string Name, Guid? CategoryId);
    private sealed record Line(Guid ProductId, string Name, decimal OrderedQty, decimal UnitPurchaseCost);

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
}
