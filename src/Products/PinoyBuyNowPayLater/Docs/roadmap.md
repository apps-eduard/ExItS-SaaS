# Pinoy Buy Now Pay Later — Roadmap / Phase Plan

> Foundation: [exits-product-foundation-reference.md](../../../../docs/Product-Foundation/exits-product-foundation-reference.md)  
> Phase names are for **this** product only — do not copy POS, PLM, or PSP phases as authority.

| Field | Value |
|---|---|
| Product | Pinoy Buy Now Pay Later |
| Current phase | BNPL-04 — Financing Application Lifecycle (complete) |
| Status | Financing Application Complete through APPROVED_PENDING_SALE; ACTIVE Not Started |
| Last updated | 2026-08-27 |

## Phase objective (BNPL-00)

Establish complete documentation-only foundation for BNPL: first-class product identity, domain ownership, commerce/inventory boundaries, financed purchase lifecycle, failure/idempotency, Utang/PLM boundaries, security/authorization intent, Web/PWA online-only policy, regulatory open questions, and implementation package sequence — without implementing product code.

## BNPL-00 work packages (documentation)

| WP | Name | Status |
|---|---|---|
| BNPL-00-WP01 | Documentation workspace and product identity | Completed |
| BNPL-00-WP02 | Product definition and ownership matrix | Completed |
| BNPL-00-WP03 | Commerce / inventory / dual-path orchestration | Completed |
| BNPL-00-WP04 | Financing lifecycle, eligibility, installments, repayments | Completed |
| BNPL-00-WP05 | Settlement, returns, reporting, Utang/PLM boundaries | Completed |
| BNPL-00-WP06 | Platform integration, persistence, API, PWA, failure, idempotency | Completed |
| BNPL-00-WP07 | Security, authorization, privacy, regulatory risk register | Completed |
| BNPL-00-WP08 | Roadmap, readiness checklist, foundation closeout | Completed |

## Implementation roadmap after BNPL-00 (planning only — not authorization)

| Package | Purpose | Status | Depends on | Owned areas | Explicit non-goals | Test gates |
|---|---|---|---|---|---|---|
| **BNPL-01** | Product scaffold + Platform registration | **COMPLETE** | BNPL-D-00-01..03 provisional approve | Projects, isolation, ProductCode, Local Validation catalog | No financing domain entities; no migrations | Build; architecture isolation tests |
| **BNPL-02** | Authorization + Organization/Branch access | **COMPLETE** | BNPL-01; BNPL-D-00-18 | Product-local capabilities, org/branch context, access guard | No money mutations; no grant DB | Authz fail-closed tests |
| **BNPL-03** | Customer / reference foundation | **COMPLETE** | BNPL-02 | BnplCustomer, DbContext, migration, customer API | No financing entities | Isolation + idempotent create tests |
| **BNPL-04** | Financing application + lifecycle | **COMPLETE** | BNPL-03 | Application through APPROVED_PENDING_SALE | No ACTIVE; no installments | State machine + ACTIVE prohibition tests |
| **BNPL-05** | Installment engine | Planned | BNPL-04; open term policy (BNPL-D-00-14) | Schedule generation | No repayments posting | Schedule correctness tests |
| **BNPL-06** | Commerce/POS product + availability integration | BNPL-04 | Read contracts for catalog/availability | No local inventory ledger; no sale finalization yet | Contract tests; no POS DB |
| **BNPL-07** | Financed sale orchestration | BNPL-05, BNPL-06 | Activate financing after commerce sale; snapshots | No second sale engine | Path A/B; stock fail; idempotency |
| **BNPL-08** | Repayments | BNPL-07 | Repayment posting, allocation | No payment-provider integration unless authorized | Idempotent repayments |
| **BNPL-09** | Overdue / collections | BNPL-08 | Overdue flags, queues, reminders intent | Do not auto-import PLM collectors | Overdue calculation tests |
| **BNPL-10** | Merchant settlement | BNPL-07; **BNPL-D-00-08** | Settlement records | No silent choice of regulated funding model | Settlement isolation from customer balance |
| **BNPL-11** | Returns / refunds coordination | BNPL-07, BNPL-08 | Cross-domain workflows | No direct POS stock edits from BNPL | Coordination contract tests |
| **BNPL-12** | Reports / audit | Incremental from BNPL-04+ | Merchant/customer/audit reports | — | Report authz |
| **BNPL-13** | Personal / customer experience | BNPL-08; BNPL-D-00-13 | Customer plan visibility | No staff grant leakage | Customer scope tests |
| **BNPL-14** | Hardening / e2e / security | Prior packages | E2E, threat regression | — | Full gates |

### Sequence rationale

Authorization and customer references precede financing mutations. Lifecycle (BNPL-04) precedes installments (BNPL-05). Commerce availability (BNPL-06) and orchestration (BNPL-07) precede ACTIVE financing in a real purchase sense. Repayments and overdue follow ACTIVE plans. Settlement is deliberately after (or gated by) commercial/legal funding decisions. Returns require both commerce sale and financing. Personal UX is late so staff core is stable. Hardening is last.

If Product Owner prioritizes POS-first Path A only for MVP, BNPL-06/07 still remain mandatory for stock truth; BNPL-first browse UI can slip later without changing ownership rules.

## Dependencies

| Dependency | Notes |
|---|---|
| Platform subscription for BNPL product code | Required; slug Open (BNPL-D-00-02) |
| D-P12-03 | Commercial transport Open |
| R-091 | Production auth Open |
| PinoyBusinessPOS commerce contracts | Required for inventory/sale; BNPL must not bypass |
| Legal/commercial settlement model | Blocks BNPL-10 (BNPL-D-00-08, BNPL-D-00-20) |

## Exit criteria for BNPL-00

- Documentation tree complete under `src/Products/PinoyBuyNowPayLater/Docs/`
- Decision register populated with DECIDED / OPEN / DEFERRED
- No product code, migrations, or databases created
- Readiness checklist and closeout report written
