# Platform Admin Web — Application Shell & Global Interactions

**Status:** Documentation Only — implementation not authorized  
**Source:** PLATFORM-WEB-DOC-05  
**Branch:** `docs/platform-admin-web-v2`

---

## 0. Evidence alignment (existing Admin shell + routes)

This DOC specifies shell behavior to match the approved Platform Admin Web IA (DOC-02) while reflecting the current Platform Admin evidence:

- Navigation: the current Admin uses a sidebar menu (`AdminNav`) with top-level groups and a collapsible sidebar (desktop) that becomes drawer-style navigation for narrow layouts.
- Shell chrome: the current Admin uses a persistent header with:
  - navigation/menu controls for mobile
  - Theme and Language selectors
  - an organization-context switcher
  - an account/user dropdown menu
- Routing & authorization: the current Admin uses route-level authorization gating (`AuthorizeRouteView` + NotAuthorized handling) and a dedicated `NotFound` route for unknown paths.
- Breadcrumb concept: DOC-02 mandates breadcrumbs under the top-level navigation and that navigation visibility is not authorization.

This DOC defines the future app shell behavior without requiring any specific React/route implementation details.

## 0.1 Shell surface responsibility boundaries

Each shell surface has a distinct responsibility. Avoid duplicating the same information or control across multiple surfaces.

| Surface | Responsibility | Question it answers |
|---|---|---|
| **Sidebar** | Global navigation structure | "Where can I go?" |
| **Top bar** | Global context, tools, and preferences | "What global context/tool/preference do I need?" |
| **Breadcrumb** | Current location in the navigation hierarchy | "Where am I?" |
| **Page header** | Entity/page identity and available actions | "What am I looking at and what can I do?" |
| **Workspace navigation** | Entity-local section selection | "Which part of this entity am I managing?" |

The canonical navigation structure is defined in `navigation-registry.md`. This document specifies shell behavior; it does not duplicate the full registry.

---

## 1. Application Shell (behavior spec)

### 1.1 Persistent primary sidebar

The primary sidebar is always present in the Platform shell. Its sole responsibility is answering "Where can I go?" — the canonical navigation structure is defined in `navigation-registry.md`.

- **Purpose:** provide global access to top-level navigation groups.
- **State:** the sidebar supports expand/collapse on desktop.
- **Icon-only collapsed mode:** when collapsed, show icons with accessible tooltips for each section/item.
- **Selected state indication:**
  - active group is visually indicated in the sidebar
  - active page highlights the specific leaf item where applicable
- **Hover / focus states:** clear visual feedback for hover and keyboard focus on all items.
- **Permission-aware visibility (`UNAUTHORIZED`):** items the user cannot access are hidden (not shown disabled). Server-side authorization remains authoritative.
- **Planned disabled state (`PLANNED_DISABLED`):** items that exist in an approved roadmap but are not yet implemented are visible but disabled with a small "Planned" badge and tooltip: "Planned — not available yet." No route to fake/empty implementation.
- **Context required state (`CONTEXT_REQUIRED`):** items requiring a selected organization/entity context are visible but disabled until context is selected, with tooltip explaining what must be selected.
- **Development-only visibility (`DEV_TEST_ONLY`):** items available only in Development/Testing are completely absent from Production navigation.
- **Loading fallback:**
  - while permission facts load, the sidebar must not show unauthorized items
  - show a loading placeholder (spin/skeleton) instead of briefly flashing hidden items
- **Keyboard interaction:** all sidebar items reachable via keyboard. Arrow keys navigate within sections. Enter/Space activates.
- **EN / fil-PH resilience:** sidebar labels must not clip or overflow with longer Filipino translations. Test with representative fil-PH strings.
- **Light/dark behavior:** sidebar follows the active theme using ExItS design tokens. No arbitrary one-off menu styling.
- **No clipping:** labels, icons, and badges must remain fully visible in both expanded and collapsed modes at all supported viewports.

### 1.2 Collapse/expand behavior

- **Desktop:** sidebar starts collapsed by default on first load only if the user’s last preference is stored; otherwise use the default for the product design.
- **User control:** expand/collapse persists for the user/session (implementation decided later).
- **Mobile/tablet:** sidebar becomes a navigation drawer (see §9).

### 1.3 Current section indication

In addition to the selected leaf item highlight, the shell exposes a “current section” indication in the header/title area:

- The page title aligns with the active navigation group.
- If the active route is an organization/product detail page, the section indication reflects the top-level group (not just the tab).

### 1.4 Top bar

The top bar is persistent. Its responsibility is "What global context/tool/preference do I need?" It contains:

1. **Navigation control**
   - on tablet/narrow, show a hamburger/menu button that opens the navigation drawer
2. **Global search entry**
   - only when global entity search capability is genuinely usable (`PWEB-CAP-*` backed)
   - if server-side entity search is not yet available, either disable with "Planned" state or provide only the subset genuinely backed
   - do not create fake search results
   - command palette may still support safe local navigation even if global server entity search is unavailable
3. **Command palette entry**
   - either an explicit button or a visible hint for keyboard access (e.g., "Ctrl+K")
4. **Organization/entity context switcher**
   - shows the current organization context when applicable to the route
5. **Environment indicator**
   - Production: restrained neutral indicator (may be absent or subtle)
   - Development/Testing/Staging: visually distinctive — an administrator must not easily confuse non-production with Production
6. **Language selector**
   - English (`en`) / Filipino (`fil-PH`)
7. **Theme selector**
   - System / Light / Dark
8. **User/account menu**
   - sign out
   - account settings, preferences (language, theme, density)
   - settings links that depend on selected organization context (disabled/hidden if not available)
9. **Notifications**
   - only when a real approved capability exists; do not invent notification infrastructure

Breadcrumbs sit directly under the top bar (see §1.6).

**Responsive collapse/overflow:** top bar elements compress gracefully. Search and command palette remain accessible via keyboard on narrow screens. Non-essential elements may move to overflow menus.

### 1.5 User/account menu

Account menu behavior:

- The menu must never execute destructive actions directly.
- Selecting an account action must either:
  - navigate to a safe settings page, or
  - open a confirmation dialog for destructive actions.
- Keyboard accessibility:
  - menu is fully operable via keyboard
  - focus returns to the triggering element after menu close

### 1.6 Breadcrumb area

Breadcrumbs appear:

- directly under the header (between top bar and main content)
- always visible for pages below the top-level navigation
- clickable to ancestor levels

Breadcrumb behavior:

- breadcrumbs reflect the URL state
- changing breadcrumbs updates the page context and restores the correct tab/list state
- breadcrumbs must not provide authorization; direct URL navigation remains subject to server authorization

### 1.7 Page title / action area

Each page has a consistent title/action block:

- page title (h1)
- optional subtitle/description
- primary action (highest-impact allowed action)
- secondary actions (e.g., create/edit/export) presented as buttons or dropdowns

Responsive behavior:
- actions wrap below the title on narrow widths
- on narrow screens, avoid horizontal overflow by stacking actions

### 1.8 Main content region

The main content region:

- is scrollable independently from the sidebar where possible
- preserves layout stability during loading (no “jumping” skeleton widths)
- supports “content focus” for keyboard navigation (first focusable element after load)

### 1.9 Responsive navigation rules

Responsive shell:

- **Large desktop:** sidebar expanded/collapsed, persistent; content area uses max width.
- **Laptop:** sidebar can be collapsed; content remains stable; avoid full-page reflows.
- **Tablet:** sidebar becomes a drawer/sheet.
  - when drawer opens, focus moves into the drawer
  - Escape closes the drawer when safe (see keyboard model)
- **Narrow:** the top bar compresses; search entry and command palette remain accessible via keyboard.

Hard rule:
- avoid horizontal page overflow as the default interaction
- tables may scroll horizontally inside their container when necessary (controlled overflow)

---

## 2. Global Search vs Command Palette

### 2.1 Global search (entity discovery)

Global search is for **finding supported Platform entities**:

- organizations
- users (Platform users and/or account profiles depending on API scope)
- products and plan/catalog entries (as permitted by Platform Admin scope)
- subscriptions (where supported)

Global search behavior:

- input is available from the header
- results are shown as suggestions/preview cards
- selecting a result navigates to the entity detail page with the correct breadcrumbs and context

Security rules:

- do **not** assume a backend global-search endpoint exists
- therefore, global search is a **capability requirement** (see §8 / DOC-09)
- search results must never expose unauthorized entities
- if server-side search is not available yet, the global search UI must degrade safely:
  - show “Search not available” (or equivalent), and
  - keep the user on the current safe page

Keyboard/interaction safety:
- searching must not execute any destructive action
- selecting a result is navigation only (no mutation)

### 2.2 Command palette (safe navigation and commands)

The command palette is for **triggering safe permitted UI commands** and navigation:

- navigate to known pages/routes (e.g., Organizations list, Audit logs, Platform roles)
- open non-destructive UI dialogs (e.g., help/about/shortcuts list)
- focus global search entry
- open settings panels

Explicit restrictions:

- command palette must never:
  - execute a destructive action without a confirmation dialog
  - bypass required multi-step confirmation
- accidental keystrokes should not cause mutations

Result behavior:
- the palette supports filtering commands
- selection closes the palette and restores focus predictably

---

## 3. Entity Context Model

Platform Admin is globally scoped administration. The shell must still support multiple contexts, but authorization must remain server-side.

### 3.1 Organization context

Organization context is established by:

- the organization-context switcher in the header (when applicable)
- entity detail navigation into an organization

Organization context rules:

- switching organization must reload or invalidate organization-scoped state
- navigating away from an organization detail must not leak organization-specific context into unrelated pages
- the client must never trust a client-supplied `OrganizationId` as an authorization signal
  - it is a navigation identifier only; server must validate permission and membership

### 3.2 Organization detail context

Within an organization detail workspace:

- tab content remains consistent and does not mix unrelated organization data
- actions in the organization detail require confirmation when destructive
- tab selection is URL-driven for deep links (as described in DOC-02)

### 3.3 Product context

Product context is established by product detail navigation (catalog/product/plans) and is local to that workspace.

Product context rules:

- product-local actions require server permission checks
- product context must not grant access to product operational workflows (POS/PLM)

### 3.4 User context

User context is established by platform staff/user detail navigation.

User context rules:

- user detail pages are subject to server authorization
- the UI must show a forbidden state if access is denied, not a generic error

### 3.5 Commercial context

Commercial context covers subscription/entitlement/billing views and remains within Platform scope.

Commercial context rules:

- commercial state is read-only unless the user has explicit authorization to mutate
- when the user triggers a plan/subscription action, show a confirmation dialog and then a success toast on completion

---

## 4. Canonical Page Templates

All templates share these required status surfaces:

- loading
- empty/zero-result
- error
- forbidden
- success feedback (toasts or inline confirmation)

### A. Overview / Dashboard template

Header:
- page title: “Overview” / “Dashboard”
- secondary action: refresh/reload (if supported)

Primary action:
- none by default; if present, it must be the safest allowed admin action in that environment.

Filters:
- optional small filter row (e.g., time window), URL-driven where feasible.

Content:
- summary/stat cards
- recent activity/audit highlights
- actionable “needs attention” indicators (permission-aware)

Status surfaces:
- loading: skeleton cards + skeleton table preview
- error: full-page error with retry
- forbidden: forbidden state
- empty: “No activity yet” (or equivalent)

Responsive:
- stacks cards into one column
- avoids dense tables unless space allows

### B. Collection/List page template

Header:
- page title
- primary action: “Create / Add / Invite / Upload” when permitted
- secondary actions: bulk export (if permitted), filters reset

Filters:
- search + filter controls in a toolbar
- filter state is URL-driven where feasible (for deep links)

Content:
- table (desktop) / card list (mobile) with pagination
- row actions are permission-aware

Status surfaces:
- loading: table skeleton + filter skeleton
- empty: empty state with optional action
- zero-result: zero-result state that reflects query terms
- forbidden: forbidden state

Responsive:
- table becomes card layout below tablet breakpoint (aligns with existing table strategy)
- if horizontal overflow is necessary, keep it inside the table container

### C. Entity detail / Workspace template

Header:
- breadcrumb path
- page title: entity name
- primary action: “Edit” (if permitted)
- secondary actions: “View history” / “Export” / “More”

Content:
- detail summary area (key/value metadata)
- tabs aligned with DOC-02 drill-down architecture (only Platform-owned tabs)
- detail actions open in a side drawer/sheet when possible to preserve context

Status surfaces:
- loading: skeleton for summary + skeleton tabs
- error: inline error for the tab plus a retry control
- forbidden: forbidden state
- empty: detail not found or no related records (within the tab)

Responsive:
- on narrow screens, switch tabs to stacked layout
- side drawer becomes full-width sheet

### D. Settings page template

Header:
- section title: “Settings”
- primary action: “Save changes” if editing is enabled
- secondary actions: “Reset to defaults” (when permitted)

Content:
- sectioned forms (readable)
- show current configuration summary before editable controls

Status surfaces:
- loading: form skeleton
- error: inline error with field mapping
- forbidden: forbidden state

Responsive:
- single-column forms; avoid wide tables

### E. Wizard / Multi-step flow template

Header:
- page title: wizard name
- progress/step indicator

Primary action:
- Next / Finish

Secondary actions:
- Back
- Cancel (requires confirm when unsaved changes exist)

Content:
- step content with one logical task per step
- inline validation

Unsaved-change rules:
- attempting to close/route-away mid-step:
  - if there are unsaved changes → show confirmation dialog
  - if no changes → allow navigation

Status surfaces:
- loading: step skeleton if server preloading is required
- error: show step-level failure with retry

Responsive:
- steps stack; keep primary action visible

### F. Audit/Event timeline template

Header:
- page title: “Audit Logs” / “Activity”
- primary action: filter/export (when permitted)

Filters:
- time range
- action type
- entity scoping (organization/product) when context is active

Content:
- chronological events
- expand/collapse details

Status surfaces:
- loading: skeleton timeline
- empty: “No events for the selected filters”
- error/forbidden: as above

Responsive:
- timeline wraps content to readable widths; avoid long horizontal lines

### G. Data-heavy management page template

Header:
- page title
- primary action: “Export” or “Run reconciliation” only if non-destructive and permitted

Filters:
- advanced filters row
- server paging and sorting (contract later)

Content:
- dense table with keyboard-friendly row navigation
- selection/bulk actions always require explicit confirmation for destructive operations

Status surfaces:
- loading: table skeleton rows
- error: full-page error if table cannot load; otherwise inline error

Responsive:
- mobile uses card layout and hides non-critical columns
- desktop retains density

---

## 5. Global UX Behavior (cross-page rules)

### 5.1 Breadcrumbs & URL-driven state

- breadcrumbs always represent URL navigation path
- list filters, selected entity, and selected tab should be URL-driven so browser back/forward works reliably

### 5.2 Browser back/forward compatibility

- back/forward navigation restores:
  - list filter state
  - detail view selected tab
  - wizard step when safe
- in read-only states, no data loss should occur

### 5.3 Deep links and deep context

- direct links to entity pages restore the correct shell context
- opening a deep link into an organization detail establishes the organization context for that page only

### 5.4 Session expiry

When session expires:

- API calls return an auth/session invalid problem payload
- the shell shows a session-expired notice (toast/banner)
- the user is redirected to login and cached server state is cleared

### 5.5 Unauthorized / Forbidden

- unauthorized access must show a clear forbidden/unauthorized page
- the shell must never silently hide required context while still allowing access to deep-linked routes

### 5.6 Not-found (route-not-found)

- unknown routes show the NotFound page with a clear call-to-action back to the dashboard
- not-found is distinct from forbidden/unauthorized

### 5.7 Transient network failure & retry behavior

- if the network fails:
  - show inline “Reconnecting / Retry” controls for the affected section
  - preserve user input when safe
- retries must be safe:
  - idempotent GET retry is always safe
  - non-idempotent mutations must require user confirmation (no silent retry on mutation)

### 5.8 Stale-data indication

- when cached server state becomes stale:
  - show a subtle “may be outdated” indicator where appropriate
  - allow user-initiated refresh

### 5.9 Refresh behavior

- refresh should revalidate page data with server
- avoid losing URL-driven filter state during refresh

### 5.10 Success feedback

- non-destructive actions: show inline confirmation or toast
- actions that change entity state should show a toast with a success label

### 5.11 Destructive confirmation

Destructive operations must:

- require explicit confirmation via modal/dialog
- protect against accidental keystroke execution
- require “confirm” intent (at minimum a button click; high-impact may require typing)
- default focus must be on “Cancel”, not “Confirm”

---

## 6. Keyboard Model

### 6.1 Predictable Tab order

- Tab order follows visual layout and avoids jumping focus into hidden side drawers until opened
- newly opened dialogs receive focus immediately

### 6.2 Escape behavior

Escape closes:

- drawers/sheets when there are no unsaved changes in the underlying context
- command palette and global search overlays when open

Escape does not bypass:

- required confirmations
- unsaved-change confirmations

### 6.3 Command palette shortcut

- Shortcut: `Ctrl+K` (and `Cmd+K` on macOS)
- opening the palette focuses the command input/list

### 6.4 Global search focus shortcut

- Shortcut: `Alt+/`
- opening focuses the global search input

### 6.5 No shortcuts that bypass confirmation

- no single-key action triggers destructive mutations
- command palette shortcuts must follow the same rules as direct UI actions (confirmation required when needed)

### 6.6 Discoverable shortcut documentation

- the command palette includes a “Shortcuts” help command

### 6.7 Focus restoration

When overlays close (palette/search/drawer):

- focus returns to the element that triggered the overlay
- in navigation-triggering actions, focus moves to the first meaningful heading (`h1`)

---

## 7. Responsive Shell (tablet/narrow behavior)

Rules:

- below tablet breakpoint:
  - sidebar → drawer/sheet
  - top bar elements stack/wrap while remaining accessible
- data-heavy tables:
  - controlled horizontal scrolling only inside table container
  - responsive card layout below tablet breakpoint
- avoid full-page reflows that break keyboard focus

---

## 8. Capability requirements (DOC-09 dependency)

Global search capability:

- must provide server-side search for supported entities
- must apply server authorization to search results
- must not leak existence of unauthorized entities

If the contract is not available yet, the UI must degrade safely (no unauthorized disclosure and no broken search UX).

