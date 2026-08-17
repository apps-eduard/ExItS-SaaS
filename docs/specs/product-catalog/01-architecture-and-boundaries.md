# Product Catalog Architecture and Boundaries

**Purpose**  
Define how the ExItS Platform global catalog integrates with PinoyBusinessPOS without violating product ownership, database isolation, authorization, or operational independence.

---

| Field | Value |
|---|---|
| Status | Proposed |
| Phase | Phase 20 |
| Work Package | P20-WP01 |
| Document Type | Architecture Specification |

---

## 1. Goals

- Centralize reusable product definitions on the Platform.
- Keep each organization’s operational catalog inside POS.
- Allow fast template-based onboarding.
- Preserve local price, stock, tax, category, and active-state control.
- Keep checkout independent from Platform availability.

---

## 2. Ownership Model

| Area | Platform | POS Organization |
|---|---:|---:|
| Global product definition | Owner | Read/import only |
| Global category definition | Owner | Read/import only |
| Business templates | Owner | Select/import only |
| Local product | No | Owner |
| Local category | No | Owner |
| Selling price | Suggested only | Owner |
| Cost | No | Owner |
| Tax setting | No | Owner |
| Inventory and movements | No | Owner |
| Product active/inactive | No | Owner |
| Sales and receipts | No | Owner |

---

## 3. High-Level Architecture

```text
Employees / Merchants
        |
        +-----------------------------+
        |                             |
Platform Admin                  POS Mobile / Web
        |                             |
Platform API                    POS API
        |                             |
Platform PostgreSQL             POS PostgreSQL
  catalog.*                       pos.*
        |                             |
        +---- catalog contract -------+
             HTTPS / authenticated
             no shared tables
             no cross-database FK
```

---

## 4. Global-to-Local Product Flow

```text
Platform GlobalProduct
        |
        | authenticated import contract
        v
POS Product snapshot
- PlatformGlobalProductId
- imported name
- imported default SKU/barcode (org-owned after create)
- PlatformBarcode (template/manufacturer GTIN snapshot; not overwritten by org edits)
- imported unit
- shared Platform image **reference** (not a copied file)
- optional merchant image override
- initial selling price
- source metadata
        |
        +--> POS category mapping
        +--> existing inventory workflow
        +--> cashier selling flow
```

---

## 5. Required Boundary Rules

1. `PlatformGlobalProductId` is an external identifier only.
2. No EF relationship or database FK may target the Platform database.
3. POS must continue selling when Platform is unavailable.
4. Product import copies a snapshot of operational fields.
5. Platform updates may be offered as optional non-destructive suggestions.
6. Platform must never overwrite local price, stock, tax, cost, local name, category, or active state.
7. Inventory remains authoritative in existing POS inventory tables and services.
8. Organization isolation applies to every local product and import operation.
9. Platform product visibility does not imply POS operational permission.
10. Product-local roles and grants remain authoritative for POS actions.

---

## 6. Suggested Catalog Source Metadata

```text
CatalogSource
- Manual
- Template
- GlobalSearch
- BulkImport
```

Suggested local metadata:

```text
PlatformGlobalProductId UUID?
PlatformTemplateId UUID?
CatalogSource CatalogSource
CatalogImportedAt timestamp?
CatalogSnapshotVersion integer?
```

These fields support traceability only. They do not transfer ownership to the Platform.

---

## 7. Category Mapping

```text
Platform GlobalCategory
        |
        v
POS local category
- optional SourceGlobalCategoryId
- local name
- local sort order
- local active state
```

Rules:

- Import maps to an existing local category when a safe source mapping exists.
- Otherwise import creates a local category.
- Merchant may rename or deactivate the local category.
- Platform category edits must not silently reorganize existing local catalogs.

---

## 8. Security and Authorization

### Platform permissions

- `ViewGlobalCatalog`
- `ManageGlobalCategories`
- `ManageGlobalProducts`
- `ImportGlobalProducts`
- `ManageCatalogTemplates`
- `PublishCatalogTemplates`
- `ReviewProductRequests`

### POS organization permissions

- `ViewCatalogSuggestions`
- `ImportCatalogProducts`
- `ManageProducts`
- `ManageCategories`
- `AdjustInventory`

Role names must not be hard-coded. Authorization must be permission-based.

---

## 9. Failure Behavior

| Failure | Required behavior |
|---|---|
| Platform unavailable during selling | Selling continues using POS data |
| Platform unavailable during import | Show retryable import failure |
| Duplicate barcode/SKU | Partial success with explicit result |
| Unauthorized import | 403 or concealed result according to existing policy |
| Cross-organization ID | Conceal/not found according to existing policy |
| Import repeated | Idempotent result; do not duplicate products |

---

## 10. Acceptance Criteria

- [ ] Separate database ownership is preserved.
- [ ] No cross-database FK exists.
- [ ] Imported products are local snapshots.
- [ ] Checkout uses only POS-owned operational data.
- [ ] Local price and stock are not overwritten by Platform changes.
- [ ] Organization isolation tests exist.
- [ ] Authorization tests cover Platform and POS boundaries.

---

## 11. Related Documents

- `02-data-model-and-migrations.md`
- `03-api-contracts.md`
- `05-merchant-onboarding-and-import.md`

---

**Document Owner**: Architecture / Engineering  
**Last Updated**: 2026-08-04
