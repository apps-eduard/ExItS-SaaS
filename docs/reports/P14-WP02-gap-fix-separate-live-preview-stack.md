# P14-WP02 Gap Fix — Separate ExItS Live Preview Stack

> **Historical / superseded.** This report documents the former separate Live Preview Compose stack (`exits-live-preview`).
> Current operator runtime is **Local Validation** (`exits-local-validation`).
> See [P16-WP11 Local Validation replaces Live Preview](P16-WP11-local-validation-replaces-live-preview.md).
> Filenames are retained for history; do not treat this package as active guidance.

Phase marker: `P14-WP02-gap-fix-separate-live-preview-stack`

Package: **P14-WP02 Gap Fix — Separate ExItS Live Preview Stack**
Prior tip: `69ddba1f1089d37db474a28981be091f053ab20a`
Feature tip: `16342195ff4999f7c0fc99fa15306fc3fa530074`

## Status

**Complete.** Independent Compose project `exits-live-preview` for personal local Admin + API + DB preview. Packaging baseline (`exits-packaging` / `compose.yaml` ports 8081/8082/15433/15434) left unchanged. **Not Production.** **P14-WP03 not started.**

## 1. Delivered capability

| Area | Evidence |
|---|---|
| Compose | `deploy/docker/compose.live-preview.yaml` (`name: exits-live-preview`) |
| Env template | `deploy/docker/.env.live-preview.example` → local `.env.live-preview` (gitignored) |
| Topology | `admin-web`, `platform-api`, `platform-db`, `pos-api`, `pos-db` |
| Isolation | Distinct container names, volumes, network, host ports, env file vs packaging |
| Docs | `deploy/docker/README.md` live-preview section |
| Tests | `LivePreviewPackagingArchitectureTests` |

## 2. Default host ports (vs packaging)

| Service | Live preview | Packaging (unchanged) |
|---|---|---|
| admin-web | **8090** | *(not in packaging)* |
| platform-api | **8091** | 8081 |
| pos-api | **8092** | 8082 |
| platform-db | **15533** | 15433 |
| pos-db | **15534** | 15434 |

Browser entry: `http://localhost:8090/` → `/admin`. Health: `http://localhost:8091/health`, `http://localhost:8092/health`.
Apps use **Development** (personal HTTP preview; Admin HTTPS guard). Packaging remains **Staging**. Not Production-secure.

## 3. Operator commands

```powershell
cd deploy\docker
Copy-Item .env.live-preview.example .env.live-preview
# replace REPLACE_* passwords

docker compose -f compose.live-preview.yaml --env-file .env.live-preview build
docker compose -f compose.live-preview.yaml --env-file .env.live-preview up -d
docker compose -f compose.live-preview.yaml --env-file .env.live-preview ps
docker compose -f compose.live-preview.yaml --env-file .env.live-preview down
```

## 4. Explicit exclusions

- No change to packaging `compose.yaml` / `.env.example` ports or topology
- No reverse proxy / TLS (**P14-WP03**)
- No Production cutover claim; no auto-migrate at startup
- Not a replacement for pilot nginx TLS stack

## 5. Validation

| Check | Result |
|---|---|
| Packaging stack remains up on 8081/8082 during live-preview bring-up | Yes (both projects Up concurrently) |
| `docker compose -f compose.live-preview.yaml ... config/build/up/ps` | Succeeded |
| Admin `:8090` | 302 `/` → 200 `/admin` |
| Platform `/health` `:8091`, POS `/health` `:8092` | 200 Healthy |
| Full Release tests | **1266 passed / 0 failed / 0 skipped** |
| Portfolio independence | No legacy product root / solution projects |

## Exact next work package

**P14-WP03 — Reverse Proxy, TLS, and Network Hardening** when explicitly authorized. Do **not** begin P14-WP03.
