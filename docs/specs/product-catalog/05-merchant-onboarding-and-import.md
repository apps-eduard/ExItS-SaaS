# Merchant Onboarding and Catalog Import

**Purpose**  
Define how a new POS organization selects a business template, imports an initial product batch, reviews local data, and begins selling.

---

| Field | Value |
|---|---|
| Status | Proposed |
| Phase | Phase 20 |
| Work Package | P20-WP06 |
| Primary Template | Sari-Sari / Mini Grocery |

---

## 1. Goals

- Merchant starts with useful products in minutes.
- Merchant does not encode hundreds of products manually.
- Merchant reviews local price and stock before relying on them.
- Import remains safe, resumable, and idempotent.
- Existing POS setup and authorization are preserved.

---

## 2. Onboarding Flow

```text
Create organization / enter POS
        ↓
Complete existing POS setup
        ↓
Choose business template
        ↓
Preview categories and sample products
        ↓
Confirm first-batch import
        ↓
Track import progress
        ↓
Review local prices and opening stock
        ↓
Open shift
        ↓
Start selling
```

Do not duplicate existing organization creation or POS operational setup.

---

## 3. Template Selection

Each published template card shows:

- icon/cover
- template name
- business type
- short description
- estimated first-batch product count
- sample categories
- sample products

Primary MVP template:

```text
Sari-Sari / Mini Grocery
```

Other templates may remain draft until curated.

---

## 4. Template Preview

Preview includes:

- common categories
- 10–20 sample products
- expected first-batch count
- explanation of local ownership

Required message:

> Common products will be added as editable local POS products. You remain in control of prices, stock, tax, and active status.

---

## 5. Import Confirmation

Before import, show:

- number of products
- number of categories
- duplicate handling behavior
- initial stock behavior
- suggested-price behavior

Recommended defaults:

```text
Initial stock: 0
Suggested price: copied as editable starting value
Product status: Active, unless validation requires review
```

Do not write stock directly if existing POS inventory requires an opening-stock transaction. Use the existing inventory workflow.

---

## 6. Import Progress

UI states:

```text
Queued
Preparing
Importing
Completed
Completed with warnings
Failed — Retry available
```

Progress screen shows:

- processed count
- imported count
- skipped count
- failed count
- retry action when safe
- view error details

The merchant may leave the screen; the job continues.

---

## 7. Post-Import Review

After import, direct the merchant to a review screen:

- product name
- image
- barcode/SKU
- category
- selling price
- opening stock action
- active status

Actions:

- accept suggested price
- edit price
- enter opening stock through existing inventory flow
- deactivate irrelevant products
- add more products

---

## 8. Add More Products

Supported methods:

1. Search global catalog.
2. Browse by category.
3. Load next template batch.
4. Add selected products.
5. Create a fully custom local product.
6. Request a missing global product when the request feature exists.

Never force the full global catalog into the organization.

---

## 9. Permissions

- Owner: may select template and import products when entitled.
- Manager: may import products only when granted product-management permission.
- Cashier: must not receive onboarding/import management automatically.
- All users remain subject to Platform entitlement plus POS product-local permissions.

---

## 10. Edge Cases

| Case | Behavior |
|---|---|
| Import interrupted | Continue/retry idempotently |
| Duplicate barcode/SKU | Skip or map safely; report result |
| Template unpublished after selection | Existing started job follows defined snapshot policy |
| Template has no first-batch products | Warn and offer search/manual creation |
| Product already imported | Do not duplicate |
| Platform unavailable | Show retryable state; existing POS remains usable |
| User switches organization | Import status remains organization-scoped |

---

## 11. Success Metrics

- Time from template confirmation to usable catalog.
- Percentage of first-batch products imported successfully.
- Time to first sale.
- Number of manually created products after onboarding.
- Number of products added through search versus batch.

Metrics are optional unless analytics infrastructure already exists.

---

## 12. Acceptance Criteria

- [ ] Merchant can select and preview a published template.
- [ ] First batch imports through a background job.
- [ ] Progress and partial errors are visible.
- [ ] Local price remains editable.
- [ ] Inventory uses existing POS authority.
- [ ] Cashier can sell imported products after normal shift requirements.
- [ ] Import is organization-isolated and idempotent.

---

**Document Owner**: POS Product / Engineering  
**Last Updated**: 2026-08-04
