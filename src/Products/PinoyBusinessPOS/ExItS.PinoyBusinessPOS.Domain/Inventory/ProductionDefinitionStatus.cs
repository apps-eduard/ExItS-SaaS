using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

/// <summary>Lifecycle of a production definition. Member names are stable persistence codes.</summary>
public enum ProductionDefinitionStatus
{
    Active = 0,
    Inactive = 1
}

public static class ProductionDefinitionStatuses
{
    public const int CodeMaxLength = 16;

    public static IReadOnlyList<string> Codes { get; } =
    [
        nameof(ProductionDefinitionStatus.Active),
        nameof(ProductionDefinitionStatus.Inactive)
    ];

    public static string ToCode(ProductionDefinitionStatus status) => status.ToString();

    public static bool TryParse(string? code, out ProductionDefinitionStatus status)
    {
        status = ProductionDefinitionStatus.Active;
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

        status = Enum.Parse<ProductionDefinitionStatus>(match, ignoreCase: false);
        return true;
    }

    public static ProductionDefinitionStatus Parse(string? code)
    {
        if (!TryParse(code, out var status))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductionDefinitionStatus,
                $"Production definition status must be one of: {string.Join(", ", Codes)}.");
        }

        return status;
    }
}
