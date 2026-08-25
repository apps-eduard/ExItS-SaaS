# POS-PWA-OPTIONAL-DEVICE-REGISTRATION-01

**Package:** POS-PWA-OPTIONAL-DEVICE-REGISTRATION-01  
**Branch:** `feat/pos-react-client`  
**Scope:** Pure React PWA — device registration optional; architecture retained for Capacitor

## Pure React PWA — current policy

- Device registration **infrastructure retained** (PosDevice, installationDeviceId, register/authorize/revoke/history/capacity)
- Registration is **optional** for the web PWA
- Money-affecting **transaction device enforcement is paused** when `PosDeviceAuthorization:EnforcementEnabled=false` (Local Validation)
- **Subscription device allowance retained** (`MaxActivePosDevices`) — not redesigned
- Browser identities are **not** treated as reliable physical-device identities
- Explicit **Register this browser** remains available for testing/support and still respects capacity
- Unregistered browsers are **not** auto-registered on login/Sell/shift
- **Cash registers** and **shifts** are separate from authorized devices

## Future Capacitor policy (not implemented here)

- Native installation identity + SecureStorage + native SQLite
- Device registration **required**
- Transaction authorization **required**
- Subscription device capacity **enforced**

## Configuration

| Setting | Value |
|--------|--------|
| Default / Production | `EnforcementEnabled=true` (fail-closed; Production cannot disable) |
| Local Validation PWA | `PosDeviceAuthorization__EnforcementEnabled=false` via `tools/Start-LocalValidation.ps1` |
| Bypass style | Trusted server config only — no client spoof headers/query flags |

## Transaction authorization call sites (current)

Device gate via `IPosDeviceTransactionAuthorizer.EnsureAuthorizedAsync` (no-ops when enforcement disabled):

- `SaleEndpoints` (cash + other sale posts / void-related money paths present there)
- `SaleReturnEndpoints`
- `PaymentAttemptEndpoints` (attempt / cancel / reconcile paths)
- `ExpenseEndpoints` (create / mutate)

### Future Capacitor hardening gaps (not expanded in this package)

- Offline operating grant still requires a device authorization path separate from the pause flag’s money endpoints
- Business Utang / write-off endpoints (if any) without `EnsureAuthorizedAsync` today — document for native hardening; do **not** invent coverage here solely for PWA pause

## Cash registers vs devices (PWA)

- **Device registration** remains optional while `EnforcementEnabled=false` (logic retained for Capacitor).
- **Cash register** is still required by the shift domain. For pure PWA, when none are available,
  Open Shift auto-creates/reuses a cash register named `PWA-0001` (server code remains `REG-NNNNNN`).
- The previous blocking “No register available / Go to registers” UI is **commented in place** in
  `ShiftOpenPage` for easy restore when Capacitor re-enables strict register setup.

## UI policy (PWA)

- Device registration messaging stays in **Authorized devices** (`/org/devices`, `/devices/register`)
- Normal POS screens (Sell, Shift, Registers, Orders, Customers, Inventory, Payments, Utang, Expenses, Returns) must not show register-device banners while `EnforcementEnabled=false`
- Home “Register this browser” CTA is hidden while enforcement is paused; **Authorized devices** remains available
- Unregistered browser on Devices: optional CTA only (form does not auto-open)
- Active registered browser: status + capacity + manage/revoke — no register push CTA

## Explicit exclusions

- No subscription/plan/price/entitlement changes
- No Capacitor / native SQLite
- No automatic browser registration
- No deletion of device architecture
- No merge to main
- `MIGRATION=NONE`
