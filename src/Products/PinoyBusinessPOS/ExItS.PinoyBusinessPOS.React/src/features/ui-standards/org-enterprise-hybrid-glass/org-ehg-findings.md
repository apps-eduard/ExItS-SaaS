# Phase 0 findings — Personal vs Organization CSS boundaries

**Date:** 2026-10-24  
**Scope:** Design showcase only (no production Organization page migration)

## Uncommitted work preserved

SignalR / scheduled-order / realtime files remain untouched by this showcase. Accidental untracked React `.js` siblings and `App_Data` are not deleted.

## Existing UI Standards

| Item | Location |
|---|---|
| Route | `/ui-standards` behind `import.meta.env.DEV` + local-validation gate |
| Nav catalog | `ui-standard-catalog.ts` → `UI_STANDARD_NAV` |
| Showcase CSS | `ui-standards.css` imported **only** by `UiStandardsPage.tsx` |
| Guard | `ui-standards-dev-only.test.ts` |

## Leakage risks identified

| Risk | Detail | Mitigation for Org Hybrid Glass |
|---|---|---|
| Unscoped rules in `globals.css` | Shared tokens (`--exits-*`) and some Personal Soft Glass under `.personal-*` | Org showcase uses **only** `.org-ehg` / `[data-org-ehg]` prefixed selectors; no new Personal rules |
| `html:has(.personal-shell)` | Personal shell bumps root layout | Org samples never mount `.personal-shell` |
| Shared `Button` / `ExitsModal` defaults | Intent locks are global | Showcase reuses components; presentation wrappers use `.org-ehg` surfaces only — no Button default rewrite |
| Soft Glass tokens in globals | `--soft-glass-*`, `.personal-qa-glass*` | Not referenced by Org Hybrid Glass CSS |
| Importing showcase CSS into product | Would ship in production | Org CSS imported only from `UiStandardsPage` (DEV tree-shaken) |

## Product styling contexts (locked for this task)

| Product | Visual system | Mount boundary |
|---|---|---|
| Personal | Soft Glass (existing) | `.personal-shell`, `.personal-*`, Soft Glass locks |
| Organization (prototype) | Enterprise Hybrid Glass | `.org-ehg` root inside UI Standards only |

Organization Enterprise Hybrid Glass is **not** a global appearance preference and must not become Personal defaults.
