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

    /// <summary>POS Basic Store: read the product catalog, categories, and SKU/barcode lookup.</summary>
    public const string StoreCatalogView = "store-catalog-view";

    /// <summary>POS Basic Store: create, edit, and change lifecycle of catalog products and categories.</summary>
    public const string StoreCatalogManage = "store-catalog-manage";

    /// <summary>POS Basic Store: read recorded sales history and sale details.</summary>
    public const string StoreSalesView = "store-sales-view";

    /// <summary>POS Basic Store: record a simple retail sale at checkout.</summary>
    public const string StoreSalesCreate = "store-sales-create";

    /// <summary>POS Basic Store: void a recorded sale with a reason.</summary>
    public const string StoreSalesVoid = "store-sales-void";

    /// <summary>POS Basic Store: view on-hand stock, low-stock, and movement history.</summary>
    public const string StoreInventoryView = "store-inventory-view";

    /// <summary>POS Basic Store: enable/disable tracking and record manual stock adjustments.</summary>
    public const string StoreInventoryManage = "store-inventory-manage";

    /// <summary>POS Basic Store: view expense history, categories, and period summaries.</summary>
    public const string StoreExpensesView = "store-expenses-view";

    /// <summary>POS Basic Store: record and void expenses and manage expense categories.</summary>
    public const string StoreExpensesManage = "store-expenses-manage";

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
