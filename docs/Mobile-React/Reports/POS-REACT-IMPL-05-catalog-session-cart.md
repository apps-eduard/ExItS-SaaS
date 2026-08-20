# POS-REACT-IMPL-05 — Catalog Search + Session Cart

**Package:** POS-REACT-IMPL-05  
**Worktree:** `ExItS-SaaS-pos-react-client` (`feat/pos-react-client`)  
**Client root:** `src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Client/`  
**Status:** Complete in worktree (not committed)  
**Base commit:** `e953839099d6382cf59d136d8a3872865830bef7` (WP04 clean tip)  
**Expected commit message:** `feat(pos-react): add catalog search and session cart`

---

## Delivered capability

- Vite `/pos-api` dev/preview proxy (loopback `8092` default) mirroring the Platform proxy pattern
- PWA Workbox `NetworkOnly` routing for `/pos-api/` plus validation script enforcement
- Hand-typed POS catalog HTTP client (`/pos-api` relative origin, Bearer grant token, org/branch headers)
- Catalog endpoints: list/search products, list categories, lookup by SKU, lookup by barcode
- Debounced sell-floor search order: barcode exact → SKU exact → name search (bounded page size)
- Unknown barcode: alert only — no silent product create
- In-memory session cart (tab lifetime only): add/increment, qty ±, remove, clear, subtotal
- Cart survives category/orientation/search changes; clears on sign-out
- PWA update guard blocks refresh while cart has lines
- Sell-floor wiring: real browse grid, category chips from API, landscape cart + phone sheet with live lines
- Pay button remains **disabled** (checkout deferred to WP06) but shows line count when non-empty

---

## Explicit exclusions

- Cash checkout / POST sales (WP06)
- Offline catalog source-of-record, LocalStore, Background Sync
- Silent product create from sell floor
- Platform / MAUI / POS API C# changes
- Typed OpenAPI client generation (`TYPED_CLIENT_GENERATION_CONTRACT_MISSING` remains **OPEN**)

---

## Key files

| Area | Path |
|---|---|
| POS proxy | `vite.pos-api-proxy.ts`, `vite.config.ts` |
| POS HTTP | `src/api/pos/pos-http.ts` |
| Catalog client | `src/api/pos/pos-catalog-client.ts`, `pos-catalog-types.ts`, `catalog-lookup.ts` |
| Session cart | `src/cart/SessionCartProvider.tsx`, `SessionCartLifecycle.tsx` |
| Sell floor | `src/features/sell/SellFloorPage.tsx`, `SellCartPanel.tsx` |
| PWA guard | `src/pwa/apply-pwa-update.ts`, `scripts/validate-pwa.mjs` |

---

## Sell-floor regions (`data-testid`)

| Region | Purpose |
|---|---|
| `sell-search` | Debounced HID/search input |
| `sell-search-error` | Unknown barcode / load errors |
| `sell-categories` | API-backed category chips |
| `sell-product-{id}` | Product tile (tap to add) |
| `sell-cart-line-{id}` | Cart line with qty controls |
| `sell-cart-subtotal` | Live subtotal |
| `sell-pay` | Disabled Pay (shows count when cart non-empty) |

---

## Validation evidence

| Command | Result |
|---|---|
| `npm run typecheck` | PASS |
| `npm run lint` | PASS (0 errors; react-refresh warnings only) |
| `npm run format:check` | PASS |
| `npm run test` | PASS — 53 Vitest tests |
| `npm run test:e2e` | PASS — 29 Playwright tests |
| `npm run test:pwa` | PASS |

Screenshots: `docs/Mobile-React/Reports/impl-pos-react-05-catalog-session-cart/`

---

## Tests added

- Vitest: `src/cart/session-cart.test.tsx`
- Vitest: `src/api/pos/catalog-lookup.test.ts`
- Vitest: `src/features/sell/sell-floor.test.tsx` (catalog + cart)
- Vitest: `vite.pos-api-proxy.test.ts`
- Vitest: `src/pwa/pwa.test.tsx` (cart blocks PWA apply)
- Playwright: `e2e/sell-floor-catalog-cart.spec.ts`
- Playwright: `e2e/sell-floor.spec.ts` (pos-api mocks)

---

## Next work package

**POS-REACT-IMPL-06** — cash checkout / POST sales (not authorized in this recovery scope until explicitly approved).
