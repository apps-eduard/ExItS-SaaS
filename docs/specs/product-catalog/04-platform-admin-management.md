# Platform Admin Global Catalog Management

**Purpose**  
Define the Ant Design Blazor administration experience for global categories, global products, templates, imports, and product requests.

---

| Field | Value |
|---|---|
| Status | Proposed |
| Phase | Phase 20 |
| Work Package | P20-WP03 / P20-WP04 / P20-WP05 |
| UI | ExItS Admin — Ant Design Blazor |

---

## 1. Roles and Permissions

Use permission assignments, not hard-coded role names.

| Permission | Capability |
|---|---|
| `ViewGlobalCatalog` | Read categories, products, templates |
| `ManageGlobalCategories` | Create/edit/status categories |
| `ManageGlobalProducts` | Create/edit/status products |
| `ImportGlobalProducts` | Upload and confirm bulk import |
| `ManageCatalogTemplates` | Create/edit template composition |
| `PublishCatalogTemplates` | Publish/unpublish/archive templates |
| `ReviewProductRequests` | Resolve merchant requests |

---

## 2. Navigation

```text
Platform Administration
└── Product Catalog
    ├── Categories
    ├── Products
    ├── Imports
    ├── Templates
    └── Product Requests
```

Navigation visibility must follow permissions.

---

## 3. Categories Screen

Required capabilities:

- hierarchy/tree or parent-aware list
- search
- business-type filter
- status filter
- create category
- edit category
- activate/deactivate/archive
- sort order
- icon/image reference
- concurrency conflict handling

Required fields:

- name
- parent category
- business types
- sort order
- icon/image
- status

Products may remain uncategorized.

---

## 4. Products Screen

Required list columns:

- image thumbnail
- name
- barcode
- SKU
- category
- unit
- suggested price
- business types
- status
- updated date

Required filters:

- free-text search
- business type
- category
- status
- has barcode
- has image

Required actions:

- create
- edit
- archive
- reactivate
- open details
- bulk import
- export only when already supported or explicitly approved

---

## 5. Product Form

Required fields:

- name
- description
- SKU
- barcode
- category
- unit
- suggested selling price
- suggested cost, optional/internal
- image (one shared WebP; preview, upload, replace, remove; server-controlled processing)
- tags/search aliases
- business types
- status

Rules:

- Validate normalized barcode and SKU.
- Do not require barcode.
- Image upload is Platform-owned; merchants cannot mutate it from POS.
- Crop/rotate UI is not in V1; server AutoOrient + Strip.
- Require at least one business type only when product policy says so.
- Display concurrency conflict clearly.
- Never expose sensitive/internal price fields to merchant APIs unless approved.

---

## 6. Templates Screen

Template list shows:

- name
- primary business type
- status
- total products
- first-batch count
- last published date

Template builder supports:

- basic information
- default categories
- searchable available products
- selected products
- featured/first-batch toggle
- ordering
- batch size
- preview
- publish/unpublish/archive

Publishing requires explicit confirmation.

---

## 7. Bulk Import Screen

Route: `/admin/global-catalog/imports`

Flow:

```text
Download CSV Template
→ Fill / replace SAMPLE rows (keep headers unchanged; save as CSV UTF-8)
→ Upload CSV/XLSX
→ Validate (headers + row rules)
→ Preview mapping and issues
→ Explicit Confirm
→ Background processing
→ Result summary
→ Download / view error report
```

The **Download CSV Template** control must be available on the upload screen and on the import preview/detail screen. Short on-screen instructions must tell operators to:

1. download the template
2. keep header names unchanged
3. remove or replace SAMPLE rows
4. save as CSV UTF-8
5. upload for validation and preview
6. understand that import does not begin until Confirm

Authoritative CSV columns (exact order; shared by template generator and importer via `CatalogImportCsvSchema`):

`ProductName, Category, Description, Brand, Unit, Barcode, SuggestedSku, SuggestedSellingPrice, SuggestedCostPrice, TaxHint, Tags, BusinessTypes, Status`

- Multi-value `Tags` and `BusinessTypes` use `|`
- Decimals use invariant culture (example: `25.50`)
- Valid enums: `Unit` (`ProductUnit`), `Status` (`Draft|Active|Archived`), `BusinessTypes` (`BusinessType`)
- No formulas, macros, or executable content

Validation must include:

- missing / unknown / duplicate / out-of-order headers
- required fields
- duplicate barcode/SKU within file
- duplicate barcode/SKU in global catalog
- invalid category name (blank when policy requires, length/normalization failures)
- unknown category names are **warnings**, not failures: preview shows `Valid with new category` / `New category will be created: {name}`
- on Confirm, missing root categories are created once (Active), then products associate by id
- invalid unit
- invalid status
- invalid money (invariant decimal)
- invalid business type
- formula-injection prefixes (`=`, `+`, `-`, `@`)

Preview summary must show totals such as:
`80 products valid · 8 new categories will be created`

Partial success is allowed only after clear preview and confirmation. Plain category names create **root** categories only — hierarchy is not invented from free text.

---

## 8. Product Requests

When supported:

- list pending requests
- search/filter by organization/date/status
- inspect request details
- create or link global product
- resolve
- reject with reason
- audit every decision

---

## 9. UX Requirements

- responsive desktop-first admin layout
- server-side pagination
- clear empty states
- clear permission-denied states
- no placeholder navigation
- consistent ExItS status badges
- confirmation for publish/archive operations
- loading and retry behavior
- accessible labels and keyboard navigation basics

---

## 10. Acceptance Criteria

- [ ] Authorized Platform staff can manage categories.
- [ ] Authorized Platform staff can manage products.
- [ ] Bulk import supports preview and error reporting.
- [ ] Templates can be curated and published.
- [ ] Permission visibility and API authorization agree.
- [ ] Audit records exist for significant actions.

---

**Document Owner**: Platform Product / Engineering  
**Last Updated**: 2026-08-04
