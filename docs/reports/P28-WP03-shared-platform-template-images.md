# P28-WP03 — Shared Platform Template Images + Org-Safe Adoption/Overrides

| Field | Value |
|---|---|
| Status | **Code Complete / Validation Pending** |
| Phase | Phase 28 — Open |
| Starting SHA | `14a5c4c62597788e010506414071514dd2a74fb7` |
| Feature commit(s) | `957ab6f4` (catalog) · `3611665d` (maui) · docs commit *(after push)* |
| Device Verified | **No** |
| Browser Verified | **No** |
| Production Ready | **No** |

## Delivered capability

### Platform shared template image (V1)

- One shared WebP image per Platform `GlobalProduct` (`catalog.global_product_images`).
- Magick.NET server pipeline: validate magic bytes (JPEG/PNG/WebP; HEIC rejected), max size/dimensions, AutoOrient, strip metadata, fit without stretch, thumb (~200px) + medium (~800px) WebP variants.
- Local filesystem object store (`PlatformMedia:RootPath` / `App_Data/platform-product-images`); provider-neutral interface for future object storage/CDN (**not deployed**).
- Platform Admin product create/edit: preview, choose/upload, replace, remove, save (no merchant-selectable compression; no crop/rotate UI in V1).
- APIs: admin `PUT/DELETE/GET …/global-catalog/products/{id}/image`; merchant `GET …/catalog/products/image-meta` and `GET …/catalog/products/{id}/image/{variant}` (authenticated, Active-only reads).

### Template → organization product

- Import/adoption sets `PlatformGlobalProductId`; **does not** copy image files or insert `pos.product_images`.
- Duplicate `PlatformGlobalProductId` in an org is skipped (no SKU/barcode overwrite).
- Nullable `PlatformBarcode` (template/manufacturer GTIN snapshot at import; historical rows stay null).
- Nullable `PlatformImageVersion` snapshot hint; live list/get refreshes Platform image meta when reachable.

### Org merchant override

- `pos.product_images` remains override-only.
- Display resolution: merchant override → shared Platform template image → placeholder.
- "Use standard image" clears override only; never deletes Platform asset.
- Storefront and catalog DTOs carry `HasImage` / `ImageVersion` / `ImageSource` only (no bytes/base64).

### Offline / MAUI

- Private thumbnail cache under app data (`media/product-image-cache`); platform keys `platform_{globalProductId}_v{version}_thumb.webp`.
- Explicit template adoption best-effort thumb prefetch (failure never blocks import).
- Queueable offline org product create (`catalog.product.create`): metadata JSON in outbox; pending photos in `media/pending-product-images` (never SQLite bytes); sync product first, then upload image.
- `/catalog/products/new` Queueable; catalog list/import/edit remain OnlineRequired.

## Explicit exclusions

- Production object storage / CDN deployment
- Generic SHA-256 content-hash physical dedup (documented future optimization)
- HEIC decode, crop/rotate editors, multi-image gallery
- Per-product storefront exposure flag
- Device/browser/production validation
- Pre-existing unrelated MAUI/Admin guard-test failures (Sales checkout stepper copy, dashboard Statistic assertions, etc.)

## Persistence / migrations

| Database | Migration | Change |
|---|---|---|
| Platform | `20260817220000_AddGlobalProductImages` | `catalog.global_product_images` (one row per global product) |
| POS | `20260817210000_AddProductPlatformBarcodeAndImageVersion` | `pos.products.platform_barcode`, `platform_image_version` (nullable) |

Both migrations apply/rollback/re-apply verified via Testcontainers integration tests. `dotnet ef migrations has-pending-model-changes` reports no pending model drift.

## Build / test evidence

| Target | Result |
|---|---|
| `dotnet build ExItS.slnx -c Release` | **Succeeded** — 0 errors, 15 warnings (NU1510/NU1903/CS0067 pre-existing) |
| Platform unit tests | **987 passed**, 0 failed |
| POS unit tests | **1008 passed**, 0 failed |
| Platform Admin unit tests (P20 global catalog) | **4 passed** (image UI guard included); full suite has **5 pre-existing failures** unrelated to this package |
| MAUI tests (catalog/offline/image filters) | **92 passed**; full suite **487 passed / 5 failed** (pre-existing unrelated guards) |
| Platform integration `AddGlobalProductImages` | **1 passed** |
| POS integration barcode/image migrations | **2 passed** |
| New image-focused unit tests | Platform GlobalProductImage (8), POS SharedTemplateImage + ProductImage extensions |

## Security / tenant boundaries

- Platform image mutation requires `ManageGlobalProducts`; merchant reads require authenticated Platform session and Active product for discovery endpoints.
- Org image mutation requires org `ManageCatalog`; org A cannot read/mutate org B override files.
- Shared Platform image exposure follows template/storefront authorization; shared image does not share price/SKU/stock/inventory.

## Architecture reused

- Existing Phase 28 org `CatalogProductImage` / `pos.product_images` / Magick.NET WebP / `IProductImageObjectStore` pipeline.
- POS proxies Platform shared bytes via `IPlatformMerchantCatalogClient` + `PlatformSession` HTTP (no cross-database access).
- Offline queue / dispatcher patterns from P7 customer/credit sync.

## Docs updated

- [product-images-and-storefront-availability.md](../engineering/product-images-and-storefront-availability.md) (canonical)
- data-authority-matrix, offline-sync-design, customer-ordering-pickup-and-delivery
- product-catalog specs 01–05, 07; phase-28, portfolio-progress, reports README
- FILE-MANIFEST

## Exact next

**P28-WP10** E2E validation and Phase 28 closeout (device/browser evidence, per-product exposure flag, automated settlement rails, migration apply evidence as needed). Phase 27 remains Open.
