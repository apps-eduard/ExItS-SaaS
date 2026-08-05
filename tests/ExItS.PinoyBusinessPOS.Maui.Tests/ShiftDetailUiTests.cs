namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class ShiftDetailUiTests
{
    [Fact]
    public void Detail_removes_content_back_button_and_uses_header_stack()
    {
        var detail = ReadDetail();
        Assert.DoesNotContain("Shifts_BackToList", detail, StringComparison.Ordinal);
        Assert.DoesNotContain("GoList", detail, StringComparison.Ordinal);
        Assert.Contains("HeaderState.EnterInner(\"/shifts\")", detail, StringComparison.Ordinal);
        Assert.Contains("HeaderState.ExitInner()", detail, StringComparison.Ordinal);
        Assert.Contains("IDisposable", detail, StringComparison.Ordinal);
    }

    [Fact]
    public void Detail_presents_bound_register_not_select_register()
    {
        var detail = ReadDetail();
        Assert.Contains("Shifts_Register", detail, StringComparison.Ordinal);
        Assert.DoesNotContain("Shifts_RegisterLabel", detail, StringComparison.Ordinal);
        Assert.DoesNotContain("Select register", detail, StringComparison.Ordinal);
        Assert.Contains("RegisterPrimary", detail, StringComparison.Ordinal);
        Assert.Contains("RegisterName", detail, StringComparison.Ordinal);
        Assert.DoesNotContain("RegisterId.ToString", detail, StringComparison.Ordinal);
    }

    [Fact]
    public void Detail_resolves_cashier_display_names_without_guids()
    {
        var detail = ReadDetail();
        Assert.Contains("IPlatformAccessClient", detail, StringComparison.Ordinal);
        Assert.Contains("GetUserAsync", detail, StringComparison.Ordinal);
        Assert.Contains("OpenedByDisplay", detail, StringComparison.Ordinal);
        Assert.Contains("Shifts_OpenedByFormat", detail, StringComparison.Ordinal);
        Assert.Contains("Shifts_IdentityUnavailable", detail, StringComparison.Ordinal);
        Assert.DoesNotContain("OpenedBy.ToString", detail, StringComparison.Ordinal);
        Assert.DoesNotContain("ClosedBy.ToString", detail, StringComparison.Ordinal);
        Assert.DoesNotContain("RecordedBy.ToString", detail, StringComparison.Ordinal);
        Assert.DoesNotContain("CancelledBy.ToString", detail, StringComparison.Ordinal);
    }

    [Fact]
    public void Cash_summary_shows_opening_sales_movements_and_expected()
    {
        var detail = ReadDetail();
        Assert.Contains("Shifts_CashSummarySection", detail, StringComparison.Ordinal);
        Assert.Contains("Shifts_OpeningCashLabel", detail, StringComparison.Ordinal);
        Assert.Contains("Shifts_NetCashSales", detail, StringComparison.Ordinal);
        Assert.Contains("Shifts_CashIn", detail, StringComparison.Ordinal);
        Assert.Contains("Shifts_CashOut", detail, StringComparison.Ordinal);
        Assert.Contains("Shifts_ExpectedCash", detail, StringComparison.Ordinal);
        Assert.Contains("TotalCashIn", detail, StringComparison.Ordinal);
        Assert.Contains("TotalCashOut", detail, StringComparison.Ordinal);
        Assert.Contains("ExpectedCashAmount", detail, StringComparison.Ordinal);
        Assert.Contains("CurrencyCode=\"PHP\"", detail, StringComparison.Ordinal);
        Assert.Contains("pos-shift-detail__total-row--emphasis", detail, StringComparison.Ordinal);
    }

    [Fact]
    public void Movement_entry_validates_amount_reason_and_blocks_duplicates()
    {
        var detail = ReadDetail();
        Assert.Contains("ValidateMovement", detail, StringComparison.Ordinal);
        Assert.Contains("_movementAmount <= 0m", detail, StringComparison.Ordinal);
        Assert.Contains("Shifts_MovementAmountRequired", detail, StringComparison.Ordinal);
        Assert.Contains("Shifts_MovementReasonRequired", detail, StringComparison.Ordinal);
        Assert.Contains("if (_recording ||", detail, StringComparison.Ordinal);
        Assert.Contains("finally", detail, StringComparison.Ordinal);
        Assert.Contains("_recording = false", detail, StringComparison.Ordinal);
        Assert.Contains("MovementId: Guid.NewGuid()", detail, StringComparison.Ordinal);
        Assert.Contains("_banner = L[\"Shifts_MovementRecorded\"]", detail, StringComparison.Ordinal);
        Assert.Contains("await ReloadAsync()", detail, StringComparison.Ordinal);
    }

    [Fact]
    public void Movement_history_distinguishes_cash_in_and_cash_out()
    {
        var detail = ReadDetail();
        Assert.Contains("Shifts_MovementHistorySection", detail, StringComparison.Ordinal);
        Assert.Contains("Shifts_NoMovementsTitle", detail, StringComparison.Ordinal);
        Assert.Contains("MovementType", detail, StringComparison.Ordinal);
        Assert.Contains("CashIn", detail, StringComparison.Ordinal);
        Assert.Contains("CashOut", detail, StringComparison.Ordinal);
        Assert.Contains("IconGlyphs.Get", detail, StringComparison.Ordinal);
        Assert.Contains("\"lent\"", detail, StringComparison.Ordinal);
        Assert.Contains("\"borrowed\"", detail, StringComparison.Ordinal);
        Assert.Contains("movement.Reason", detail, StringComparison.Ordinal);
        Assert.Contains("RecordedAtUtc", detail, StringComparison.Ordinal);
    }

    [Fact]
    public void Closing_requires_confirmation_and_shows_variance_kinds()
    {
        var detail = ReadDetail();
        Assert.Contains("BeginCloseReview", detail, StringComparison.Ordinal);
        Assert.Contains("ConfirmCloseAsync", detail, StringComparison.Ordinal);
        Assert.Contains("_confirmClose", detail, StringComparison.Ordinal);
        Assert.Contains("Shifts_ReviewAndClose", detail, StringComparison.Ordinal);
        Assert.Contains("Shifts_ConfirmClose", detail, StringComparison.Ordinal);
        Assert.Contains("Shifts_ContinueEditing", detail, StringComparison.Ordinal);
        Assert.Contains("Shifts_CloseConfirmMessage", detail, StringComparison.Ordinal);
        Assert.Contains("variancePreview = countedPreview - expectedCash", detail, StringComparison.Ordinal);
        Assert.Contains("Shifts_VarianceOverage", detail, StringComparison.Ordinal);
        Assert.Contains("Shifts_VarianceShortage", detail, StringComparison.Ordinal);
        Assert.Contains("Shifts_VarianceBalanced", detail, StringComparison.Ordinal);
        Assert.Contains("if (_closing ||", detail, StringComparison.Ordinal);
        Assert.Contains("_closing = false", detail, StringComparison.Ordinal);
    }

    [Fact]
    public void Cancel_is_separated_confirmation_protected_and_gated()
    {
        var detail = ReadDetail();
        Assert.Contains("Shifts_MoreActions", detail, StringComparison.Ordinal);
        Assert.Contains("ConfirmDialog", detail, StringComparison.Ordinal);
        Assert.Contains("Shifts_CancelTitle", detail, StringComparison.Ordinal);
        Assert.Contains("Shifts_CancelMessage", detail, StringComparison.Ordinal);
        Assert.Contains("Danger=\"true\"", detail, StringComparison.Ordinal);
        Assert.Contains("CanOfferCancel", detail, StringComparison.Ordinal);
        Assert.Contains("UtangCapability.ManageShifts", detail, StringComparison.Ordinal);
        Assert.Contains("if (_cancelling ||", detail, StringComparison.Ordinal);
        Assert.Contains("_cancelling = false", detail, StringComparison.Ordinal);
    }

    [Fact]
    public void Closed_and_cancelled_shifts_hide_editable_controls()
    {
        var detail = ReadDetail();
        Assert.Contains("isOpen && canManage", detail, StringComparison.Ordinal);
        Assert.Contains("Shifts_CountedCash", detail, StringComparison.Ordinal);
        Assert.Contains("Shifts_FinalDifference", detail, StringComparison.Ordinal);
        Assert.Contains("ClosedByDisplay", detail, StringComparison.Ordinal);
        Assert.Contains("CancelledByDisplay", detail, StringComparison.Ordinal);
        Assert.Contains("Shifts_ClosedByFormat", detail, StringComparison.Ordinal);
        Assert.Contains("Shifts_CancelledByFormat", detail, StringComparison.Ordinal);
    }

    [Fact]
    public void Offline_and_unauthorized_states_block_server_actions()
    {
        var detail = ReadDetail();
        Assert.Contains("onlineActionsEnabled", detail, StringComparison.Ordinal);
        Assert.Contains("_isOffline", detail, StringComparison.Ordinal);
        Assert.Contains("Shifts_OfflineReadonlyMessage", detail, StringComparison.Ordinal);
        Assert.Contains("Shifts_OfflineActionBlocked", detail, StringComparison.Ordinal);
        Assert.Contains("Shifts_ViewOnlyTitle", detail, StringComparison.Ordinal);
        Assert.Contains("Shifts_ViewOnlyMessage", detail, StringComparison.Ordinal);
        Assert.Contains("ConnectivityChanged", detail, StringComparison.Ordinal);
        Assert.Contains("ErrorState", detail, StringComparison.Ordinal);
        Assert.Contains("Shifts_DetailLoading", detail, StringComparison.Ordinal);
    }

    [Fact]
    public void Localization_covers_shift_detail_keys_in_en_and_fil()
    {
        var en = File.ReadAllText(Path.Combine(FindRepoRoot(),
            "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Maui",
            "Localization", "PosResources.resx"));
        var fil = File.ReadAllText(Path.Combine(FindRepoRoot(),
            "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Maui",
            "Localization", "PosResources.fil-PH.resx"));

        foreach (var key in new[]
                 {
                     "Shifts_CashSummarySection",
                     "Shifts_Register",
                     "Shifts_RecordMovementSection",
                     "Shifts_MovementHistorySection",
                     "Shifts_ReviewAndClose",
                     "Shifts_ConfirmClose",
                     "Shifts_ContinueEditing",
                     "Shifts_CloseConfirmMessage",
                     "Shifts_MoreActions",
                     "Shifts_CancelTitle",
                     "Shifts_CancelMessage",
                     "Shifts_OpenedByFormat",
                     "Shifts_VarianceOverage",
                     "Shifts_VarianceShortage",
                     "Shifts_VarianceBalanced",
                     "Shifts_MovementAmountRequired",
                     "Shifts_MovementReasonRequired",
                     "Shifts_OfflineReadonlyMessage",
                     "Shifts_IdentityUnavailable",
                 })
        {
            Assert.Contains($"name=\"{key}\"", en, StringComparison.Ordinal);
            Assert.Contains($"name=\"{key}\"", fil, StringComparison.Ordinal);
        }

        Assert.Contains("name=\"Shifts_RegisterLabel\"", en, StringComparison.Ordinal);
        Assert.Contains("<value>Select register</value>", en, StringComparison.Ordinal);
        Assert.Contains("<value>Register</value>", en, StringComparison.Ordinal);
    }

    [Fact]
    public void Compact_shift_detail_css_exists()
    {
        var css = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui",
            "wwwroot",
            "app.css"));
        Assert.Contains(".pos-shift-detail", css, StringComparison.Ordinal);
        Assert.Contains(".pos-shift-detail__total-row--emphasis", css, StringComparison.Ordinal);
        Assert.Contains(".pos-shift-detail__movement", css, StringComparison.Ordinal);
        Assert.Contains(".pos-shift-detail__confirm", css, StringComparison.Ordinal);
        Assert.Contains(".pos-shift-detail__more", css, StringComparison.Ordinal);
    }

    private static string ReadDetail() => File.ReadAllText(Path.Combine(
        FindRepoRoot(),
        "src",
        "Products",
        "PinoyBusinessPOS",
        "ExItS.PinoyBusinessPOS.Maui",
        "Components",
        "Pages",
        "Shifts",
        "ShiftDetail.razor"));

    private static string FindRepoRoot()
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

        throw new InvalidOperationException("Repository root not found.");
    }
}
