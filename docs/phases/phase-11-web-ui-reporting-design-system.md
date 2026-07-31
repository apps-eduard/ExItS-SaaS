# Phase 11 — Web UI and Reporting Design System

[Dashboard](../portfolio-progress.md) | [All Phases](README.md) | [Previous](phase-10-full-pos.md)

## Status

**Complete with documented risks.** **P11-WP01**–**P11-WP08** are **complete**. Phase 11 closed. Exact next: **Phase 12 — Reusable SaaS Product Foundation and Bootstrap** when authorized (do not begin).

Pre-Phase-11 Admin shell/theme baseline remains locked (feature tip `46b99a7f6baa87977fb0ed37e678231fa1eb1344`). Theme reapply uses `Blazor.enhancedload` — do **not** reintroduce document-wide permanence attributes on `<html>`.

## Progress

| WP | Status | Report / tip |
|---|---|---|
| P11-WP01 — Web UI Audit and Component Inventory | **Complete** | [report](../reports/P11-WP01-web-ui-audit-and-component-inventory.md) · `221fe69ab179956e8a73411cf3eb58fd6f199c3c` |
| P11-WP02 — Global Web Layout and Navigation | **Complete** | [report](../reports/P11-WP02-global-web-layout-and-navigation.md) · `7ce7df139a9494c9aab7d189900e96d5e43fdc1d` |
| P11-WP03 — Shared Forms, Validation, and Dialogs | **Complete** | [report](../reports/P11-WP03-shared-forms-validation-and-dialogs.md) · `6825b8eb423e73cd5d3dc24e393e7201b04232bc` |
| P11-WP04 — Shared Tables, Lists, Cards, and Status Components | **Complete** | [report](../reports/P11-WP04-shared-tables-lists-cards-and-status-components.md) · `0351f547457522a97a168b802ec050ef6f37ee83` |
| P11-WP05 — Shared Reporting Framework | **Complete** | [report](../reports/P11-WP05-shared-reporting-framework.md) · `4d832b39d85d7f8db55234f609188666035f34c5` |
| P11-WP06 — Dashboard and Report Refactoring | **Complete** | [report](../reports/P11-WP06-dashboard-and-report-refactoring.md) · `6688fa674e5edc139a931dae3faefeb8b25a806b` |
| P11-WP07 — Localization, Theme, Accessibility, and Responsive QA | **Complete** | [report](../reports/P11-WP07-localization-theme-accessibility-responsive-qa.md) · `24ee744fa15152bc325568ba6c5a99de78359921` |
| P11-WP08 — Phase 11 Closeout | **Complete** | [report](../reports/P11-WP08-phase-11-closeout.md) · *(tip filled after commit)* |

## Purpose

Phase 11 standardizes and finalizes the ExItS web user interface after the Phase 10 business workflows are complete.

The phase focuses on reusable web components, consistent page composition, responsive behavior, localization, theme support, accessibility, and a shared reporting framework.

This phase must not redesign business rules or add new POS domain capabilities unless a later work package explicitly authorizes them.

## Phase Objective

Create a production-quality shared web UI foundation that can be reused across:

- Dashboard
- Products
- Sales
- Customers and Product-Based Utang
- Inventory
- Suppliers
- Purchasing
- Expenses
- Cashier Shifts
- Returns and Refunds
- Reports
- Administration surfaces already authorized for the POS product

The web application must provide a consistent experience across desktop, tablet, and mobile-browser layouts.

## Architectural Principles

1. Reuse before creation.
2. One shared web design system must remain authoritative.
3. Feature pages compose shared components instead of duplicating controls.
4. Business behavior remains server-authoritative.
5. UI state must not become a second source of business truth.
6. Platform and POS databases remain separate.
7. No cross-product database access or foreign keys.
8. No PHI is introduced into POS.
9. ExItS remains independent from the removed HealthCare workspace.
10. Production authentication and POS operational roles remain separate roadmap items unless explicitly authorized.

## Phase Scope

### Included

- audit of the existing web UI
- audit of existing reusable components
- consolidation of duplicate components
- global layout and navigation refinement
- responsive desktop, tablet, and mobile-browser behavior
- shared forms and validation patterns
- shared tables, cards, dialogs, and status components
- shared loading, empty, error, denied, conflict, and continuity states
- shared report page framework
- reusable report filters
- reusable KPI and summary components
- reusable report tables
- reusable totals and grouped sections
- global English and Filipino localization review
- global System, Light, and Dark theme review
- accessibility review
- visual consistency review
- regression testing of completed POS workflows through the web UI
- documentation and component usage guidance

### Excluded

- new POS business modules
- production authentication implementation
- POS operational role implementation
- Windows MAUI support
- MAUI redesign
- accounting
- payroll
- bank reconciliation
- tax or fiscal certification
- payment-gateway integration
- GCash verification
- warehouse or branch management
- new reporting calculations not already supported by authoritative domain data
- arbitrary exports or printing unless authorized by a dedicated work package
- dashboard analytics that require new business policy

## Existing Foundation Assumption

The repository is expected to already contain shared web UI foundations and the shared design system.

Before implementation, Cursor must inspect the actual repository and identify:

- current shared Razor components
- current shared CSS and design tokens
- current layout and navigation components
- current table, form, card, dialog, badge, and state components
- current localization infrastructure
- current theme infrastructure
- current report pages
- duplicated feature-specific UI
- inconsistent responsive behavior

Do not assume a component exists merely because it was planned. Verify it in the repository.

## Phase Work Packages

### P11-WP01 — Web UI Audit and Component Inventory

#### Objective

Create an authoritative inventory of the current web UI and identify reuse, duplication, inconsistency, and missing shared foundations.

#### Deliverables

- component inventory
- page inventory
- duplicate-component report
- design-token inventory
- localization coverage report
- theme coverage report
- responsive-layout audit
- accessibility gap list
- prioritized refactoring plan
- approved component naming and placement conventions

#### Required Review Areas

- application shell
- top navigation
- side navigation
- breadcrumbs
- page headers
- action bars
- cards
- forms
- validation messages
- tables
- pagination
- filters
- dialogs
- confirmation prompts
- status badges
- KPI cards
- loading states
- empty states
- error states
- access-denied states
- conflict states
- offline or continuity states
- responsive breakpoints
- localization
- themes

#### Acceptance

- every active web page is inventoried
- duplicates are identified
- no new design system is created
- the shared component roadmap is approved
- no business workflow is changed

#### Implementation status

**Complete** — see [P11-WP01 report](../reports/P11-WP01-web-ui-audit-and-component-inventory.md). Audit only; Admin shell/theme baseline preserved.

---

### P11-WP02 — Global Web Layout and Navigation

#### Objective

Finalize the shared web application shell and navigation experience.

#### Deliverables

- reusable application shell
- consistent sidebar and top-bar behavior
- responsive navigation
- page-title and breadcrumb conventions
- shared action-area pattern
- consistent content-width rules
- global spacing and typography rules
- organization and product context display
- safe user/session context display when available

#### Requirements

- desktop layout
- tablet layout
- mobile-browser layout
- keyboard navigation
- screen-reader-friendly landmarks
- English and Filipino
- System, Light, and Dark themes
- no feature-specific duplicate navigation shells

#### Exclusions

- production login redesign
- new role-based navigation policy
- Windows MAUI navigation

#### Implementation status

**Complete** — see [P11-WP02 report](../reports/P11-WP02-global-web-layout-and-navigation.md). Routing defect (`data-permanent` on `<html>`) fixed; single `MainLayout` shell.

---

### P11-WP03 — Shared Forms, Validation, and Dialogs

#### Objective

Standardize data-entry and confirmation experiences across all web modules.

#### Shared Components

- form section
- labeled field
- required-field indicator
- text input
- numeric input
- money input
- quantity input
- date input
- date-range input
- select and searchable select
- textarea
- checkbox and toggle
- validation summary
- inline validation message
- save bar
- confirmation dialog
- destructive confirmation dialog
- conflict dialog
- unsaved-changes warning

#### Requirements

- server errors map consistently to fields and page-level messages
- duplicate-submit protection
- loading and disabled states
- accessible labels and error associations
- no client-authoritative business calculations
- shared monetary and quantity formatting
- shared localization
- shared theme behavior

#### Implementation status

**Complete** — see [P11-WP03 report](../reports/P11-WP03-shared-forms-validation-and-dialogs.md). Shared Admin form/field/actions/dialog foundation; Users, OrganizationMembers, Payments migrated as proof.

---

### P11-WP04 — Shared Tables, Lists, Cards, and Status Components

#### Objective

Standardize data presentation across operational pages.

#### Shared Components

- data table
- responsive table-to-card layout
- sortable header
- filter summary
- pagination
- row action menu
- bulk-selection shell where already authorized
- status badge
- amount display
- quantity display
- date/time display
- entity summary card
- metric card
- detail section
- key-value list
- timeline or activity list where supported

#### Requirements

- deterministic ordering
- consistent empty state
- consistent loading state
- consistent error state
- accessible table headers
- no color-only status meaning
- desktop table and mobile card behavior
- safe organization-scoped navigation

#### Exclusions

- new bulk business actions
- new export behavior
- new domain status values

#### Implementation status

**Complete** — see [P11-WP04 report](../reports/P11-WP04-shared-tables-lists-cards-and-status-components.md). Shared Admin data table/card/pagination/status foundation; Products, Organizations, Payments, Users, OrganizationMembers migrated as proof.

---

### P11-WP05 — Shared Reporting Framework

#### Objective

Create one reusable reporting framework for all current and future POS reports.

#### Core Shared Components

- `ReportPageShell`
- `ReportHeader`
- `ReportFilterBar`
- `ReportDateRangeFilter`
- `ReportQuickRangeSelector`
- `ReportKpiGrid`
- `ReportKpiCard`
- `ReportSummaryCard`
- `ReportSection`
- `ReportTable`
- `ReportTotalsRow`
- `ReportGroupHeader`
- `ReportStatusBadge`
- `ReportLoadingState`
- `ReportEmptyState`
- `ReportErrorState`
- `ReportAccessDeniedState`
- `ReportConflictState`
- `ResponsiveReportLayout`

#### Report Definition Model

Each report should provide only its own:

- title
- description
- authorization requirement
- filters
- KPI definitions
- columns
- rows
- grouping
- totals
- source links
- empty-state wording

The report framework owns:

- layout
- responsive behavior
- standard filter placement
- KPI presentation
- table presentation
- state handling
- localization hooks
- theme behavior
- accessibility

#### Initial Report Areas

The framework must support existing authorized reports for:

- sales
- inventory
- purchasing
- suppliers
- expenses
- cashier shifts
- returns and refunds
- Product-Based Utang
- dashboard summaries

#### Rules

- reports use server-authoritative data
- report filters are bounded
- organization isolation is enforced
- no cross-product reporting
- no hidden client recalculation of financial totals
- no profit, COGS, tax, valuation, or accounting claims unless already authorized by domain scope
- ManualGCash remains manual and unverified
- returns remain separate from original gross sale records
- shift expected cash follows the authoritative formula
- inventory remains movement-derived

#### Responsive Behavior

Desktop:

- full filter bar
- KPI grid
- tables with stable columns
- grouped summaries

Tablet:

- compact filter layout
- reduced KPI columns
- horizontally safe tables or card fallback

Mobile browser:

- stacked filters
- KPI cards
- report rows rendered as cards when tables are not usable
- no hidden critical values

#### Acceptance

- at least the principal existing reports use the shared framework
- no separate report-specific layout system remains without documented justification
- all reports support English and Filipino
- all reports support System, Light, and Dark themes
- all reports have consistent loading, empty, error, denied, and conflict states
- responsive behavior is validated

#### Implementation status

**Complete** — see [P11-WP05 report](../reports/P11-WP05-shared-reporting-framework.md). Shared Admin reporting framework composing WP03/WP04; Admin dashboard + Payments list migrated as proof. Broad POS/Admin report rollout reserved for P11-WP06.

---

### P11-WP06 — Dashboard and Report Refactoring

#### Objective

Refactor the existing dashboard and report pages onto the shared reporting framework.

#### Required Areas

- dashboard summary cards
- sales report
- inventory report
- purchasing report
- supplier report
- expense report
- cashier-shift report
- returns and refunds report
- Product-Based Utang report

#### Requirements

- preserve existing calculations
- preserve existing authorization
- preserve existing filters unless explicitly improved without changing meaning
- preserve organization isolation
- remove duplicated report layout code
- align terminology across reports
- align date-range behavior
- align pagination and sorting
- align responsive behavior

#### Exclusions

- new analytics
- demand forecasting
- profitability analysis
- accounting reports
- tax reports
- inventory valuation

#### Implementation status

**Complete** — see [P11-WP06 report](../reports/P11-WP06-dashboard-and-report-refactoring.md). Admin dashboard polished landing; Subscriptions/Entitlements/Audit/Products/Organizations/Users lists on shared reporting framework. POS operational reports remain MAUI-only debt.

---

### P11-WP07 — Localization, Theme, Accessibility, and Responsive QA

#### Objective

Complete cross-cutting quality validation for the web UI.

#### Localization

Validate:

- English
- Filipino (`fil-PH`)
- labels
- buttons
- validation messages
- empty states
- report headings
- filter labels
- status labels
- date and money formatting

#### Themes

Validate:

- System
- Light
- Dark
- contrast
- focus states
- charts or visual elements if present
- print-safe behavior only when printing is authorized

#### Accessibility

Validate:

- keyboard navigation
- visible focus
- semantic headings
- landmarks
- form labels
- validation associations
- table headers
- dialog focus trapping
- screen-reader text
- no color-only meaning
- minimum touch target sizes for mobile browser

#### Responsive QA

Validate:

- common desktop resolutions
- tablet portrait and landscape
- mobile-browser widths
- overflow behavior
- long Filipino labels
- long product and supplier names
- large numbers
- empty and error states

#### Implementation status

**Complete** — see [P11-WP07 report](../reports/P11-WP07-localization-theme-accessibility-responsive-qa.md). Localization/theme/a11y/responsive QA hardening for Admin; Product Access localized; dialog focus trap/return; no formal WCAG claim.

---

### P11-WP08 — Phase 11 Closeout

#### Objective

Close Phase 11 with complete evidence and no unfinished design-system migration hidden as done.

#### Required Evidence

- component inventory
- shared component documentation
- report framework documentation
- web-page coverage matrix
- localization evidence
- theme evidence
- accessibility evidence
- responsive screenshots
- full automated test results
- manual web workflow validation
- open risks
- deferred work
- exact next phase

#### Acceptance

- all authorized web pages use the approved shared foundation
- report pages use the shared report framework
- duplicate components are removed or explicitly justified
- completed Phase 10 workflows still function correctly
- no business scope was unintentionally changed
- all tests pass
- documentation matches implementation
- `main` matches `origin/main`
- working tree is clean

## Shared Web Component Structure

Recommended logical structure:

```text
Shared Web UI
├── Layout
│   ├── AppShell
│   ├── Sidebar
│   ├── TopBar
│   ├── Breadcrumbs
│   └── PageHeader
├── Forms
│   ├── FormSection
│   ├── Field
│   ├── ValidationSummary
│   ├── MoneyInput
│   ├── QuantityInput
│   ├── DateRangeInput
│   └── SaveBar
├── Data Display
│   ├── DataTable
│   ├── ResponsiveList
│   ├── EntityCard
│   ├── StatusBadge
│   ├── MetricCard
│   └── KeyValueList
├── Feedback
│   ├── LoadingState
│   ├── EmptyState
│   ├── ErrorState
│   ├── AccessDeniedState
│   ├── ConflictState
│   └── ContinuityState
├── Dialogs
│   ├── ConfirmationDialog
│   ├── DestructiveActionDialog
│   └── UnsavedChangesDialog
└── Reports
    ├── ReportPageShell
    ├── ReportFilterBar
    ├── ReportDateRangeFilter
    ├── ReportKpiGrid
    ├── ReportKpiCard
    ├── ReportSection
    ├── ReportTable
    ├── ReportTotalsRow
    └── ResponsiveReportLayout
```

The actual component names must follow repository conventions discovered during P11-WP01.

## Reporting Data Rules

### Sales

- original completed sales remain authoritative
- voids follow existing treatment
- returns and refunds are shown separately
- net sales must be clearly labeled
- no profit or COGS unless later authorized

### Inventory

- on-hand remains movement-derived
- purchase receipts, sales, void restores, manual adjustments, and stock counts remain separate movement sources
- no valuation

### Purchasing

- operational ordered, received, and outstanding values only
- no accounts payable
- no supplier balance
- no tax or landed cost

### Cashier Shifts

- physical cash includes Cash only
- ManualGCash and Product-Based Utang are reported separately
- Cash refunds reduce expected physical cash exactly once
- no payroll or accounting treatment for variance

### Returns and Refunds

- returns remain separate immutable records
- original sale totals are not rewritten
- refund method follows original payment method

### Expenses

- expenses remain separate from cashier CashOut unless a future package explicitly links them

## Security Requirements

- trusted organization context
- trusted actor context where applicable
- concealed cross-organization resources
- no client-authoritative permissions
- no full financial request-body logging
- no sensitive supplier or customer data leakage
- no new browser storage of sensitive authoritative state
- no weakening of Phase 9 controls
- no weakening of Phase 10 authorization or concurrency safeguards

## Testing Strategy

### Automated

- shared component rendering tests
- form validation tests
- table and pagination tests
- report filter tests
- authorization-state tests
- localization tests
- theme tests
- accessibility checks where tooling supports them
- responsive rendering tests where supported
- API contract regression
- business workflow regression
- architecture tests preventing duplicate design-system foundations where practical

### Manual Web Validation

Validate complete workflows for:

- product creation
- sale
- Product-Based Utang
- inventory adjustment
- supplier creation
- purchase order and receiving
- stock count
- expense
- cashier shift
- return and refund
- reports

Validate on:

- Windows desktop browser
- tablet-sized browser viewport
- mobile-sized browser viewport

Suggested browsers:

- Microsoft Edge
- Google Chrome

## Phase Exit Criteria

Phase 11 is complete when:

- the existing web UI has been fully audited
- one authoritative reusable web component foundation is documented
- duplicate components are removed or justified
- all active reports use the shared reporting framework where applicable
- responsive desktop, tablet, and mobile-browser behavior is validated
- English and Filipino are validated
- System, Light, and Dark themes are validated
- accessibility requirements are met or documented as open risks
- no completed business workflow regressed
- no unauthorized business capability was introduced
- all tests pass
- documentation is complete
- validated commits are pushed
- `main` equals `origin/main`
- the working tree is clean

## Remaining Risks to Preserve

Unless completed by another authorized work package, Phase 11 must continue to report:

- R-091 — production authentication remains open
- POS operational roles remain open
- R-109 — Android interactive/device validation remains open when no device evidence exists
- R-129 — local database encryption/package risk remains open when unresolved
- Production TLS remains required
- MAUI HTTPS-only production enforcement remains required
- ManualGCash remains manually confirmed and unverified

## Suggested Phase Marker

```text
P11-web-ui-reporting-design-system
```

## Suggested Primary Report

```text
docs/reports/P11-web-ui-reporting-design-system.md
```

## Final Response Format for Each Work Package

Each P11 work-package completion response should report:

1. Status
2. Delivered scope
3. Shared components added or consolidated
4. Pages migrated
5. Report-framework impact
6. Localization, theme, accessibility, and responsive evidence
7. Preserved business rules and exclusions
8. Test results
9. Manual browser evidence
10. Remaining risks
11. Git commits and final tip
12. Exact next work package
