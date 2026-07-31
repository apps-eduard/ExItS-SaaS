namespace ExItS.PinoyBusinessPOS.Domain.Returns;

/// <summary>Return lifecycle status. MVP supports Completed only.</summary>
public enum SaleReturnStatus
{
    Completed = 0
}

public static class SaleReturnStatuses
{
    public const int CodeMaxLength = 32;

    public static IReadOnlyList<string> Codes { get; } = [nameof(SaleReturnStatus.Completed)];

    public static string ToCode(SaleReturnStatus status) => status.ToString();

    public static SaleReturnStatus Parse(string? code) =>
        string.Equals(code?.Trim(), nameof(SaleReturnStatus.Completed), StringComparison.OrdinalIgnoreCase)
            ? SaleReturnStatus.Completed
            : throw new InvalidOperationException($"Unsupported sale return status: {code}");
}
