# P3-WP04 — Entitlement Snapshots and Grace Rules

## 1. Assignment

| Field | Value |
|---|---|
| Phase | Phase 3 — Portfolio Billing, Plans and Entitlements |
| Work package | P3-WP04 — Entitlement Snapshots and Grace Rules |
| Status | Ready for Review |
| Branch | `main` |
| Date | 2026-07-29 |

## 2. Summary

Persisted authoritative Platform feature overrides and immutable entitlement snapshots (with grants), wired composition through the existing `EntitlementSnapshotComposer`, added configurable provisional refresh policy (R-022 remains open), development-stage REST APIs, migration `AddEntitlementSnapshotsAndOverrides`, and expanded unit/architecture/integration tests.

**Snapshots are authoritative Platform records only.** No product delivery, broker, Hangfire, HealthCare, or POS projection implementation exists.

**Security note:** Entitlement and override mutation endpoints are **development-stage and unauthenticated** (R-045 expanded). Actor references accept `PlatformUserId` GUIDs without authentication — production blocker.

## 3. Entitlement and override domain

| Item | Value |
|---|---|
| Composer | `EntitlementSnapshotComposer` — plan/trial grants → overrides → status restrictions |
| Override | `FeatureOverride` with reason, creator, optional expiry, revoke(reason, actor, utc) |
| Snapshot | Immutable `EntitlementSnapshot` + `EntitlementGrant` list; optional `ExpiresAtUtc` |
| Refresh | `IEntitlementRefreshPolicy` / `ProvisionalEntitlementRefreshPolicy` — **24h** for all states (provisional) |
| Schema version | `EntitlementSnapshot.CurrentSchemaVersion = 1` |

## 4. Snapshot versioning strategy

- Scope: `(organization_id, product_code)`
- Allocation: `latest + 1` in application; enforced by unique index `ux_entitlement_snapshots_org_product_version`
- Reconciliation always inserts a **new** version; historical rows never updated
- Optimistic conflict → `application.entitlement_snapshot.version_conflict` / 409

## 5. State rules

| Status | Behavior |
|---|---|
| Trialing | Trial feature grants |
| Active | Published plan-version grants + active overrides |
| GracePeriod | Base grants retained; `inGracePeriod=true` |
| PastDue | `customer-credit-create` disabled (fail closed) |
| Suspended | `customer-credit-create` disabled |
| Cancelled | Non view/repay grants disabled; create disabled |
| Expired | Trial `PostExpiryFeatureGrants` (view/repay on, create off for Utang) |
| Unknown | Fail closed (domain exception) |

## 6. Persistence

| Table | Notes |
|---|---|
| `platform.feature_overrides` | Org FK; reason/actor; revoke metadata; xmin; expiry/limit checks |
| `platform.entitlement_snapshots` | Org + subscription FKs; unique org/product/version; positive version/schema checks |
| `platform.entitlement_snapshot_grants` | PK `(snapshot_id, feature_code)`; cascade delete with snapshot |

Migration: `AddEntitlementSnapshotsAndOverrides` (`20260729191447_AddEntitlementSnapshotsAndOverrides`).

## 7. API routes

Development-stage, unauthenticated:

- `POST/GET .../organizations/{orgId}/products/{productCode}/entitlements/snapshots`
- `GET .../entitlements/snapshots/latest`
- `GET /api/v1/platform/entitlements/snapshots/{snapshotId}`
- `POST .../entitlements/reconcile`
- `POST/GET .../feature-overrides`
- `GET/POST /api/v1/platform/feature-overrides/{id}` / `{id}/revoke`

Phase marker: `P3-WP04-entitlement-snapshots-grace-rules`. Retained: `GET /`, `GET /health`.

## 8. Migration validation

Isolated Docker `postgres:18` on `127.0.0.1:5434`:

```text
dotnet ef database update
  → Applied through AddEntitlementSnapshotsAndOverrides (13 platform tables)
dotnet ef database update AddManualSaaSPayments
  → Dropped entitlement + override tables (10 tables remain)
dotnet ef database update
  → Re-applied (13 tables)
```

## 9. Build and tests

| Suite | Passed | Failed | Skipped |
|---|---:|---:|---:|
| Unit | 200 | 0 | 0 |
| Architecture | 38 | 0 | 0 |
| Integration | 63 | 0 | 0 |
| **Total** | **301** | **0** | **0** |

## 10. Runtime validation

| Step | Result |
|---|---|
| Phase marker | `P3-WP04-entitlement-snapshots-grace-rules` |
| Active snapshot | v1 |
| Override precedence | create disabled, source=Override |
| Revoke + regenerate | create re-enabled |
| Grace | `inGracePeriod=true` |
| PastDue | create=false |
| Suspended | create=false |
| Expired Utang | view=true, repay=true, create=false |
| Version conflict | **409** |
| Reconcile | new immutable version |
| Delivery | none |

## 11. HealthCare freeze

- `git ls-files -- HealthCare/` empty
- `/HealthCare/` ignored
- No HealthCare project in `ExItS.slnx`

## 12. Risks

| ID | Note |
|---|---|
| R-022 | Still open — provisional 24h refresh policy only |
| R-034 | Product-specific tuning still open |
| R-035 | Calendar EOM still open |
| R-036 / R-037 / R-040 | Contract skew / projection gaps / contracts ≠ delivery — open |
| R-045 | Expanded to entitlement/override APIs |
| R-046 | Migration targeting continues |
| R-058 | Snapshot-version race (mitigated by unique index) |
| R-059 | Override misuse without auth/SoD |
| R-060 | Snapshot mistaken for completed product delivery |
| R-061 | Manual regeneration gaps (no scheduler) |
| R-062 | Unauthenticated override/snapshot mutation APIs |

## 13. Git evidence

| Field | Value |
|---|---|
| Feature commit | _(to be recorded)_ |
| Message | `feat(platform): persist entitlement snapshots and grace rules` |

## 14. Next work package

**P3-WP05 — Billing Closeout** (do not begin until authorized).
