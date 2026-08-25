# Registers, Devices, and Shifts

## Separation (CURRENT)

| Concept | Meaning | Authority | Status |
|---------|---------|-----------|--------|
| **Branch** | Physical/fulfillment location of the organization | Platform | PROVEN_CURRENT |
| **Register** | POS station / till concept — “Not a branch, drawer, device, or cash account” | POS | PROVEN_CURRENT |
| **Device** | Registered POS client device identity | Platform (`PosDevice`) | PROVEN_CURRENT |
| **CashierShift** | Cash authority window; optional `RegisterId` | POS | PROVEN_CURRENT |

## Registers

API: `/api/v1/pos/registers`
Table: `pos.registers`
MAUI: `/registers*`
Offline: OnlineRequired for admin.

## Devices

API: `/api/v1/platform/organizations/{id}/pos-devices/*`
MAUI: `/devices/register`, `/organization/devices`
POS session carries `PosDeviceId` for transaction authorization.
Lost/revoked devices fail closed.

## Shifts

| Topic | Status | Evidence |
|-------|--------|----------|
| Open shift + opening cash | PROVEN_CURRENT | CashierShift use cases/API |
| One-active-shift rules | PROVEN_CURRENT | Domain constraints + tests |
| Cash movements | PROVEN_CURRENT | Shift movements |
| Close + variance | PROVEN_CURRENT | Close shift |
| Denomination assistance | PROVEN_PARTIAL / CURRENT in MAUI UX where present | MAUI shifts UI |
| History / reports | PROVEN_CURRENT | Shift reports |
| Offline PIN / grant | PROVEN_CURRENT | Offline operating grant + PIN pages |

API: `/api/v1/pos/cashier-shifts`

## React

Register list + open/close shift UX and open-shift **checkout readiness gate**: **PROVEN_CURRENT** (RMAP-10). Browser PosDevice registration/authorization: **PROVEN_CURRENT** (RMAP-10b) via durable installation identity, `/org/devices`, `/devices/register`, Platform authorize, and `X-Pos-Installation-Device-Id`. `moneyPostReady` is true only when shift gate is ready **and** the browser device is authorized for the selected branch — **except** when Local Validation / PWA preview sets `PosDeviceAuthorization:EnforcementEnabled=false` (server gate skipped; UX follows runtime policy). Online Cash sale POST: **PROVEN_CURRENT** (RMAP-11).

### PURE REACT PWA — CURRENT POLICY (POS-PWA-OPTIONAL-DEVICE-REGISTRATION-01)

Device registration infrastructure is retained; **registration is optional** for the web PWA. Transaction device enforcement is paused only via trusted server config:

- Local Validation launcher: `PosDeviceAuthorization__EnforcementEnabled=false`
- Default / Production: `EnforcementEnabled=true` (Production startup **fails** if disabled)
- Subscription `MaxActivePosDevices` unchanged — capacity still applies to **explicit** registration
- Unregistered browsers may use POS while enforcement is disabled; browsers are **not** auto-registered
- Cash registers / shifts remain separate operational requirements

See `docs/reports/POS-PWA-OPTIONAL-DEVICE-REGISTRATION-01.md`.

### FUTURE CAPACITOR

Re-enable with `PosDeviceAuthorization__EnforcementEnabled=true` and reuse existing installation identity, SecureStorage, registration, revocation, Platform authorize, and offline grant — do not rebuild the device model. At that stage subscription device capacity becomes strict enforcement again.
