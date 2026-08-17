# Product Catalog Data Model and Migrations

**Purpose**  
Define Platform and POS persistence models for the global catalog, templates, import tracking, and local product snapshots.

---

| Field | Value |
|---|---|
| Status | Proposed |
| Phase | Phase 20 |
| Work Package | P20-WP01 / P20-WP02 |
| Database | PostgreSQL |

---

## 1. Platform Schema

Recommended schema:

```text
catalog
```

Recommended entities:

- `GlobalCategory`
- `GlobalProduct`
- `GlobalProductBusinessType`
- `CatalogTemplate`
- `CatalogTemplateCategory`
- `CatalogTemplateProduct`
- `CatalogProductRequest`
- `CatalogImportDefinition`
- `CatalogAuditEvent` only when not already covered by Platform audit

---

## 2. Platform Enums

```text
GlobalProductStatus
- Draft
- Active
- Archived

CatalogTemplateStatus
- Draft
- Published
- Unpublished
- Archived

BusinessType
- SariSari
- MiniGrocery
- Bakery
- Cafe
- Pharmacy
- GeneralRetail

ProductUnit
- Piece
- Pack
- Box
- Bottle
- Can
- Sachet
- Kilogram
- Gram
- Liter
- Milliliter
```

Use existing shared enum/value-object conventions when available. Do not duplicate established types.

---

## 3. GlobalCategory

Required fields:

```text
Id UUID
Name text
ParentId UUID?
IconReference text?
SortOrder integer
Status Active/Inactive/Archived
RowVersion or concurrency token
CreatedAt
CreatedBy
UpdatedAt
UpdatedBy
```

Rules:

- Supports parent/child hierarchy.
- Soft lifecycle only.
- Name uniqueness must be defined within the appropriate parent/scope.
- Categories may be linked to multiple business types.

---

## 4. GlobalProduct

Required fields:

```text
Id UUID
Name text
Description text?
Sku text?
Barcode text?
GlobalCategoryId UUID?
Unit ProductUnit
SuggestedPrice decimal(18,2)?
SuggestedCost decimal(18,2)?
ImageReference text?  (legacy; V1 shared image is catalog.global_product_images)
SearchTags text[] or normalized child table
Status GlobalProductStatus
RowVersion or concurrency token
CreatedAt
CreatedBy
UpdatedAt
UpdatedBy
```

Recommended indexes:

- normalized barcode
- normalized SKU
- product name search index
- category
- status
- business type mapping

Barcode and SKU normalization rules must be explicit and tested.

---

## 5. CatalogTemplate

Required fields:

```text
Id UUID
Name text
Slug text
Description text?
IconReference text?
PrimaryBusinessType BusinessType
Status CatalogTemplateStatus
DefaultBatchSize integer
SelectionMode Curated/Auto/Hybrid
PublishedAt timestamp?
RowVersion or concurrency token
```

---

## 6. CatalogTemplateProduct

Required fields:

```text
Id UUID
CatalogTemplateId UUID
GlobalProductId UUID
SortOrder integer
IsFeatured boolean
IsFirstBatch boolean
```

Constraint:

```text
unique(CatalogTemplateId, GlobalProductId)
```

---

## 7. POS Product Alignment

Do not create a replacement product aggregate if one already exists.

Extend the existing POS product only where required:

```text
PlatformGlobalProductId UUID?
PlatformTemplateId UUID?
CatalogSource Manual/Template/GlobalSearch/BulkImport
CatalogImportedAt timestamp?
CatalogSnapshotVersion integer?
PlatformBarcode text?  (template/manufacturer GTIN snapshot; historical null)
PlatformImageVersion int?  (hint; live ImageVersion may be newer)
ImageReference text?  (legacy; do not copy Platform image files here)
```

Rules:

- These fields are informational/external references.
- Shared Platform images are referenced, not copied per org.
- No cross-database FK.
- Existing product price fields remain authoritative.
- Existing inventory entities remain authoritative for stock.
- Existing category entity remains authoritative locally.

---

## 8. Import Tracking

Recommended POS-side import entities:

```text
CatalogImportJob
- Id
- OrganizationId
- PlatformTemplateId
- RequestedBy
- Status
- TotalCount
- ProcessedCount
- ImportedCount
- SkippedCount
- FailedCount
- StartedAt
- CompletedAt
- ErrorSummary
- IdempotencyKey

CatalogImportItemResult
- Id
- CatalogImportJobId
- PlatformGlobalProductId
- Status Imported/Skipped/Failed
- LocalProductId?
- ErrorCode?
- ErrorMessage?
```

Import tracking belongs in POS because it creates POS operational data.

---

## 9. Migration Rules

- Use additive migrations first.
- Do not alter existing price or inventory semantics.
- No destructive migration without explicit approval.
- Backfill catalog source as `Manual` for existing products.
- Backfill external IDs as null.
- Add indexes only after reviewing current query patterns.
- Preserve current PostgreSQL schema ownership and migration conventions.

---

## 10. Data Integrity Rules

- Decimal for all money values.
- Decimal for quantity where existing inventory supports fractional units.
- Barcode uniqueness is per existing POS organization rule, not automatically global.
- Global catalog barcode may be globally unique when present.
- POS local barcode conflicts must produce partial import results.
- Products with no barcode remain valid.
- Products may be uncategorized.

---

## 11. Acceptance Criteria

- [ ] Platform models exist in Platform DB only.
- [ ] POS extensions exist in POS DB only.
- [ ] Existing products are backfilled safely.
- [ ] Existing inventory remains unchanged.
- [ ] No cross-database FK exists.
- [ ] Concurrency tokens are enforced.
- [ ] Required indexes and uniqueness rules are tested.

---

**Document Owner**: Data / Engineering  
**Last Updated**: 2026-08-04
