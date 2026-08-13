# P25-WP03 — Unified Web Authentication, SSO and Workspace Routing

## 1. Assignment

| Field | Value |
|---|---|
| Phase | 25 |
| Work package | P25-WP03 Unified Web Authentication, SSO and Workspace Routing |
| Status | Code Complete / Ready for Owner Validation |
| Branch | `main` |
| Date | 2026-08-13 |
| Device Verified | **No** |
| Production Ready | **No** |

## 2. Authentication topology

```text
User
  → canonical sign-in  Platform Admin /admin/login  (:8090)
  → Platform session token (existing Platform auth)
  → workspace list     GET /api/v1/platform/auth/workspaces
  → one-time handoff   POST /api/v1/platform/auth/web-handoff
  → target host        GET {origin}/session/establish?ticket=…
  → redeem             POST /api/v1/platform/auth/web-handoff/redeem
  → app cookie         .ExItS.Admin.Auth | .ExItS.OrgWeb.Auth | .ExItS.PersonalWeb.Auth
```

Authenticate **once**. Authorize **per app / account scope** (ADR-016, ADR-017). Switching AccountClass uses existing `SelectAccountProfileSession` (revokes the previous session).

## 3. Canonical sign-in

One browser login: Admin `/admin/login` (credentials + Local Validation picker). Org Web and Personal Web `/login` pages only CTA to that URL with `returnApp` + `returnPath`. Anonymous visits to Org/Personal redirect there with a sanitized return path.

## 4. App-specific sessions

| Host | Cookie |
|---|---|
| Platform Admin | `.ExItS.Admin.Auth` / session |
| Organization Web | `.ExItS.OrgWeb.Auth` / session |
| Personal Web | `.ExItS.PersonalWeb.Auth` / session |

HttpOnly, SameSite=Lax, SecurePolicy Always in non-dev (SameAsRequest on local test hosts). CSRF antiforgery remains on Blazor forms. Login credential POST is a full HTTP round-trip (same pattern as existing Admin login).

Platform Admin session ≠ Organization authorization ≠ Personal authorization.

## 5. Workspace discovery / switching

`ListWebWorkspaces` uses account profiles + Platform roles + **active memberships** (not display name/email text).

- One workspace → auto-route.
- Multiple → `/admin/workspaces`.
- None → empty chooser (safe state).

Workspace switcher: Admin `/admin/handoff/{app}`, Org `/handoff/{app}`, Personal `/handoff/{app}`. Client-selected `organizationId` is validated against memberships before ticket issue.

## 6. Handoff mechanism

- 32 random bytes, URL-safe; **SHA-256 hash** stored; plaintext ticket returned once.
- TTL **60 seconds**; `TakeAsync` is single-use (missing and replay share `web_handoff_replay` to avoid oracles).
- Store: `MemoryWebHandoffTicketStore` (no migration, no Redis). Multi-instance production needs a later shared store.
- Ticket may appear in the establish **query string** because it is short-lived and one-time. Passwords and long-lived bearer tokens never go in URLs.

## 7. Return URL protection

`SafeReturnPath` / `WebHandoffReturnPath`: relative path starting with `/`, reject `//`, `://`, `\`, non-relative values. Fallback: `/admin`, `/overview`, or `/home`.

## 8. Local Validation quick login

**One** picker on canonical login. Entries are **current Local Validation database accounts** (MODEL A), not a static dump of every catalog name. Canonical baseline identities are labeled `Baseline ·`. Full-catalog demo users appear only after an explicit `-SeedScope Full` seed. See [P25-WP04](P25-WP04-web-host-legacy-cleanup-and-local-validation-identity-determinism.md). Selecting an identity:

1. Server-side credential login with `LOCAL_VALIDATION_SHARED_PASSWORD` (never sent to the browser).
2. Select account profile + organization context from seed metadata.
3. Route by AccountClass: Platform → 8090, Organization → 8093, Personal → 8094.

`GET /admin/login/as/{key}` returns **404 in Production**. Unavailable when Local Validation is disabled.

## 9. Logout

Each host `/logout` signs out **that app cookie** and revokes the Platform session via the existing session service, then redirects to canonical login.

**Defined behavior:** sign-out of the current app **and** revoke the central Platform session. Other host cookies become useless on the next API call (401 → login). Visiting every origin to delete cookies is not required for security once the session is revoked. There is no separate “this app only” mode in this WP.

## 10. Session expiry

Expired/invalid session → one redirect to canonical login with sanitized return path. Handoff redeem of missing/expired tickets does not retry. No infinite ticket exchange.

Membership revocation: product APIs remain authority; Org Web hydration fails closed (no organization → no management body).

## 11. Direct URL behavior

| State | Result |
|---|---|
| Anonymous Org/Personal/Admin | Canonical login, safe return preserved |
| Authenticated + authorized | Establish/use app session |
| Authenticated + unauthorized | Workspace chooser / access denied (no empty misleading dashboard) |
| Unsafe return URL | Ignored |

## 12. Security threats / mitigations

| Threat | Mitigation |
|---|---|
| Personal user opens Org Web | No Organization workspace; handoff unauthorized; Org shell requires membership |
| Linked customer ≠ staff | Unchanged identity model; workspaces from memberships only |
| Org owner ≠ Platform admin | Platform workspace requires Platform role |
| Wrong OrganizationId | Membership check on create handoff + API tenant headers |
| Replay / expire / tamper ticket | Hash store, Take-once, TTL, SHA mismatch = replay |
| Open redirect | Relative-path sanitizer |
| Quick login in Production | NotFound |
| Credentials in URL | Forbidden; only one-time ticket |

HTTPS production assumptions: reverse proxy terminates TLS; app cookies Secure in non-dev; internal ports not internet-facing.

## 13. Tests

Handoff redeem once/replay/expire/tamper/invalid; open-redirect matrix; architecture: Production NotFound on quick login, identity AccountClass routing, no second picker on Org/Personal, ticket URL has no password.

## 14. Owner browser checklist

**Platform:** Quick login Olivia/Rafael → 8090; Platform-only nav; AntDesign theme.

**Organization:** Org identity → 8093; no second login; correct org; AntDesign shell; products, inventory, transfers, expiration, customers, staff, branches, devices, shifts, reports, cash-count settings; **NO checkout**.

**Personal:** Personal identity → 8094; no second login; contacts, utang, invitations, notifications, profile/settings.

**Workspace:** Personal+Org user switch without password; unauthorized workspaces hidden; multi-org selector validates membership.

**Security:** Personal-only → 8093 denied/rerouted; Org user → Platform denied unless role; wrong org denied; sign out; stale session unusable.

**Responsive:** desktop / tablet / mobile browser width.

Owner controls acceptance. **Do not mark Device Verified.**

## 15. Git

Starting SHA: `9a3be47879dc89cf392ae3a0ef84d209cc52e2ef`

| Commit | Message |
|---|---|
| `9f4be5b` | feat(auth): add unified web workspace routing |
| `4fdddfe5` | test(web): cover web host and SSO boundaries |
| `5be25973` | docs(p25): document web host separation and unified authentication |
