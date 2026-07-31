# {{PRODUCT_NAME}} — Development Plan

> Template: P12-WP03. Foundation: [exits-product-foundation-reference.md](../../exits-product-foundation-reference.md)  
> Do not prescribe Loan/Pawnshop/BNPL/POS-specific phases here.

| Field | Value |
|---|---|
| Product | {{PRODUCT_NAME}} |
| Plan status | Draft / Approved |

## Delivery approach

- One authorized work package at a time.
- Server-authoritative business rules; UI/API do not become a second SoR.
- Scope gate: missing product-owner policy → stop and record a decision (do not invent).

## Phased delivery (product-defined)

| Phase | Objective | Exit criteria |
|---|---|---|
| {{PHASE_1}} | {{PHASE_1_OBJ}} | {{PHASE_1_EXIT}} |
| {{PHASE_2}} | {{PHASE_2_OBJ}} | {{PHASE_2_EXIT}} |

Detail per phase: `roadmap.md`.

## Work-package format

Each WP must define: objective; business rules; authz; persistence; API; UI; online/offline; security; concurrency/idempotency; tests; docs; exclusions; acceptance; Git.

Report template: `Templates/work-package-report.md` → `Docs/reports/`.

## Scope gates

Stop when any of these are missing without an approved decision:

- [ ] Product definition approved
- [ ] Roles/grants matrix draft
- [ ] Operational-money definition
- [ ] DB name/schema
- [ ] Privacy classification (PHI default none)
- [ ] Explicit exclusions

## Dependencies

| Dependency | Type | Notes |
|---|---|---|
| Platform catalog / subscription | Platform | Independent subscription required |
| Commercial-state transport | **DECISION D-P12-03** | Provisional patterns only; do not invent final |
| Production authentication | **R-091** | Keep Dev/Testing language honest |
| {{DEP_N}} | {{DEP_TYPE}} | {{DEP_NOTES}} |

## Testing expectations

| Layer | Expectation |
|---|---|
| Unit / domain | {{UNIT_EXPECT}} |
| Architecture guards | Enforce isolation (no Platform EF from product UI/domain, etc.) |
| Integration | Real product DB (e.g. Testcontainers); no EF InMemory as PostgreSQL proof |
| Migrations | Apply / rollback / re-apply when persistence changes |
| UI / device | {{UI_EXPECT}} — do not claim evidence you do not have |

Never weaken tests to pass a WP.

## Documentation and Git closeout

- [ ] Product docs updated to match code
- [ ] WP report filed
- [ ] Risks/decisions updated
- [ ] Focused commits; push; `main = origin/main`
- [ ] Working tree clean except intentional deferred files

## Readiness boundaries

| Environment | Decision |
|---|---|
| Development / Testing | {{DEV_READY}} |
| Production | Blocked while R-091 / TLS / other open risks remain — do not claim ready |

## Explicit exclusions from this plan

- {{PLAN_EXCLUSION_1}}
