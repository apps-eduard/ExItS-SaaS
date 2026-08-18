# Pinoy Loan Manager — Product Definition

> Template: P12-WP03. Contract: [exits-product-foundation-reference.md](../../../../docs/Product-Foundation/exits-product-foundation-reference.md)
> Unresolved items → [risks-and-decisions.md](risks-and-decisions.md). Do not invent policy.

| Field | Value |
|---|---|
| Product name | Pinoy Loan Manager |
| Platform product code | `pinoy-loan-manager` (proposed — **Status: Open / Product Owner Decision Required**, PLM-D-00-01) |
| Docs root | `src/Products/PinoyLoanManager/Docs/` |
| Status | Draft — documentation baseline only; not product-owner approved |
| Last updated | 2026-08-18 |
| Implementation present | No |

## Purpose and users

- Purpose: Independently subscribed ExItS product for **loan operations**. Exact product purpose, borrower types, loan products, and workflows are **not** defined. **Status: Open / Product Owner Decision Required** (PLM-D-00-08).
- Target organizations: Independently subscribed ExItS organizations. Specific segments and operating models are **Status: Open / Product Owner Decision Required**.
- Target users / jobs: **Status: Open / Product Owner Decision Required**. Do not copy PinoyBusinessPOS Owner / Manager / Cashier roles (PLM-D-00-06).

Pinoy Loan Manager is a **separate first-class ExItS SaaS product**, a sibling of PinoyBusinessPOS. It is not a POS module, POS feature, or POS database extension.

```text
ExItS Platform
├── PinoyBusinessPOS
├── Pinoy Loan Manager
└── future products
```

## Platform integration

| Concern | Owner | Notes |
|---|---|---|
| Identity / production auth | Platform | **DECISION:** R-091 open — do not claim production-secure auth. Keep Dev/Testing vs Production language honest (D-P12-05). |
| Organizations | Platform | Product will store organization id as a `Guid` reference / contract only. Field name **Status: Open / Product Owner Decision Required**. |
| Catalog / plans / subscription | Platform | **Required:** independent subscription for this product only. Catalog registration of `pinoy-loan-manager` is not done (PLM-D-00-01). |
| Entitlements / commercial access | Platform facts | **DECISION:** D-P12-03 commercial-state transport — do not invent. Platform entitlement does not replace Loan product-local authorization. |
| SaaS billing payments | Platform | Never store product operational money in Platform SaaS billing. |
| Operational workflows / roles / money | **This product** | Not implemented. Roles and operational-money model remain open (PLM-D-00-06, PLM-D-00-07). |

## Boundaries (checklist)

Recorded as **required intent**. Nothing below is implemented.

- [x] Independent product subscription (not shared with other products) — required intent
- [ ] Separate database `ExItS_PinoyLoanManager` / schema **Status: Open / Product Owner Decision Required** (PLM-D-00-02) — proposed name only; not created
- [x] No direct Platform table reads; no cross-product FKs — required intent
- [ ] Product-local roles and grants defined — **Status: Open / Product Owner Decision Required** (PLM-D-00-06)
- [ ] Operational money defined separately from SaaS billing — ownership boundary recorded; model open (PLM-D-00-07)
- [x] Trusted org + product context enforced server-side — required intent; not implemented
- [x] PHI / sensitive data: default **none** unless explicitly authorized below
- [x] No customer-specific source forks (config only)

Additional isolation (required intent):

- no direct POS database reads
- no direct Platform reads of Loan operational tables
- OrganizationId crosses boundaries only as an identifier/contract
- approved APIs/contracts only

## Surfaces

| Surface | Ownership | Notes |
|---|---|---|
| API | Product | Intended. No API project authorized. |
| Web UI | Product | Proposed: Blazor Web business / administration UI. No client project authorized (PLM-D-00-09). |
| Mobile UI | Product | Proposed: .NET MAUI Blazor Hybrid. Possible later native capabilities: secure storage, camera/document capture, biometrics, connectivity, notifications, SQLite/offline support. None authorized. |
| Reports | Product | Intended. Report contents **Status: Open / Product Owner Decision Required**. |

## Operational money

**Status: Open / Product Owner Decision Required** (PLM-D-00-07).

Required boundary only:

- SaaS subscription / billing money remains Platform-owned.
- Future Loan operational financial records remain Pinoy Loan Manager–owned and must never become Platform `SaaSPayment*` records.
- Entities, ledgers, posting rules, and which money types exist are **not** defined. Do not copy PinoyBusinessPOS money models.

## Product-local roles and grants (summary)

**Status: Open / Product Owner Decision Required** (PLM-D-00-06).

No Loan roles or grants are defined. Do **not** copy POS Owner / Manager / Cashier.

| Role | Purpose | Key grants |
|---|---|---|
| Not defined | Final Loan roles are not invented in this package | Not defined |

Access intersection (required, not implemented): trusted actor + trusted organization context + Platform product access + valid commercial state + required entitlement + active Loan product-local role + required Loan product-local grant + resource/workflow authorization.

Detail: [authorization-matrix.md](authorization-matrix.md).

## Privacy classification

| Class | Present? | Notes |
|---|---|---|
| PHI | No (default) | Not authorized. Do not add PHI unless a later package explicitly authorizes and designs for it. |
| PII | Expected later / not implemented | Borrower and related person data, if created later, will likely include PII. Retention and handling **Status: Open / Product Owner Decision Required**. |
| Financial operational | Intended later / not implemented | Loan operational financial records belong in this product when defined (PLM-D-00-07). |
| Other sensitive | **Status: Open / Product Owner Decision Required** | Consent records for optional Personal linking, if implemented later, would be sensitive. Schema not designed (PLM-D-00-04, PLM-D-00-05). |

## MVP inclusions

**Status: Open / Product Owner Decision Required.**

This package records only:

- documentation workspace (PLM-00-WP01, completed)
- product definition and architecture baseline (this package)

No loan MVP capability is approved.

## Explicit exclusions

- Loan implementation, entities, calculations, workflows, and business rules
- .NET projects, tests, solution entries, migrations, Docker, and deployment
- Final Loan roles/grants
- Generic Platform cross-product relationship schema
- Copying PinoyBusinessPOS domain, roles, or financial models
- Interest method/formula, amortization, loan types, payment allocation, rounding, grace periods, penalties, delinquency, approval limits, credit scoring, collateral, refinancing, restructuring, write-off, collections policy, and legal/regulatory operating rules (PLM-D-00-08)
- Production authentication (R-091)
- Final Platform→product commercial-state transport (D-P12-03)

## Assumptions

- ExItS remains one Platform plus independently subscribed products.
- Pinoy Loan Manager remains operationally isolated from PinoyBusinessPOS.
- Documentation in this `Docs/` root is the product documentation authority (D-P12-02).
- Physical source/test/deploy layout beside `Docs/` is not decided (PLM-D-00-03).

## Unresolved decisions

| ID | Question | Blocks |
|---|---|---|
| PLM-D-00-01 | Final product code / slug registration | Platform catalog, subscription bootstrap |
| PLM-D-00-02 | Final database name / schema | Persistence, migrations |
| PLM-D-00-03 | Physical source / test / deploy layout | Scaffold (PLM-01) |
| PLM-D-00-04 | Generic Platform cross-product relationship model | Personal multi-product participation |
| PLM-D-00-05 | Personal-to-Borrower linking mechanism | Borrower identity design (PLM-04) |
| PLM-D-00-06 | Loan roles and grants | Authorization (PLM-03) |
| PLM-D-00-07 | Operational financial model | Origination, payments, collections |
| PLM-D-00-08 | Loan business / calculation rules | Product configuration through collections |
| PLM-D-00-09 | Web / MAUI component-sharing strategy | Client scaffold |
| PLM-D-00-10 | Product documentation baseline completion / owner approval | Closing PLM-00 |
| D-P12-03 | Commercial-state transport | Product access enforcement |
| R-091 | Production authentication | Production readiness |
| D-P12-05 | Honest Dev/Testing vs Production language | Tied to R-091 |

## Document links

| Doc | Path |
|---|---|
| Architecture | [architecture.md](architecture.md) |
| Security | [security.md](security.md) |
| Authorization | [authorization-matrix.md](authorization-matrix.md) |
| Development plan | [development-plan.md](development-plan.md) |
| Roadmap | [roadmap.md](roadmap.md) |
| Risks / decisions | [risks-and-decisions.md](risks-and-decisions.md) |
| Manifest | [FILE-MANIFEST.md](FILE-MANIFEST.md) |
| Deployment | `deployment-notes.md` (not created; optional until packaging) |
