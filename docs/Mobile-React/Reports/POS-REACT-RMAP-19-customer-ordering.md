# RMAP-19 — Customer ordering / storefront / pickup / delivery

## Status

**PASS** (pending parent commit + native-speaker review)

| Flag                                | Value                                       |
| ----------------------------------- | ------------------------------------------- |
| `RMAP_19_AUTHORIZED`                | YES (authorized after RMAP-18 PASS)         |
| `RMAP_19_PASS`                      | PASS                                        |
| `RMAP_19_CLIENT`                    | PASS                                        |
| `RMAP_19_CAPABILITIES`              | PASS                                        |
| `RMAP_19_BUYER_UX`                  | PASS                                        |
| `RMAP_19_SELLER_UX`                 | PASS                                        |
| `RMAP_19_STOCK_CONFLICT`            | PASS                                        |
| `RMAP_19_I18N`                      | PASS                                        |
| `RMAP_19_VITEST`                    | PASS                                        |
| `RMAP_19_E2E`                       | PASS                                        |
| `RMAP_19_TYPECHECK`                 | PASS                                        |
| `RMAP_19_NATIVE_SPEAKER`            | PENDING                                     |
| `RMAP_B05_AUTHORIZED`               | **NO**                                      |
| `RMAP_B05 accidentally implemented` | **NO**                                      |
| `HARD_STOP`                         | NO (await RMAP-20 authorization separately) |

## Contract

| Area         | Finding                                                                                                                                                                                                  |
| ------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| API          | Existing POS `/api/v1/pos/customer-orders` (+ storefront, quote-delivery, mine) and `/api/v1/pos/organizations/{org}/customer-orders` (+ accept/reject/fulfillment/complete) — **no invented contracts** |
| Discovery    | Personal linked merchants via Platform `/api/v1/personal/linked-merchants` — **not** public slug landing                                                                                                 |
| Inventory    | React is **not** inventory authority; availability from storefront DTO; place-order stock conflicts use server `pos.inventory.insufficient_stock`                                                        |
| Delivery fee | Server-authoritative via `quote-delivery` / order `DeliveryFee` — client displays only                                                                                                                   |
| Tracking     | No continuous tracking; status on refresh / navigation                                                                                                                                                   |
| Transitions  | Seller actions mirror MAUI allowed set; server remains authoritative                                                                                                                                     |
| Capabilities | `ViewCustomerOrders` Owner/Admin/StoreManager + ReportingUser; `ManageCustomerOrders` Owner/Admin/StoreManager; Cashier DENY                                                                             |
| Offline      | OnlineRequired residual                                                                                                                                                                                  |
| Locales      | en, fil-PH, ceb-PH, ilo-PH, hil-PH                                                                                                                                                                       |

## Implementation

- `pos-customer-orders-client.ts` — storefront browse, quote, place, mine, seller list/detail/transitions + stock conflict helper
- `linked-merchants-client.ts` — personal linked merchant list + ordering capability
- `personal-buyer-token.ts` — session grant for Personal buyer POS Bearer when unbound
- Features under `src/features/customer-ordering/` — cart, checkout UI helpers, buyer shop/checkout/history, seller queue/detail
- Routes: `/personal/linked-merchants*`, `/personal/orders*`, `/orders*`
- i18n `personal.*` / `orders.*` in five locales
- Vitest: cart, availability, seller actions, stock conflict, client paths, capabilities
- Playwright `e2e/rmap-19-customer-ordering.spec.ts`
- Report + roadmap status update

## Exclusions

- **RMAP-B05** public landing / `exitsapp.com/store/{slug}` / org CMS / social links / custom domains / slug redirects (`RMAP_B05_AUTHORIZED=NO` — **not started**, **not accidentally implemented**)
- Continuous GPS / live courier tracking
- React inventing delivery fees or inventory quantities
- Migrations / backend changes
- Native-speaker i18n sign-off
- Commits / SHAs (deferred to parent)

## Validation

### React gates

| Gate                                   | Result |
| -------------------------------------- | ------ |
| prettier (touched)                     | PASS   |
| typecheck                              | PASS   |
| Vitest (customer-ordering focused)     | PASS   |
| Playwright `rmap-19-customer-ordering` | PASS   |

Responsive matrix (seller queue):

| Viewport | Result     |
| -------- | ---------- |
| 375×812  | PASS (e2e) |
| 768×1024 | PASS (e2e) |
| 1024×768 | PASS (e2e) |
| 1440×900 | PASS (e2e) |

### Proven behaviors

- Personal linked-merchant shop: branch select, browse, cart, checkout
- Pickup vs delivery; delivery address/coords; fee from server quote
- Stock conflict on place → message + refresh affordance, then successful place
- Buyer order detail/history (no live tracking copy)
- Seller queue filters + accept → prepare → ready → collected → complete
- Cashier denied `/orders`
- `/store/{slug}` not found (no B05 public landing)

## Exact next

Do **not** start RMAP-B05 (`RMAP_B05_AUTHORIZED=NO`). RMAP-20 authorized and complete — see [POS-REACT-RMAP-20-reports-dashboard.md](./POS-REACT-RMAP-20-reports-dashboard.md). Native-speaker i18n review remains PENDING.
