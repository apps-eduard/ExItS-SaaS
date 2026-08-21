# POS React — Responsive Bottom Navigation UX Repair 01

**Status:** `AWAITING_PRODUCT_OWNER_CHATGPT_REVIEW`  
**Branch:** `feat/pos-react-client`  
**Starting HEAD:** `174b94493941b22764245edff329cfeb3bc70db1` (RMAP-21 review repair 01)  
**Package:** Adaptive org bottom navigation (not a new RMAP)

## Scope

Improve org POS React shell navigation so phone and small-tablet users get a persistent bottom-tab experience, while `lg+` (1024px+) keeps the existing top-bar desktop paradigm without a stretched mobile bar.

## Architecture before / after

| Surface | Before | After |
|---|---|---|
| Org phone / tablet portrait | Top bar only; destinations on role-home tiles | Fixed 5-tab bottom nav + top bar |
| Org tablet landscape ≥1024 / desktop | Top bar | Unchanged (bottom nav `lg:hidden`) |
| Personal | Existing 5-tab bottom nav | Unchanged |
| More | No org hub | `/more` permission-filtered hub |

### Primary tabs (permission-filtered)

Order when Sell is available: **Home · Catalog · Sell · Orders · More**

- **Sell** is centered, primary treatment, **Banknote** (money) icon
- Catalog falls back to `/inventory` when catalog manage is denied but inventory is allowed
- Orders falls back to `/customers` when customer-orders view is denied
- Unauthorized destinations are omitted (not disabled decoys)

## Sell floor

- Floating **View Cart** bottom offset raised above bottom nav below `lg`; restored at desktop
- Product grid uses `auto-fill` + max card width (`.sell-product-grid`) so sparse catalogs do not stretch oversized cards

## Localization

New `org.nav.*` / `org.more.*` keys with parity across `en`, `fil-PH`, `ceb-PH`, `ilo-PH`, `hil-PH`.

## Tests

- Unit: `src/features/shell/org-nav-config.test.ts`
- E2E: `e2e/responsive-bottom-nav.spec.ts` (4 cases)
- Screenshots: `e2e/responsive-bottom-nav-screenshots.spec.ts` → `docs/Mobile-React/Reports/impl-pos-react-responsive-bottom-nav/`

## Visual validation (inspected)

| Viewport | Result |
|---|---|
| 390×844 phone | Bottom nav present; Sell centered with banknote; Home active styling |
| 390×844 sell | Sell active muted background; content above nav |
| 768×1024 tablet portrait | Bottom nav balanced (constrained inner width); product cards not stretched |
| 1024×768 landscape | Bottom nav hidden; landscape cart panel visible |
| 1440×900 desktop | Bottom nav hidden; top bar + landscape cart |

## Explicit exclusions

- Device Management UX Repair WIP (left uncommitted / not finished)
- No RMAP-21 financial/offline changes
- No backend authorization changes
- Protected historical report PNGs untouched

## Quality gates (client)

| Gate | Result |
|---|---|
| Prettier (touched files) | PASS |
| `tsc -b` | PASS (with concurrent device WIP locales present in tree) |
| ESLint | PASS (0 errors; pre-existing warnings) |
| Vitest shell + message parity | PASS (14) |
| `vite` build | PASS |
| Playwright bottom-nav + screenshots | PASS (5) |

## Flags

`RESPONSIVE_BOTTOM_NAV_UX_REPAIR=AWAITING_PRODUCT_OWNER_CHATGPT_REVIEW`  
`NEXT_RMAP_AUTHORIZED=NO`  
`PRODUCTION_CUTOVER=NO`
