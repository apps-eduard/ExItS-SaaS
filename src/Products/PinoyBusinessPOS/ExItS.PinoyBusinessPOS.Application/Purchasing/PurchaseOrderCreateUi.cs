using ExItS.PinoyBusinessPOS.Application.Sales;

namespace ExItS.PinoyBusinessPOS.Application.Purchasing;

/// <summary>
/// Presentation helpers for the MAUI purchase-order create draft.
/// Does not change purchase-order domain or persistence rules.
/// </summary>
public static class PurchaseOrderCreateUi
{
    /// <summary>Sentinel category filter for products with no category.</summary>
    public static readonly Guid UncategorizedFilterId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");

    public static bool HasSupplierSelected(string? supplierId) =>
        Guid.TryParse(supplierId, out var id) && id != Guid.Empty;

    /// <summary>
    /// True when the user is switching to a different supplier while draft lines already exist.
    /// </summary>
    public static bool RequiresSupplierChangeConfirmation(
        string? currentSupplierId,
        string? nextSupplierId,
        int lineCount)
    {
        if (lineCount <= 0)
        {
            return false;
        }

        if (!HasSupplierSelected(currentSupplierId) || !HasSupplierSelected(nextSupplierId))
        {
            return false;
        }

        return !string.Equals(currentSupplierId, nextSupplierId, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsConnectedOrganizationSupplier(string? connectionType) =>
        string.Equals(connectionType, "ConnectedOrganization", StringComparison.OrdinalIgnoreCase);

    public static decimal LineTotal(decimal orderedQuantity, decimal unitPurchaseCost) =>
        PosSaleOptions.RoundMoney(orderedQuantity * unitPurchaseCost);

    public static decimal OrderTotal(IEnumerable<(decimal OrderedQuantity, decimal UnitPurchaseCost)> lines) =>
        lines.Sum(line => LineTotal(line.OrderedQuantity, line.UnitPurchaseCost));

    /// <summary>
    /// One linked/orderable product the Create PO browser can show.
    /// Identity is the buyer's catalog product; supplier product id is retained for connected PO lines.
    /// </summary>
    public sealed record LinkedReadyProduct(
        Guid LinkId,
        Guid BuyerProductId,
        Guid SupplierProductId,
        string ProductName,
        string UnitOfMeasure,
        decimal LastKnownOrderPrice,
        bool IsOrderable,
        bool IsActive,
        string? SupplierSku);

    public static bool MatchesSearch(string productName, string? searchText) =>
        MatchesSearch(productName, searchText, extraTokens: null);

    public static bool MatchesSearch(string productName, string? searchText, IEnumerable<string?>? extraTokens)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return true;
        }

        var query = searchText.Trim();
        if (productName.Contains(query, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (extraTokens is null)
        {
            return false;
        }

        foreach (var token in extraTokens)
        {
            if (!string.IsNullOrWhiteSpace(token)
                && token.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Online Create PO: server active links are authoritative. Local SQLite only enriches
    /// display fields. A partial local cache must not hide valid server products.
    /// When the server list is empty (or unavailable), fall back to the local cache.
    /// </summary>
    public static IReadOnlyList<LinkedReadyProduct> ReconcileOnlineReadyProducts(
        IReadOnlyList<LinkedReadyProduct> serverLinks,
        IReadOnlyList<LinkedReadyProduct> localCache)
    {
        if (serverLinks.Count == 0)
        {
            return localCache
                .Where(IsOrderableReady)
                .OrderBy(p => p.ProductName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        var localByBuyer = localCache
            .GroupBy(p => p.BuyerProductId)
            .ToDictionary(g => g.Key, g => g.First());

        return serverLinks
            .Where(s => s.IsActive && s.BuyerProductId != Guid.Empty && s.LastKnownOrderPrice > 0m)
            .Select(s =>
            {
                if (!localByBuyer.TryGetValue(s.BuyerProductId, out var local))
                {
                    return s with { IsOrderable = true };
                }

                var name = string.IsNullOrWhiteSpace(local.ProductName) ? s.ProductName : local.ProductName;
                var unit = string.IsNullOrWhiteSpace(local.UnitOfMeasure) ? s.UnitOfMeasure : local.UnitOfMeasure;
                var sku = string.IsNullOrWhiteSpace(local.SupplierSku) ? s.SupplierSku : local.SupplierSku;
                var price = s.LastKnownOrderPrice > 0m ? s.LastKnownOrderPrice : local.LastKnownOrderPrice;
                return s with
                {
                    ProductName = name,
                    UnitOfMeasure = unit,
                    SupplierSku = sku,
                    LastKnownOrderPrice = price,
                    IsOrderable = true
                };
            })
            .Where(IsOrderableReady)
            .OrderBy(p => p.ProductName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static IReadOnlyList<LinkedReadyProduct> FilterOfflineReadyProducts(
        IReadOnlyList<LinkedReadyProduct> localCache) =>
        localCache
            .Where(IsOrderableReady)
            .OrderBy(p => p.ProductName, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public static IReadOnlyList<T> FilterConnectedReadyProducts<T>(
        IEnumerable<T> products,
        Func<T, string> name,
        Func<T, IEnumerable<string?>> searchTokens,
        Func<T, Guid?> categoryId,
        string? searchText,
        Guid? selectedCategoryId)
    {
        return products
            .Where(p => MatchesSearch(name(p), searchText, searchTokens(p))
                        && MatchesCategory(categoryId(p), selectedCategoryId))
            .OrderBy(p => name(p), StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static string DraftProductSummaryKey(int count) =>
        count == 1 ? "Purchasing_DraftSummaryOne" : "Purchasing_DraftSummaryMany";

    private static bool IsOrderableReady(LinkedReadyProduct product) =>
        product.IsActive
        && product.IsOrderable
        && product.BuyerProductId != Guid.Empty
        && product.LastKnownOrderPrice > 0m;

    public static bool MatchesCategory(Guid? productCategoryId, Guid? selectedCategoryId)
    {
        if (selectedCategoryId is null)
        {
            return true;
        }

        if (selectedCategoryId == UncategorizedFilterId)
        {
            return productCategoryId is null;
        }

        return productCategoryId == selectedCategoryId;
    }

    public static IReadOnlyList<T> FilterEligibleProducts<T>(
        IEnumerable<T> products,
        Func<T, Guid> productId,
        Func<T, string> name,
        Func<T, Guid?> categoryId,
        IReadOnlySet<Guid> excludedProductIds,
        Guid? editingProductId,
        string? searchText,
        Guid? selectedCategoryId)
    {
        return products
            .Where(p =>
            {
                var id = productId(p);
                if (excludedProductIds.Contains(id) && editingProductId != id)
                {
                    return false;
                }

                return MatchesSearch(name(p), searchText)
                    && MatchesCategory(categoryId(p), selectedCategoryId);
            })
            .OrderBy(p => name(p), StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static IReadOnlyList<(Guid CategoryId, string Name)> RelevantCategories(
        IEnumerable<(Guid? CategoryId, string? CategoryName)> productsWithCategories,
        string uncategorizedLabel)
    {
        var named = productsWithCategories
            .Where(p => p.CategoryId is not null && !string.IsNullOrWhiteSpace(p.CategoryName))
            .GroupBy(p => p.CategoryId!.Value)
            .Select(g => (CategoryId: g.Key, Name: g.First().CategoryName!.Trim()))
            .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var hasUncategorized = productsWithCategories.Any(p => p.CategoryId is null);
        if (hasUncategorized)
        {
            named.Add((UncategorizedFilterId, uncategorizedLabel));
        }

        return named;
    }

    public static IReadOnlyList<TLine> UpsertLine<TLine>(
        IReadOnlyList<TLine> lines,
        Func<TLine, Guid> productId,
        TLine line,
        bool replaceExisting)
    {
        var id = productId(line);
        var next = lines.ToList();
        var index = next.FindIndex(l => productId(l) == id);
        if (index >= 0)
        {
            if (!replaceExisting)
            {
                return lines;
            }

            next[index] = line;
            return next;
        }

        next.Add(line);
        return next;
    }

    public static IReadOnlyList<TLine> RemoveLine<TLine>(
        IReadOnlyList<TLine> lines,
        Func<TLine, Guid> productId,
        Guid removeProductId) =>
        lines.Where(l => productId(l) != removeProductId).ToList();
}
