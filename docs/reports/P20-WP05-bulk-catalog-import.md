# P20-WP05 — Bulk Catalog Import

| Field | Value |
|---|---|
| Status | **Code Complete** |
| Phase | [Phase 20](../phases/phase-20-global-product-catalog-and-business-template-onboarding.md) — **Open** |
| Specs | [product-catalog/](../specs/product-catalog/) |
| Commit | `5f68258` |
| Date | 2026-08-05 |
| Device Verified | **No** |
| Production Ready | **No** |

## 1. Objective

Deliver Platform-owned CSV/XLSX bulk import for the global merchandise catalog: upload → validate/preview → explicit confirm → PostgreSQL-backed background processing → progress and error reporting. Permission: `import_global_products`.

## 2. Delivered capability

- Domain aggregates `CatalogImportJob` + `CatalogImportItem` with statuses Validated → Queued → Processing → Completed / CompletedWithWarnings / Failed
- Formula-injection protection: cells starting with `=`, `+`, `-`, `@` are detected and rejected (sanitized for storage)
- Safe CSV parser (quoted fields); XLSX via ClosedXML without formula evaluation / macro execution
- File limits: 5 MB, 5000 rows; type/extension validation (`.csv`, `.xlsx` only)
- Application use cases: `CreateCatalogImport`, `ConfirmCatalogImport`, `CatalogImportQueryService`, `ProcessCatalogImportChunk`
- Persistence schema `catalog` via migration `AddGlobalCatalogImportJobs` (`catalog_import_jobs`, `catalog_import_items`)
- Unique optional idempotency key; item-level Pending → Imported/Skipped/Failed (restart-safe)
- `CatalogImportBackgroundService` (`IHostedService` / `BackgroundService`) — no Redis
- Admin APIs under `/api/v1/platform/global-catalog/products/imports*`
- Admin UI `/admin/global-catalog/imports` with upload, preview, confirm, progress polling, error list
- Nav item **Imports** under Product Catalog (permission-gated)

## 3. Explicit exclusions

- POS merchant template/product import jobs (P20-WP06+)
- Redis / Hangfire / Quartz
- Macro-enabled `.xlsm` workbooks
- Blob storage of original upload bytes after parse (parsed rows persisted; SHA-256 retained)
- Multi-instance SKIP LOCKED claim locking (single-worker reclaim via heartbeat staleness)
- Commercial SaaS catalog routes unchanged

## 4. Persistence / migrations

- Migration: `20260804223724_AddGlobalCatalogImportJobs`
- Schema: `catalog`
- Soft job lifecycle; item status drives progress; no hard-delete of jobs in this WP

## 5. Build / test evidence

| Check | Result |
|---|---|
| Platform.Api Release build | Succeeded (0 warnings/errors) |
| Platform.Admin Release build | Succeeded (pre-existing Checkbox obsolete warnings) |
| GlobalCatalog unit tests (incl. import) | **48 passed**, 0 failed |

## 6. Security limitations

- Development-stage Platform auth remains as previously established; not production-ready.
- Import permission is Platform Admin operational only (`import_global_products`).
- ClosedXML reads cached/display values only; formula cells without cache fall back to formula text and fail formula-injection validation.

## 7. Risks / open decisions

- Concurrent multi-instance workers may race on claim without `FOR UPDATE SKIP LOCKED`.
- Large imports rely on per-row product creates (chunk size 50); bulk insert not implemented.
- Category name resolution fails closed on ambiguous names (use CategoryId).

## 8. Files / docs changed

- Domain: `CatalogImportJob*`, `CatalogImportItem*`, `CatalogImportRules`, enums/errors/audit
- Application: parsing/mapping, import use cases, DTOs, repository contracts
- Infrastructure: records/mapper/repos, ClosedXML parser, background worker, migration `AddGlobalCatalogImportJobs`
- API endpoints + DI; Admin page/nav/client/localization
- Unit tests: `CatalogImportTests.cs`
- This report + phase progress

## 9. Exact next work package

**P20-WP06** — Merchant onboarding and POS import.
