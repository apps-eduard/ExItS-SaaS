# POS-REACT RMAP-21D — POS offline Sell + Cash

**Status:** PASS  
**Dependencies:** 21A matrix; 21B LocalStore + encrypted outbox; 21C Connection & Sync UI

## Capability flags

| Flag                              | Value       |
| --------------------------------- | ----------- |
| `OFFLINE_SELL_CASH`               | YES         |
| `OFFLINE_GCASH`                   | NO          |
| `OFFLINE_BUSINESS_UTANG_CHECKOUT` | NO          |
| `OFFLINE_DISCOUNT`                | NO          |
| `OFFLINE_PRICE_OVERRIDE`          | NO          |
| `LOT_EXPIRY_OFFLINE`              | FAIL_CLOSED |
| `DEVICE_SHIFT_SELL_PRESERVED`     | YES         |

## Delivered

| Capability                                  | Evidence                                                                                                                                                       |
| ------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Sale idempotency headers on online checkout | `src/api/pos/pos-sales-client.ts` — `Idempotency-Key`, `X-Pos-Payload-Hash`, `X-Pos-Operation-Id`, `X-Pos-Operation-Type` keyed on `saleId`                    |
| Shared online/offline checkout body         | `buildCheckoutSalePayload` used by `checkoutSale` and by the offline enqueue                                                                                   |
| Schema version 2                            | `OFFLINE_SCHEMA_VERSION = 2`; stores `outbox`, `meta`, `entityMap`, `catalogProducts`, `catalogCategories`, `sellReadiness`                                    |
| One shared connection per scope             | `openSharedOfflineDatabase` — screens never close a database the shell is reading                                                                              |
| Read-only Sell catalog cache                | `src/offline/catalog-cache.ts` — write-through from a successful online browse only; reads fail closed to empty                                                |
| Last-good Sell readiness snapshot           | `src/offline/sell-readiness-snapshot.ts` — `{ deviceReady, moneyPostReady, shiftId, openShiftNumber, capturedAt }`, unusable when incomplete or older than 24h |
| Offline Cash enqueue                        | `src/offline/cash-sale-offline.ts` — encrypted envelope, `sale.checkout`, `Pending`, idempotency key from `saleId`                                             |
| Online-required codes and card              | `src/offline/online-required.ts`, `src/components/exits/OnlineRequiredCard.tsx` (GCash, Utang, discount, price override, open shift, device register)          |
| Offline checkout behavior                   | `src/features/checkout/CheckoutCashPage.tsx` — Cash forced, GCash/Utang disabled, discount panel hidden, quote skipped, cart subtotal used as amount           |
| Offline queued outcome page                 | `src/features/sell/OfflineSaleQueuedPage.tsx` at `/sell/offline-queued/:saleId`                                                                                |
| Warm-session continuation                   | `src/features/sell/use-sell-offline-readiness.ts` used by `SellReadinessGate`, `SellFloorPage`, `CheckoutCashPage`                                             |
| Auth tokens in IndexedDB                    | **NO**                                                                                                                                                         |
| Workbox API caching                         | **NO** (API routes remain `NetworkOnly`)                                                                                                                       |

## Offline Cash flow

1. Online and ready: device + shift readiness is written to the `sellReadiness` snapshot; a successful full catalog browse replaces the catalog cache.
2. Network drops: the shell reports Offline; a stale quote is discarded and payment falls back to Cash; the cart subtotal becomes the amount to pay.
3. Confirm: the Cash body is encrypted into the outbox as `Pending` with the `saleId` idempotency key, the cart clears, and the cashier lands on the queued page.
4. The queued page is deliberately not a Transaction Summary — no sale number, no receipt, no server totals, because nothing is recorded on the server yet.

## Fail-closed and honesty rules

| Situation                              | Behavior                                                                                                                                                                                                                                                                                                            |
| -------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Discount applied, then network drops   | Only the server quote ever applied a discount, so the intent is dropped and announced with the online-required notice instead of charging the undiscounted subtotal while still showing a discount; confirm rejects any surviving intent and the enqueue throws `offline.sale.discount_not_supported` as a backstop |
| Price override intent present offline  | Confirm rejected; enqueue throws `offline.sale.price_override_not_supported`; the cashier can still clear the override from the cart and finish at the regular price                                                                                                                                                |
| GCash or Utang offline                 | Buttons disabled, payment forced to Cash, enqueue never sees another method                                                                                                                                                                                                                                         |
| Customer attach offline                | Panel hidden and selection cleared; enqueue throws `offline.sale.customer_not_supported` rather than silently dropping the customer                                                                                                                                                                                 |
| Lot / expiry sellable quantity offline | No cached lot or expiry authority; the tile hint uses the cached catalog on-hand only and the entry-dialog sellable lookup simply does not resolve — no offline expiry decision is invented                                                                                                                         |
| No usable readiness snapshot offline   | Sell and checkout stay blocked with `offline.notReady`; device register and open shift show the online-required notice instead of a link that cannot work                                                                                                                                                           |
| Catalog cache empty                    | Empty product grid, never a fabricated catalog                                                                                                                                                                                                                                                                      |
| Snapshot older than 24h or incomplete  | Treated as unusable                                                                                                                                                                                                                                                                                                 |

## Explicit non-claims

- The snapshot is UX continuation, not authorization. The server still accepts or rejects every queued sale, and `moneyPostReady` offline never means "the server approved this".
- No sync processor: queued sales stay `Pending` until 21H wires the network processor. Nothing in this package sends a queued sale.
- No offline sale number, receipt, tax, or discount math. All money remains server-computed on sync.
- No offline returns, shifts, cash counts, purchasing, inventory, or Personal-domain writes.
- Not native SecureStorage / Keystore / Keychain parity; cold-start unlock remains `DEFERRED_SECURITY_GAP` from 21B.
- Bumping the schema to v2 changes the database name, so any v1 database is left in place and unread. No v1 rows are migrated — acceptable pre-production, and no v1 sale enqueue path ever shipped.

## Tests

- `src/api/pos/pos-sales-client.test.ts` — checkout sends the idempotency key, operation id/type, and payload hash
- `src/offline/cash-sale-offline.test.ts` — schema v2 stores; encrypted `Pending` Cash envelope with the `saleId` idempotency key; discount, price override, shift, line, and tender rejections
- `src/offline/catalog-cache.test.ts` — cache fails closed to empty, replace removes withdrawn products, readiness snapshot round-trip and staleness

Full suite: `npm run test` — 83 files, 381 tests passed. `npm run typecheck` clean. `npx eslint` clean (two pre-existing `react-refresh` warnings in `OfflineSyncProvider.tsx`).

## Gaps carried to 21E–21H

- Offline search matches the cached catalog by name, exact SKU, and exact barcode only; there is no local search index or fuzzy match.
- Offline stock hints come from the cached catalog snapshot; there is no offline reservation, so two devices can oversell the same stock until sync resolves it.
- Sale line prices offline come from the cached catalog price; the server re-prices on sync and a queued sale can therefore fail or change total. **Repaired** — see [Review Repair 01](POS-REACT-RMAP-21-REVIEW-REPAIR-01-offline-cash-finality.md): an offline Cash line now carries a server-signed price lease and the server bills the leased price, so a shelf price edited while the device was offline no longer rewrites a sale already paid in cash.
- No conflict UX yet for a queued sale rejected on sync (closed shift, revoked device, withdrawn product) — needs 21H.
- Personal-domain offline stores (Utang, todo) remain outbox-envelope only.

## Next

RMAP-21E — offline Personal/business domain read models, then 21H sync processor with conflict resolution UX.
