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

    public static bool MatchesSearch(string productName, string? searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return true;
        }

        return productName.Contains(searchText.Trim(), StringComparison.OrdinalIgnoreCase);
    }

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
