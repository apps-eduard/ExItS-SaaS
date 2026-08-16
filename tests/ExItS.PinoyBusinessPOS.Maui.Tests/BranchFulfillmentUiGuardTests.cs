namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class BranchFulfillmentUiGuardTests
{
    [Fact]
    public void Maui_branch_management_is_mobile_card_based()
    {
        var maui = MauiProject();
        var list = File.ReadAllText(Path.Combine(maui, "Components", "Pages", "Organization", "Branches.razor"));
        var edit = File.ReadAllText(Path.Combine(maui, "Components", "Pages", "Organization", "BranchEdit.razor"));
        Assert.Contains("@page \"/organization/branches\"", list, StringComparison.Ordinal);
        Assert.Contains("pos-branches__card", list, StringComparison.Ordinal);
        Assert.DoesNotContain("<table", list, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("@page \"/organization/branches/{BranchId:guid}\"", edit, StringComparison.Ordinal);
        Assert.Contains("PickupEnabled", edit, StringComparison.Ordinal);
        Assert.Contains("DeliveryEnabled", edit, StringComparison.Ordinal);
        Assert.Contains("UpsertBranchDeliveryPolicyAsync", edit, StringComparison.Ordinal);
    }

    [Fact]
    public void Branch_fulfillment_strings_exist_in_en_and_fil()
    {
        var localization = Path.Combine(MauiProject(), "Localization");
        var en = File.ReadAllText(Path.Combine(localization, "PosResources.resx"));
        var fil = File.ReadAllText(Path.Combine(localization, "PosResources.fil-PH.resx"));
        foreach (var key in new[]
                 {
                     "Branches_Title",
                     "Branches_Pickup",
                     "Branches_Delivery",
                     "Branches_DeliverySettings",
                     "Branches_MinimumOrder",
                     "Branches_BaseFee",
                     "Branches_IncludedKm",
                     "Branches_AdditionalPerKm",
                     "Branches_MaximumKm"
                 })
        {
            Assert.Contains($"<data name=\"{key}\"", en, StringComparison.Ordinal);
            Assert.Contains($"<data name=\"{key}\"", fil, StringComparison.Ordinal);
        }
    }

    private static string MauiProject()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Maui");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("MAUI project not found.");
    }
}
