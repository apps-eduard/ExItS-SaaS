# Pinoy Loan Manager — Mobile Offline Boundary

**Status:** Planning / architecture baseline (documentation only); aligned with PLM-D-00-09
**Implementation present:** No
**Last updated:** 2026-08-19

The organization/field client is **online / server-authoritative initially**. Do not enable offline financial operations until a dedicated **PLM-13** package explicitly authorizes and designs them.

Related: [react-pwa-capacitor-client.md](react-pwa-capacitor-client.md), [source-and-project-layout.md](source-and-project-layout.md), [../Product/daily-operational-workflow.md](../Product/daily-operational-workflow.md), [../architecture.md](../architecture.md), [../Decisions/PLM-D-00-09-react-pwa-capacitor-client-strategy.md](../Decisions/PLM-D-00-09-react-pwa-capacitor-client-strategy.md).

---

## Current baseline

Server remains authoritative for final financial authorization / posting.

Do **not** treat queued device activity as immediately authoritative.

`ExItS.PinoyLoanManager.LocalStore` is **not** justified yet. Create it only if/when an authorized offline package requires local persistence.

PWA service worker may cache the static application shell and required assets. It must **not** cache as authoritative state: Loan API responses, borrower financial state, loan balances, payment/collection/cash records, authorization responses, session tokens, or sensitive financial payloads.

No Background Sync for financial commands initially. No offline financial posting.

Architecture must leave a clean adapter seam for later PLM-13 work. Do not define IndexedDB/SQLite financial schemas now. Do not queue payments, collections, disbursements, or financial postings.

---

## Future offline architecture (if authorized later — PLM-13)

Must explicitly handle:

- encryption
- device trust / secure device identity
- local encrypted storage
- queued commands
- idempotency
- stale data
- conflict handling
- revoked permissions
- duplicate submission
- cash reconciliation
- offline receipt state

Collector device security remains **OPEN**.

---

## Explicit non-goals

- Offline financial posting in this package
- SQLite / IndexedDB financial schema
- Choosing LocalStore by default
- Creating Capacitor or PWA code here
