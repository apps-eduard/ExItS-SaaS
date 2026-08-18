# P19 — Multi-user offline cashier PIN access (shared POS terminals)

| Field | Value |
|---|---|
| Status | **Code Complete** |
| Device Verified | **No** |
| Production Ready | **No** |
| Phase 19 Closed | **No** |
| Date | 2026-08-13 |
| Tip hash | `8387f0af786c69bb7b52550908d0e364274a310f` |
| Related | [P19-offline-operability-foundation](P19-offline-operability-foundation.md), [P19-offline-pin-same-user-relogin-fix](P19-offline-pin-same-user-relogin-fix.md) |

## Model

**One authorized POS device → multiple previously online-verified cashiers.**

| Concern | Behavior |
|---|---|
| Storage | Per-user SecureStorage slots: `pos.offline.grant.{userId}`, `pos.offline.pin.{userId}`, directory `pos.offline.enrolledUsers` (no secrets) |
| PIN | Salted PBKDF2 verifier per user; never raw/reversible; same numeric PIN allowed across users (identity-scoped) |
| Grant | Per-user offline operating grant; default lifetime **720 hours (30 days)** from last **online** validation |
| PIN vs grant | PIN itself does **not** time-expire; offline **authorization** expires |
| Offline unlock | Does **not** extend `IssuedAtUtc` / `LastOnlineValidatedAtUtc` / `ExpiresAtUtc` |
| Online renew | Successful authoritative online establish renews **that** user's grant only |
| Logout | Clears active session; **preserves all** enrolled users' grants + PINs |
| Account switch | Enrolling Paul does **not** delete Mica |
| Remove offline access | `RemoveEnrolledUserAsync` / `RemoveEnrolledOfflineUserAsync` removes one user only |
| Lockout | Failed attempts + lockout are **per-user** (default 5 / 15 minutes) |
| Device binding | Still enforced on unlock |
| Expiry UI | Uses **Online verification required** (`Offline_GrantExpired`) — never “PIN expired” |
| Revocation | When online, server denial clears **that** user's grant; cached grant never overrides server denial |

## Offline UX

- Multiple unlockable enrolled users → account picker → PIN for selected user
- Single enrolled user → direct PIN entry
- Only locally enrolled users are listed (no arbitrary enumeration)
- Sign In shows a round keypad (**Sign in with PIN**) beside Google whenever a complete eligible PIN identity exists, online or offline. Eligibility is restored from persisted per-user grant + PIN (`EvaluateOfflineColdStartOfferAsync` / `CanOfferPinUnlock`). After a correct PIN, reachable servers revalidate **that** user (`ValidatedOnline` / `TransientUnavailable` / `ExplicitlyRevoked`); unreachable servers stay LocalOffline. Online restore must not wipe a grant unless the server explicitly revokes product/device access. Lock then another user's PIN never reuses the prior AccessToken. Pending outbox `user_id` stays the original creator. Local Validation uses the same grant+verifier enrollment path. Unprovisioned, expired, revoked, or corrupted credentials do not expose PIN. First-time offline devices never see a PIN field or **Invalid PIN** / **Incorrect PIN**; that copy is only after an enrolled verifier exists and the entered PIN is wrong. Forgot PIN while offline cannot be reset locally — reconnect and authenticate to change PIN.

## Legacy migration

On first store access:

1. Read legacy `pos.offline.operatingGrant` / `pos.offline.pinVerifier`
2. Migrate only when a stable `UserId` is attributable (grant.UserId or bound pin.UserId)
3. Copy into per-user keys; update directory
4. Remove legacy keys only after successful attributable migration
5. Corrupt / unbound / ambiguous legacy → fail closed (leave keys; do not guess)
6. `EnsureMigratedAsync` is idempotent

## Personal vs Organization

Shared per-user storage applies to both scopes, but **scope isolation is unchanged**:

- Personal eligibility / Personal local DB unchanged
- Personal grant must not unlock Organization POS shell
- Org POS cashiers remain identity-scoped staff/owner users

## Development expiry override

`OfflineOperatingGrantOptions.AllowDevelopmentExpiryOverride` (default **false**) gates `ForceExpireGrantForDevelopmentAsync` for QA. Must stay off outside Development/Test.

## Owner validation checklist (manual)

### ONLINE
1. Login Mica → enroll PIN → logout  
2. Login Paul → enroll PIN → logout  
3. Login Mica online again → no unnecessary PIN recreation  

### OFFLINE
4. Disconnect network  
5. Confirm Mica and Paul selectable  
6. Unlock Mica with Mica PIN → verify identity/permissions  
7. Logout → unlock Paul with Paul PIN → verify identity/permissions  
8. Cross-PIN attempts fail  
9. Permitted offline operation attributes to the unlocked cashier  

### EXPIRY
10. Dev-only force-expire (or FakeClock in tests)  
11. Unlock fails with online-verification message (not “PIN expired”)  
12. Reconnect online → grant renews without unnecessary PIN recreation  

## Explicit exclusions

- Device Verified / Production Ready remain **No** until owner validation
- No shared cashier PIN
- No unlimited offline lifetime
- Do not invent a second shift system
