namespace ExItS.PinoyBusinessPOS.Application.Options;

/// <summary>
/// Controls whether money-affecting POS APIs require a registered installation device
/// (<c>X-Pos-Installation-Device-Id</c> + Platform <c>/pos-devices/authorize</c>).
/// </summary>
/// <remarks>
/// Pure React PWA current policy: set <see cref="EnforcementEnabled"/> to false only in
/// Local Validation / non-Production so the web PWA can operate without requiring browser
/// registration. Device registration endpoints, capacity, history, and revoke remain available
/// for optional/manual use and for future Capacitor.
/// Future Capacitor / native transactional client should set
/// <c>PosDeviceAuthorization__EnforcementEnabled=true</c>
/// (reuse DeviceIdentityProvider, installation GUID, registration, revocation, and Platform authorize).
/// Production startup fails closed if this is disabled.
/// </remarks>
public sealed class PosDeviceAuthorizationOptions
{
    public const string SectionName = "PosDeviceAuthorization";

    /// <summary>
    /// When true (default), money-affecting APIs require an active authorized POS installation.
    /// When false, the device gate is skipped; user/org/capability/business rules still apply.
    /// </summary>
    public bool EnforcementEnabled { get; set; } = true;
}
