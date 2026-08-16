# P3-WP02 — Trials and Subscription Lifecycle

## 1. Assignment

| Field | Value |
|---|---|
| Phase | Phase 3 — Portfolio Billing, Plans and Entitlements |
| Work package | P3-WP02 — Trials and Subscription Lifecycle |
| Status | Ready for Review |
| Branch | `main` |
| Date | 2026-07-29 |

## 2. Summary

Implemented persistent Platform organizations (subscription ownership only) and the full commercial subscription lifecycle: trials, paid activation **without payment processing**, grace period, past due, suspension, reactivation, cancellation, and expiration. Added EF migration `AddPlatformOrganizationsAndSubscriptions`, repositories, commands/queries, development-stage REST API, unit/architecture/integration tests, and isolated PostgreSQL migration validation.

**Activation in this work package is a commercial lifecycle command, not payment processing.**

**Security note:** Organization and subscription mutation endpoints are **development-stage and unauthenticated** (R-045 expanded). Not production-ready.

## 3. Persistence result

| Item | Value |
|---|---|
| Organizations | `platform.organizations` — id, display_name, unique slug, status, created/updated UTC, `xmin` concurrency |
| Subscriptions | `platform.subscriptions` — org FK, product_code, plan/plan_version/trial FKs, status, trial/paid/grace/suspend/cancel/past-due/expired timestamps, aggregate_version, `xmin` |
| Migration | `AddPlatformOrganizationsAndSubscriptions` (`20260729173841_AddPlatformOrganizationsAndSubscriptions`) |
| Prior migration | `InitialPlatformCatalog` retained |
| Active-like uniqueness | Partial unique index `ux_subscriptions_one_active_like` on `(organization_id, product_code)` WHERE status ∈ Trialing, Active, GracePeriod, PastDue, Suspended |
| Concurrency | PostgreSQL `xmin` row version on organizations and subscriptions; conflicts → `application.concurrency_conflict` |
| Check constraints | `ck_subscriptions_trial_range`, `ck_subscriptions_paid_range` |
| Explicitly excluded tables | users, memberships, payments, invoices, entitlement snapshots, POS |

## 4. Trial lifecycle

- Eligibility: organization must exist and be **Active**; product for the plan must be **Active**; trial definition **Active**; published plan version; no existing active-like subscription for org+product.
- Duration: `TrialDefinition.Duration` (`TimeSpan` / `duration_ticks`) — **configurable**; no `TimeSpan.FromDays(90)`.
- Start: `Subscription.StartTrial` + `StartTrialSubscription` command; UTC trial start/end.
- Expiration: explicit `ExpireSubscription` command; `SubscriptionLifecycleEvaluator` can recommend expire when trial end has passed (no Hangfire/scheduler).
- Duplicate prevention: application `ExistsActiveLike` + DB partial unique index → 409 `ActiveSubscriptionConflict`.
- Repeat-trial policy: **safe default** — historical Cancelled/Expired allowed; one active-like slot only. Permanent one-trial-ever policy **not** enforced (open decision / risk).
- **R-035** remains **Open** (three calendar months / EOM rule undecided).

## 5. Subscription lifecycle

| Transition | Behavior |
|---|---|
| Activate | Trialing → Active with paid period; commercial command only — no payment/invoice rows |
| Grace | Active → GracePeriod; grace end must not precede paid-period end |
| Past due | → PastDue; `PastDueAtUtc` recorded |
| Suspend | → Suspended; `SuspendedAtUtc` recorded |
| Reactivate | From Suspended (period optional); from Grace/PastDue **requires** new paid period; Cancelled/Expired **terminal** |
| Cancel / Expire | Terminal; history retained; new commercial relationship needs new `SubscriptionId` |

## 6. Payment boundary

Confirmed absent: payment collection, invoices, payment tables, GCash, gateways, webhooks, QR codes, card fields. Activation does not verify payment.

## 7. Application capability

- Repositories: `IPlatformOrganizationRepository`, `ISubscriptionRepository` (EF implementations; no generic repository).
- Commands: Create/Suspend organization; StartTrial; Activate; EnterGrace; MarkPastDue; Suspend; Reactivate; Cancel; Expire.
- Queries: get by id; current for org+product; list by org/product/status; expiring trials; grace; past-due (paginated).
- Evaluator: `SubscriptionLifecycleEvaluator` (pure domain; uses caller clock).
- Conflicts: active-like, slug, concurrency, invalid transition → stable Application/Domain error codes; API maps to ProblemDetails (409 where conflict/transition).

## 8. API capability

Development-stage, unauthenticated:

**Organizations**

- `POST /api/v1/platform/organizations`
- `GET /api/v1/platform/organizations/{organizationId}`
- `POST /api/v1/platform/organizations/{organizationId}/suspend`

**Subscriptions**

- `GET /api/v1/platform/subscriptions?status=&productCode=`
- `GET /api/v1/platform/subscriptions/{subscriptionId}`
- `GET /api/v1/platform/organizations/{organizationId}/subscriptions`
- `GET /api/v1/platform/organizations/{organizationId}/subscriptions/current?productCode=`
- `POST /api/v1/platform/organizations/{organizationId}/subscriptions/trials`
- `POST /api/v1/platform/subscriptions/{subscriptionId}/activate`
- `POST /api/v1/platform/subscriptions/{subscriptionId}/grace-period`
- `POST /api/v1/platform/subscriptions/{subscriptionId}/past-due`
- `POST /api/v1/platform/subscriptions/{subscriptionId}/suspend`
- `POST /api/v1/platform/subscriptions/{subscriptionId}/reactivate`
- `POST /api/v1/platform/subscriptions/{subscriptionId}/cancel`
- `POST /api/v1/platform/subscriptions/{subscriptionId}/expire`

Confirmed absent: payment, invoice, GCash, entitlement-delivery, and POS routes.

Phase marker: `P3-WP02-trials-subscription-lifecycle`. Retained: `GET /`, `GET /health`.

## 9. Migration result

Isolated Docker `postgres:18` on `127.0.0.1:5434` (container `exits-platform-p3wp02`):

```text
dotnet ef database update
  → Applied InitialPlatformCatalog + AddPlatformOrganizationsAndSubscriptions
dotnet ef database update InitialPlatformCatalog
  → Dropped organizations + subscriptions (7 catalog tables remain)
dotnet ef database update
  → Re-applied AddPlatformOrganizationsAndSubscriptions (9 platform tables)
```

History table: `public.__EFMigrationsHistory`. Partial unique index verified. No payment/user/membership/entitlement/POS tables.

## 10. Build and tests

| Command | Result |
|---|---|
| `dotnet build ExItS.slnx -c Release` | Exit 0; 0 warnings; 0 errors |
| `dotnet test ExItS.slnx -c Release --no-build` | Exit 0 |

| Suite | Passed | Failed | Skipped |
|---|---:|---:|---:|
| ExItS.Platform.UnitTests | 127 | 0 | 0 |
| ExItS.ArchitectureTests | 32 | 0 | 0 |
| ExItS.Platform.IntegrationTests | 26 | 0 | 0 |
| **Total** | **185** | **0** | **0** |

## 11. Runtime validation

| Step | Result |
|---|---|
| API | `http://127.0.0.1:5288` |
| `GET /` | `phase=P3-WP02-trials-subscription-lifecycle` |
| `GET /health` | Healthy |
| Create org → start trial → get current | OK |
| Duplicate trial | **409** |
| Activate → grace → past-due → suspend → reactivate → cancel | OK |
| Terminal reactivate | **409** |
| Payment tables | None |
| Shutdown | Process stopped cleanly |

## 12. Security and authorization

- No credentials committed beyond local-dev Docker password pattern (Development / design-time only).
- No PHI / clinical entities in Platform persistence.
- No fake production authentication.
- Routes remain development-stage until auth WP (R-045, R-047+).

## 13. portfolio independence verification

- No unauthorized nested product tree is tracked
- `ExItS.slnx` contained only approved Platform projects
- Versioned Platform contract interfaces remained unchanged in purpose

## 14. Risks

| ID | Note |
|---|---|
| R-012 | Partially mitigated — subscriptions persist; billing collection still open |
| R-031 / R-032 | Identity/membership still not authenticated/persisted |
| R-035 | Still open — calendar EOM |
| R-045 | Expanded to org/subscription APIs |
| R-046 | Migration targeting discipline continues |
| R-047 | Manual activation ≠ payment verification |
| R-048 | No background scheduler — expiration requires explicit command |
| R-049 | Repeat-trial eligibility ambiguity (safe default only) |
| R-050 | Unsecured subscription mutation endpoints (prod gate) |

## 15. Git evidence

| Field | Value |
|---|---|
| Feature commit | `616d8ad1a76f02b6494b905549908c1a15e7f812` |
| Message | `feat(platform): implement trial and subscription lifecycle` |
| Hash-record commit | _(this docs commit)_ |

## 16. Next work package

**P3-WP03 — Manual Payment Activation** (do not begin until authorized).
