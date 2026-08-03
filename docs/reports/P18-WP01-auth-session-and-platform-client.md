# P18-WP01 — Auth, session, and Platform client

## Summary

Dual authentication for Mobile: Platform session token for Personal/Org Owner APIs and Bearer access token for POS APIs. Expanded `IPlatformAccessClient` / `PlatformAccessClient` and `PlatformSessionHeaderHandler`.

## Delivered

- Register / activate / login / logout / token issue-bind-revoke / start-business / members / invitations / product-local roles / subscription / entitlement client methods
- `AuthSession.PlatformSessionToken` persisted via secure store
- Password sign-in obtains both session and bearer tokens when available
- Logout best-effort remote revoke for both tokens

## Tests

Covered by `AuthenticationServiceTests` (including production password grant + dual token persistence).
