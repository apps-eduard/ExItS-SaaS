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

    /// <summary>POS Basic Store: view the operational dashboard (read-only projections).</summary>
    public const string StoreDashboardView = "store-dashboard-view";

    /// <summary>POS Basic Store: view operational reports (read-only projections).</summary>
    public const string StoreReportsView = "store-reports-view";

    /// <summary>POS Full POS: view supplier master data.</summary>
    public const string StoreSuppliersView = "store-suppliers-view";

    /// <summary>POS Full POS: create, edit, activate, and deactivate suppliers.</summary>
    public const string StoreSuppliersManage = "store-suppliers-manage";

    /// <summary>POS Full POS: view cashier shift history and summaries.</summary>
    public const string StoreShiftsView = "store-shifts-view";

    /// <summary>POS Full POS: open, close, cancel shifts and record cash movements.</summary>
    public const string StoreShiftsManage = "store-shifts-manage";

    /// <summary>POS Full POS: view sale return history and refundable lines.</summary>
    public const string StoreReturnsView = "store-returns-view";

    /// <summary>POS Full POS: process sale returns and refunds.</summary>
    public const string StoreReturnsManage = "store-returns-manage";

    /// <summary>POS Full POS: view product-local role assignments and effective permissions.</summary>
    public const string StorePermissionsView = "store-permissions-view";

    /// <summary>POS Full POS: assign and revoke product-local POS roles.</summary>
    public const string StorePermissionsManage = "store-permissions-manage";

    /// <summary>POS Full POS: view organization registers (logical sales stations).</summary>
    public const string StoreRegistersView = "store-registers-view";

    /// <summary>POS Full POS: create, edit, activate, and deactivate organization registers.</summary>
    public const string StoreRegistersManage = "store-registers-manage";

    /// <summary>POS plan commercial limit: maximum branches (QuantityLimit).</summary>
    public const string PlanMaxBranches = "plan-max-branches";

    /// <summary>POS plan commercial limit: maximum active staff (QuantityLimit).</summary>
    public const string PlanMaxActiveStaff = "plan-max-active-staff";

    /// <summary>POS plan commercial limit: maximum active registered POS devices (QuantityLimit).</summary>
    public const string PlanMaxActivePosDevices = "plan-max-active-pos-devices";

    /// <summary>POS plan commercial limit: maximum concurrent effective Business Types (QuantityLimit).</summary>
    public const string PlanMaxActiveBusinessTypes = "plan-max-active-business-types";

    /// <summary>POS plan commercial limit: maximum Active organization Areas (QuantityLimit).</summary>
    public const string PlanMaxAreas = "plan-max-areas";

    /// <summary>POS advanced reporting beyond basic operational reports.</summary>
    public const string StoreAdvancedReports = "store-advanced-reports";

    /// <summary>POS data export capability.</summary>
    public const string StoreExport = "store-export";

    /// <summary>POS customer ordering (pickup/delivery storefront orders).</summary>
    public const string StoreCustomerOrdering = "store-customer-ordering";

    /// <summary>POS delivery-order fulfillment capability (paired with customer ordering).</summary>
    public const string StoreDeliveryOrders = "store-delivery-orders";

    /// <summary>POS Area management (grouping / navigation). Capacity uses plan-max-areas.</summary>
    public const string StoreAreaManagement = "store-area-management";

    /// <summary>POS Warehouse branch type. Warehouse branches still consume plan-max-branches.</summary>
    public const string StoreWarehouse = "store-warehouse";

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
