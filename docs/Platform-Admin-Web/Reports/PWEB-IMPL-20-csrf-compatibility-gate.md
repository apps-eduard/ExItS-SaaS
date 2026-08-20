# PWEB-IMPL-20 — CSRF Compatibility Gate

**Status:** COMPLETE  
**Branch:** `feat/platform-admin-web-v2`  
**Starting SHA:** `96a3acdf87a8e0f244237d6fc98354b2ac1b7684`

PWEB-19 and PWEB-20 were not reopened. This gate only closed remaining compatibility checkpoints before PWEB-21.

## CSRF model (unchanged)

| Item | Value |
| --- | --- |
| Bootstrap | `GET /api/v1/platform/antiforgery/token` |
| Header | `X-XSRF-TOKEN` |
| Session cookie | `.ExItS.Platform.Auth` |
| Antiforgery cookie | `.ExItS.Platform.Antiforgery` |
| React token storage | in-memory only |
| Protected methods | POST, PUT, PATCH, DELETE when session cookie is present without `X-ExItS-Session-Token` |
| GET/HEAD | unaffected |
| Session-header callers | exempt |

## POS React CSRF

**Result:** `FIX_REQUIRED_AND_COMPLETED`

There is no POS React application in this repository. POS clients use header/Bearer authentication (`X-ExItS-Session-Token` / `Authorization`), not the Platform Admin browser cookie as the intended credential.

Concrete PWEB-20 incompatibility: default `HttpClient` cookie jars store `.ExItS.Platform.Auth` from login `Set-Cookie`. `PlatformSessionHeaderHandler` omits the session header on `/auth/introspect`, `/auth/token/bind`, and `/auth/token/revoke`, matching the browser antiforgery enforcement path.

**Fix:** disable cookie jars on Platform-facing POS HttpClients (`UseCookies = false`). No antiforgery bootstrap was added to POS; header/Bearer remains the POS auth path.

## PLM / PWA CSRF

**Result:** `NO_CHANGE_REQUIRED`

PLM in this worktree is documentation-only (`src/Products/PinoyLoanManager/Docs`). No PWA/service worker exists. No PLM client to patch.

Future PLM React/PWA work on other branches that uses `credentials: "include"` against Platform cookie mutations must adopt the PWEB-20 bootstrap + `X-XSRF-TOKEN` contract before those mutations will succeed.

## Social-auth URL blocker

**Result:** `NOT_RELEVANT_TO_PWEB21`

`ExternalAuthEndpoints` still appends `sessionToken=` to `returnUrl` (`BLOCKS_CUTOVER` for Google/Facebook complete). React Admin omits social buttons. Password + HttpOnly cookie login is the current path. Not a prerequisite for PWEB-21 unless a later package ships social login or cutover of that flow.

## PWEB-21 scope search

Repository search for `PWEB-21` / `PWEB-IMPL-21` returned **zero** hits. Highest committed package report is PWEB-IMPL-20. No canonical PWEB-21 definition exists in plans, ADRs, reports, tests, or TODOs.

```
PWEB21_STATUS=BLOCKED
PWEB21_BLOCKER=CANONICAL_SCOPE_NOT_FOUND
```
