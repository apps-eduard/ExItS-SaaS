using ExItS.Platform.Application.LocalValidation;
using ExItS.Platform.Domain.Catalog;

namespace ExItS.Platform.UnitTests.LocalValidation;

public sealed class QuickLoginAndStartBusinessContractTests
{
    [Fact]
    public void Starter_mvp_plan_allows_14_day_trial()
    {
        var starter = MvpPosPlanCatalog.Plans.Single(p => p.PlanKey == MvpPosPlanCodes.Starter);
        Assert.True(starter.TrialAllowed);
        Assert.Equal(14, starter.DefaultTrialDays);
    }

    [Fact]
    public void Start_business_defaults_assign_first_pos_owner_role()
    {
        var requestType = typeof(ExItS.Platform.Application.Personal.StartBusinessRequest);
        var prop = requestType.GetProperty("AssignPosOwnerRole");
        Assert.NotNull(prop);
        var ctor = requestType.GetConstructors().Single();
        var param = ctor.GetParameters().Single(p => p.Name == "AssignPosOwnerRole");
        Assert.True((bool)param.DefaultValue!);
    }

    [Fact]
    public void Quick_login_endpoint_and_dto_are_profile_scoped()
    {
        var root = FindRepoRoot();
        var endpoints = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Api", "LocalValidation", "LocalValidationEndpoints.cs"));
        var auth = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Application", "LocalValidation", "LocalValidationAuthUseCases.cs"));
        var signIn = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Admin", "Services", "LocalValidationSignInService.cs"));
        var startBusiness = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Personal.Web", "Components", "Pages", "StartBusiness.razor"));

        Assert.Contains("quick-login-identities", endpoints, StringComparison.Ordinal);
        Assert.Contains("Results.NotFound()", endpoints, StringComparison.Ordinal);
        Assert.Contains("ListLocalValidationQuickLoginIdentities", auth, StringComparison.Ordinal);
        Assert.Contains("Organization Administration", auth, StringComparison.Ordinal);
        Assert.Contains("_users", auth, StringComparison.Ordinal);
        Assert.Contains("ListAsync", auth, StringComparison.Ordinal);
        Assert.Contains("IsCanonicalBaseline", auth, StringComparison.Ordinal);
        Assert.Contains("quick-login-identities", signIn, StringComparison.Ordinal);
        Assert.Contains("account-profiles/select", signIn, StringComparison.Ordinal);
        Assert.Contains("Task.Delay(2500)", startBusiness, StringComparison.Ordinal);
        Assert.Contains("class ListLocalValidationQuickLoginIdentities", auth, StringComparison.Ordinal);
    }

    [Fact]
    public void Personal_start_business_loads_business_types_from_personal_onboarding_endpoint()
    {
        var root = FindRepoRoot();
        var startBusiness = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Personal.Web", "Components", "Pages", "StartBusiness.razor"));
        var client = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Personal.Web", "Services", "PersonalWebSession.cs"));
        var adminForm = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Pages", "PersonalStartBusiness.razor"));

        Assert.Contains("GetOnboardingBusinessTypesAsync", startBusiness, StringComparison.Ordinal);
        Assert.Contains("GetCommercialPlansAsync", startBusiness, StringComparison.Ordinal);
        Assert.Contains("StartBusinessAsync", startBusiness, StringComparison.Ordinal);
        Assert.Contains("/api/v1/personal/onboarding/business-types", client, StringComparison.Ordinal);
        Assert.Contains("/start-business", adminForm, StringComparison.Ordinal);
        Assert.DoesNotContain("Start Free Trial", adminForm, StringComparison.Ordinal);
    }

    [Fact]
    public void Local_validation_shared_password_is_accepted_only_when_enabled()
    {
        var root = FindRepoRoot();
        var login = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Application", "Identity", "SessionUseCases.cs"));
        Assert.Contains("localValidationSharedPassword", login, StringComparison.Ordinal);
        Assert.Contains("_localValidation.Enabled", login, StringComparison.Ordinal);
        Assert.Contains("SharedPassword", login, StringComparison.Ordinal);
    }

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
