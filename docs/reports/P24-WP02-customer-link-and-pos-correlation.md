# P24-WP02 — Customer-link completeness + POS↔Platform customer correlation

[Phase 24](../phases/phase-24-linked-customer-statements-and-personal-monetization.md) | [WP01](P24-WP01-current-state-and-architecture-contract.md) | [ADR-021](../decisions/ADR-021-linked-customer-statements-and-personal-monetization.md) | [Portfolio](../portfolio-progress.md)

| Field | Value |
|---|---|
| Status | **Complete** (identity join; no statement APIs) |
| Date | 2026-08-12 |
| Starting SHA | `b27c94b6328d7d5fb56e2b2b7d0d77372141e912` on `main` |
| Implementation commit | `19786a3d2a19fdf0131c5ca315a272e012ab2926` |
| Docs/hash-stamp commit | *(this docs commit)* |
| Device Verified | **No** |
| Production Ready | **No** |
| Migration added | **Yes** — `AddPosCustomerPlatformBusinessCustomerId` (`20260812130703`) |
| Platform migration | **No** |

## Status legend

WP02 delivers the identity bridge between Platform `BusinessCustomer` and POS `POSCustomer`, plus Personal-only accept, list-by-current-user, and auditable unlink. It does **not** implement Personal statement/history APIs, copy Business Utang, or start WP03. **Not Device Verified. Not Production Ready.**

## Architecture inspected (before change)

Confirmed in code at starting SHA:

| Area | Finding |
|---|---|
| `BusinessCustomer` | Org-owned; `LinkedUserIdentityId`; `LinkAppUser` / `UnlinkAppUser`; never staff |
| `CustomerLinkRequest` | Token + email; Pending/Active/Declined/Revoked/Expired |
| `LinkedCustomerAppUser` | Created on accept; `Revoke()` existed but unused; unique active link per customer |
| Accept flow | Active user + email match only — **no** Personal `AccountClass` / `HomeOrganizationId` / Platform-staff guard |
| Accept/decline routes | Scope-guard exempt so Personal sessions can hit them |
| `POSCustomer` | **No** Platform correlation column |
| POS Utang | Authoritative ledger; outstanding = active credits − active repayments; unchanged |
| Personal APIs | No list-of-linked-merchants; no unlink |
| Cross-DB FK | None; ownership boundaries intact |
| Test seeds | POS customers created without Platform ids (legacy-valid) |

No unexpected architecture required a different correlation property name. Repository convention uses explicit Platform-prefixed value ids on POS (not a navigation/FK).

## Correlation model

```text
POSCustomer.PlatformBusinessCustomerId : Guid?
```

- Value/correlation identifier only — **not** a cross-database FK
- Nullable for legacy/unlinked POS customers
- Unique filtered index per organization: `ux_customers_org_platform_business_customer` on `(organization_id, platform_business_customer_id)` WHERE `platform_business_customer_id IS NOT NULL`
- Index is **not** status-filtered: an inactive POS customer still holds the correlation
- Lookups are always organization-scoped: `FindByPlatformBusinessCustomerIdAsync(org, id)`
- Empty GUID rejected
- Never match by display name, email, or phone
- Do not auto-merge customers
- Do not rewrite POS financial history

### Lifecycle

| Case | Behavior |
|---|---|
| New Platform BusinessCustomer that also needs POS representation | POS create may pass optional `PlatformBusinessCustomerId`; rejected if another POS customer in the **same org** already has that id |
| Existing POS customer later correlated | `PUT /api/v1/pos/customers/{id}/platform-correlation` (staff, `EditCustomer`) |
| Existing Platform BusinessCustomer later correlated to POS | Same PUT / create-with-id; POS cannot HTTP-verify Platform org in WP02 |
| Duplicate correlation (same POS customer, same id) | Idempotent success |
| Duplicate correlation (same id, different POS customer, same org) | Conflict (`pos.customer.platform_business_customer.correlation_conflict`) |
| Different id on an already-correlated POS customer | Domain conflict; must clear first |
| Cross-org lookup/mutate | Fail closed (404) — POS org header/scope required |
| Same Platform id in two POS orgs | Allowed at POS persistence (no Platform round-trip in WP02). WP03 statement authz must still require Platform link org + this correlation |
| Clear correlation | Explicit staff `DELETE .../platform-correlation`; idempotent if already null |
| Platform unlink | Does **not** clear POS correlation |
| Deactivate POS customer | Correlation retained (unique index not status-filtered) |
| Legacy null correlation | Valid for all existing POS Utang/customer operations |

## Personal-only accept

`AcceptCustomerLinkRequest` now requires:

1. Authenticated user exists and `AccountStatus.Active`
2. Session `AccountClass == Personal` (missing class treated as Platform at the endpoint → denied)
3. Identity is not org-scoped staff (`HomeOrganizationId` null / `IsOrganizationScopedStaff` false)
4. Identity is not Platform Admin (`StaffNumber` null)
5. Invitation email still matches `NormalizedEmail`
6. `CustomerStaffSeparationGuard` still proves no membership/staff/product role

Denied: organization session, Platform session, org-scoped staff identity, Platform staff identity, inactive user, unrelated Personal user (email mismatch), guessed/expired/revoked tokens.

Accept still creates **no** Organization membership, staff role, or product role.

## List by current Personal user

`GET /api/v1/personal/linked-merchants?page&pageSize`

- Server uses the authenticated Personal session user (no client `userId`)
- Personal scope guard (`/api/v1/personal` → `AccountClass.Personal`)
- Offset pagination via `CatalogPagination` (default 20, max 100)
- Active `LinkedCustomerAppUser` rows only
- Safe metadata: `LinkedCustomerId`, `BusinessCustomerId`, `OrganizationId`, `OrganizationDisplayName`, `CustomerDisplayName`, `LinkStatus`, `LinkedAtUtc`
- Skips rows whose BusinessCustomer is missing or org-mismatched (fail closed)

## Revoke / unlink

| Operation | Who | Effect |
|---|---|---|
| Pending invitation revoke | Org staff (`POST .../customer-link-requests/{id}/revoke`) | Request status `Revoked`; accept token unusable |
| Accepted link unlink (Personal) | Owner only (`POST /api/v1/personal/linked-merchants/{id}/unlink`) | `LinkedCustomerAppUserStatus.Revoked`; `BusinessCustomer.UnlinkAppUser`; unknown/other-user id → 404 |
| Accepted link unlink (staff) | Org membership managers (`POST .../linked-customer-app-users/{id}/revoke`) | Same soft revoke, org-scoped |

Soft lifecycle (no hard delete):

- Does not delete POS transactions
- Does not delete `BusinessCustomer`
- Does not delete Personal user
- Does not mutate Personal Utang
- Future Personal statement access must fail closed (list no longer returns the link; WP03 must also check Active status)

**Re-link after unlink requires a new invitation.** The original accept token is consumed (request stays `Active`). Unique active-link index allows a new Active row after Revoked.

Unlink is idempotent if already Revoked (owner/staff who can see the row).

## POS correlation APIs (staff, org-scoped)

| Method | Path | Capability |
|---|---|---|
| GET | `/api/v1/pos/customers/by-platform-business-customer/{id}` | `ViewCustomersAndHistory` |
| PUT | `/api/v1/pos/customers/{id}/platform-correlation` | `EditCustomer` |
| DELETE | `/api/v1/pos/customers/{id}/platform-correlation` | `EditCustomer` |
| POST create | optional `PlatformBusinessCustomerId` | `CreateCustomer` |

Not unauthenticated. Development-stage POS still uses `X-Pos-Organization-Id` (existing POS API model). Application-layer methods exist for internal use (`GetByPlatformBusinessCustomerIdAsync`, `CorrelatePOSCustomerToPlatformBusinessCustomer`, `ClearPOSCustomerPlatformCorrelation`). No Personal statement endpoints.

## Authorization behavior

- Personal list/unlink: current user only; identifier guessing → 404
- Staff cannot accept as Personal (session class and identity class)
- Cross-user unlink/list leakage denied
- Unlink immediately removes the row from the Personal list
- Pending revoke still blocks accept
- POS correlation is org-scoped; wrong org → 404
- Legacy uncorrelated POS customers remain valid for Utang

## Tests

| Suite | Result |
|---|---|
| `ExItS.Platform.UnitTests` Release | **746 passed**, 0 failed, 0 skipped |
| `ExItS.PinoyBusinessPOS.UnitTests` Release | **495 passed**, 0 failed, 0 skipped |
| `ApiOrganizationStaffCustomerSeparationTests` | **8 passed** (includes new accept/list/unlink/revoke cases) |
| `PosCustomerApiTests` + `AddPosCustomerPlatformBusinessCustomerIdMigrationTests` | **5 passed** |
| Platform API Release build | Succeeded (2 pre-existing CS0618 check-constraint warnings) |
| POS API Release build | Succeeded |

Covered: Personal accept success; staff/Platform/org-staff identity denied; unrelated Personal denied; inactive denied; no membership/role; list current-user only; cross-user leakage; unlink blocks list; pending revoke; duplicate/idempotent correlation; cross-org POS lookup 404; identifier guessing 404; legacy null correlation; Utang unit tests still pass on uncorrelated customers.

## Known limitations

- POS does not call Platform to verify `BusinessCustomer.OrganizationId`. Same Platform id may exist in two POS orgs. WP03 must fail closed using Platform link org + POS org + correlation.
- Platform unlink does not clear POS `PlatformBusinessCustomerId`.
- Unique POS correlation index is not status-filtered.
- `ListLinkedMerchantsForPersonalUser` `totalCount` is the Active-link table count; mapped items may be fewer if a BusinessCustomer row is missing.
- POS APIs remain development-stage org-header scoped (not production-secure).
- No customer-link email delivery (pre-existing P16 exclusion).
- No Personal statement/balance/receipt APIs (WP04+).
- Not Device Verified. Not Production Ready.

## Explicit exclusions (not started)

Personal Business Utang statement API; current balance projection; receipt history; cursor pagination; free-vs-paid history; Personal entitlements; rewards; ads; receipt lazy-loading UI; disputes; reconciliation UI; cold archive; PDF/export.

## Exact WP03 recommendation

**P24-WP03 — Linked-customer authorization contract.**

Implement the fail-closed authorization principal for future Personal statement reads:

```text
Personal session
  → active LinkedCustomerAppUser (this user + org + BusinessCustomer)
  → POSCustomer in that org with PlatformBusinessCustomerId = BusinessCustomerId
  → then (later WP) project POS ledger
```

Do **not** start statement/balance/receipt APIs until this contract and tests exist. Include Platform-side link verification that POS can trust (or a Platform-issued proof) so cross-org correlation cannot be used for statements. Keep POS Business Utang authoritative. Do not copy into Personal Utang.

## Files / docs changed

See git commits. Primary surfaces: POS `POSCustomer` + migration; Platform accept/list/unlink; tests; this report; phase/portfolio indexes.

## Checks performed

- `git status` / branch `main` / starting HEAD = `origin/main` = `b27c94b6328d7d5fb56e2b2b7d0d77372141e912`
- No stash, reset, rebase, amend, squash, or force push
- Unrelated local/untracked APKs/screenshots/tmp files preserved
- Focused commits only (`git add` of intended paths, not `git add .`)
