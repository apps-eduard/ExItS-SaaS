namespace ExItS.Deployment;

/// <summary>P9-WP06 Commercial MVP closeout — honest environment decisions and risk classification.</summary>
public static class CloseoutConstants
{
    public const string PhaseMarker = "P9-WP06-commercial-mvp-closeout";
    public const string ExactNextPhase = "Phase 10 — Full POS";
}

public enum EnvironmentDecisionState
{
    Ready = 1,
    ReadyWithDocumentedNonBlockingRisks = 2,
    Blocked = 3
}

public enum RiskClassification
{
    ReleaseBlocker = 1,
    PilotBlocker = 2,
    AcceptedCommercialLimitation = 3,
    DeferredEnhancement = 4,
    OperationalDependency = 5
}

public enum CloseoutTargetEnvironment
{
    Development = 1,
    TestingCi = 2,
    ControlledInternalTechnicalPilot = 3,
    RestrictedExternalPilot = 4,
    Production = 5
}

public sealed record ClassifiedRisk(
    string Id,
    string Title,
    RiskClassification Classification,
    string OwnerPlaceholder,
    string Mitigation,
    string Evidence,
    string NextAction);

public sealed record EnvironmentReadinessDecision(
    CloseoutTargetEnvironment Environment,
    EnvironmentDecisionState State,
    IReadOnlyList<string> BlockingIds,
    IReadOnlyList<string> Notes);

public sealed record CapabilityInventoryItem(
    string Name,
    string DeliveredStatus,
    string AuthoritativeOwner,
    string DatabaseOwnership,
    string AuthorizationGrant,
    string OnlineOfflineStatus,
    string ImportantLimitation,
    string TestEvidence);

public static class CommercialMvpRiskRegister
{
    public static IReadOnlyList<ClassifiedRisk> Current { get; } =
    [
        new("R-091", "Real production authentication (JWT/MFA/SSO/AD)", RiskClassification.ReleaseBlocker,
            "Platform Security Lead", "Keep Dev/Testing headers unavailable outside approved envs; no fake auth",
            "PlatformAuthz remains Development/Testing; Production blocked by closeout board",
            "Implement approved production identity before external pilot/Production"),
        new("R-109", "Interactive Android device validation", RiskClassification.ReleaseBlocker,
            "POS Client Lead", "Release APK builds; disclose interactive gap",
            "P9-WP04/P9-WP05 retained R-109 without device evidence",
            "Complete TalkBack/install/network/workflow validation on device"),
        new("R-129", "Local SQLite encryption / NU1903 package advisory", RiskClassification.ReleaseBlocker,
            "POS Client Lead", "AES-GCM queue encryption remains; SQLCipher decision pending",
            "NU1903 warnings on SQLitePCLRaw; documented in Phase 9 reports",
            "Approved encryption package decision and remediation"),
        new("TLS-PROD", "Production TLS and certificate validation", RiskClassification.ReleaseBlocker,
            "Platform Ops", "Pilot nginx TLS template only; no Production cert claim",
            "P9-WP05 TLS honesty; Production validation open",
            "Deploy and test real Production certificates end-to-end"),
        new("MAUI-HTTPS", "MAUI HTTPS-only Production network configuration", RiskClassification.ReleaseBlocker,
            "POS Client Lead", "Cleartext limited to localhost/emulator; Production HTTPS-only policy not shipped",
            "network_security_config.xml comments + Android validators",
            "Replace cleartext domains with HTTPS-only Production policy"),
        new("POS-ROLES", "POS operational roles", RiskClassification.ReleaseBlocker,
            "POS Product Owner", "Platform product access ≠ POS operational roles",
            "Documented across Phase 5–9; not implemented",
            "Authorize and implement POS operational role model"),
        new("GCASH-MANUAL", "Manual GCash operator-confirmed and unverified", RiskClassification.AcceptedCommercialLimitation,
            "POS Product Owner", "Label Manual GCash as unverified operator confirmation",
            "Sales domain + reports; no gateway",
            "Optional verified GCash/gateway in later phase"),
        new("ONLINE-ONLY", "Basic Store online-only limitations", RiskClassification.AcceptedCommercialLimitation,
            "POS Product Owner", "Offline limited to supported Utang/customer/repayment queue paths",
            "Phase 7/8 closeouts; catalog/sales/inventory/expenses/reports online-only",
            "Expand offline only under authorized Phase 10+ scope"),
        new("REPORT-EXPORT", "Report file export deferred", RiskClassification.DeferredEnhancement,
            "POS Product Owner", "Read-only in-app projections only",
            "P8-WP06/P8-WP07 exclusions",
            "Authorize export work package if required"),
        new("CATEGORY-LABEL", "Category-label reporting caveat", RiskClassification.AcceptedCommercialLimitation,
            "POS Product Owner", "Document snapshot/label caveats in reports",
            "P8-WP06 documentation",
            "Clarify labeling rules if customers escalate"),
        new("MVP-SCALE", "MVP-scale inventory and performance limits", RiskClassification.AcceptedCommercialLimitation,
            "Platform/POS Ops", "Provisional budgets only — not SLAs",
            "P9-WP02 evidence; CI scaled smoke only",
            "Measure under real pilot load; escalate if budgets breached"),
        new("PITR", "Point-in-time recovery deferred", RiskClassification.DeferredEnhancement,
            "Platform Ops", "Logical dump/restore is MVP path",
            "P9-WP03 explicit deferral",
            "Authorize WAL/PITR when RPO requires it"),
        new("LOCAL-UNSYNCED", "Local unsynced operations excluded from server backup", RiskClassification.AcceptedCommercialLimitation,
            "POS Client Lead", "Disclose device-loss risk; sync before reinstall",
            "P9-WP03 backup boundaries",
            "Operator training; optional future device backup"),
        new("TAX-REFUND-ACCT", "No tax, refund, accounting, supplier, or purchasing workflows", RiskClassification.DeferredEnhancement,
            "Portfolio Owner", "Out of Commercial MVP / Phase 10+ territory",
            "Phase 8–9 exclusions",
            "Authorize Phase 10 Full POS packages")
    ];
}

public static class CommercialMvpReadinessBoard
{
    /// <summary>
    /// Honest closeout board. Does not close R-091/R-109/R-129/TLS.
    /// Restricted external pilot and Production remain Blocked while mandatory release blockers remain.
    /// </summary>
    public static IReadOnlyList<EnvironmentReadinessDecision> Assess(
        bool automatedTestsPassed = true,
        bool androidReleaseBuildSucceeded = true,
        bool internalPilotEntryMet = true,
        bool productionAuthImplemented = false,
        bool interactiveAndroidValidated = false,
        bool localEncryptionResolved = false,
        bool productionTlsValidated = false,
        bool mauiHttpsOnlyProduction = false,
        bool posOperationalRolesImplemented = false)
    {
        var releaseBlockers = new List<string>();
        if (!productionAuthImplemented) releaseBlockers.Add("R-091");
        if (!interactiveAndroidValidated) releaseBlockers.Add("R-109");
        if (!localEncryptionResolved) releaseBlockers.Add("R-129");
        if (!productionTlsValidated) releaseBlockers.Add("TLS-PROD");
        if (!mauiHttpsOnlyProduction) releaseBlockers.Add("MAUI-HTTPS");
        if (!posOperationalRolesImplemented) releaseBlockers.Add("POS-ROLES");

        var pilotBlockers = new List<string>();
        if (!automatedTestsPassed) pilotBlockers.Add("TESTS");
        if (!androidReleaseBuildSucceeded) pilotBlockers.Add("ANDROID-RELEASE");
        if (!internalPilotEntryMet) pilotBlockers.Add("PILOT-ENTRY");

        return
        [
            new EnvironmentReadinessDecision(
                CloseoutTargetEnvironment.Development,
                EnvironmentDecisionState.ReadyWithDocumentedNonBlockingRisks,
                [],
                ["Development/Testing identity headers allowed only here.", "Not a Production claim."]),
            new EnvironmentReadinessDecision(
                CloseoutTargetEnvironment.TestingCi,
                EnvironmentDecisionState.ReadyWithDocumentedNonBlockingRisks,
                [],
                ["CI/Testcontainers disposable evidence.", "Provisional performance budgets are not SLAs."]),
            new EnvironmentReadinessDecision(
                CloseoutTargetEnvironment.ControlledInternalTechnicalPilot,
                pilotBlockers.Count == 0
                    ? EnvironmentDecisionState.ReadyWithDocumentedNonBlockingRisks
                    : EnvironmentDecisionState.Blocked,
                pilotBlockers,
                [
                    "Non-production / internal technical pilot only.",
                    "Release blockers remain disclosed to pilot users.",
                    "No Dev/Testing identity headers on StagingPilot hosts."
                ]),
            new EnvironmentReadinessDecision(
                CloseoutTargetEnvironment.RestrictedExternalPilot,
                EnvironmentDecisionState.Blocked,
                releaseBlockers,
                ["External pilot requires production authentication (R-091) at minimum."]),
            new EnvironmentReadinessDecision(
                CloseoutTargetEnvironment.Production,
                releaseBlockers.Count == 0
                    ? EnvironmentDecisionState.Ready
                    : EnvironmentDecisionState.Blocked,
                releaseBlockers,
                ["Production remains blocked while any mandatory release blocker is open."])
        ];
    }

    public static EnvironmentReadinessDecision For(CloseoutTargetEnvironment environment) =>
        Assess().Single(d => d.Environment == environment);
}

public static class CommercialMvpCapabilityInventory
{
    public static IReadOnlyList<CapabilityInventoryItem> Platform { get; } =
    [
        Item("Organizations", "Platform", "ExItS_Platform", "PlatformAdmin/system roles", "Online", "No product-local roles", "Platform integration/unit tests"),
        Item("Memberships", "Platform", "ExItS_Platform", "Org membership", "Online", "Dev/Testing identity until R-091", "Platform identity/membership tests"),
        Item("Product catalog", "Platform", "ExItS_Platform", "Platform catalog APIs", "Online", "Catalog ≠ POS catalog", "Catalog API tests"),
        Item("Plans", "Platform", "ExItS_Platform", "Platform admin/commercial", "Online", "Per-product plans", "Billing/plan tests"),
        Item("Subscriptions", "Platform", "ExItS_Platform", "Platform commercial", "Online", "Continuity states enforced", "Subscription lifecycle tests"),
        Item("SaaS payments", "Platform", "ExItS_Platform", "Platform commercial", "Online", "Separate from store Cash/GCash/Utang", "Manual payment activation tests"),
        Item("Entitlements", "Platform", "ExItS_Platform", "Platform commercial", "Online", "Authoritative snapshots; delivery deferred", "Entitlement evaluation tests"),
        Item("Product access", "Platform", "ExItS_Platform", "Product-access assignment", "Online", "Does not grant POS operational roles", "Access evaluation tests"),
        Item("Feature grants", "Platform", "ExItS_Platform", "Feature grant codes", "Online", "POS enforces store-* locally", "Commercial gate tests"),
        Item("Commercial-state enforcement", "Platform+POS", "Platform authoritative", "Commercial headers/gates (Dev/Testing)", "Online", "Suspended/missing/stale/unknown fail closed", "POS commercial gate tests"),
        Item("Platform administration", "Platform Admin", "ExItS_Platform via API", "System roles", "Online", "Native CSS Admin; no production auth", "Admin unit tests"),
        Item("Audit and operational safeguards", "Platform", "ExItS_Platform", "PlatformAuthz + audit", "Online", "Append-only audit; Dev actor header limitation", "Audit/authorization tests")
    ];

    public static IReadOnlyList<CapabilityInventoryItem> PinoyBusinessPos { get; } =
    [
        Item("Customer management", "POS", "ExItS_PinoyBusinessPOS", "store-customers-*", "Online + offline queue", "Org isolation; Dev headers", "Customer unit/integration/offline tests"),
        Item("Utang credits and repayments", "POS", "ExItS_PinoyBusinessPOS", "store-credit-*", "Online + offline queue", "Remarks-based credit model", "Credit/repayment tests"),
        Item("Due dates and aging", "POS", "ExItS_PinoyBusinessPOS", "store-credit-*", "Online", "FIFO aging; R-035 calendar caveat", "Due-date/aging tests"),
        Item("Statements and receipts", "POS", "ExItS_PinoyBusinessPOS", "store-statements-*", "Online", "Projection only; no second ledger", "Statement/receipt tests"),
        Item("Catalog, SKU, barcode", "POS", "ExItS_PinoyBusinessPOS", "store-catalog-*", "Online-only", "Not Platform catalog", "Catalog/barcode tests"),
        Item("Cash sales", "POS", "ExItS_PinoyBusinessPOS", "store-sales-*", "Online-only", "Server totals; idempotent", "Sales integration tests"),
        Item("Manually confirmed GCash sales", "POS", "ExItS_PinoyBusinessPOS", "store-sales-*", "Online-only", "Unverified operator confirmation", "Sales Manual GCash tests"),
        Item("Product-Based Utang", "POS", "ExItS_PinoyBusinessPOS", "store-sales-* + store-credit-*", "Online-only", "Atomic sale+credit", "Product-Based Utang tests"),
        Item("Basic inventory", "POS", "ExItS_PinoyBusinessPOS", "store-inventory-*", "Online-only", "Movement-derived on-hand", "Inventory tests"),
        Item("Expenses", "POS", "ExItS_PinoyBusinessPOS", "store-expenses-*", "Online-only", "No effect on Platform billing", "Expense tests"),
        Item("Dashboard and reports", "POS", "ExItS_PinoyBusinessPOS", "store-reports-* / dashboard", "Online-only", "No file export; category-label caveat", "Reporting tests"),
        Item("Offline foundation", "POS MAUI/LocalStore", "Device SQLite (not server backup)", "Protected shell + queue", "Partial offline", "Unsynced local loss risk", "Offline unit tests"),
        Item("Accessibility, localization, themes", "Admin + POS + DesignSystem", "N/A", "UI resources", "Client", "No WCAG certification; R-109 interactive open", "P9-WP04 tests")
    ];

    private static CapabilityInventoryItem Item(
        string name,
        string owner,
        string database,
        string auth,
        string onlineOffline,
        string limitation,
        string evidence) =>
        new(name, "Delivered", owner, database, auth, onlineOffline, limitation, evidence);
}

public static class DatabaseOwnershipBoundaries
{
    public const string PlatformDatabase = "ExItS_Platform";
    public const string PosDatabase = "ExItS_PinoyBusinessPOS";

    public static IReadOnlyList<string> PlatformOwns { get; } =
    [
        "organizations", "users/memberships", "products", "plans",
        "subscriptions", "SaaS payments", "entitlements", "access", "audit"
    ];

    public static IReadOnlyList<string> PosOwns { get; } =
    [
        "customers", "Utang", "catalog", "sales", "inventory",
        "expenses", "idempotency", "operational projections"
    ];

    public static bool ForbidsCrossDatabaseForeignKeys => true;
    public static bool ForbidsForeignProductCoupling => true;
}

public static class Phase9ReconciliationGuard
{
    public static IReadOnlyList<string> RequiredSecurityTokens { get; } =
    [
        "ValidateProductionConfigurationOrThrow",
        "PosProductionSecurityGuard",
        "AddRateLimiter",
        "AllowedHosts",
        "exits_platform_dev_only"
    ];

    public static IReadOnlyList<string> RequiredReliabilityTokens { get; } =
    [
        "/health/ready",
        "AddPosPerformanceIndexes"
    ];

    public static IReadOnlyList<string> RequiredBackupTokens { get; } =
    [
        "P9-WP03-backup-and-restore",
        "DESTROY_AND_RESTORE"
    ];

    public static IReadOnlyList<string> RequiredDeployTokens { get; } =
    [
        "DEPLOY_PILOT_CONFIRMED",
        "NON-PRODUCTION",
        "backup.required"
    ];
}
