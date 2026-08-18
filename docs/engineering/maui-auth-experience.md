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

## Offline PIN on Sign In

- No large offline information panel
- No full-width **Use PIN** / **Continue offline** button in the auth card
- Compact **Use PIN** text link in the Remember / Forgot row when a persisted offline grant+PIN is eligible **and** the OS reports no network interface (or a sign-in attempt just failed as unreachable)
- Debug Local Validation may still treat `NetworkAccess.None` as connected for password/API attempts; PIN offer uses `HasNoNetworkInterfaceAsync` so airplane mode still shows **Use PIN**
- Online with a working interface: PIN link hidden
- Slow-login prompt still offers **Use PIN instead** when eligible
- Tapping PIN still navigates to `/offline-pin`
- Failed/unreachable login uses existing error copy only after a real Sign in attempt (`SignIn_ServerUnreachablePinHint` / `SignIn_OfflineNoPinMessage`)
- First-time / incomplete offline setup never shows a PIN field, never navigates to PIN unlock, and never says **Invalid PIN** / **Incorrect PIN**. That copy is only after an enrolled verifier exists and the entered PIN is wrong.
- `SignIn_OfflineNoPinMessage`: internet is required to sign in once and set up offline access
- Eligibility is persisted grant + PIN verifier + matching device id (see [P19 offline PIN](../reports/P19-offline-operability-foundation.md)). Local Validation uses that same architecture; it does not fake authorization.

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
   ├─ correct → offline app
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

Circular Facebook / Google actions remain **placeholders**. Accessible names: “Continue with Facebook”, “Continue with Google”. Real OAuth is not implied and is not implemented.

## Honesty

Device Verified: **No**. Production Ready: **No**.
