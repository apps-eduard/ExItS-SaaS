namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Onboarding;

internal sealed class OrganizationOnboardingProgressRecord
{
    public Guid OrganizationId { get; set; }
    public string OrganizationSetupStatus { get; set; } = string.Empty;
    public string BusinessSetupStatus { get; set; } = string.Empty;
    public string ProductTemplateStatus { get; set; } = string.Empty;
    public string OverallStatus { get; set; } = string.Empty;
    public Guid? PrimaryBusinessTypeId { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
