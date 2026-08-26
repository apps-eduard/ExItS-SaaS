# Pinoy Loan Manager — Mobile Offline Boundary

**Status:** Accepted architecture policy (PLM-DOC-09); not implemented
**Implementation present:** No
**Last updated:** 2026-08-19

Summary boundary for MAUI offline posture. Canonical detail: [mobile-and-offline-operating-model.md](mobile-and-offline-operating-model.md). Related: [source-and-project-layout.md](source-and-project-layout.md), [../Product/daily-operational-workflow.md](../Product/daily-operational-workflow.md), [../architecture.md](../architecture.md).

---

## Current baseline

Server remains authoritative for final financial authorization / posting.

Do **not** treat queued device activity as immediately authoritative.

MVP allows **read-only cache** and **offline drafts** in planning only. **Offline final financial posting is not authorized.**

`ExItS.PinoyLoanManager.LocalStore` is **not** justified yet. Create it only if/when an authorized offline posting package requires local persistence.

---

## Future offline architecture (if authorized later)

Must explicitly handle:

- secure device identity ([../Security/collector-device-security-policy.md](../Security/collector-device-security-policy.md))
- local encrypted storage
- queued commands
- idempotency
- stale data
- conflict handling
- revoked permissions
- duplicate submission
- offline receipt state
- cash reconciliation

Offline financial posting remains **deferred** beyond PLM-DOC-09.

---

## Explicit non-goals

- Offline final financial posting in MVP
- SQLite schema
- Choosing LocalStore by default
- Claiming offline sync is implemented
