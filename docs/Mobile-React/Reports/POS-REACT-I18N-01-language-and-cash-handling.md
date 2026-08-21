# POS-REACT I18N-01 — Philippine language expansion + cash handling policy closeout

## Status

**PASS**

| Flag | Value |
|------|-------|
| `I18N_01_PASS` | YES |
| `CASH_DENOMINATION_POLICY_CLOSEOUT` | YES |
| `DEFAULT_DENOMINATION_001_REMOVED` | YES |
| `OPENING_COUNT_DEFAULT_OPTIONAL` | YES |
| `CLOSING_COUNT_DEFAULT_OPTIONAL` | YES |
| `RMAP_B05_STATUS` | NOT_STARTED (design doc only) |
| `RMAP_B05_AUTHORIZED` | NO |
| `RMAP_15_AUTHORIZED` | NO |
| `RMAP_B01_AUTHORIZED` | NO |
| `RMAP_12B_AUTHORIZED` | NO |
| `RMAP_B04_AUTHORIZED` | NO |
| `RMAP_TAX_AUTHORIZED` | NO |
| `PRODUCTION_CUTOVER` | NO |

## Locales

| Locale | Label | Status |
|--------|-------|--------|
| `en` | English | PASS |
| `fil-PH` | Filipino | PASS |
| `ceb-PH` | Bisaya (Cebuano) | PASS |
| `ilo-PH` | Ilocano | PASS |
| `hil-PH` | Ilonggo (Hiligaynon) | PASS |

- Persistence: same `exits.pos-client.ui-preferences.v1` preference key as before
- Message-key parity: typed `Record<keyof typeof en, string>` per locale + Vitest parity suite (non-empty strings)
- Arabic: **not** added

Product search placeholder (EN): “Search by product name, barcode, or SKU”

## Cash handling policy

### Defaults

- Philippine denomination defaults: `1000, 500, 200, 100, 50, 20, 10, 5, 1, 0.25, 0.10, 0.05` — **no `0.01`**
- Opening cash count required default: **Optional (NO)**
- Closing cash count required default: **Optional (NO)**

### Admin (React `/org/cash-handling`)

- Require cash count when opening shift
- Require cash count when closing shift
- Add / remove denominations
- Zero denominations allowed; empty helper + manual total entry
- Snapshot hint: changes apply to the next shift; already-open shift keeps the policy it started with

### Opening / closing UX

- Denomination helper uses configured denominations only
- Optional: skip count / manual total
- Required: cannot finish without count
- Empty denominations: friendly empty state + manual total

### Backend (additive)

- Independent `OpeningCashCountMode` / `ClosingCashCountMode` on operational setup
- Shift snapshots `EffectiveOpeningCashCountMode` / `EffectiveClosingCashCountMode` (legacy `CashCountMode` / `EffectiveCashCountMode` retained for compatibility)
- Migration: `AddOpeningClosingCashCountModes`
- MAUI cash-handling settings updated to dual opening/closing toggles

## Tests

| Suite | Result |
|-------|--------|
| format:check | PASS |
| typecheck | PASS |
| lint | PASS (existing refresh warnings only) |
| Vitest | PASS (51 files) |
| build | PASS |
| Playwright `i18n-01-language-cash-handling.spec.ts` | PASS (10) |
| Playwright `rmap-10-register-shift.spec.ts` | PASS |
| Responsive 375 / 768 / 1024 / 1440 | PASS |

## Related future design (not implemented)

[platform-organization-public-landing.md](../../engineering/platform-organization-public-landing.md) — RMAP-B05 design only (`RMAP_B05_AUTHORIZED=NO`).
