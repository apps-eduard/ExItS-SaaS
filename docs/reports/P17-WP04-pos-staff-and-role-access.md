# P17-WP04 — POS Staff and Role Access

| Field | Value |
|---|---|
| Status | **Complete** (reconciled + access messaging) |
| Phase | [Phase 17](../phases/phase-17-pos-mvp-operational-onboarding-and-first-sale.md) |
| Final Phase 17 commit | See [P17-WP08](P17-WP08-reports-hardening-and-closeout.md) |
| Date | 2026-07-29 |

## Objective

Complete POS product-local role management for Owner, Manager (StoreManager), and Cashier without mixing Organization roles.

## Existing functionality reused

- POS `AssignPosRole` / `RevokePosRole`, `PosRoleAssignment` persistence, Permission endpoints.
- Platform product-local role grants (P16-WP09) with membership checks on Platform Admin assign/launch.
- Role-based Maui navigation via capability evaluator; API `PosRoleAuth`.
- Immediate denial after revoke/suspension/entitlement loss via bearer + commercial + role middleware.

## Implementation summary

- Confirmed MVP mapping: Owner → Owner/Admin; Manager → StoreManager; Cashier → Cashier.
- Hierarchy: POS Owner ⊇ Manager ⊇ Cashier (capability matrix). Start Selling is interface mode only (MAUI UX deferred; API preserves role).
- Creating Organization Staff does not auto-grant POS roles.
- **Post-validation:** Start a Business grants first POS Owner when entitlement activates; MVP enforces a single Organization Owner (no second Owner invite/promote).
- Access messaging updated so missing POS role is explicit (WP01).
- No custom roles introduced.

## Deferred items

- POS-native `AssignPosRole` live Platform membership callback in Testing (Platform already enforces membership for product-local grants used for launch).
- Viewer/InventoryStaff remain available for Full POS but are not Phase 17 MVP labels.
- Mobile Organization Owner essentials screens and Start Selling mode UX (client-experience-boundaries); backend role model aligned.

## Commit reference

Final Phase 17 feature commit `0f00afe`; post-validation alignment commit recorded with this docs refresh.
