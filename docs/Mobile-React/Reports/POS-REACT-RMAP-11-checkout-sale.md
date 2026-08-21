# RMAP-11 — Checkout / sale (online cash first)

## Status

**COMPLETE**

## Baseline

starting SHA: `4db1f09f` (Master Run 02 tip after RMAP-10b; verified green)

## Contract

| Area | Finding |
|------|---------|
| Endpoint | `POST /api/v1/pos/sales` online cash |
| Lines | `productId`, `quantity`, optional `sellingUnitId` + `enteredQuantity` — **no** online snapshots |
| Payment | `paymentMethod: "Cash"`; `amountTendered` required ≥ total |
| Idempotency | Client `saleId` (`crypto.randomUUID`) retained across retry |
| Shift | `shiftId` from open shift (`ShiftContext`) |
| Headers | Org, branch, Bearer, `X-Pos-Installation-Device-Id` via central `pos-http` |
| Document | Transaction Summary only — never Invoice |
| Disclaimer | `SalesDocumentWording` / MAUI `SalesDocument_DisclaimerBody` |
| Gate | Pay enabled only when cart nonempty **and** `moneyPostReady` **and** `canCreateSale` |
| Fail closed | Checkout blocked without authorized device / open shift |

## Implementation

- `src/api/pos/pos-sales-client.ts` — `checkoutSale`, `getSale`, `listSales` + zod schemas
- `mapCartLinesToCheckoutRequest` — online line mapping (no snapshots / no discounts)
- Routes: `/sell/checkout` (Cash), `/sell/sales/:saleId/summary` (Transaction Summary)
- Sell Pay navigates to checkout (does not POST from cart)
- Double-submit guard + same `saleId` retry
- Friendly error mapping (session, product access, shift, device, stock, tender)
- i18n en + fil-PH
- Vitest client/helpers + Playwright `e2e/rmap-11-checkout-sale.spec.ts`

## Exclusions

- ManualGCash / Utang / Card UX (**RMAP-12**)
- Commercial discount UX (**RMAP-11b**)
- Offline outbox / fake sale success
- Development money / device bypass
- Capacitor

## Implementation SHA

`a43d26b8` (feat); docs _(this commit)_

## Validation

### React gates

| Gate | Result |
|------|--------|
| Vitest | 43 files / **183** tests passed |
| typecheck | PASS |
| lint | PASS (0 errors; existing react-refresh warnings only) |
| format:check | PASS (after prettier) |
| build | PASS |
| Playwright `rmap-11` | **9** passed |
| Playwright `rmap-10` | **15** passed (regression) |
| Playwright `rmap-10b` | **8** passed (regression) |

Responsive matrix (checkout + summary):

| Viewport | Result |
|----------|--------|
| 375×812 | PASS (e2e) |
| 768×1024 | PASS (e2e) |
| 1024×768 | PASS (e2e) |
| 1440×900 | PASS (e2e) |

### Proven behaviors

- Real online Cash POST (mocked contract) with installation device header
- Pay disabled without authorized device even when shift open
- Success clears cart and shows Transaction Summary disclaimer (not Invoice)
- Failure keeps cart; insufficient tender / stock errors mapped
- Idempotent retry reuses the same `saleId`
- Checkout gate fail-closed when `moneyPostReady` is false

### Flags

- `RMAP_11_PASS=YES`
- `RMAP_11_MONEY_POST_READY=YES`
- `RMAP_11_DEVICE_HEADER=YES`
- `RMAP_11_IDEMPOTENCY=YES`
- `RMAP_11_NO_FAKE_SALE=YES`
- `RMAP_11_NO_DISCOUNT_UX=YES`
- `RMAP_11_NO_GCASH_UTANG_CARD=YES`
- `RMAP_11_TRANSACTION_SUMMARY_NOT_INVOICE=YES`
- `HARD_STOP=NO`

## Next

**RMAP-11b — Commercial Discount UX** (authorized separately). Do not start RMAP-12 without authorization.
