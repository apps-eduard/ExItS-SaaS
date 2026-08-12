# P19 — Offline PIN same-user re-login fix

| Field | Value |
|---|---|
| Status | **Code Complete** |
| Device Verified | **No** (emulator retest recommended) |
| Production Ready | **No** |
| Date | 2026-08-13 |
| Tip hash | `d88c6a8bfd7153ea9f87c9fbab9251d36079242b` |
| Superseded note | Shared-terminal multi-cashier storage is in [P19-multi-user-offline-cashier-pin](P19-multi-user-offline-cashier-pin.md). Same-user retention remains required; wiping other users on enroll is no longer correct. |
| Related | [P19-offline-operability-foundation](P19-offline-operability-foundation.md), [P19-personal-scope-offline-operability](P19-personal-scope-offline-operability.md) |

## Symptom

After Personal/POS online login, user set an offline PIN, signed out, then signed in again as the **same** account → forced `/offline-pin-setup` every time.

## Root cause

`EnsurePinBelongsToUserAsync` (on every online grant establish) **cleared** any PIN verifier whose `UserId` was unbound (`null`) or failed to deserialize. That wiped a valid same-user PIN and forced re-enrollment.

## Fix

| Behavior | Rule |
|---|---|
| Same user re-login | Keep existing PIN; bind legacy/unbound/`Guid.Empty` verifiers to the signed-in user |
| Different user on same device | Clear prior PIN and require enrollment (one device-scoped verifier) |
| JSON load | Case-insensitive deserialize + explicit `userId` property name so SecureStorage round-trips survive |
| Sign out | Still keeps durable grant + PIN for cold-start unlock (unchanged) |

**Account switch is expected:** Mica PIN → Paul login clears Mica’s PIN → Paul enrolls → Mica login again must re-enroll. Same-account logout/login must **not** re-prompt.

> **Update (2026-08-13):** Shared POS terminals now use **per-user** grant/PIN slots. See [P19-multi-user-offline-cashier-pin](P19-multi-user-offline-cashier-pin.md). Enrolling Paul must not delete Mica.

## Validation

| Check | Result |
|---|---|
| `AuthOfflineUxLayerTests` + logout/relogin Auth tests | Passed (25 focused) |
| Emulator install of fixed MAUI | Owner retest: same-user logout/login OK; cross-user re-enroll expected |

## Explicit exclusions

- No per-user multi-PIN map on one device (still one verifier key)
- Device Verified / Phase 19 closeout unchanged
