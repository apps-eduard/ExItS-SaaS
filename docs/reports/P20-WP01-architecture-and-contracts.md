# P20-WP01 — Architecture and Contracts

| Field | Value |
|---|---|
| Status | **Code Complete** |
| Phase | [Phase 20](../phases/phase-20-global-product-catalog-and-business-template-onboarding.md) — **Open** |
| Specs | [product-catalog/](../specs/product-catalog/) |
| Date | 2026-08-05 |
| Device Verified | **No** |
| Production Ready | **No** |

## 1. Objective

Reconcile Phase 20 product-catalog specifications with the current repository and lock ownership, external IDs, snapshots, lifecycle, concurrency, idempotency, audit, isolation, and failure behavior before domain coding.

## 2. Preflight (worktree)

| Bucket | Disposition |
|---|---|
| A. QR / Public User ID | Already committed/pushed (`076512e`, `7354fba`, `9ef5a53`, `dfe135a`) |
| B. `docs/specs/product-catalog/**` | Committed as `5c7736f` — `docs: add Phase 20 product catalog specifications` |
| C. `tools/p18-*.mjs` | Left untracked / untouched |
| Unrelated dirty files | Left unstaged (compose, AccessTokenUseCases, POS Program, Catalog/Expense/SignIn/Select, SalePageGuardTests) |

## 3. Reconciliation decisions

### 3.1 Commercial vs global merchandise catalog

Existing Platform **commercial** catalog (`Product` / `Plan` / features) already mounts:

```text
/api/v1/platform/catalog/products
/api/v1/platform/catalog/plans
```

Phase 20 specs proposed the same `/api/v1/platform/catalog/products` path for **merchandise** SKUs. That would collide.

**Decision:** Implement global merchandise under:

```text
/api/v1/platform/global-catalog/categories
/api/v1/platform/global-catalog/products
/api/v1/platform/global-catalog/templates
/api/v1/platform/global-catalog/products/imports
```

Merchant discovery stays as specified:

```text
/api/v1/catalog/templates
/api/v1/catalog/products/search
/api/v1/catalog/categories
```

POS imports stay as specified:

```text
/api/v1/pos/catalog-imports/...
```

Permission `platform.permission.manage_catalog` remains **commercial SaaS only**. New permissions:

- `platform.permission.view_global_catalog`
- `platform.permission.manage_global_categories`
- `platform.permission.manage_global_products`
- `platform.permission.import_global_products`
- `platform.permission.manage_catalog_templates`
- `platform.permission.publish_catalog_templates`

### 3.2 Schema

Platform schema `catalog` for GlobalCategory / GlobalProduct / CatalogTemplate / import definitions (separate from `platform` commercial tables).

### 3.3 Ownership matrix

| Concern | Owner |
|---|---|
| Global categories, products, templates, publication, Platform bulk import, Platform audit | Platform |
| Org-local products/categories, prices, tax, inventory, sales, local permissions | POS |
| Stock authority | Existing POS inventory only |
| Checkout data | Local POS only |

### 3.4 Snapshot contract

Import creates editable local POS product with external refs only (`PlatformGlobalProductId`, optional `PlatformTemplateId`, `CatalogSource`, `CatalogImportedAt`, `CatalogSnapshotVersion`). Platform updates never overwrite local price, stock, tax, name, category, or active status.

### 3.5 Concurrency / idempotency / failure

- Platform mutables: optimistic concurrency (`UpdatedAtUtc` / row version).
- Import commands: idempotency key per organization.
- Partial success allowed after preview+confirm.
- Transient retries bounded; validation/auth failures not retried.
- Platform outage: selling continues; import returns retryable failure.

## 4. Docs updated

- Phase 20 phase index page
- Spec API contracts note (route prefix)
- Architecture / security / authorization / implementation summary / index / portfolio (this WP + follow-on WPs)

## 5. Explicit non-claims

Phase 19 and Phase 20 remain **Open**. Not Device Verified. Not production-ready.
