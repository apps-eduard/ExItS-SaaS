# P10-WP06 — Advanced Permissions and Operational Reports

Phase marker: `P10-WP06-advanced-permissions-operational-reports`

## Status

**Complete.** Product-local POS operational roles (org-scoped assignments), role×capability matrix, InventoryStaff receive-only purchasing mutations, fifteen role-aware operational reports, migration `AddPosOperationalRoles`, typed permissions API/MAUI surfaces, and focused tests. **Product-local POS-ROLES gap closed.** **R-091 remains open.** Do **not** begin P10-WP07.

Authorize commit: `98264fc`  
Feature tip: `1e46f6eb142d1c14455f954e7c8286abeb1ddff3`  

## Delivered capability

| Area | Delivered |
|---|---|
| Roles | `Owner`, `Admin`, `StoreManager`, `Cashier`, `InventoryStaff`, `ReportingUser`; one active assignment per org+actor; revoke+replace; audited history; no hard delete |
| Bootstrap | Dev/Testing first-owner auto-bootstrap; unassigned actors in Dev/Testing act as Owner when org already has owners (shared-fixture aid); last-owner revoke protection |
| Matrix | `PosRoleMatrix` intersects commercial grants; InventoryStaff PO create/edit/submit/cancel denied (receive allowed) |
| Grants | `store-permissions-view` / `store-permissions-manage`; Platform `FeatureCode`; default development grants |
| Persistence | Migration `AddPosOperationalRoles` → `pos_role_assignments` (+ unique active org+actor index) |
| API | `/api/v1/pos/permissions/*`; operational reports under `/api/v1/pos/reports/{overview,sales-summary,sales-by-payment,sales-by-product,returns,shifts-summary,cash-variance,inventory-status,inventory-movements,stock-count-variance,purchasing-summary,purchase-outstanding,supplier-purchasing,expenses-summary,utang-by-product}` |
| Reports | All 15 approved families; Cashier own-shift scoping; InventoryStaff inventory/purchasing/count only; online-only; no export/P&L |
| MAUI | `/permissions*`; reports hub + `/reports/operational/{kind}`; EN + fil-PH |
| Architecture | `PosPermissionsReportsScopeArchitectureTests`; phase marker on POS/Platform/Deployment |

## Explicit exclusions

Production auth (R-091), MFA/IdP, Platform membership admin, Windows MAUI, Phase 11 web report redesign, multiple registers (WP07), manager approval workflows, accounting/tax/valuation/P&L, payment gateways, export/scheduled/email reports, offline role/report authority.

## Persistence

Database: `ExItS_PinoyBusinessPOS` · Schema: `pos`  
Migration: `20260731061054_AddPosOperationalRoles`  
Prior: `20260731052329_AddPosSaleReturns`  
Table: `pos_role_assignments`  
Validated: apply → rollback → re-apply (`AddPosOperationalRolesMigrationTests`).

## API / UI

- Permissions: list/get/effective/assign/revoke; online-only mutations  
- Operational reports: role + commercial capability gated; Cashier shift reports restricted to own actor  
- MAUI capability gates remain commercial; server enforces role matrix

## Build / test evidence

| Check | Result |
|---|---|
| `dotnet test ExItS.slnx -c Release` | **1138 / 0 / 0** (baseline 1110) |
| MAUI Android Release | Builds with 0 errors (NU1903 advisory remains) |
| Migration apply/rollback/re-apply | Pass |

## Security limitations

- Development/Testing actor + commercial headers are **not** production authentication (R-091 open).  
- Unassigned Dev/Testing actors may act as Owner when org already has owners (fixture compatibility); explicit assignments always win.  
- Product-local POS-ROLES closed; Platform IdP/JWT/MFA not delivered.

## Portfolio independence

- No root `HealthCare/` directory  
- `git ls-files -- HealthCare/` empty  
- `dotnet sln ExItS.slnx list` has no HealthCare project  
- Untracked `docs/phases/phase-11-web-ui-reporting-design-system.md` left alone (not committed; WP07/Phase 11 not started)

## Risks / open decisions

- **R-091** production auth remains open  
- **POS-ROLES** product-local gap **closed** in this WP  
- R-109, R-129/NU1903, TLS-PROD, MAUI-HTTPS remain open  
- Report export deferred; Phase 11 web redesign deferred

## Exact next work package

**P10-WP07 — Multiple Registers** — do **not** begin until explicitly authorized.
