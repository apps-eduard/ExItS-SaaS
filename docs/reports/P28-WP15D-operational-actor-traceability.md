# P28-WP15D — Operational Actor Attribution and Transaction Traceability

[Phase 28](../phases/phase-28-customer-ordering-pickup-and-delivery.md) | [WP15C staff branch ACL](P28-WP15C-staff-branch-authorization.md) | [Capability matrix](../engineering/organization-branch-capability-matrix.md) | [Sales buyer/actor model](../engineering/sales-buyer-party-model.md)

| Field | Value |
| --- | --- |
| Status | **Code Complete / Validation Pending** |
| Depends on | WP15A–C on `origin/main` |
| Closes | Fulfillment handoff + stock-count create + provider-finalization actor gaps |

## Goal

Every meaningful POS operational mutation must be traceable to the authenticated human operator or an explicit **System** actor — without a generic audit table substituting for authoritative transaction fields.

## Audit-first result

Most POS domains already carried actor provenance (`RecordedBy`, `AcceptedBy`, `StartedBy`, transfer dispatch/receive actors, shift/register actors, etc.) resolved server-side via `PosOrganizationScope.TryGetActorId`.

**Real gaps filled in WP15D:**

| Area | Before | After |
| --- | --- | --- |
| Customer order fulfillment handoffs (`mark-ready`, `out-for-delivery`, `delivered`, `collected`) | Status/timestamp only | `*AtUtc` + `*By` on order aggregate; API requires authenticated actor |
| Stock count draft create | No creator actor | Nullable `CreatedBy` on `StockCount`; POST `/stock-counts` requires actor |
| Electronic payment provider finalization | `CreatedBy` stayed cashier; no system marker | `ProviderFinalizedBySystem` bool on `PaymentAttempt` (no fake user GUID) |

**Unchanged (already solid):** sale checkout/void, sale returns, shifts/register cash events, stock adjustments, stock count start/complete/cancel, inventory movements, transfer dispatch/receive, customer order accept/reject/cancel/complete.

## Persistence / migration

**Migration:** `20260819071116_AddOperationalActorTraceabilityFields`

| Table | Columns | Legacy |
| --- | --- | --- |
| `pos.customer_orders` | `ready_*`, `out_for_delivery_*`, `delivered_*`, `collected_*` | Nullable — no backfill |
| `pos.stock_counts` | `created_by` | Nullable — historical counts remain null |
| `pos.payment_attempts` | `provider_finalized_by_system` (default `false`) | Existing rows remain `false` |

No guesswork actor backfill. Reversal/void paths continue to preserve original actor plus reversing actor on domain records.

## Server rules

- Actor resolved from authenticated identity (`TryGetActorId` / Bearer introspect / Dev header in Testing only).
- Client-supplied arbitrary `UserId` is **not** accepted on mutation bodies.
- Provider webhooks/simulation set `ProviderFinalizedBySystem = true` while retaining initiating cashier on `CreatedBy`.
- Branch truth unchanged from WP13–15C: physical/money/stock events use validated branch context; transfer receive remains destination-actor scoped.

## API / DTO

- Fulfillment endpoints under `POST .../mark-*` now require actor (same pattern as accept/reject/complete).
- `CustomerOrderDto` (detail) exposes fulfillment actor timestamps/ids; list DTOs remain lean.
- `PaymentAttemptDto` exposes `ProviderFinalizedBySystem`.

No MAUI/Web UI changes — actor display remains a future detail/history enhancement.

## Tests (Release)

| Suite | Scope | Result |
| --- | --- | --- |
| `ExItS.PinoyBusinessPOS.UnitTests` | `OperationalActorTraceabilityTests`, `OperationalActorEndpointGuardTests`, updated domain tests | **53 passed** (filtered WP15D-related) |

Covers: authenticated actor persisted, empty actor rejected, legacy null provenance valid, provider system flag, endpoint guards for fulfillment + stock-count create.

## Explicit exclusions

- Generic POS audit log table
- Actor name snapshot denormalization on list endpoints (N+1 avoidance)
- UI actor chips on primary lists
- Platform `AuditActorType` replication inside POS domain (boolean system marker used where needed)

**Related (separate concern):** [P28-WP15E](P28-WP15E-governance-audit-trail.md) records **Platform governance** mutations in append-only `platform.audit_records` — not a substitute for operational actor fields on POS transactions documented above.

## Readiness

Actor gaps on fulfillment, stock-count creation, and provider finalization are closed at the domain/API/persistence layer. Production readiness still requires PostgreSQL migration apply/rollback validation and broader Release suite sign-off on `ExItS.slnx`.
