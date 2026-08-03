namespace ExItS.Platform.Admin.UnitTests;

public sealed class AdminPaymentsWorkspaceTests
{
    [Fact]
    public void Payments_list_page_uses_workspace_layout()
    {
        var payments = ReadPaymentsPage();
        var css = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "Platform", "ExItS.Platform.Admin", "wwwroot", "app.css"));

        Assert.Contains("<PageHeader", payments, StringComparison.Ordinal);
        Assert.Contains("Title=\"Payments\"", payments, StringComparison.Ordinal);
        Assert.Contains("Review and manage manual SaaS subscription payments.", payments, StringComparison.Ordinal);
        Assert.Contains("Record Payment", payments, StringComparison.Ordinal);
        Assert.Contains("AlertType.Warning", payments, StringComparison.Ordinal);
        Assert.Contains("card numbers, CVV, PIN, OTP, gateway secrets", payments, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("admin-elevated-card", payments, StringComparison.Ordinal);
        Assert.Contains("admin-elevated-card", css, StringComparison.Ordinal);
        Assert.Contains("[data-theme=\"dark\"] .admin-elevated-card", css, StringComparison.Ordinal);
        Assert.Contains("Pending Confirmation", payments, StringComparison.Ordinal);
        Assert.Contains("Rejected / Voided", payments, StringComparison.Ordinal);
        Assert.DoesNotContain("Total this month", payments, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Payment records", payments, StringComparison.Ordinal);
        Assert.Contains("Xs=\"24\"", payments, StringComparison.Ordinal);
        Assert.Contains("Sm=\"12\"", payments, StringComparison.Ordinal);
        Assert.Contains("Lg=\"8\"", payments, StringComparison.Ordinal);
    }

    [Fact]
    public void Payments_create_flow_uses_drawer_with_validation_and_busy_reset()
    {
        var payments = ReadPaymentsPage();

        Assert.Contains("<Drawer", payments, StringComparison.Ordinal);
        Assert.Contains("Visible=\"@_createOpen\"", payments, StringComparison.Ordinal);
        Assert.Contains("OpenCreateDrawer", payments, StringComparison.Ordinal);
        Assert.Contains("CloseCreateDrawer", payments, StringComparison.Ordinal);
        Assert.Contains("Save Payment", payments, StringComparison.Ordinal);
        Assert.Contains("Cancel", payments, StringComparison.Ordinal);
        Assert.Contains("Select organization", payments, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Select product", payments, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("GetOrganizationsAsync", payments, StringComparison.Ordinal);
        Assert.Contains("GetProductsAsync", payments, StringComparison.Ordinal);
        Assert.DoesNotContain("Placeholder=\"GUID\"", payments, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Span=\"14\"", payments, StringComparison.Ordinal);
        Assert.Contains("Span=\"10\"", payments, StringComparison.Ordinal);
        Assert.Contains("ShowTime=\"@true\"", payments, StringComparison.Ordinal);
        Assert.Contains("Local time is converted to UTC", payments, StringComparison.Ordinal);
        Assert.Contains("AdminFormErrorMapper.TryBeginSubmit", payments, StringComparison.Ordinal);
        Assert.Contains("finally", payments, StringComparison.Ordinal);
        Assert.Contains("_busy = false", payments, StringComparison.Ordinal);
        Assert.Contains("Message.SuccessAsync", payments, StringComparison.Ordinal);
        Assert.Contains("Message.ErrorAsync", payments, StringComparison.Ordinal);
        Assert.Contains("CreateManualPaymentAsync", payments, StringComparison.Ordinal);
        Assert.Contains("LoadListAsync()", payments, StringComparison.Ordinal);
        Assert.Contains("LoadSummaryAsync()", payments, StringComparison.Ordinal);
    }

    [Fact]
    public void Payments_filters_and_table_use_supported_fields_without_raw_guid_labels()
    {
        var payments = ReadPaymentsPage();

        Assert.Contains("Today", payments, StringComparison.Ordinal);
        Assert.Contains("7 days", payments, StringComparison.Ordinal);
        Assert.Contains("30 days", payments, StringComparison.Ordinal);
        Assert.Contains("This month", payments, StringComparison.Ordinal);
        Assert.Contains("Placeholder=\"Status\"", payments, StringComparison.Ordinal);
        Assert.Contains("Placeholder=\"Product\"", payments, StringComparison.Ordinal);
        Assert.Contains("Placeholder=\"Organization\"", payments, StringComparison.Ordinal);
        Assert.Contains(">Reset<", payments, StringComparison.Ordinal);
        Assert.Contains(">Refresh<", payments, StringComparison.Ordinal);
        Assert.Contains("ClearFiltersAsync", payments, StringComparison.Ordinal);
        Assert.Contains("OnQuickRangeAsync", payments, StringComparison.Ordinal);
        Assert.Contains("RemoteDataSource", payments, StringComparison.Ordinal);
        Assert.Contains("OnPaymentsTableChangeAsync", payments, StringComparison.Ordinal);
        Assert.Contains("OrgLabel(", payments, StringComparison.Ordinal);
        Assert.Contains("ProductLabel(", payments, StringComparison.Ordinal);
        Assert.Contains("FormatMoney(", payments, StringComparison.Ordinal);
        Assert.Contains("FriendlyDate(", payments, StringComparison.Ordinal);
        Assert.Contains("PaymentStatusColor(", payments, StringComparison.Ordinal);
        Assert.Contains("\"success\"", payments, StringComparison.Ordinal);
        Assert.Contains("\"processing\"", payments, StringComparison.Ordinal);
        Assert.Contains("\"error\"", payments, StringComparison.Ordinal);
        Assert.Contains("Recorded By", payments, StringComparison.Ordinal);
        Assert.Contains("Paid Date", payments, StringComparison.Ordinal);
        Assert.Contains("Label=\"@org.DisplayName\"", payments, StringComparison.Ordinal);
        Assert.DoesNotContain("Title=\"OrganizationId\"", payments, StringComparison.Ordinal);
    }

    [Fact]
    public void Payments_page_preserves_authorization_audit_surface()
    {
        var payments = ReadPaymentsPage();

        Assert.Contains("ManageManualPayments", payments, StringComparison.Ordinal);
        Assert.Contains("UnauthorizedPanel", payments, StringComparison.Ordinal);
        Assert.Contains("ConfirmDialog", payments, StringComparison.Ordinal);
        Assert.Contains("ConfirmPaymentAsync", payments, StringComparison.Ordinal);
        Assert.Contains("RejectPaymentAsync", payments, StringComparison.Ordinal);
        Assert.Contains("VoidPaymentAsync", payments, StringComparison.Ordinal);
        Assert.DoesNotContain("Stripe", payments, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PayPal", payments, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("_cardNumber", payments, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No payment gateway, webhook, QR, invoice engine", payments, StringComparison.OrdinalIgnoreCase);
    }

    private static string ReadPaymentsPage()
    {
        var path = Path.Combine(FindRepositoryRoot(), "src", "Platform", "ExItS.Platform.Admin", "Components", "Pages", "Payments.razor");
        Assert.True(File.Exists(path), path);
        return File.ReadAllText(path);
    }

    private static string FindRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "ExItS.slnx")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate ExItS.slnx.");
    }
}
