# PWEB-IMPL-04C — Parallel React Local-Validation Container

**Status:** COMPLETE  
**Branch:** `feat/platform-admin-web-v2`  
**Predecessor:** PWEB-IMPL-04B-A (`ba721168ef3b6c8d05b0a3f6f412fa002b7b0102`)

## Delivered

- Production-style React Admin image (`deploy/docker/Dockerfile.platform-admin-web`, nginx)
- Compose service `admin-web-react` on host port **8095**
- Existing Blazor Admin remains on **8090**; Platform API remains on **8091**
- SPA fallback for `/admin/*`
- Runtime `PLATFORM_API_PUBLIC_URL` injected into `/config.js` (no baked machine-specific URL)
- CORS allowlist extended with the React origin only (`AllowCredentials` unchanged)

## Validation (this worktree)

| Check | Result |
|---|---|
| Frontend typecheck / lint / format | PASS |
| Unit tests | 107 PASS |
| `npm run build` | PASS |
| Existing Playwright | 19 PASS |
| Docker image build | PASS (`exits/platform-admin-web:pweb-04c`) |
| Isolated container on 8095 | PASS (`/health`, `/admin`, `/admin/organizations`, `/config.js`) |
| Playwright container smoke | 1 PASS |
| Existing 8090 Blazor Admin | Reachable HTTP 200 `/admin/login` on this machine |
| Existing 8091 Platform API | Reachable HTTP 200 `/health` on this machine |
| Full `Start-DockerLocalValidation.ps1` | **NOT RUN** — `deploy/docker/.env.local-validation` is not present in this worktree |
| Cookie login from 8095 against live API | **NOT RE-PROVED** after image smoke (container stopped; host API CORS updates apply on next launcher start) |

Container logs showed nginx start and HTTP 200 for `/admin` and `/admin/organizations`. No runtime error after the LF entrypoint fix.

## Explicitly not claimed

- Dashboard
- Feature screens
- Cutover from Blazor Admin
- Logout redesign
- Backend/DB/POS/PLM capability changes
- Wildcard CORS or cookie SameSite weakening
