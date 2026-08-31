using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.Application.Access;

public sealed record ProductLocalRolePermissionItemDto(
    string Code,
    string DisplayName,
    bool Allowed);

public sealed record ProductLocalRolePermissionGroupDto(
    string Code,
    string DisplayName,
    IReadOnlyList<ProductLocalRolePermissionItemDto> Items);

public sealed record ProductLocalRoleDefinitionDto(
    string Code,
    string DisplayName,
    string Description,
    int SortOrder,
    bool IsSystemRole,
    bool IsAssignable,
    string MappedPosRoleCode,
    int? ActiveStaffCount,
    IReadOnlyList<ProductLocalRolePermissionGroupDto> PermissionGroups);

/// <summary>
/// Authoritative Pinoy Business POS product-local role catalog for org role management UI.
/// Permission groups mirror PinoyBusinessPOS PosRoleMatrix (P10-WP06).
/// </summary>
public static class PinoyBusinessPosProductLocalRoleCatalog
{
    private sealed record PermissionDescriptor(string Code, string DisplayName, string GroupCode, string GroupName);

    private static readonly PermissionDescriptor[] Permissions =
    [
        new("sell.products", "Sell products", "selling", "Selling"),
        new("sell.cash_checkout", "Cash checkout", "selling", "Selling"),
        new("sell.manual_gcash", "Manual GCash checkout", "selling", "Selling"),
        new("sell.utang", "Create Utang", "selling", "Selling"),
        new("sell.discount", "Apply discount", "selling", "Selling"),
        new("sell.price_override", "Override price", "selling", "Selling"),
        new("sell.price_override_unlimited", "Override unlimited price", "selling", "Selling"),
        new("sell.void", "Void transaction", "selling", "Selling"),
        new("customers.view", "View customers", "customers", "Customers & Utang"),
        new("customers.add", "Add customers", "customers", "Customers & Utang"),
        new("customers.edit", "Edit customers", "customers", "Customers & Utang"),
        new("customers.repayments", "Record repayments", "customers", "Customers & Utang"),
        new("customers.statements", "View statements", "customers", "Customers & Utang"),
        new("customers.write_off", "Write off debt", "customers", "Customers & Utang"),
        new("inventory.view", "View inventory", "inventory", "Inventory"),
        new("inventory.adjust", "Adjust inventory", "inventory", "Inventory"),
        new("inventory.stock_count", "Stock count", "inventory", "Inventory"),
        new("inventory.stock_use", "Stock use", "inventory", "Inventory"),
        new("inventory.waste_loss", "Waste/loss", "inventory", "Inventory"),
        new("inventory.transfer", "Transfer stock", "inventory", "Inventory"),
        new("inventory.production", "Production", "inventory", "Inventory"),
        new("purchasing.view_suppliers", "View suppliers", "purchasing", "Purchasing & suppliers"),
        new("purchasing.manage_suppliers", "Manage suppliers", "purchasing", "Purchasing & suppliers"),
        new("purchasing.view", "View purchasing", "purchasing", "Purchasing & suppliers"),
        new("purchasing.create", "Create purchases", "purchasing", "Purchasing & suppliers"),
        new("purchasing.receive", "Receive purchases", "purchasing", "Purchasing & suppliers"),
        new("purchasing.supplier_payments", "Supplier balances/payments", "purchasing", "Purchasing & suppliers"),
        new("operations.shifts", "Open/close shifts", "operations", "Operations"),
        new("operations.registers", "Registers", "operations", "Operations"),
        new("operations.returns", "Returns", "operations", "Operations"),
        new("operations.customer_orders", "Customer orders", "operations", "Operations"),
        new("operations.expenses", "Expenses", "operations", "Operations"),
        new("operations.branch_fulfillment", "Branch fulfillment", "operations", "Operations"),
        new("reports.dashboard", "Dashboard", "reports", "Reports"),
        new("reports.sales", "Sales reports", "reports", "Reports"),
        new("reports.inventory", "Inventory reports", "reports", "Reports"),
        new("reports.utang", "Utang reports", "reports", "Reports"),
        new("reports.expenses", "Expenses reports", "reports", "Reports"),
        new("reports.export", "Export data", "reports", "Reports"),
        new("settings.branch_fulfillment", "Branch fulfillment settings", "settings", "Settings"),
        new("settings.pos_configuration", "POS configuration", "settings", "Settings"),
    ];

    private static readonly Dictionary<string, HashSet<string>> RolePermissionCodes =
        BuildRolePermissionCodes();

    private static readonly IReadOnlyList<(string Code, string DisplayName, string Description, int Sort)> RoleMeta =
    [
        (
            ProductLocalRoleCodes.Owner,
            ProductRoleDisplay.PosOwner,
            "Full operational access to Pinoy Business POS. This does not transfer ownership of the organization.",
            1),
        (
            ProductLocalRoleCodes.Manager,
            ProductRoleDisplay.StoreManager,
            "Runs day-to-day store operations.",
            2),
        (
            ProductLocalRoleCodes.Cashier,
            ProductRoleDisplay.Cashier,
            "Sells products and handles checkout.",
            3),
        (
            ProductLocalRoleCodes.InventoryStaff,
            ProductRoleDisplay.InventoryStaff,
            "Handles stock, purchasing, and inventory operations.",
            4),
        (
            ProductLocalRoleCodes.ReportingUser,
            ProductRoleDisplay.ReportingUser,
            "Views reports and business information without operational changes.",
            5),
    ];

    public static IReadOnlyList<ProductLocalRoleDefinitionDto> BuildDefinitions(
        IReadOnlyDictionary<string, int>? activeStaffCounts = null)
    {
        return RoleMeta
            .Select(meta =>
            {
                int? count = null;
                if (activeStaffCounts is not null
                    && activeStaffCounts.TryGetValue(meta.Code, out var resolved))
                {
                    count = resolved;
                }

                return new ProductLocalRoleDefinitionDto(
                    meta.Code,
                    meta.DisplayName,
                    meta.Description,
                    meta.Sort,
                    IsSystemRole: true,
                    IsAssignable: ProductLocalRoleCodes.Assignable.Contains(meta.Code, StringComparer.Ordinal),
                    ProductLocalRoleCodes.MapToPosRoleCode(meta.Code),
                    count,
                    BuildPermissionGroups(meta.Code));
            })
            .ToArray();
    }

    public static bool RoleAllowsPermission(string roleCode, string permissionCode)
    {
        var normalized = ProductLocalRoleCodes.NormalizeCatalogCode(roleCode);
        return RolePermissionCodes.TryGetValue(normalized, out var allowed)
            && allowed.Contains(permissionCode, StringComparer.Ordinal);
    }

    private static IReadOnlyList<ProductLocalRolePermissionGroupDto> BuildPermissionGroups(string roleCode)
    {
        var normalized = ProductLocalRoleCodes.NormalizeCatalogCode(roleCode);
        var allowed = RolePermissionCodes[normalized];
        return Permissions
            .GroupBy(p => (p.GroupCode, p.GroupName))
            .OrderBy(g => g.Key.GroupCode, StringComparer.Ordinal)
            .Select(g => new ProductLocalRolePermissionGroupDto(
                g.Key.GroupCode,
                g.Key.GroupName,
                g.Select(p => new ProductLocalRolePermissionItemDto(
                    p.Code,
                    p.DisplayName,
                    allowed.Contains(p.Code, StringComparer.Ordinal))).ToArray()))
            .ToArray();
    }

    private static Dictionary<string, HashSet<string>> BuildRolePermissionCodes()
    {
        var owner = AllPermissionCodes();
        var manager = new HashSet<string>(owner, StringComparer.Ordinal);
        manager.Remove("sell.price_override_unlimited");
        manager.Remove("settings.pos_configuration");
        var cashier = new HashSet<string>(StringComparer.Ordinal)
        {
            "sell.products", "sell.cash_checkout", "sell.manual_gcash", "sell.utang",
            "operations.shifts", "operations.registers", "operations.returns",
        };
        var inventoryStaff = new HashSet<string>(StringComparer.Ordinal)
        {
            "inventory.view", "inventory.adjust", "inventory.stock_count", "inventory.stock_use",
            "inventory.waste_loss", "inventory.transfer", "inventory.production",
            "purchasing.view_suppliers", "purchasing.view", "purchasing.create", "purchasing.receive",
            "purchasing.supplier_payments", "operations.registers",
            "reports.inventory", "reports.sales",
        };
        var reportingUser = new HashSet<string>(StringComparer.Ordinal)
        {
            "reports.dashboard", "reports.sales", "reports.inventory", "reports.utang", "reports.expenses",
            "customers.view", "customers.statements",
            "inventory.view", "purchasing.view_suppliers", "purchasing.view",
            "operations.shifts", "operations.registers", "operations.returns", "operations.customer_orders",
            "operations.expenses",
        };

        return new Dictionary<string, HashSet<string>>(StringComparer.Ordinal)
        {
            [ProductLocalRoleCodes.Owner] = owner,
            [ProductLocalRoleCodes.Manager] = manager,
            [ProductLocalRoleCodes.Cashier] = cashier,
            [ProductLocalRoleCodes.InventoryStaff] = inventoryStaff,
            [ProductLocalRoleCodes.ReportingUser] = reportingUser,
        };
    }

    private static HashSet<string> AllPermissionCodes() =>
        Permissions.Select(p => p.Code).ToHashSet(StringComparer.Ordinal);
}

public sealed class ProductLocalRoleDefinitionQueryService(
    IProductLocalRoleGrantRepository grants)
{
    public async Task<IReadOnlyList<ProductLocalRoleDefinitionDto>> ListPinoyBusinessPosDefinitionsAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var activeGrants = await grants
            .ListByOrganizationAsync(organizationId, ProductLocalRoleGrantStatus.Active, cancellationToken)
            .ConfigureAwait(false);

        var counts = activeGrants
            .Where(g => string.Equals(g.ProductCode, ProductCode.PinoyBusinessPos, StringComparison.OrdinalIgnoreCase))
            .GroupBy(g => ProductLocalRoleCodes.NormalizeCatalogCode(g.RoleCode), StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);

        return PinoyBusinessPosProductLocalRoleCatalog.BuildDefinitions(counts);
    }
}
