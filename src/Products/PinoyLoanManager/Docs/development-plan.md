# Pinoy Loan Manager — Development Plan

> Template: P12-WP03. Foundation: [exits-product-foundation-reference.md](../../../../docs/Product-Foundation/exits-product-foundation-reference.md)
> Do not invent Loan business policy in this plan.

| Field | Value |
|---|---|
| Product | Pinoy Loan Manager |
| Plan status | Draft — planning buckets plus operating-model through technical-boundary direction; not product-owner approved |
| Implementation present | No |

## Delivery approach

- One authorized work package at a time.
- Server-authoritative business rules; UI/API do not become a second SoR.
- Scope gate: missing product-owner policy → stop and record a decision (do not invent).
- Do not copy PinoyBusinessPOS implementation to fill Loan gaps.

## Phased delivery (product-defined)

| Phase | Objective | Exit criteria |
|---|---|---|
| PLM-00 Foundation & Product Decisions | Documentation, identity, isolation, operating-model, financial-lifecycle, operational-control, borrower/Personal/publishing planning, and owner-decision register | Canonical docs exist; open decisions listed; no implementation claimed |
| PLM-01 Product Scaffold & Isolation | Authorized source/test/deploy skeleton and isolation guards | Projects exist without Loan domain; solution isolation proven; layout: [Architecture/source-and-project-layout.md](Architecture/source-and-project-layout.md) |
| PLM-02 Identity / Organization / Product Access | Consume Platform actor/org/product access without Platform table reads | Access intersection entry gates exist; D-P12-03 not invented |
| PLM-03 Loan Product Authorization | Product-local roles/grants | Matrix implemented only after PLM-D-00-06 |
| PLM-04 Borrower Foundation | Product-local borrower records; optional Personal link | Borrower exists without requiring Personal; no POS Customer reads |
| PLM-05 Loan Product Configuration | Configurable loan products | Only after PLM-D-00-08 decisions for configuration |
| PLM-06 Loan Application / Approval | Application and approval workflows | Only after owner approval rules exist |
| PLM-07 Origination / Disbursement | Starting loans and disbursing | Only after PLM-D-00-07 / relevant PLM-D-00-08 |
| PLM-08 Schedule / Calculation Engine | Schedules and calculations | Only after calculation policy exists |
| PLM-09 Payment Posting | Applying payments | Only after allocation rules exist |
| PLM-10 Collections / Delinquency | Arrears and collections | Only after collections policy exists |
| PLM-11 Reporting / Documents | Product reports and documents | Only after report contents are decided |
| PLM-12 Security / Audit / Privacy | Product audit, privacy, consent hardening | Evidence against this product’s security docs |
| PLM-13 Offline / Mobile Field Capabilities | MAUI/offline/field capabilities | Only after PLM-D-00-09 and owner authorization |
| PLM-14 Production Validation / Closeout | Production-readiness evidence | Blocked while R-091 / other portfolio production risks remain |

Detail per current phase: [roadmap.md](roadmap.md).

## Work-package format

Each WP must define: objective; business rules; authz; persistence; API; UI; online/offline; security; concurrency/idempotency; tests; docs; exclusions; acceptance; Git.

Report template: `docs/Product-Foundation/Templates/work-package-report.md` → product `Docs/Reports/`.

## Scope gates

Stop when any of these are missing without an approved decision:

- [ ] Product definition approved (draft plus operating-model direction exists; owner approval open — PLM-D-00-10)
- [ ] Roles/grants matrix draft (presets and grant **intent** recorded; identifiers open — PLM-D-00-06)
- [ ] Operational-money definition (boundary and ledger-vs-cash direction — PLM-D-00-07)
- [ ] DB name/schema (name proposed; schema open — PLM-D-00-02)
- [x] Privacy classification (PHI default none) — recorded
- [x] Explicit exclusions — recorded in [product-definition.md](product-definition.md)

Do not start PLM-01 until PLM-00 owner decisions needed for scaffold are resolved or explicitly deferred.

## Dependencies

| Dependency | Type | Notes |
|---|---|---|
| Platform catalog / subscription | Platform | Independent subscription required; slug registration open (PLM-D-00-01) |
| Commercial-state transport | **DECISION D-P12-03** | Provisional patterns only; do not invent final |
| Production authentication | **R-091** | Keep Dev/Testing language honest (D-P12-05) |
| Personal / cross-product relationship model | Platform + product | Open (PLM-D-00-04, PLM-D-00-05) |
| Loan owner policy | Product owner | Open (PLM-D-00-06, PLM-D-00-07, PLM-D-00-08) |

## Testing expectations

| Layer | Expectation |
|---|---|
| Unit / domain | When domain exists: server-authoritative rules; no invented policy in tests |
| Architecture guards | Enforce isolation (no Platform EF from product UI/domain; no POS project/table access) |
| Integration | Real product DB (e.g. Testcontainers); no EF InMemory as PostgreSQL proof |
| Migrations | Apply / rollback / re-apply when persistence changes |
| UI / device | Do not claim evidence you do not have. This documentation WP: **Not Applicable** |

Never weaken tests to pass a WP.

## Documentation and Git closeout

- [ ] Product docs updated to match code (no code in this WP)
- [ ] WP report filed (chat completion report; in-tree report optional)
- [x] Risks/decisions updated
- [ ] Focused commits; push; `main = origin/main` (push not authorized in this WP)
- [x] Working tree expected clean after the authorized commit except intentional deferred files

## Readiness boundaries

| Environment | Decision |
|---|---|
| Development / Testing | Not ready — no product runtime exists |
| Production | Blocked while R-091 / TLS / other open risks remain — do not claim ready |

## Explicit exclusions from this plan

- Implementing Loan capability in PLM-00
- Finalizing calculation, penalty amounts, rounding mode, grant identifiers, or regulatory rules
- Creating .NET projects, migrations, APIs, UI, Docker, or `ExItS.slnx` entries in this WP
- Copying POS phases or grant sets
