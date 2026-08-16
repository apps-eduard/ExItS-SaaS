# Production Deployment Architecture

[Home](../index.md) | [Readiness audit](production-readiness-audit.md) | [Pilot architecture (non-production)](../operations/pilot-and-deployment/deployment-architecture.md) | [Product Foundation](../Product-Foundation/exits-product-foundation-reference.md) | [Auth architecture](authentication-architecture.md) | [Phase 14](../phases/phase-14-production-deployment-and-operations.md) | [P14-WP01 report](../reports/P14-WP01-deployment-architecture-and-production-readiness-audit.md)

**Status:** Authoritative **production** deployment direction (**P14-WP01**). Packaging baseline for local Compose testing delivered in **P14-WP02** (`deploy/docker/compose.yaml`). Reverse-proxy/TLS/network template delivered in **P14-WP03** (`compose.production.yaml` + `nginx/production.conf`) — **not** a Production cutover or readiness claim.

**Relationship to pilot:** [`docs/operations/pilot-and-deployment/`](../operations/pilot-and-deployment/) and `deploy/docker/docker-compose.pilot.yml` remain **non-production** (P9-WP05). Default `compose.yaml` is the P14-WP02 packaging baseline for local testing.

---

## 1. Purpose

Define how ExItS is intended to run in **customer on-prem Production** so later Phase 14 work packages do not:

- collapse Platform and product databases
- treat pilot Compose as Production-ready
- invent cloud-only topology without evidence
- claim Production readiness while release blockers remain open
- silently `Migrate()` databases on app startup

---

## 2. Target topology (D-P14-01)

```text
Customer On-Prem Server
├── Reverse proxy / HTTPS termination
│     ├── /admin/*     → Platform Admin
│     ├── /platform/*  → Platform API
│     └── /{product}/* → Licensed product API(s) (e.g. /pos/*)
├── ExItS Platform application (API + Admin surfaces)
├── Platform PostgreSQL (Platform schema/DB only)
├── Licensed Product application(s) (independently versioned)
└── One PostgreSQL instance or container per product
```

| Element | Rule |
|---|---|
| **Host** | Customer-operated on-prem server (or equivalent single-tenant host they control) |
| **Platform** | One Platform deployable set (API; Admin as Platform UI) |
| **Products** | Deploy **only** licensed/subscribed products for that customer |
| **Databases** | Platform DB separate from every product DB; **no** cross-product DB access or cross-DB FKs |
| **Edge** | Reverse proxy terminates TLS; apps may listen HTTP behind the proxy on a private network |
| **Clients** | Browser Admin via HTTPS; MAUI/POS via HTTPS Platform + product APIs |

**Alignment:** Matches Product Foundation §8 (one Platform image family, one product image family, one DB per product, config not forks).

**Adjustment note:** Repository already has multi-container pilot Compose and separate Platform/POS Dockerfiles. Production keeps the same **logical** boundaries; packaging hardening is later Phase 14 WPs — not claimed complete here.

---

## 3. Trust and ownership boundaries

1. **Internet → reverse proxy** — TLS required in Production; no Dev/Testing identity headers.
2. **Proxy → apps** — private network; least privilege; no public DB ports.
3. **Platform API ↔ Product API** — HTTP contracts only; never shared DbContext or DB credentials across products.
4. **Platform DB ↔ Product DB** — separate instances (or strictly isolated instances/containers); backups independent.
5. **Device (MAUI)** — local SQLite/SecureStorage is **not** the server backup SoR; unsynced local loss remains disclosed risk (**LOCAL-UNSYNCED**).
6. **Repository isolation** — Production targets only approved ExItS services and connection strings.

Locked commercial/identity chain (unchanged):

```text
Platform User → Active Organization Membership → Product Access / Entitlement → Product-Local Role
```

Authentication SoR remains Platform (**D-P13-02**). Deployment does not grant membership, entitlement, or product roles.

---

## 4. What exists today (evidence)

| Area | Evidence | Production claim |
|---|---|---|
| Pilot packaging | `deploy/docker/docker-compose.pilot.yml`, nginx `pilot.conf` | **Non-production only** |
| Packaging baseline | `deploy/docker/compose.yaml` (P14-WP02) | Local Compose testing; **not** Production cutover |
| Production proxy/TLS template | `deploy/docker/compose.production.yaml`, `nginx/production.conf` (P14-WP03) | Topology baseline; operator certs required; **not** Production-ready claim |
| Local validation (production-equivalent) | `deploy/docker/compose.local-validation.yaml` (`exits-local-validation`) | **Default:** Docker DBs only (ports 15533/15534); local Platform/POS/Admin via `Start-LocalValidation.ps1`. Optional `--profile apps` for containerized APIs/Admin. Same app code as Production; config-only differences. **Not** packaging baseline |
| Ops scripts | `ops/deploy/*`, `ops/backup/*` | Pilot/ops helpers; Production cutover **not** evidenced |
| Deployment library | `ExItS.Deployment` + CLI | Validation, backup gate, migration order, readiness evaluator |
| AuthN | Phase 13 sessions + Bearer + external login | **R-091 closed for Phase 13 scope**; residuals remain |
| Health | `/health`, `/health/ready` | Present; not a full monitoring stack |
| Backups | Logical `pg_dump` tooling (P9-WP03) | Tooling present; PITR deferred; Production off-host schedule **environment-owned** |
| Production guards | P9-WP01 fail-closed config / header rejection | Present; insufficient alone for “Production-ready” portfolio claim |

---

## 5. Configuration and secrets (D-P14-02)

| Rule | Detail |
|---|---|
| No secrets in repo or images | Connection strings, OAuth client secrets, TLS private keys, backup encryption keys stay environment-owned |
| Templates only | `ops/deploy/templates/*.env.example` and checklists — placeholders |
| Production startup | Reject known-dev DB password marker; reject `AllowedHosts=*`; require secure configuration |
| Per-product config | Customer-specific **configuration**, never customer-specific source forks |
| Auth outbound email | Token workflows exist; **email vendor not selected** — Production notification delivery remains residual |

---

## 6. Migrations and data safety (D-P14-03)

1. Verify environment and secrets.
2. Backup Platform DB and each product DB; verify manifests/SHA-256.
3. Migrate Platform, validate.
4. Migrate each licensed product, validate.
5. Start apps; health/readiness; smoke appropriate to environment.
6. Record deploy evidence (version, set IDs, operator).

**Forbidden:** automatic `Database.Migrate()` on Production application startup paths.

**Rollback:** ordinary app rollback ≠ database restore. Destructive restore requires explicit confirmation and runbook.

---

## 7. TLS and network (P14-WP03 baseline)

| Layer | Production direction |
|---|---|
| Reverse proxy | nginx HTTPS with operator-supplied certificates; HSTS on HTTPS listener |
| Forwarded headers | Enabled only with explicit KnownNetworks/KnownProxies — no trust-all |
| Platform Admin BaseUrl / PlatformAuth BaseUrl | HTTPS in Production |
| MAUI | HTTPS-only Production BaseUrl validation in ApiClient; **MAUI-HTTPS** device evidence still open |
| Public ports | Reverse proxy 80/443 only in production Compose template |

Open release items: **TLS-PROD** (customer cutover evidence), **MAUI-HTTPS** (device/emulator cert validation). See readiness audit.
| Pilot TLS | Operator-supplied cert dir for StagingPilot — **not** Production TLS evidence |

Open release items: **TLS-PROD**, **MAUI-HTTPS** (see readiness audit).

---

## 8. Observability and support (direction only)

Present: health/readiness endpoints, deploy/backup runbooks, audit events for auth.

Not claimed: centralized metrics/log aggregation, paging, SLA dashboards, APM agents.

Later Phase 14 WPs may add monitoring agents/runbooks **when authorized** — not in P14-WP01.

---

## 9. Decisions (P14-WP01)

| ID | Decision | State |
|---|---|---|
| **D-P14-01** | Production topology = customer on-prem host with Platform + per-product apps/DBs + reverse-proxy HTTPS | **Closed** |
| **D-P14-02** | Secrets and Production connection strings are environment-owned; never committed | **Closed** |
| **D-P14-03** | Production migrate path = backup-verify → migrate → validate; no silent startup `Migrate()` | **Closed** |
| **D-P14-04** | P9 pilot Docker/Compose/nginx are non-production references; Production packaging is separate Phase 14 implementation work | **Closed** |
| **D-P14-05** | Portfolio remains **not Production-ready** until readiness audit blockers are closed with evidence | **Closed** (honesty) |
| **D-P12-03** | Commercial-state transport | **Open** (preserved; not invented here) |

---

## 10. Explicit non-goals (this WP)

- New Dockerfiles, Compose files, CI/CD, reverse-proxy conf, TLS certs, secrets, DB containers, monitoring agents
- Application feature code, migrations, NuGet packages
- Claiming Production-ready or closing R-109 / R-129 / TLS-PROD / MAUI-HTTPS without evidence
- deployment of unapproved services or source trees

---

## 11. Recommended next work package

**P14-WP04 — Production Backup, Restore, and Ops Evidence** when explicitly authorized.
