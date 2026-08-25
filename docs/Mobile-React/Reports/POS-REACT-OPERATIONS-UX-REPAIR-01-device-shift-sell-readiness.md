# POS-REACT OPERATIONS UX REPAIR 01 — Device → Shift → Sell readiness

**Status:** `POS_OPERATIONS_UX_REPAIR_01=APPROVED` (capacity semantics closed by Review Repair 01)  
**Start SHA:** `5e65fa22c22f21537bf52e164ecf14f8dd995d22` (RMAP-22 Personal Master Run 01 final tip)  
**Implementation commit:** `a2fd04a51e078959ed114c938424f448c89692e4`  
**Branch:** `feat/pos-react-client`

## Locked operational flow

```
DEVICE → SHIFT → SELL
```

| Rule | Result |
|------|--------|
| `NO_DEVICE_NO_SELL` | PASS |
| `NO_SHIFT_NO_SELL` | PASS |
| `CASHIER_DEVICE_ADMIN_EXPOSURE` | NO |
| `OWNER_DEVICE_CAPACITY_VISIBLE` | PASS |
| `MOBILE_FLOATING_CART` | PASS |
| `DESKTOP_STICKY_CART` | PASS |
| `FEW_PRODUCT_STRETCH` | FIXED |
| `NORMAL_CART_READINESS_CLUTTER` | REMOVED |

Client route/readiness UI is **not** security authority. Server continues to deny unauthorized money-posting.

## Delivered

### Pre-Sell readiness gate

- `SellReadinessGate` on `/sell` index: device required → shift required → `SellFloorPage`
- Direct `/sell` navigation obeys the same gate
- States: Device setup required / Open shift required / Ready (floor)
- Cashier: Register this browser only; Owner/Admin may also open POS devices
- After redeem / open shift with `?from=sell`, continue into the next readiness step

### Mid-session readiness loss

- Cart preserved
- Compact `sell-mid-session-warning`
- Pay disabled; no money POST while blocked

### Cashier device registration

- Bound branch shown as locked label (no dropdown)
- No inventory / revoke / create-code on cashier redeem surface

### Owner/Admin POS devices

- Capacity from `getPosDeviceCapacity` (server-authoritative)
- Finite server-authoritative capacity: PASS (`used` / `allowed` / available + progress bar)
- Invented unlimited sentinel: REMOVED (`allowed >= 10000` is still a finite plan max of 10,000)
- Flags: `POS_DEVICE_CAPACITY_SERVER_AUTHORITY=PASS`, `POS_DEVICE_CAPACITY_10000_IS_FINITE=PASS`, `POS_DEVICE_UNLIMITED_CLIENT_SENTINEL=REMOVED`
- Raw installation UUID de-emphasized (copy under details)

### Organization essentials

- Shared `ActionCard` tiles with lucide icons + chevrons
- Groups: Operations / Administration / Workspace
- Responsive 2-column → 3-column grid

### Sell floor layout

- Mobile floating View cart CTA when cart non-empty; hidden when empty or sheet open
- Desktop/tablet sticky landscape cart panel
- Permanent Checkout readiness block removed from cart
- Product grid: `content-start` / `self-start`; few-result cards no longer stretch to viewport height

### i18n

- New keys in en, fil-PH, ceb-PH, ilo-PH, hil-PH
- Native-speaker certification: **PENDING**

## Explicit exclusions

- RMAP-21 Offline, RMAP-B04/B05, RMAP-TAX, RMAP-23/24
- New payment providers / subscription model
- New PosDevice or shift accounting backend architecture
- No backend contract expansion (`POS_OPERATIONS_UX_BACKEND_CONTRACT_GAP` not raised)

## Evidence (gates)

- `format:check` PASS
- `typecheck` PASS
- `lint` PASS (existing warnings only)
- Vitest: 352 passed
- `build` PASS
- Playwright ops UX + sell/shift/device/cart suites PASS
- Critical: RMAP-02, RMAP-03, RMAP-10, RMAP-10b, RMAP-11 subset PASS
- Backend: no code changes; existing PosDevice/shift contracts reused

## Out of scope flags

- `RMAP_21_AUTHORIZED=NO`
- `RMAP_B04_AUTHORIZED=NO`
- `RMAP_B05_AUTHORIZED=NO`
- `RMAP_TAX_AUTHORIZED=NO`
- `PRODUCTION_CUTOVER=NO`
