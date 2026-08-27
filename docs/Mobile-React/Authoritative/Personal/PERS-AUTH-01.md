# PERS-AUTH-01 — Personal Account Activation + Password Reset Completion

**Package:** PERS-AUTH-01  
**Status:** COMPLETE  
**Branch:** `feat/personal`  
**Baseline:** `c3ba903fed3a083a4f55006324f04e0bad0e2f71`  
**Implementation SHA:** _(filled after push)_  

## Implemented routes (POS / Personal React)

| Route | Page | Guard |
| --- | --- | --- |
| `/activate-account?token=…` | `ActivateAccountPage` | Public (not GuestOnly) |
| `/reset-password?token=…` | `ResetPasswordPage` | Public (not GuestOnly) |

Existing `/sign-in` and `/forgot-password` remain unchanged except:

- register / forgot-password now send `publicSurface: "pinoy-business-pos"`
- Sign In shows success notices after activation / reset

## Platform endpoints used

| Flow | Endpoint | Body |
| --- | --- | --- |
| Activate | `POST /api/v1/platform/auth/activate-account` | `{ token, password }` |
| Reset | `POST /api/v1/platform/auth/reset-password` | `{ token, newPassword }` |
| Register | `POST /api/v1/platform/auth/register` | `{ displayName, email, publicSurface }` |
| Forgot | `POST /api/v1/platform/auth/forgot-password` | `{ usernameOrEmail, publicSurface }` |

No duplicate backend auth was invented.

## Public surface / email callbacks

New allow-listed surface: `pinoy-business-pos`.

Config:

- `PlatformEmail:PinoyBusinessPosPublicBaseUrl`
- Local Validation: `http://localhost:5177` (React POS Vite)

Generated links:

- `{POS_BASE}/activate-account?token=…`
- `{POS_BASE}/reset-password?token=…`

Admin (`null` surface) and PLM (`pinoy-loan-manager`) URLs are unchanged. Arbitrary callback URLs from the browser remain rejected.

## Token handling

PLM-proven pattern, local duplicate (no PLM import):

1. Capture `?token=` into a React ref on first render  
2. Scrub address bar via `history.replaceState`  
3. Keep token in memory only for the form lifecycle  
4. POST only to the intended Platform auth endpoint  

Not persisted in localStorage, sessionStorage, IndexedDB, offline DB, outbox, logs, or diagnostics.

Auth mutations are NETWORK ONLY (no offline enqueue / Background Sync).

Referrer-Policy remains `no-referrer` (POS `index.html` + Platform/POS security pipelines).

## Lifecycles

**Activation:** Register → email → open link → set password → Activate → Sign In notice.

**Reset:** Forgot (enumeration-safe ack) → email → open link → new password → Reset → Sign In notice.

Default: do **not** auto-sign-in after reset/activation.

## Error behavior

| Case | UX |
| --- | --- |
| Missing token | Invalid-link state + Back to Sign In |
| Expired token | Safe “link expired” copy |
| Invalid / already-used token | Safe “link no longer valid” copy |
| Validation / mismatch | Field errors |
| Network / unexpected | Generic failure without token dump |

## Mailpit / Local Validation

Configured so POS registration/forgot emails point at React POS `:5177` when Local Validation is started.

**MAILPIT_ACTIVATION_FLOW:** NOT_APPLICABLE in this agent run (no live LV stack exercised end-to-end).  
**MAILPIT_RESET_FLOW:** NOT_APPLICABLE (same).  

Config + packaging assertions cover the email origin wiring; live Mailpit click-through remains operator validation when LV is running.

## Tests

- React: `account-lifecycle.test.tsx` (capture, scrub, success, expired, double-submit, mismatch, storage none)
- Platform unit: `PlatformAuthCallbackResolverTests` (+ POS surface)
- Platform integration: `ApiAuthPublicSurfaceTests` (+ POS surface)
- Architecture: Local Validation packaging asserts `PinoyBusinessPosPublicBaseUrl`

## Remaining gaps (unchanged)

### P0

- People offline contact UI still unwired

### P1 / P2 / P3

- Settlement wizard, ownership-transfer UI, diagnostics page, durable cart, Todo share, external-camera QR / Install ExItS / advanced payment QR (deferred)
