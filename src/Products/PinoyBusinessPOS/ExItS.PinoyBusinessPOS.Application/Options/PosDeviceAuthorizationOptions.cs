namespace ExItS.PinoyBusinessPOS.Application.Options;

/// <summary>
/// Controls whether money-affecting POS APIs require a registered installation device
/// (<c>X-Pos-Installation-Device-Id</c> + Platform <c>/pos-devices/authorize</c>).
/// </summary>
/// <remarks>
/// Temporary PWA preview/dev pause: set <see cref="EnforcementEnabled"/> to false only in
/// Local Validation / non-Production so the intermediate React PWA can transact without a
/// Capacitor-wrapped installation identity.
/// Re-enable registered installation enforcement when the React Capacitor native shell becomes
/// the transactional production client:
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
