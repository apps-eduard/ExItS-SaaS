using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

/// <summary>
/// Completeness of MATERIAL_ONLY production cost snapshots.
/// Never invent SellingPrice when costs are unknown.
/// </summary>
public enum ProductionCostStatus
{
    Complete = 0,
    Partial = 1,
    Unavailable = 2
}

public static class ProductionCostStatuses
{
    public const int CodeMaxLength = 16;

    public static IReadOnlyList<string> Codes { get; } =
    [
        nameof(ProductionCostStatus.Complete),
        nameof(ProductionCostStatus.Partial),
        nameof(ProductionCostStatus.Unavailable)
    ];

    public static string ToCode(ProductionCostStatus status) => status.ToString();

    public static bool TryParse(string? code, out ProductionCostStatus status)
    {
        status = ProductionCostStatus.Unavailable;
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var trimmed = code.Trim();
        var match = Codes.FirstOrDefault(c => string.Equals(c, trimmed, StringComparison.OrdinalIgnoreCase));
        if (match is null)
        {
            return false;
        }

        status = Enum.Parse<ProductionCostStatus>(match, ignoreCase: false);
        return true;
    }

    public static ProductionCostStatus Parse(string? code)
    {
        if (!TryParse(code, out var status))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductionCostStatus,
                $"Production cost status must be one of: {string.Join(", ", Codes)}.");
        }

        return status;
    }

    public static ProductionCostStatus FromMaterialCosts(IReadOnlyList<decimal?> unitCosts)
    {
        if (unitCosts.Count == 0)
        {
            return ProductionCostStatus.Unavailable;
        }

        var known = unitCosts.Count(c => c is not null);
        if (known == 0)
        {
            return ProductionCostStatus.Unavailable;
        }

        return known == unitCosts.Count
            ? ProductionCostStatus.Complete
            : ProductionCostStatus.Partial;
    }
}
