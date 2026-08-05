# Production Mobile Design System (Pinoy Business POS)

| Field | Value |
|---|---|
| Status | Active specification |
| Authority | `src/Shared/ExItS.DesignSystem/` |
| POS aliases | `src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Maui/wwwroot/app.css` |
| Reference screen | Cashier sell floor — `/sales/new` (`SaleCheckout.razor`) |
| Phase | 19 / 20 — Open; **Not Device Verified**; **Not production-ready** |

## 1. Purpose

Define a coherent, cashier-friendly visual language for Pinoy Business POS MAUI. Business behavior and authorization are unchanged. Visual principles were informed by retail density (Polaris), mobile touch (Material 3), and accessible hierarchy (Fluent 2) — **without** copying branding, packages, or source.

## 2. Authority and non-goals

**Do**

- Extend `--exits-*` tokens and Design System components
- Add thin `--pos-*` semantic aliases that map to `--exits-*`
- Prefer shared components (`Button`, `QuantityStepper`, `MoneyDisplay`, `EmptyState`, `Skeleton`, …)

**Do not**

- Add a second design system, React packages, Bootstrap, Tailwind, or ad-hoc CSS frameworks
- Imitate Shopify / Square / Google product chrome
- Hard-code one-off colors, type sizes, or touch sizes in feature pages
- Use emoji as production icons

## 3. Typography

**Stack (approved):**
`IBM Plex Sans, Source Sans 3, system-ui, -apple-system, Segoe UI, sans-serif`

| Role | Token | Approx size | Weight |
|---|---|---|---|
| Display total | `--exits-type-display` | 28px | bold |
| Page title | `--exits-type-page-title` | 22px | semibold |
| Section title | `--exits-type-section` | 17px | semibold |
| Body | `--exits-type-body` | 15px | regular |
| Compact body | `--exits-type-compact` | 14px | regular |
| Label | `--exits-type-label` | 14px | semibold (via components) |
| Helper / error | `--exits-type-helper` | 13px | regular |
| Monetary | `--exits-type-monetary` | 15px | semibold + tabular |
| Button | `--exits-type-button` | 15px | semibold |

**Tabular numerals** (`--exits-font-tabular` / `.pos-tabular` / `MoneyDisplay`) for prices, quantities, totals, tendered, change, and stock.

POS helpers: `.pos-type-page-title`, `.pos-type-section`, `.pos-type-helper`.

## 4. Surfaces, color, spacing

POS aliases (light/dark via Design System themes):

| Alias | Maps to |
|---|---|
| `--pos-surface-page` | `--exits-bg` |
| `--pos-surface-panel` | `--exits-surface` |
| `--pos-surface-raised` | `--exits-surface-elevated` |
| `--pos-border-subtle` | `--exits-border` |
| `--pos-text-primary` / `--pos-text-secondary` | `--exits-text` / `--exits-text-muted` |
| `--pos-accent` / `--pos-danger` / `--pos-success` | primary / danger / success |
| `--pos-total-font` | `--exits-type-display` |
| `--pos-touch-target` | `--exits-touch-target-min` (3rem / 48dp) |
| `--pos-category-width` | ~20% |
| `--pos-cart-width` | ~25% |

Prefer whitespace and alignment over heavy borders. Use muted surfaces; avoid wrapping every block in a large card.

## 5. Touch and accessibility

- Minimum interactive target: **48dp** (`--exits-touch-target-min: 3rem`)
- Visible `:focus-visible` rings (`--exits-focus`)
- Selected / pressed / disabled / loading / error states on controls
- Do not communicate state with color alone (e.g. `aria-pressed`, in-cart border + quantity)
- Respect `prefers-reduced-motion`
- Support EN + Filipino expansion; avoid fixed heights that clip text
- Semantic labels on steppers, category chips, product tiles, cart sheet

## 6. Cashier sell floor (reference)

### Landscape / tablet (`min-width: 900px` and landscape)

| Region | Approx width | Behavior |
|---|---|---|
| Categories | 20% | Vertical chips; independent scroll |
| Products | ~55% | Browse grid; independent scroll |
| Cart | 25% | Lines + totals; independent scroll |

Sticky portrait cart bar is hidden. Payment section remains below the floor.

### Portrait / narrow

- Horizontal category chips
- Product list/grid
- Sticky compact cart summary (`View cart` + total)
- Cart bottom sheet / drawer
- Checkout / payment remains the primary commit action in the payment section

### Product row / tile

- 48–64px placeholder thumbnail
- Name ≤ 2 lines
- Price prominent; unit secondary
- In-cart: `QuantityStepper` (minus / qty / plus), ≥48dp
- Selected/in-cart without loud fills

### Cart line

- Name; unit × qty; line total
- Compact stepper
- Remove is Ghost and spatially separated (not adjacent-only destructive)

### Category switching

Filters browse products only. **Never** clears the cart. Quantities stay reflected on tiles. Totals / tendered / change remain stable.

## 7. Components

| Need | Use |
|---|---|
| ± quantity | `QuantityStepper` |
| Money | `MoneyDisplay` (`Emphasized` for totals) |
| Primary CTA | one `Button` Primary per screen region |
| Initial load | `Skeleton` (not full-page spinner after first paint) |
| Empty | `EmptyState` |
| Errors | `Alert` / `ErrorState` |

## 8. Performance

- Cart quantity taps are **in-memory** (`SaleCartService`) — no API per ±
- Stable `@key` on product rows
- Debounced search + stale-generation discard
- Preserve cart across category and orientation changes
- Lazy thumbnails when images exist (placeholder today)

## 9. Correct vs incorrect

**Correct**

- `font-size: var(--exits-type-section)`
- `min-height: var(--exits-touch-target-min)`
- `color: var(--pos-text-secondary)`

**Incorrect**

- `font-size: 13px` / `min-height: 40px` sprinkled in a page
- Second CSS framework for one screen
- Clearing cart when a category chip is tapped
- Full-page spinner on every browse refresh

## 10. Rollout sequence

1. Spec + tokens (this doc)
2. Shared components / CSS
3. Cashier sell floor (reference)
4. Checkout / payment chrome
5. Catalog / customers / inventory ops pages (incremental)
6. Personal + organization surfaces (incremental)
7. Visual + a11y verification on device widths 360 / 412 / 640–740 landscape / tablet

## 11. Related

- [Mobile production UI redesign report](../../reports/mobile-production-ui-redesign.md)
- [P11-WP01 UI audit](../../reports/P11-WP01-web-ui-audit-and-component-inventory.md)
- [P19-WP04 cashier selling](../../reports/P19-WP04-mobile-cashier-selling-experience.md)
