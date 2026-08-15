# P25 — Organization Web runtime Owner authorization remediation (AsyncLocal + entitlement)

| Field | Value |
|---|---|
| Status | Code Complete / Owner Validation Pending |
| Phase | 25 (Open) |
| Browser Verified | No |
| Device Verified | No |
| Production Ready | No |
| Prior package | [P25-owner-organization-management-authority-fix.md](P25-owner-organization-management-authority-fix.md) |

## Bug

After the first management-authority fix, Owner login and Org Web routing still succeeded in Local Validation, but Overview and Branches failed at runtime:

- Overview: sanitized “We couldn't verify your access…”
- Branches: sanitized “You don't have permission…” (Platform `view_portfolio` fallthrough)

## Exact runtime root cause

1. **AsyncLocal session/Bearer loss (primary):** `OrgWebSessionAmbient` was set during hydrate / circuit open, but Blazor Server inbound activities often lose `AsyncLocal`. `IHttpClientFactory` handlers are **not** circuit-scoped, so PlatformSession and product Bearer were frequently empty on page HTTP calls.
2. **Branches symptom:** Platform `EnsureCanViewOrganizationAsync` could not resolve `PlatformUserId` → membership check failed → fallthrough to `platform.permission.view_portfolio` → Owner saw “permission” wording.
3. **Overview symptom:** POS Staging rejected Development-stage headers when Bearer was missing.
4. **Secondary policy:** management authority previously required commercial entitlement for token qualification; core Org Web management must not depend on paid entitlement globally.

## Why previous unit tests passed

Matrix / hydrator source guards and `OrganizationManagementAuthority.Qualifies` unit tests never exercised Blazor circuit `AsyncLocal` + HttpClient factory handler wiring under Local Validation Staging POS.

## Fix

| Area | Change |
|------|--------|
| Circuit ambient | `OrgWebCircuitSession` holds Session/Access/OrgId; `CreateInboundActivityHandler` calls `ApplyToAmbient()` every inbound activity |
| Management authority | `Qualifies(role)` membership-only; entitlement ignored for core management tokens |
| Overview | Partial failure: Platform branch/staff can render when POS overview 403s |
| Branches | Mutation buttons only after list authorization succeeds |
| OrgWebUi | Distinguish 401 session / 403 permission / plan / verify_portfolio session-gap |
| Navigation | Icon-complete sider (top-level + children); collapsed width 64; Title tooltips |
| Diagnostics | Hydrator logs user/org/owner/manager/tokenIssued/managementAuthorityClaim (never tokens) |

## Authority dimensions (kept separate)

| Dimension | Meaning |
|-----------|---------|
| A. Organization management | Owner full / Manager subset / Cashier none |
| B. Commercial entitlement | Paid/product feature gates |
| C. POS selling | CreateSale / EnterPos |

Owner management (A) must not require C. A must not globally require B for the Org Web host.

## Navigation icon standard

Top-level: Overview `dashboard`, Business `shop`, People `team`, Catalog `appstore`, Inventory `database`, Sales `dollar`, Operations `control`, Settings `setting`.

Collapsed: icons remain; `Title` provides labels; drawer on phone.

## Tests

- Integration: `Owner_session_grant_issues_management_authority_without_selling_role` (+ branches via PlatformSession)
- Unit: entitlement-independent Qualifies; Owner management wiring; sidenav icon/collapsed guards; OrgWebUi state distinction; hydrator inbound ambient guard

## Migrations

None. LocalStore version unchanged.

## Privacy

No privacy expansion. Phase 21 remains Open.

## Validation

Browser Verified: **No** (Owner must retest Local Validation after rebuild). Device Verified: **No**. Production Ready: **No**.
