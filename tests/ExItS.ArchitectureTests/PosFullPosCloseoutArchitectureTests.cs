namespace ExItS.ArchitectureTests;

/// <summary>
/// P10-WP08 closeout: Full POS boundary and deferred-scope guards.
/// </summary>
public sealed class PosFullPosCloseoutArchitectureTests
{
    private static readonly string[] ForbiddenDeferredConcepts =
    [
        "WarehouseTransfer",
        "CostOfGoodsSoldCalculator",
        "ProfitAndLossStatement",
        "TaxInvoiceIssuer",
        "PaymentGatewayClient",
        "GCashVerificationClient",
        "AccountsPayableService",
        "SupplierPaymentService",
        "RegisterCashBalance",
        "BranchInventoryTransfer",
        "OfflinePurchaseOrderQueue",
        "OfflineStockCountQueue",
        "OfflineReturnQueue",
        "OfflineRoleAssignmentQueue",
        "StoreReportsExport"
    ];

    [Fact]
    public void Pos_phase_marker_is_phase_10_closeout()
    {
        var program = File.ReadAllText(Path.Combine(
            PosProject("ExItS.PinoyBusinessPOS.Api"), "Program.cs"));
        Assert.Contains("P10-WP08-phase-10-closeout", program, StringComparison.Ordinal);
        Assert.DoesNotContain(".Migrate(", program, StringComparison.Ordinal);
        Assert.DoesNotContain(".MigrateAsync(", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Full_pos_surfaces_exist_without_forbidden_deferred_concepts()
    {
        foreach (var root in new[]
                 {
                     Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Application")),
                     Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Api")),
                     Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Domain"))
                 })
        {
            foreach (var file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                var text = File.ReadAllText(file);
                foreach (var concept in ForbiddenDeferredConcepts)
                {
                    Assert.DoesNotContain(concept, text, StringComparison.OrdinalIgnoreCase);
                }
            }
        }
    }

    [Fact]
    public void Pos_dbcontext_owns_phase_10_tables_and_excludes_accounting_and_phi()
    {
        var context = File.ReadAllText(Path.Combine(
            PosProject("ExItS.PinoyBusinessPOS.Infrastructure"), "Persistence", "PosDbContext.cs"));

        foreach (var table in new[]
                 {
                     "\"suppliers\"", "\"purchase_orders\"", "\"goods_receipts\"",
                     "\"stock_counts\"", "\"cashier_shifts\"", "\"sale_returns\"",
                     "\"pos_role_assignments\"", "\"registers\""
                 })
        {
            Assert.Contains(table, context, StringComparison.Ordinal);
        }

        foreach (var table in new[]
                 {
                     "\"warehouses\"", "\"branches\"", "\"cash_drawers\"",
                     "\"accounts_payable\"", "\"supplier_payments\"",
                     "\"general_ledger\"", "\"journal_entries\"", "\"tax_invoices\"",
                     "\"patients\"", "\"phi_records\""
                 })
        {
            Assert.DoesNotContain(table, context, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Store_feature_codes_include_phase_10_grants_without_export_alias()
    {
        var policy = File.ReadAllText(Path.Combine(
            PosProject("ExItS.PinoyBusinessPOS.Application"),
            "Commercial", "UtangCapabilityPolicy.cs"));

        foreach (var code in new[]
                 {
                     "store-suppliers-view", "store-suppliers-manage",
                     "store-purchasing-view", "store-purchasing-manage",
                     "store-shifts-view", "store-shifts-manage",
                     "store-returns-view", "store-returns-manage",
                     "store-permissions-view", "store-permissions-manage",
                     "store-registers-view", "store-registers-manage"
                 })
        {
            Assert.Contains(code, policy, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("store-reports-export", policy, StringComparison.Ordinal);
    }

    private static string PosProject(string name) =>
        Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            "src", "Products", "PinoyBusinessPOS", name));
}
