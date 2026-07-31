# P11-WP06 — Dashboard and Report Refactoring

Package: **P11-WP06 — Dashboard and Report Refactoring**  
Prior tip: `e5d004000db7ffe4a19743c6f280b0eacd94c5ec`  
Feature tip (this WP): `6688fa674e5edc139a931dae3faefeb8b25a806b`  
Docs tip: _(recorded after docs commit)_

## Status

**Complete.** Admin landing dashboard is a polished portfolio composition on the P11-WP05 reporting framework. Authorized Admin list/report surfaces use shared report shell/filters/tables. POS operational reports remain MAUI-only (documented). No new analytics or business calculations. No Tailwind.

## Dashboard before / after

| Before (WP05 proof) | After (WP06) |
|---|---|
| Single flat 8-card KPI grid | Primary KPI row + lifecycle secondary grid |
| No operational shortcuts | Authorized quick-link summary cards |
| Minimal landing atmosphere | `.dashboard-landing` layered background, header panel, primary card emphasis |
| Same portfolio API | Same `GetPortfolioSummaryAsync` — no invented metrics |

## Pages migrated

| Page | Change |
|---|---|
| `AdminDashboard.razor` | Polished landing: sections, primary/secondary KPIs, ops links, states |
| `Subscriptions.razor` (list) | Report shell, filter bar, table, pagination; `?status=` deep link |
| `Entitlements.razor` (list + history) | Report shell + table (+ pagination on latest) |
| `Audit.razor` (list) | Report shell + filter bar + table; permission gate preserved |
| `Products.razor` (list) | Report shell + `ReportTable` |
| `Organizations.razor` (list) | Report shell + `ReportTable` |
| `Users.razor` (list) | Report shell + filter bar + `ReportTable` |
| `Payments.razor` (list) | Already on WP05 framework (retained) |

## Not in Admin (documented debt)

Sales, inventory, purchasing, suppliers, expenses, cashier shifts, returns/refunds, Product-Based Utang reports exist only in POS MAUI (`DashboardPage`, `*ReportPage`, `OperationalReportPage`, `ReportsHub`). No Admin equivalents invented.

## Framework reuse

Composes P11-WP02 shell, WP03 forms, WP04 tables/status, WP05 reporting components. `ReportTable` → `AdminDataTable`; KPI cards remain non-clickable; ops use explicit `<a>` links.

## Visual direction

Tailwind-inspired SaaS dashboard via semantic CSS + ExItS tokens (IBM Plex Sans / Source Sans 3). No Tailwind/shadcn/Flowbite/DaisyUI.

## Authoritative data

Portfolio counts and list rows from existing Platform APIs only. Partial failures still show `—`. No trends, percentages, forecasts, profit, COGS, valuation, or tax metrics.

## Browser / screenshots

Host: `http://127.0.0.1:5289`  
Script: `artifacts/p11-wp06-dashboard.mjs`  
Screenshots: `artifacts/p11-wp06-screenshots/`

| Evidence | Result |
|---|---|
| Light desktop dashboard | Pass — `dashboard-light-desktop.png` |
| Dark desktop dashboard | Pass — `dashboard-dark-desktop.png` |
| Mobile dashboard | Pass — `dashboard-mobile.png` |
| Subscriptions/Products/Payments report shell | Pass |
| WP02–WP05 regressions | Pass |

## Tests

Full `ExItS.slnx` Release: **1181 passed / 0 failed / 0 skipped** (baseline 1177 + 4 dashboard refactoring tests).

Admin unit tests: **61 passed**.

## Remaining debt (→ P11-WP07 / later)

- POS MAUI report pages onto DesignSystem/reporting patterns
- OrganizationMembers / Product Access polish
- Payments paid-at server date filter (API gap)
- Formal a11y certification not claimed
- Header env-chip document overflow (pre-existing)

## Exact next

**P11-WP07 — Localization, Theme, Accessibility, and Responsive QA** when explicitly authorized.
