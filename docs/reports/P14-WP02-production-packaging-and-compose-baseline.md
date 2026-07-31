# P14-WP02 — Production Packaging and Compose Baseline

Phase marker: `P14-WP02-production-packaging-and-compose-baseline`

Package: **P14-WP02 — Production Packaging and Compose Baseline**
Prior tip: `7dbd7bf8835b564e86ec3ced8259bee23cceee4c`
Feature tip: *(recorded after feature commit)*

## Status

**Complete.** Reproducible container + Compose baseline for **local deployment testing** of Platform API, Platform PostgreSQL, PinoyBusinessPOS API, and POS PostgreSQL. **Not** a Production cutover; TLS/reverse proxy deferred to **P14-WP03**. Portfolio remains **not Production-ready**.

Exact next: **P14-WP03 — Reverse Proxy, TLS, and Network Hardening** when authorized (do **not** begin).

## 1. Delivered capability

| Area | Evidence |
|---|---|
| Default Compose | `deploy/docker/compose.yaml` (`exits-packaging`) |
| Env template | `deploy/docker/.env.example` (placeholders; `.env` gitignored) |
| Images | Shared `Dockerfile.platform-api` / `Dockerfile.pos-api` (no apt; non-root user) |
| Docs | `deploy/docker/README.md` |
| Architecture tests | `ProductionPackagingArchitectureTests` |
| Pilot preserved | `docker-compose.pilot.yml` remains NON-PRODUCTION; not renamed as Production |

## 2. Topology exercised

```text
platform-api  →  platform-db (exits_platform)
pos-api       →  pos-db (exits_pos)
```

Separate databases; no HealthCare; `ASPNETCORE_ENVIRONMENT=Staging` (no Dev/Testing identity headers). No app-startup `Migrate()`.

## 3. Operator commands (validated)

From `deploy/docker` (with local untracked `.env`):

```text
docker compose build
docker compose up -d
docker compose ps
docker compose down
```

Evidence this session: images `exits/platform-api:local` and `exits/pos-api:local` built; four services Up; `/health` returned **200 Healthy** on ports 8081/8082 with `Host: localhost`; `down` cleared the stack.

## 4. Explicit exclusions

- Reverse proxy / TLS certificates (**P14-WP03**)
- Platform Admin in default packaging compose (still in pilot compose)
- CI/CD pipelines; monitoring agents; Production secrets
- Claiming Production-ready; closing TLS-PROD / R-109 / R-129
- Auto-migrate on container start
- Promoting `docker-compose.pilot.yml` as Production

## 5. Validation

| Check | Result |
|---|---|
| `docker compose config` | Succeeded |
| `docker compose build` | Succeeded |
| `docker compose up -d` / `ps` / `down` | Succeeded |
| Host `/health` | 200 Healthy (Platform + POS) |
| Full Release tests | **1264 passed / 0 failed / 0 skipped** |
| Portfolio independence | No HealthCare root / solution projects |

## Exact next work package

**P14-WP03 — Reverse Proxy, TLS, and Network Hardening** when explicitly authorized. Do **not** begin P14-WP03.
