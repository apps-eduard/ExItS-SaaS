namespace ExItS.Platform.Infrastructure.Persistence.Organizations;

internal sealed class PosDeviceRegistrationTokenRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset? RedeemedAtUtc { get; set; }
    public string? RedeemedByInstallationDeviceId { get; set; }
    public Guid? RedeemedPosDeviceId { get; set; }
    public string Status { get; set; } = string.Empty;
}
