# MAUI Offline and Platform Session Recovery

Canonical connectivity and authentication decision flow for PinoyBusinessPOS MAUI Platform-backed screens.

## Critical distinctions

| Concept | Meaning |
| --- | --- |
| API health | `/health` or POS Business reachability |
| Authenticated Platform session | Live `PlatformSession` (+ product `AccessToken` where required) |
| Offline | Device network unavailable (or connectivity short-circuit) |
| Session expired | Online Platform auth rejected after one secure renewal attempt |

**API health ≠ authenticated Platform session.**  
A healthy POS Business endpoint (`PosBusinessApi`, local validation `:8092`) does **not** prove Platform Personal/Org calls (`PosApi`, `:8091`) will succeed.

**Offline ≠ session expired.**  
Connectivity loss must never clear `PlatformSession`, force sign-out, or show “Authentication is required.”

## Canonical state machine

1. **ONLINE + AUTHENTICATED** → normal Platform behavior.
2. **OFFLINE** → keep valid local session; do not sign out; continue supported offline features; Platform-only screens explain internet is required.
3. **ONLINE + Platform HTTP 401** → attempt secure AccessToken reissue from Platform session **once** → retry original request **once** → if still unauthorized, show Session expired / Sign in again.
4. **ONLINE + authenticated + service failure** (5xx / timeout / host unreachable while device online) → “Can't connect right now” / Try again. **Not** session expired.

403 remains permission denial — never refresh/login loop.

## Two API bases

| Client | Config | Local validation |
| --- | --- | --- |
| Platform (`IPosApiClient` / `IPlatformAccessClient`) | `PosApi:BaseUrl` | host `:8091` |
| POS Business | `PosBusinessApi:BaseUrl` | host `:8092` |

Same intended host; different ports. Honor `MauiProgram` / Local Validation `PublicHost` — do not hard-code a new host casually.

## Central handler

`PlatformAccessTokenRecoveryHandler` (outermost on Platform `HttpClient`):

- Attaches nothing itself; runs after existing `PlatformSessionHeaderHandler` / `PlatformBearerHandler` on the outbound path via pipeline order.
- On **401** while online: resolves `IPlatformAccessTokenRecovery` **lazily** from `IServiceProvider` (avoids Auth ↔ HttpClient DI cycles that crash MAUI Android at startup), then calls `TryReissueAccessTokenAsync` once.
- On success: retries the original request once (Authorization re-attached from updated `CurrentUserContext`).
- Never loops (`X-ExItS-Platform-Recovery-Attempt`); skips auth bootstrap paths (`/auth/token`, login, logout, introspect, revoke).
- Does **not** clear session on failure.
- Does **not** retry ambiguous transport failures (timeouts / connection resets). Explicit 401 before processing may retry POST/PUT/PATCH/DELETE once after refresh.

Session storage remains `MauiSecureTokenStore` → `SecureSessionStore` (`AccessToken`, `PlatformSessionToken`, expiry metadata). Secrets stay out of Preferences / LocalStore / logs.

## Session renewal

**Supported:** `POST /api/v1/platform/auth/token` with `GrantType: "session"` when a live `PlatformSessionToken` exists (`AuthenticationService.TryReissueAccessTokenAsync`).

**Not supported:** inventing a refresh_token or extending local expiry without Platform acceptance. Revoked sessions must not be resurrected.

Nominal local expiry while **offline** does not force immediate login; stored identity is kept for offline work. When connectivity returns, restore/refresh attempts renewal; failure then shows Session expired.

## UX copy (EN)

| State | Title | Body / action |
| --- | --- | --- |
| Offline (global) | You're offline | You can keep using features that are available offline… |
| Platform-only offline | You're offline | This information needs an internet connection… / Try again |
| Session expired | Your session has expired | For your security, please sign in again… / Sign in again |
| Service unavailable | Can't connect right now | We couldn't reach this service… / Try again |
| Reconnect | Back online | Only claim sync completion when sync infrastructure confirms it |

Filipino (`fil-PH`) equivalents live in `PosResources.fil-PH.resx`.

## Screens

- **Personal Notifications** — offline / session-expired / service-unavailable terminal states; never raw “Authentication is required.”
- **Organization summary** — local/POS identity retained offline; subscription/entitlement rows use offline-specific copy instead of generic “Unavailable.”
- **Header cloud indicator** (`ShellSyncStatus`) — Online / Offline / Syncing; tap opens status panel with offline guidance.

## Intended-route restore

`PostSignInReturnRoute` captures safe internal routes (e.g. `/personal/notifications`, `/org`) before Session expired → Sign in. After successful sign-in, `SignIn` navigates to that route when safe.

## LocalStore

No LocalStore version bump for connectivity state. Session secrets remain in secure storage only.

## PIN sign-in recovery

PIN verifies a previously enrolled identity on this device. When the Platform is reachable, MAUI reissues an AccessToken with `GrantType: session` using that **same user's** stored Platform session handle (`pos.pin.recovery.session.{userId}`). Results are classified:

| Outcome | Behavior |
|---|---|
| ValidatedOnline | Replace local-only state; existing reconnect auto-sync may run |
| TransientUnavailable | Keep LocalOffline grant; do not revoke; retry later |
| ExplicitlyRevoked | Invalidate that user's grant/handle per existing policy; no session continues |
| LocalOffline | Server unreachable, or no recoverable handle for this user |

Wrong PIN never calls Platform. User B never receives User A's AccessToken. Logout revokes the bearer and clears the active session slot; it does **not** destroy the per-user Platform session handle, so PIN can recover online until the server session is expired/revoked. PIN itself is never sent to the server and is not a password replacement.
