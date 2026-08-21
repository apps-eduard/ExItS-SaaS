# RMAP-20 — Reports + management dashboard

## Status

**PASS** (pending parent commit + native-speaker review)

| Flag | Value |
| --- | --- |
| `RMAP_20_AUTHORIZED` | YES (authorized after RMAP-19 PASS) |
| `RMAP_20_PASS` | PASS |
| `RMAP_20_CLIENT` | PASS |
| `RMAP_20_CAPABILITIES` | PASS |
| `RMAP_20_DASHBOARD` | PASS |
| `RMAP_20_REPORTS` | PASS |
| `RMAP_20_I18N` | PASS |
| `RMAP_20_VITEST` | PASS |
| `RMAP_20_E2E` | PASS |
| `RMAP_20_TYPECHECK` | PASS |
| `RMAP_20_NATIVE_SPEAKER` | PENDING |
| Tax UI exposed | **NO** |
| Fake P&L | **NO** |
| Buyer purchase projection | **NO** |
| `RMAP_TAX_AUTHORIZED` | **NO** |
| `RMAP_B04_AUTHORIZED` | **NO** |
| `HARD_STOP` | YES — do **not** start RMAP-21 |

## Contract

| Area | Finding |
| --- | --- |
| API | Existing `/api/v1/pos/management/overview`, `/api/v1/pos/dashboard`, `/api/v1/pos/reports/*` — **no invented contracts** |
| Date range | Explicit `fromDate`/`toDate` (`yyyy-MM-dd`); presets Today / Yesterday / This week / This month / Custom; UTC calendar-day membership per `ReportDateRange` |
| Branch | Bound branch shown + workspace switch; totals remain **organization-wide** (server does not branch-filter these aggregates) |
| Sales metrics | Gross, voids, returns, Net, Cash / GCash / Utang from proven DTOs — **not** tax terminology |
| Commercial discounts | Period commercial-discount totals are **not** in report DTOs — UI states unavailable; **not invented** |
| Export | Deferred footnote only (MAUI parity) — no unsafe client export |
| Tax / VAT / BIR | No nav, widgets, or report routes (`RMAP_TAX_AUTHORIZED=NO`) |
| Fake P&L / COGS / valuation | Not claimed / not linked |
| Buyer purchase projection | Not implemented (`RMAP_B04_AUTHORIZED=NO`) |
| Capabilities | `ViewDashboard` / `ViewReports` Owner/Admin/StoreManager/ReportingUser (+ org management); Cashier DENY dashboard/sales; Cashier may open shifts reports; InventoryStaff inventory/purchasing subset |
| Offline | OnlineRequired residual |
| Locales | en, fil-PH, ceb-PH, ilo-PH, hil-PH |

## Implementation

- `pos-reporting-client.ts` — typed client for management overview, dashboard, classic + operational reports
- `report-date-range.ts` — UTC presets → explicit dates
- `report-access.ts` — hub groups + per-kind role matrix
- Features under `src/features/reports/` — dashboard, hub, operational, classic pages + filters
- Routes: `/dashboard`, `/reports`, `/reports/operational/:kind`, `/reports/{sales\|utang\|inventory\|expenses}`
- Capabilities + SessionGuards + role-home links
- i18n `dashboard.*` / `reports.*` in five locales
- Vitest `rmap-20-reports.test.ts` + capability coverage
- Playwright `e2e/rmap-20-reports-dashboard.spec.ts`
- Report + roadmap + Master Run 03 stub

## Exclusions

- Tax / VAT / BIR UI (`RMAP_TAX_AUTHORIZED=NO`)
- Fake P&L / COGS / inventory valuation
- Buyer Personal/Org purchase-history projection (`RMAP_B04_AUTHORIZED=NO`)
- Client-side invented accounting or commercial-discount period aggregates
- File export (deferred)
- Migrations / backend changes
- Native-speaker i18n sign-off
- Commits / SHAs (deferred to parent)

## Validation

### React gates

| Gate | Result |
| --- | --- |
| prettier (touched) | PASS |
| typecheck | PASS |
| Vitest (reports focused) | PASS |
| Playwright `rmap-20-reports-dashboard` | PASS |

Responsive matrix (dashboard + reports hub):

| Viewport | Result |
| --- | --- |
| 375×812 | PASS (e2e) |
| 768×1024 | PASS (e2e) |
| 1024×768 | PASS (e2e) |
| 1440×900 | PASS (e2e) |

### Proven behaviors

- Management overview KPIs (today business date) + period dashboard Cash/GCash/Utang/voids
- Report filters send explicit dates; branch context + org-wide note; UTC calendar note
- Sales summary Gross / voids / returns / Net; payment report Cash/GCash/Utang
- Inventory status + purchasing summary where capability allows
- Cashier denied `/dashboard` and sales reports; hub shifts link retained
- No tax/P&L/buyer-purchase nav; `/personal/purchase-history` not a seller report surface

## Exact next

**HARD STOP.** Do **not** start RMAP-21 until authorized. Do **not** start RMAP-TAX or RMAP-B04. Native-speaker i18n review remains PENDING.
