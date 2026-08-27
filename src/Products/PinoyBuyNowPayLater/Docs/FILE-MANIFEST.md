# Pinoy Buy Now Pay Later — File Manifest / Documentation Index

> Foundation: [exits-product-foundation-reference.md](../../../../docs/Product-Foundation/exits-product-foundation-reference.md)

| Field | Value |
|---|---|
| Product | Pinoy Buy Now Pay Later |
| Last updated | 2026-08-27 |
| Implementation present | No |

## Authoritative docs (`src/Products/PinoyBuyNowPayLater/Docs/`)

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
| `Product/` | Domain planning docs | Draft-complete |
| `Architecture/` | Surfaces, persistence, contracts, failure | Draft-complete |
| `Security/` | Role/grant, audit, privacy baselines | Draft-complete |
| `Decisions/` | Future ADR index | Index only |
| `Phases/` | Sequencing index | Index only |
| `Reports/` | WP evidence | BNPL-00 closeout |
| `Validation/` | Readiness checklist | BNPL-00 checklist |
| `Operations/` | Ops planning index | Index only |

## Product domain docs

| Path | Purpose |
|---|---|
| `Product/README.md` | Product category index |
| `Product/commerce-and-financed-purchase-model.md` | Commerce coordination + Utang/PLM |
| `Product/customer-model.md` | Customers / Personal refs |
| `Product/financing-lifecycle.md` | State machine |
| `Product/eligibility-and-approval.md` | Eligibility |
| `Product/installment-model.md` | Installments |
| `Product/repayment-model.md` | Repayments |
| `Product/overdue-and-collections.md` | Overdue / collections |
| `Product/merchant-settlement.md` | Settlement |
| `Product/returns-cancellations-refunds.md` | Returns |
| `Product/reporting-baseline.md` | Reporting |

## Architecture docs

| Path | Purpose |
|---|---|
| `Architecture/README.md` | Architecture category index |
| `Architecture/platform-integration.md` | Platform contracts |
| `Architecture/commerce-pos-boundary.md` | POS boundary |
| `Architecture/inventory-boundary.md` | Inventory |
| `Architecture/persistence-and-database-boundary.md` | DB isolation |
| `Architecture/api-and-contract-boundary.md` | API contracts |
| `Architecture/failure-and-reconciliation.md` | Failure matrix |
| `Architecture/idempotency-model.md` | Idempotency |
| `Architecture/web-pwa-runtime-policy.md` | Online-only PWA |

## Security docs

| Path | Purpose |
|---|---|
| `Security/README.md` | Security category index |
| `Security/role-and-grant-baseline.md` | Presets / grants |
| `Security/audit-and-history-baseline.md` | Audit |
| `Security/privacy-and-sensitive-data-baseline.md` | Privacy |

## Reports and validation

| Path | Purpose |
|---|---|
| `Reports/README.md` | Reports index |
| `Reports/BNPL-00-foundation-closeout.md` | BNPL-00 closeout |
| `Reports/BNPL-01-product-scaffold-platform-registration.md` | BNPL-01 scaffold evidence |
| `Reports/BNPL-02-authorization-organization-branch-access.md` | BNPL-02 access foundation |
| `Validation/README.md` | Validation index |
| `Validation/BNPL-00-readiness-checklist.md` | Readiness checklist |

## Workspace indexes

| Path | Purpose | Implementation present |
|---|---|---|
| `src/Products/PinoyBuyNowPayLater/` | Product workspace root | Scaffold + access foundation + Docs |
| `ExItS.PinoyBuyNowPayLater.Domain` | Product identity + capability catalog | Yes |
| `ExItS.PinoyBuyNowPayLater.Application` | Access guard / context / branch scope | Yes |
| `ExItS.PinoyBuyNowPayLater.Api` | Health + `/api/v1/bnpl/access/me` | Yes |

## Not present (intentionally)

| Item | Reason |
|---|---|
| Financing / installment / repayment / settlement entities | BNPL-04+ |
| Database / migrations | BNPL-D-00-04 OPEN |
| ApiClient / Web / React Client | Deferred; PWA client later |
| Docker / deploy for BNPL | Not authorized |
| BNPL product-local grants | BNPL-02 / BNPL-D-00-18 |
