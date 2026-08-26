# PWEB-IMPL-14B — Entitlements compact grants disclosure

**Status:** COMPLETE  
**Branch:** `feat/platform-admin-web-v2`  
**Starting HEAD:** `262cfdce2813769935caf3e235e7a75b00a09599`  
**Commit:** `ab0e4da9d13fb2d441984f9b31e0a470df5e5c37`  
**Message:** `fix(platform-web): compact entitlement grant disclosure`

## Problem

Always-expanded grant lists inside a narrow Grants cell made entitlement snapshot rows excessively tall and hard to scan.

## Delivered

- Compact grant summary with enabled/disabled counts from returned grants
- Collapsed default; Show grants / Hide grants progressive disclosure
- Full-width expanded detail (featureCode, Enabled/Disabled, numericLimit when present)
- Mobile snapshot cards with the same disclosure
- Accessibility: button semantics, `aria-expanded`
- No URL or persistence of expansion state
- Zero grants: “No grants”; no disclosure button
- No backend, API, DB, Blazor, POS, PLM, or mutation changes

## Evidence

Screenshots: `docs/Platform-Admin-Web/Reports/impl-14b-entitlements-compact-grants/`

This report records committed Git evidence only. Validation counts from the original package execution were not stored in a canonical report at commit time and are **not invented here**.

## Visual approval

**AWAITING PRODUCT OWNER + CHATGPT**
