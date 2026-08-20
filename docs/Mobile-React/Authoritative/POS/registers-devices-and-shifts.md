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

Workspace may bind branch/device context for session; register/shift management and open-shift gate for checkout: **MISSING** / not enforced for disabled checkout.
