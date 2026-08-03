# P17-WP02 — Initial POS Setup

| Field | Value |
|---|---|
| Status | **Complete** |
| Phase | [Phase 17](../phases/phase-17-pos-mvp-operational-onboarding-and-first-sale.md) |
| Final Phase 17 commit | See [P17-WP08](P17-WP08-reports-hardening-and-closeout.md) |
| Date | 2026-07-29 |

## Objective

First-time POS operational onboarding for POS Owner: store display name, PHP currency, tax inclusive/exclusive mode, receipt information, one default register, setup completion + resume, Cashier cannot change setup.

## Existing functionality reused

- Register create/list (`CreateRegister`, `IRegisterRepository`, register code sequences).
- Capability + role middleware patterns from registers/permissions.
- Client onboarding (language/theme) remains separate from store setup.

## Implementation summary

- New `pos.operational_setups` aggregate (one row per organization).
- `GET/POST complete/PUT` operational-setup API.
- Completing setup provisions **Main Register** when none exist and sets `DefaultRegisterId`.
- Tax mode + rate stored; applied at checkout when rate &gt; 0.
- Maui `/setup` page; Owners with incomplete setup redirected from `NavigationGate`.
- Cashier denied `ManageOperationalSetup` (role matrix + API 403).

## Files / components changed

- Domain/Application/Infrastructure OperationalSetup vertical slice
- Migration `20260803180624_AddPosOperationalSetup`
- `Sale.TaxAmount` + checkout tax calculation
- Maui `OperationalSetupPage.razor`, Settings/Deferred links, NavigationGate
- Integration: `PosOperationalSetupApiTests`; unit: `OperationalSetupTaxCalculatorTests`

## Authorization and isolation behavior

- View: any EnterPos role; Manage: Owner/Admin only.
- Queries keyed by organization id; cross-org concealed.
- Incomplete GET returns in-memory defaults (not forced persist until complete).

## Tests executed and results

- `PosOperationalSetupApiTests` — complete + default register; Cashier denied; cross-org isolation (**3 passed** in feature run).
- `OperationalSetupTaxCalculatorTests` + role matrix updates (**passed**).

## Deferred items

- Advanced tax engines; multi-branch/store trees.
- Editable default register reassignment UX beyond setup fields.

## Commit reference

Final Phase 17 commit recorded in P17-WP08.
