# P10-WP02 — Purchasing

Date: 2026-07-31  
Phase marker: `P10-WP02-purchasing`  
Status: **Complete**  
Branch: `main`  
Prior tip (Part A cleanup): `fd77f8892c363e48c40b4b35a6c9f4430af2d090`

## 1. Summary

Organization-isolated purchase orders and goods receipts for PinoyBusinessPOS: Draft → Ordered → PartiallyReceived → Received (Cancelled terminal), immutable GRNs, partial/full receiving with over-receipt denial, idempotent submit/receive, atomic `PurchaseReceipt` inventory movements for tracked products only, typed API + MAUI purchasing screens (online-only), PostgreSQL migrations `AddPosPurchasing` + `EnrichPosGoodsReceiptFields`, feature grants, and focused tests. No AP, payments, tax, returns, unplanned GRN, or offline purchasing queue.

Gap-fix follow-up after the initial feature tip enriched goods-receipt fields (SupplierId, ReceivedDate, DeliveryReference, Notes, cost snapshots, InventoryMovementId), hardened cancel-after-receipt rules, fixed MAUI receive/detail (including Android compile), and linked stock movements onto receipt lines.

## 2. Purchase order model

| Field | Notes |
|---|---|
| PurchaseOrderId | GUID PK |
| OrganizationId | Immutable; server context authoritative |
| PoNumber | `PO-YYYYMMDD-NNNNNN`; allocated on submit; null while Draft |
| SupplierId | Required; references P10-WP01 supplier |
| Status | Draft, Ordered, PartiallyReceived, Received, Cancelled |
| OrderDate | Required |
| ExpectedDeliveryDate | Optional |
| SupplierReference / Notes | Optional bounded text |
| OrderedAtUtc / OrderedBy | Set on submit |
| Timestamps + xmin | UTC + PostgreSQL concurrency token |

### Lines

ProductId, NameSnapshot, UomSnapshot (frozen on submit), OrderedQty, UnitPurchaseCost ≥ 0, LineTotal (`RoundMoney`), ReceivedQty, OutstandingQty, LineNotes, LineNumber. Duplicate products rejected on draft create/update.

## 3. Goods receipt model

Immutable GRN with `GRN-YYYYMMDD-NNNNNN`. Parent fields include SupplierId (inherited from PO), ReceivedDate, optional DeliveryReference/Notes, ReceivedAtUtc/ReceivedBy. Lines include PurchaseOrderLineId, ProductId, QuantityReceived, UnitPurchaseCostSnapshot, LineTotalSnapshot, UomSnapshot, and optional InventoryMovementId. Over-receipt denied at domain and API layers. Receive is idempotent via `IPosIdempotencyService` when `GoodsReceiptId` and idempotency headers are supplied.

## 4. Inventory hook

- `StockMovementType.PurchaseReceipt` + `StockMovementSourceType.PurchaseReceipt`
- `PurchaseStockService` applied inside receive transaction for **tracked** products only
- Receipt line `AttachInventoryMovement` persists movement id after create
- Unique index on purchase-receipt stock movements; no COGS/valuation from purchase cost

## 5. Authorization matrix

Grants: `store-purchasing-view`, `store-purchasing-manage`

| State | View | Manage (create/update/submit/cancel/receive) |
|---|---|---|
| Trialing / Active / GracePeriod | Grant-controlled | Grant-controlled |
| PastDue / Cancelled / Expired | Grant-controlled | Deny |
| Suspended / missing / stale / unknown | Deny | Deny |

## 6. Migration and indexes

Database: `ExItS_PinoyBusinessPOS` · Schema: `pos`  
Migrations: `AddPosPurchasing` (after `AddPosSuppliers`), then `EnrichPosGoodsReceiptFields`

Tables: `pos.purchase_orders`, `pos.purchase_order_lines`, `pos.purchase_order_number_sequences`, `pos.goods_receipts`, `pos.goods_receipt_lines`, `pos.grn_number_sequences`

Enrichment backfills supplier/received date and cost snapshots from PO data before NOT NULL + FK.

Validated: apply → rollback to `AddPosSuppliers` → re-apply (integration tests).

## 7. API inventory

- `GET/POST /api/v1/pos/purchase-orders`
- `GET/PUT /api/v1/pos/purchase-orders/{purchaseOrderId}`
- `POST .../submit` (idempotent), `POST .../cancel`, `POST .../receive` (idempotent)
- `GET /api/v1/pos/goods-receipts/{goodsReceiptId}`

Typed DTOs; ProblemDetails `errorCode`; org concealment; pagination on list.

## 8. MAUI routes

- `/purchasing`, `/purchasing/new`, `/purchasing/{id}`, `/purchasing/{id}/receive`
- Detail: status-gated submit / cancel / receive
- Receive: outstanding-qty line editors + delivery reference / notes
- Entry from `/more` (not bottom nav)
- EN + fil-PH resources; online-only reconnect UX

### Create draft UX refinement (later)

`/purchasing/new` product selection and draft lines:

| Topic | Behavior |
|---|---|
| Search | Client-side name filter over already-loaded active products (`Search products`) |
| Category | Horizontal chips defaulting to **All**; only categories present on available products; products without a category use **No category** |
| Lines | Each draft line shows product name, `Qty N ×` unit purchase cost (`MoneyDisplay`), and line total |
| Edit | Pencil action prefills product / qty / unit cost; **Save changes** updates the same line (no duplicate) |
| Delete | Trash action with confirmation: “Remove this item from the purchase order?” |
| Totals | Line and order totals use `PosSaleOptions.RoundMoney` via `PurchaseOrderCreateUi` — domain create/submit/receive unchanged |

### Connected ExItS suppliers (later)

Optional connected-organization suppliers sit above this purchasing spine. Buyer inventory still changes only on Goods Receipt. See [connected-exits-suppliers.md](../engineering/connected-exits-suppliers.md).

## 9. Online-only policy

`OfflineOperationTypes` defines `purchase_order.submit` and `purchase_order.receive` for idempotency header constants only — **not** mapped as full offline queue handlers for ordinary external POs.

Connected ExItS Suppliers Phase 1 adds **device-local** connected-PO drafts and linked-product projections (LocalStore v8). Server search and supplier submission remain online-required with revalidation. See [connected-exits-suppliers.md](../engineering/connected-exits-suppliers.md).

## 10. Security and privacy

- Org from trusted server context / headers (Dev/Testing)
- Cross-org IDs concealed
- Purchase cost is operational reference only — not exposed as COGS
- Production rejects Development/Testing bypasses
- No foreign-product/PHI coupling (Part A cleanup preserved at `fd77f88`)

## 11. Explicit exclusions (preserved)

Accounts payable, supplier payments, tax, purchase returns, unplanned GRN without PO, offline purchasing mutations, inventory valuation/COGS from purchase cost, POS operational roles, **P10-WP03+**.

## 12. Tests and build evidence

| Suite | Passed | Failed | Skipped |
|---|---:|---:|---:|
| Full `ExItS.slnx` test projects (`net10.0`) | **1067** | **0** | **0** |

Breakdown: Platform.UnitTests 265; Platform.Admin.UnitTests 27; Platform.IntegrationTests 90; ArchitectureTests 116; Deployment.Tests 40; DesignSystem.Tests 33; BackupRestore.Tests 10; PinoyBusinessPOS.UnitTests 292; PinoyBusinessPOS.IntegrationTests 109; PinoyBusinessPOS.ApiClient.Tests 24; PinoyBusinessPOS.Maui.Tests 61.

Release build: POS API **succeeded**. MAUI `net10.0-android` Release **succeeded** on this machine (compile evidence; device deploy not required for this WP). NU1903 (R-129) remains open.

New focused coverage: domain lifecycle, `AddPosPurchasing`/`EnrichPosGoodsReceiptFields` schema assertions, PO API lifecycle + idempotency + over-receipt, MAUI page guards (no `.AsTask()`, outstanding receive UX), `PosPurchasingScopeArchitectureTests`, capability matrix purchasing grants.

## 13. Portfolio independence

- No root `HealthCare/` directory
- `git ls-files -- HealthCare/` empty
- `dotnet sln ExItS.slnx list` — no HealthCare project

## 14. Open risks

Unchanged release blockers: R-091, R-129, TLS-PROD, MAUI-HTTPS, POS-ROLES; Manual GCash unverified; online-only Basic Store limits; PITR deferred; etc. R-109 Android compile gap closed for this WP’s purchasing pages; device-level verification remains out of scope unless required later.

## 15. Exact next work package

**P10-WP03 — Advanced Inventory** — do **not** begin until explicitly authorized.

## 16. Git

| Field | Value |
|---|---|
| Feature commit (initial) | `c0f8130ef99e958bceaee98024a69339b7e8e41a` |
| Feature message | `feat(pos): add purchase orders, goods receipts, and purchase receipt inventory (P10-WP02)` |
| Docs commit (initial) | `bc6dc7477e74c3c03785862dd98317d39c55eee1` |
| Gap-fix feature commit | `bfb4c6b454757e2794aec33399b4556a711dc934` |
| Gap-fix feature message | `fix(pos): enrich goods receipts and fix purchasing MAUI receive (P10-WP02)` |
| Gap-fix docs / hash-record | `dfea2fa5b34a3d7a59e6d251f3559813dbdeb444` |

Exact next: **P10-WP03 — Advanced Inventory** (do not begin until authorized).
