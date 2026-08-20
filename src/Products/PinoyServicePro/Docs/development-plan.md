# PinoyServicePro — Development Plan

> Template: P12-WP03. Foundation: [exits-product-foundation-reference.md](../../../../docs/Product-Foundation/exits-product-foundation-reference.md)

| Field | Value |
|---|---|
| Product | PinoyServicePro |
| Plan status | Draft — PSP-00 documentation complete; implementation not started |
| Last updated | 2026-08-20 |

## Delivery approach

- One authorized work package at a time.
- Server-authoritative business rules; UI/API do not become a second SoR.
- Scope gate: missing product-owner policy → stop and record a decision (do not invent).
- PSP-00 is **documentation-only**. Completing PSP-00 does **not** authorize PSP-01.

## Phased delivery (product-defined)

| Phase | Objective | Exit criteria |
|---|---|---|
| PSP-00 | Product discovery and documentation foundation | Docs complete, consistent, decisions registered; owner approval pending |
| PSP-01 | Product skeleton and Platform integration | Projects/isolation/scaffold only when authorized |
| PSP-02 | Product-local authorization and org/branch foundation | Roles/grants/branch scope enforceable |
| PSP-03 | Customer and service catalog foundation | Customers + ServiceOffering baseline |
| PSP-04 | Booking, scheduling, and walk-in operations | Booking ≠ completed transaction; walk-in supported |
| PSP-05 | Service jobs / work orders | Job lifecycle with lines |
| PSP-06 | Staff and resource assignment | Assignment without terminology-as-authz |
| PSP-07 | Customer assets and service history | Optional assets + durable history |
| PSP-08 | Estimates, pricing, labor, and materials | Capability-gated estimate and materials |
| PSP-09 | Payments, receipts, operational financial controls | Operational money ≠ SaaS billing |
| PSP-10 | Reporting, notifications, operational audit | Ops visibility without false compliance claims |
| PSP-11 | Mobile/offline capability if authorized | Only after PSP-D-00-04 |
| PSP-12 | Business-template hardening and initial vertical validation | Barber + Auto Repair validation |
| PSP-13 | Production/security/operational hardening | Blocked while R-091 and related risks remain |

Detail: [roadmap.md](roadmap.md). Sequence may change if dependency analysis improves; meaningful changes must be explained in roadmap updates.

## PSP-00 work packages (documentation-only)

| WP | Name | Status |
|---|---|---|
| PSP-00-WP01 | Documentation workspace and product identity | Completed (this foundation) |
| PSP-00-WP02 | Product definition and Platform/Product boundaries | Completed |
| PSP-00-WP03 | Dynamic business-template and capability model | Completed |
| PSP-00-WP04 | Core service operating model | Completed |
| PSP-00-WP05 | Booking, scheduling, walk-in and work-order model | Completed |
| PSP-00-WP06 | Customer, customer-asset and service-history model | Completed |
| PSP-00-WP07 | Services, labor, parts/materials, estimates and pricing baseline | Completed |
| PSP-00-WP08 | Staff/resource assignment, roles, grants and authorization baseline | Completed |
| PSP-00-WP09 | Payments, documents, reporting, notification and audit baseline | Completed |
| PSP-00-WP10 | Technical product layout, persistence, API, UI and offline boundaries | Completed |
| PSP-00-WP11 | Security, privacy and compliance baseline | Completed |
| PSP-00-WP12 | Foundation closeout and implementation-readiness review | Completed |

## Work-package format

Each future implementation WP must define: objective; business rules; authz; persistence; API; UI; online/offline; security; concurrency/idempotency; tests; docs; exclusions; acceptance; Git.

Report template: `docs/Product-Foundation/Templates/work-package-report.md` → product `Docs/Reports/`.

## Scope gates

Stop when any of these are missing without an approved decision:

- [x] Product definition drafted (owner approval pending — PSP-D-00-21)
- [x] Roles/grants matrix draft (identifiers open — PSP-D-00-18)
- [x] Operational-money ownership defined (policy details open)
- [ ] DB name/schema approved (proposed — PSP-D-00-02)
- [x] Privacy classification (PHI default none)
- [x] Explicit exclusions recorded

## Dependencies

| Dependency | Type | Notes |
|---|---|---|
| Platform catalog / subscription | Platform | Independent subscription required; slug open (PSP-D-00-01) |
| Commercial-state transport | **DECISION D-P12-03** | Provisional patterns only; do not invent final |
| Production authentication | **R-091** | Keep Dev/Testing language honest |
| Offline primitives | Optional shared tech | May evaluate later; do not inherit POS offline by default |
| Compliance architecture | Portfolio | Tax docs only via controlled ExItS compliance path (PSP-D-00-16) |

## Testing expectations (when implementation begins)

| Layer | Expectation |
|---|---|
| Unit / domain | Domain invariants for booking/job/money transitions |
| Architecture guards | Enforce isolation (no Platform EF from product UI/domain; no POS/Loan project refs) |
| Integration | Real product DB (e.g. Testcontainers); no EF InMemory as PostgreSQL proof |
| Migrations | Apply / rollback / re-apply when persistence changes |
| UI / device | Do not claim evidence you do not have |

Never weaken tests to pass a WP.

## Documentation and Git closeout

- [x] Product docs created for PSP-00
- [ ] Owner approval of documentation baseline (PSP-D-00-21)
- [x] Risks/decisions updated
- [ ] Focused commits; push only when separately authorized
- [ ] Working tree clean of unrelated product changes when committing PSP docs

## Readiness boundaries

| Environment | Decision |
|---|---|
| Development / Testing | Not started — scaffold blocked until PSP-01 authorized |
| Production | Blocked while R-091 / TLS / other open risks remain — do not claim ready |

## Explicit exclusions from this plan

- Starting PSP-01 without explicit authorization
- Treating PSP-00 completion as database/API/mobile/offline/BIR readiness
- Copying POS or Loan implementation phases as ServicePro policy
