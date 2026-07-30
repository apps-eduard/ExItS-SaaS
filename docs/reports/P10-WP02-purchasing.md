# P10-WP02 — Purchasing

Date: 2026-07-31  
Phase marker: `P10-WP02-purchasing`  
Status: **Complete**  
Branch: `main`  
Prior tip (Part A cleanup): `fd77f8892c363e48c40b4b35a6c9f4430af2d090`

## 1. Summary

Organization-isolated purchase orders and goods receipts for PinoyBusinessPOS: Draft → Ordered → PartiallyReceived → Received (Cancelled terminal), immutable GRNs, partial/full receiving with over-receipt denial, idempotent submit/receive, atomic `PurchaseReceipt` inventory movements for tracked products only, typed API + MAUI purchasing screens (online-only), PostgreSQL migration `AddPosPurchasing`, feature grants, and focused tests. No AP, payments, tax, returns, unplanned GRN, or offline purchasing queue.

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

Immutable GRN with `GRN-YYYYMMDD-NNNNNN`, receipt lines tied to PO lines, over-receipt denied at domain and API layers. Receive is idempotent via `IPosIdempotencyService` when `GoodsReceiptId` and idempotency headers are supplied.

## 4. Inventory hook

- `StockMovementType.PurchaseReceipt` + `StockMovementSourceType.PurchaseReceipt`
- `PurchaseStockService` applied inside receive transaction for **tracked** products only
- Unique index on purchase-receipt stock movements; no COGS/valuation from purchase cost

## 5. Authorization matrix

Grants: `store-purchasing-view`, `store-purchasing-manage`

| State | View | Manage (create/update/submit/cancel/receive) |
|---|---|---|
| Trialing / Active / GracePeriod | Grant-controlled | Grant-controlled |
| PastDue / Cancelled / Expired | Grant-controlled | Deny |
| Suspended / missing / stale / unknown | Deny | Deny |

## 6. Migration and indexes

Database: `ExItS_PinoyBusinessPOS` · Schema: `pos` · Migration: `AddPosPurchasing` (after `AddPosSuppliers`)

Tables: `pos.purchase_orders`, `pos.purchase_order_lines`, `pos.purchase_order_number_sequences`, `pos.goods_receipts`, `pos.goods_receipt_lines`, `pos.goods_receipt_number_sequences`

Validated: apply → rollback to `AddPosSuppliers` → re-apply (integration tests).

## 7. API inventory

- `GET/POST /api/v1/pos/purchase-orders`
- `GET/PUT /api/v1/pos/purchase-orders/{purchaseOrderId}`
- `POST .../submit` (idempotent), `POST .../cancel`, `POST .../receive` (idempotent)
- `GET /api/v1/pos/goods-receipts/{goodsReceiptId}`

Typed DTOs; ProblemDetails `errorCode`; org concealment; pagination on list.

## 8. MAUI routes

- `/purchasing`, `/purchasing/new`, `/purchasing/{id}`, `/purchasing/{id}/receive`
- Entry from `/more` (not bottom nav)
- EN + fil-PH resources; online-only reconnect UX

## 9. Online-only policy

`OfflineOperationTypes` defines `purchase_order.submit` and `purchase_order.receive` for idempotency header constants only — **not** mapped in offline capability map. No purchasing offline queue or local projections.

## 10. Security and privacy

- Org from trusted server context / headers (Dev/Testing)
- Cross-org IDs concealed
- Purchase cost is operational reference only — not exposed as COGS
- Production rejects Development/Testing bypasses
- No HealthCare/PHI coupling (Part A cleanup preserved at `fd77f88`)

## 11. Explicit exclusions (preserved)

Accounts payable, supplier payments, tax, purchase returns, unplanned GRN without PO, offline purchasing mutations, inventory valuation/COGS from purchase cost, POS operational roles, **P10-WP03+**.

## 12. Tests and build evidence

| Suite | Passed | Failed | Skipped |
|---|---:|---:|---:|
| Full `ExItS.slnx` test projects (`net10.0`) | **1067** | **0** | **0** |

Breakdown: Platform.UnitTests 265; Platform.Admin.UnitTests 27; Platform.IntegrationTests 90; ArchitectureTests 116; Deployment.Tests 40; DesignSystem.Tests 33; BackupRestore.Tests 10; PinoyBusinessPOS.UnitTests 292; PinoyBusinessPOS.IntegrationTests 109; PinoyBusinessPOS.ApiClient.Tests 24; PinoyBusinessPOS.Maui.Tests 61.

Release build: POS API + integration test projects **succeeded** (`net10.0`). Full solution `net10.0-android` MAUI target **not built** on this machine — Android SDK unavailable (**R-109 retained**). NU1903 (R-129) remains open.

New focused coverage: domain lifecycle, `AddPosPurchasing` rollback/re-apply, PO API lifecycle + idempotency + over-receipt, MAUI page guards, `PosPurchasingScopeArchitectureTests`, capability matrix purchasing grants.

## 13. Portfolio independence

- No root `HealthCare/` directory
- `git ls-files -- HealthCare/` empty
- `dotnet sln ExItS.slnx list` — no HealthCare project

## 14. Open risks

Unchanged release blockers: R-091, R-109, R-129, TLS-PROD, MAUI-HTTPS, POS-ROLES; Manual GCash unverified; online-only Basic Store limits; PITR deferred; etc.

## 15. Exact next work package

**P10-WP03 — Advanced Inventory** — do **not** begin until explicitly authorized.

## 16. Git

| Field | Value |
|---|---|
| Feature commit | `c0f8130ef99e958bceaee98024a69339b7e8e41a` |
| Feature message | `feat(pos): add purchase orders, goods receipts, and purchase receipt inventory (P10-WP02)` |
| Docs commit | `bc6dc7477e74c3c03785862dd98317d39c55eee1` |
| Docs message | `docs(pos): record P10-WP02 purchasing completion evidence` |

Exact next: **P10-WP03 — Advanced Inventory** (do not begin until authorized).
