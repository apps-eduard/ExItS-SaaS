# ExItS Docker packaging + Local Validation (P14-WP02 / P16-WP11)

## Layout

| File | Purpose |
|---|---|
| `compose.yaml` | **Default** packaging baseline (Platform API + POS API + separate PostgreSQL). Local deployment testing. **Not Production-ready.** |
| `compose.production.yaml` | Production topology template: nginx reverse proxy TLS, internal DBs/APIs only (**P14-WP03**). **Not** a Production-ready claim. |
| `compose.local-validation.yaml` | Local Validation project `exits-local-validation`. **Default `up` = DBs only**; app services behind profile `apps`. |
| `README.local-validation.md` | Authoritative daily workflow: Docker DBs + local .NET APIs/Admin |
| `Start-LocalValidation.ps1` | Bring up DBs (if needed) and start local Platform/POS/Admin |
| `Stop-LocalValidation.ps1` | Stop DBs without deleting volumes |
| `docker-compose.pilot.yml` | Controlled **pilot** stack with Admin + nginx TLS template (NON-PRODUCTION) |
| `Dockerfile.platform-api` | Platform API image |
| `Dockerfile.pos-api` | PinoyBusinessPOS API image |
| `Dockerfile.platform-admin` | Platform Admin image |
| `nginx/pilot.conf` | Pilot reverse-proxy template (NON-PRODUCTION) |
| `nginx/production.conf` | Production reverse-proxy template (P14-WP03) |
| `certs/README.md` | Operator TLS mount guidance (no real certs in repo) |
| `.env.example` | Packaging baseline placeholders |
| `.env.local-validation.example` | Local Validation placeholders (copy to `.env.local-validation`) |
| `.env.production.example` | Production Compose placeholders (copy to `.env.production`) |

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
- TLS / reverse proxy are **not** included here (see production Compose below).
- Portfolio remains **not Production-ready**.

## Production reverse proxy / TLS (P14-WP03)

```powershell
Copy-Item .env.production.example .env.production
# Edit — replace REPLACE_* passwords; set PRODUCTION_TLS_CERT_DIR to host path with fullchain.pem + privkey.pem

docker compose -f compose.production.yaml --env-file .env.production config
docker compose -f compose.production.yaml --env-file .env.production up -d
```

| Surface | Default public | Notes |
|---|---|---|
| Reverse proxy HTTP | **80** | Redirects to HTTPS |
| Reverse proxy HTTPS | **443** | Sole intended app entry |
| Platform API / POS API / Admin / DBs | internal only | No host ports published |

Routes: `/admin/*`, `/platform/*`, `/pos/*`. Proxy technology: **nginx** (same family as pilot; production conf is separate). Forwarded headers are constrained via `ForwardedHeaders:KnownNetworks`. Local Validation ports **8090–8092 / 15533–15534** are unchanged. **Not Production-ready** — see [P14-WP03 report](../../docs/reports/P14-WP03-reverse-proxy-tls-network-hardening.md).

## Local Validation — recommended daily workflow

**Docker: databases only. Host: Platform API + POS API + Admin.** Sign in with approved named Local Validation identities via normal `POST /auth/login` (password from `LOCAL_VALIDATION_SHARED_PASSWORD` in `.env.local-validation` — never commit the secret).

```powershell
# from repository root
.\tools\Start-LocalValidation.ps1
```

Full guide: [`README.local-validation-workflow.md`](README.local-validation-workflow.md) · overview: [`README.local-validation.md`](README.local-validation.md).

```powershell
Copy-Item deploy\docker\.env.local-validation.example deploy\docker\.env.local-validation
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

- [Local Validation workflow](README.local-validation.md)
- [P16-WP11 Local Validation report](../../docs/reports/P16-WP11-local-validation-replaces-live-preview.md)
- [Production deployment architecture](../../docs/engineering/production-deployment-architecture.md)
- [Production readiness audit](../../docs/engineering/production-readiness-audit.md)
- [P14-WP02 report](../../docs/reports/P14-WP02-production-packaging-and-compose-baseline.md)
- [P14-WP02 Live Preview gap fix (historical)](../../docs/reports/P14-WP02-gap-fix-separate-live-preview-stack.md)
- [P14-WP02A Live Preview launcher (historical)](../../docs/reports/P14-WP02A-live-preview-test-users-and-quick-login.md)
