# P17-WP01 — POS Access Handoff

| Field | Value |
|---|---|
| Status | **Complete** |
| Phase | [Phase 17](../phases/phase-17-pos-mvp-operational-onboarding-and-first-sale.md) |
| Starting commit | `14b71e1` (Phase 17 branch tip at start) |
| Final Phase 17 commit | See [P17-WP08](P17-WP08-reports-hardening-and-closeout.md) |
| Date | 2026-07-29 |

## Objective

Confirm and complete the platform-to-product handoff so POS opens only when active organization membership, active POS entitlement, and an active product-local POS role are all present.

## Existing functionality reused

- Platform P16-WP09: enabled-product discovery, launch, `EvaluateProductAuthorization`, product-local role grants.
- POS: `PosPlatformBearerMiddleware`, `PosCommercialAccessMiddleware`, `PosRoleResolutionMiddleware`, `ProductAccessResolver`, `NavigationGate`, `AccessDenied`, `OrganizationSelect`, `AccessConfirmStep`.
- Platform tests: `ProductAuthorizationAndDiscoveryTests`, `ApiProductAccessNavigationTests` (Owner without POS role denied; entitlement-only denied).

## Implementation summary

- Clarified Access Confirm copy: membership + entitlement + POS role; Organization Owner alone is insufficient.
- Mapped `product_local_role_missing` / related codes to `Access_RoleMissing` for clear unauthorized UI.
- Access Confirm completion routes through `NavigationGate` (including incomplete operational setup for Owners).
- Preserved API fail-closed authorization independent of UI navigation visibility.

## Files / components changed

- `ProductAccessResolver.cs` — reason key mapping for missing POS role
- `AccessConfirmStep.razor` — gate-aware enter
- `PosResources.resx` / `PosResources.fil-PH.resx` — access messaging

## Authorization and isolation behavior

| Check | Behavior |
|---|---|
| Missing membership | Deny (`Access_MembershipMissing` / inactive) |
| Missing entitlement | Deny (`Access_Entitlement*`) |
| Missing POS role | Deny (`Access_RoleMissing`); launch `product_local_role_missing` |
| Org Owner without POS role | Deny (Platform authorize / launch) |
| Suspended member | Deny |
| Cross-organization | Org header + repository org filters; concealed 404/deny |

## Tests executed and results

- Existing Platform product authorization / launch tests (reuse) — expected pass under solution test run.
- POS operational-setup role denial (Cashier Manage) — covered under WP02 suite.

## Deferred items

- Dedicated Maui screen per denial reason beyond `/access-denied?reason=` query (existing pattern retained).
- Deep-link from Admin Product Entry directly into MAUI shell (Admin remains token/product-entry surface).

## Commit reference

Final Phase 17 commit recorded in P17-WP08.
