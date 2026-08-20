# POS-REACT-WORKSPACE-EXPERIENCE-01

## Status

**PASS** (implementation + focused React/Playwright evidence)

## Package ID

`POS-REACT-WORKSPACE-EXPERIENCE-01`

## Starting SHA

`035ceb074f5cf1e1c27e10e9ee84766892280b63`

## Implementation SHA

`b92c2a14fd1ad08411b843ff5c9295bd046f6df6`

Branch: `feat/pos-react-client`

## Discovery

### Organization-level Manage Business

**SUPPORTED** without inventing a branch.

- Platform session grant + organization context do **not** require a branch for management authority.
- React previously forced `/org` behind `RequireWorkspaceBound` with a non-null `branchId`.
- This package adds `bindOrganizationManagementGrant` (org context + grant, no branch / no operational-branch) and `RequireOrganizationBound` for `/org`.

### Staff roster contract

**YES** (Platform) — `GET /api/v1/platform/organizations/{id}/members` (ManageMemberships / Owner path).

React now consumes this read-only for Owner/Admin experience when the call succeeds. Branch staff display uses DTO `productRoles` + `branch` when present. No migration. No impersonation.

### Personal ↔ Business switcher

**DEFERRED** — Platform profile APIs exist (`listAccountProfiles` / `selectAccountProfile`), but React account-menu Personal switch was not proven safe for full POS context clear in this package.

Flag: `OWNER_PERSONAL_SWITCHER_DEFERRED_CONTRACT_GAP=YES`

Staff `LinkedPersonalUserId` never creates a Personal shortcut.

## Destination model

UX experiences (not security roles):

| Experience | Route | Branch |
|---|---|---|
| `manage_business` | `/org` | null |
| `operations` | `/role/manager` | required |
| `start_selling` | `/sell` | required |

Smart routing: exactly one meaningful destination → auto-bind + navigate; else chooser. Never auto-select across orgs.

## Role vs experience

Experience selection does **not** mutate POS/security role. Owner Start Selling remains Owner actor.

## UI

- Expandable organization cards with Management + Branches hierarchy
- Friendly labels (Owner / Admin / Manager / Cashier)
- Account menu: Switch workspace / Switch experience (clears bind → chooser)
- Responsive max-width chooser; ≥44px targets; keyboard focus-visible

## Payment product note (docs only)

Current user-facing checkout methods remain Cash / GCash / Utang (internal ManualGCash). Card and provider GCash are future — not implemented here.

## Tests

- Vitest: 35 files / 131 passed (includes `workspace-destinations`, members roster helpers)
- Playwright focused: RMAP-02 / 02R / 03 / 01b / auth-session / shell-account-ux — **35 passed**
- typecheck / lint (warnings only pre-existing) / build — pass

## Flags

- `WORKSPACE_EXPERIENCE_UX_PASS=YES`
- `ORG_LEVEL_MANAGE_BUSINESS_PROVEN=YES`
- `EXPERIENCE_DOES_NOT_MUTATE_ROLE=YES`
- `SMART_SINGLE_DESTINATION_ROUTING=YES`
- `WORKSPACE_STAFF_ROSTER_DEFERRED_CONTRACT_GAP=NO` (management + best-effort branch staff from members DTO)
- `OWNER_PERSONAL_SWITCHER_DEFERRED_CONTRACT_GAP=YES`
- `RMAP_08_STARTED=NO`
- `RMAP_B04_STARTED=NO`
- `RMAP_TAX_STARTED=NO`
- `REACT_DISCOUNT_UX_STARTED=NO`
- `PRODUCTION_READY=NO`
- `CUTOVER_AUTHORIZED=NO`

## Exact next

HARD STOP. Do not start RMAP-08 / B04 / TAX / discount / checkout.
