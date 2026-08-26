# POS-REACT-IMPL-04 — POS Sell-Floor Shell

**Package:** POS-REACT-IMPL-04  
**Worktree:** `ExItS-SaaS-pos-react-client` (`feat/pos-react-client`)  
**Client root:** `src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.React/`  
**Status:** Complete in worktree (not committed)  
**Base commit:** `d1c35bdcbae88c5bcde1c3302f4e7986abb7b82c` (WP03 clean tip)  
**Expected commit message:** `feat(pos-react): add pos sell-floor shell`

---

## Delivered capability

- Extended in-memory session grant store with role facts (`mappedPosRoleCode`, `productLocalRoleCode`, `membershipRole`, `organizationManagementAuthority`, `productAccessAllowed`)
- Pure POS capability helpers (`canEnterSellFloor`, `canCreateSale`, role home resolver)
- `SellingModeProvider` analogue (`enter` / `exit` / `clear` on sign-out)
- Routes: `/sell`, `/role/owner`, `/role/manager`, `/role/cashier`, `/org`
- Post-bind navigation to role home; Cashier primary CTA opens sell floor
- Sell-floor UI shell with responsive browse/cart regions (placeholders only)
- Access restricted page when bound but lacking sell/create capability
- EN + fil-PH strings for sell floor, role homes, org essentials

---

## Role and capability gates

| Grant shape | Role home | `/sell` |
|---|---|---|
| Cashier / Manager / Owner POS role + `productAccessAllowed` | Matching role home | Allowed |
| Organization owner membership without POS role | `/org` | Denied |
| `productAccessAllowed === false` | `/` or bind failure | Denied |

Owner membership alone does **not** authorize sell floor or pay chrome.

---

## Sell-floor regions (`data-testid`)

| Region | Purpose |
|---|---|
| `sell-floor` | Root layout |
| `sell-search` | HID/search input (autofocus, no API) |
| `sell-categories` | Placeholder chips (All + 2 stubs) |
| `sell-products` | Skeleton grid + catalog placeholder copy |
| `sell-cart-landscape` | Side cart at ≥900px landscape |
| `sell-cart-bar` | Sticky phone/portrait summary (`0 items · —`) |
| `sell-cart-sheet` | Phone drawer from bar |
| `sell-pay` | Disabled Pay (checkout deferred) |

Tablet landscape uses ~75/25 browse|cart split via CSS in `globals.css`.

---

## Explicit exclusions

- Catalog API calls, session cart lines, cash checkout, POST sales
- Platform / MAUI / POS API C# changes
- PIN enrollment or device removal (MOBILE-D-060 remains open)
- Platform Admin visual language

---

## Validation evidence

| Command | Result |
|---|---|
| `npm run typecheck` | PASS |
| `npm run lint` | PASS (0 errors; react-refresh warnings only) |
| `npm run format:check` | PASS |
| `npm run test` | PASS — 44 Vitest tests |
| `npm run test:e2e` | PASS — 27 Playwright tests |

Screenshots: `docs/Mobile-React/Reports/impl-pos-react-04-sell-floor-shell/`

---

## Tests added

- Vitest: `src/access/pos-capabilities.test.ts`
- Vitest: `src/features/sell/sell-floor.test.tsx`
- Playwright: `e2e/sell-floor.spec.ts` (375 / 1024×768 / 1440 viewports)
- Playwright: updated `e2e/auth-session.spec.ts` for Cashier grant mock + role home landing

---

## Key files

| Area | Path |
|---|---|
| Capabilities | `src/access/pos-capabilities.ts` |
| Grant store | `src/api/platform/pos-session-grant.ts` |
| Selling mode | `src/selling/SellingModeProvider.tsx` |
| Sell UI | `src/features/sell/SellFloorPage.tsx` |
| Role homes | `src/features/role/*` |
| Routes/guards | `src/app/router.tsx`, `src/session/SessionGuards.tsx` |
| i18n | `src/i18n/messages.ts` |
| Layout CSS | `src/styles/globals.css` |

---

## Next package

`POS-REACT-IMPL-05` — catalog search and session cart (`feat(pos-react): add catalog search and session cart`).
