# Pinoy Loan Manager — Architecture

> Template: P12-WP03. Do not duplicate the foundation; link it.
> Foundation: [exits-product-foundation-reference.md](../../../../docs/Product-Foundation/exits-product-foundation-reference.md)

| Field | Value |
|---|---|
| Product | Pinoy Loan Manager / `pinoy-loan-manager` (**Closed**, PLM-D-00-01) |
| Database | `ExItS_PinoyLoanManager` (**logical name Closed**, PLM-D-00-02); not created; schema/placement deferred |
| Status | PLM MVP Product planning documentation complete (PLM-DOC-01–11); **PLM-D-00-10 Closed / Product Owner Accepted**; **R-091 Closed for Phase 13 scope**; **D-P12-05 Closed / satisfied for authentication honesty**; **D-P12-03 Open**; implementation absent and paused |
| Implementation present | No |

## System context

```text
[Actors] → Platform (identity, org, subscription, entitlements, SaaS billing)
                ↓ commercial access (contract — see D-P12-03; do not invent)
         Pinoy Loan Manager API / UI (not implemented)
                ↓
         ExItS_PinoyLoanManager (product only; not created)
```

Surfaces (agreed direction, not implemented): Platform Admin (SaaS only) · Organization Web (full ops) · MAUI Hybrid (field subset) · ExItS Personal (borrower presentation). Detail: [Architecture/application-surface-model.md](Architecture/application-surface-model.md).

Pinoy Loan Manager must never take a project or database dependency on PinoyBusinessPOS.

**Hosting:** this product is intended to follow the portfolio hosting model — hosted multi-tenant SaaS as default (**D-HOST-01**), dedicated hosting optional, customer on-prem as a special mode. Same PLM architecture/source across modes; no customer forks. Hosted infrastructure is **not** implemented. Product implementation remains paused. See [hosting-and-deployment-operating-model.md](../../../../docs/Product-Foundation/hosting-and-deployment-operating-model.md).

## Responsibility boundary

| Area | Platform | This product |
|---|---|---|
| Identity / accounts / Platform auth | Yes (**R-091 Closed for Phase 13 scope**) | Consume trusted actor only |
| Organizations / memberships | Yes | Guid reference + isolation |
| Product catalog / plans / subscriptions / entitlements | Yes | Enforce; no Platform table reads |
| SaaS billing / Platform administration / Platform audit | Yes | No |
| Borrower operational records | No | Yes (future) |
| Loan-domain state and workflows | No | Yes (future) — Traditional and Quick origination; one core Loan after disbursement |
| Loan operational financial state | No | Yes (future) — operational Loan subledger separate from Cash Accountability; not a complete GL |
| Product-local authorization | No | Yes (future; PLM Authorization Policy v1 — PLM-D-00-06 Closed for MVP) |
| Product DB / migrations | No | Yes (future) |
| Product API / Web UI / MAUI UI / reports / product audit | No | Yes (future) |

## Personal / Borrower model (requirement — not implemented)

ExItS Personal is Platform-owned and product-neutral.

```text
ExItS Personal
      |
      +-- PinoyBusinessPOS
      |      +-- Customer relationship
      |
      +-- Pinoy Loan Manager
      |      +-- Borrower relationship
      |
      +-- future independent ExItS product
             +-- future product-specific relationship
```

Rules (planning; not designed as schema):

- one Personal identity may participate in multiple products
- each product owns its own local relationship
- POS Customer != Loan Borrower
- POS customer status never auto-creates a Loan borrower
- Pinoy Loan Manager never reads POS Customer tables
- a borrower may exist without ExItS Personal
- Personal linking is optional
- EX ID / QR resolution identifies only
- resolution alone never links
- an active Personal relationship requires explicit Personal consent
- Loan data remains Loan-product-owned
- Personal may eventually consume authorized Loan information through Loan APIs/contracts only

Detail: [Product/borrower-model.md](Product/borrower-model.md), [Product/borrower-identity-and-duplicate-policy.md](Product/borrower-identity-and-duplicate-policy.md), [Product/personal-borrower-linking.md](Product/personal-borrower-linking.md), [Product/personal-linking-lifecycle-and-visibility.md](Product/personal-linking-lifecycle-and-visibility.md), [Architecture/personal-integration-boundary.md](Architecture/personal-integration-boundary.md).

Do **not** design the final generic Platform relationship schema here (**PLM-D-00-04 Open / External Platform relationship-schema dependency**). PLM Personal/Borrower **behavior and contract requirements** are **Closed** under **PLM-D-00-05**; Platform implementation and transport remain external. Detail: [Architecture/personal-link-and-consent-contract.md](Architecture/personal-link-and-consent-contract.md), [Decisions/ADR-019-platform-personal-contract-requirements.md](Decisions/ADR-019-platform-personal-contract-requirements.md).

## Product modules

Planning modules only. None are designed or implemented. MVP calculation, calendar, penalty, settlement, refund, and cash-control **engine** rules are recorded (PLM-DOC-02–04); default rates remain undefined. Origination: [Product/lending-operating-model.md](Product/lending-operating-model.md). Authorization: [Security/role-and-grant-baseline.md](Security/role-and-grant-baseline.md). Borrower / Personal: [Product/borrower-model.md](Product/borrower-model.md), [Architecture/personal-integration-boundary.md](Architecture/personal-integration-boundary.md). Cash / daily ops: [Product/cashier-and-collector-control-model.md](Product/cashier-and-collector-control-model.md), [Product/daily-operational-workflow.md](Product/daily-operational-workflow.md), [Product/cash-variance-and-session-close-policy.md](Product/cash-variance-and-session-close-policy.md). Financial planning: [Product/financial-calculation-baseline.md](Product/financial-calculation-baseline.md), [Product/schedule-and-collection-calendar-policy.md](Product/schedule-and-collection-calendar-policy.md), [Product/early-settlement-and-principal-prepayment-policy.md](Product/early-settlement-and-principal-prepayment-policy.md), [Architecture/loan-ledger-and-balance-model.md](Architecture/loan-ledger-and-balance-model.md), [Architecture/operational-subledger-and-accounting-boundary.md](Architecture/operational-subledger-and-accounting-boundary.md).

| Module | Responsibility | Notes |
|---|---|---|
| Product access / isolation | Independent subscription, org isolation, commercial gate | Depends on D-P12-03; no Platform table reads |
| Product-local authorization | Loan presets + explicit grants | **PLM Authorization Policy v1** (PLM-D-00-06 Closed for MVP); no role-name hard-coding |
| Borrower foundation | Product-local borrower records | Optional Personal link; **PLM-D-00-04** external; **PLM-D-00-05 Closed** for PLM contract; [Product/borrower-model.md](Product/borrower-model.md) |
| Loan product configuration | Traditional products and Quick Loan Templates | Templates are organization-configured, not built-in types. Traditional: [Product/loan-product-configuration.md](Product/loan-product-configuration.md) |
| Application / approval | Traditional application and Quick Loan Request | Manual approval default; no auto-approval |
| Origination / disbursement | Starting a loan and releasing funds | Approved ≠ Disbursed; office or collector; cash availability and readiness checks |
| Shared Loan core | Ledger, balances, schedule, payments, penalties, collections, settlement, audit | One engine after disbursement |
| Schedule / calculation engine | Schedules and calculations | MVP methods (PLM-DOC-02); calendar accepted (PLM-DOC-03); rounding Closed (PLM-D-00-12) |
| Payment posting | Applying receipts | Partial/multiple payments; oldest-due; component order Interest → Principal → Fees → Penalties |
| Collector cash / reconciliation | Float, Cashier Session, remittance, variance | Separate from loan ledger; unresolved variance remains visible |
| Collections / delinquency | Arrears, exceptions, waivers, reversals | Separate from lifecycle; penalty engine accepted; no hard-coded rate |
| Reporting / documents | Product reports and documents | MVP report formulas and aging definitions **accepted** — [Product/reporting-kpi-and-aging-policy.md](Product/reporting-kpi-and-aging-policy.md), [Decisions/ADR-015-documents-receipts-and-reporting-policy.md](Decisions/ADR-015-documents-receipts-and-reporting-policy.md); [Product/reporting-baseline.md](Product/reporting-baseline.md) |
| Security / audit / privacy | Product audit, consent, classification | See [security.md](security.md) |
| Offline / MAUI field capabilities | MVP online authority; read-only cache and drafts in planning; offline final posting deferred | Server remains authoritative; implementation not authorized |

## Data ownership

| Data | SoR | Cross-boundary |
|---|---|---|
| Platform Org / User / Personal ids | Platform | Guid / contract only — no FK |
| Product operational entities (borrower, loan-domain, operational money, collector cash, product audit) | Product DB | Never in Platform DB; never in POS DB |
| Commercial subscription state | Platform | Via approved contract only (D-P12-03 open) |
| POS Customer / POS operational data | PinoyBusinessPOS | Pinoy Loan Manager must not read |

## Organization isolation

- Server will derive/validate org context; do not trust client org ids as authority alone.
- Cross-org access: conceal using the Product Foundation default (404). This is isolation behavior, not a Loan business rule.
- No shared operational DB with other products.
- Multi-branch organizations are in-scope from the beginning; operational records may be branch-scoped later. Schema is not designed.
- Initial operating currency: PHP. MVP may be PHP-only. Multi-currency implementation is not authorized.

## Isolation rules (non-negotiable)

Recorded as required intent. Not implemented.

- [x] No cross-product FKs
- [x] No direct Platform table reads from this product
- [x] No Platform reads of this product’s operational tables
- [x] No shared authoritative operational database
- [x] No direct POS database reads (additional PLM rule)

## Client direction (proposed — not authorized)

Agreed split (not implemented). Detail: [Architecture/application-surface-model.md](Architecture/application-surface-model.md).

| Surface | Proposed direction |
|---|---|
| Platform Admin Web | Existing unified Platform Admin — SaaS control plane only |
| Organization Web | Blazor Web — full operational application |
| Mobile / Desktop | .NET MAUI Blazor Hybrid — limited field / collector application |
| ExItS Personal | Existing Personal — borrower presentation only |

Possible later native MAUI capabilities (not designed):

- secure storage
- camera / document capture
- biometrics
- connectivity
- notifications
- SQLite / offline support

Web / MAUI component-sharing strategy is **Closed** (PLM-D-00-09). Target source/project layout is **Closed** (PLM-D-00-03). See [Architecture/web-maui-component-sharing-policy.md](Architecture/web-maui-component-sharing-policy.md). **No client project** is authorized until **Gate A** documentation merge and explicit Product Owner implementation authorization. Future project names: [Architecture/source-and-project-layout.md](Architecture/source-and-project-layout.md). Offline: [Architecture/mobile-and-offline-operating-model.md](Architecture/mobile-and-offline-operating-model.md), [Architecture/mobile-offline-boundary.md](Architecture/mobile-offline-boundary.md). Contracts: [Architecture/api-and-contract-boundary.md](Architecture/api-and-contract-boundary.md). Persistence: [Architecture/persistence-and-database-boundary.md](Architecture/persistence-and-database-boundary.md). Commercial: [Architecture/platform-commercial-integration.md](Architecture/platform-commercial-integration.md).

## External integrations

| System | Direction | Contract | Notes |
|---|---|---|---|
| ExItS Platform | both (future) | Approved APIs/contracts only | Identity, org context, catalog/subscription/entitlements. Transport open (D-P12-03). |
| ExItS Personal | both (future) | Approved APIs/contracts only | Optional linking; consent required; no auto-link from EX ID / QR. **PLM-D-00-04** external schema; **PLM-D-00-05 Closed** for PLM contract |
| PinoyBusinessPOS | none | None | No project dependency; no table reads; no FKs. |

## Deployment boundary

| Artifact | Name / notes |
|---|---|
| Product image | Independently versioned when packaging is authorized — **implementation deferred** |
| Platform images | Separate — do not fork per customer |
| Persistent DB | `ExItS_PinoyLoanManager` (logical name Closed, PLM-D-00-02; not created) |
| Config | Environment / secrets — not source forks |
| Source / project layout | **Closed for approved target architecture/layout** (PLM-D-00-03) — [Architecture/source-and-project-layout.md](Architecture/source-and-project-layout.md); projects not implemented on main; product packaging/image implementation deferred |

Detail: `deployment-notes.md` when packaging begins. Not created in this package.

## Observability and background work

| Concern | Approach |
|---|---|
| Logging / correlation | **Required by Product Foundation** — tenant/product/organization/correlation-aware observability; exact implementation/tooling remains future implementation/Production work; no secrets, card data, or PHI in logs |
| Metrics / health | Same — required direction documented; tooling selection deferred to implementation |
| Background jobs | Product-owned workers only when authorized; no shared Hangfire DB with other products |

## Explicit non-goals

- Implementing code, projects, databases, migrations, APIs, UI, Docker, or solution entries in this documentation package
- Inventing default peso/percent rates
- Calendar, penalty **amounts**, or post-maturity **rates** (engine accepted; no defaults)
- Designing the generic Platform relationship schema (**PLM-D-00-04** external)
- BNPL or buy-now-pay-later lending modules (explicitly out of PLM scope)
- Copying PinoyBusinessPOS architecture, grants, or money models
- Claiming overall portfolio **Production Ready**
- Claiming PLM production-security or legal compliance certification
- Treating Dev/Testing commercial shortcuts as the production design (**D-P12-05 Closed / satisfied for authentication honesty**)
- Inventing PLM-specific production authentication — consume trusted Platform identity (**R-091 Closed for Phase 13 scope**); residual MFA enforcement, step-up authentication, enterprise SSO/AD, outbound auth delivery, device hardening, and other security work remain future gates and do **not** reopen R-091
- Using Platform Admin as the borrower-loan operations UI
- Duplicate Traditional vs Quick financial engines
- Implicit role hierarchy or client-only authorization
- Silent deletion of posted financial events or unexplained cash-balance edits
