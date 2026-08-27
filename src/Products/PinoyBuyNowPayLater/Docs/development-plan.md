# Pinoy Buy Now Pay Later — Development Plan

> Foundation: [exits-product-foundation-reference.md](../../../../docs/Product-Foundation/exits-product-foundation-reference.md)  
> Sequencing: [roadmap.md](roadmap.md)

| Field | Value |
|---|---|
| Product | Pinoy Buy Now Pay Later |
| Status | Documentation foundation only (BNPL-00) |
| Last updated | 2026-08-27 |
| Implementation present | No |

## Delivery buckets

| Bucket | Purpose | Notes |
|---|---|---|
| Documentation | Product/architecture foundation | BNPL-00 — this package |
| Scaffold | Solution projects, isolation, Platform registration | After naming decisions close enough to register |
| Authorization | Product-local grants + org/branch access | Before money mutations |
| Customer / reference | Customer refs, Personal link contracts | Before heavy financing |
| Financing core | Application + lifecycle | Before installments |
| Installments | Schedule engine | Before repayments |
| Commerce integration | Availability + financed sale orchestration | Before ACTIVE financing in production sense |
| Repayments / overdue | Collections baseline | After ACTIVE plans exist |
| Settlement | Merchant settlement | Blocked on commercial model (BNPL-D-00-08) |
| Returns | Cross-domain coordination | After sale + financing exist |
| Reports / audit | Merchant + customer + audit | Incremental |
| Personal experience | Customer surfaces | Optional / authorized separately |
| Hardening | E2E, security, ops | Late |

## Testing expectations (when implementation exists)

| Area | Expectation |
|---|---|
| Unit | Lifecycle transitions, allocation, eligibility rules (policy-backed) |
| Integration | Commerce contract mocks; no direct POS DB; idempotency under retry |
| Architecture guards | No cross-product project refs that violate isolation; no POS inventory tables in BNPL |
| E2E | Path A and Path B converge; stock failure cancels ACTIVE; POS outage does not block repayments |
| Migration | Apply / rollback / re-apply when persistence authorized; never production Migrate-at-start |

## Non-goals of the development plan

- Authorizing implementation in BNPL-00
- Inventing interest/settlement legal models
- Copying PLM collector architecture wholesale
- Building offline mutation queues
