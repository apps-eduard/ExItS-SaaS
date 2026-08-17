# Product images and Personal storefront availability

[Phase 28](../phases/phase-28-customer-ordering-pickup-and-delivery.md) | [Customer ordering](customer-ordering-pickup-and-delivery.md) | [Product units / inventory](product-units-and-inventory-behavior.md)

Status: **Code Complete / Validation Pending** (`5083076f`, `95276a8e`). Not Device Verified, Browser Verified, or Production Ready. CDN and production object storage are **not** deployed.

## One primary image (V1)

Each `CatalogProduct` may have **one** primary image. There is no gallery, no per-variant merchandising image, and no HEIC pipeline.

Accepted uploads (magic bytes, not filename):

- JPEG/JPG
- PNG
- WebP

HEIC/AVIF are rejected. Magick.NET is not used as a fragile HEIC converter.

Merchant UI (organization product create/edit): compact Product Image section with choose image, take photo (existing MAUI `MediaPicker`), preview, replace, remove, and save with the product. Compression/format controls are not exposed. Crop/rotate editors are not in V1; the server `AutoOrient`s and strips EXIF.

MAUI may downscale very large phone photos (longest side ~1600px on Android) before upload. That is traffic optimization only. **The server remains authoritative.**

## Server processing

Never trust the upload as-is.

Pipeline: validate size/type/dimensions → reject malformed/extreme/decompression-bomb-like input → AutoOrient → strip metadata → fit while preserving aspect ratio (never stretch) → WebP variants.

| Variant | Target | Quality | Role |
|---|---|---|---|
| `thumb` | max edge ~200px | ~78 | lists, cart, review |
| `medium` | max edge ~800px | ~80 | merchant preview / larger product presentation |

Upload max **10 MB**. Targets are practical, not a reason to destroy quality.

**Library:** `Magick.NET-Q8-AnyCPU` (Apache-2.0) in POS Infrastructure only. MAUI/UI projects do not reference Magick, EF, or Npgsql.

## Metadata vs files

PostgreSQL stores **metadata only** (`pos.product_images`): image id, organization, product, server-generated `storage_key`, version, variant dimensions, content type, timestamps. No image BLOBs. No base64 in catalog/storefront JSON. No user filenames as storage paths.

Files live behind `IProductImageObjectStore`. V1 provider is a local filesystem rooted at `PosMedia:RootPath` or `{ContentRoot}/App_Data/product-images` (gitignored). The interface is shaped for a future S3/Azure provider. **Do not treat this as production object storage or a CDN.**

Keys are server-generated and versioned:

`products/{storageKey}/thumb-v{N}.webp`  
`products/{storageKey}/medium-v{N}.webp`

Path traversal and rooted/user paths are rejected.

Replace: write new version files → persist metadata → delete old files. Failed processing must not change the active image. Remove: delete metadata first, then files; storefront uses a local placeholder.

## Authorization

Product-image mutation uses the same organization + `ManageCatalog` gate as product management. Organization reads use `ViewCatalog`. Personal storefront image GET uses the same seller capability / active-link gate as the storefront catalog. Opaque URLs are not authorization. Images are **not** public anonymous content in V1.

## MAUI private thumbnail cache

Downloaded thumbnails are **files** under app-private storage (`FileSystem.AppDataDirectory/media/product-image-cache`). Never Pictures/DCIM/Downloads. Never SQLite bytes.

Cache metadata is the filename key: seller + product + version. If local version equals server `ImageVersion`, use the file (no network). A new version replaces and expires the old file. Cache is disposable and is never business truth. LRU cleanup is bounded (~300 MB) and must never touch Sale, inventory, Goods Receipt, PurchaseOrder, or CustomerOrder data.

Catalog/storefront DTOs carry `HasImage` + `ImageVersion` only. Do **not** download all images during product sync. Lazy-load visible row thumbnails. Failed download must not block shopping. List/cart/review use **thumb** only.

Image file/download/processing operations must never join Sale, inventory, Goods Receipt, PurchaseOrder, or CustomerOrder database transactions.

## Customer-facing stock

Authoritative orderable quantity is existing `InventoryAccount.AvailableQuantity` = **OnHand − Reserved**.

Server storefront fields: `TracksInventory`, `AvailableQuantity` (null when untracked), `AvailabilityStatus` (`Untracked` | `InStock` | `LowStock` | `OutOfStock`). Low-stock threshold is centralized: `CustomerStorefrontAvailability.LowStockThreshold` (5). Independent of merchant reorder level.

Display:

| State | Copy |
|---|---|
| Tracked, comfortable | `{n} available` |
| Tracked, 1–5 | `Only {n} left` |
| Tracked, ≤ 0 | `Out of stock` (add disabled) |
| Untracked / missing account | `Available` |

Never show `0 available` or `Stock not tracked` to Personal customers.

Cart: tracked quantity cannot exceed known `AvailableQuantity`; `+` disables at the cap; untracked has **no** fake maximum. Out-of-stock cannot be added. Direct `+` / `−` stepper is unchanged.

## Soft availability until Accept

Displayed/local stock is **not** a reservation. Two customers can see the same units before either order is accepted.

- Browse/submit = soft check (`EnsureAvailableAsync`)
- Seller Accept = atomic `Reserve`
- Cancel/reject releases reservation
- Complete consumes reservation for **tracked** lines only

Untracked lines remain orderable, are not reserved, and do not fabricate tracked decrements. CustomerOrder line snapshots still store product/price/qty.

Place revalidates on the server. Stale MAUI cart quantity cannot bypass availability. Insufficient stock returns structured details (`productId`, `productName`, `requestedQuantity`, `availableQuantity`). MAUI shows **Stock changed** with requested vs available and does **not** silently rewrite cart quantity. No payment/refund implication: submit remains Unpaid / manual settlement.

## Future work

- Production object storage / CDN
- HEIC decode, crop UI, multi-image gallery
- Per-product storefront exposure flag
- Real-device / browser / production validation
