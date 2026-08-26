# PA-INTEGRATION-04 — React POS Docker runtime wiring

**Status:** COMPLETE  
**Branch:** `feat/platform-admin-react-integration`  
**Package:** PA-INTEGRATION-04  

## Objective

Optionally run the approved React POS Docker container (`:5177`) from the **POS worktree** during React Platform Admin Local Validation, without copying or downgrading POS into the Platform integration worktree.

## Source of truth

| Surface | Source | Port |
| --- | --- | --- |
| Platform API | Integration worktree (`feat/platform-admin-react-integration`) | 8091 |
| POS API | POS worktree (`feat/pos-react-client`) | 8092 |
| React Admin | Integration worktree Vite | 8095 |
| React POS (default) | POS worktree `npm run dev` | 5177 |
| React POS (optional) | POS worktree `Dockerfile.pos-react` via compose profile `react-pos` | 5177 |
| MAUI isolated stack | Untouched | 8190–8194 |

Approved React POS Docker implementation:

- Branch: `feat/pos-react-client`
- SHA: `7511987d306ebe3d6e820b8ea293f1857ac0806b` (or newer on that branch; launcher records live POS HEAD)

## Launcher

```powershell
# Default — Vite React POS
.\tools\Start-ReactIntegrationLocalValidation.ps1

# Optional — Docker React POS built from POS worktree
.\tools\Start-ReactIntegrationLocalValidation.ps1 -ReactPosDocker

# Force no-cache rebuild of the React POS image
.\tools\Start-ReactIntegrationLocalValidation.ps1 -ReactPosDocker -ReactPosDockerRebuild
```

`-ReactPosDocker` sets nginx upstreams to `host.docker.internal:8091` / `:8092` so the container proxies `/platform-api` and `/pos-api` to the host-run integration APIs.

## Explicit exclusions

- No merge to `main`
- No POS detach/downgrade
- No React POS image build from Platform integration tree sources
- Default remains Vite unless `-ReactPosDocker` is passed
- MAUI Local Validation ports 8190–8194 unchanged

## Validation

Launcher proved (2026-08-22, `-ReactPosDocker`):

| Check | Result |
| --- | --- |
| LOGIN `POST /platform-api/api/v1/platform/auth/login` | PASS |
| AUTH_ME `GET /platform-api/api/v1/platform/auth/me` | PASS |
| `/platform-api/health` | PASS |
| `/pos-api/health` | PASS |
| POS Docker from POS worktree | YES |
| POS SHA | `7511987d306ebe3d6e820b8ea293f1857ac0806b` |
| Platform API :8091 / POS API :8092 / Admin :8095 / React POS :5177 | PASS |
| MAUI 8190–8194 untouched | YES |

Provenance: `%LOCALAPPDATA%\ExItS\LocalValidation\pa-integration-provenance.json`.
