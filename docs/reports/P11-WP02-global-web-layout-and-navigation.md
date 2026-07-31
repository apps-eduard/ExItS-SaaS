# P11-WP02 — Global Web Layout and Navigation

Package: **P11-WP02 — Global Web Layout and Navigation**  
Prior tip (P11-WP01): `221fe69ab179956e8a73411cf3eb58fd6f199c3c`  
Feature tip (this WP): `7ce7df139a9494c9aab7d189900e96d5e43fdc1d`  
Docs tip: `2db60f5e65556259d7ab724c84568bfb78a69de5`

## Status

**Complete.** Sidebar navigation now updates URL **and** page content. One authoritative Admin shell (`MainLayout`). Theme persistence preserved without document-wide permanence attributes.

## Defect reproduction (before)

Host: `http://127.0.0.1:5289/admin` (Playwright).

| Observation | Value |
|---|---|
| Before click | `/admin`, h1 = Portfolio dashboard |
| After Products click | URL `/admin/products`, active nav `/admin/products` |
| Content | **Still** Portfolio dashboard (stale) |
| `html[data-permanent]` | **true** |
| Permanent nodes | only `<HTML>` |

## Exact root cause

`<html data-permanent>` was added during Pre-P11 theme persistence reopen so enhanced navigation would keep `data-theme`.

Blazor enhanced navigation **does not synchronize elements marked `data-permanent` or their subtrees**. Marking `<html>` therefore froze the **entire document** (including `#main-content` / `@Body`) while History still updated the URL and InteractiveServer `NavLink` still updated the active class.

Theme reapply via `Blazor.addEventListener('enhancedload')` + `LocationChanged` + `pageshow` was already sufficient; document-wide permanence was not required and was harmful.

## Fix

1. Removed `data-permanent` from `App.razor` `<html>`.
2. Left theme authority unchanged: `exits-admin-theme` / `exitsAdminTheme` / html+body apply / `Blazor.enhancedload` / `LocationChanged` / `pageshow`.
3. Architecture guards now **forbid** any `data-permanent` under Admin Components and require sole shell = `MainLayout`.

## Shell / navigation changes

- **One shell:** deleted unused duplicate `AppShell.razor`; `Routes` → `MainLayout` only.
- Shell landmarks: skip link, `aside` sidebar, `header` top bar, `main#main-content`, footer, drawer + collapse toggles.
- Top-bar context chip for available actor display name (dev operator); EnvironmentBanner retained.
- Brand text is a navigable link; long Filipino labels still truncate via existing nav CSS.
- **PageHeader:** sets `PageTitle`, breadcrumb `<nav aria-label>`, optional Status/Actions slots.
- **PageFrame:** Standard / Wide / Form content-width conventions (applied on Dashboard + Products as proof).
- Locked baseline preserved: sidebar `16rem` / `4.25rem`, IBM Plex Sans stack, tokens, reduced-motion, drawer close-on-nav.

## Playwright route matrix (after)

All passed (`artifacts/p11-wp02-nav-matrix.mjs`):

- Sidebar: Dashboard, Products, Organizations, Subscriptions, Payments, Users, Entitlements, Audit — URL, h1, active link, previous content replaced, document title present, no Hello world, no `data-permanent`.
- Direct `/admin/products` + refresh.
- Back / Forward between Products and Organizations.
- Dark theme through Products → Subscriptions → Audit → Dashboard with content changes + theme retained; Dark refresh OK.
- Light nav + refresh OK; System stored as `system`.
- Mobile viewport: drawer opens, closes after Products navigation, content updates.

## Tests

- Full `ExItS.slnx` Release: **1161 passed / 0 failed / 0 skipped** (baseline 1160 + 1 shell/`data-permanent` guard)
- Admin unit tests: **41 passed**

## Remaining UI debt

- Broad PageFrame adoption on remaining Admin pages (later WPs)
- ToastHost vs inline `_toast` inconsistency (P11-WP03+)
- Admin ↔ DesignSystem consolidation still deferred
- Formal a11y certification not claimed
- R-091 production auth remains open

## Explicit exclusions

No business-rule changes; P11-WP03 not started; Phase 12 / Product-Foundation untouched.

## Exact next

**P11-WP03 — Shared Forms, Validation, and Dialogs** when explicitly authorized.
