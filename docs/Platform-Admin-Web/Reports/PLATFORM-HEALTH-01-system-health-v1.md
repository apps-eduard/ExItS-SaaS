# PLATFORM-HEALTH-01 — Platform Operations System Health V1

**Status:** Implemented on `feat/platform-admin-system-health` (not merged)  
**Work package:** PLATFORM-HEALTH-01  
**Starting HEAD:** `4cce7d9fc25942e156eab5f9abdc6748504e5f7a` (`feat/platform-admin-error-diagnostics`)  
**Worktree:** `C:\Users\speed\Desktop\ExItS-SaaS-PlatformWeb-SystemHealth`  
**Implementation commit:** recorded after the feature commit  
**Final HEAD:** recorded after the documentation hash-record commit

## Baseline selection

Remote Platform Admin React branches were compared against `origin/main` (`5979a9ce`). Active Agent 2 (`feat/platform-admin-pa-com-*`) and Agent 3 (`feat/platform-admin-global-catalog-*`) tips were excluded.

Selected starting SHA: **`4cce7d9fc25942e156eab5f9abdc6748504e5f7a`**.

This is the newest React Platform Admin baseline that already includes:

- `feat/platform-admin-web-v2`
- local-access / Mailpit auth fixes
- PA-ERR-01 error diagnostics

It is **not** an ancestor of Agent 2 or Agent 3 work. No merge, rebase, or cherry-pick of those branches was performed.

## Objective

Platform Admin System Health V1 at `/admin/system-health`.

This is a Platform Operations feature. It is not POS operational UI, Global Catalog, or commercial product/plan/subscription management.

## Health sources

| Check | Source | Notes |
|---|---|---|
| Platform API | Existing ASP.NET health liveness (`/health` predicate: no checks) via `HealthCheckService` | In-process; process is serving this request |
| Platform Database | Reused `PlatformDatabaseReadyHealthCheck` (`platform-database`) | Same check as `/health/ready` |
| POS API | HTTP GET `{PosProductApi:BaseUrl}/health` | Public liveness only; no support API key |
| POS Database | HTTP GET `{PosProductApi:BaseUrl}/health/ready` | Independent of POS API liveness; not inferred from process aliveness |
| Host CPU / RAM / storage / uptime | `Process` + `GC.GetGCMemoryInfo` + `DriveInfo` | No Docker socket, no shell, no path leakage |
| Build | `IHostEnvironment` + entry assembly informational version | Commit SHA only when `+sha` metadata exists |
| Backup | Not queried | No safe in-app backup catalog exists; ops manifests are host files |

## API contract

`GET /api/v1/platform/operations/system-health`

Read-only JSON:

- `overallStatus`: `Healthy` \| `Degraded` \| `Unhealthy` \| `Unknown`
- `host`: `cpuPercent`, `memoryUsedBytes`, `memoryTotalBytes`, `storageUsedBytes`, `storageFreeBytes`, `storageTotalBytes`, `uptimeSeconds` (nullable when unavailable)
- `services[]`: `name`, `status`, `latencyMs`, `checkedAtUtc`
  - `platform-api`, `pos-api`, `platform-database`, `pos-database`
- `build`: `environment`, `applicationVersion`, `commitSha`
- `backup`: `status`, `lastSuccessfulAtUtc`, `ageSeconds`

Service statuses: `Healthy`, `Degraded`, `Unhealthy`, `Unavailable`, `Unknown`.  
Backup V1 status: `NotAvailable`.

Overall status never treats unknown/unavailable as Healthy. Database health is not inferred from API liveness.

The endpoint returns HTTP 200 with a structured snapshot when authorized, including Unhealthy/Degraded dependencies. Authorization failures return 403.

## Authorization

Existing permission reused: **`platform.permission.view_portfolio`**.

Audit:

- No dedicated operations/system-health permission exists.
- `ViewPortfolio` is the existing Platform Admin operational read permission used by admin aggregation endpoints.
- A new permission was **not** invented.
- Server `PlatformAuthz.EnsureAsync` is authoritative. React route hiding is not sufficient.
- Unauthenticated / permission-less actors fail closed (403).

## Metrics implemented

- Platform API health + latency
- POS API health + latency (when `PosProductApi:BaseUrl` is configured)
- Platform database readiness + latency
- POS database readiness + latency (independent `/health/ready` probe)
- CPU percent (process average since start)
- RAM used / total (GC-visible host/container memory)
- Storage used / free / total (current drive; path omitted)
- Process uptime
- Environment name
- Application version from assembly informational/version metadata
- Commit SHA when `AssemblyInformationalVersion` contains `+sha`

## Metrics unavailable

| Metric | V1 status | Reason |
|---|---|---|
| Backup last success / age | `NotAvailable` | Backup state lives in operator-owned manifests/scripts (`ops/backup`, `ExItS.BackupRestore`). Reading those would inspect host files. No approved in-process backup reporting API exists. Not faked. |
| Docker container existence | Not exposed | Service health is used instead of container internals. |
| Instantaneous host-wide CPU (perf counters / `/proc/stat`) | Process CPU used instead | Avoid host file and OS-specific counters. |

When POS base URL is missing or unreachable, POS API and POS database are `Unavailable` (not Healthy).

## Security / redaction rules

React must not, and does not:

- access the Docker socket
- run docker CLI or host shell
- inspect arbitrary host files
- connect to PostgreSQL
- receive secrets or raw environment dumps

The operations API returns only approved operational facts. Responses are tested to omit:

- connection strings / `Password=`
- support API keys
- Docker socket details
- stack traces
- filesystem paths
- raw environment variables
- private keys

POS probe discards secret-like health bodies. Host metric and dependency exceptions are swallowed and never serialized.

## React UI

- Page: `/admin/system-health`
- Additive `App.tsx` routes only:
  - `system-health` → `SystemHealthPage`
  - `operations/health` → redirect to `/admin/system-health` (existing nav href; **no sidebar/registry restructure**)
- Cards + service table, Refresh, 30s auto-refresh
- Loading / error / Healthy / Degraded / Unhealthy / Unavailable
- Lucide icons, responsive desktop/tablet, human-readable sizes and durations
- Permission: `view_portfolio` (fail-closed to page-not-found)

**Not in this package:** dashboard health card changes, permanent sidebar/nav restructuring.

## Tests

### Backend

- `tests/ExItS.Platform.UnitTests/Operations/*` — status rules, host metric capture without leakage, POS probe truthful/redacted
- `tests/ExItS.Platform.IntegrationTests/ApiSystemHealthTests.cs`
  - authorized success
  - unauthorized fail-closed
  - secret redaction
  - DB unhealthy reflected
  - unavailable POS dependency
  - host metric failure does not crash
  - degraded POS API does not infer DB health

### React

- Healthy / Degraded / Unhealthy / Unavailable rendering
- CPU/RAM/storage formatting
- service table, version/SHA, backup unavailable
- refresh, loading/error, permission handling

### Playwright

- page opens, healthy scenario, degraded service
- no serious/critical axe violations
- 1440×900 and 768×1024 without horizontal overflow

## Evidence (this worktree)

| Check | Result |
|---|---|
| `dotnet test` operations unit | 18 passed |
| `dotnet test` `ApiSystemHealthTests` | 7 passed |
| `npm test` (Admin Web) | 314 passed |
| `npm run typecheck` | passed |
| `npm run lint` | passed (0 errors) |
| `npm run build` | passed |
| Playwright `e2e/system-health.spec.ts` | 4 passed |

## Known limitations

- Backup health is honestly `NotAvailable` until a safe, authoritative reporting channel exists.
- CPU is process-average, not a sampled host-wide instantaneous counter.
- Storage is the drive containing the API process, not a Docker volume inventory.
- Commit SHA is omitted when the assembly has no source-revision metadata.
- POS probes require `PosProductApi:BaseUrl`; they do not open the POS database from Platform.
- Existing dashboard liveness widget is unchanged (out of package).
- Sidebar item label remains “Platform Health”; canonical route is `/admin/system-health`.
- No merge to `main` and no production cutover.

## Explicit exclusions

- Agent 1 POS React / offline PIN / sales / payment / inventory
- Agent 2 `features/products/*`, `features/plans/*`, `features/commercial/*`
- Agent 3 `features/global-catalog/*`, `api/global-catalog/*`
- Permission-model expansion
- Historical charts
