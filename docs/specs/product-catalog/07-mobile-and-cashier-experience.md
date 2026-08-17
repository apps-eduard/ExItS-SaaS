# Mobile Catalog and Cashier Experience

**Purpose**  
Define the MAUI experience for product discovery, local catalog expansion, product tiles, images, barcode search, and cashier selling integration.

---

| Field | Value |
|---|---|
| Status | Proposed |
| Phase | Phase 20 |
| Work Package | P20-WP07 |
| Client | ExItS PinoyBusinessPOS MAUI |

---

## 1. Goals

- Make products easy to find on a Samsung phone.
- Keep cashier selling fast and simple.
- Use local POS data during selling.
- Allow Owner/Manager to add products from the global catalog.
- Avoid loading hundreds of full-image cards at once.

---

## 2. Product Tile Design

Each tile should support:

```text
[ Product image or placeholder ]
Product name
₱ Price
Stock / unit when appropriate
```

Rules:

- Use thumbnail-sized images.
- Lazy-load images.
- Show a stable placeholder when no image exists.
- Never block selling because an image fails.
- Do not download the entire Platform template catalog.
- Explicit template adoption may best-effort cache that template's thumbnail in private app storage.
- Use responsive tile count based on device width.

---

## 3. Cashier Selling Screen

```text
Search / scan barcode

[ All ] [ Drinks ] [ Snacks ] [ Canned Goods ] [ More ]

[Product Tile] [Product Tile]
[Product Tile] [Product Tile]

Cart summary
Subtotal / Tax / Total
Checkout
```

Required search:

- product name
- SKU
- barcode

Required behavior:

- category filter
- pagination/infinite scrolling
- fast tap-to-add
- quantity adjustment
- insufficient-stock handling
- duplicate-submit protection
- active-shift/register enforcement

Selling must use local POS product data only.

Payment methods at checkout (unchanged POS contracts; Phase 19 delivery):

- Cash — immediate completion
- Manual GCash — operator-confirmed reference (unverified; legacy path)
- Card / GCash (electronic) — **simulated only** via `FakePaymentGateway`; sale enters `AwaitingPayment` until signed webhook/simulation; **Retest** on phone ([P19-card-gcash-payment-ui-and-simulation](../../reports/P19-card-gcash-payment-ui-and-simulation.md))

---

## 4. Add Products from Global Catalog

Owner/Manager flow:

```text
Products
→ Add products
→ Search global catalog or select template batch
→ Select one or more products
→ Review category and suggested price
→ Confirm import
→ Track progress
→ Review local products
```

Cashier must not receive this capability unless explicitly granted.

---

## 5. Catalog Discovery Screen

Required controls:

- search box
- barcode entry/scanner integration where already supported
- business/template context
- category filter
- result cards
- selected count
- add selected
- load next batch
- retry/error state

Exclude products already imported where the backend can determine this safely.

---

## 6. Local Product Review

After import:

- edit selling price
- view imported image
- confirm category
- deactivate irrelevant product
- open inventory action for opening stock

Do not duplicate inventory adjustment logic inside the product form.

Imported products display the shared Platform template image unless the merchant uploaded an override. Removing the override reveals the current Platform image; it does not copy or delete the Platform asset.

---

## 7. Role Experience

### Owner

- manage local catalog
- import from global catalog
- review template batches
- edit local commercial fields

### Manager

- same only when granted corresponding permissions

### Cashier

- browse/search local selling catalog
- add items to cart
- view only allowed stock information
- no global import by default
- no local product administration by default

---

## 8. Mobile States

Every screen must include:

- loading
- empty
- error
- retry
- access denied
- offline/unavailable Platform state
- import processing
- import completed with warnings

Existing local selling remains available when Platform catalog search is unavailable.

Offline org-created products (`/catalog/products/new` Queueable): enqueue metadata only; keep pending photos in private app files; never SQLite image bytes. Sync product identity first, then upload the custom image. Catalog list, import, and edit remain OnlineRequired. Do not eagerly download the Platform template catalog.

---

## 9. Performance Requirements

- Initial local product page should render quickly on target Samsung phone.
- Use server-side paging/filtering.
- Do not render 200 images simultaneously.
- Cache thumbnails according to existing client conventions.
- Cancel stale search requests.
- Debounce text search.
- Preserve cart state during non-destructive catalog browsing only when safe.

---

## 10. Physical-Device Validation

Validate:

- template selection
- first-batch progress
- local product appearance
- image placeholder
- category filter
- search by name
- search by SKU
- barcode search/scan where supported
- tap product tile
- complete cash sale
- restart next sale
- Platform unavailable while local selling continues

Use existing PhysicalDevice/Tailscale build conventions.

---

## 11. Acceptance Criteria

- [ ] Cashier can sell imported products from local POS data.
- [ ] Owner/authorized Manager can discover and import catalog products.
- [ ] Images and placeholders work without blocking selling.
- [ ] Search and category filtering are responsive.
- [ ] Platform unavailability does not stop local checkout.
- [ ] Unauthorized Cashier administration is denied and hidden.

---

**Document Owner**: Mobile Product / Engineering  
**Last Updated**: 2026-08-04
