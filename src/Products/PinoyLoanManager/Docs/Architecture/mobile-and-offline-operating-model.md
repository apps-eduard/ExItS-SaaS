# Pinoy Loan Manager — Mobile and Offline Operating Model

**Status:** Accepted architecture policy (PLM-DOC-09); not implemented
**Implementation present:** No
**Last updated:** 2026-08-19

Canonical mobile/offline operating model for Pinoy Loan Manager MAUI Blazor Hybrid. Complements [application-surface-model.md](application-surface-model.md), [mobile-offline-boundary.md](mobile-offline-boundary.md), and [../Product/daily-operational-workflow.md](../Product/daily-operational-workflow.md).

---

## MAUI purpose

Pinoy Loan Manager MAUI is a **limited field / collector** client. It is **not** a duplicate of Organization Web.

Primary intended uses:

- assigned collection work and routes
- field payment capture (when online and authorized)
- missed-collection reason recording
- authorized field disbursement
- collector cash accountability views
- float receipt acknowledgment
- partial remittance preparation
- end-of-day remittance submission

Organization Web remains the full operational application for Owner, Manager, Cashier, approvals, configuration, deep reporting, and branch cash oversight.

---

## MVP authority model — online / server-authoritative

For MVP and until a later authorized package explicitly enables offline financial posting:

1. **Server remains authoritative** for all final financial authorization and posting.
2. MAUI operates **online-first**. Connectivity loss must not be treated as permission to post authoritative financial state locally.
3. All mutating financial commands (Payment, Disbursement, Float movement, Remittance, Reversal request, Waiver request, and similar) require successful server acceptance before they become durable product history.
4. UI must distinguish **draft / pending sync** from **posted / authoritative** state.

This model does **not** claim production-grade offline sync, conflict resolution, or local encryption. Those remain future design subjects.

---

## MVP offline capabilities (explicitly allowed)

The following offline behaviors are **approved for planning** in MVP documentation. They do **not** authorize implementation or LocalStore creation.

### Read-only cache

MAUI may cache **read-only** operational data for assigned work when connectivity is intermittent:

- assigned borrowers and loans (scope-limited)
- collection schedules and due summaries
- route / assignment lists for the current day or active period
- non-authoritative balances and statuses as last synced from server
- reference configuration needed to render assigned work (within role scope)

Cached data is **stale by definition**. UI must indicate freshness / connectivity. Cached values must **not** be treated as permission to post financial events without server confirmation.

### Offline drafts

MAUI may allow **offline drafts** for non-final activity:

- draft payment intent (amount, borrower, notes) pending upload
- draft missed-collection visit notes
- draft field disbursement preparation
- draft remittance preparation worksheets

Drafts are **not** posted Payments, Disbursements, Remittances, or cash movements. Drafts require explicit online submission and server acceptance. Duplicate submission protection remains a future implementation requirement ([../security.md](../security.md)).

Drafts must be clearly labeled, revocable, and must not alter Loan subledger or cash accountability until server posting succeeds.

---

## Explicitly not in MVP offline scope

The following remain **deferred** beyond MVP documentation closure:

| Capability | MVP status |
|---|---|
| Offline final financial posting | **Not authorized** |
| Offline authoritative receipt issuance | **Not authorized** |
| Offline float issuance or remittance finalization | **Not authorized** |
| Offline penalty waiver or reversal finalization | **Not authorized** |
| Local encrypted authoritative ledger | **Not authorized** |
| Default LocalStore / SQLite project | **Not authorized** |
| Conflict resolution for competing offline edits | **Not authorized** |
| Queued-command replay as immediately authoritative | **Not authorized** |

See [mobile-offline-boundary.md](mobile-offline-boundary.md) for boundary summary.

---

## Future offline financial posting (deferred)

A future work package may authorize offline financial posting only if it explicitly designs and approves:

- secure device identity and registration
- encrypted local storage
- queued commands with idempotency keys
- stale-data handling and permission revocation
- duplicate submission protection
- cash accountability continuity offline
- conflict handling and operator reconciliation
- offline receipt state vs posted receipt identity
- end-of-day remittance integrity

Until that package is approved, **do not** implement offline posting, LocalStore, or SQLite schema.

Collector device security requirements are documented as **future requirements only**: [../Security/collector-device-security-policy.md](../Security/collector-device-security-policy.md).

---

## Relationship to other surfaces

| Surface | Offline posture (MVP) |
|---|---|
| Organization Web | Online / server-authoritative |
| MAUI Hybrid | Online authoritative posting; optional read-only cache and offline drafts only |
| ExItS Personal | Online presentation; not a field cash client |
| Platform Admin | Not applicable |

---

## Explicit non-goals

- Claiming MVP implements offline sync
- Creating `ExItS.PinoyLoanManager.LocalStore` in MVP
- Treating queued device activity as posted history
- Duplicating Organization Web on MAUI
- Inventing SQLite schemas in this package

## Legal / compliance boundary

No mobile or offline workflow in this document is claimed legally compliant. GPS, location, and field collection practices may require organization policy, consent, and qualified legal review (PLM-D-00-11). See [../Product/collector-route-and-location-policy.md](../Product/collector-route-and-location-policy.md).
