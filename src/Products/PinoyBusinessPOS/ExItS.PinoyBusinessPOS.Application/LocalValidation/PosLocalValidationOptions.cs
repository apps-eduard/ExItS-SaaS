namespace ExItS.PinoyBusinessPOS.Application.LocalValidation;

public sealed class PosLocalValidationOptions
{
    public const string SectionName = "LocalValidation";

    public bool Enabled { get; set; }

    /// <summary>Platform API base URL used to discover local-validation identity user/org IDs.</summary>
    public string PlatformApiBaseUrl { get; set; } = string.Empty;
}

public sealed record PlatformLocalValidationIdentityDto(
    string Key,
    string Username,
    string DisplayName,
    string Email,
    Guid UserId,
    Guid? OrganizationId,
    string Summary,
    string? PosLocalRoleCode);
