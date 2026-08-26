# PinoyServicePro — Product Definition

> Template: P12-WP03. Contract: [exits-product-foundation-reference.md](../../../../docs/Product-Foundation/exits-product-foundation-reference.md)
> Unresolved items → [risks-and-decisions.md](risks-and-decisions.md). Do not invent policy.

| Field | Value |
|---|---|
| Product name | PinoyServicePro |
| Platform product code | `pinoy-service-pro` (proposed — **Status: Open / Product Owner Decision Required**, PSP-D-00-01) |
| Docs root | `src/Products/PinoyServicePro/Docs/` |
| Status | PSP-00 Documentation Foundation Complete; Implementation Not Started; Product Owner Approval Pending |
| Last updated | 2026-08-20 |
| Implementation present | No |

## Purpose and users

- Purpose: Independently subscribed ExItS **dynamic service-business management** product. One stable product/domain supports many service-business families through capabilities, business templates, and configurable terminology — not separate industry products or customer source forks.
- Target organizations: Independently subscribed ExItS service organizations (barber, salon, spa, auto/moto/appliance/electronics repair, cleaning, tailoring, field technician/contractor, general/custom service). Multi-branch support is intended from the beginning; a single-branch organization may use one default branch.
- Target users / jobs: Organization staff via Owner / Manager / Front Desk / Service Provider / Cashier **presets** backed by explicit grants (identifiers open — PSP-D-00-18). Do not hard-code authorization to role names. Do not copy PinoyBusinessPOS or PinoyLoanManager grant sets.

PinoyServicePro is a **separate first-class ExItS SaaS product**, a sibling of PinoyBusinessPOS and PinoyLoanManager. It is not a POS module, Loan module, shared operational database, or industry-specific source fork.

```text
ExItS Platform
├── PinoyBusinessPOS
├── PinoyLoanManager
├── PinoyServicePro
└── future products
```

## Platform integration

| Concern | Owner | Notes |
|---|---|---|
| Identity / production auth | Platform | **DECISION:** R-091 open — do not claim production-secure auth. Keep Dev/Testing vs Production language honest (D-P12-05). |
| Organizations / account context | Platform | Product will store organization id as a `Guid` reference / contract only. Field name **Status: Open / Product Owner Decision Required**. |
| Catalog / plans / subscription | Platform | **Required:** independent subscription for this product only. Catalog registration of `pinoy-service-pro` is not done (PSP-D-00-01). |
| Entitlements / commercial access | Platform facts | **DECISION:** D-P12-03 commercial-state transport — do not invent. Platform entitlement does not replace ServicePro product-local authorization. |
| SaaS billing payments | Platform | Never store product operational money in Platform SaaS billing. |
| Operational workflows / roles / money | **This product** | Not implemented. Role presets + grant **intent** recorded; identifiers open (PSP-D-00-18). |

## Boundaries (checklist)

Recorded as **required intent**. Nothing below is implemented.

- [x] Independent product subscription (not shared with other products) — required intent
- [ ] Separate database `ExItS_PinoyServicePro` / schema **Status: Open / Product Owner Decision Required** (PSP-D-00-02) — proposed name only; not created
- [x] No direct Platform table reads; no cross-product FKs — required intent
- [ ] Product-local roles and grants defined — presets and grant **intent** recorded; identifiers **Open / Product Owner Decision Required** (PSP-D-00-18)
- [ ] Operational money defined separately from SaaS billing — ownership boundary recorded; schema/policy open (PSP-D-00-06, PSP-D-00-07, PSP-D-00-19)
- [x] Trusted org + product context enforced server-side — required intent; not implemented
- [x] PHI / sensitive data: default **none** unless explicitly authorized below
- [x] No customer-specific source forks (config / templates only)

Additional isolation (required intent):

- PinoyServicePro does not read PinoyBusinessPOS DB
- PinoyServicePro does not read PinoyLoanManager DB
- PinoyBusinessPOS does not own ServicePro operational data
- PinoyLoanManager does not own ServicePro operational data
- Platform does not own ServicePro operational records
- OrganizationId crosses boundaries only through approved identifiers/contracts
- approved APIs/contracts only

## Surfaces

| Surface | Ownership | Notes |
|---|---|---|
| API | Product | Product-owned ServicePro API intended. No API project authorized (PSP-D-00-03). |
| Platform Admin Web | Platform | Unified SaaS control plane. Must **not** become the normal ServicePro operations UI. |
| Organization Web UI | Product | Full administrative and operational service-management experience (preferred primary surface to evaluate). No client project authorized (PSP-D-00-03). |
| MAUI / Mobile UI | Product | Potential operational / front-desk / service-provider experience. Not a duplicate of Organization Web. Offline not inherited from POS (PSP-D-00-04). |
| Customer / ExItS Personal | Platform (presentation) | Future booking/history presentation only if explicitly authorized (PSP-D-00-05, PSP-D-00-13). |
| Reports | Product | Intended. Report contents **Status: Open / Product Owner Decision Required**. |

Surface detail: [Architecture/application-surface-model.md](Architecture/application-surface-model.md).

## Operational money

**Status: Open / Product Owner Decision Required** for schema, deposits, split payments, refunds, commissions, and GL integration (PSP-D-00-06, PSP-D-00-07, PSP-D-00-08, PSP-D-00-19).

Required / agreed direction:

- SaaS subscription / billing money remains Platform-owned (Organization → ExItS).
- Service-business operational money remains PinoyServicePro–owned (Customer → service organization) and must never become Platform `SaaSPayment*` records.
- Examples of product operational money (planning): service charges, labor charges, parts/material charges, deposits (if enabled), customer payments, refunds/adjustments (if enabled).
- Authoritative money math: decimal, not binary floating-point.
- Do not invent tax/legal/accounting compliance. Do not copy PinoyBusinessPOS payment/money domain by project reference.

Detail: [Product/payment-baseline.md](Product/payment-baseline.md).

## Product-local roles and grants (summary)

Role **presets** and grant **intent** recorded; identifiers **Open / Product Owner Decision Required** (PSP-D-00-18).

Do **not** hard-code authorization to role names. Do **not** implement implicit role hierarchy. Do **not** copy POS or Loan grant sets.

| Preset | Purpose | Key grant areas (intent) |
|---|---|---|
| Owner | Organization-level ServicePro administration | Broad org configuration, staff, audit |
| Manager | Service operations and supervision | Customers, bookings, jobs, estimates, reports |
| Front Desk / Reception | Intake, booking, check-in | Customers, bookings, walk-ins, limited job visibility |
| Service Provider / Technician | Assigned service execution | Assigned jobs, limited customer/asset context |
| Cashier | Operational payment capture | Payments / receipts within policy |

Catalog: [Security/role-and-grant-baseline.md](Security/role-and-grant-baseline.md), [authorization-matrix.md](authorization-matrix.md).

Access intersection (required, not implemented):

```text
trusted actor
+ trusted organization
+ valid Platform product access
+ allowed commercial state
+ required entitlement
+ active PinoyServicePro product-local role/assignment
+ required product-local grant
+ resource/workflow authorization
= operation allowed
```

## Privacy classification

| Class | Present? | Notes |
|---|---|---|
| PHI | No (default) | Not authorized. Do not store medical/health records under generic service notes. Future PHI industries require separate authorized compliance design. |
| PII | Expected later / not implemented | Customer names, contact information, addresses where applicable. Retention open (PSP-D-00-17). |
| Financial operational | Intended later / not implemented | Service amounts, payments, refunds, deposits if implemented. |
| Other sensitive | Expected later / not implemented | Booking history, service history, asset information, staff assignments. |

## MVP inclusions (documentation foundation)

This package records documentation-only PSP-00 work packages WP01–WP12 (see [roadmap.md](roadmap.md)). No ServicePro MVP **implementation** is approved.

Conceptual coverage includes: product identity; Platform/Product boundaries; business-template/capability model; core service operating model; booking/scheduling/walk-in/work-order; customer/asset/history; services/labor/parts/estimates; staff/resources/authorization; payments/reporting/notifications/audit; technical layout and offline boundaries; security/privacy/compliance baseline; foundation closeout.

## Explicit exclusions

- Implementation code, entities, EF configurations, migrations, database creation
- .NET projects, solution entries, tests, Docker, CI/CD, deployment
- Platform catalog registration, actual plans/pricing
- Real payment provider integration
- Tax-document issuance; BIR accredited/compliant/certified claims
- Arbitrary dynamic/EAV database architecture as primary operational model
- PinoyBusinessPOS domain reuse by project reference
- PinoyLoanManager domain reuse
- Final accounting/GL model; final commission/refund/deposit/scheduling-conflict/offline policies where unresolved
- Anonymous public booking implementation
- External notification vendors
- Production deployment / production-secure auth claims (R-091)

## Assumptions

- ExItS remains one Platform plus independently subscribed products.
- Business templates configure capabilities and presentation; they do not generate arbitrary schemas.
- Booking is a first-class capability, distinct from a completed service transaction.
- Barber and mechanic workflows are use-case references over one core domain.

## Unresolved decisions

See full register in [risks-and-decisions.md](risks-and-decisions.md). High-impact items include PSP-D-00-01 through PSP-D-00-21, plus portfolio R-091 and D-P12-03.

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
| Closeout | [Reports/PSP-00-foundation-closeout.md](Reports/PSP-00-foundation-closeout.md) |
