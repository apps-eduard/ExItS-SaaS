# RMAP-11b — Commercial Discount UX

## Status

**COMPLETE**

## Baseline

starting SHA: `dd9f3222` (Master Run 02 tip after RMAP-11; verified clean/pushed)

## Contract

| Area | Finding |
|------|---------|
| Quote | `POST /api/v1/pos/sales/quote` with `CheckoutSaleRequest` lines + optional discounts |
| Checkout | `POST /api/v1/pos/sales` accepts additive `discounts` intents |
| Intent | `CommercialDiscountIntentRequest`: Scope `Line`\|`Sale`, Method `Percentage`\|`FixedAmount`, Value, Reason (required), optional `ProductId` / `LineNumber` |
| Authority | Server quote/checkout compute all money; **UnitPrice never mutated client-side** |
| Auth UI | `canApplyCommercialDiscount`: Owner / StoreManager / Manager — **not** Cashier, **not** OrgAdmin alone |
| Auth server | ApplyCommercialDiscount required when intents present; Cashier DENY proven |
| Friendly wording | Total Amount / Discount / Amount to Pay (never GrossSubtotal in UI) |
| Zero total | Cash allowed; Amount Tendered / Change 0; UI “No payment required” |

## Implementation

- `pos-capabilities.ts` — `canApplyCommercialDiscount`
- `pos-sales-client.ts` — `quoteSale()`; `discounts` on `checkoutSale`
- `CheckoutCashPage` — discount panel (authorized roles), quote refresh on cart/discount change, money summary from quote, zero-total cash path
- i18n en + fil-PH
- Vitest capability + client + error mapping
- Playwright `e2e/rmap-11b-commercial-discount.spec.ts`

## Exclusions

- Price Override (RMAP-12b / B01)
- Promotions / coupons / regulatory Senior/PWD
- Card / GCash / Utang UI (**RMAP-12**)
- Offline discount (server fail-closed; not exposed)
- RMAP-TAX

## Implementation SHA

`f9fd88a4` (feat); docs SHA recorded in Cursor response (package report omits tip SHA per commit rules)

## Validation

### React gates

| Gate | Result |
|------|--------|
| typecheck | PASS |
| lint | PASS (0 errors; existing react-refresh warnings only) |
| format:check | PASS |
| Vitest | 43 files / **186** tests passed |
| build | PASS |
| Playwright `rmap-11` | **9** passed |
| Playwright `rmap-11b` | **9** passed |

Responsive matrix (discount checkout + summary):

| Viewport | Result |
|----------|--------|
| 375×812 | PASS (e2e) |
| 768×1024 | PASS (e2e) |
| 1024×768 | PASS (e2e) |
| 1440×900 | PASS (e2e) |

### Proven behaviors

- Owner/Manager discount UI: line + sale, % + fixed, reason required
- Quote drives Total Amount / Discount / Amount to Pay
- Full discount → “No payment required”; POST `amountTendered: 0`
- Cashier: no discount controls
- Cashier discount POST → 403 (server reject path covered in e2e + client test)
- UnitPrice / snapshots not sent on discount checkout

### Flags

- `RMAP_11B_PASS=YES`
- `RMAP_11B_QUOTE=YES`
- `RMAP_11B_ZERO_TOTAL_CASH=YES`
- `RMAP_11B_CASHIER_DENY=YES`
- `RMAP_11B_NO_UNITPRICE_MUTATION=YES`
- `RMAP_11B_NO_PRICE_OVERRIDE=YES`
- `RMAP_11B_NO_GCASH_UTANG_CARD=YES`
- `HARD_STOP=NO`

## Docs also in this package

- RMAP-10b report Owner decision line corrected to YES (real browser/PWA PosDevice; Development bypass rejected)
- Roadmap / matrices / Master Run 02 updated for RMAP-11b COMPLETE

## Next

**RMAP-12 — Payments expansion + void** when authorized. Do not start without authorization.
