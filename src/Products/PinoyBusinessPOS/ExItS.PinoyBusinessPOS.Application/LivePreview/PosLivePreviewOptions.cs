namespace ExItS.PinoyBusinessPOS.Application.LivePreview;

public sealed class PosLivePreviewOptions
{
    public const string SectionName = "LivePreview";

    public bool Enabled { get; set; }

    /// <summary>Platform API base URL used to discover live-preview identity user/org IDs.</summary>
    public string PlatformApiBaseUrl { get; set; } = string.Empty;
}

public sealed record PlatformLivePreviewIdentityDto(
    string Key,
    string Username,
    string DisplayName,
    string Email,
    Guid UserId,
    Guid? OrganizationId,
    string Summary,
    string? PosLocalRoleCode);
