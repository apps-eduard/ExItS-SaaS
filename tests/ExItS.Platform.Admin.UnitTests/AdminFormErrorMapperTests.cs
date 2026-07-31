using System.Net;
using ExItS.Platform.Admin.Forms;
using ExItS.Platform.Admin.Services;

namespace ExItS.Platform.Admin.UnitTests;

public sealed class AdminFormErrorMapperTests
{
    [Fact]
    public void TryBeginSubmit_blocks_duplicate_while_busy()
    {
        var busy = false;
        Assert.True(AdminFormErrorMapper.TryBeginSubmit(ref busy));
        Assert.True(busy);
        Assert.False(AdminFormErrorMapper.TryBeginSubmit(ref busy));
    }

    [Fact]
    public void MapPageError_and_conflict_detection()
    {
        var ok = ApiCallResult<string>.Success("x");
        Assert.Null(AdminFormErrorMapper.MapPageError(ok));

        var conflict = ApiCallResult<string>.Failed(new PlatformApiException(HttpStatusCode.Conflict, "Conflict", "Row changed"));
        Assert.True(AdminFormErrorMapper.IsConflict(conflict.Error));
        Assert.Equal("Row changed", AdminFormErrorMapper.MapPageError(conflict));

        var validation = ApiCallResult<string>.Validation(new PlatformApiException(HttpStatusCode.BadRequest, "Bad", null));
        Assert.False(AdminFormErrorMapper.IsConflict(validation.Error));
        Assert.Equal("Bad", AdminFormErrorMapper.MapPageError(validation));
    }

    [Fact]
    public void Admin_form_dialog_foundation_files_exist()
    {
        var root = FindRepositoryRoot();
        var shared = Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Shared");
        foreach (var name in new[]
                 {
                     "FormSection.razor", "FormField.razor", "FormValidationSummary.razor", "FormActions.razor",
                     "AdminInput.razor", "AdminSelect.razor", "AdminTextArea.razor", "AdminCheck.razor", "ConfirmDialog.razor"
                 })
        {
            Assert.True(File.Exists(Path.Combine(shared, name)), name);
        }

        var confirm = File.ReadAllText(Path.Combine(shared, "ConfirmDialog.razor"));
        Assert.Contains("AdminDialogKind", confirm, StringComparison.Ordinal);
        Assert.Contains("EventCallback", confirm, StringComparison.Ordinal);
        Assert.Contains("FocusAsync", confirm, StringComparison.Ordinal);
        Assert.Contains("Escape", confirm, StringComparison.Ordinal);
        Assert.Contains("exitsAdminA11y.dialogOpen", confirm, StringComparison.Ordinal);
        Assert.Contains("exitsAdminA11y.dialogClose", confirm, StringComparison.Ordinal);

        var users = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Pages", "Users.razor"));
        Assert.Contains("FormValidationSummary", users, StringComparison.Ordinal);
        Assert.Contains("AdminFormErrorMapper.TryBeginSubmit", users, StringComparison.Ordinal);
        Assert.Contains("ConfirmService", users, StringComparison.Ordinal);
        Assert.Contains("IMessageService", users, StringComparison.Ordinal);
        Assert.Contains("<Table", users, StringComparison.Ordinal);
        Assert.DoesNotContain("ToastService", users, StringComparison.Ordinal);
        Assert.DoesNotContain("FormSection", users, StringComparison.Ordinal);

        var members = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Pages", "OrganizationMembers.razor"));
        Assert.Contains("FormSection", members, StringComparison.Ordinal);
        Assert.Contains("AdminDialogKind.Destructive", members, StringComparison.Ordinal);

        var payments = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Pages", "Payments.razor"));
        Assert.Contains("FormSection", payments, StringComparison.Ordinal);
        Assert.Contains("Type=\"number\"", payments, StringComparison.Ordinal);
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
