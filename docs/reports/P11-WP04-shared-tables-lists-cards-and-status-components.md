# P11-WP04 — Shared Tables, Lists, Cards, and Status Components

Package: **P11-WP04 — Shared Tables, Lists, Cards, and Status Components**  
Prior tip: `d8c262d752b1af5efbab16872a7c5b8f0fae0786`  
Feature tip (this WP): `0351f547457522a97a168b802ec050ef6f37ee83`  
Docs tip: `3f0add52e73f61a69c48ccca721c28eb29c96cf6`

## Status

**Complete.** Platform Admin has one authoritative data-display foundation (native CSS/Razor). Representative list/detail pages use shared tables, pagination, amounts, badges, and states. DesignSystem data components remain for POS MAUI; Admin does not reference DesignSystem. No business-rule changes.

## Discovery

| Finding | Action |
|---|---|
| Admin tables were ad-hoc `data-table responsive-table` markup | Consolidated into `AdminDataTable` (desktop table + mobile cards) |
| Showing-count text only; server `Page`/`PageSize` unused in UI | Added `AdminPagination` wired to API page parameters |
| `StatusBadge` / `UtcTimestamp` / `SummaryCard` / Loading/Empty/Error already existed | Enhanced badge semantics; added Amount/Quantity/Date; Denied/Conflict; DataPanel |
| DesignSystem `DataTable` / `ResponsiveDataList` / `MoneyDisplay` | Left in DesignSystem for MAUI; mirrored patterns in Admin naming |
| Static metric cards had hover lift | Hover motion only when `Clickable` |

## Components added or consolidated

| Component | Role |
|---|---|
| `AdminColumnDefinition` / `AdminSortState` / `AdminFilterChip` | Presentation contracts |
| `AdminDataPanel` | Loading / empty / error / denied / conflict gate |
| `AdminDataTable` | Semantic table + mobile card list; optional sortable headers; row actions |
| `AdminSortHeader` | Optional sort UI; page owns server sort |
| `AdminPagination` | Prev/next + page summary from server totals |
| `AdminFilterSummary` | Active filter chips |
| `AmountDisplay` / `QuantityDisplay` / `DateDisplay` | Culture-aware display (no recalculation) |
| `RowActions` | Action group |
| `DetailSection` / `KeyValueList` / `EntitySummaryCard` | Detail/summary composition |
| `DeniedState` / `ConflictState` | Shared data states |
| `StatusBadge` | Text + tone; not color-only |
| `SummaryCard` | Metric card; static by default |

## Usage conventions

```razor
<AdminDataPanel IsLoading="..." HasError="..." IsEmpty="..." ...>
  <AdminFilterSummary Items="chips" OnClear="ClearAsync" />
  <AdminDataTable TItem="..." Items="..." Columns="..." CellTemplate="..." RowActions="..." />
  <AdminPagination Page="..." PageSize="..." TotalCount="..." PageChanged="OnPageAsync" />
</AdminDataPanel>
```

Pages own queries, columns, permissions, and actions. Sorting/filtering/paging remain server-authoritative when invoked.

## Pages migrated

| Page | Coverage |
|---|---|
| `Products.razor` | List table + pagination; detail KeyValueList/DetailSection + nested tables |
| `Organizations.razor` | List + pagination; detail sections with amount display |
| `Payments.razor` | Filter summary, amount column, table/pagination |
| `Users.razor` | Filter chips, table, pagination |
| `OrganizationMembers.razor` | Table, role cell, row actions, pagination |

## Responsive / status / formatting

- Desktop: `.admin-data-table-wrap` table
- Mobile ≤800px: hide wrap, show `.admin-data-cards` with labeled fields
- Legacy `.responsive-table` CSS retained for unmigrated pages
- Status: visible localized text + `data-tone`; unknown values fall back to raw text
- Amounts: `CurrencyCode` + `N2` current UI culture; quantities tabular nums

## Runtime / browser evidence

Host: `http://127.0.0.1:5289`  
Scripts: `artifacts/p11-wp04-tables.mjs`, `artifacts/p11-wp02-nav-matrix.mjs`, `artifacts/p11-wp03-forms.mjs`

| Check | Result |
|---|---|
| Products/Payments shared panel (table or empty/error) | Pass |
| Dark theme on Payments | Pass |
| Organizations mobile card fallback when rows exist / state otherwise | Pass |
| Mobile drawer closes after Users nav | Pass |
| WP02 nav matrix | Pass |
| WP03 forms regression | Pass |

## Tests

Full `ExItS.slnx` Release: **1168 passed / 0 failed / 0 skipped** (baseline 1164 + 4 Admin data-display tests).

Admin unit tests: **48 passed**.

## Remaining migration debt

- Subscriptions, Entitlements, Audit, Product Access list tables still ad-hoc
- Sortable headers not wired to APIs (no client-side inventing of order)
- Focus-return and formal a11y certification not claimed
- DesignSystem ↔ Admin token convergence deferred
- Reporting framework → **P11-WP05**

## Explicit exclusions

No bulk actions, exports, new domain statuses, report calculations, or business workflow changes. Phase 12 / Product-Foundation untouched.

## Exact next

**P11-WP05 — Shared Reporting Framework** when explicitly authorized.
