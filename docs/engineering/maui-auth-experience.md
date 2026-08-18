# MAUI rounded authentication experience

[Home](../index.md) | [P18-WP01](../reports/P18-WP01-mobile-foundation-and-authentication.md) | [P19 offline PIN](../reports/P19-offline-operability-foundation.md)

**Status:** Code complete / test guarded. **Not Device Verified.** **Not Production Ready.** Backend and security semantics are unchanged.

## Visual shell

Unauthenticated MAUI auth screens (`/signin`, `/register`, `/forgot-password`, `/activate`) and post-login PIN enrollment (`/offline-pin-setup`, `/setup-pin`) share `AuthExperience`:

- Green hero using current POS `--exits-brand` (`#1f6b45`)
- Centered brand copy: **EXPERT IT SOLUTIONS** / **Pinoy Business POS** (localized keys `SignIn_BrandTitle` / `SignIn_BrandSubtitle`)
- CSS-only translucent circles (no stock images)
- Overlapping white card (~24px radius)
- Auth-scoped rounded inputs/buttons (~16px, ~52px height) so global POS `TextInput` / `Button` styling is not changed
- Unauthenticated `AuthShell` does not render the Brand `StoreHeader` (“ES / ExItS POS” top bar). Authenticated first-time-setup pages on `AuthShell` still use `StoreHeader`.

## Routes and tabs

| Route | Active tab | Notes |
|---|---|---|
| `/signin` | Sign In | Existing password sign-in, remember-me, providers, return route |
| `/register` | Sign Up | Existing personal display-name + email registration and activation handoff |
| `/forgot-password` | none | Same visual shell; existing reset request |
| `/activate` | none | Same visual shell; existing token + password activation |
| `/offline-pin-setup` (`/setup-pin` alias) | none | Mandatory rounded PIN enrollment after online login when setup is incomplete |

Tab switching navigates between `/signin` and `/register`. Deep links and navigation guards are unchanged.

Sign Up does **not** invent phone/password registration. Personal registration remains display name + email, then activation.

## PIN on Sign In

PIN is a **trusted device-local sign-in method** for previously enrolled users, online and offline. It is not a second PIN system. Do not call it “offline PIN” in normal online UX; the accessible name is **Sign in with PIN**.

- Facebook and Google stay circular placeholders (unchanged).
- When this device has ≥1 **complete** eligible PIN identity (enrolled user + PIN verifier + valid matching device-bound grant, unexpired/non-revoked), a matching round **keypad** button appears immediately beside Google.
- If no eligible identity exists, the keypad is hidden. Visibility does not depend on typed/remembered username or online/offline state.
- Helper copy when the keypad is shown: **Tap the keypad button beside Google to sign in with your PIN.** (`SignIn_PinKeypadHint`, EN + fil-PH)
- One eligible identity → `/offline-pin` PIN entry. Multiple → existing account chooser, then PIN.
- Slow-login still offers **Use PIN instead** when eligible. The compact Remember-row **Use PIN** text link was removed so the keypad is the primary action.
- First-time / incomplete setup never shows a PIN field and never says **Invalid PIN** / **Incorrect PIN**. That copy is only after an enrolled verifier exists and the entered PIN is wrong.
- `SignIn_OfflineNoPinMessage`: internet is required to sign in once and set up PIN when no eligible identity exists.

## Canonical PIN meaning

| Concept | Meaning |
|---|---|
| PIN | Fast device-local identity verification |
| Server | Authoritative revalidation when reachable |
| Offline grant | Bounded permission when the server is unavailable |
| Sync | Resumes after successful online recovery (`IOfflineReconnectAutoSync`) |
| Lock | Switch local identities without logout; sessions are not reused across users |

## PIN then server recovery

After a correct PIN:

1. Local PIN + device + grant are verified (wrong PIN never contacts the server; existing lockout applies).
2. If the server is unreachable, or no recoverable Platform session handle exists for **that** user → **LocalOffline** (existing offline restrictions; no fabricated server credentials).
3. If the server is reachable, a short **Revalidating** state (`pin_revalidating`) runs before privileged online UI:
   - **ValidatedOnline** — replace local-only state with a normal online session for the **same** selected user; existing sync/recovery runs.
   - **TransientUnavailable** — timeout / DNS / 5xx / transport; keep the local/offline session; **do not** revoke the grant; retry later through existing restore/reconnect.
   - **ExplicitlyRevoked** — only existing authoritative reasons (user/device/product/org assignment revoked); invalidate that user's local authorization; return to sign-in. Generic network failure is never revocation.

Online recovery uses `GrantType: session` with a per-user Platform session handle (`pos.pin.recovery.session.{userId}`). PIN and passwords are never stored or sent to the server. Logout clears the active app session and bearer; it does **not** call Platform session logout, so PIN on this device can reissue an AccessToken until the server session expires or is explicitly revoked.

Lock keeps User A's tokens in the active slot. Unlocking User B never copies A's AccessToken / PlatformSessionToken. Pending outbox rows keep the original `user_id`; switching users does not rewrite creator/audit ownership.

## Online login → PIN onboarding

After any successful MAUI online auth (password, Platform session/profile, Development/Local Validation GUID where allowed):

1. Existing device bind + `EnsureOfflineOperateGrantAsync` runs
2. `EvaluateCurrentUserOfflinePinReadinessAsync` checks **this signed-in user** (not remembered username / typed email / in-memory session alone)
3. **Ready** (enrolled identity + valid device-bound grant + matching `pos.device.id` + unexpired/non-revoked + PIN verifier) → existing destination / return route
4. **Missing setup** (no verifier, no grant, or no stored identity) → `/offline-pin-setup` before dashboard/home
5. Setup save ensures bind + grant + verifier, re-evaluates, and continues only when complete. Persistence failure stays on setup with a safe error. No plaintext PIN.

Already-configured users are not prompted again. Return routes wait until setup completes. Organization POS still registers the device before PIN. Organization essentials without POS are not forced through PIN.

```
ONLINE LOGIN SUCCESS
        ↓
Check complete PIN setup
   ┌────┴────┐
 Ready      Missing
   │          │
 Home      Setup PIN
              │
              ↓
             Home

OFFLINE APP START / SIGN IN
        ↓
Check persisted eligibility
   ┌────┴────┐
 Eligible   Not eligible
   │           │
 Use PIN    Internet required
   │
 Verify PIN
   ├─ correct → recover online if reachable, else LocalOffline
   └─ wrong   → Incorrect PIN / lockout
```

Forgot PIN while offline cannot be reset locally. Reconnect and authenticate to change PIN in Settings (`/offline-pin-setup?mode=change`). Personal and Organization POS use the same grant/verifier lifecycle. Orphan grant-without-PIN requires setup when online and does not offer PIN offline. Orphan PIN-without-grant is not eligible. Expired / revoked / device mismatch fail closed.

Device Verified remains **No** unless physically tested.

## Development test user

When `IsDevelopmentAuthenticationEnabled` **and** Local Validation quick-login is available:

- Compact `<select>` is rendered **below** the white card (`BelowCard` / `pos-auth-page__below`)
- No “Development access” heading, no explanatory paragraph, no list of large buttons
- Selection still fills username only and clears password (`OnTestUserSelected`)
- Production still cannot expose the selector (same environment + Local Validation guards)

## Social providers

Circular Facebook / Google actions remain **placeholders**. Accessible names: “Continue with Facebook”, “Continue with Google”. Real OAuth is not implied and is not implemented. When PIN is eligible, a matching round keypad (**Sign in with PIN**) sits immediately beside Google.

## Honesty

Device Verified: **No**. Production Ready: **No**.
