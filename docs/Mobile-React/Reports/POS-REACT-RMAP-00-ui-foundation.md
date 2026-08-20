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

Foundation components use `--exits-touch-target-min`, rounded search/filter pills, medium card radius. Sell-floor regression preserved. Representative viewport Playwright suite left to e2e harness (existing matrix 320–1440); unit coverage for search clear, toolbar, stepper, confirmation.

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

Recorded after commit/push in closeout.

## Next

RMAP-B00
