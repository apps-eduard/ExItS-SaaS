using System.Text.RegularExpressions;
using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.Catalog;

/// <summary>Stable machine-safe commercial feature code. Labels stay outside this value object.</summary>
public sealed partial class FeatureCode : IEquatable<FeatureCode>
{
    /// <summary>POS Utang: view existing balances and historical credit records.</summary>
    public const string CustomerCreditView = "customer-credit-view";

    /// <summary>POS Utang: receive Cash/GCash repayment on existing debt.</summary>
    public const string CustomerCreditRepay = "customer-credit-repay";

    /// <summary>POS Utang: create new credit / increase debt (blocked after trial expiry).</summary>
    public const string CustomerCreditCreate = "customer-credit-create";

    private static readonly Regex ValidPattern = CreateValidPattern();

    public string Value { get; }

    private FeatureCode(string value) => Value = value;

    public static FeatureCode Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(DomainErrorCodes.InvalidFeatureCode, "FeatureCode cannot be blank.");
        }

        var normalized = value.Trim().ToLowerInvariant();
        if (!ValidPattern.IsMatch(normalized) || normalized.Length > 64)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidFeatureCode,
                "FeatureCode must be 1–64 lowercase alphanumeric segments separated by single hyphens.");
        }

        return new FeatureCode(normalized);
    }

    public bool Equals(FeatureCode? other) =>
        other is not null && string.Equals(Value, other.Value, StringComparison.Ordinal);

    public override bool Equals(object? obj) => obj is FeatureCode other && Equals(other);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
    public override string ToString() => Value;
    public static bool operator ==(FeatureCode? left, FeatureCode? right) => Equals(left, right);
    public static bool operator !=(FeatureCode? left, FeatureCode? right) => !Equals(left, right);

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex CreateValidPattern();
}
