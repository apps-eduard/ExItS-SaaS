# POS-REACT I18N-01 Repair 02 — Shift cash history semantics + reconciliation

## Status

**PASS**

| Flag | Value |
|------|-------|
| `I18N_TRANSLATION_TECHNICAL_COMPLETENESS` | YES |
| `I18N_NATIVE_SPEAKER_REVIEW` | **PENDING** |
| `SHIFT_CASH_COUNT_HISTORY_REACT` | PASS |
| `SHIFT_COUNT_SKIPPED_VS_ZERO` | PASS |
| `SHIFT_EFFECTIVE_POLICY_HISTORY` | PASS |
| `SHIFT_CASH_RECONCILIATION_HISTORY` | PASS |
| `RMAP_15_AUTHORIZED` | NO |
| `RMAP_B01_AUTHORIZED` | NO |
| `RMAP_12B_AUTHORIZED` | NO |
| `RMAP_B04_AUTHORIZED` | NO |
| `RMAP_B05_AUTHORIZED` | NO |
| `RMAP_TAX_AUTHORIZED` | NO |
| `PRODUCTION_CUTOVER` | NO |

## Starting HEAD

Baseline after preferences close fix (reported in Cursor final response).

## Delivered

### Skipped vs counted zero

- `CashCountHistoryBlock` takes authoritative `counted`
- Not counted → localized “Not counted” (never `MoneyDisplay(0)`)
- Counted zero → `PHP 0.00`
- Manual total with empty lines → amount only, no breakdown toggle

### Historical policy snapshots

- Opening: `effectiveOpeningCashCountMode` with legacy `effectiveCashCountMode` fallback
- Closing: `effectiveClosingCashCountMode` with same legacy fallback
- Closing counted: prefer `closingCashCountState`, else `closingCashAmount != null`

### Reconciliation (closed shifts)

- Server `getCashierShiftSummary` values for cash sales, refunds, in, out, expected
- Difference: Balanced / Over by / Short by from server variance
- GCash / Utang informational only — never folded into drawer cash client-side

### Denomination history

- Opening/closing lines from shift DTO snapshots only
- Live `listCashDenominations` used only for open-shift close helper
- Historical `0.01` lines still render

## Explicit exclusions

- Native-speaker certification of regional catalogs (still PENDING)
- RMAP-15 and other unauthorized packages
- Backend / migration changes (NONE)

## Evidence

Recorded in Cursor final report (Vitest, Playwright, format, typecheck, lint, build, responsive).
