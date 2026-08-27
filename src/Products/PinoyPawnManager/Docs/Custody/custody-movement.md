# Custody Movement

> Index: [README.md](README.md)  
> States: [custody-state-model.md](custody-state-model.md)  
> Locations: [storage-location-model.md](storage-location-model.md)  
> Decisions: [../risks-and-decisions.md](../risks-and-decisions.md)

| Field | Value |
|---|---|
| Product | Pinoy Pawn Manager (PPM) |
| Status | PPM-00 planning |
| Implementation | **None** |
| Last updated | 2026-08-27 |
| LEGAL_AUTHORIZATION_CLAIMED | **NO** |

A **custody movement** is an append-only audit event: an item left one control context and entered another. **Current location is not enough** — history is required for disputes, loss investigation, and insider controls ([PPM-R-00-06](../risks-and-decisions.md)).

---

## When movements are recorded

| Event | Movement? |
|---|---|
| Intake receive | Yes — into `RECEIVING` / first location |
| Vault put-away | Yes |
| Bin-to-bin / bag re-slot | Yes |
| Pull to counter for redeem | Yes — often into `MOVING` / `RELEASE_PENDING` |
| Customer handover | Yes — culminates in `RELEASED` |
| Cross-branch transfer | Yes — explicit ([PPM-D-00-16](../risks-and-decisions.md)) |
| Disposition channel transfer | Yes |
| Appraisal desk temporary hold | Yes if location/control changes |
| Ticket state-only change (e.g. matured) | **No** location movement unless item physically moved |

---

## Audit fields (planning concepts)

These are **concepts**, not an implemented table:

| Field concept | Intent |
|---|---|
| `MovementId` | Unique event id |
| `OrganizationId` / `BranchId` | Scope (destination branch if transfer) |
| `PledgedItemId` / bag id | What moved |
| `FromLocationId` (nullable on first receive) | Source node |
| `ToLocationId` (nullable on final customer release) | Destination node |
| `FromCustodyState` / `ToCustodyState` | Machine B transition |
| `ReasonCode` | PutAway / PullForRedeem / Transfer / Disposition / Correction / IntakeReturn / … |
| `TicketId` (optional link) | Related obligation |
| `ActorStaffId` | Who performed |
| `WitnessStaffId` (optional) | Dual control when policy requires |
| `Timestamp` | When |
| `Client / device metadata` | Online session facts (no offline queue initially) |
| `Notes` | Short ops note |
| `IdempotencyKey` | Prevent duplicate movement posts |
| `RelatedPaymentId` | When pull is for redemption after pay |
| `RelatedIncidentId` | When movement follows discrepancy |

---

## Rules

| Rule | Intent |
|---|---|
| Append-only | Corrections are new movements or linked incidents — not UPDATE-in-place of history |
| Actor required | No anonymous vault changes |
| Branch integrity | From/to locations must belong to allowed branch set for the operation |
| Cross-branch | Dedicated reason + authorization; never side effect of UI branch switch |
| Online-only | Initial Web/PWA cannot enqueue movements offline |
| Not POS stock transfer | Different domain |

---

## Consistency with release

Redemption pull should typically:

1. Record payment (machine C)  
2. Movement vault → counter (`RELEASE_PENDING`)  
3. Release checklist  
4. Movement to customer (`RELEASED`)  

Skipping movement records while changing state is a defect.

---

## Reporting

Movement logs feed custody reports ([../Product/reporting-baseline.md](../Product/reporting-baseline.md)): who touched the bag, when, and where it went.

---

## Exclusions

- No GPS tracking assumed  
- No silent location backfill that fabricates history  
- No cross-product movement into POS bins while pledged  
