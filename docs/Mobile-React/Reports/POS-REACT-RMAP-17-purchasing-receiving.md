# RMAP-17 — Purchasing + goods receipt + direct purchase

## Status

**PASS** (pending parent commit + native-speaker review)

| Flag | Value |
|------|-------|
| `RMAP_17_AUTHORIZED` | YES (authorized after RMAP-16 PASS) |
| `RMAP_17_PASS` | PASS |
| `RMAP_17_CLIENT` | PASS |
| `RMAP_17_CAPABILITIES` | PASS |
| `RMAP_17_UI` | PASS |
| `RMAP_17_I18N` | PASS |
| `RMAP_17_VITEST` | PASS |
| `RMAP_17_E2E` | PASS |
| `RMAP_17_TYPECHECK` | PASS |
| `RMAP_17_DIRECT_PURCHASE` | PASS |
| `RMAP_17_NATIVE_SPEAKER` | PENDING |
| `HARD_STOP` | NO (await RMAP-18 authorization separately) |

## Contract

| Area | Finding |
|------|---------|
| API | Existing `/api/v1/pos/purchase-orders`, `/api/v1/pos/goods-receipts`, `/api/v1/pos/direct-purchase-receipts` — **no invented contracts** |
| Direct purchase | **PASS** — implemented at `/api/v1/pos/direct-purchase-receipts` (list/get/create with body `idempotencyKey`) — **not** `CONTRACT_GAP` |
| Inventory invariant | PO create/submit/accept/cancel **never** increase inventory; stock increases only on goods receipt (`POST …/receive`) and direct purchase create |
| Idempotency | Submit/receive use `Idempotency-Key` + `X-Pos-Payload-Hash` + operation headers (MAUI `PosMutationIdempotencyHelper` parity); receive uses client-generated `goodsReceiptId`; direct purchase uses body `idempotencyKey` |
| Capabilities | Hub: ViewPurchasing ∪ ManageInventory ∪ ViewSuppliers; PO view: ViewPurchasing; PO manage/receive: ManagePurchasing; Direct list: ViewInventory; Direct create: ManageInventory |
| Offline | Online-only mutations (browser advisory offline banner; no LocalStore queue) |

## Implementation

- `pos-mutation-idempotency.ts` + `posRequest` optional headers
- `pos-purchase-orders-client.ts` — list/create/get/update/submit/cancel/accept-changes/receive + get goods receipt
- `pos-direct-purchase-receipts-client.ts` — list/get/create (expiry/lot when TracksExpiration)
- Features under `src/features/purchasing/`: hub, PO list/create/detail/receive, receivable orders, receive stock, direct list/detail
- Routes `/purchasing/*` with purchasing/inventory guards
- Nav from Manager/Owner role home
- i18n `purchasing.*` in en, fil-PH, ceb-PH, ilo-PH, hil-PH
- Vitest: PO alone no inventory; receive-only stock method; partial receive math; direct idempotency; capabilities; message-parity
- Playwright `e2e/rmap-17-purchasing-receiving.spec.ts`
- Report + roadmap status update

## Exclusions

- Incoming connected PO seller inbox/fulfillment UI (supplier-side)
- Offline purchasing / draft LocalStore queue UI
- Migrations / backend changes
- Native-speaker i18n sign-off

## Validation

### React gates

| Gate | Result |
|------|--------|
| prettier (touched) | PASS |
| typecheck | PASS |
| Vitest (PO + direct + receive-math + capabilities + message-parity) | PASS |
| Playwright `rmap-17-purchasing-receiving` | PASS |

Responsive matrix (purchasing hub):

| Viewport | Result |
|----------|--------|
| 375×812 | PASS (e2e) |
| 768×1024 | PASS (e2e) |
| 1024×768 | PASS (e2e) |
| 1440×900 | PASS (e2e) |

### Proven behaviors

- PO create → submit → partial receive → complete receive
- Inventory/stock endpoints untouched until receive / direct purchase
- Connected `canReceiveConnected=false` gate hides receive CTA
- Direct buy with expiry/lot
- Wrong-org PO detail not found
- Cashier denied `/purchasing`
- Locale smoke (Filipino hub title)
- Responsive 4 viewports

### Inventory invariant evidence

- Unit: `assertNotStockTouchingUrl` on create/submit/cancel; `STOCK_TOUCHING_PURCHASE_ORDER_METHODS === ["receivePurchaseOrder"]`
- E2E: `tracker.inventoryCalls === 0` across PO create/submit; receive/direct calls counted separately

## Exact next

Do **not** start RMAP-18 until authorized. Native-speaker i18n review remains PENDING.
