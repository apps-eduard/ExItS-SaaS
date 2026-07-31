# P11-WP05 — Shared Reporting Framework

Package: **P11-WP05 — Shared Reporting Framework**  
Prior tip: `ab216eabc73546bbc3c8b52bb2668afebec4911b`  
Feature tip (this WP): `4d832b39d85d7f8db55234f609188666035f34c5`  
Docs tip: _(recorded after docs commit)_

## Status

**Complete.** Platform Admin has one authoritative shared reporting framework (native CSS/Razor composition). It reuses P11-WP03 forms and P11-WP04 tables/cards/status. Representative Admin dashboard and Payments surfaces prove filters, KPIs, tables, totals/grouping, and states. POS MAUI report pages remain migration debt for P11-WP06. No new analytics, exports, printing, or business calculations.

## Discovery

| Finding | Action |
|---|---|
| Admin dashboard used ad-hoc KPI cards without report shell | Migrated to `ReportPageShell` / `ReportKpiGrid` / `ReportKpiCard` |
| Payments list had filters but no shared report filter/date UX | Migrated list to reporting filter bar + date/quick-range + `ReportTable` |
| POS MAUI reports (`SalesReportPage`, etc.) still DesignSystem-ad-hoc | Deferred to **P11-WP06** (documented debt) |
| Platform Payments API has no paid-at date query | Date range UI validates bounds only; list queries remain status/product/org server filters |
| WP03 forms + WP04 tables already suitable | Reporting wrappers compose them — no second table/form system |

## Framework components

| Component | Role |
|---|---|
| `ReportPageShell` | Wide page frame + `.report-page` composition |
| `ReportHeader` | Title / breadcrumb / description via `PageHeader` |
| `ReportFilterBar` | Apply/reset filter chrome (P11-WP03 actions) |
| `ReportDateRangeFilter` | From/to date fields via `FormField` + `AdminInput` |
| `ReportQuickRangeSelector` / `ReportQuickRangeHelper` | Today / Last7 / Last30 / ThisMonth; inclusive span validation |
| `ReportKpiGrid` / `ReportKpiCard` / `ReportSummaryCard` | KPI presentation (static, non-clickable) |
| `ReportSection` | Section chrome via `DetailSection` |
| `ReportTable` | Thin wrap of `AdminDataTable` |
| `ReportTotalsRow` / `ReportGroupHeader` | Totals and group presentation |
| `ReportStatusBadge` | Wrap of `StatusBadge` |
| `ReportLoading/Empty/Error/AccessDenied/ConflictState` | State wrappers |
| `ReportDataPanel` | Wrap of `AdminDataPanel` |
| `ResponsiveReportLayout` | Filters / KPIs / body slots |

## Usage conventions

```razor
<ReportPageShell>
  <ReportHeader Title="..." Breadcrumb="..." Description="..." />
  <ResponsiveReportLayout>
    <Filters>
      <ReportFilterBar OnApply="ApplyAsync" OnReset="ResetAsync">
        <ReportQuickRangeSelector ... />
        <ReportDateRangeFilter ... />
        <!-- page-owned filters -->
      </ReportFilterBar>
    </Filters>
    <Kpis>
      <ReportKpiGrid><ReportKpiCard Title="..." Value="..." /></ReportKpiGrid>
    </Kpis>
    <ChildContent>
      <ReportSection Title="...">
        <ReportDataPanel ...>
          <ReportTable ... />
          <ReportTotalsRow ... />
        </ReportDataPanel>
      </ReportSection>
    </ChildContent>
  </ResponsiveReportLayout>
</ReportPageShell>
```

Pages own title, authorization, filters, server queries, KPIs, columns/rows, groups/totals, links, and empty wording. Framework owns layout, responsive behavior, filter placement, KPI/table presentation, states, localization hooks, and theme classes.

## Migrated examples

| Surface | Coverage |
|---|---|
| `AdminDashboard.razor` | Report shell, KPI grid, loading/error states; values from `GetPortfolioSummaryAsync` |
| `Payments.razor` (list) | Filter bar, quick ranges, bounded date validation, status/product/org server filters, `ReportTable`/`ReportTotalsRow`/`ReportGroupHeader`, pagination |

Detail payment view left on prior detail chrome (not a report).

## Filter / KPI / table / state behavior

- **Filters:** Apply/reset; quick ranges set from/to; invalid/oversized ranges blocked client-side before reload; Payments list reload uses existing server filters only
- **KPIs:** Server counts formatted for display; partial failures show `—`; no invented trends/percentages
- **Tables:** `ReportTable` → `AdminDataTable` (desktop table + mobile cards)
- **Totals/grouping:** Page row count from server page items; group header presentation only
- **States:** Loading/empty/error via `ReportDataPanel`; denied/conflict wrappers available for later pages

## Server-authority rules

Preserved: organization isolation, authorization, deterministic ordering, server paging, no hidden client financial recalculation, no profit/COGS/valuation/tax/forecasting/AP, ManualGCash remains manual/unverified.

## Responsive strategy

- Desktop: compact filter bar, multi-column KPI grid, stable tables
- Tablet/mobile (≤800px): stacked filters, single-column KPIs, Admin card fallback for tables
- Tailwind-inspired visuals via semantic classes + existing tokens — **no Tailwind dependency**

## Browser evidence

Host: `http://127.0.0.1:5289`  
Scripts: `artifacts/p11-wp05-reporting.mjs`, `artifacts/p11-wp04-tables.mjs`, `artifacts/p11-wp03-forms.mjs`, `artifacts/p11-wp02-nav-matrix.mjs`

| Check | Result |
|---|---|
| Dashboard report shell + KPI grid | Pass |
| Payments filter bar / quick ranges / date validation | Pass |
| Payments table or empty + totals/group when rows exist | Pass |
| Dark theme on Payments | Pass |
| Tablet/mobile stacked filters; main-content no overflow | Pass |
| WP02 nav matrix | Pass |
| WP03 forms regression | Pass |
| WP04 tables regression | Pass |

Note: document-level overflow from header env chip is pre-existing shell debt; report `#main-content` validated separately.

## Tests

Full `ExItS.slnx` Release: **1177 passed / 0 failed / 0 skipped** (baseline 1168 + 9 Admin reporting tests).

Admin unit tests: **57 passed**.

## Remaining report migration debt (P11-WP06)

- POS MAUI: Dashboard, Sales, Inventory, Utang, Expenses, Operational reports, Reports hub
- Admin: Subscriptions, Entitlements, Audit, Product Access (and any other list/summary surfaces)
- Optional DesignSystem `Components/Reporting/` port for MAUI reuse
- Paid-at date query on Payments API (when authorized) so date filters can be server-backed
- Formal a11y certification not claimed

## Explicit exclusions

No exports, printing, new analytics, new business policy, Tailwind/shadcn/Flowbite/DaisyUI, or Phase 12 / Product-Foundation work.

## Exact next

**P11-WP06 — Dashboard and Report Refactoring** when explicitly authorized.
