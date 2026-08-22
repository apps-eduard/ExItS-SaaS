# PLATFORM-WEB-PA-COM-07 — Platform → POS Commercial Enforcement Integration

## Summary

PA-COM-07 proves the joined commercial spine using **real HTTP** across mixed worktrees:

`React Platform Admin → Platform subscription/plan → entitlement/introspection → POS API enforcement`

| Item | Value |
|---|---|
| Starting HEAD | `18db549af98b86a3971897c9d2b5a60cc3d6f065` |
| Branch | `feat/platform-admin-pa-com-07` |
| POS validation SHA | `7e8256b2aa6ae1e44e615a272939a7a796aeb89e` (`feat/pos-react-client`) |

Bootstrap uses Platform Testing external auth (`POST /auth/external/testing/complete`) to avoid auth/register rate limits; disposable orgs are created via `POST /api/v1/personal/start-business` on Growth trial.

POS financial mutations in Staging require registered device headers (`X-Pos-Installation-Device-Id`, `X-Pos-Branch-Id`) and an open shift id on checkout — the joined scenario registers devices through Platform first, then exercises sale checkout server-side.

## Runtime provenance (mixed worktree)

| Runtime | Worktree / branch | Port |
|---|---|---|
| React Platform Admin | Agent 2 `feat/platform-admin-pa-com-07` | **8095** |
| Platform API | Agent 2 `feat/platform-admin-pa-com-07` | **8091** |
| POS API | Agent 1 `feat/pos-react-client` @ `7e8256b2…` | **8092** |

Launcher: `tools/Start-PaCom07MixedValidation.ps1`  
Provenance file: `%LOCALAPPDATA%\ExItS\LocalValidation\pa-com-07-provenance.json`

### Strict commercial mode

| Flag | Value |
|---|---|
| `STRICT_COMMERCIAL_VALIDATION` | **ON** (`CommercialValidation:Strict=true` on POS API) |
| `DEVELOPMENT_GRANT_MERGE` | **OFF** (strict disables `ShouldMergeDevelopmentGrants`) |

## Authoritative plan contracts (seeded MVP)

| Plan | `maxActivePosDevices` | Entitlement quantity code |
|---|---|---|
| Growth | **3** | `plan-max-active-pos-devices` |
| Pro | **10** | `plan-max-active-pos-devices` |

Device capacity enforcement is **server-side** on Platform:

- `GET /api/v1/platform/organizations/{orgId}/pos-devices/capacity`
- `POST /api/v1/platform/organizations/{orgId}/pos-devices/register` → `application.pos_device.capacity_exceeded`

POS commercial enforcement uses Platform introspection (`POST /api/v1/platform/auth/introspect`) via POS bearer middleware.

## Joined scenario (Playwright + API)

File: `src/Platform/ExItS.Platform.Admin.Web/e2e/platform-pos-commercial-joined.spec.ts`

1. Bootstrap disposable org on **Growth** trial (Platform API — not mocked commercial enforcement)
2. React Admin: open org subscriptions (Growth visible)
3. Register devices 1–3 allowed; device 4 denied (`capacity_exceeded`)
4. React Admin: **Change plan → Pro → Upgrade plan**
5. Device 4 allowed after capacity refresh
6. React Admin: **Suspend subscription** → introspection `Suspended`; POS catalog + sale **403**
7. React Admin: **Reactivate subscription** → POS catalog + sale restored

Run:

```powershell
.\tools\Start-PaCom07MixedValidation.ps1
.\tools\Invoke-PaCom07JoinedIntegration.ps1
```

## Validation evidence (2026-08-22)

| Gate | Result |
|---|---|
| `npm test` (Vitest) | 359 passed |
| `npm run typecheck` | PASS |
| `npm run lint` | PASS |
| `npm run build` | PASS |
| PA-COM Playwright regression (4 specs) | 20 passed |
| Joined integration (`Invoke-PaCom07JoinedIntegration.ps1`) | 2 passed |

## Agent conflict checks

| Agent | Merged | Modified |
|---|---|---|
| Agent 1 POS branch | **NO** | POS runs external worktree only |
| Agent 3 Global Catalog | **NO** | No global-catalog paths touched |

## Explicit exclusions

- No PA-COM-08 matrix run
- No POS React UI changes
- No backend API invention
- No merge to `main`

## Known gaps

- Platform API on Agent 2 worktree may lack Agent 1 introspection role-preservation fix (`AccessTokenUseCases`); disposable org bootstrap uses owner `CanOperate` path to avoid org-management-only tokens.
- React Admin container on 8095 is built from current worktree Docker context; rebuild may be required after Admin changes.
