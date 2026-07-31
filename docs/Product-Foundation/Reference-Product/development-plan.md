# ReferenceLoan — Development Plan

> **FICTIONAL** P12-WP06. Foundation: [exits-product-foundation-reference.md](../exits-product-foundation-reference.md)  
> Do not prescribe real lending phases or copy POS phases.

| Field | Value |
|---|---|
| Product | ReferenceLoan |
| Plan status | Draft — fictional validation only |

## Delivery approach

- One authorized work package at a time.
- Server-authoritative business rules; UI/API do not become a second SoR.
- Scope gate: missing product-owner policy → stop and record a decision (do not invent).

## Phased delivery (product-defined)

| Phase | Objective | Exit criteria |
|---|---|---|
| RL-P0 Docs | Documentation baseline | Docs approved; no code |
| RL-P1 Skeleton | Product skeleton when separately authorized | Isolation guards green |

Detail per phase: `roadmap.md`.

## Work-package format

Each WP must define: objective; business rules; authz; persistence; API; UI; online/offline; security; concurrency/idempotency; tests; docs; exclusions; acceptance; Git.

Report template: `docs/Product-Foundation/Templates/work-package-report.md` → product `Docs/reports/`.

## Scope gates

Stop when any of these are missing without an approved decision:

- [x] Product definition approved (fictional)
- [x] Roles/grants matrix draft
- [x] Operational-money definition
- [x] DB name/schema
- [x] Privacy classification (PHI default none)
- [x] Explicit exclusions

## Dependencies

| Dependency | Type | Notes |
|---|---|---|
| Platform catalog / subscription | Platform | Independent subscription required |
| Commercial-state transport | **DECISION D-P12-03** | Provisional patterns only; do not invent final |
| Production authentication | **R-091** | Keep Dev/Testing language honest |
| Lending domain policy | Product owner | **Open** — blocks real MVP |

## Testing expectations

| Layer | Expectation |
|---|---|
| Unit / domain | Domain rules testable without UI |
| Architecture guards | Enforce isolation (no Platform EF from product UI/domain, etc.) |
| Integration | Real product DB (e.g. Testcontainers); no EF InMemory as PostgreSQL proof |
| Migrations | Apply / rollback / re-apply when persistence changes |
| UI / device | Do not claim evidence you do not have |

Never weaken tests to pass a WP.

## Documentation and Git closeout

- [x] Fictional docs for dry run
- [ ] WP report filed — N/A (no implementation WP authorized)
- [x] Risks/decisions updated
- [ ] Implementation commits — **out of scope**

## Readiness boundaries

| Environment | Decision |
|---|---|
| Development / Testing | Not started — docs only |
| Production | Blocked — R-091 / TLS / fictional product |

## Explicit exclusions from this plan

- Any implementation of ReferenceLoan in this Phase 12 dry run
