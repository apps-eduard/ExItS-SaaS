# RMAP-10 — Registers + open shift gate

## Status

**COMPLETE**

## Baseline

starting SHA: `6aa0d48ba617de3b7f6746ae2a3ea54ed2f385e3` (RMAP-08/09 review reconcile)

## Contract review

| Area | Finding |
|------|---------|
| Register | POS station / till — not branch, device, or cash account; list + available-for-shift |
| CashierShift | Open/close + opening cash; current open for actor; org + branch headers |
| Open shift without PosDevice | **Allowed** — browser/PWA may open shift; API does not invent device |
| Checkout readiness | Open shift + register → `shiftGateReady`; Pay remains disabled (no sale POST) |
| Money / sale device | `moneyPostReady` stays false while `PosDevice` is deferred (RMAP-03); RMAP-11 concern |
| Cashier capabilities | ManageShifts + ViewRegisters + ViewOperationalSetup; **not** ManageRegisters / admin / catalog |
| Org / branch isolation | `X-Pos-Organization-Id` + `X-Pos-Branch-Id` on all POS calls; wrong scope fails closed |
| RMAP-09 cart | Unchanged: SellingUnitId / custom qty / ByWeight identity preserved; readiness is additive gate |
| Owner decision | NO |

## Device contract conclusion

**Do not invent PosDevice.** Browser may open and close shifts without a registered installation device. Money and sale authorization remain server-gated until a real device contract exists (RMAP-11+). `DEFERRED_POS_DEVICE_CONTEXT` stays authoritative; `moneyPostReady` is never forced true in this package.

## Implementation

- API clients: `pos-registers-client`, `pos-shifts-client`, `pos-operational-setup-client`
- `ShiftContextProvider` loads `/cashier-shifts/current`, re-reads on tab visibility
- `evaluateCheckoutShiftReadiness` gates sell Pay UI (`shiftGateReady` vs `moneyPostReady`)
- Pages: Registers list (view), Shifts hub, Open shift (register + opening cash), Shift detail (close)
- Routes `/registers`, `/shifts`, `/shifts/open`, `/shifts/:shiftId` with capability guards
- Cashier role home links without elevating to admin experience
- i18n en + fil-PH (shift/register keys; sell readiness copy)

## Exclusions

- Sale POST / checkout payment (RMAP-11)
- Fake PosDevice / money-post bypass
- Register CRUD admin UX (ManageRegisters) — view list only in this package
- Denomination-line cash count UX (policy mode + amount only)
- Offline shift / offline PIN

## Implementation SHA

`356cdfdec7d30d87a4551b10358dacac33cd2f5b` (feat); docs commit on `feat/pos-react-client`

## Validation

### React gates

| Gate | Result |
|------|--------|
| Vitest | 39 files / **164** tests passed (readiness + register/shift clients + cashier ManageShifts) |
| typecheck | PASS |
| lint | PASS (0 errors; existing react-refresh warnings only) |
| format:check | PASS |
| build | PASS |
| Playwright `rmap-10` | **15** passed |

Responsive matrix (shift hub):

| Viewport | Result |
|----------|--------|
| 375×812 | PASS |
| 768×1024 | PASS |
| 1024×768 | PASS |
| 1440×900 | PASS |

### Proven behaviors

- No open shift → checkout readiness `blocked_no_shift`; Pay disabled; CTA to open shift
- Open shift → readiness `ready` (`shiftGateReady`); Pay still disabled (no sale POST / no invented device)
- Closed shift → `blocked_closed`
- Denied shifts → `blocked_denied`
- Wrong branch / wrong organization on open → error, no silent success
- Cashier opens shift without admin/catalog powers; registers view-only
- Org admin without POS sell role → shifts view denied
- Server state re-read when tab becomes visible

### Flags

- `RMAP_10_PASS=YES`
- `RMAP_10_RESPONSIVE_MATRIX_PROVEN=YES`
- `RMAP_10_DEVICE_NOT_INVENTED=YES`
- `RMAP_10_MONEY_POST_DEFERRED=YES`
- `RMAP_10_SALE_POST_EXCLUDED=YES`
- `HARD_STOP=NO`

## Next

RMAP-11 — Checkout / sale (online cash first). Do **not** invent PosDevice; keep money-post gated until a contracted device path exists.
