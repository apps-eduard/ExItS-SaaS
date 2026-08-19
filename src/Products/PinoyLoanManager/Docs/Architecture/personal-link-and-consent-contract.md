# Pinoy Loan Manager — Personal Link and Consent Contract

**Status:** Accepted product contract requirements (PLM-DOC-10); Platform persistence schema **not** designed
**Implementation present:** No
**Last updated:** 2026-08-19

Required **operations** and **contract facts** for optional Personal ↔ Borrower linking. Product behavior is canonical in [../Product/personal-linking-lifecycle-and-visibility.md](../Product/personal-linking-lifecycle-and-visibility.md). This document states the cross-product contract surface PLM and Platform/Personal must implement later.

**PLM-D-00-04 remains Open** (External Platform generic relationship model). **PLM-D-00-05 is Closed for PLM behavior/contract requirements**; Platform transport and persistence implementation remain external.

Related: [personal-integration-boundary.md](personal-integration-boundary.md), [../Product/personal-borrower-linking.md](../Product/personal-borrower-linking.md), [../Decisions/ADR-002-borrower-personal-cardinality-and-consent.md](../Decisions/ADR-002-borrower-personal-cardinality-and-consent.md), [../Decisions/ADR-019-platform-personal-contract-requirements.md](../Decisions/ADR-019-platform-personal-contract-requirements.md).

---

## Authority

| Concern | Owner |
|---|---|
| Personal identity | Platform |
| PLM Borrower and Loan operational data | Pinoy Loan Manager |
| Link/consent **relationship state** exposed to products | Approved Platform contract (schema TBD under PLM-D-00-04) |
| Consent UX in ExItS Personal | Personal presentation; PLM initiates business request |

PLM stores only the minimum relationship reference needed for authorization and customer presentation. Platform must not store Loan history as the system of record.

---

## Required contract operations

Future approved integration must support the following operations. Names are logical, not final route names.

### Identity resolution (identify only)

| Operation | Initiator | Outcome |
|---|---|---|
| Resolve Personal preview by EX ID / QR | Authorized PLM org user | Returns **minimum** Personal preview facts for wrong-person avoidance |
| Resolve Personal preview by approved alternate key | Authorized PLM org user | Same; alternate keys remain Platform-owned |

Resolution **never** creates a link, Borrower, Loan, or offer.

### Link request and consent

| Operation | Initiator | Outcome |
|---|---|---|
| Create link request | Authorized PLM org user (`plm.personal-links.request`) | Moves Borrower toward Pending Personal Consent |
| Deliver consent prompt | Platform / Personal | Personal user sees consent request with minimum context |
| Accept consent | Personal user | Active link established; auditable consent record |
| Decline consent | Personal user | No active link; Borrower unchanged |
| Expire pending request | Platform or PLM policy job | No active link; new request required |

MVP: organization-initiated only. Personal self-claim is **not** in this contract for MVP.

### Active link maintenance

| Operation | Initiator | Outcome |
|---|---|---|
| Query link status | PLM or Personal via contract | Returns current relationship state for one org/Borrower/Personal tuple |
| Revoke consent | Personal user | Link ends; PLM blocks new Personal-delivered actions |
| Suspend link | Authorized PLM org user | Link suspended; same blocking rules as revoke for Personal-delivered actions |
| Resume / relink | New request + new consent | New audit trail; same Personal identity only after re-consent |

Changing Borrower from one Personal identity to a **different** Personal identity is a high-risk correction (maker/checker per PLM-D-00-13 Closed), not a silent contract update.

---

## Required contract facts

Responses and events must be able to express at least:

| Fact | Requirement |
|---|---|
| Organization identifier | Guid |
| Borrower identifier | PLM-owned Guid |
| Personal user identifier | Platform-owned Guid |
| Relationship state | Unlinked, Link Requested, Pending Personal Consent, Linked, Declined, Expired, Consent Revoked, Organization Suspended |
| Consent timestamp / version | When link became active |
| Preview minimum fields | Enough to confirm identity; field list not finalized here |
| Blocking flags | Whether Personal-delivered Quick Loan actions are allowed |
| Audit correlation | Idempotency / request identifier for each transition |

Cardinality rules (accepted product behavior):

- at most one **active** Personal link per Borrower per Organization
- at most one **active** Borrower per Personal identity per Organization
- separate Borrowers across Organizations; no cross-lender visibility

---

## Effects that the contract must preserve

### On decline, expiry, revoke, or suspend

Must **not** delete or alter:

- Borrower
- applications / requests
- Loans, schedules, balances
- payments, receipts, audit

Must **block**:

- new Quick Loan offers delivered through Personal
- new Personal-originated Quick Loan requests
- other Personal-relationship-dependent actions defined in PLM policy

### Pending Quick Loan offers after unlink

| Situation | Contract behavior |
|---|---|
| Offer published while Linked, unlinked before acceptance | Offer no longer actionable through Personal; PLM org workflows unchanged |
| Request submitted while Linked, unlinked before approval | Request remains in PLM; Personal-delivered status may become read-only or hidden per visibility rules |
| Active Loan after unlink | Remains in PLM; Personal may retain read-only visibility where permitted |

### Historical Personal visibility after unlink

Contract must allow Personal to continue presenting **authorized read-only** views of submitted requests and active contractual obligations where product policy permits. Exact legal basis remains **PLM-D-00-11 Open**.

### Re-linking and consent history

- Relink to same Personal identity requires new request + new consent + new audit entry
- Prior link history must remain auditable; no silent overwrite of prior Personal reference

---

## Data minimization

The contract must not require PLM to receive:

- unrelated POS purchase history
- another lender’s Borrower data
- unrestricted Personal profile exports

The contract must not require Platform to persist Loan balances, schedules, or payment history.

---

## Explicit non-goals

- Platform relationship table design (**PLM-D-00-04 Open**)
- Transport selection (**D-P12-03 Open** where overlapping commercial/session facts apply)
- Copying POS Customer / linking tables
- Auto-link from EX ID / QR
- Legal compliance sign-off (**PLM-D-00-11 Open**)
