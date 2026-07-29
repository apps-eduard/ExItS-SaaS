using System.Text.RegularExpressions;
using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.Payments;

/// <summary>
/// Immutable 3-letter ISO 4217 currency code value object. Not a monetary amount, exchange rate,
/// or gateway currency configuration.
/// </summary>
public sealed partial class CurrencyCode : IEquatable<CurrencyCode>
{
    public const string PHP = "PHP";
    public const string USD = "USD";

    private static readonly Regex ValidPattern = CreateValidPattern();

    public string Value { get; }

    private CurrencyCode(string value) => Value = value;

    public static CurrencyCode Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(DomainErrorCodes.PaymentCurrencyInvalid, "CurrencyCode cannot be blank.");
        }

        var normalized = value.Trim().ToUpperInvariant();
        if (!ValidPattern.IsMatch(normalized))
        {
            throw new DomainException(
                DomainErrorCodes.PaymentCurrencyInvalid,
                "CurrencyCode must be a 3-letter ISO 4217 currency code (e.g. PHP, USD).");
        }

        return new CurrencyCode(normalized);
    }

    public bool Equals(CurrencyCode? other) =>
        other is not null && string.Equals(Value, other.Value, StringComparison.Ordinal);

    public override bool Equals(object? obj) => obj is CurrencyCode other && Equals(other);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);

    public override string ToString() => Value;

    public static bool operator ==(CurrencyCode? left, CurrencyCode? right) => Equals(left, right);

    public static bool operator !=(CurrencyCode? left, CurrencyCode? right) => !Equals(left, right);

    [GeneratedRegex("^[A-Z]{3}$", RegexOptions.CultureInvariant)]
    private static partial Regex CreateValidPattern();
}
