# PWEB-IMPL-20 — CSRF-Safe Browser Mutation Foundation

**Status:** COMPLETE

**Branch:** `feat/platform-admin-web-v2`

**Starting SHA (PWEB-19):** `8cbd8ff0fe35bf3bd567423fc2b68ff53a5e6227`

**Message:** `feat(platform-web): add csrf-safe mutation foundation`

## Prior CSRF state

`BLOCKS_FUTURE_MUTATION` — Platform API had no browser antiforgery enforcement; React mutations used `fetch` with `credentials: "include"` only.

## Final architecture

| Item | Value |
| --- | --- |
| Bootstrap | `GET /api/v1/platform/antiforgery/token` |
| Header | `X-XSRF-TOKEN` |
| Antiforgery cookie | `.ExItS.Platform.Antiforgery` (HttpOnly, SameSite=Lax) |
| Session cookie | `.ExItS.Platform.Auth` (unchanged HttpOnly semantics) |
| Protected methods | POST, PUT, PATCH, DELETE when session cookie present without `X-ExItS-Session-Token` |
| GET/HEAD/OPTIONS | Unaffected |
| Exempt paths | login, register, forgot/reset password, bootstrap, external callbacks, token bootstrap |

## React centralized client

`platform-http.ts` bootstraps token in memory on first mutation, attaches header, clears on logout. Login uses `skipAntiforgery: true`. No localStorage/sessionStorage/IndexedDB persistence.

## Logout

React sign-out bootstraps antiforgery then `POST /api/v1/platform/auth/logout` with header + cookie.

## Blazor compatibility

Unchanged. Blazor continues `X-ExItS-Session-Token` header path; middleware skips antiforgery when session header is present.

## CORS

Unchanged policy; added PATCH to allowed methods list only.

## Security tests

`ApiBrowserAntiforgeryTests` (7) + `ApiSessionAuthTests` (3) pass.

## Known unrelated T0 baseline

`ApiAuthorizationAuditTests` 5/12 failures unchanged on branch/main.

`ApiOrganizationContextTests` member-add paths return 400 on current branch baseline (pre-existing; not introduced by PWEB-20).

## Compatibility checkpoints

Closed by `PWEB-IMPL-20-csrf-compatibility-gate.md` (post-PWEB-20 gate):

| Flag | Value |
| --- | --- |
| PLM_PWA_CSRF_COMPAT_REVIEW_REQUIRED | NO_CHANGE_REQUIRED (PLM PWA ABSENT in this tree) |
| POS_REACT_CSRF_COMPAT_REVIEW_REQUIRED | FIX_REQUIRED_AND_COMPLETED (no POS React; Platform HttpClient cookie jar isolated) |
| Social-auth URL blocker | OPEN / NOT_RELEVANT_TO_PWEB21 (`BLOCKS_CUTOVER` for social flow only) |

## Scope

| Area | State |
| --- | --- |
| Platform API | NARROW SECURITY CHANGE |
| Platform React Admin | CHANGED |
| Blazor | UNCHANGED |
| DB/migrations | NONE |
| POS | UNCHANGED |
| PLM | UNCHANGED |
| Business mutation UI | NONE |

## Production Ready

**NO**

## Cutover Authorized

**NO**
