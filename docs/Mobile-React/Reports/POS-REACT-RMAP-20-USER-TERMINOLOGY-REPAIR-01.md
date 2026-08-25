# RMAP-20 — User-facing report boundary cleanup (Review Repair 01)

## Status

**PASS**

| Flag | Value |
| --- | --- |
| `RMAP20_USER_TERMINOLOGY_BOUNDARY` | PASS |
| `RMAP20_TAX_NOT_AVAILABLE_HIDDEN` | PASS |
| `RMAP20_B04_NOT_EXPOSED` | PASS |
| `RMAP20_NO_FAKE_PNL` | PASS |
| `RMAP20_MANUAL_GCASH_UI_LEAK` | NO |
| `RMAP_20_NATIVE_SPEAKER` | PENDING |
| `RMAP_TAX_AUTHORIZED` | NO |
| `RMAP_B04_AUTHORIZED` | NO |
| `RMAP_21_AUTHORIZED` | NO |

## Change

Removed developer/roadmap explanatory copy from normal Reports and Dashboard surfaces.

- No tax / VAT / BIR unavailable placeholders
- No RMAP_TAX_AUTHORIZED / RMAP-B04 user-facing text
- No “contracts are not proven” P&L notice
- No “backend deferred” export footnote (export control removed; not implemented)
- Commercial discount remaining honest as ordinary “not available yet” wording
- Connected-supplier Guid wording rewritten to ordinary “raw ID” language
- ManualGCash continues to render as GCash

## Validation

Vitest terminology boundary + message-parity + Playwright RMAP-20 updated to assert absence (not visibility) of roadmap flags.
