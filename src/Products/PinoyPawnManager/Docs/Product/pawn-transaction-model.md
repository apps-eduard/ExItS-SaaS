# Pawn Transaction Model (State Machine A)

> Index: [README.md](README.md)  
> Custody companion (machine B): [../Custody/custody-state-model.md](../Custody/custody-state-model.md)  
> Decisions: [../risks-and-decisions.md](../risks-and-decisions.md)

| Field | Value |
|---|---|
| Product | Pinoy Pawn Manager (PPM) |
| Status | PPM-00 planning |
| Implementation | **None** |
| Last updated | 2026-08-27 |
| LEGAL_AUTHORIZATION_CLAIMED | **NO** |

This document is the **canonical pawn obligation lifecycle** for PPM. Names are planning labels and may be refined at implementation; do not invent Philippine statutory timelines.

**LEGAL_AUTHORIZATION_CLAIMED=NO.** Operational states are **not** legal conclusions about ownership transfer, auction authority, or regulatory compliance ([PPM-D-00-10](../risks-and-decisions.md), [PPM-D-00-14](../risks-and-decisions.md), [PPM-D-00-20](../risks-and-decisions.md)).

---

## Canonical operational flow

```text
Customer identified
→ Item presented & inspected (pledged-item intake)
→ Appraisal recorded (APPRAISED)
→ Terms offered (OFFERED)
→ Customer accepts (ACCEPTED)
→ Agreement/ticket snapshot created
→ Item enters custody (machine B) — usually before or with activation
→ Funds released (machine C) → ACTIVE
→ Later paths:
     REDEEM  → payment (C) then physical release (B) → REDEEMED → CLOSED
     RENEW   → RENEWAL_PENDING → payment/acceptance → ACTIVE (new maturity)
     FAIL    → MATURED → UNREDEEMED (ops) → DISPOSITION_PENDING (if started)
             → CLOSED (after disposition/handoff completes per policy)
→ Or CANCELLED before activation
```

Supporting docs: [customer-model.md](customer-model.md), [pledged-item-model.md](pledged-item-model.md), [appraisal-model.md](appraisal-model.md), [pawn-ticket-and-agreement.md](pawn-ticket-and-agreement.md), [loan-release-model.md](loan-release-model.md), [maturity-model.md](maturity-model.md), [renewal-model.md](renewal-model.md), [redemption-model.md](redemption-model.md), [unredeemed-and-disposition-model.md](unredeemed-and-disposition-model.md).

---

## Design invariants

1. **Machine A ≠ machine B.** Ticket state and custody state move independently but must remain consistent (e.g. do not claim `ACTIVE` with no custody record once funds are out and item should be held).
2. **Payment ≠ release.** Redemption payment completion does **not** alone set custody to `RELEASED` ([redemption-model.md](redemption-model.md)).
3. **Snapshots.** Agreement, appraisal, and identifying item evidence are historical; later config changes do not rewrite them ([pawn-ticket-and-agreement.md](pawn-ticket-and-agreement.md)).
4. **Scoping.** Every transaction carries `OrganizationId` and `BranchId` (holding/origin branch). Cross-branch moves are explicit ([PPM-D-00-16](../risks-and-decisions.md)).
5. **Online-only (initial Web/PWA).** Creating offers, accepting, releasing funds, renewing, redeeming payments, and disposition starts require connectivity. No offline mutation outbox for these in the initial surface.
6. **Not PLM.** Do not map this machine onto PLM installment/unsecured loan entities.

---

## State catalog (planning)

| State | Meaning (planning) |
|---|---|
| `DRAFT` | Intake started; customer/item/appraisal incomplete or not yet binding |
| `APPRAISED` | At least one appraisal recorded; offer not yet presented as binding |
| `OFFERED` | Terms proposed to customer; awaiting accept/decline |
| `ACCEPTED` | Customer accepted terms; ticket/agreement pending activation (funds + custody) |
| `ACTIVE` | Obligation open: funds released; item in pawnshop custody |
| `MATURED` | Past maturity datetime; redemption/renewal still possible per policy (Open) |
| `RENEWAL_PENDING` | Renewal accepted or payment in progress; not yet back to `ACTIVE` |
| `REDEEMED` | Obligation settled **and** physical release process completed (or product policy defines settle vs release coupling — prefer separate custody completion) |
| `UNREDEEMED` | Operational classification: not redeemed after policy threshold; **not** automatic ownership transfer |
| `DISPOSITION_PENDING` | Disposition workflow started inside PPM |
| `CLOSED` | Terminal successful close (redeemed path or disposition path complete) |
| `CANCELLED` | Terminal cancel before activation (or policy-authorized cancel) |

State names may be refined; the **separation of payment, custody, and disposition** must not be refined away.

---

## Per-state detail

### `DRAFT`

| Aspect | Planning rule |
|---|---|
| **Entry** | Staff starts a pawn intake for an org/branch; customer and/or item may be partial |
| **Allowed next** | `APPRAISED`, `CANCELLED` |
| **Money** | No principal release |
| **Custody** | Item may be in `RECEIVING` only; not treated as pledged collateral under an active ticket |
| **Notes** | Soft draft; no customer-facing “pawn ticket” as binding instrument |

### `APPRAISED`

| Aspect | Planning rule |
|---|---|
| **Entry** | Manual appraisal recorded ([appraisal-model.md](appraisal-model.md), [PPM-D-00-06](../risks-and-decisions.md)) |
| **Allowed next** | `OFFERED`, `DRAFT` (revise intake), `CANCELLED` |
| **Money** | None released |
| **Custody** | Still receiving / holding for appraisal; not `ACTIVE` collateral |
| **Notes** | Appraised value recorded; principal not implied ([PPM-D-00-07](../risks-and-decisions.md)) |

### `OFFERED`

| Aspect | Planning rule |
|---|---|
| **Entry** | Staff proposes principal, charges (method Open [PPM-D-00-08](../risks-and-decisions.md)), maturity ([PPM-D-00-09](../risks-and-decisions.md)), and disclosure snapshot |
| **Allowed next** | `ACCEPTED`, `APPRAISED` / `OFFERED` (re-offer), `CANCELLED` |
| **Money** | None released until activation path |
| **Custody** | Item remains under shop control pending accept |
| **Notes** | Offer is not an unlimited standing quote; expiry of offer UX is product policy (Open, not legal)

### `ACCEPTED`

| Aspect | Planning rule |
|---|---|
| **Entry** | Customer (or authorized process) accepts disclosed terms |
| **Allowed next** | `ACTIVE` (after funds release + custody commitment), `CANCELLED` (pre-activation abort) |
| **Money** | Release **authorized** but not complete until machine C succeeds ([loan-release-model.md](loan-release-model.md)) |
| **Custody** | Must move toward `IN_CUSTODY` before or as part of activation |
| **Notes** | Ticket/agreement snapshot should be created at or before activation ([pawn-ticket-and-agreement.md](pawn-ticket-and-agreement.md)) |

### `ACTIVE`

| Aspect | Planning rule |
|---|---|
| **Entry** | Idempotent funds release completed **and** custody holds the pledged item |
| **Allowed next** | `MATURED`, `RENEWAL_PENDING`, redemption path toward `REDEEMED`, `CANCELLED` **prohibited** without extraordinary policy |
| **Money** | Principal outstanding; charges accrue per policy (Open) |
| **Custody** | Typically `IN_CUSTODY` (or `MOVING` within vault); not POS inventory |
| **Notes** | Core open pawn obligation |

### `MATURED`

| Aspect | Planning rule |
|---|---|
| **Entry** | Current time ≥ agreement maturity ([maturity-model.md](maturity-model.md)); **no invented grace law** |
| **Allowed next** | `RENEWAL_PENDING`, redemption → `REDEEMED`, `UNREDEEMED` (ops classification), possibly stay `MATURED` while still redeemable |
| **Money** | Outstanding may include matured charges; do not invent statutory multipliers |
| **Custody** | Remains in pawnshop custody until redeem release or disposition handoff |
| **Notes** | Maturity ≠ ownership transfer ([PPM-D-00-10](../risks-and-decisions.md)) |

### `RENEWAL_PENDING`

| Aspect | Planning rule |
|---|---|
| **Entry** | Renewal initiated ([renewal-model.md](renewal-model.md), [PPM-D-00-11](../risks-and-decisions.md)) |
| **Allowed next** | `ACTIVE` (renewal completed with new maturity snapshot), `MATURED` / prior state on abort, `UNREDEEMED` if renewal fails and policy classifies |
| **Money** | Renewal payment in progress (machine C); partials Open ([PPM-D-00-12](../risks-and-decisions.md)) |
| **Custody** | Item stays in custody; renewal is **not** a release |
| **Notes** | Renewals are not unlimited by assumption |

### `REDEEMED`

| Aspect | Planning rule |
|---|---|
| **Entry** | Required redemption payment accepted **and** physical release completed (recommended coupling) — see [redemption-model.md](redemption-model.md) |
| **Allowed next** | `CLOSED` |
| **Money** | Obligation settled; no further principal release |
| **Custody** | Must be `RELEASED` to customer (or authorized party per [PPM-D-00-13](../risks-and-decisions.md)) |
| **Notes** | Prefer an intermediate “payment complete / release pending” **only** as custody state (`RELEASE_PENDING`), not by skipping release |

### `UNREDEEMED`

| Aspect | Planning rule |
|---|---|
| **Entry** | Operational classification after maturity/policy window without redeem/renew ([unredeemed-and-disposition-model.md](unredeemed-and-disposition-model.md)) |
| **Allowed next** | `DISPOSITION_PENDING`, late redeem/renew if policy still allows (Open), `CLOSED` only via disposition completion path |
| **Money** | May still quote redeem amounts if policy allows; do not invent forfeiture law |
| **Custody** | Still PPM custody until disposition transfer |
| **Notes** | **Technical eligibility ≠ legal sale authority** |

### `DISPOSITION_PENDING`

| Aspect | Planning rule |
|---|---|
| **Entry** | Disposition workflow started inside PPM ([PPM-D-00-14](../risks-and-decisions.md)) |
| **Allowed next** | `CLOSED` (after authorized disposition + optional Commerce handoff), abort back to `UNREDEEMED` if cancelled |
| **Money** | Disposition proceeds accounting is Open; not SaaS billing |
| **Custody** | Moves toward `DISPOSITION_PENDING` / `TRANSFERRED_FOR_DISPOSITION` (machine B) |
| **Notes** | No silent auto-write into POS inventory ([PPM-D-00-15](../risks-and-decisions.md)) |

### `CLOSED`

| Aspect | Planning rule |
|---|---|
| **Entry** | Redeemed path complete, or disposition path complete under product rules |
| **Allowed next** | None (terminal) |
| **Money** | No new releases |
| **Custody** | Released or transferred; history retained |
| **Notes** | Audit and reports remain queryable |

### `CANCELLED`

| Aspect | Planning rule |
|---|---|
| **Entry** | Abort before activation (or rare authorized cancel); reason required |
| **Allowed next** | None (terminal) |
| **Money** | Must not leave uncleared principal release; if funds wrongly released, reconcile via machine C / ops procedure |
| **Custody** | Item returned or never fully pledged; movements audited |
| **Notes** | Cancelling an `ACTIVE` ticket is **not** a normal transition |

---

## Allowed transition matrix (summary)

| From → To | DRAFT | APPRAISED | OFFERED | ACCEPTED | ACTIVE | MATURED | RENEWAL_PENDING | REDEEMED | UNREDEEMED | DISPOSITION_PENDING | CLOSED | CANCELLED |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| DRAFT | — | ✓ | | | | | | | | | | ✓ |
| APPRAISED | ✓* | — | ✓ | | | | | | | | | ✓ |
| OFFERED | | ✓* | ✓* | ✓ | | | | | | | | ✓ |
| ACCEPTED | | | | — | ✓ | | | | | | | ✓ |
| ACTIVE | | | | | — | ✓ | ✓ | ✓† | | | | ✗ |
| MATURED | | | | | | — | ✓ | ✓† | ✓ | | | ✗ |
| RENEWAL_PENDING | | | | | ✓ | ✓* | — | | ✓* | | | ✗ |
| REDEEMED | | | | | | | | — | | | ✓ | ✗ |
| UNREDEEMED | | | | | | | ✓* | ✓* | — | ✓ | | ✗ |
| DISPOSITION_PENDING | | | | | | | | | ✓* | — | ✓ | ✗ |
| CLOSED | | | | | | | | | | | — | |
| CANCELLED | | | | | | | | | | | | — |

\*Revisions / aborts under explicit staff action.  
†Redemption path requires payment (C) + release (B); do not jump on payment alone.

---

## Prohibited transitions (hard rules)

| Prohibited | Why |
|---|---|
| `DRAFT`/`APPRAISED`/`OFFERED` → `ACTIVE` skipping accept + release | Binding obligation without acceptance/funds |
| `ACTIVE` → `CANCELLED` as casual undo | Financial and custody integrity |
| `MATURED` → `CLOSED` without redeem or disposition | Silent ownership / stock conversion risk |
| `UNREDEEMED` → POS inventory without disposition handoff | [PPM-D-00-15](../risks-and-decisions.md); pledged ≠ retail stock |
| Any state → `REDEEMED` on payment only | Payment ≠ physical release |
| `CLOSED`/`CANCELLED` → any non-terminal | Terminal integrity |
| Cross-org state change | Isolation breach ([PPM-R-00-08](../risks-and-decisions.md)) |
| Implicit cross-branch activation | [PPM-D-00-16](../risks-and-decisions.md) |

---

## Money vs custody implications (cheat sheet)

| Transition | Money (C) | Custody (B) |
|---|---|---|
| → `ACTIVE` | Principal release succeeds | Item `IN_CUSTODY` |
| → `RENEWAL_PENDING` / back to `ACTIVE` | Renewal payment | Unchanged hold |
| → `REDEEMED` | Redemption payment complete | `RELEASE_PENDING` → `RELEASED` |
| → `UNREDEEMED` | No auto forfeiture posting invented | Still held |
| → `DISPOSITION_PENDING` / `CLOSED` | Disposition money Open | Disposition / transfer states |

---

## Consistency with other docs

- Appraisal / principal: [appraisal-model.md](appraisal-model.md), [loan-release-model.md](loan-release-model.md)  
- Ticket immutability: [pawn-ticket-and-agreement.md](pawn-ticket-and-agreement.md)  
- Maturity / renewal / redeem / disposition: linked models above  
- Storage & release safeguards: [../Custody/](../Custody/README.md)

---

## Exclusions

- No implemented workflow engine or enum  
- No statutory PH maturity, grace, or auction schedule  
- No claim that `UNREDEEMED` equals legal ownership by the pawnshop  
