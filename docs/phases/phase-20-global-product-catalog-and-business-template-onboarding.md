# Phase 20 — Global Product Catalog and Business Template Onboarding

[Specs](../specs/product-catalog/phase-20-global-product-catalog-and-business-template-onboarding.md) | [Portfolio](../portfolio-progress.md) | [Phase 19](phase-19-mobile-pos-operations-and-cashier-experience.md)

| Field | Value |
|---|---|
| Status | **Open** |
| Overall | Implementation in progress → Validation Pending |
| Device Verified | **No** |
| Production Ready | **No** |
| Phase 19 | Remains **Open** (unchanged) |

## Objective

Platform-owned global merchandise catalog (categories, products, business templates, bulk import) with POS-owned local snapshots for merchant onboarding and cashier selling. Separate databases; no cross-DB FKs; Platform never overwrites local price/stock/tax/name/category/active status.

## Work packages

| WP | Name | Target status |
|---|---|---|
| P20-WP01 | Architecture and contracts | Code Complete (docs + reconciliation) |
| P20-WP02 | Global categories and products domain | Code Complete |
| P20-WP03 | Platform Admin catalog management | Code Complete |
| P20-WP04 | Business templates | Code Complete |
| P20-WP05 | Platform CSV/XLSX bulk import | Code Complete |
| P20-WP06 | Merchant onboarding and POS import | Code Complete |
| P20-WP07 | MAUI catalog and cashier integration | Code Complete |
| P20-WP08 | End-to-end validation and user closeout | In Progress — User Physical-Device Validation Pending |

## Authoritative specs

All files under `docs/specs/product-catalog/`.

## Route reconciliation

Commercial SaaS catalog already owns `/api/v1/platform/catalog/products` and `/plans`. Phase 20 global merchandise APIs use:

```text
/api/v1/platform/global-catalog/...
```

Merchant discovery remains `/api/v1/catalog/...`. POS imports remain `/api/v1/pos/catalog-imports/...`.

## Progress

| WP | Report | Notes |
|---|---|---|
| P20-WP01 | [P20-WP01](../reports/P20-WP01-architecture-and-contracts.md) | Route prefix reconciliation; commercial catalog untouched |
| P20-WP02 | [P20-WP02](../reports/P20-WP02-global-categories-and-products-domain.md) | Domain + API + `catalog` schema migration `AddGlobalProductCatalog` (`ad93c19`) |
| P20-WP03 | [P20-WP03](../reports/P20-WP03-platform-admin-catalog-management.md) | Admin Ant Design Categories + Products UI; Imports/Templates nav deferred (`7a8c1b8`) |
| P20-WP04 | [P20-WP04](../reports/P20-WP04-business-templates.md) | Business templates domain + API + Admin builder; migration `AddCatalogTemplates` (`aea02e3`) |
| P20-WP05 | [P20-WP05](../reports/P20-WP05-bulk-catalog-import.md) | CSV/XLSX bulk import + PostgreSQL worker; migration `AddGlobalCatalogImportJobs` (`5f68258`) |
| P20-WP06 | [P20-WP06](../reports/P20-WP06-merchant-onboarding-and-import.md) | POS catalog import APIs + worker + merchant discovery products/categories |
