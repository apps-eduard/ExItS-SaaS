# Pinoy Loan Manager — Mobile Offline Boundary

**Status:** Planning / architecture baseline (documentation only)
**Implementation present:** No
**Last updated:** 2026-08-19

MAUI is **online / server-authoritative initially** unless a later package explicitly enables offline financial operations.

Related: [source-and-project-layout.md](source-and-project-layout.md), [../Product/daily-operational-workflow.md](../Product/daily-operational-workflow.md), [../architecture.md](../architecture.md).

---

## Current baseline

Server remains authoritative for final financial authorization / posting.

Do **not** treat queued device activity as immediately authoritative.

`ExItS.PinoyLoanManager.LocalStore` is **not** justified yet. Create it only if/when an authorized offline package requires local persistence.

---

## Future offline architecture (if authorized later)

Must explicitly handle:

- secure device identity
- local encrypted storage
- queued commands
- idempotency
- stale data
- conflict handling
- revoked permissions
- duplicate submission
- offline receipt state
- cash reconciliation

Collector device security remains **OPEN**.

---

## Explicit non-goals

- Offline financial posting in this package
- SQLite schema
- Choosing LocalStore by default
