# Deployment architecture (pilot / non-production)

Phase: `P9-WP05-pilot-and-deployment`

## Topology

```text
[Android MAUI pilot APK] --HTTPS--> [nginx reverse proxy / TLS termination]
                                        |-- /platform/* --> Platform API
                                        |-- /pos/*      --> PinoyBusinessPOS API
                                        |-- /admin/*    --> Platform Admin
Platform API --> PostgreSQL Platform DB (separate)
POS API      --> PostgreSQL POS DB (separate)
Backup tooling --> protected backup storage (manifest + SHA-256)
Logs/health  --> container healthchecks + /health + /health/ready
Secrets      --> environment-owned secret storage (not repository)
```

## Boundaries

- Platform and POS databases remain separate; no cross-product DB access; no HealthCare coupling.
- Platform and POS APIs are independently deployable (separate containers/processes).
- Least-privilege DB accounts; environment isolation; no production secrets in repo or images.
- Development/Testing identity headers only in Development/Testing environments.

## Packaging

- `deploy/docker/Dockerfile.platform-api`
- `deploy/docker/Dockerfile.pos-api`
- `deploy/docker/Dockerfile.platform-admin`
- `deploy/docker/docker-compose.pilot.yml` (**NON-PRODUCTION**)
- `deploy/docker/nginx/pilot.conf`

## TLS status

- Pilot/staging: reverse-proxy HTTPS with operator-supplied cert directory (`PILOT_TLS_CERT_DIR`).
- Production TLS with real public certificate: **not claimed complete** (open blocker).
- Android: cleartext limited to localhost/emulator domains; arbitrary Production cleartext remains blocked; Production HTTPS-only policy replacement remains open.
