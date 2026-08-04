# Phase 20 — Global Product Catalog and Business Template Onboarding

**Purpose**  
Deliver a Platform-owned global catalog and business-template onboarding system that allows POS organizations to preload common products quickly while preserving POS ownership of local prices, stock, tax, categories, and selling behavior.

---

| Field | Value |
|---|---|
| Status | Proposed |
| Phase | Phase 20 |
| Product | ExItS PinoyBusinessPOS |
| Platform Owner | ExItS Platform |
| POS Owner | PinoyBusinessPOS |
| Starting Commit | Pending |
| Final Commit | Pending |

---

## 1. Goals

- Let a new merchant begin selling within minutes.
- Avoid manual encoding of hundreds of common products.
- Keep Platform and POS databases separate.
- Preserve organization ownership of local commercial and operational data.
- Support progressive catalog loading instead of copying the entire global catalog.
- Provide a strong Sari-Sari / Mini Grocery first template.
- Reuse existing POS products, categories, inventory, authorization, audit, and mobile UI patterns.
- Keep the cashier selling flow independent from Platform availability.

---

## 2. Non-Goals

- Multi-branch catalog sharing.
- Warehouses or supplier purchasing.
- Automatic Platform overwrite of local price, stock, tax, or active state.
- Offline sales synchronization.
- AI-generated product recognition.
- Loyalty, lending, customer credit, or advanced promotions.
- Real-time dependency on the Platform API during checkout.

---

## 3. Permanent Architecture Rules

1. Platform owns global definitions, templates, publication, and Platform audit.
2. POS owns operational products, categories, inventory, sales, reports, and product-local authorization.
3. Platform and POS remain separate databases.
4. No cross-database foreign keys.
5. POS stores Platform catalog identifiers only as external references.
6. Imported products become local POS snapshots.
7. Platform updates never overwrite local price, stock, tax, or active state.
8. Existing POS inventory remains the only stock authority.
9. Existing POS permissions remain authoritative for product and inventory actions.
10. Active mobile navigation must never lead to placeholder pages.

---

## 4. Work Packages

| Work Package | Name | Required Outcome |
|---|---|---|
| P20-WP01 | Architecture and Platform/POS Contracts | Ownership, IDs, snapshots, versioning, isolation, API contracts |
| P20-WP02 | Global Categories and Products Domain | Platform catalog domain, lifecycle, permissions, audit |
| P20-WP03 | Platform Admin Catalog Management | Ant Design Blazor category/product management |
| P20-WP04 | Business Templates | Template builder, first-batch rules, publication |
| P20-WP05 | Bulk Catalog Import | CSV/XLSX validation, preview, partial success, reports |
| P20-WP06 | Merchant Onboarding and Import | Template selection, preview, progress, local import |
| P20-WP07 | Mobile Catalog Discovery and Cashier Integration | Search, browse, add products, images, cashier tiles |
| P20-WP08 | End-to-End Validation and User Closeout | Automated evidence, APK, phone checklist, user approval gate |

---

## 5. Definition of Done by Work Package

### P20-WP01 — Architecture and Platform/POS Contracts

- Platform/POS ownership documented.
- External reference strategy implemented without cross-database FK.
- Import snapshot contract defined.
- Idempotency and concurrency behavior defined.
- Security and organization isolation tests added.

### P20-WP02 — Global Categories and Products Domain

- Global categories support parent/child hierarchy.
- Global products support barcode, SKU, unit, image, business types, lifecycle status, and search tags.
- Platform permissions are explicit and role-name independent.
- Audit and optimistic concurrency are enforced.

### P20-WP03 — Platform Admin Catalog Management

- Category tree/list management works.
- Global product list, search, filters, create, edit, archive, and reactivate work.
- Images use a safe upload/reference pattern.
- Product requests inbox is available if backend scope supports it.

### P20-WP04 — Business Templates

- Platform staff can create, edit, preview, publish, unpublish, and archive templates.
- Products can be assigned, ordered, and marked first-batch/featured.
- Sari-Sari / Mini Grocery template is implemented first.
- Published-template updates do not mutate existing POS products.

### P20-WP05 — Bulk Catalog Import

- CSV and/or XLSX template is available.
- Import supports validation preview before commit.
- Duplicate barcode/SKU handling is explicit.
- Partial success is supported.
- Error report is downloadable.
- Import is idempotent and audited.

### P20-WP06 — Merchant Onboarding and Import

- Merchant selects a published template.
- Merchant previews categories and sample products.
- First batch imports through background processing.
- Progress and partial-failure states are visible.
- Local POS products and categories are created safely.
- Existing inventory authority is reused.

### P20-WP07 — Mobile Catalog Discovery and Cashier Integration

- Merchant can search the global catalog.
- Merchant can add selected products or additional template batches.
- Cashier sees fast product tiles with image placeholders.
- Search supports name, SKU, and barcode.
- Category filter, pagination, and lazy image loading work.
- Selling uses local POS data only.

### P20-WP08 — End-to-End Validation and User Closeout

- Platform Catalog Manager creates and publishes a template.
- Merchant imports the template.
- Imported products appear locally.
- Local prices and inventory remain organization-owned.
- Cashier can sell an imported product.
- Platform changes do not overwrite local commercial data.
- Emulator and PhysicalDevice APKs are built.
- Phase remains Open until explicit user phone-validation approval.

---

## 6. Documentation Requirements

Create and maintain:

- `docs/specs/product-catalog/01-architecture-and-boundaries.md`
- `docs/specs/product-catalog/02-data-model-and-migrations.md`
- `docs/specs/product-catalog/03-api-contracts.md`
- `docs/specs/product-catalog/04-platform-admin-management.md`
- `docs/specs/product-catalog/05-merchant-onboarding-and-import.md`
- `docs/specs/product-catalog/06-background-jobs-and-imports.md`
- `docs/specs/product-catalog/07-mobile-and-cashier-experience.md`
- `docs/reports/P20-WP01-architecture-and-contracts.md`
- `docs/reports/P20-WP02-global-categories-and-products-domain.md`
- `docs/reports/P20-WP03-platform-admin-catalog-management.md`
- `docs/reports/P20-WP04-business-templates.md`
- `docs/reports/P20-WP05-bulk-catalog-import.md`
- `docs/reports/P20-WP06-merchant-onboarding-and-import.md`
- `docs/reports/P20-WP07-mobile-catalog-and-cashier-integration.md`
- `docs/reports/P20-WP08-end-to-end-validation-and-user-closeout.md`

Also update the phase index, portfolio progress, implementation summary, architecture, security, authorization matrix, and any catalog-related guides.

---

## 7. Status Rules

- WP01–WP07 may be marked Code Complete only with implementation and automated evidence.
- WP08 must remain `In Progress — User Physical-Device Validation Pending` until user confirmation.
- Phase 20 must remain Open until explicit user approval.
- Do not claim Device Verified without a full phone checklist.
- Do not claim production readiness as part of this phase.

---

## 8. Related Documents

- `../specs/product-catalog/01-architecture-and-boundaries.md`
- `../specs/product-catalog/02-data-model-and-migrations.md`
- `../specs/product-catalog/03-api-contracts.md`
- `../specs/product-catalog/04-platform-admin-management.md`
- `../specs/product-catalog/05-merchant-onboarding-and-import.md`
- `../specs/product-catalog/06-background-jobs-and-imports.md`
- `../specs/product-catalog/07-mobile-and-cashier-experience.md`

---

**Document Owner**: Product / Engineering  
**Last Updated**: 2026-08-04
