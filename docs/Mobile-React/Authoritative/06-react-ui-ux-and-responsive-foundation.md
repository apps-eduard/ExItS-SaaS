# React UI/UX and Responsive Foundation

**Status:** AUTHORITATIVE for React migration UI Definition of Done
**Audience:** Every visual React WP after roadmap approval
**Related roadmap:** [Migration/react-migration-roadmap.md](Migration/react-migration-roadmap.md) → **RMAP-00**

## Hard UI requirement (OWNER-CONFIRMED)

React is:

| Priority | Target | Role |
|----------|--------|------|
| Primary | **Phone** | Primary operational experience |
| High | **Tablet** (portrait + landscape) | High-priority operational experience |
| Required | **Desktop** | Supported; management/reporting optimized where appropriate |

Desktop layout must **not** dictate mobile UX. Mobile-first; tablet-strong; desktop-capable.

## UI Definition of Done

A UI WP is **NOT PASS** merely because API works, a component renders, or unit tests pass.

Required:

| Gate | Required |
|------|----------|
| Functional PASS | YES |
| Mobile UX PASS | YES (if UI) |
| Tablet UX PASS | YES (if UI) |
| Desktop PASS | YES (if UI) |
| Accessibility PASS | YES (if UI) |
| Responsive validation | YES (if UI) |
| E2E where applicable | YES |
| Related docs updated | YES |

Technically working but visibly poor phone/tablet UX = **PARTIAL**, not PASS.

## Required behavior

- No normal-workflow horizontal overflow
- No clipped totals, action buttons, or critical business values
- Touch-friendly controls; minimum effective touch target ≈ **44px** (`--exits-touch-target-min` exists in `globals.css`)
- Long English and Filipino labels must fit
- Large currency values and decimal quantities/UOM must fit
- Keyboard/focus accessibility for browser UI
- Loading / empty / error / denied / offline states must be responsive
- Dialogs/sheets usable on phone
- No hover-only critical actions
- Safe-area and sticky-bottom behavior where required

## Visual style

Clean, modern, professional, compact enough for POS, touch-friendly, consistent — **not** excessively rounded.

| Element | Radius treatment |
|---------|------------------|
| Search fields | Rounded |
| Filter buttons | Rounded / pill |
| Filter chips | Pill |
| Statuses | Pill |
| Cards | Medium radius |
| Dialogs/sheets | Medium radius |
| Forms | Consistent medium radius |

Do **not** turn every row, section, table cell, or surface into floating rounded bubbles. Use hierarchy, spacing, typography, borders, and surfaces intentionally.

## Reuse-before-create rule

Before creating feature-specific controls, WPs must inventory and **reuse or extend** existing shared components.

Standardize **interaction patterns**, not genericize unrelated business-domain logic across aggregates.

## Current shared inventory (baseline React client)

Client: `ExItS.PinoyBusinessPOS.React`

**RMAP-00 foundation:** IMPLEMENTED for the shared primitives listed under EXISTS below. Sell floor is the first reuse proof (`SellFloorPage` / `SellCartPanel` consume `SearchField`, `MoneyDisplay`, `QuantityStepper`, `LoadingSkeleton`, `StickyActionBar`).

### EXISTS (reuse)

| Component | Path |
|-----------|------|
| PageHeader | `src/components/exits/PageHeader.tsx` |
| LoadingState | `src/components/exits/LoadingState.tsx` |
| EmptyState | `src/components/exits/EmptyState.tsx` |
| ErrorState | `src/components/exits/ErrorState.tsx` |
| SegmentedControl | `src/components/ui/segmented-control.tsx` |
| Button / Input / Card / Dropdown | `src/components/ui/*` |
| StatusChip / StatusPill (alias) | `src/components/exits/StatusChip.tsx` |
| ThemeControl / LanguageControl | `src/components/exits/` |
| AppTopBar / AccountMenu | `src/components/exits/` |
| Tokens | `src/styles/globals.css` (`--exits-*`, Tailwind `@theme`) |
| SearchField | `src/components/exits/SearchField.tsx` |
| FilterButton / FilterChips / SortButton / ListToolbar | `src/components/exits/ListToolbar.tsx` |
| EntityCard / ResponsiveEntityList | `src/components/exits/EntityCard.tsx` |
| MoneyDisplay / QuantityDisplay / QuantityStepper | `src/components/exits/MoneyQuantity.tsx` |
| MoneyInput / QuantityInput | `src/components/exits/MoneyQuantityInputs.tsx` |
| LoadingSkeleton / AccessDeniedState / ConflictState / OfflineBanner / FormSection / StickyActionBar | `src/components/exits/FoundationStates.tsx` |
| BottomSheet / ConfirmationDialog | `src/components/exits/SheetDialog.tsx` |

### PARTIAL (domain or prefs-specific; not RMAP-00 blockers)

| Pattern | Current location |
|---------|------------------|
| SelectField (prefs) | `settings-select.tsx` (prefs-oriented; remains prefs until a shared Select is needed) |
| ProductTile | Inline product buttons on sell floor (POS-specific) |
| Sell category strip | `SellCategoryFilter.tsx` (domain strip; shared FilterChips available for list toolbars) |
| ConnectivityIndicator | `ConnectivityIndicator.tsx` (shell indicator; shared OfflineBanner exists) |

### DEFERRED-TO-FIRST-CONSUMER

Date/DateTime fields, Tabs, ToggleRow (settings-select remains prefs until a shared ToggleRow is needed).

### MISSING (later consumer or domain WPs; not RMAP-00)

KpiCard, MetricStrip, Timeline, NotificationBadge, and other domain-specific checklist items not listed as EXISTS / PARTIAL / DEFERRED-TO-FIRST-CONSUMER.

## Standard ListToolbar pattern

First-class shared interaction pattern for modules where it fits (Products, Customers, Suppliers, Inventory, POs, Sales History, Returns, Orders, Staff, Branches, Devices). Do **not** force onto unfit screens.

**Phone**

```text
┌───────────────────────────────┐
│ 🔍 Search products...      × │
└───────────────────────────────┘
[ Filters 3 ] [ Sort ]

[ Active × ] [ Low stock × ] [ By weight × ]
```

**Tablet**

```text
[ 🔍 Search ... ] [ Filters 3 ] [ Sort ] [+ Product]
```

Composition: SearchField + FilterButton + SortButton + optional FilterChips + optional primary action.

## Mobile entity list pattern

When tables become unusable on phone, use tappable **EntityCard** rows. Whole safe card area navigates/selects. Do not require tiny edit icons.

Conceptual product card:

```text
Coca-Cola 1.5L                    Active
SKU: COKE15

₱72.00                            18 pcs

Tracked · Main Branch                  >
```

Tablet may use denser cards, two-column grids, or responsive tables depending on density.
Desktop may use tables where appropriate (`ResponsiveEntityList`).

## Mobile form / detail pattern

Avoid giant uninterrupted forms on phones. Use logical `FormSection`s with progressive disclosure.

Product example sections: Basic information · Selling & pricing · Inventory · Units & packaging · Expiry & batches · Supplier & purchasing · Customer ordering.

Critical primary actions may use `StickyActionBar` on mobile/tablet.

## POS-specific reusable patterns (examples)

Sell: ProductTile, SellCategoryStrip, CartLine, QuantityStepper, WeightEntrySheet, PriceDisplay, StockIndicator, CustomerPicker, PaymentMethodCard, TenderInput, SaleSummary, ReceiptView, ShiftStatusCard, RegisterSelector, BranchSelector

Supplier/purchasing: SupplierCard, PurchaseOrderStatus, ReceivingLine, DiscrepancyBadge

Ordering: OrderCard, FulfillmentBadge, PickupDeliverySelector, DeliveryFeeSummary, OrderTimeline

These are reusable **interaction** components. Do **not** create one giant generic business component that owns domain behavior across unrelated aggregates.

## Responsive visual validation targets

Every future UI WP must include representative visual validation at least equivalent to:

| Device | Viewport |
|--------|----------|
| Phone | 375 × 812 |
| Tablet portrait | 768 × 1024 |
| Tablet landscape | 1024 × 768 |
| Desktop | 1440 × 900 |

Capture screenshots/evidence into the WP report when practical. Existing Playwright already exercises 320/375/768/1024/1440 in places — extend, do not invent a parallel unverified matrix.

## RMAP-00 relationship

[RMAP-00](Migration/react-migration-roadmap.md) **COMPLETE** (Master Run 01): shared reusable responsive foundation primitives and interaction standards are in place. Later visual WPs must **reuse** these components unless the roadmap explicitly states the package has no UI dependency. Evidence: `shared-ui-foundation.test.tsx`; sell-floor reuse proof.
