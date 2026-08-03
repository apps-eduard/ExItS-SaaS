# Ant Design Admin UI Standards

## Status

**Authoritative UI and content-layout standard for ExItS Platform Administration and Organization Administration.**

All new Admin pages must follow this standard. Existing pages should be aligned gradually when they are modified. Functional behavior, authorization, tenant isolation, audit, and business rules remain authoritative and must not be weakened for visual consistency.

## 1. Design goals

The Admin UI must be professional, modern, compact, content-first, easy to scan, consistent, responsive, dark-theme compatible, accessible, and built primarily with Ant Design Blazor components.

The interface must not look like a raw developer page, unrelated blocks placed vertically, flat borderless surfaces, a page with excessive empty space, or different applications stitched together.

## 2. Core visual principles

### 2.1 Content hierarchy first

Every page must clearly answer:

1. Where am I?
2. What is the current state?
3. What is the most important action?
4. What supporting information is available?
5. What history or related records can I inspect?

Arrange content in this order:

```text
Page identity
→ current state or summary
→ primary actions
→ main working content
→ related information
→ history or audit
```

### 2.2 One visual system

Use the same page header pattern, spacing scale, card treatment, status tags, table density, form layout, button hierarchy, date and money formatting, and loading/empty/error/success states.

Do not invent a new layout for every page.

### 2.3 Compact, not crowded

Avoid oversized titles, very tall cards, excessive card padding, large empty areas, one field per full-width row when two columns fit naturally, and unnecessary decorative sections.

## 3. Page canvas and spacing

Use the full available Admin content area.

- Do not place normal Admin content inside a narrow centered column.
- Tables should use the available width.
- Forms may use a practical maximum width.
- Commercial and dashboard pages may use responsive grids.

Recommended values:

```text
Desktop horizontal padding: 24px
Tablet horizontal padding: 20px
Mobile horizontal padding: 16px
Major section gap: 24px
Related-content gap: 16px
Compact control gap: 8px
Card internal padding: 20px to 24px
Form content maximum width: approximately 760px to 900px
```

Use existing application spacing tokens where available.

## 4. Surface and card standard

Cards group meaningful related content. Do not wrap every small element in a card.

### 4.1 Card appearance

In light mode, important cards must stand clearly above the page background.

Use a subtle border, soft visible shadow, consistent corner radius, clear internal padding, and a slightly elevated hover state only when clickable.

Recommended behavior:

```text
Light mode:
- white or theme surface background
- subtle neutral border
- soft shadow visible against the page background

Dark mode:
- dark theme surface
- restrained border
- softer shadow or elevation
- enough contrast without bright glow
```

Suggested CSS concept:

```css
.admin-card {
    border: 1px solid var(--admin-border-color);
    border-radius: 10px;
    background: var(--admin-surface-color);
    box-shadow:
        0 1px 2px rgba(0, 0, 0, 0.05),
        0 6px 18px rgba(0, 0, 0, 0.08);
}

[data-theme="dark"] .admin-card {
    box-shadow:
        0 1px 2px rgba(0, 0, 0, 0.30),
        0 8px 20px rgba(0, 0, 0, 0.20);
}
```

Adapt selectors and theme tokens to the existing implementation. Prefer Ant Design theme tokens over hard-coded values.

### 4.2 Shadow rules

Use shadows for summary cards, commercial plan cards, action cards, dashboard metric groups, and important independent sections.

Avoid very dark shadows, glow effects, different shadow styles on every page, heavy shadows around tables inside cards, and nested cards with competing shadows.

### 4.3 Card hierarchy

Use primary cards for major sections, smaller summary blocks in grids, and plan cards for commercial choices. Avoid deep card nesting.

## 5. Typography

Use the existing Ant Design typography scale.

- Page title: concise, one line where practical, not oversized.
- Subtitle: one short sentence using secondary text color.
- Section heading: consistent size and weight.
- Labels: secondary emphasis.
- Important values: normal or medium emphasis.
- Essential values must not use very faint text.

## 6. Standard page header

Every major page must use:

```text
Breadcrumb or back link, when needed

Page title                              Primary action
Short subtitle                         Secondary actions
```

Example:

```text
Users                                      [+ Create User]
Manage platform identities and access.
```

Rules:

- primary action is on the right on desktop
- actions wrap or stack cleanly on mobile
- do not repeat the page title inside the first card
- status may appear beside the title on detail pages
- destructive actions belong in a secondary menu or separated danger action

Recommended reusable component: `AdminPageHeader`.

## 7. Standard list page

Used for users, organizations, products, plans, subscriptions, entitlements, roles, and audit records.

```text
┌───────────────────────────────────────────────────────────────┐
│ Page title                                  [Primary Action]  │
│ Subtitle                                                      │
├───────────────────────────────────────────────────────────────┤
│ [Search........] [Status ▼] [Type ▼]             [Refresh]   │
├───────────────────────────────────────────────────────────────┤
│ Main data table                                               │
├───────────────────────────────────────────────────────────────┤
│ Result count and pagination                                  │
└───────────────────────────────────────────────────────────────┘
```

Rules:

- group search and filters in one toolbar
- keep the primary action in the page header
- use compact row density
- support meaningful sorting
- show friendly display values and status tags
- preserve server-side paging, filtering, and sorting
- the main data table follows the table width, column, and scrolling rules in §12

## 8. Standard detail page

Used for a user, organization, product, plan, subscription, entitlement, or role.

```text
← Back

Entity name                       [Status] [Edit] [More]
Short description

┌───────────────────────────────────────────────────────────────┐
│ Summary                                                       │
│ Key fields arranged in a compact descriptions grid            │
└───────────────────────────────────────────────────────────────┘

[Overview] [Related Records] [Activity]

Selected tab content
```

Rules:

- title, status, and actions appear together
- summary appears before detailed tabs
- important values are visible immediately
- technical identifiers belong in a secondary details area

## 9. Standard form page

Used for creating or editing users, organizations, products, plans, subscriptions, and roles.

```text
Page title
Short explanation

┌───────────────────────────────────────────────────────────────┐
│ Basic Information                                             │
│ Field groups                                                  │
├───────────────────────────────────────────────────────────────┤
│ Additional Settings                                           │
│ Related controls                                              │
├───────────────────────────────────────────────────────────────┤
│                                      [Cancel] [Save Changes]  │
└───────────────────────────────────────────────────────────────┘
```

Rules:

- group fields by purpose
- use two columns only for naturally related short fields
- required labels use a subtle red `*`
- show once: `Fields marked * are required.`
- validation appears near the relevant field
- submit is disabled while invalid or submitting
- no silent submit failures
- busy state always resets with `try/finally` or equivalent

Recommended reusable component: `AdminFormActions`.

## 10. Standard commercial page

Used for Current Subscription, plan selection, Start a Business, billing and renewal, and subscription history.

Organization layout:

```text
┌───────────────────────────────────────────────────────────────┐
│ Current Subscription                                          │
│ Manage your plan, billing, and subscription status.           │
├───────────────────────────────┬───────────────────────────────┤
│ Subscription Summary          │ Subscription Actions          │
│ Product                       │ Primary action                │
│ Current plan                  │ Secondary actions            │
│ Status                        │ Relevant alert               │
│ Billing cycle                 │                               │
│ Current price                 │                               │
│ Trial / renewal date          │                               │
├───────────────────────────────────────────────────────────────┤
│ Available Plans                                               │
│ [Plan] [Current Plan] [Plan]                                  │
├───────────────────────────────────────────────────────────────┤
│ Subscription History                                         │
│ Product | Plan | Billing | Price | Status | Dates | Actions  │
└───────────────────────────────────────────────────────────────┘
```

Personal layout:

```text
Choose a Plan / Start a Business
├── eligibility or current account state
├── available plans
├── Start Trial / Subscribe action
└── recent subscription activity
```

Plan cards must use equal height, subtle border, visible soft shadow in light mode, aligned feature content, bottom-aligned actions, clear current-plan highlighting, and restrained decorative color.

Every visible commercial action must have a real handler, loading state, duplicate-submission protection, validation, visible success/error feedback, and state refresh after success.

## 11. Dashboard and summary pattern

Use compact summary cards for important metrics only.

```text
┌────────────────┐ ┌────────────────┐ ┌────────────────┐
│ Active Orgs    │ │ Trialing       │ │ Past Due       │
│ 24             │ │ 7              │ │ 2              │
└────────────────┘ └────────────────┘ └────────────────┘
```

Use 2 to 4 cards per row, consistent height, concise labels, prominent values, and soft shadow in light mode.

## 12. Tables

Tables must be compact, full width, readable, and professional. All Admin tables follow Ant Design Blazor native table behavior.

### 12.1 Width, columns, and scrolling

- tables use the full available content width
- use Ant Design Blazor native table features
- configure `ScrollX` using the practical combined minimum width of the columns
- fixed-width columns should be used for:
  - status
  - dates
  - numeric values
  - compact actions
- leave at least one important descriptive column flexible so it expands on wide screens
- important content should wrap where appropriate
- do not apply ellipsis to every column
- use ellipsis only for optional long content and expose the full value with a tooltip
- columns may shrink only until their practical minimum width is reached
- once the viewport becomes narrower than the combined minimum widths, preserve column readability and enable horizontal scrolling
- horizontal scrolling is preferred over unreadably narrow columns or excessive `...`
- tables must remain usable on desktop, tablet, and mobile
- avoid transforming normal data tables into unrelated card/list layouts unless specifically designed for that page

Expected behavior:

```text
Wide screen
→ table fills the available width
→ flexible columns expand

Medium screen
→ flexible columns shrink or wrap
→ important values remain readable

Small screen
→ minimum column widths are preserved
→ horizontal scrolling appears
```

### 12.2 Content and density

- use friendly display names instead of GUIDs
- use friendly dates and money
- use consistent status tags
- group actions compactly in a fixed-width actions column
- show explicit empty, loading, and error states

Recommended date format:

```text
03 Aug 2026, 12:44 PM
```

Recommended money format:

```text
PHP 699.00
PHP 699.00 / month
```

## 13. Status tags

Use one shared status mapping.

```text
Active          success / green
Trialing        processing / blue
Grace Period    warning / gold
Past Due        warning / orange
Suspended       error / red
Cancelled       default / gray
Expired         default / gray
Inactive        default / gray
Retired         default / gray
Draft           neutral
Pending         processing
```

Recommended reusable component: `AdminStatusTag`.

## 14. Buttons and actions

Primary actions: Create, Save, Subscribe, Start Trial, Upgrade, Confirm.

Secondary actions: Cancel, View History, Refresh, Export, secondary navigation.

Danger actions: suspend, cancel subscription, retire role, close organization, revoke access.

Rules:

- one dominant primary action per section
- do not show many primary buttons together
- loading buttons display progress
- disabled buttons should have a clear reason when not obvious
- no active-looking button without behavior
- danger actions require confirmation
- page-header and section actions follow the page patterns in §6–§10
- table row actions stay compact in a fixed-width actions column; prefer icon buttons or a short overflow menu over wide multi-button clusters
- filter and refresh controls belong in the list toolbar, not mixed into the table actions column

## 15. Loading, empty, error, and success states

Loading:
- Skeleton for page/card structure
- Spin for short localized operations
- button loading state for mutations

Empty:
- Ant Design Empty
- clear explanation
- optional authorized action

Error:
- Alert for page/section load errors
- inline field validation
- Message/Notification for mutation results
- retry action where appropriate

Success:
- visible success message
- refresh affected state
- navigate only when it improves the flow

## 16. Responsive behavior

Desktop:
- multi-column summary layouts
- full filter toolbars
- 3 or 4 plan/metric cards per row where space allows

Tablet:
- fewer columns
- wrapping controls
- readable card widths

Mobile:
- stack cards vertically
- actions may become full width
- filters may stack or use a drawer
- tables preserve practical minimum column widths and enable horizontal scrolling when needed (§12)
- do not convert normal data tables into ad-hoc card/list layouts on small screens unless the page is specifically designed that way
- maintain at least 16px page padding

## 17. Light and dark themes

### Light mode

Light mode must not appear flat.

Use clear page-background versus card-surface contrast, subtle borders, soft card shadows, readable secondary text, and distinct hover/selected states.

### Dark mode

Use theme tokens, controlled elevation, restrained borders, accessible contrast, no bright white cards, and no excessive glow.

Do not solve light-mode flatness by adding a heavy universal shadow to every element.

## 18. Recommended reusable components

```text
AdminPageHeader
AdminSection
AdminSummaryCard
AdminStatusTag
AdminDataTable
AdminEmptyState
AdminFormActions
AdminMoneyDisplay
AdminDateTimeDisplay
AdminFilterBar
```

Do not build a large custom design framework that duplicates Ant Design.

## 19. Content-writing rules

Prefer:

```text
Current Subscription
Available Plans
Billing & Renewal
Subscription History
Current Price
Updated
Organization Members
Platform Staff
Roles & Permissions
```

Avoid raw status codes, internal entity names, database identifiers as headings, debug messages, and technical UTC wording.

## 20. Authorization-aware UI

The UI reflects permissions but never replaces server authorization.

- hide or disable unavailable actions appropriately
- never expose cross-organization data
- Organization Administration must not expose Platform RBAC
- Platform, Organization, and product-local roles remain separate
- menu visibility is not authorization
- APIs enforce all security boundaries independently

## 21. Accessibility

Use semantic headings, visible form labels, keyboard-accessible actions, visible focus indicators, sufficient contrast, status text in addition to color, accessible icon labels, associated validation messages, and meaningful modal/drawer titles.

## 22. Anti-patterns

Do not implement:

- raw stacked label/value text for major summaries
- flat borderless cards that disappear into the page
- large blank vertical gaps
- narrow centered Admin pages
- random button placement
- duplicate page titles
- inconsistent shadows and radii
- cards nested several levels deep
- raw GUIDs as normal user-facing content
- inconsistent status colors
- unformatted UTC timestamps
- active buttons without handlers
- swallowed exceptions
- permanently stuck busy state
- desktop-only layouts
- placeholder actions that look functional
- multiple unrelated layout systems on one page

## 23. UI review checklist

### Structure

- [ ] Page follows an approved page pattern.
- [ ] Content order is understandable.
- [ ] Current state is immediately visible.
- [ ] Primary action is obvious.
- [ ] Related information is grouped correctly.
- [ ] History/audit is placed logically.

### Visual consistency

- [ ] Page header matches the standard.
- [ ] Section spacing is consistent.
- [ ] Cards use the approved border, radius, and shadow.
- [ ] Light-mode cards stand above the page background.
- [ ] Dark mode remains readable.
- [ ] Status tags are consistent.
- [ ] Buttons follow the correct hierarchy.

### Tables and forms

- [ ] Tables are compact and use the full available content width.
- [ ] `ScrollX` matches the practical combined minimum column width.
- [ ] Status, dates, numbers, and compact actions use fixed-width columns.
- [ ] At least one important descriptive column remains flexible.
- [ ] Ellipsis is limited to optional long content and exposes a tooltip.
- [ ] No raw GUIDs are used as primary content.
- [ ] Dates and money are friendly.
- [ ] Forms group related fields.
- [ ] Required fields and validation are clear.
- [ ] Submit state always resets.

### Responsive behavior

- [ ] Desktop layout is balanced; flexible table columns expand.
- [ ] Tablet wrapping is clean; important values remain readable.
- [ ] Mobile content stacks properly.
- [ ] No overlap or clipping.
- [ ] Narrow viewports preserve table minimum widths and use horizontal scroll instead of unreadably narrow columns.

### Functional behavior

- [ ] Every visible action works.
- [ ] Loading state is visible.
- [ ] Duplicate submission is prevented.
- [ ] Success refreshes state.
- [ ] Errors are visible and useful.
- [ ] Authorization boundaries remain enforced.

## 24. Approved page patterns

Every new page should be classified before implementation:

```text
List page
Detail page
Form page
Commercial page
Dashboard/summary page
Settings page
```

If a page does not fit, document why and reuse the closest existing components and visual rules.

## 25. Implementation guidance for Cursor

Before changing a page:

1. Identify its approved page pattern.
2. Inspect existing shared components.
3. Inspect the current content hierarchy.
4. Fix content grouping before styling.
5. Reuse Ant Design components and theme tokens.
6. Apply the standard card elevation and spacing.
7. Confirm all visible actions are functional.
8. Test desktop, tablet, mobile, light mode, and dark mode.
9. Review the final diff for one-off CSS and duplicated patterns.
10. Update this document only for intentional, reusable design decisions.

Do not treat visual cleanup as an excuse to rewrite domain logic or weaken authorization.

## 26. Initial reference layout

The Organization Current Subscription page is the initial reference for the Commercial page pattern:

```text
Current Subscription
├── Subscription Summary
├── Subscription Actions
├── Available Plans
└── Subscription History
```

Once implemented and approved, its reusable layout and styling should guide other commercial screens, including Personal Start a Business and plan selection.
