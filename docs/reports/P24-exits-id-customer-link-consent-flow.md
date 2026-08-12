# ExItS-ID customer-link consent flow

| Field | Value |
|---|---|
| Status | **Complete** (implementation) |
| Device Verified | **No** |
| Production Ready | **No** |
| Phase 24 Closed | **No** |
| Migration | `20260812204536_AddCustomerLinkTargetAndOrgNotifications` |
| Implementation tip | _(stamped after push)_ |
| Platform feat | `1d18baab` |
| POS/UI/tests feat | `ad5d9e37` |
| Docs | `4f843034` + stamp `9fa4b8f5` / `7c55f907` |

## Rule (authoritative)

For an **existing ExItS Personal user**:

1. Public ExItS ID / QR resolution identifies a person — it does **not** grant access or activate a link.
2. Organization saves an organization-local `BusinessCustomer` (POS-correlated via `PlatformBusinessCustomerId`).
3. Platform creates a **PENDING** `CustomerLinkRequest` targeted to that Personal identity.
4. Personal user receives an in-app notification.
5. Personal user **Accept** or **Decline** in-app.
6. **Accept** activates `LinkedCustomerAppUser` (and only then linked-merchant statements/receipts).
7. **Decline** leaves the merchant `BusinessCustomer` intact with **no** active Personal link.

**Auto-linking is prohibited.**

Email invitation/token remains the **fallback** for non-ExItS / legacy flows. Existing token security (expiry, revoke, resend, email match) is preserved.

## Architecture

| Piece | Behavior |
|---|---|
| Target | `CustomerLinkRequest.TargetUserIdentityId` + denormalized `TargetPublicUserId` |
| Orchestration | `CreateBusinessCustomerWithPersonalLink` (customer + pending request, one `SaveChanges`) |
| Personal notify | `PersonalInAppNotification` (`CustomerLinkRequest`) |
| Org response notify | `OrganizationInAppNotification` to **InvitedByUserId** only (`CustomerLinkAccepted` / `CustomerLinkDeclined`) |
| Personal APIs | `GET/POST /api/v1/personal/customer-link-requests…` |
| Org history/status | link-requests list, link-status, stats COUNT aggregation |
| MAUI | Org create → pending; Personal Accept/Decline + notifications; Org response inbox |

## Follow-up fix — create save “Business customer was not found”

| Field | Value |
|---|---|
| Symptom | ExItS ID resolve succeeded (“Identity confirmed”) but **Save** returned **Business customer was not found** |
| Cause | Orchestration `Add`ed the customer then called `CreateCustomerLinkRequest` with `persist: false`. Link use case `GetByIdAsync` used `AsNoTracking` and could not see the unsaved tracked entity |
| Fix | Pass `knownCustomer` into link creation; repository `GetByIdAsync` also checks `DbSet.Local` before the database query |
| Deploy note | Requires **Platform API** restart/redeploy (MAUI rebuild not required for this error alone) |

## Explicit exclusions

- No organization membership / staff / product role from resolve or accept
- No balances/history before Accept
- Device Verified / Production Ready remain **No** until owner validation
- WP24 remains **Awaiting Owner Validation**

## Tests

`FullyQualifiedName~CustomerLink`: **Passed 26** / failed 0 / skipped 0 (includes unsaved-customer orchestration regression)
