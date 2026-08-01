# P16-WP02 — Account Profiles and Session Isolation

| Field | Value |
|---|---|
| Status | **Complete** |
| Starting commit | `14938f023bac8dce5289bff13556b0a2986b46ba` |
| Feature commit | `f0bb6c9ec87e75e7505087404cad463f931f5a67` |
| Date | 2026-08-02 |

## Scope completed

- Account profile domain (`AccountClass`, `AccountProfile`) with Platform / Personal / Organization classes.
- Scope-bound sessions: `PlatformAuthSession` requires `AccountProfileId` + `AccountClass`; org context only for Organization sessions.
- Login / external login / account-profile select issue sessions bound to an ensured profile.
- Session claims: AccountProfileId, AccountClass, AllowedScope (plus existing identity/session claims).
- API family guard middleware (`AccountScopeGuardMiddleware`) for authenticated PlatformSession actors.
- Personal stub endpoints under `/api/v1/personal/*`.
- Account-profile list/select under `/api/v1/platform/auth/account-profiles*`.
- Dev/Testing/LivePreview Phase 16 identity seed (`InitializePhase16AccountSeed`) — never Production.
- EF migration `AddAccountProfilesAndSessionScope` with Personal profile backfill for existing users/sessions.
- Cross-class denial integration tests; Production seed architecture guard.

## Files changed (high level)

- Domain: `AccountClass.cs`, `AccountProfile.cs`, `PlatformAuthSession.cs`, audit actions
- Application: account-profile use cases, seed, session/external login/org-context updates, error codes
- Infrastructure: records, repos, DbContext, migration, LivePreview hosted seed wire-up
- API: scope guard, personal endpoints, auth profile routes, Program DI
- Tests: unit, integration isolation, architecture seed guard
- Docs: this report; phase-16 + portfolio updates

## Schema and migration changes

- Table `platform.account_profiles` (unique user + account_class).
- Columns on `platform.platform_auth_sessions`: `account_profile_id`, `account_class`.
- Backfill: Personal profile per existing user; existing sessions rebound to Personal; orphan sessions deleted.

## API / authorization / UI changes

- Guard enforces Personal → `/api/v1/personal/*`; Platform → `/api/v1/platform/*` (auth/live-preview exempt); Organization → `/api/v1/organizations/*`, `/api/v1/products/*`, and interim `/api/v1/platform/organizations/*` until WP03 remaps.
- Invitation accept remains exempt (identity-bound before Organization profile selection; WP06 remaps).
- DevelopmentOperator / unauthenticated actors are not scope-classified (legacy test path).

## Seed-data changes

Deterministic non-Production seed (idempotent):

| Identity | Purpose |
|---|---|
| `platform.admin1@exits.test`, `platform.admin2@exits.test` | Platform Administrator |
| `personal.user1@exits.test`, `personal.user2@exits.test` | Personal-only |
| `org.seed.owner@exits.test` + org `phase16-seed-org` | Organization owner |

Password from `LivePreview:SharedPassword` when LivePreview enabled; Testing fallback password in code. Never runs in Production.

## Audit coverage

- `platform.auth.account_profile_selected`
- `platform.auth.account_scope_denied` (error code + middleware denial surface)

## Tests added

- `ApiAccountScopeIsolationTests` (Personal/Platform/Organization denial; foreign profile select denied)
- `AccountProfileTests`, updated session/external-login unit tests
- `Phase16AccountSeedArchitectureTests`

## Focused test results

- Unit (full Platform unit suite): **316 passed**
- Integration Api filter (`ASPNETCORE_ENVIRONMENT=Testing`): **102 passed**
- Architecture Phase16 seed guard: **1 passed**

## Full regression result

Platform Api integration suite under Testing: **102 / 102 passed**. Broader non-Api integration projects not re-run in this WP closeout; focused identity/session/catalog/authz Api coverage included in the 102.

## Issues found and fixed

- Session `Create` call sites and external-login tests updated for profile binding.
- Migration rewritten to create profiles before NOT NULL session columns + backfill.
- Organization context select denied for non-Organization sessions.
- Shell `ASPNETCORE_ENVIRONMENT=Staging` (Live Preview) disables DevelopmentOperator full access for factories that do not force Testing — run Api regression with `Testing` (or set factory environment). Documented as residual risk for local runs.
- Organization sessions temporarily allowed on `/api/v1/platform/organizations/*` until WP03 remaps Organization APIs.

## Residual risks

- Organization APIs still physically under `/api/v1/platform/organizations` (WP03 remaps + switcher UX).
- Personal surface is stub (`/personal/me`, `/personal/health`) until WP04/WP05.
- Support Session (ADR-018) not implemented.
- Live Preview must migrate + re-seed after pull; existing browser sessions rebound to Personal via migration.

## Deferred items

- WP03 Organization context navigation and API family remap
- WP04+ Personal Utang and business upgrade journey
- Support Session

## Production blockers

Unchanged. Phase 14 not modified. App remains **not production-ready**.

## Explicit next authorization

Phase 16 complete-execution mandate authorizes **P16-WP03** next.
