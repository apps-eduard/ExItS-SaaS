# P25 — Organization Owner management authority fix

| Field | Value |
|---|---|
| Status | Code Complete / Owner Validation Pending |
| Phase | 25 (Open) |
| Browser Verified | No |
| Device Verified | No |
| Production Ready | No |

## Bug

Organization Owner (e.g. Mica Uy → Mica Store) successfully authenticated and was correctly routed to Organization Web, but Overview and other business/API requests failed with a sanitized unauthorized message suggesting Platform Admin.

## Root cause

1. Org Web hydrator required successful `IssueToken` **and** `/access/evaluate` Allowed before binding product Bearer.
2. `IssueToken` with product code required `CanOperate` = commercial entitlement **and** product-local role.
3. Organization Owner without a product-local selling role received no Bearer; POS APIs fell through to Development-stage org/actor/commercial headers and returned unauthorized.
4. UI wording incorrectly suggested a Platform-only account.

## Fix

- Introduce `OrganizationManagementAuthority` for Platform `OrganizationOwner` / `OrganizationAdministrator` with active commercial entitlement.
- Session grant / bind / introspect may issue org+product Bearer for management without inventing a POS checkout role.
- POS `PosRoleAuth` allows management capabilities under that authority; **denies** `CreateSale` and `EnterPos`.
- Org Web hydrator binds Bearer from session grant success (not admin evaluate).
- Sanitized error no longer tells Org workspace users to open Platform Admin.

## Invariants preserved

- Owner ≠ automatic Cashier / checkout.
- Manager remains management subset; Owner-only surfaces stay Owner-only.
- Cashier remains denied Organization Web.
- No `view_portfolio` granted to Owners.
- Multi-org authority recomputes per selected OrganizationId.

## Tests

- `OrganizationManagementAuthorityTests`
- `PosRoleMatrixTests` organization-management cases
- `OrganizationOwnerManagementMatrixTests` (Owner / Manager / Cashier / multi-org / hydrator guard)

## Privacy

No privacy expansion; authorization mapping only. Phase 21 remains Open.
