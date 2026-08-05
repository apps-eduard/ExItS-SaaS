# Phase 20 — Final Implementation Report

| Field | Value |
|---|---|
| Phase | [Phase 20](../phases/phase-20-global-product-catalog-and-business-template-onboarding.md) |
| Status | **Open** |
| Overall | **Implementation Complete — Validation Pending** |
| WP01–WP07 | **Code Complete** (automated evidence) |
| WP08 | **In Progress — User Physical-Device Validation Pending** |
| Device Verified | **No** |
| Production Ready | **No** |
| Branch | `main` (origin) |
| Date | 2026-08-05 |

## Preflight separation

| Bucket | Result |
|---|---|
| QR / Public User ID | Already on main before Phase 20 (`076512e` … `dfe135a`) |
| Specs `docs/specs/product-catalog/**` | `5c7736f` — `docs: add Phase 20 product catalog specifications` |
| `tools/p18-*.mjs` | Left untracked / untouched |
| Unrelated dirty WIP | Left unstaged throughout |

## Architecture decisions

- Platform owns global merchandise catalog + templates + Platform bulk import
- POS owns local snapshots, prices, inventory, sales
- Separate PostgreSQL DBs; external IDs only; no cross-DB FK
- Route reconciliation: `/api/v1/platform/global-catalog/*` (avoids commercial `/platform/catalog/products` collision)
- Merchant discovery: `/api/v1/catalog/*`
- POS imports: `/api/v1/pos/catalog-imports/*`
- Checkout uses local POS data only

## Schema / migrations

| Migration | Database |
|---|---|
| `AddGlobalProductCatalog` | Platform `catalog` schema — categories/products |
| `AddCatalogTemplates` | Platform — templates + composition |
| `AddGlobalCatalogImportJobs` | Platform — bulk import jobs/items |
| `AddPosCatalogImportMetadata` | POS — provenance fields + import jobs |

## APIs and Admin

- Admin: `/admin/global-catalog/categories|products|templates|imports`
- Permissions: `view_global_catalog`, `manage_global_categories`, `manage_global_products`, `import_global_products`, `manage_catalog_templates`, `publish_catalog_templates`
- Commercial `manage_catalog` unchanged

## Imports / jobs

- Platform CSV/XLSX: downloadable UTF-8 template (`CatalogImportCsvSchema`) → validate headers/rows → preview → confirm → `CatalogImportBackgroundService`
- Template filename `exits-global-product-import-template.csv`; endpoints under `/products/imports/template.csv` (+ `/imports/template.csv` alias)
- POS template/selected import: PostgreSQL jobs + `PosCatalogImportBackgroundService`
- Idempotent keys; partial success; stock remains 0 until OpeningStock

## POS / MAUI

- ManageCatalog: template onboarding, global browse, job progress, review + inventory link
- Cashier: local sell only; CSS tile placeholders; no global admin by default

## Security / authorization evidence

- Permission-gated Admin nav and APIs
- POS import gated by ManageCatalog (Cashier excluded by default)
- Org context from trusted auth; Platform IDs external refs only
- Formula-injection rejection on Platform bulk import
- Release builds do not rely on Development auth bypasses for these features

## Test totals (WP08 session)

| Suite | Passed | Failed | Notes |
|---|---:|---:|---|
| Platform Unit (GlobalCatalog filter) | 48 | 0 | |
| Platform Unit (full) | 543 | 2 | Pre-existing commercial/payment failures |
| MAUI Tests | 109 | 1 | Pre-existing InventoryPageGuard |
| WP02–WP07 focused suites | See WP reports | 0 Phase-20 regressions reported | |

## Commits by WP

See [P20-WP08](P20-WP08-end-to-end-validation-and-user-closeout.md) table.

## APK

PhysicalDevice signed APK path (rebuild in WP08):

`src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Maui/bin/Debug/net10.0-android/com.exits.pinoybusinesspos-Signed.apk`

## Known limitations / deferred

- Multi-instance `SKIP LOCKED` job claiming not implemented
- Remote product images not fetched (placeholders)
- Sari-Sari seed not auto-created in LocalValidation (create via Admin)
- Entitlement-aware merchant discovery filtering partial
- Camera QR scan still deferred (Phase 19 QR manual entry)
- Pre-existing unit test failures outside Phase 20 not fixed here

## Explicit status confirmation

**Phase 19 remains Open.**  
**Phase 20 remains Open.**  
**Device Verified: No.**  
**Production Ready: No.**  
Phone scenarios: **Retest** pending user physical confirmation.
