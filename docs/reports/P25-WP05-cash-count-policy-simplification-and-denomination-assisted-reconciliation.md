# P25-WP05 — Cash Count Policy Simplification and Denomination-Assisted Reconciliation

## 1. Assignment

| Field | Value |
|---|---|
| Phase | 25 |
| Work package | P25-WP05 Cash Count Policy Simplification and Denomination-Assisted Reconciliation |
| Status | Code Complete / Ready for Owner Validation |
| Branch | `main` |
| Date | 2026-08-13 |
| Starting SHA | `147b94e4d0363354a80b8c31a9f353ae1299bb80` |
| Implementation SHA | `cbcdb8a9` |
| Test SHA | `8869a179` |
| Docs SHA | `528de183` |
| Denomination default refinement | `a50413bc` |
| Device Verified | **No** |
| Browser Verified | **No** |
| Production Ready | **No** |
| Database migration | **Yes** — `20260813153741_AddPosCashDenominationsAndRequiredDefault` |

## 2. Delivered capability

- Configurable Cash Count Policy is **Required** (default for new organizations) or **Optional**.
- `Off` is not selectable in Org Web or MAUI. API rejects new Off writes (`CashCountModeOffRetired`).
- Leftover org `Off` rows migrate to **Optional**. Historical shift snapshots may still store Off.
- Only `ManageOperationalSetup` (owner/admin) may change policy or denominations. Server-enforced. Cashiers count with `ManageShifts` but cannot change policy. Personal/cross-org rejected.
- Same setting from Organization Web Settings → Cash handling and MAUI **Settings → Cash handling** (owner/admin). Operational setup no longer hosts cash-count policy UI.
- `EffectiveCashCountMode` remains snapshotted at shift open.
- Optional denomination helper on MAUI open/close. Authoritative totals remain `OpeningCashAmount` / `ClosingCashAmount`. Server recalculates `sum(value * qty)` and rejects mismatch.
- Organization-configurable denominations with PHP defaults **1000, 500, 200, 100, 50, 20, 10, 5, 1, 0.25, 0.10, 0.05, 0.01** (not 0.50). Custom values such as 5000 or 0.50 still require no code change. Historical breakdown snapshots denomination values used at count time. Missing defaults are appended for older peso-only seeds; custom/disabled rows are preserved. Denomination UI/UX was preserved in the centavo-default refinement.
- Closing UX hides expected cash until the cashier submits a count (except historical Off snapshots that skip counting).

## 3. Explicit exclusions

- Denomination counting is not mandatory.
- Breakdown is not a second accounting total.
- GCash and Utang are not physical drawer cash.
- No offline shift open/close sync.
- No Redis, microservices, Platform Admin product-facing cash UI, or production-ready claim.
- Device Verified / Browser Verified remain No until the owner confirms.

## 4. Persistence / migrations

Migration `AddPosCashDenominationsAndRequiredDefault`:

- `operational_setups.cash_count_mode` default Required; Off → Optional; check `IN ('Optional', 'Required')`
- `organization_cash_denominations` (unique org+value)
- `cashier_shift_cash_count_lines` (unique shift+kind+value)
- `cashier_shifts.effective_cash_count_mode` still allows Off for history

Existing Required/Optional unchanged. PHP denomination seed is idempotent in application code, not a one-time SQL dump. Centavo default refinement does **not** add a migration (`numeric(18,2)` already stores 0.25 / 0.10 / 0.05 / 0.01). Missing default values are appended; existing custom rows are preserved.

## 5. API / UI

- `GET/PUT /api/v1/pos/operational-setup/cash-denominations`
- Open/close requests accept optional `denominationLines`
- Shift DTOs return opening/closing breakdown when present
- Org Web Settings: Required/Optional + denomination admin (AntDesign)
- MAUI: policy select, compact denomination add/enable, money-icon helper

## 6. Build / test evidence

| Suite | Passed | Failed | Skipped | Notes |
|---|---:|---:|---:|---|
| PinoyBusinessPOS.UnitTests | 654 | 0 | 0 | Includes centavo default and line-total tests |
| PinoyBusinessPOS.IntegrationTests (CashCount/OperationalSetup filter) | 11 | 0 | 0 | NEW |
| PinoyBusinessPOS.Maui.Tests | 380 | 1 | 0 | 1 PRE-EXISTING (`Cashier` substring in auth foundation guard) |
| PinoyBusinessPOS.Web.Tests | 8 | 0 | 0 | Includes Org Web cash-handling guard |
| PinoyBusinessPOS.ApiClient.Tests | 48 | 0 | 0 | |
| Platform.UnitTests | 856 | 0 | 0 | |
| Platform.Admin.UnitTests | 135 | 5 | 0 | 5 PRE-EXISTING (Statistic/AmountDisplay/FormatMoney/payments audit) |
| Personal.Web.Tests | 3 | 0 | 0 | |
| ArchitectureTests | 161 | 4 | 0 | 4 PRE-EXISTING |

No new regressions identified in this work package.

## 7. Security limitations

Development-stage unauthenticated POS APIs remain development-stage. Authorization uses existing POS commercial capabilities. Organization id comes from request scope. Client-calculated denomination totals are not trusted.

## 8. Portfolio independence

No HealthCare tree. POS remains the only product in this WP. Platform Admin has no product-facing cash-count implementation.

## 9. Risks / open decisions

- Owner must validate Org Web and a physical MAUI device before Device/Browser Verified.
- PinoyBusinessPOS remains PHP-authoritative; other currencies can reuse the denomination table but are not seeded here.
- Historical Off shifts still skip counting by snapshot.

## 10. Files / docs

See git commits `cbcdb8a9` (feat), `8869a179` (test), `528de183` (docs). Canonical engineering page: [pos-cashier-cash-count.md](../engineering/pos-cashier-cash-count.md).

## 11. Owner acceptance

Checklist is in [pos-cashier-cash-count.md](../engineering/pos-cashier-cash-count.md). Do not mark Device Verified or Production Ready until the owner validates.

## 12. Next work package

Continue Phase 25 owner validation of WP01–WP05, or the next authorized focused POS/platform package. Do not mark Phase 25 closed.
