# P16-WP04 — Personal Account Foundation

| Field | Value |
|---|---|
| Status | **Complete** |
| Starting commit | `be8fd9084e6117220a27f6498e5ff84dd5dbf62e` (after P16-WP03 tip-hash) |
| Feature commit | `17f53e204243844b86602eaf12369495ffd8db01` |
| Date | 2026-08-02 |

## Scope completed

- Personal dashboard (`GET /api/v1/personal/dashboard`) with account class, `utangAvailable=true`, and zeroed Utang aggregate stubs (populated in WP05).
- Personal profile (`GET /api/v1/personal/profile`) from User Identity (`PlatformUser`) + Personal account profile.
- Personal settings foundation (`GET/PUT /api/v1/personal/settings`) with notification preference booleans and optimistic `version`.
- `PersonalAccountSettings` domain + `platform.personal_account_settings` table (keyed by `user_identity_id`).
- Audit `platform.personal.account_settings.updated` on settings update.
- Scope guard unchanged: Personal endpoints reject Platform/Organization sessions (existing middleware).
- API-first surface under `PersonalEndpoints.cs` (no separate Blazor host).

## Files changed (high level)

- Domain: `PersonalAccountSettings.cs`, concurrency error code, audit action
- Application: `PersonalAccountUseCases.cs`, `IPersonalAccountSettingsRepository`
- Infrastructure: settings record/repository, DbContext, migration `AddPersonalAccountSettings`
- API: expanded `PersonalEndpoints.cs`, Program DI
- Tests: `ApiPersonalAccountTests`, `PersonalAccountSettingsTests`

## Schema and migration changes

- Migration `AddPersonalAccountSettings` adds `platform.personal_account_settings` (FK to `platform_users`, `version` concurrency token).

## API / authorization

| Route | Description |
|---|---|
| `GET /api/v1/personal/dashboard` | Personal session; stub summary (`utangAvailable=true`, zero aggregates) |
| `GET /api/v1/personal/profile` | Personal session; identity + profile |
| `GET /api/v1/personal/settings` | Personal session; auto-creates defaults |
| `PUT /api/v1/personal/settings` | Personal session; optional `expectedVersion` |

Platform and Organization sessions receive `403` + `application.auth.account_scope_denied` from existing scope guard.

## Audit coverage

- `platform.personal.account_settings.updated`

## Tests added

- `ApiPersonalAccountTests` — dashboard/profile/settings; Platform session denied on dashboard
- `PersonalAccountSettingsTests` — defaults, version increment, stale-version conflict

## Build / test evidence

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Testing"
dotnet build src/Platform/ExItS.Platform.Api/ExItS.Platform.Api.csproj -c Release
dotnet build src/Platform/ExItS.Platform.Admin/ExItS.Platform.Admin.csproj -c Release
dotnet test tests/ExItS.Platform.UnitTests/ExItS.Platform.UnitTests.csproj -c Release
dotnet test tests/ExItS.Platform.IntegrationTests/ExItS.Platform.IntegrationTests.csproj -c Release
```

- Platform unit: **319 passed**, 0 failed, 0 skipped
- Platform integration: **156 passed**, 0 failed, 0 skipped
- Build: Platform API + Admin Release — 0 warnings, 0 errors

## Explicit exclusions

- No Personal Utang contacts/relationships/entries (P16-WP05)
- No Personal Blazor/mobile UI (API-first per WP04 scope)
- Notification delivery/reminders deferred to P16-WP06
- P16-WP03 Organization API remap unchanged

## Explicit next work package

**P16-WP05** — Personal Utang Core

## Production blockers

Unchanged. Phase 14 not modified. App remains **not production-ready**. Seeds never run in Production.
