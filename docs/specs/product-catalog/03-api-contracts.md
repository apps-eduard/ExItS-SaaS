# Product Catalog API Contracts

**Purpose**  
Define versioned Platform and POS API contracts for catalog administration, template discovery, product import, and import progress.

---

| Field | Value |
|---|---|
| Status | Proposed |
| Phase | Phase 20 |
| Work Package | P20-WP01–P20-WP07 |
| API Style | Existing ExItS REST conventions |

---

## 1. Contract Rules

- Follow existing API versioning, envelopes, problem details, audit, and authorization conventions.
- Never trust organization identifiers from the client without authorization validation.
- Use opaque Platform identifiers across the product boundary.
- Apply pagination to all list/search endpoints.
- Use optimistic concurrency for mutable Platform records.
- Use idempotency for import commands.
- Prefer lifecycle commands or status transitions over hard delete.

---

## 2. Platform Admin Endpoints

### Global categories

```text
GET    /api/v1/platform/catalog/categories
POST   /api/v1/platform/catalog/categories
GET    /api/v1/platform/catalog/categories/{id}
PUT    /api/v1/platform/catalog/categories/{id}
PATCH  /api/v1/platform/catalog/categories/{id}/status
```

### Global products

```text
GET    /api/v1/platform/catalog/products
POST   /api/v1/platform/catalog/products
GET    /api/v1/platform/catalog/products/{id}
PUT    /api/v1/platform/catalog/products/{id}
PATCH  /api/v1/platform/catalog/products/{id}/status
POST   /api/v1/platform/catalog/products/imports
GET    /api/v1/platform/catalog/products/imports/{jobId}
GET    /api/v1/platform/catalog/products/imports/{jobId}/errors
```

### Templates

```text
GET    /api/v1/platform/catalog/templates
POST   /api/v1/platform/catalog/templates
GET    /api/v1/platform/catalog/templates/{id}
PUT    /api/v1/platform/catalog/templates/{id}
POST   /api/v1/platform/catalog/templates/{id}/publish
POST   /api/v1/platform/catalog/templates/{id}/unpublish
POST   /api/v1/platform/catalog/templates/{id}/archive
POST   /api/v1/platform/catalog/templates/{id}/products
PUT    /api/v1/platform/catalog/templates/{id}/products/order
DELETE /api/v1/platform/catalog/templates/{id}/products/{productId}
```

`DELETE` is acceptable only for removing a product association from a template. It must not delete the global product.

---

## 3. Merchant Discovery Endpoints

These endpoints are Platform read APIs consumed by authorized POS users or the POS API.

```text
GET /api/v1/catalog/templates
GET /api/v1/catalog/templates/{id}
GET /api/v1/catalog/templates/{id}/products
GET /api/v1/catalog/products/search
GET /api/v1/catalog/categories
```

Suggested search query:

```text
q
businessType
categoryId
barcode
sku
page
pageSize
```

The client must not download the full global catalog.

---

## 4. POS Import Endpoints

Import commands should be handled by POS because POS owns the resulting local products.

```text
POST /api/v1/pos/catalog-imports/template
POST /api/v1/pos/catalog-imports/products
POST /api/v1/pos/catalog-imports/template/{templateId}/next-batch
GET  /api/v1/pos/catalog-imports/{jobId}
GET  /api/v1/pos/catalog-imports/{jobId}/items
```

### Import template request

```json
{
  "platformTemplateId": "uuid",
  "batchNumber": 1,
  "idempotencyKey": "uuid"
}
```

### Import selected products request

```json
{
  "platformGlobalProductIds": ["uuid"],
  "idempotencyKey": "uuid"
}
```

The organization must be derived from authenticated working context, not blindly accepted from request body.

---

## 5. Import Result

```json
{
  "jobId": "uuid",
  "status": "Queued",
  "totalCount": 200,
  "processedCount": 0,
  "importedCount": 0,
  "skippedCount": 0,
  "failedCount": 0
}
```

Statuses:

```text
Queued
Processing
Completed
CompletedWithWarnings
Failed
Cancelled
```

---

## 6. Error Codes

Recommended codes:

```text
CATALOG_TEMPLATE_NOT_FOUND
CATALOG_TEMPLATE_NOT_PUBLISHED
CATALOG_PRODUCT_NOT_ACTIVE
CATALOG_DUPLICATE_BARCODE
CATALOG_DUPLICATE_SKU
CATALOG_IMPORT_ALREADY_PROCESSED
CATALOG_IMPORT_PARTIAL_SUCCESS
CATALOG_IMPORT_FAILED
CATALOG_IMPORT_NOT_AUTHORIZED
CATALOG_CONCURRENCY_CONFLICT
```

Use existing API error-envelope conventions rather than introducing a separate response format.

---

## 7. Security Requirements

- Platform write endpoints require Platform catalog permissions.
- Merchant discovery endpoints are read-only.
- POS import endpoints require organization entitlement plus POS product-management permission.
- Cashier users must not receive import or catalog-administration permission automatically.
- Cross-organization access is concealed according to existing policy.
- Import requests and results must be audited.

---

## 8. Acceptance Criteria

- [ ] APIs use existing versioning and error patterns.
- [ ] All list endpoints are paginated.
- [ ] Import is idempotent.
- [ ] Organization is derived from trusted context.
- [ ] Cross-product ownership is preserved.
- [ ] Authorization tests cover Platform, Owner, Manager, and Cashier.

---

**Document Owner**: API / Engineering  
**Last Updated**: 2026-08-04
