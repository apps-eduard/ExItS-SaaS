using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Onboarding;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Onboarding;

internal static class OrganizationOnboardingProgressEntityMapper
{
    public static OrganizationOnboardingProgress ToDomain(OrganizationOnboardingProgressRecord record) =>
        OrganizationOnboardingProgress.Rehydrate(
            PosOrganizationId.From(record.OrganizationId),
            Enum.Parse<OnboardingStepStatus>(record.OrganizationSetupStatus, ignoreCase: false),
            Enum.Parse<OnboardingStepStatus>(record.BusinessSetupStatus, ignoreCase: false),
            Enum.Parse<OnboardingStepStatus>(record.ProductTemplateStatus, ignoreCase: false),
            Enum.Parse<OnboardingOverallStatus>(record.OverallStatus, ignoreCase: false),
            record.PrimaryBusinessTypeId,
            record.UpdatedAtUtc,
            record.CreatedAtUtc);

    public static OrganizationOnboardingProgressRecord ToRecord(OrganizationOnboardingProgress progress) =>
        new()
        {
            OrganizationId = progress.OrganizationId.Value,
            OrganizationSetupStatus = progress.OrganizationSetupStatus.ToString(),
            BusinessSetupStatus = progress.BusinessSetupStatus.ToString(),
            ProductTemplateStatus = progress.ProductTemplateStatus.ToString(),
            OverallStatus = progress.OverallStatus.ToString(),
            PrimaryBusinessTypeId = progress.PrimaryBusinessTypeId,
            UpdatedAtUtc = progress.UpdatedAtUtc,
            CreatedAtUtc = progress.CreatedAtUtc
        };

    public static void ApplyToRecord(
        OrganizationOnboardingProgress progress,
        OrganizationOnboardingProgressRecord record)
    {
        record.OrganizationSetupStatus = progress.OrganizationSetupStatus.ToString();
        record.BusinessSetupStatus = progress.BusinessSetupStatus.ToString();
        record.ProductTemplateStatus = progress.ProductTemplateStatus.ToString();
        record.OverallStatus = progress.OverallStatus.ToString();
        record.PrimaryBusinessTypeId = progress.PrimaryBusinessTypeId;
        record.UpdatedAtUtc = progress.UpdatedAtUtc;
    }
}
