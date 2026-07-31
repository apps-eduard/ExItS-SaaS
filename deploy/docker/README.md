# ExItS Docker packaging (P14-WP02 + P9-WP05 pilot)

## Layout

| File | Purpose |
|---|---|
| `compose.yaml` | **Default** packaging baseline (Platform API + POS API + separate PostgreSQL). Local deployment testing. **Not Production-ready.** |
| `docker-compose.pilot.yml` | Controlled **pilot** stack with Admin + nginx TLS template (NON-PRODUCTION) |
| `Dockerfile.platform-api` | Platform API image |
| `Dockerfile.pos-api` | PinoyBusinessPOS API image |
| `Dockerfile.platform-admin` | Platform Admin image (pilot compose) |
| `nginx/pilot.conf` | Pilot reverse-proxy template |
| `.env.example` | Packaging baseline placeholders |

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

Notes:

- Separate `platform-db` and `pos-db` — no shared product database.
- `ASPNETCORE_ENVIRONMENT=Staging` — Dev/Testing identity headers unavailable.
- Apps do **not** auto-migrate. Apply migrations with approved ops tooling against published DB ports before expecting full readiness.
- TLS / reverse proxy are **not** included here (**P14-WP03**).
- Portfolio remains **not Production-ready**.

## Pilot stack (P9-WP05)

```powershell
docker compose -f docker-compose.pilot.yml --env-file <your-pilot-env> up -d
```

See `ops/deploy/README.md` and `docs/operations/pilot-and-deployment/`.

## Related docs

- [Production deployment architecture](../../docs/engineering/production-deployment-architecture.md)
- [Production readiness audit](../../docs/engineering/production-readiness-audit.md)
- [P14-WP02 report](../../docs/reports/P14-WP02-production-packaging-and-compose-baseline.md)
