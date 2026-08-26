# PinoyServicePro — File Manifest / Documentation Index

> Template: P12-WP03. List authoritative product docs and tracked source roots.
> Foundation: [exits-product-foundation-reference.md](../../../../docs/Product-Foundation/exits-product-foundation-reference.md)

| Field | Value |
|---|---|
| Product | PinoyServicePro |
| Last updated | 2026-08-20 |
| Implementation present | No |

## Authoritative docs (`src/Products/PinoyServicePro/Docs/`)

| Path | Purpose | Status |
|---|---|---|
| `README.md` | Doc index | Draft-complete |
| `product-definition.md` | Overview / boundaries | Draft-complete |
| `architecture.md` | Architecture | Draft-complete |
| `security.md` | Security / privacy | Draft-complete |
| `authorization-matrix.md` | Roles / grants intent | Draft-complete |
| `development-plan.md` | Delivery plan | Draft-complete |
| `roadmap.md` | Phases / WPs | Draft-complete |
| `risks-and-decisions.md` | Risks / decisions | Draft-complete |
| `FILE-MANIFEST.md` | This inventory | Draft-complete |
| `deployment-notes.md` | Deploy notes | N/A (not created in PSP-00) |
| `Product/` | Domain planning docs | Draft-complete |
| `Architecture/` | Surface/persistence/API/offline | Draft-complete |
| `Security/` | Role/grant and audit baselines | Draft-complete |
| `Decisions/` | Future ADR index | Index only |
| `Phases/` | Sequencing index | Index only |
| `Reports/` | WP evidence | PSP-00 closeout |
| `Validation/` | Readiness checklist | PSP-00 checklist |
| `Operations/` | Ops planning index | Index only |

## Product domain docs

| Path | Purpose |
|---|---|
| `Product/README.md` | Product category index |
| `Product/business-template-and-capability-model.md` | Templates and capabilities |
| `Product/core-service-operating-model.md` | Core operating model |
| `Product/booking-and-scheduling-model.md` | Booking and scheduling |
| `Product/walk-in-and-check-in-model.md` | Walk-in / check-in |
| `Product/service-job-and-work-order-model.md` | Jobs / work orders |
| `Product/customer-model.md` | Customers |
| `Product/customer-asset-model.md` | Customer assets |
| `Product/service-history-model.md` | Service history |
| `Product/service-catalog-and-pricing.md` | Services and pricing |
| `Product/estimate-and-approval-model.md` | Estimates |
| `Product/labor-parts-and-materials-model.md` | Labor / parts / materials |
| `Product/staff-and-resource-scheduling.md` | Staff and resources |
| `Product/payment-baseline.md` | Operational payments |
| `Product/reporting-baseline.md` | Reporting |
| `Product/notification-model.md` | Notifications |

## Architecture docs

| Path | Purpose |
|---|---|
| `Architecture/README.md` | Architecture category index |
| `Architecture/application-surface-model.md` | Surfaces |
| `Architecture/persistence-and-database-boundary.md` | DB isolation |
| `Architecture/api-and-contract-boundary.md` | API contracts |
| `Architecture/mobile-offline-boundary.md` | Offline policy planning |
| `Architecture/platform-commercial-integration.md` | Platform commercial/identity |

## Security docs

| Path | Purpose |
|---|---|
| `Security/README.md` | Security category index |
| `Security/role-and-grant-baseline.md` | Presets and grant catalog intent |
| `Security/audit-and-history-baseline.md` | Operational audit |

## Reports and validation

| Path | Purpose |
|---|---|
| `Reports/README.md` | Reports index |
| `Reports/PSP-00-foundation-closeout.md` | PSP-00 closeout |
| `Validation/README.md` | Validation index |
| `Validation/PSP-00-readiness-checklist.md` | Docs-only readiness |

## Source roots (high level)

| Path | Role |
|---|---|
| `src/Products/PinoyServicePro/Docs/` | **Only** authorized tree for PSP-00 |
| `src/Products/PinoyServicePro/` (code) | **Forbidden** in PSP-00 — not created |
| Solution / Platform / POS / Loan | **Out of scope** — must not be modified for PSP-00 |

## Explicitly not in this product tree

- Platform operational ownership
- Other products’ databases / domains
- Customer-specific forks
- Implementation projects, migrations, APIs, UI

## Notes

PSP-00 is documentation foundation only. Maximum honest status: Documentation Foundation Complete; Implementation Not Started; Product Owner Approval Pending (PSP-D-00-21).
