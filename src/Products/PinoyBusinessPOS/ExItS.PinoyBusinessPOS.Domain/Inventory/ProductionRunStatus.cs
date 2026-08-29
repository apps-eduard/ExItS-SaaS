using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

/// <summary>Lifecycle of a production run document. Member names are stable persistence codes.</summary>
public enum ProductionRunStatus
{
    Posted = 0,
    Voided = 1
}

public static class ProductionRunStatuses
{
    public const int CodeMaxLength = 16;

    public static IReadOnlyList<string> Codes { get; } =
    [
        nameof(ProductionRunStatus.Posted),
        nameof(ProductionRunStatus.Voided)
    ];

    public static string ToCode(ProductionRunStatus status) => status.ToString();

    public static bool TryParse(string? code, out ProductionRunStatus status)
    {
        status = ProductionRunStatus.Posted;
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

        status = Enum.Parse<ProductionRunStatus>(match, ignoreCase: false);
        return true;
    }

    public static ProductionRunStatus Parse(string? code)
    {
        if (!TryParse(code, out var status))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductionRunStatus,
                $"Production run status must be one of: {string.Join(", ", Codes)}.");
        }

        return status;
    }
}
