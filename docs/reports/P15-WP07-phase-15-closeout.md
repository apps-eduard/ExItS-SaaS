# P15-WP07 — Audit, Authorization, UX Hardening, and Phase Closeout

[Phase 15](../phases/phase-15-ant-design-platform-admin.md) | [Portfolio](../portfolio-progress.md) | [ADR-015](../decisions/ADR-015-antdesign-blazor-platform-admin.md)

## Status

**Complete.** Starting tip `142fc645df971ea48d9f5b8deac4ea4bfd08e618`. Final tip `3e533f143b3dda73045d6a8b988417003e0f6424` (feature `77f4030fa6110a20f854d0e146132aad0ec5e31c`). Phase 15 closed. P14-WP03 not started. Application remains **not production-ready**.

## Authorization review

### Findings fixed
- Payment GETs (`/payments`, `/payments/{id}`, `/organizations/{id}/payments`) now require `ManageManualPayments` (were unauthenticated reads).
- `GET /users/{id}/product-access` requires `ManageProductAccess`.
- `GET /access/evaluate` requires `ManageProductAccess` or trusted org Owner/Admin.
- Admin pages now show `UnauthorizedPanel` independently of nav: Users, Platform Roles, Payments, Audit, Organization Members, Organization Product Access (`[Authorize]` added where missing).
- Org product-access grants hidden for org admins without `ManageProductAccess` (read/evaluate only).

### Verified already solid
- Trusted session organization context (no URL org substitution for governing mutations).
- Platform directory / platform role APIs require platform permissions; org admins denied.
- Platform / organization / product-local permission catalogs remain isolated.
- Final Platform Administrator revoke blocked; inactive/retired custom roles grant no permissions.

### Residual authorization gaps (accepted for closeout)
- Custom organization permission codes are catalog + effective-permission DTOs; APIs still gate on Owner/Admin membership or `ManageMemberships` (not fine-grained org permission codes).
- Custom platform role assignments are not organization-scoped in the permission resolver (system role org-scoping differs).
- Dev/Live Preview permission fallback may over-open nav when `/authorization/me` fails (documented Live Preview behavior).
- Not every commercial page has a full page-level deny panel (Products/Plans/Subscriptions rely primarily on API 403 + mutation gates).

## Audit coverage

### Findings fixed
- List/read denials for users, members, invitations, and product-access use `platform.access.checked` instead of mutation action codes.
- Invitation create success summary no longer embeds invitee email; denial target id uses organization id (not email).

### Verified
- Phase 15 mutation families emit success audits with actor, action, target, org/product scope, timestamp, and correlation when available.
- Invitation accept tokens and passwords are not written to audit summaries.

### Residual audit gaps
- Most failed business mutations still lack Failed outcome records (Denied is recorded on authz failure).
- Catalog feature/trial mutations reuse product/plan updated action codes.
- Broader success-audit assertions remain spot-checked rather than exhaustive per mutation.

## UX / navigation review

### Findings fixed
- Platform Users taxonomy: All / Unassigned / Organization Users / Platform Staff / Roles & Permissions.
- Organization Memberships label disambiguates the org-picker route from the directory filter.
- Organization shell People group: Members, Invitations (`?tab=invitations`), Roles & Permissions.
- Removed dead Product Access / System Status unfinished ops items; Select organization / Coming soon tags replace misleading Phase 15 tags on disabled items.
- Nav selection keys corrected for directory views, platform roles, and invitations tab.
- Settings submenu retained in both shells; Live Preview login path preserved.

### Residual UX gaps
- Header bell / profile preferences remain non-functional placeholders.
- Launch Product / Usage / Billing Renewal remain Coming soon.
- Some legacy report-style pages (Payments, Audit, Product Access) still mix older shell primitives with Ant Design.
- User-detail assignment wizard remains list/API oriented (WP06 residual).

## Issues found and fixed (summary)

| Area | Issue | Fix |
|---|---|---|
| API | Payment directory readable without authz | Require `ManageManualPayments` |
| API | Product-access evaluate/user list open | Require platform/org authz |
| API | Denial audits mislabeled as creates | `platform.access.checked` |
| Audit | Invitation email in summary | Redacted summary |
| UI | Hidden menu ≠ denied URL | UnauthorizedPanel + Authorize |
| Nav | Duplicate Organization Users labels | Memberships vs directory |
| Nav | Invitations deep-link | `?tab=invitations` |

## Tests

- Unit: Admin architecture guards for permission-gated pages; localization keys.
- Integration: `ApiPhase15CloseoutAuthzTests` (payment reads, product-access reads, org-admin directory denial, invitation audit hygiene, denied list action code).
- Full Release suite: **1301 passed / 0 failed / 0 skipped** (`ASPNETCORE_ENVIRONMENT=Testing`, `dotnet test ExItS.slnx -c Release`). Baseline was 1295; +6 closeout coverage.

## Production blockers (unchanged)

TLS-PROD; MAUI-HTTPS; R-109; R-129 / NU1903; auth email vendor; MFA deferred; D-P12-03; D-P12-04; EVAL-DRIFT; Manual GCash unverified; online-only limits; report export deferred; PITR deferred; local unsynced ops; tax/accounting deferred; formal WCAG cert not claimed. Phase 14 Production remains **Blocked** pending P14-WP03+.

## Residual gaps

- Fine-grained OrganizationPermission API enforcement
- Custom platform role org-scoping parity with system roles
- Exhaustive Failed-audit emission
- Full Ant Design normalization of legacy report pages
- Personal Utang / POS role administration (explicitly out of scope)

## Final Phase 15 status

**Phase 15 — Ant Design Platform Administration is complete** (P15-WP01–WP07). Exact next when authorized: **P14-WP03** (Production TLS / reverse proxy) or other authorized work. Do not claim production readiness.
