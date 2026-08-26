# PLATFORM-WEB-PA-COM-08 — Subscription / Billing React UI Parity

## Summary

Completed Blazor→React commercial UI parity for portfolio and remaining catalog gaps, reusing existing Platform APIs and org-scoped commercial operators.

| Item | Value |
|---|---|
| Branch | `feat/platform-admin-pa-com-07` |
| Starting HEAD | `e59e6dbd11f315fd9ca819bd7d3fc18f8c22eacf` |

## Delivered

| Surface | Status |
|---|---|
| SaaS Products | Already complete (lifecycle) |
| Plans + Create Plan | Create Plan dialog added |
| Plan limits / features / versions | Already ahead of Blazor |
| Org subscription + plan change | Already complete; enriched summary fields |
| Suspend / Reactivate / Cancel | Already complete |
| Convert trial | Wired to existing `convert-trial` API |
| Org billing | Already complete |
| Org entitlements | Already complete |
| Global subscriptions list/detail | **New** |
| Global payments list/detail | **New** |
| Global entitlements latest | **New** |

## Backend

- Existing Platform APIs reused only
- No business-logic changes
- `BACKEND_API_GAP=` (none for delivered parity)

## Explicit exclusions

- Agent 3 Global Catalog
- Agent 4 System Health / Audit / Privacy
- POS React
- Payment provider / tax / BIR
- Merge to main
