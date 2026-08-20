# PinoyServicePro — Product Documentation

Authoritative product docs for **PinoyServicePro** (`pinoy-service-pro`, proposed).

Always load with:

1. `.cursor/rules/exits-workflow.mdc`
2. `.cursor/rules/exits-product-context.mdc`
3. `docs/Product-Foundation/exits-product-foundation-reference.md` (repo path)
4. Docs in this folder
5. The active work-package prompt/report
6. Files required for the task only

**Status:** PinoyServicePro — PSP-00 Documentation Foundation Complete; Implementation Not Started; Product Owner Approval Pending  
**Implementation present:** No  
**Documentation root:** `src/Products/PinoyServicePro/Docs/` (D-P12-02)

PinoyServicePro is a **separate first-class ExItS SaaS product**, a sibling of PinoyBusinessPOS and PinoyLoanManager. It is not a POS module, Loan module, shared operational database, or industry-specific source fork.

```text
ExItS Platform
├── PinoyBusinessPOS
├── PinoyLoanManager
├── PinoyServicePro
└── future products
```

Permanent product principle:

```text
One Product
+ Stable Core Domain
+ Capabilities
+ Business Templates
+ Configurable Terminology
= Different Service-Business Experiences
```

---

## Canonical documents

| Doc | Description |
|---|---|
| [product-definition.md](product-definition.md) | Purpose, ownership, boundaries, exclusions |
| [architecture.md](architecture.md) | System, data, surface, and isolation boundaries |
| [security.md](security.md) | Security, privacy, compliance posture |
| [authorization-matrix.md](authorization-matrix.md) | Access layers; role presets and grant intent; identifiers open |
| [development-plan.md](development-plan.md) | Delivery buckets and testing expectations |
| [roadmap.md](roadmap.md) | Phases and work packages |
| [risks-and-decisions.md](risks-and-decisions.md) | Open risks and `PSP-D-00-XX` decisions |
| [FILE-MANIFEST.md](FILE-MANIFEST.md) | Path inventory |

Focused planning documents (not implementation specs):

| Doc | Description |
|---|---|
| [Product/business-template-and-capability-model.md](Product/business-template-and-capability-model.md) | Templates, capabilities, terminology |
| [Product/core-service-operating-model.md](Product/core-service-operating-model.md) | Stable domain and operating flow |
| [Product/booking-and-scheduling-model.md](Product/booking-and-scheduling-model.md) | Booking lifecycle and scheduling concerns |
| [Product/walk-in-and-check-in-model.md](Product/walk-in-and-check-in-model.md) | Walk-in and arrival |
| [Product/service-job-and-work-order-model.md](Product/service-job-and-work-order-model.md) | Jobs / work orders |
| [Product/customer-model.md](Product/customer-model.md) | Service-business customer |
| [Product/customer-asset-model.md](Product/customer-asset-model.md) | Optional CustomerAsset capability |
| [Product/service-history-model.md](Product/service-history-model.md) | Durable service history |
| [Product/service-catalog-and-pricing.md](Product/service-catalog-and-pricing.md) | Service offerings and pricing baseline |
| [Product/estimate-and-approval-model.md](Product/estimate-and-approval-model.md) | Estimates / quotations |
| [Product/labor-parts-and-materials-model.md](Product/labor-parts-and-materials-model.md) | Labor, parts, materials boundary |
| [Product/staff-and-resource-scheduling.md](Product/staff-and-resource-scheduling.md) | Staff/resource assignment |
| [Product/payment-baseline.md](Product/payment-baseline.md) | Operational payments |
| [Product/reporting-baseline.md](Product/reporting-baseline.md) | Operational reporting |
| [Product/notification-model.md](Product/notification-model.md) | Notification candidates |
| [Architecture/application-surface-model.md](Architecture/application-surface-model.md) | Web / MAUI / Personal / Admin / API |
| [Architecture/persistence-and-database-boundary.md](Architecture/persistence-and-database-boundary.md) | Separate DB isolation |
| [Architecture/api-and-contract-boundary.md](Architecture/api-and-contract-boundary.md) | API and contracts |
| [Architecture/mobile-offline-boundary.md](Architecture/mobile-offline-boundary.md) | Offline as deliberate decision |
| [Architecture/platform-commercial-integration.md](Architecture/platform-commercial-integration.md) | Commercial/identity contracts; D-P12-03 open |
| [Security/role-and-grant-baseline.md](Security/role-and-grant-baseline.md) | Presets and grant catalog intent |
| [Security/audit-and-history-baseline.md](Security/audit-and-history-baseline.md) | Operational audit |
| [Validation/PSP-00-readiness-checklist.md](Validation/PSP-00-readiness-checklist.md) | Docs-only readiness checklist |
| [Reports/PSP-00-foundation-closeout.md](Reports/PSP-00-foundation-closeout.md) | PSP-00 closeout |

Category folders below are indexes only. They must not become a second source of truth.

---

## Category indexes

| Directory | Purpose |
|---|---|
| [Product/](Product/README.md) | **WHAT** — operating model and domain planning |
| [Architecture/](Architecture/README.md) | **HOW** — surfaces, persistence, contracts, offline |
| [Security/](Security/README.md) | Access, privacy, audit baselines |
| [Decisions/](Decisions/README.md) | Future ADRs — register is [risks-and-decisions.md](risks-and-decisions.md) |
| [Phases/](Phases/README.md) | Sequencing — points to [roadmap.md](roadmap.md) |
| [Reports/](Reports/README.md) | Work-package evidence |
| [Validation/](Validation/README.md) | Readiness / validation evidence |
| [Operations/](Operations/README.md) | Deployment and production operations (planning) |

Do not scatter PinoyServicePro documentation into the repository-root `docs/` tree unless the content is genuinely portfolio-wide.

---

## Identity (proposed)

| Item | Value | Status |
|---|---|---|
| Display name | PinoyServicePro | Recorded |
| Short identifier | PSP | Recorded |
| Repository directory | `PinoyServicePro` | Recorded |
| Product code / slug | `pinoy-service-pro` | Open (PSP-D-00-01) |
| Future database | `ExItS_PinoyServicePro` | Open (PSP-D-00-02) — planning name only; not created |

---

## Ownership (summary)

Platform owns identity, organizations, memberships, account/session context, product catalog, plans, subscriptions, entitlements, SaaS billing, Platform administration, and Platform audit.

PinoyServicePro will own service-business operational data: customers, bookings, jobs/work orders, staff assignments, services, optional assets, estimates, materials/parts when enabled, operational payments, service history, product-local authorization, operational audit, and reports.

Isolation: independent subscription; separate database; no cross-product FKs; no direct POS, Loan, or Platform table reads; OrganizationId as identifier only; approved contracts/APIs only; SaaS billing ≠ ServicePro operational money.

Authoritative text: [product-definition.md](product-definition.md) and [architecture.md](architecture.md).

---

## Client direction (proposed)

Organization Web: full administrative and operational service-management experience. MAUI / Mobile: potential operational / front-desk / service-provider experience. Customer / ExItS Personal: future booking/history presentation only if explicitly authorized. Platform Admin: SaaS administration only — must not become normal ServicePro operations UI. API: product-owned ServicePro API.

No client or API project is authorized in PSP-00.

---

## Explicit exclusions (PSP-00)

No implementation exists. No solution/project creation, migrations, database creation, Platform catalog registration, real payment providers, tax-document issuance, BIR compliance claims, EAV/dynamic schema architecture, POS or Loan domain reuse by project reference, final accounting/GL, final commission/refund/deposit/offline/scheduling policies where open, anonymous public booking implementation, external notification vendors, or production deployment.
