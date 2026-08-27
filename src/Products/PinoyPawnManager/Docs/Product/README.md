# Pinoy Pawn Manager — Product Domain Docs

> Parent index: [../README.md](../README.md)  
> Decisions: [../risks-and-decisions.md](../risks-and-decisions.md)  
> Custody companion: [../Custody/README.md](../Custody/README.md)

| Field | Value |
|---|---|
| Product | Pinoy Pawn Manager (PPM) |
| Folder | `Docs/Product/` |
| Status | PPM-00 planning |
| Implementation | **None** |
| Last updated | 2026-08-27 |
| LEGAL_AUTHORIZATION_CLAIMED | **NO** |

This folder defines the **pawn business domain**: customer reference, pledged item, appraisal, agreement/ticket, funds release, maturity, renewal, redemption, unredeemed disposition, and operational reporting.

It is **architecture and product intent only**. Nothing here is implemented. Do not treat field lists as schemas or state names as shipped enums.

---

## Permanent principles (Product)

| Principle | Value |
|---|---|
| PPM is first-class ExItS product | YES — not a PLM / POS / BNPL module |
| Appraisal value ≠ loan principal | YES — record separately ([appraisal-model.md](appraisal-model.md), [loan-release-model.md](loan-release-model.md)) |
| Payment ≠ physical release | YES — financial settle then custody release ([redemption-model.md](redemption-model.md)) |
| Custody history required | YES — see [../Custody/](../Custody/README.md) |
| Pledged item ≠ POS inventory while pledged | YES ([pledged-item-model.md](pledged-item-model.md), [unredeemed-and-disposition-model.md](unredeemed-and-disposition-model.md)) |
| Web/PWA financial & custody mutations (initial) | **ONLINE-ONLY** |
| Philippine legal rates / grace / auction invented | **Forbidden** — mark Open as `PPM-D-00-XX` |

---

## Document index

| Doc | Purpose |
|---|---|
| [pawn-transaction-model.md](pawn-transaction-model.md) | Canonical flow + **state machine A** (DRAFT → … → CLOSED/CANCELLED) |
| [customer-model.md](customer-model.md) | Platform identity vs PPM customer reference; no second auth |
| [pledged-item-model.md](pledged-item-model.md) | Collateral concepts, categories, evidence; not POS stock |
| [appraisal-model.md](appraisal-model.md) | Manual appraisal vs principal; history; no AI |
| [pawn-ticket-and-agreement.md](pawn-ticket-and-agreement.md) | Ticket/agreement snapshot; immutability of history |
| [loan-release-model.md](loan-release-model.md) | Principal vs appraised; release channels (cash Open) |
| [maturity-model.md](maturity-model.md) | Maturity dates/TZ Open; no legal guess |
| [renewal-model.md](renewal-model.md) | Renewal/extension flow; not unlimited |
| [redemption-model.md](redemption-model.md) | Payment then separate physical release |
| [unredeemed-and-disposition-model.md](unredeemed-and-disposition-model.md) | Operational vs legal; POS handoff boundary |
| [reporting-baseline.md](reporting-baseline.md) | Operational / custody / financial / management reports |

---

## How Product relates to other machines

PPM keeps **four** planning state machines separate ([../architecture.md](../architecture.md)):

| Machine | Owns | Primary doc |
|---|---|---|
| **A. Pawn transaction** | Obligation lifecycle | [pawn-transaction-model.md](pawn-transaction-model.md) |
| **B. Custody** | Physical control of pledged items | [../Custody/custody-state-model.md](../Custody/custody-state-model.md) |
| **C. Payment / financial op** | Idempotent money events | [../Architecture/idempotency-and-reconciliation.md](../Architecture/idempotency-and-reconciliation.md) (when present) |
| **D. Disposition** | Unredeemed eligibility → handoff | [unredeemed-and-disposition-model.md](unredeemed-and-disposition-model.md) |

Agents must not collapse **A** and **B** (e.g. marking ticket `REDEEMED` solely because cash was received without a release event).

---

## Open decisions most used in this folder

| ID | Topic |
|---|---|
| [PPM-D-00-05](../risks-and-decisions.md) | Accepted collateral categories |
| [PPM-D-00-06](../risks-and-decisions.md) | Appraisal methodology |
| [PPM-D-00-07](../risks-and-decisions.md) | Loan-to-appraisal policy |
| [PPM-D-00-08](../risks-and-decisions.md) | Interest / finance charge model |
| [PPM-D-00-09](../risks-and-decisions.md) | Maturity model / TZ |
| [PPM-D-00-10](../risks-and-decisions.md) | Grace / default process |
| [PPM-D-00-11](../risks-and-decisions.md) | Renewal rules |
| [PPM-D-00-12](../risks-and-decisions.md) | Partial-payment policy |
| [PPM-D-00-13](../risks-and-decisions.md) | Authorized representative redemption |
| [PPM-D-00-14](../risks-and-decisions.md) | Disposition / auction model |
| [PPM-D-00-15](../risks-and-decisions.md) | POS / Commerce inventory handoff |
| [PPM-D-00-17](../risks-and-decisions.md) | Cash-management integration |
| [PPM-D-00-20](../risks-and-decisions.md) | Regulatory / licensing prerequisites |

---

## Reading order for new agents

1. [../product-definition.md](../product-definition.md) — ownership and exclusions  
2. [pawn-transaction-model.md](pawn-transaction-model.md) — state machine A  
3. [customer-model.md](customer-model.md) → [pledged-item-model.md](pledged-item-model.md) → [appraisal-model.md](appraisal-model.md)  
4. [pawn-ticket-and-agreement.md](pawn-ticket-and-agreement.md) → [loan-release-model.md](loan-release-model.md)  
5. [maturity-model.md](maturity-model.md) → [renewal-model.md](renewal-model.md) → [redemption-model.md](redemption-model.md)  
6. [unredeemed-and-disposition-model.md](unredeemed-and-disposition-model.md)  
7. [../Custody/README.md](../Custody/README.md) — physical control  
8. [reporting-baseline.md](reporting-baseline.md)

---

## Exclusions (PPM-00)

- No domain entities, DbContext, migrations, APIs, or UI  
- No fixed ₱ rates, LTV %, grace days, or auction calendars  
- No claim that ExItS/PPM is a licensed Philippine pawnshop operator  
- No PLM loan-entity reuse; no POS inventory rows for pledged collateral  
