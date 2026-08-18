namespace ExItS.PinoyBusinessPOS.Application.Offline;

/// <summary>
/// Metadata-driven connectivity policy for POS. Unknown routes default to OnlineRequired
/// so new destinations fail closed for offline navigation until classified.
/// </summary>
public sealed class PosOfflineCapabilityPolicy : IPosOfflineCapabilityPolicy
{
    private static readonly Dictionary<string, PosConnectivityRequirement> Routes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // Offline capable / session local
            ["/"] = PosConnectivityRequirement.OfflineCapable,
            ["/offline-pin"] = PosConnectivityRequirement.OfflineCapable,
            ["/offline-pin-setup"] = PosConnectivityRequirement.OfflineCapable,
            ["/setup-pin"] = PosConnectivityRequirement.OfflineCapable,
            ["/signin"] = PosConnectivityRequirement.OfflineCapable,
            ["/welcome"] = PosConnectivityRequirement.OfflineCapable,
            ["/onboarding/language"] = PosConnectivityRequirement.OfflineCapable,
            ["/onboarding/theme"] = PosConnectivityRequirement.OfflineCapable,
            ["/onboarding/density"] = PosConnectivityRequirement.OfflineCapable,
            ["/access-denied"] = PosConnectivityRequirement.OfflineCapable,
            ["/not-found"] = PosConnectivityRequirement.OfflineCapable,
            ["/home"] = PosConnectivityRequirement.OfflineCapable,
            ["/owner"] = PosConnectivityRequirement.OfflineCapable,
            ["/manager"] = PosConnectivityRequirement.OfflineCapable,
            ["/cashier"] = PosConnectivityRequirement.OfflineCapable,
            ["/more"] = PosConnectivityRequirement.OfflineCapable,
            ["/settings"] = PosConnectivityRequirement.OfflineCapable,
            ["/settings/support/diagnostics"] = PosConnectivityRequirement.OfflineCapable,
            ["/personal"] = PosConnectivityRequirement.OfflineCapable,
            ["/personal/more"] = PosConnectivityRequirement.OfflineCapable,
            ["/personal/settings"] = PosConnectivityRequirement.OfflineCapable,
            ["/personal/settings/support/diagnostics"] = PosConnectivityRequirement.OfflineCapable,
            ["/personal/profile"] = PosConnectivityRequirement.OfflineCapable,

            // Queueable / local-first Personal Utang (longer prefixes beat blanket /personal/utang)
            ["/personal/utang/people"] = PosConnectivityRequirement.Queueable,
            ["/personal/utang/lent"] = PosConnectivityRequirement.Queueable,
            ["/personal/utang/borrowed"] = PosConnectivityRequirement.Queueable,
            ["/personal/utang/relationships"] = PosConnectivityRequirement.Queueable,

            // Queueable / local-first operational (org POS)
            ["/sales/new"] = PosConnectivityRequirement.Queueable,
            ["/sales/local"] = PosConnectivityRequirement.OfflineCapable,
            ["/customers"] = PosConnectivityRequirement.Queueable,
            ["/customers/new"] = PosConnectivityRequirement.Queueable,
            ["/purchasing/new"] = PosConnectivityRequirement.Queueable,

            // Sales list is reachable offline (history itself is online-only action)
            ["/sales"] = PosConnectivityRequirement.OfflineCapable,

            // Online required destinations
            ["/organization-select"] = PosConnectivityRequirement.OnlineRequired,
            ["/reconnect"] = PosConnectivityRequirement.OnlineRequired,
            ["/register"] = PosConnectivityRequirement.OnlineRequired,
            ["/forgot-password"] = PosConnectivityRequirement.OnlineRequired,
            ["/activate"] = PosConnectivityRequirement.OnlineRequired,
            ["/setup"] = PosConnectivityRequirement.OnlineRequired,
            ["/settings/cash-handling"] = PosConnectivityRequirement.OnlineRequired,
            ["/org"] = PosConnectivityRequirement.OnlineRequired,
            ["/org/profile"] = PosConnectivityRequirement.OnlineRequired,
            ["/org/staff"] = PosConnectivityRequirement.OnlineRequired,
            ["/org/subscription"] = PosConnectivityRequirement.OnlineRequired,
            ["/catalog"] = PosConnectivityRequirement.OnlineRequired,
            ["/catalog/products/new"] = PosConnectivityRequirement.Queueable,
            ["/catalog/import"] = PosConnectivityRequirement.OnlineRequired,
            ["/catalog/todays-prices"] = PosConnectivityRequirement.OnlineRequired,
            ["/catalog/categories"] = PosConnectivityRequirement.OnlineRequired,
            ["/catalog/global"] = PosConnectivityRequirement.OnlineRequired,
            ["/catalog/barcode-lookup"] = PosConnectivityRequirement.OnlineRequired,
            ["/products"] = PosConnectivityRequirement.OnlineRequired,
            ["/inventory"] = PosConnectivityRequirement.OnlineRequired,
            ["/purchasing"] = PosConnectivityRequirement.OnlineRequired,
            ["/expenses"] = PosConnectivityRequirement.OnlineRequired,
            ["/suppliers"] = PosConnectivityRequirement.OnlineRequired,
            ["/registers"] = PosConnectivityRequirement.OnlineRequired,
            ["/shifts"] = PosConnectivityRequirement.OnlineRequired,
            ["/permissions"] = PosConnectivityRequirement.OnlineRequired,
            ["/reports"] = PosConnectivityRequirement.OnlineRequired,
            ["/dashboard"] = PosConnectivityRequirement.OnlineRequired,
            ["/overdue"] = PosConnectivityRequirement.OnlineRequired,
            ["/personal/utang/invitations"] = PosConnectivityRequirement.OnlineRequired,
            ["/personal/customer-link-requests"] = PosConnectivityRequirement.OnlineRequired,
            ["/personal/notifications"] = PosConnectivityRequirement.OnlineRequired,
            ["/org/notifications"] = PosConnectivityRequirement.OnlineRequired,
            ["/org/customer-link-notifications"] = PosConnectivityRequirement.OnlineRequired,
            ["/personal/explore-pos"] = PosConnectivityRequirement.OnlineRequired,
            ["/personal/start-business"] = PosConnectivityRequirement.OnlineRequired,
            ["/start-business"] = PosConnectivityRequirement.OnlineRequired,
            ["/personal/resolve-user"] = PosConnectivityRequirement.OnlineRequired,
            ["/personal/my-qr"] = PosConnectivityRequirement.OnlineRequired,
            ["/personal/invitations/accept"] = PosConnectivityRequirement.OnlineRequired,
            // Remaining /personal/utang/* (if any unclassified) stays online-required via fail-closed
            // unless matched by longer Queueable prefixes above.
            ["/personal/utang"] = PosConnectivityRequirement.OnlineRequired,
        };

    private static readonly Dictionary<string, PosConnectivityRequirement> Actions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [PosOfflineActionKeys.SwitchOrganization] = PosConnectivityRequirement.OnlineRequired,
            [PosOfflineActionKeys.SwitchToPersonal] = PosConnectivityRequirement.OnlineRequired,
            [PosOfflineActionKeys.CatalogImport] = PosConnectivityRequirement.OnlineRequired,
            [PosOfflineActionKeys.CatalogManage] = PosConnectivityRequirement.OnlineRequired,
            [PosOfflineActionKeys.CatalogProductCreate] = PosConnectivityRequirement.Queueable,
            [PosOfflineActionKeys.PermissionsManage] = PosConnectivityRequirement.OnlineRequired,
            [PosOfflineActionKeys.SaleNonCashPayment] = PosConnectivityRequirement.OnlineRequired,
            [PosOfflineActionKeys.ReportsView] = PosConnectivityRequirement.OnlineRequired,
            [PosOfflineActionKeys.InventoryManage] = PosConnectivityRequirement.OnlineRequired,
            [PosOfflineActionKeys.PurchasingManage] = PosConnectivityRequirement.OnlineRequired,
            [PosOfflineActionKeys.ExpensesManage] = PosConnectivityRequirement.OnlineRequired,
            [PosOfflineActionKeys.SuppliersManage] = PosConnectivityRequirement.OnlineRequired,
            [PosOfflineActionKeys.RegistersManage] = PosConnectivityRequirement.OnlineRequired,
            [PosOfflineActionKeys.ShiftsManage] = PosConnectivityRequirement.OnlineRequired,
            [PosOfflineActionKeys.SetupManage] = PosConnectivityRequirement.OnlineRequired,
            [PosOfflineActionKeys.StaffManage] = PosConnectivityRequirement.OnlineRequired,
            [PosOfflineActionKeys.SubscriptionView] = PosConnectivityRequirement.OnlineRequired,
            [PosOfflineActionKeys.CustomerLedger] = PosConnectivityRequirement.OnlineRequired,
            [PosOfflineActionKeys.CustomerStatement] = PosConnectivityRequirement.OnlineRequired,
            [PosOfflineActionKeys.CustomerOverdue] = PosConnectivityRequirement.OnlineRequired,
            [PosOfflineActionKeys.SaleHistory] = PosConnectivityRequirement.OnlineRequired,
            [PosOfflineActionKeys.PersonalInvite] = PosConnectivityRequirement.OnlineRequired,
            [PosOfflineActionKeys.PersonalLinkUser] = PosConnectivityRequirement.OnlineRequired,
            [PosOfflineActionKeys.PersonalStartBusiness] = PosConnectivityRequirement.OnlineRequired,
            [PosOfflineActionKeys.ConnectedSupplierOfferSearch] = PosConnectivityRequirement.OnlineRequired,
            [PosOfflineActionKeys.ConnectedSupplierOrderSubmit] = PosConnectivityRequirement.OnlineRequired,

            // Explicit local/queueable actions (coverage + mixed pages)
            ["sale.checkout.cash"] = PosConnectivityRequirement.Queueable,
            ["customers.create"] = PosConnectivityRequirement.Queueable,
            ["customers.update"] = PosConnectivityRequirement.Queueable,
            ["credit.create"] = PosConnectivityRequirement.Queueable,
            ["repayment.create"] = PosConnectivityRequirement.Queueable,
            ["offline.pin.unlock"] = PosConnectivityRequirement.OfflineCapable,
            ["offline.pin.enroll"] = PosConnectivityRequirement.OfflineCapable,
            [PosOfflineActionKeys.PersonalContactCreate] = PosConnectivityRequirement.Queueable,
            [PosOfflineActionKeys.PersonalLentCreate] = PosConnectivityRequirement.Queueable,
            [PosOfflineActionKeys.PersonalBorrowedCreate] = PosConnectivityRequirement.Queueable,
            [PosOfflineActionKeys.PersonalEntryRecord] = PosConnectivityRequirement.Queueable,
            [PosOfflineActionKeys.ConnectedSupplierLinkedProductsView] = PosConnectivityRequirement.OfflineCapable,
            [PosOfflineActionKeys.ConnectedSupplierDraftSave] = PosConnectivityRequirement.Queueable,
        };

    // Longer prefixes first for nested routes (e.g. /catalog/import before /catalog).
    private static readonly string[] RoutePrefixes = Routes.Keys
        .OrderByDescending(k => k.Length)
        .ToArray();

    public IReadOnlyDictionary<string, PosConnectivityRequirement> ImportantRoutes => Routes;

    public IReadOnlyDictionary<string, PosConnectivityRequirement> ImportantActions => Actions;

    public PosConnectivityRequirement GetRouteRequirement(string relativePath)
    {
        var path = Normalize(relativePath);
        if (Routes.TryGetValue(path, out var exact))
        {
            return exact;
        }

        // Nested OnlineRequired under OfflineCapable/Queueable parents (prefix alone is insufficient).
        if (IsServerSalesHistoryPath(path) || IsOnlineRequiredCustomerSubpath(path))
        {
            return PosConnectivityRequirement.OnlineRequired;
        }

        if (IsConnectedSupplierLinkedProductsPath(path))
        {
            return PosConnectivityRequirement.OfflineCapable;
        }

        foreach (var prefix in RoutePrefixes)
        {
            if (prefix == "/")
            {
                continue;
            }

            if (path.Equals(prefix, StringComparison.OrdinalIgnoreCase)
                || path.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase))
            {
                return Routes[prefix];
            }
        }

        // Fail closed for unclassified operational destinations.
        return PosConnectivityRequirement.OnlineRequired;
    }

    /// <summary>
    /// Server sale detail/receipt under /sales/{id}… — not /sales, /sales/new, or /sales/local…
    /// </summary>
    private static bool IsServerSalesHistoryPath(string path) =>
        path.StartsWith("/sales/", StringComparison.OrdinalIgnoreCase)
        && !path.StartsWith("/sales/new", StringComparison.OrdinalIgnoreCase)
        && !path.StartsWith("/sales/local", StringComparison.OrdinalIgnoreCase);

    /// <summary>Server-only customer reporting subpaths under an otherwise Queueable /customers tree.</summary>
    private static bool IsOnlineRequiredCustomerSubpath(string path) =>
        path.StartsWith("/customers/", StringComparison.OrdinalIgnoreCase)
        && (path.EndsWith("/ledger", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith("/statement", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith("/overdue", StringComparison.OrdinalIgnoreCase));

    private static bool IsConnectedSupplierLinkedProductsPath(string path) =>
        path.StartsWith("/suppliers/", StringComparison.OrdinalIgnoreCase)
        && path.EndsWith("/linked-products", StringComparison.OrdinalIgnoreCase);

    public PosConnectivityRequirement GetActionRequirement(string actionKey)
    {
        if (string.IsNullOrWhiteSpace(actionKey))
        {
            return PosConnectivityRequirement.OnlineRequired;
        }

        return Actions.TryGetValue(actionKey.Trim(), out var requirement)
            ? requirement
            : PosConnectivityRequirement.OnlineRequired;
    }

    public bool RequiresOnlineForRoute(string relativePath) =>
        GetRouteRequirement(relativePath) == PosConnectivityRequirement.OnlineRequired;

    public bool RequiresOnlineForAction(string actionKey) =>
        GetActionRequirement(actionKey) == PosConnectivityRequirement.OnlineRequired;

    public static string Normalize(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return "/";
        }

        var path = relativePath.Split('?', '#')[0].Trim();
        if (!path.StartsWith('/'))
        {
            path = "/" + path;
        }

        if (path.Length > 1)
        {
            path = path.TrimEnd('/');
        }

        return path;
    }
}
