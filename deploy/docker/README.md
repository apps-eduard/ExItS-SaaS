# ExItS Docker packaging + live preview (P14-WP02)

## Layout

| File | Purpose |
|---|---|
| `compose.yaml` | **Default** packaging baseline (Platform API + POS API + separate PostgreSQL). Local deployment testing. **Not Production-ready.** |
| `compose.live-preview.yaml` | **Separate** personal live-preview stack (`exits-live-preview`) with Admin + APIs + DBs. Independent ports/volumes/env. **Not Production.** |
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

## Live preview stack (P14-WP02 gap fix)

Independent Compose project for opening Admin in a browser while packaging can stay up. Uses **different** project name, container names, volumes, ports, and env file.

```powershell
Copy-Item .env.live-preview.example .env.live-preview
# Edit .env.live-preview — replace REPLACE_* passwords; never commit

docker compose -f compose.live-preview.yaml --env-file .env.live-preview build
docker compose -f compose.live-preview.yaml --env-file .env.live-preview up -d
docker compose -f compose.live-preview.yaml --env-file .env.live-preview ps
docker compose -f compose.live-preview.yaml --env-file .env.live-preview down
```

| Service | Default host port | Open |
|---|---|---|
| **admin-web** | **8090** | http://localhost:8090/ (redirects to `/admin`) |
| platform-api | 8091 | http://localhost:8091/ and `/health` |
| pos-api | 8092 | http://localhost:8092/health |
| platform-db | 15533 | Postgres (ops tooling) |
| pos-db | 15534 | Postgres (ops tooling) |

Uses `ASPNETCORE_ENVIRONMENT=Staging` with `LivePreview:Enabled=true` so Admin requires login and quick-login test users are seeded. **Not** Production-secure. Do **not** reuse packaging ports. Do **not** treat as P14-WP03.

Open **http://localhost:8090/** → `/admin/login` → **Live Preview Test User** dropdown.

## Pilot stack (P9-WP05)

```powershell
docker compose -f docker-compose.pilot.yml --env-file <your-pilot-env> up -d
```

See `ops/deploy/README.md` and `docs/operations/pilot-and-deployment/`.

## Related docs

- [Production deployment architecture](../../docs/engineering/production-deployment-architecture.md)
- [Production readiness audit](../../docs/engineering/production-readiness-audit.md)
- [P14-WP02 report](../../docs/reports/P14-WP02-production-packaging-and-compose-baseline.md)
- [P14-WP02 live-preview gap fix](../../docs/reports/P14-WP02-gap-fix-separate-live-preview-stack.md)
