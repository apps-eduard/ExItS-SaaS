# P20 Supplemental — Dynamic Business Types & Catalog Classification

| Field | Value |
|---|---|
| Status | **Code Complete** |
| Phase | [Phase 20](../phases/phase-20-global-product-catalog-and-business-template-onboarding.md) — **Open** / **Validation Pending** |
| Date | 2026-08-10 |
| Device Verified | **No** (unchanged) |
| Production Ready | **No** |

## 1. Business Type vs Template

| Concept | Role |
|---|---|
| **Business Type** | Platform-owned classification for filtering/tagging global categories and products. Dynamic CRUD entity (`catalog.business_types`). |
| **Template** | Curated starter product pack. Still composition-based (`catalog_template_products`). Has one `PrimaryBusinessTypeId` — not an alias for a business type. |

Example: Bakery business type can power categories Bread/Cakes/Pastries and multiple templates (Starter Bakery, Standard Bakery, Bakery + Cafe).

## 2. Schema & migration

Migration: `20260810065327_AddDynamicBusinessTypes`

- Creates `catalog.business_types` (Id, Code unique, NormalizedName unique, Name, Description, Status, SortOrder, IconReference, timestamps)
- Seeds six legacy codes with stable GUIDs (`LegacyBusinessTypeSeeds`): SariSari, MiniGrocery, Bakery, Cafe, Pharmacy, GeneralRetail
- Migrates join tables from string `business_type` → `business_type_id` FK (Restrict)
- Migrates `catalog_templates.primary_business_type` → `primary_business_type_id` FK
- No cross-database FKs; POS continues to consume codes via discovery DTOs

## 3. CRUD / status rules

- Permissions: `ViewGlobalCatalog` (read), `ManageGlobalCategories` (mutate)
- Soft lifecycle: Active ↔ Inactive ↔ Archived (prefer archive over hard delete)
- Hard delete not exposed in Admin UI; referenced types remain readable historically
- Code immutable after create (stable); Name/Description/Sort/Icon editable

## 4. Category / product relationships

- Many-to-many via `global_category_business_types` / `global_product_business_types` on `business_type_id`
- Category Admin: tags column, multi-select, bulk **Add / Remove / Replace**
- Product Admin: multi-select from Active types
- Import CSV `BusinessTypes` column: resolve by Code or normalized Name; unknown tokens fail the row clearly (no enum parsing)

## 5. Templates & onboarding

- Template editor selects Primary business type from persisted Active list; archived historical references still render via id→code lookup
- Multiple templates may share one PrimaryBusinessTypeId
- Merchant discovery: `GET /api/v1/catalog/business-types` returns Active only
- Organization entities had no business-type column; no org schema change required. Template onboarding still works via existing template APIs exposing PrimaryBusinessType code string.

## 6. Privacy

No new personal/customer data. Classification is business/product metadata only.

## 7. Explicit exclusions / deferred

- Organization profile `BusinessTypeId` field (not present pre-change)
- Auto-suggest templates from business type during Start Business (still template-first)
- Closing Phase 20 / Device Verified
