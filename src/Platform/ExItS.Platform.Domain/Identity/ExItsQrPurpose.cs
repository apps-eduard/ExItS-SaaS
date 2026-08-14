namespace ExItS.Platform.Domain.Identity;

/// <summary>
/// Scoped ExItS QR purpose. Payloads never encode secrets beyond opaque one-time registration tokens.
/// Personal subjects remain PlatformUser-scoped (PublicUserId / PlatformUserId); Organization subjects are OrganizationId-owned.
/// </summary>
public enum ExItsQrPurpose
{
    Personal = 0,
    Organization = 1,
    PosDeviceRegistration = 2
}
