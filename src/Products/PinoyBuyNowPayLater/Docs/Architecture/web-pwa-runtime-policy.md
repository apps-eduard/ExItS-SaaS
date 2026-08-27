# Web / PWA Runtime Policy

**Status:** Planning baseline (BNPL-00)  
**Implementation present:** No  
**Related:** BNPL-D-00-11

## Baseline recommendation

For Web/PWA: **ONLINE-ONLY** operational runtime for business and financial mutations.

| Allowed offline-ish behavior | Forbidden without future ADR |
|---|---|
| Installable PWA | Offline financing mutation queue |
| Standalone display mode | Offline approval / activation |
| Static shell / asset cache | Offline repayment posting |
| Read-only cached non-authoritative UI (if ever) clearly labeled | Treating cache as stock reservation |

## Rationale

Financing and commerce coordination are concurrency- and money-sensitive. Offline mutation queues increase duplicate and reconciliation risk. POS offline patterns are **not** inherited by default.

## Future native / Capacitor offline

If considered later, record as a **separate architecture decision**. Not authorized in BNPL-00.
