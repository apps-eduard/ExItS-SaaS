using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Onboarding;

namespace ExItS.PinoyBusinessPOS.UnitTests.Onboarding;

public sealed class OrganizationOnboardingProgressTests
{
    private static readonly Guid OrgId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-08-25T10:00:00Z");

    [Fact]
    public void Create_starts_InProgress_with_NotStarted_steps_and_does_not_backfill()
    {
        var progress = OrganizationOnboardingProgress.Create(PosOrganizationId.From(OrgId), utcNow: Now);

        Assert.Equal(OnboardingOverallStatus.InProgress, progress.OverallStatus);
        Assert.Equal(OnboardingStepStatus.NotStarted, progress.OrganizationSetupStatus);
        Assert.Equal(OnboardingStepStatus.NotStarted, progress.BusinessSetupStatus);
        Assert.Equal(OnboardingStepStatus.NotStarted, progress.ProductTemplateStatus);
        Assert.Null(progress.PrimaryBusinessTypeId);
    }

    [Fact]
    public void Marking_all_steps_keeps_overall_InProgress_until_explicit_complete()
    {
        var progress = OrganizationOnboardingProgress.Create(PosOrganizationId.From(OrgId), utcNow: Now);

        progress.MarkOrganizationSetup(OnboardingStepStatus.Completed, Now.AddMinutes(1));
        progress.MarkBusinessSetup(OnboardingStepStatus.Skipped, Now.AddMinutes(2));
        progress.MarkProductTemplate(OnboardingStepStatus.Completed, Now.AddMinutes(3));

        Assert.Equal(OnboardingOverallStatus.InProgress, progress.OverallStatus);

        progress.MarkCompleted(Now.AddMinutes(4));
        Assert.Equal(OnboardingOverallStatus.Completed, progress.OverallStatus);
    }

    [Fact]
    public void MarkCompleted_requires_all_steps_resolved()
    {
        var progress = OrganizationOnboardingProgress.Create(PosOrganizationId.From(OrgId), utcNow: Now);
        progress.MarkOrganizationSetup(OnboardingStepStatus.Completed, Now.AddMinutes(1));

        var ex = Assert.Throws<DomainException>(() => progress.MarkCompleted(Now.AddMinutes(2)));
        Assert.Equal(DomainErrorCodes.InvalidOnboardingCompletion, ex.ErrorCode);
    }

    [Fact]
    public void MarkFinishedLater_then_EnsureInProgress_resumes()
    {
        var progress = OrganizationOnboardingProgress.Create(PosOrganizationId.From(OrgId), utcNow: Now);
        progress.MarkFinishedLater(Now.AddMinutes(1));
        Assert.Equal(OnboardingOverallStatus.FinishedLater, progress.OverallStatus);

        progress.EnsureInProgress(Now.AddMinutes(2));
        Assert.Equal(OnboardingOverallStatus.InProgress, progress.OverallStatus);
    }

    [Fact]
    public void MarkOrganizationSetup_rejects_NotStarted()
    {
        var progress = OrganizationOnboardingProgress.Create(PosOrganizationId.From(OrgId), utcNow: Now);
        var ex = Assert.Throws<DomainException>(
            () => progress.MarkOrganizationSetup(OnboardingStepStatus.NotStarted, Now.AddMinutes(1)));
        Assert.Equal(DomainErrorCodes.InvalidOnboardingStepStatus, ex.ErrorCode);
    }
}
