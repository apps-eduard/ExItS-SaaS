# P8-WP01 — Catalog and Barcode

Phase marker: `P8-WP01-catalog-and-barcode`

## Status

**Complete with documented risks.** Organization-owned retail product catalog and barcode foundation only. Sales, inventory, stock, and offline catalog cache/queue remain excluded. **Not production-ready.** Do **not** begin P8-WP02 until explicitly authorized.

Feature commit: `5573822ca116ab46f1a5cdce407e1d7b4f58f796`

## Delivered model

- Domain: `CatalogProduct`, `ProductCategory` (flat categories only)
- Product fields: ProductId, OrganizationId, Name (required), optional Description, optional SKU, optional primary Barcode, optional CategoryId, UnitOfMeasure (required), SellingPrice (required), Status Active|Inactive, CreatedAtUtc, UpdatedAtUtc, concurrency metadata
- Category fields: CategoryId, OrganizationId, Name (required), Status, CreatedAtUtc, UpdatedAtUtc, concurrency metadata; active names unique per org (normalized)
- Catalog CRUD + Active/Inactive lifecycle (no hard delete)
- Exact SKU and barcode lookup
- Feature grants `store-catalog-view` / `store-catalog-manage` with continuity matrix
- Typed POS API under `/api/v1/pos/catalog/*`
- MAUI catalog screens (list, create/edit/detail, categories, barcode lookup)

## SKU / barcode rules

| Rule | MVP behavior |
|---|---|
| SKU | Optional; trim; uniqueness via uppercase invariant; display form preserved; charset letters/digits/hyphen/underscore/period/slash; max 64; inactive SKU remains reserved |
| Barcode | Optional; digits only; length 8–14; GS1 Mod-10 check digits for EAN-8, UPC-A, EAN-13, GTIN-14; inactive barcode remains reserved; lookup exact normalized |
| Excluded | Generation, labels, multi-barcode per product |

## Unit of measure

Controlled set (stable codes; localized labels): Piece, Pack, Box, Bottle, Can, Sachet, Kilogram, Gram, Liter, Milliliter, Meter.

## Price

Required selling price: `decimal`, ≥ 0, ≤ 2 decimal places. Single price only — no multi-price, tax/VAT, or discounts in this WP.

## Organization ownership

- System of record: PinoyBusinessPOS database `ExItS_PinoyBusinessPOS`, schema `pos`
- `OrganizationId` is a Platform organization GUID value only (no cross-database FK)
- All catalog queries/mutations scoped by organization; cross-org access fails closed (404)
- No Platform or HealthCare catalog tables in POS DB

## Auth / continuity

| Subscription state | View (`store-catalog-view`) | Manage (`store-catalog-manage`) |
|---|---|---|
| Trialing / Active / GracePeriod | Allow when granted | Allow when granted |
| PastDue / Cancelled / Expired | Allow when granted (continuity) | Deny |
| Suspended / missing / stale / unknown | Deny | Deny |

Mutations require `store-catalog-manage`. Organization scope via `X-Pos-Organization-Id`. Commercial feature grants via Development-stage headers — **not production-secure**.

## Online-only

Catalog is **online-only** for P8-WP01. No offline catalog cache, no queued catalog mutations, no local SQLite catalog projections. Offline API client calls fail fast; architecture guards assert catalog paths do not use Idempotency / OfflineOperation / LocalStore.

## Explicit exclusions

Sales/checkout, inventory/stock, suppliers/purchasing, discounts/tax/VAT, receipts/invoices, multiple prices, customer/Utang on sales, barcode generation/printing, offline catalog mutations/cache, gateways/QR/cards, POS operational roles.

## Persistence / migration

| Item | Value |
|---|---|
| Migration | `AddPosCatalogAndBarcodes` (`20260730144243`) |
| Tables | `pos.product_categories`, `pos.products` |
| Indexes | Active category name uniqueness per org; normalized SKU uniqueness per org; barcode uniqueness per org (partial when non-null) |
| Validation | Apply / rollback / re-apply via Testcontainers |

## API / MAUI

### API (`/api/v1/pos/catalog/*`)

| Area | Routes |
|---|---|
| Categories | `GET/POST /categories`, `GET/PUT /categories/{id}`, `POST .../deactivate`, `POST .../reactivate` |
| Products | `GET/POST /products`, `GET/PUT /products/{id}`, `POST .../deactivate`, `POST .../reactivate` |
| Lookup | `GET /products/by-sku/{sku}`, `GET /products/by-barcode/{barcode}` |

View → `store-catalog-view`; mutations → `store-catalog-manage`.

### MAUI

| Route | Screen |
|---|---|
| `/catalog` | Product list |
| `/catalog/products/new` | Create (optional **Track stock** when `ManageInventory`; default off — see [P8-WP04 later addition](P8-WP04-basic-inventory.md#catalog-track-stock-create--edit)) |
| `/catalog/products/{id}` | Detail |
| `/catalog/products/{id}/edit` | Edit (same **Track stock** switch, seeded from inventory `IsTracked`) |
| `/catalog/categories` | Categories |
| `/catalog/barcode-lookup` | Barcode lookup |

English + Filipino strings; DesignSystem themes/density unchanged.

## Tests / Android

| Suite | Passed | Failed | Skipped |
|---:|---:|---:|---:|
| Full `ExItS.slnx` Release | **684** | **0** | **0** |

Baseline 619 preserved and exceeded (+65). Focused coverage includes domain/SKU/barcode checksum, capability/continuity, API + migration (Testcontainers), MAUI page guards, and online-only architecture scope.

Android Release APK:

`src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Maui/bin/Release/net10.0-android/com.exits.pinoybusinesspos-Signed.apk`

Interactive device/emulator validation **not** claimed (`adb` unavailable) — **R-109 remains open**.

## Risks / open decisions

| ID / topic | Status |
|---|---|
| R-109 device validation | **Open** — no interactive Android E2E |
| Production auth / POS roles | **Open** — commercial/org headers Development-stage only |
| Online-only catalog | By design for P8-WP01; offline catalog deferred |
| R-022 / R-129 / full-DB encryption | Unchanged from Phase 7; not catalog-specific |

## Portfolio independence

Root `HealthCare/` must remain absent/untracked and outside `ExItS.slnx`.

## Documentation and Git

| Field | Value |
|---|---|
| Feature commit | `5573822ca116ab46f1a5cdce407e1d7b4f58f796` |
| Docs hash-record commit | `51963b92e3841aad3321fbcd41b6c525671a5f1f` |
| Phase marker | `P8-WP01-catalog-and-barcode` |
| Final working tree | clean after push |

Updated: phase-08, portfolio-progress, reports index, FILE-MANIFEST, README, risks, release-plan, security, data-ownership, testing-strategy, platform-product-contracts, pinoy-business-pos-requirements, this report.

## Exact next work package

**P8-WP02 — Simple Sales** completed separately; next authorized WP is **P8-WP03 — Product-Based Utang**.
