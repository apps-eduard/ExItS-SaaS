# Global Search + Filter Pattern

[UI design system](ui-design-system.md) | [Component catalog](reusable-component-catalog.md) | [Localization](localization.md)

This document defines the canonical shared search and filter **presentation** pattern for ExItS surfaces that consume `ExItS.DesignSystem`.

---

## Separation of concerns

| Layer | Owns |
|---|---|
| **`SearchBar`** (shared) | Visual design, search input interaction, search icon, clear action, optional filter trigger, active-filter badge, optional chip row slot |
| **Feature / page** | Search semantics, filter model, query/API calls, authorization, sorting, pagination, result rendering |

Do **not** add feature-specific properties to `SearchBar` (category IDs, order status, date ranges, barcode rules, pagination models, etc.).

Do **not** create a global filter model shared across products.

---

## Component location and API

**Path:** `src/Shared/ExItS.DesignSystem/Components/Data/SearchBar.razor`

**Namespace:** `ExItS.DesignSystem.Components.Data`

**Related:** `SearchBox` (search-only thin wrapper), `SearchToolbar` (SearchBar + optional `Actions` slot)

```razor
<SearchBar @bind-Value="_search"
           Placeholder="@L["Catalog_SearchShort"]"
           AriaLabel="@L["Catalog_SearchShort"]"
           Disabled="@_loading"
           ShowFilterButton="true"
           ActiveFilterCount="@ActiveFilterCount"
           FiltersExpanded="@_filtersOpen"
           FiltersControlsId="catalog-filters"
           FilterLabel="@L["Catalog_FiltersTitle"]"
           OnFilterClick="ToggleFilters"
           OnClear="ClearSearchAsync"
           OnSearch="ReloadAsync"
           DebounceMilliseconds="0">
    <FilterChips>
        @* optional chip markup owned by the page *@
    </FilterChips>
</SearchBar>
```

| Parameter | Purpose |
|---|---|
| `Value` / `ValueChanged` | Bound search text (`string?`) |
| `Placeholder`, `AriaLabel` | Page-localized strings |
| `Disabled` | Loading / offline / auth gates |
| `InputId` | Associate external `<label for>` when needed |
| `DebounceMilliseconds` | Optional debounce before `ValueChanged` (0 = immediate) |
| `OnSearch` | Enter key callback (use **either** debounced `ValueChanged` or `OnSearch` for API refresh — not both for the same reload) |
| `OnClear` | Optional hook after clear resets value |
| `ShowFilterButton` | Sliders filter affordance (44px+ touch target) |
| `ActiveFilterCount` | Meaningful non-search filters only; badge hidden at 0 |
| `FiltersExpanded`, `FiltersControlsId` | `aria-expanded` / `aria-controls` for filter panel |
| `OnFilterClick` | Opens page-owned filter sheet/panel |
| `FilterChips` | Optional render fragment under the row |

Generic UI strings (`Search`, `Clear search`, `Filters`, active-filter count) come from `DesignSystemResources` (`en` + `fil-PH`). Page placeholders remain feature-owned.

---

## Canonical mobile layout

```text
[ 🔍 Search products...          × ] [ sliders ]
```

When filters are active:

```text
[ 🔍 Search products...          × ] [ sliders 2 ]
```

Optional chips underneath:

```text
[ Beverages × ] [ Available × ]
```

- Rounded field consistent with POS shell (`--exits-radius-full` where available)
- Compact height via `--exits-control-height` with `min-height: --exits-touch-target-min` (≥44px)
- Search field flexes to remaining width; no horizontal overflow at 360 / 390 / 430px
- Clear `×` only when text exists
- Filter button uses `sliders` icon glyph

Tablet and desktop use the **same component** — no alternate search implementation.

---

## Filter standard (page-owned)

`SearchBar` does **not** render filter fields.

1. Filter button opens the feature filter UI (bottom sheet, drawer, inline panel, or modal — existing page pattern).
2. Page owns filter fields, validation, state, and result refresh.
3. **Apply** updates results and closes the panel.
4. **Reset** clears feature filters.
5. `ActiveFilterCount` excludes search text.
6. Optional chips reflect important active filters; removing a chip updates page-owned state.

---

## Debounce guidance

| Pattern | Recommendation |
|---|---|
| Server-backed list with keystroke reload | Keep page-owned debounce **or** set `DebounceMilliseconds` on `SearchBar` — not both |
| Local/in-memory filter (e.g. reports hub) | `DebounceMilliseconds="0"` |
| Explicit search button (Organization Web) | Bind value only; call `OnSearch` / page `SearchAsync` on Enter or button |

---

## Adoption checklist for new pages

1. Use `<SearchBar>` — do not add page-local search markup/CSS.
2. Keep API/query/filter logic in the page service layer.
3. Pass localized placeholder/`AriaLabel` from product resources.
4. Wire filter UI separately; pass `ActiveFilterCount` for non-search filters only.
5. Add regression tests if the page has custom debounce or scanner behavior.

---

## Migration inventory (PinoyBusinessPOS MAUI)

**Migrated to `SearchBar`:** Customers, Inventory, Catalog (products, categories, global browse, today's prices, import ×2, connected buyer availability), Sales list, Shifts, Registers, Suppliers (+ connected catalog ×2, linked products, buyer shared products), Expenses (+ categories), Purchasing (direct purchases, create modal, receive stock), Reports hub, Personal merchant shop, Dev component showcase.

**Intentional exception:**

| Page | Reason |
|---|---|
| `Sales/SaleCheckout.razor` | Scanner-first product lookup with inline category chips and helper text — specialized cashier UX; not a list search bar |

---

## Organization Web (PWA)

Organization Web list pages use `SearchBar` with DesignSystem CSS loaded in `App.razor`. Search buttons and separate filter fields (status, date range, branch) remain page-owned beside the shared search field.

---

## Platform Admin

`SearchInput.razor` wraps `SearchBar` for future Admin list surfaces. Ant Design–embedded compact toolbars (e.g. template composition transfer dual-pane filters with Apply/Reset) may keep Ant `Input` where the shared bar does not fit the layout — document per page when added.

---

## Accessibility

- Native `type="search"` input with visible focus ring
- Localized `aria-label` on input, clear, and filter buttons
- Active filter count announced via visually hidden text when badge shown
- Filter button: `aria-expanded`, `aria-controls` when panel id provided
- Minimum 44px touch targets on clear and filter actions

---

## Tests

`tests/ExItS.DesignSystem.Tests/SearchBarComponentTests.cs` guards component contract, CSS layout tokens, wrapper delegation, and MAUI migration coverage.
