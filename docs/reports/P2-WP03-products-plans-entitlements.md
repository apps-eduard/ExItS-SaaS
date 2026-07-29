# P2-WP03 — Products, Plans and Entitlement Foundation

## 1. Assignment

| Field | Value |
|---|---|
| Phase | Phase 2 — Platform Extraction and HealthCare Reconnection |
| Work package | P2-WP03 — Products, Plans and Entitlement Foundation |
| Status | Ready for Review |
| Branch | `main` |
| Date | 2026-07-29 |

## 2. Summary

Implemented Platform Domain and Application foundation for the commercial catalog and entitlement model: Product, FeatureDefinition, Plan, PlanVersion (published immutable), TrialDefinition, Subscription lifecycle, EntitlementGrant, FeatureOverride, EntitlementSnapshot, and deterministic `EntitlementSnapshotComposer`. No persistence, billing/payment collection, GCash, auth, Admin UI, product APIs, brokers, HealthCare integration, or POS entities.

## 3. Acceptance criteria and evidence

| Criterion | Status | Evidence |
|---|---|---|
| P2-WP02 recorded Complete | Met | portfolio-progress / phase-02 |
| Product / Feature / Plan / PlanVersion / Trial | Met | Domain Catalog |
| Subscription + controlled transitions | Met | Domain Subscriptions + tests |
| Grants, overrides, snapshots | Met | Domain Entitlements |
| Published plan version immutable | Met | Domain + architecture test |
| POS Utang trial expiry feature representation | Met | `customer-credit-view/repay/create` + post-expiry grants |
| Explicit repos + use cases; no generic repo | Met | Application Catalog/Subscriptions |
| No EF/auth/payment/business API | Met | packages + API routes |
| Tests pass | Met | **82** passed / 0 failed / 0 skipped |
| HealthCare freeze | Met | ignored, untracked, not in solution |

## 4. Types created

### Identifiers
`ProductId`, `PlanId`, `PlanVersionId`, `TrialDefinitionId`, `SubscriptionId`, `EntitlementSnapshotId`, `FeatureOverrideId`

### Catalog
`Product`, `ProductStatus`, `FeatureCode`, `FeatureDefinition`, `FeatureValueType`, `FeatureDefinitionStatus`, `FeatureGrantSpec`, `Plan`, `PlanCode`, `PlanStatus`, `PlanVersion`, `PlanVersionStatus`, `BillingPeriod`, `TrialDefinition`, `TrialDefinitionStatus`

### Subscriptions / entitlements
`Subscription`, `SubscriptionStatus`, `EntitlementGrant`, `EntitlementGrantSource`, `FeatureOverride`, `FeatureOverrideStatus`, `EntitlementSnapshot`, `EntitlementSnapshotComposer`

Reused: `ProductCode` (P2-WP02).

## 5. Entitlement-generation rules

1. Base grants: Trialing → trial active grants; Expired + trial present → trial post-expiry grants; else → plan version grants.
2. Suspended/Cancelled: force `customer-credit-create` off; Cancelled also disables other commercial grants except view/repay continuity codes.
3. Active overrides for same org+product replace matching feature (override wins).
4. Expired/revoked overrides ignored.
5. Snapshot version monotonic at application boundary; snapshot immutable after create.
6. No DB/clock inside Domain composer (clock only at use-case boundary).

## 6. Application boundary

**Repositories:** `IProductRepository`, `IFeatureDefinitionRepository`, `IPlanRepository`, `ITrialDefinitionRepository`, `ISubscriptionRepository`, `IFeatureOverrideRepository`, `IEntitlementSnapshotRepository`

**Use cases:** `CreateProduct`, `CreatePlan`, `PublishPlanVersion`, `StartTrialSubscription`, `ActivateSubscription`, `SuspendSubscription`, `CancelSubscription`, `CreateFeatureOverride`, `RevokeFeatureOverride`, `GenerateEntitlementSnapshot`

## 7. Packages / API

- No new NuGet packages.
- API routes unchanged: `GET /`, `GET /health` (phase marker updated). Port **5288**. No DB.

## 8. Tests

| Suite | Passed | Failed | Skipped |
|---|---:|---:|---:|
| ExItS.Platform.UnitTests | 67 | 0 | 0 |
| ExItS.ArchitectureTests | 15 | 0 | 0 |
| **Total** | **82** | **0** | **0** |

| Command | Exit |
|---|---:|
| `dotnet restore ExItS.slnx` | 0 |
| `dotnet build ExItS.slnx -c Release` | 0 (0 warnings, 0 errors) |
| `dotnet test ExItS.slnx -c Release --no-build` | 0 |

## 9. HealthCare freeze

`git ls-files HealthCare` empty; ignored; not in solution; unchanged.

## 10. Risks

- R-022 entitlement duration windows still open (categorical behavior implemented).
- No persistence uniqueness yet for product/plan codes.
- Billing/payment still absent by design.
- R-016 remote empty; not pushed.

## 11. Next work package

**P2-WP04 — HealthCare Contract Adaptation** (do not begin until authorized).

## 12. Commit

| Field | Value |
|---|---|
| Hash | _(recorded after commit)_ |
| Message | `feat(platform): add products plans entitlement foundation` |
