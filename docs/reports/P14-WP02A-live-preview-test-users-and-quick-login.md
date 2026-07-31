# P14-WP02A — Live Preview Test Users and Quick Login

Phase marker: `P14-WP02A-live-preview-test-users-and-quick-login`

Package: **P14-WP02A Development Workflow Adjustment — Docker Databases Only, Local Web/API Execution**
Prior tip: `844fdf17bece466d52228defdb03c2ae5c13b579`
Feature tip: `10e77e13c1702db4a75d163a847112e0064ef3b8`

## Status

**Complete.** Daily live-preview workflow is **Docker PostgreSQL only** (existing `exits_live_preview_*` volumes preserved) with **Platform API, POS API, and Platform Admin** run as local .NET processes. Optional containerized apps remain behind Compose profile `apps`. Quick-login / login CSS fixes from prior tips remain in effect. **Not Production.** **P14-WP03 not started.**

## Delivered workflow

```text
Docker
├── Platform PostgreSQL  (:15533, volume exits_live_preview_platform_db_data)
└── POS PostgreSQL       (:15534, volume exits_live_preview_pos_db_data)

Local .NET
├── Platform API         (:8091, LivePreview profile)
├── POS API              (:8092, LivePreview profile)
└── Platform Admin       (:8090, LivePreview profile)
```

| Artifact | Role |
|---|---|
| `compose.live-preview.yaml` | Default `up` = DBs only; `platform-api` / `pos-api` / `admin-web` use `profiles: ["apps"]` |
| `README.live-preview.md` | Operator doc for the workflow |
| `Start-LivePreviewLocal.ps1` | DB up (no app containers) + local `dotnet run` windows |
| `Stop-LivePreviewLocal.ps1` | `stop` / `down` **without** `-v` |
| Launch profiles `LivePreview` | Staging + LivePreview ports on Platform/POS/Admin |

## Operator

```powershell
cd deploy\docker
# .env.live-preview already filled; never commit
docker compose -f compose.live-preview.yaml --env-file .env.live-preview up -d
.\Start-LivePreviewLocal.ps1
```

Open **http://localhost:8090/admin/login**.

Do **not** `docker compose down -v` (preserves seeded preview identities).

## Explicit exclusions

- P14-WP03 TLS/proxy
- Resetting or deleting live-preview DB volumes
- Changing packaging compose ports/DBs
- Phase 15 Admin UX

## Validation

| Check | Result |
|---|---|
| `docker compose ... up -d` (no profile) | Only `platform-db` + `pos-db` |
| Volumes `exits_live_preview_*_db_data` | Present / unchanged |
| Architecture tests | Live-preview profile + README/scripts asserted |
| Full Release tests | **1267 passed / 0 failed / 0 skipped** |

## Exact next

**P14-WP03 — Reverse Proxy, TLS, and Network Hardening** when authorized.
