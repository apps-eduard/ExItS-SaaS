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
- image
- tags/search aliases
- business types
- status

Rules:

- Validate normalized barcode and SKU.
- Do not require barcode.
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

Flow:

```text
Download template
→ Upload CSV/XLSX
→ Validate
→ Preview mapping and issues
→ Confirm
→ Background processing
→ Result summary
→ Download error report
```

Validation must include:

- required fields
- duplicate barcode/SKU within file
- duplicate barcode/SKU in global catalog
- unknown category
- invalid unit
- invalid money
- invalid business type
- invalid image URL/reference

Partial success is allowed only after clear preview and confirmation.

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
