# RMAP-00 — React Shared UI/UX & Responsive Foundation

**Status:** PASS  
**Baseline SHA:** `f72afcd526f37d77e9a537515ef9f2830d630917`  
**Branch:** `feat/pos-react-client`

## Objective

Establish shared mobile-first / tablet-strong / desktop-capable React interaction primitives for later Master Run 01 visual packages.

## Dependencies

None.

## Source audit summary

| Pattern | Classification | Notes |
|---------|----------------|-------|
| PageHeader, LoadingState, EmptyState, ErrorState | REUSE | exits/ |
| SegmentedControl, Button, Input, Card | REUSE | ui/ |
| StatusChip | EXTEND | added danger tone + StatusPill alias |
| Sell search input | EXTRACT → SearchField | sell-floor now consumes SearchField |
| Sell category chips | REUSE (sell-specific) | FilterChips is general; category strip remains sell-local |
| Cart qty +/- | EXTRACT → QuantityStepper | SellCartPanel reuses |
| formatPeso | EXTEND → MoneyDisplay | display component wrapper |
| ConnectivityNotice | REUSE | OfflineBanner added as parallel shared banner |
| Cart sheet / sticky bar | EXTRACT → StickyActionBar (+ BottomSheet available) | sell cart bar uses StickyActionBar |
| ListToolbar / EntityCard / ResponsiveEntityList | CREATE | new |
| FormSection / ConfirmationDialog / MoneyInput / QuantityInput | CREATE | product-neutral |
| Date/DateTime, Tabs, ToggleRow, SelectField product-neutral | DEFERRED-TO-FIRST-CONSUMER | settings-select exists; date/tabs not needed yet |

## Implementation

New shared components under `src/components/exits/`:

- SearchField, ListToolbar (+ FilterButton, FilterChips, SortButton)
- EntityCard, ResponsiveEntityList
- MoneyDisplay, QuantityDisplay, QuantityStepper
- MoneyInput, QuantityInput
- LoadingSkeleton, AccessDeniedState, ConflictState, OfflineBanner, FormSection, StickyActionBar
- BottomSheet, ConfirmationDialog
- StatusChip danger + StatusPill alias

Proof of reuse: SellFloorPage + SellCartPanel consume SearchField, LoadingSkeleton, MoneyDisplay, QuantityStepper, StickyActionBar without new business behavior.

## Tests

- `npm run typecheck` PASS
- `npm test` 86 passed (added `shared-ui-foundation.test.tsx`)
- Existing sell-floor tests PASS

## UI validation

Foundation components use `--exits-touch-target-min`, rounded search/filter pills, medium card radius. Sell-floor regression preserved. Unit coverage for search clear, toolbar, stepper, confirmation.

### Responsive closeout (Master Run 01 Repair 02)

Playwright command (client project):

```
npx playwright test e2e/rmap-00-responsive.spec.ts
```

Result: **5 passed** (4 viewports + delayed-catalog LoadingSkeleton).

| Viewport | SearchField | QuantityStepper | MoneyDisplay | LoadingSkeleton | StickyActionBar | Sell/cart layout | Horizontal overflow | Search focus |
|----------|-------------|-----------------|--------------|-----------------|-----------------|------------------|---------------------|--------------|
| 375 × 812 | PASS (`sell-search`) | PASS (cart sheet) | PASS (`sell-product-price-*`) | PASS (delayed catalog) | PASS (`sticky-action-bar` + `sell-cart-bar`) | Phone bar + sheet | PASS (≤1px) | PASS (focused) |
| 768 × 1024 | PASS | PASS (cart sheet) | PASS | Covered on 375 | PASS | Tablet portrait bar + sheet | PASS | PASS |
| 1024 × 768 | PASS | PASS (landscape cart) | PASS | Covered on 375 | Hidden by landscape split (expected) | Split browse + cart | PASS | PASS |
| 1440 × 900 | PASS | PASS (landscape cart) | PASS | Covered on 375 | Hidden by landscape split (expected) | Desktop split | PASS | PASS |

Screenshots: `docs/Mobile-React/Reports/impl-pos-react-rmap-00-responsive/{375x812,768x1024,1024x768,1440x900}.png`

No unexplained overflow or clipped Pay/cart actions. Landscape StickyActionBar hiding at ≥900px landscape is the existing sell-floor split, not a defect.

## Known limitations

- Date/DateTime, Tabs, ToggleRow deferred to first consumer
- SellCategoryFilter not forced into FilterChips (different interaction: selection strip vs removable filters)
- Full ListToolbar demo screen deferred; pattern exported for RMAP-04+

## Authoritative docs updated

- 06-react-ui-ux-and-responsive-foundation.md
- Migration/react-current-state.md
- Migration/capability-parity-matrix.md
- Migration/react-migration-roadmap.md
- Migration/validation-matrix.md

## Commits / push

- Implementation (RMAP-00): `391330852918dd990c5a9053af5943cb8da91407`
- Original docs: `c4b82ace89a1d87d14ae4dfdd31c6c2d4e8e02ae`
- Responsive validation code: `fd19f2ecaf111ee5d1ff59581c05783cb6e0ea1f`
- Responsive validation docs: recorded after this closeout commit

**Final RMAP-00 status:** PASS (viewport evidence closed).

## Next

RMAP-B00 identity reconciliation (formal same-human link). Do not start RMAP-01 in this repair run.
