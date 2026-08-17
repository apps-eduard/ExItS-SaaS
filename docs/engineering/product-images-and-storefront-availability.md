# Product images and Personal storefront availability

[Phase 28](../phases/phase-28-customer-ordering-pickup-and-delivery.md) | [Customer ordering](customer-ordering-pickup-and-delivery.md) | [Product units / inventory](product-units-and-inventory-behavior.md) | [Product catalog specs](../specs/product-catalog/01-architecture-and-boundaries.md)

Status: **Code Complete / Validation Pending**. Not Device Verified, Browser Verified, or Production Ready. CDN and production object storage are **not** deployed. Generic content-hash dedup is **not** implemented.

## Display resolution (V1)

One primary image per product, chosen in this order:

1. Organization merchant override (`pos.product_images`)
2. Shared Platform template image on the referenced `GlobalProduct` (`catalog.global_product_images`)
3. Bundled/local placeholder

10,000 organizations adopting the same Platform template reference the **same** storage object. Import/adoption must **not** copy image files or create a `product_images` row per organization.

Templates and global products with no image remain valid and show a placeholder.

## Platform template image

V1 allows **one** default/shared image per Platform `GlobalProduct` (the template product identity). It is Platform-owned, not copied into each organization.

Platform Admin product create/edit (`/admin/global-catalog/products`): preview, choose/upload, replace, remove, save. Compression/type are not merchant-selectable. Crop/rotate editors are not in V1; the server `AutoOrient`s and strips EXIF.

Accepted uploads (magic bytes, not filename): JPEG/JPG, PNG, WebP. HEIC/AVIF are rejected. Magick.NET is not used as a fragile HEIC converter.

### Server processing (authoritative)

Never trust the upload as-is. Same pipeline for Platform shared images and org overrides:

validate size/type/dimensions → reject malformed/extreme/decompression-bomb-like input → AutoOrient → strip metadata → fit while preserving aspect ratio (never stretch) → WebP variants.

| Variant | Target | Quality | Role |
|---|---|---|---|
| `thumb` | max edge ~200px | ~78 | lists, cart, review, explicit-adopt cache |
| `medium` | max edge ~800px | ~80 | merchant/admin preview |

Upload max **10 MB**.

**Libraries:** `Magick.NET-Q8-AnyCPU` in Platform Infrastructure **and** POS Infrastructure (no shared media project; no Magick in MAUI/UI).

### Storage

PostgreSQL stores **metadata/version/reference only**. No image BLOBs. No base64 in catalog/storefront/template JSON. No user filenames as storage paths.

| Kind | Table | Files | Keys |
|---|---|---|---|
| Shared Platform | `catalog.global_product_images` | `IGlobalProductImageObjectStore` | `global-products/{storageKey}/thumb-v{N}.webp` |
| Org override | `pos.product_images` | `IProductImageObjectStore` | `products/{storageKey}/thumb-v{N}.webp` |

V1 providers are local filesystems: `PlatformMedia:RootPath` or `{ContentRoot}/App_Data/platform-product-images`; `PosMedia:RootPath` or `{ContentRoot}/App_Data/product-images` (gitignored). Interfaces are shaped for a future S3/Azure/CDN provider. **Do not treat this as production object storage or a CDN.**

Replace: write new version files → persist metadata → delete old files. Failed processing must not change the active image.

Physical-byte content-hash dedup is a documented future optimization. Shared template-image reuse is mandatory and is **reference reuse**, not hash-based sharing. Hash dedup must never imply authorization sharing.

## Template → organization product

When an org downloads/adopts a Platform template:

- Snapshot name, default SKU/barcode, unit, suggested price, Platform ids.
- Set `PlatformGlobalProductId` (and optional `PlatformTemplateId`).
- Set `PlatformBarcode` from the template/manufacturer GTIN at import (nullable on historical rows; **not** backfilled from org `Barcode`).
- **Do not** duplicate the shared image file or insert `pos.product_images`.

Duplicate `PlatformGlobalProductId` in the same org is skipped. Names matching is not a merge key.

## SKU / barcode ownership

Platform template data is reference/default data. Organization operational identifiers stay organization-owned.

| Field | Owner | Behavior |
|---|---|---|
| Org `Sku` | Organization | Prefill on **new** import only; later template updates must not overwrite |
| Org `Barcode` | Organization (scan code) | Prefill on **new** import only; later template updates must not overwrite |
| `PlatformBarcode` | Platform snapshot at import | Canonical/template GTIN; org edits must not mutate it |
| Platform `GlobalProduct` barcode/SKU | Platform | Org edits never write back |

`UpdateDetails` changes org name/SKU/barcode/price only. It does not clear Platform provenance, `PlatformBarcode`, or `PlatformImageVersion`.

## Org custom image override

Org product create/edit:

- Use standard/template image (clears/deactivates only the org override; never deletes the Platform asset)
- Upload / replace custom image (server WebP pipeline)
- Remove custom image → fall back to shared Platform image if present, else placeholder

An org upload must never replace the Platform shared image. Platform image mutation requires Platform `ManageGlobalProducts`. Org image mutation requires organization `ManageCatalog`. No cross-tenant mutation or private-file leak.

Org-created products with no Platform template: custom image is primary; placeholder if none.

## Offline / MAUI

DTOs carry `HasImage`, `ImageVersion`, and (POS) `ImageSource` / `HasMerchantImageOverride` / `PlatformBarcode`. Never image bytes.

Thumbnails are **files** under app-private storage (`FileSystem.AppDataDirectory/media/product-image-cache`). Never Pictures/DCIM/Downloads. Never SQLite bytes.

Cache keys:

- Org/storefront: `{sellerOrg}_{productId}_v{version}_thumb.webp`
- Explicit adopted Platform template: `platform_{globalProductId}_v{version}_thumb.webp`

If local version equals server `ImageVersion`, use the file (zero network). A new Platform version expires the old file; orgs using the standard image need no new `product_images` row.

Do **not** eagerly download the entire Platform catalog. Fetch/cache only visible/near-visible rows, or templates the merchant **explicitly** imported/adopted (best-effort; cache failure must not block import or product create).

### Offline org-created product + photo

`/catalog/products/new` is Queueable. Metadata is encrypted outbox JSON (`catalog.product.create`). Pending originals stay in private `media/pending-product-images`. Sync: product metadata first (client `ProductId` is idempotent) → upload optimized custom image → server reprocesses to WebP → update local cache metadata. The large camera original is not kept as a permanent server original in V1.

Catalog list/import/edit remain OnlineRequired. Inventory tracking stays online-only.

Image file/download/processing operations must never join Sale, inventory, Goods Receipt, PurchaseOrder, or CustomerOrder database transactions.

## Personal storefront

Same resolution order. Storefront GET is the existing seller-capability / active-link gate; POS may proxy the shared Platform thumb. Shared template images must not share selling price, SKU, org barcode, inventory, Reserved/OnHand, category, availability, or merchant permissions.

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
- Generic SHA-256 content-hash physical dedup (authorization remains separate)
- HEIC decode, crop UI, multi-image gallery
- Per-product storefront exposure flag
- Real-device / browser / production validation
