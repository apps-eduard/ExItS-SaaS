# ExItS Docker packaging + live preview (P14-WP02 / P14-WP02A)

## Layout

| File | Purpose |
|---|---|
| `compose.yaml` | **Default** packaging baseline (Platform API + POS API + separate PostgreSQL). Local deployment testing. **Not Production-ready.** |
| `compose.live-preview.yaml` | Live-preview project `exits-live-preview`. **Default `up` = DBs only**; app services behind profile `apps`. |
| `README.live-preview.md` | Authoritative daily workflow: Docker DBs + local .NET APIs/Admin |
| `Start-LivePreviewLocal.ps1` | Bring up DBs (if needed) and start local Platform/POS/Admin |
| `Stop-LivePreviewLocal.ps1` | Stop DBs without deleting volumes |
| `docker-compose.pilot.yml` | Controlled **pilot** stack with Admin + nginx TLS template (NON-PRODUCTION) |
| `Dockerfile.platform-api` | Platform API image |
| `Dockerfile.pos-api` | PinoyBusinessPOS API image |
| `Dockerfile.platform-admin` | Platform Admin image |
| `nginx/pilot.conf` | Pilot reverse-proxy template |
| `.env.example` | Packaging baseline placeholders |
| `.env.live-preview.example` | Live-preview placeholders (copy to `.env.live-preview`) |

## Packaging baseline (P14-WP02)

From this directory:

```powershell
Copy-Item .env.example .env
# Edit .env — replace REPLACE_* passwords; never commit .env

docker compose build
docker compose up -d
docker compose ps
docker compose down
```

Default host ports: Platform API **8081**, POS API **8082**, Platform DB **15433**, POS DB **15434**.

Notes:

- Separate `platform-db` and `pos-db` — no shared product database.
- `ASPNETCORE_ENVIRONMENT=Staging` — Dev/Testing identity headers unavailable.
- Apps do **not** auto-migrate. Apply migrations with approved ops tooling against published DB ports before expecting full readiness.
- TLS / reverse proxy are **not** included here (**P14-WP03**).
- Portfolio remains **not Production-ready**.

## Live preview (P14-WP02A) — recommended daily workflow

**Docker: databases only. Host: Platform API + POS API + Admin.**

```powershell
# from repository root
.\tools\Start-LivePreviewLocal.ps1
```

Full guide: [`README.live-preview-local-development.md`](README.live-preview-local-development.md) · overview: [`README.live-preview.md`](README.live-preview.md).

```powershell
Copy-Item deploy\docker\.env.live-preview.example deploy\docker\.env.live-preview
# Edit — replace REPLACE_* passwords; never commit
```

| Surface | Default | Open |
|---|---|---|
| **Admin (local)** | **8090** | http://localhost:8090/ |
| Platform API (local) | 8091 | http://localhost:8091/health |
| POS API (local) | 8092 | http://localhost:8092/health |
| platform-db (Docker) | 15533 | Postgres volume preserved |
| pos-db (Docker) | 15534 | Postgres volume preserved |

Optional containerized apps: `--profile apps`. Do **not** reuse packaging ports. **Not** Production-secure. Do **not** treat as P14-WP03.

## Pilot stack (P9-WP05)

```powershell
docker compose -f docker-compose.pilot.yml --env-file <your-pilot-env> up -d
```

See `ops/deploy/README.md` and `docs/operations/pilot-and-deployment/`.

## Related docs

- [Live preview workflow](README.live-preview.md)
- [Production deployment architecture](../../docs/engineering/production-deployment-architecture.md)
- [Production readiness audit](../../docs/engineering/production-readiness-audit.md)
- [P14-WP02 report](../../docs/reports/P14-WP02-production-packaging-and-compose-baseline.md)
- [P14-WP02 live-preview gap fix](../../docs/reports/P14-WP02-gap-fix-separate-live-preview-stack.md)
- [P14-WP02A quick login](../../docs/reports/P14-WP02A-live-preview-test-users-and-quick-login.md)
