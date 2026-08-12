# P24-WP03 — Linked Customer Authorization Contract

[Phase 24](../phases/phase-24-linked-customer-statements-and-personal-monetization.md) | [WP01](P24-WP01-current-state-and-architecture-contract.md) | [WP02](P24-WP02-customer-link-and-pos-correlation.md) | [ADR-021](../decisions/ADR-021-linked-customer-statements-and-personal-monetization.md) | [Portfolio](../portfolio-progress.md)

| Field | Value |
|---|---|
| Status | **Complete** (authorization foundation; no statement APIs) |
| Date | 2026-08-12 |
| Starting SHA | `457accc0bf9d05e0c00b87460e5a9190b347a168` on `main` |
| Implementation commit | `d8c90f0c46fe8d70efb93970fb93d96412c5fc39` |
| Docs/hash-stamp commit | pending hash-stamp |
| Device Verified | **No** |
| Production Ready | **No** |
| Migration | **No** |

## Status legend

WP03 delivers a reusable, fail-closed authorization contract for later Personal statement/balance/receipt reads. It does **not** implement those APIs, copy Business Utang, or start WP04. **Not Device Verified. Not Production Ready.**

## Architecture inspected (before change)

Confirmed in code at starting SHA `457accc0`:

| Area | Finding |
|---|---|
| `LinkedCustomerAppUser` | Created on accept; statuses **Active** / **Revoked** only. Pending/declined/expired live on `CustomerLinkRequest`. |
| Active lookup | `FindActiveByUserOrganizationAndBusinessCustomerAsync` existed and was unused — this is the WP03 Platform lookup. Do **not** use `FindActiveByUserAndOrganizationAsync` (ambiguous when one Personal user has multiple customers in one org). |
| Unique active link | Per `business_customer_id` only; one Personal user may have several Active links in the same org to different customers. |
| Unlink | Soft revoke; does **not** clear `POSCustomer.PlatformBusinessCustomerId`. |
| Personal APIs | `GET /api/v1/personal/linked-merchants`; unlink; `TryGetPersonalContext` reads `NameIdentifier` (no client userId). Scope guard allows Personal only. Missing `AccountClass` treated as Platform (fail closed). |
| POS correlation | `POSCustomer.PlatformBusinessCustomerId` value only; unique filtered index per org; lookups org-scoped. Same Platform id may exist in two POS orgs (WP02 limitation). |
| Ownership | Platform: identity, `BusinessCustomer`, `LinkedCustomerAppUser`. POS: `POSCustomer`, Business Utang. **No cross-database FK.** |
| POS staff statements | Staff + `UtangCapability` / `X-Pos-Organization-Id`. Must **not** be reused as the linked-customer principal. |
| Platform→POS catalog client | Support-key catalog client — **not** reused for Personal authz. |
| Error mapping | `AccountScopeDenied` / `CustomerLinkPersonalIdentityRequired` → 403; `LinkedCustomerAppUserNotFound` → 404; `UserNotActive` → 409. |

No unexpected architecture required a schema change or a second customer authority.

## Authorization contract

Two complementary application services, split on the ownership boundary:

```text
AuthorizeLinkedCustomerAccess          (Platform Application)
AuthorizeLinkedCustomerStatementAccess (POS Application)
```

Platform proves the Personal link. POS proves organization-scoped correlation. POS Application does not query the Platform database; it depends on `ILinkedCustomerPlatformAuthorization` (HTTP adapter deferred to WP04).

### Inputs

Authenticated Personal identity comes from **server session context only**. Callers must not pass a client-supplied user id.

Platform:

```text
AuthorizeLinkedCustomerAccess.ExecuteAsync(
    currentPersonalUser,   // from session
    accountClass,          // from session; missing → Platform → deny
    organizationId,
    platformBusinessCustomerId)
```

POS composer (for later statement APIs):

```text
AuthorizeLinkedCustomerStatementAccess.ExecuteAsync(
    organizationId,
    platformBusinessCustomerId,
    posCustomerId: optional)
```

### Exact proof chain

```text
Authenticated Personal identity
        ↓
current identity is active
        ↓
AccountClass is Personal
        ↓
not Organization staff (HomeOrganizationId / IsOrganizationScopedStaff)
        ↓
not Platform Admin (StaffNumber)
        ↓
active accepted LinkedCustomerAppUser
  (same Personal user + requested Organization + requested BusinessCustomer)
        ↓
BusinessCustomer still belongs to that Organization
        ↓
BusinessCustomer is not Archived
        ↓
BusinessCustomer.LinkedUserIdentityId matches the Personal user
        ↓
POSCustomer in the same Organization
        ↓
exactly one POSCustomer with PlatformBusinessCustomerId = that BusinessCustomer
        ↓
POSCustomer.PlatformBusinessCustomerId matches
        ↓
optional posCustomerId matches (when supplied)
        ↓
AUTHORIZED
```

If any required condition is missing or mismatched: **DENY**.

No email, name, or phone matching. No fallback to legacy null correlation.

### Success context

Platform HTTP / `AuthorizedLinkedCustomerPlatformContext`:

```text
PersonalUserId
OrganizationId
PlatformBusinessCustomerId
LinkedCustomerAppUserId
```

POS composer / `AuthorizedLinkedCustomerContext` adds:

```text
PosCustomerId
```

No balances, ledger rows, notes, staff metadata, or other internal fields.

### Failure behavior

| Condition | Platform | POS composer |
|---|---|---|
| Wrong account class | `application.auth.account_scope_denied` **403** | `pos.linked_customer.denied` **403** (when Platform outcome is Denied) |
| Staff / Platform Admin identity | `platform.customer_link.personal_identity_required` **403** | Denied **403** |
| Inactive user | `UserNotActive` **409** | NotFound **404** if Platform maps inactive as not-found at the adapter; unit fake Denied/NotFound as configured |
| Missing / pending / declined / expired / revoked link | `application.linked_customer_app_user.not_found` **404** | `pos.linked_customer.not_found` **404** |
| Wrong org / wrong BusinessCustomer / guessed ids | same **404** | same **404** |
| Archived customer / identity mismatch | **404** | **404** |
| No POS customer / null correlation / mismatch / other org / duplicate correlation | n/a (Platform-only proof) | **404** |

Guessing does not reveal whether another customer's link, another org's BusinessCustomer, or a POS customer elsewhere exists. Messages are `"Linked customer was not found."` / `"Linked customer access is denied."` with no internal ids.

### Link revoke behavior

```text
active accepted link  → authorized
revoked/unlinked link → immediately denied
```

POS `PlatformBusinessCustomerId` may remain. That is intentional:

```text
correlation = identity relationship between records
link status = current Personal access permission
```

WP03 does not clear POS correlation on unlink.

### POS correlation behavior

- Lookup is always `FindByPlatformBusinessCustomerIdAsync(org, id)`.
- Count must be exactly **1**; 0 or >1 → 404.
- Null correlation (legacy) is not a match.
- A correlated POS customer in another organization is not visible to the requested org (404).
- Unique index still rejects duplicate correlation on create (WP02 regression).

### Platform HTTP surface (justified)

```text
GET /api/v1/personal/linked-merchants/authorization?organizationId=&businessCustomerId=
```

Personal scope. Session identity from `TryGetPersonalContext`. Missing query ids → 404. **No balances.** No public POS authorization or statement endpoint in WP03.

WP04 should wire `ILinkedCustomerPlatformAuthorization` to this GET using the Personal `PlatformSession` (not the support-key catalog client), then call `AuthorizeLinkedCustomerStatementAccess`.

### Cross-database FK

**None.** Platform stores no POS customer id. POS stores only the Platform `BusinessCustomerId` value. No EF navigation across databases.

## Tests / builds

| Suite | Result |
|---|---|
| `ExItS.Platform.UnitTests` Release | **Passed 765**, failed 0, skipped 0 |
| Filter `LinkedCustomerAuthorizationTests\|CustomerLinkCompletenessTests` | **Passed 31**, failed 0, skipped 0 |
| `ExItS.PinoyBusinessPOS.UnitTests` Release | **Passed 506**, failed 0, skipped 0 |
| Filter `LinkedCustomerAuthorizationUseCaseTests\|CreatePOSCustomerUseCaseTests` | **Passed 19**, failed 0, skipped 0 |
| Filter POS `Customers\|Credit\|Statements\|Payments.Repayment` | **Passed 72**, failed 0, skipped 0 |
| `ApiOrganizationStaffCustomerSeparationTests` Release | **Passed 9**, failed 0, skipped 0 |
| `ExItS.Platform.Api` Release build | Succeeded; 2 pre-existing CS0618 `HasCheckConstraint` warnings |
| `ExItS.PinoyBusinessPOS.Api` Release build | Succeeded; 4 pre-existing NU1510 prune warnings |

Covered: active Personal success and context fields only; unrelated/inactive/staff/admin/wrong class; no link; pending/declined/expired invitation; revoked link; wrong org; wrong BusinessCustomer; guessing 404; merchant A vs B in same org; two Active links in one org independently; archived customer; Platform 403/404 mapping; missing POS customer; legacy null correlation; mismatched correlation; other-org POS customer; optional `posCustomerId` mismatch; duplicate correlation fail-closed; WP02 accept/list/unlink regression; duplicate POS create still conflicts.

Not run: full `ExItS.slnx` test pass; POS HTTP integration against the Platform authorization GET (no POS adapter in WP03); device/UI validation.

## Known limitations

- POS API does not yet register `ILinkedCustomerPlatformAuthorization` or `AuthorizeLinkedCustomerStatementAccess`. WP04 must add a session-forwarding HTTP adapter and DI.
- Same Platform BusinessCustomer id may still exist in two POS organizations at persistence; authorization requires Platform link org **and** POS org **and** correlation.
- Unique POS correlation index is not status-filtered (inactive POS customer still holds the id).
- Platform unlink still does not clear POS correlation.
- The Platform GET is a Personal proof endpoint, not a statement API. Do not treat it as production-complete customer access.
- Development-stage unauthenticated/org-header POS APIs remain not production-secure.
- Not Device Verified. Not Production Ready.

## Explicit exclusions (not started)

Outstanding balance endpoint; Personal statement; recent activity; pagination; receipt summary/detail; partial-payment projection; paid history gating; rewards; ads; feature unlocks; disputes; reconciliation UI; archive; copying Business Utang into Personal Utang.

## Exact WP04 recommendation

**P24-WP04 — Lightweight linked Business Utang statement projection.**

WP04 may implement:

- current outstanding balance
- small recent activity page
- existing partial-payment projection
- summary-only DTOs
- server page limits

Wire POS `ILinkedCustomerPlatformAuthorization` to the Platform Personal authorization GET, then call `AuthorizeLinkedCustomerStatementAccess` before projecting POS ledger data. Keep POS Business Utang authoritative. Do not copy into Personal Utang.

Still defer full receipt detail / lazy receipt work if the phase plan assigns that to WP05.

Do **not** start WP04 automatically from this package.

## Files / docs changed

Implementation: Platform `AuthorizeLinkedCustomerAccess` + Personal GET; POS `AuthorizeLinkedCustomerStatementAccess` + `ILinkedCustomerPlatformAuthorization`; `CountByPlatformBusinessCustomerIdAsync`; error codes; unit and integration tests.

Documentation: this report; phase-24; portfolio-progress; reports README; FILE-MANIFEST; phases README.

## Checks performed

- `git status` / branch `main` / starting HEAD = `origin/main` = `457accc0bf9d05e0c00b87460e5a9190b347a168`
- No stash, reset, rebase, amend, squash, or force-push
- Focused commits only (no `git add .`)
- Migration: **No**
