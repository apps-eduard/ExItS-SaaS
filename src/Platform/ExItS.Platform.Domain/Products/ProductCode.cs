using System.Text.RegularExpressions;
using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.Products;

/// <summary>
/// Stable machine-safe product code. Not a catalog entry, plan, subscription, or entitlement.
/// </summary>
public sealed partial class ProductCode : IEquatable<ProductCode>
{
    public const string PinoyBusinessPos = "pinoy-business-pos";
    public const string PinoyLoanManager = "pinoy-loan-manager";
    public const string PinoyBuyNowPayLater = "pinoy-buy-now-pay-later";
    public const string PinoyPawnManager = "pinoy-pawn-manager";

    private static readonly Regex ValidPattern = CreateValidPattern();

    public string Value { get; }

    private ProductCode(string value) => Value = value;

    public static ProductCode Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductCode,
                "ProductCode cannot be blank.");
        }

        var normalized = value.Trim().ToLowerInvariant();
        if (!ValidPattern.IsMatch(normalized))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductCode,
                "ProductCode must be lowercase alphanumeric segments separated by single hyphens.");
        }

        return new ProductCode(normalized);
    }

    public bool Equals(ProductCode? other) =>
        other is not null && string.Equals(Value, other.Value, StringComparison.Ordinal);

    public override bool Equals(object? obj) =>
        obj is ProductCode other && Equals(other);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);

    public override string ToString() => Value;

    public static bool operator ==(ProductCode? left, ProductCode? right) =>
        Equals(left, right);

    public static bool operator !=(ProductCode? left, ProductCode? right) =>
        !Equals(left, right);

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex CreateValidPattern();
}
