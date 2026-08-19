# PLATFORM-WEB-DOC-05 — Application Shell and Global Interactions

**Status:** Complete  
**Branch:** `docs/platform-admin-web-v2`

## Delivered capability

This package defines the future Platform Admin Web application shell behavior and its global interaction model (planning-only, documentation-only).

It specifies:

- persistent primary sidebar behavior (expand/collapse, selected section indication, permission-aware visibility)
- persistent top bar content (context switcher, environment indicator, account menu, search entry, command palette entry)
- breadcrumb area placement and interaction expectations
- main content region responsibilities and focus management expectations
- responsive navigation behavior for laptop/tablet/narrow breakpoints
- global search vs command palette distinction and security constraints
- entity context interaction rules (organization, product, user, commercial contexts) with explicit “do not trust client-supplied IDs for auth”
- canonical page templates (overview/dashboard, collection/list, entity detail/workspace, settings, wizard, audit timeline, data-heavy management)
- global UX behavior for deep links, browser back/forward compatibility, unsaved-change handling, session expiry, unauthorized/forbidden, not-found, network failure/retry, stale-data indication, and destructive confirmation
- keyboard model requirements (Tab order, Escape behavior, shortcuts, focus restoration, no bypass-confirmation shortcuts)

## Evidence alignment

The shell spec is aligned with existing Platform Admin evidence:

- current sidebar navigation comes from `AdminNav`
- header shell chrome comes from `MainLayout` (theme/language/org context/account menu)
- route-level auth gating comes from `Routes.razor` (`AuthorizeRouteView` + NotAuthorized handling)
- not-found is handled via `NotFound` page

## Exclusions

- No implementation, no route code, no React components
- No CSS or package changes
- No backend changes
- No DesignSystem code edits

