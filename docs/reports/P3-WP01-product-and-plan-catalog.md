# P3-WP01 — Product and Plan Catalog

## 1. Assignment

| Field | Value |
|---|---|
| Phase | Phase 3 — Portfolio Billing, Plans and Entitlements |
| Work package | P3-WP01 — Product and Plan Catalog |
| Status | Ready for Review |
| Branch | `main` |
| Date | 2026-07-29 |

## 2. Summary

Implemented the first **persistent** Platform commercial catalog: EF Core + PostgreSQL (`ExItS_Platform` / schema `platform`), repositories, unit of work, expanded catalog commands/queries, REST API under `/api/v1/platform/catalog`, migration `InitialPlatformCatalog`, and Testcontainers integration tests. Subscriptions, payments, Admin UI, HealthCare integration, and POS were **not** implemented.

**Security note:** Catalog endpoints are **development-stage and unauthenticated**. They are not production-ready (R-045).

## 3. Persistence foundation

| Item | Value |
|---|---|
| EF Core | 10.0.4 |
| Npgsql EF provider | 10.0.2 |
| DbContext | `PlatformDbContext` |
| Database | `ExItS_Platform` |
| Schema | `platform` |
| Config key | `ConnectionStrings:PlatformDatabase` |
| Migration | `InitialPlatformCatalog` (`20260729171154_InitialPlatformCatalog`) |
| Startup migrate | **No** — explicit `dotnet ef database update` only |

Local Development uses Docker Postgres on port **5434** with a local-only password in `appsettings.Development.json` (not production credentials). Prefer user-secrets/env for shared machines.

## 4. Catalog persistence

| Table | Key constraints |
|---|---|
| `products` | PK `id`; unique `code`; `xmin` concurrency |
| `feature_definitions` | PK (`product_code`,`feature_code`) |
| `plans` | PK `id`; unique (`product_code`,`code`) |
| `plan_versions` | PK `id`; unique (`plan_id`,`version_number`) |
| `plan_version_feature_grants` | PK (`plan_version_id`,`feature_code`) |
| `trial_definitions` | PK `id`; `duration_ticks` (configurable TimeSpan; **not** 90 days) |
| `trial_definition_feature_grants` | PK (`trial_definition_id`,`feature_code`,`grant_kind`) |

No identity, organization, subscription, payment, entitlement-projection, HealthCare, or POS tables.

## 5. Application capability

Commands: create/rename/activate/deactivate/retire product; create/retire feature; create/rename/activate/retire plan; create draft plan version; upsert draft grants; publish draft/version; create/retire trial.

Queries: list/get products; list features; list/get plans; list/get versions; latest published; list trials (pagination default 20 / max 100).

Repositories: explicit EF implementations + `IPlatformUnitOfWork`. Conflicts mapped to stable Application error codes (409).

## 6. API routes (`/api/v1/platform/catalog`)

Products, features, plans, plan versions, trials as implemented in `CatalogEndpoints.cs`.

Confirmed absent: subscription, payment, GCash, HealthCare, POS routes.

Phase marker: `P3-WP01-product-plan-catalog`. Routes: `GET /`, `GET /health` retained.

## 7. Trial duration

- Generic `TrialDefinition.Duration` remains configurable `TimeSpan` (stored as ticks).
- No `TimeSpan.FromDays(90)` helper.
- PinoyBusinessPOS **three calendar months** remains documented; **R-035 open**.

## 8. Migration validation

Isolated Docker `postgres:18` on `127.0.0.1:5434`:

- `dotnet ef database update` → applied `InitialPlatformCatalog`
- `dotnet ef database update 0` → rollback validated (prior run)
- Re-apply succeeded
- Seven expected catalog tables only

## 9. Tests

| Suite | Passed | Failed | Skipped |
|---|---:|---:|---:|
| ExItS.Platform.UnitTests | 100 | 0 | 0 |
| ExItS.ArchitectureTests | 27 | 0 | 0 |
| ExItS.Platform.IntegrationTests | 13 | 0 | 0 |
| **Total** | **140** | **0** | **0** |

Integration strategy: **Testcontainers PostgreSQL 18** (not EF InMemory).

## 10. Runtime validation

| Check | Result |
|---|---|
| Port | 5288 |
| `GET /` | phase `P3-WP01-product-plan-catalog` |
| `GET /health` | Healthy |
| POST product | **201** |
| Duplicate product | **409** |
| Full catalog flow | Covered by IntegrationTests |
| Shutdown | Clean |

## 11. HealthCare freeze

`/HealthCare/` ignored; `git ls-files -- HealthCare/` empty; not in solution; unchanged. Platform `Integration/HealthCare/` untouched.

## 12. Risks

- **R-033** reduced/mitigated — DB unique constraints + integration tests prove catalog uniqueness.
- **R-012** still open for billing/payments/subscription persistence.
- **R-035** open (calendar EOM).
- **R-031** open (no authentication).
- **R-045** added — unsecured development-stage catalog API.
- **R-046** added — local-dev connection string pattern / migration misuse risk.

## 13. Commits

| Field | Value |
|---|---|
| Hash | `9d01f26095c3c76ffd67aa2b7b5bcf1a19a328f2` |
| Message | `feat(platform): implement product and plan catalog` |

## 14. Next work package

**P3-WP02 — Trials and Subscription Lifecycle** (do not begin until authorized).
