# Pre-Phase-11 — Admin UI Theme, Visual Polish, and Motion Gap Fix

Package: **Pre-Phase-11 Admin UI Theme, Visual Polish, and Motion Gap Fix**  
Prior tip: `fed2cd935f9812ab70506e535d90691d776b799e`  
Phase marker: unchanged (`P10-WP08-phase-10-closeout`) — Phase 11 not started.

## Status

**Complete.** Shared Admin theme persistence, light-mode contrast, shell depth, and restrained motion are fixed at the foundation layer.

## Root causes (confirmed)

1. **Dark mode lost after sidebar navigation**  
   - Blazor enhanced navigation can replace document attributes from SSR HTML without re-running `theme-boot.js`.  
   - `ThemeService.InitializeAsync` read `localStorage` into `Current` but **did not call `applyTheme`**, so `data-theme` was not restored after remount.  
   - Storage used PascalCase (`Light`/`Dark`/`System`) while CSS attributes use lowercase — fragile and inconsistent with the authoritative model.

2. **Light mode washed out**  
   - Background `#eef4f3`, muted `#526a70`, and border `#cddbda` produced low separation between canvas, surfaces, and text.

3. **Shell visual depth / motion**  
   - Sidebar/header/nav lacked restrained depth cues and interaction feedback; drawer/nav transitions were minimal.

## Fixes delivered

- Persist authoritative lowercase `system` | `light` | `dark` (legacy PascalCase still parsed)
- Re-apply theme in `InitializeAsync` and on `enhancedload` / `pageshow`
- Stronger light (and refined dark) design tokens
- Sidebar depth, active/hover nav feedback, header/control polish, button/card interaction
- Restrained motion with existing `prefers-reduced-motion` guard
- Unit + architecture coverage for storage/parse/boot wiring

## Explicit exclusions

No Phase 11 work; no new business capability; no report redesign; Phase 11/12/Product-Foundation docs untouched.

## Tests

- Admin unit tests: **40 passed / 0 failed / 0 skipped**
- Full `ExItS.slnx` Release: **1160 passed / 0 failed / 0 skipped** (baseline 1148 + ThemeService/parse coverage)

## Runtime validation

- Admin: `http://127.0.0.1:5289/admin`  
- Confirm Light contrast, Dark persists across nav, System follows OS, reduced motion honored

## Exact next

**Phase 11 — Web UI and Reporting Design System / P11-WP01** when explicitly authorized. Do not begin until approved.
