# P9-WP02 — Performance and Reliability

Phase marker: `P9-WP02-performance-and-reliability`

## Status

**Complete with documented risks.** Hardened Platform and PinoyBusinessPOS for MVP-scale performance baselines, query/index hot paths, API health/readiness, concurrency-safe idempotent client mutations, and offline BlockedByAccess reclaim. **No new business features.** **P9-WP01 security controls preserved.** **Not production-ready** while R-091, R-109, R-129, and related blockers remain open. **P9-WP03 was not started.**

Feature commit: 46a4ac7bacfad0736fba4741817958862fadf9e2

## Delivered

| Area | Delivered |
|---|---|
| Health/readiness | `/health` liveness (no dependency checks); `/health/ready` DB `CanConnect` (Infrastructure checks; no secrets) on Platform + POS |
| Query optimization | Reporting N+1 removed via `SumActiveAmountsByOrganizationAsync` and category/customer `ListByIdsAsync` |
| Indexes | Migration `AddPosPerformanceIndexes`: `ix_sale_lines_org_product`, `ix_stock_movements_org_recorded`, `ix_customers_org_updated` |
| API clients | Sale checkout / expense record attach `Idempotency-Key` + payload hash when client entity id present |
| MAUI | Expense attempt-id kept on ambiguous timeout/unavailable; rotate only on definitive failures; double-submit guards on sale/expense |
| Offline | `ReclaimBlockedByAccessAsync`; processor reclaims when access restored; no silent discard |
| Performance evidence | CI smoke at scaled volume (25 products / 25 customers); provisional budgets asserted; full MVP volumes not claimed |
| Tests | Health readiness; migration apply/rollback; offline reclaim; sale idempotency headers; architecture guards; perf smoke |

## Performance budgets (provisional engineering)

Not business-approved SLAs:

| Path | Provisional p95 |
|---|---|
| Common reads | ≤ 500 ms |
| Search/list | ≤ 750 ms |
| Dashboard | ≤ 1.5 s |
| Ordinary mutations | ≤ 1 s |
| Checkout / financial workflows | ≤ 2 s |
| Local SQLite reads | ≤ 150 ms (not re-measured this WP at full volume) |
| App-start foundation | ≤ 2 s (not interactively measured; R-109) |

## Test environment and data volumes

| Item | Value |
|---|---|
| Environment | Developer workstation + Testcontainers PostgreSQL |
| Perf smoke seed | 25 products, 25 customers (scaled for CI) |
| Full MVP targets (not seeded here) | ~10 orgs, 5k products, 10k customers, 50k sales, etc. |
| Limitation | Do not claim representative production latency; CI budgets are provisional |

Measured warm-path smoke (Release tests): catalog list/search, customer search, and dashboard stayed within provisional budgets at scaled volume with 0 errors.

## Timeout / retry defaults

| Component | Default | Notes |
|---|---|---|
| MAUI `PosApi` / `PosBusinessApi` | 15 s | Configured in `MauiProgram` |
| Offline queue max attempts | 8 | `OfflineRetryClassifier.DefaultMaxAttempts` |
| Offline backoff | Classifier schedule | No tight retry loops |
| Financial mutations | Idempotent replay only | No broad automatic retries on non-idempotent paths |
| Validation / authz / conflict | Not retried | Client keeps attempt id on Timeout/Unavailable/Offline |

## Health / readiness

| Endpoint | Behavior |
|---|---|
| `GET /health` | Liveness — always Healthy when process is up; no DB |
| `GET /health/ready` | Readiness — Unhealthy/503 when DB unreachable; no connection strings/secrets in body |
| Production | Startup still fail-closed per P9-WP01; readiness reflects ability to serve protected work |

## Offline backlog / recovery

- Abandoned `Syncing` → `Pending` on recover
- `BlockedByAccess` → `Pending` on reclaim when access restored
- Per-context FIFO preserved; one bad op does not silently delete unrelated work
- No automatic destructive cleanup
- Local DB growth / SQLCipher remains R-129 gate

## Observability

No new external monitoring platform. Existing correlation IDs + health endpoints remain. Duration metrics for CI smoke live in `PosPerformanceBudgetSmokeTests`. Bounded operational Meter export deferred until authorized tooling.

## Caching

No Redis / distributed cache introduced. Prefer query + index fixes.

## Failure scenarios (summary)

| Scenario | User-facing | Integrity | Residual |
|---|---|---|---|
| PostgreSQL down | Readiness 503; liveness OK | No write | Ops must restore DB |
| Checkout timeout | Timeout message; keep attempt id | Idempotent replay safe when headers present | Confirm after reconnect |
| Access revoked during queue | BlockedByAccess | Retained until reclaim | Reconnect required |
| SecureStorage / SQLite fail | RecoveryRequired / fail closed | No silent drop | R-129 |
| Android process death mid-sync | Startup recover Syncing | Durable queue | Interactive device R-109 |

## Explicit exclusions

- New business features; legacy product changes
- Redis/distributed cache
- Weakening P9-WP01 security
- Automatic destructive offline cleanup
- P9-WP03 or later
- Full MVP data-volume load as production SLA proof

## Build / test evidence

| Check | Result |
|---|---|
| `dotnet build ExItS.slnx -c Release` | Succeeded |
| `dotnet test ExItS.slnx -c Release` | **915 / 0 / 0** (baseline 900) |
| Android Release (`net10.0-android`) | Succeeded (NU1903 warnings retained as R-129) |
| Interactive Android device | **Not claimed** (R-109) |

## Security

P9-WP01 Production guards, rate limits, safe ProblemDetails, header gating, and fail-closed commercial behavior remain. Health responses do not leak secrets. No cross-org caching.

## Portfolio independence

Unchanged: ignored, untracked, outside `ExItS.slnx`.

## Unresolved risks / release blockers

- R-091 production auth
- R-109 interactive Android validation
- R-129 SQLCipher / SQLitePCLRaw NU1903
- Full MVP-scale load/soak not run in this environment
- Production TLS / MAUI cleartext replacement gate

## Exact next work package

**P9-WP03 — Backup and Restore** (do not begin until authorized)

## Files / docs changed

See Git feature and documentation commits for this WP.

## Git evidence

| Item | Value |
|---|---|
| Feature commit | 46a4ac7bacfad0736fba4741817958862fadf9e2 |
| Docs commit | 61b85779ef5b2fa48f63f8c38194a56daaf6627e |
| Docs hash-record commit | 61b85779ef5b2fa48f63f8c38194a56daaf6627e |
| Tests | 915 / 0 / 0 |
| Exact next WP | **P9-WP03 — Backup and Restore** |
