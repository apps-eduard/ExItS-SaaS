namespace ExItS.PinoyBusinessPOS.Application.Options;

/// <summary>
/// Controls whether money-affecting POS APIs require a registered installation device
/// (<c>X-Pos-Installation-Device-Id</c> + Platform <c>/pos-devices/authorize</c>).
/// </summary>
/// <remarks>
/// Web/PWA sets <see cref="EnforcementEnabled"/> to false. Device registration is disabled
/// on the website. User/org/capability/business rules still apply.
/// Capacitor Android and iOS apps set
/// <c>PosDeviceAuthorization__EnforcementEnabled=true</c>
/// (reuse DeviceIdentityProvider, installation GUID, registration, revocation, and Platform authorize).
/// Production startup fails closed if this is disabled.
/// </remarks>
public sealed class PosDeviceAuthorizationOptions
{
    public const string SectionName = "PosDeviceAuthorization";

    /// <summary>
    /// When true (default), money-affecting APIs require an active authorized POS installation.
    /// When false, the website skips device registration.
    /// User/org/capability/business rules still apply.
    /// </summary>
    public bool EnforcementEnabled { get; set; } = true;
}
