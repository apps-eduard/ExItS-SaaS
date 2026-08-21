# POS-REACT I18N-01 Repair 01 — Translation fidelity + shift cash count history

## Status

**PASS**

| Flag | Value |
|------|-------|
| `I18N_01_IMPLEMENTATION` | PASS (repair) |
| `I18N_TRANSLATION_FIDELITY` | **CLOSED** |
| `SHIFT_CASH_COUNT_HISTORY_REACT` | **CLOSED** |
| `CASH_POLICY_BACKEND` | PASS (unchanged) |
| `RMAP_B05_AUTHORIZED` | NO |
| `RMAP_15_AUTHORIZED` | NO |

## Starting HEAD

`417fe06286181c87d0ef0f21adab1b6be3b25d29`

## Commits

| Role | Hash |
|------|------|
| Implementation | `e7bd79a9d69920d3cf4052d5da0cfdcc0334bce6` |
| Docs closeout | `d1c13e256190f0a486e9151c51ee1f34dab4a5ad` |
| Final HEAD (after hash record) | `e3c4ac0dc8cb9b91fdb36081c0d7630d5ae5aade` |

## Evidence

- format:check PASS
- typecheck PASS
- lint PASS (existing refresh warnings only)
- Vitest `src/i18n/message-parity.test.ts` + `src/features/shifts` PASS (19)
- Client production build PASS
- Fidelity: ceb 21.1% / ilo 18.0% / hil 22.4% identical to fil-PH (guard &lt; 35%)

## Delivered

### Translation fidelity

- Regenerated `ceb-PH`, `ilo-PH`, `hil-PH` from English meanings with regional phrasing (not Filipino clones)
- Fidelity check: identical-to-`fil-PH` under 35% (ceb ~21%, ilo ~18%, hil ~22%)
- Vitest guard in `message-parity.test.ts`
- Helper: `scripts/i18n-fidelity-check.cjs`

### Shift cash count history (React)

- New `CashCountHistoryBlock` on `ShiftDetailPage`
- Opening breakdown from stored `openingDenominationLines` when counted
- Closed shift: counted cash + `closingDenominationLines`, variance, closing notes
- No new backend APIs (detail DTO already carried lines)

### New i18n keys

- `shift.tapDenominationHint`
- `shift.viewOpeningDenominationBreakdown`
- `shift.viewDenominationBreakdown`
- `shift.countedCash`
- `shift.cashVariance`

## Explicit exclusions

- RMAP-15 / B01 / 12b / B04 / tax / Arabic / RMAP-B05 implementation
- Native-speaker certification of every string (regional rewrite + CI fidelity guard; further linguistic polish welcome)

## Next

Await Product Owner authorization for the next package.
