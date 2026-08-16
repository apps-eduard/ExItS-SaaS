# P29-WP13 — Concurrency & PostgreSQL Execution Plan Validation

| Field | Value |
|---|---|
| Status | **Code Complete / Validation Evidence Recorded** |
| Phase | Phase 29 (Open / Partial Closeout — continued; **not** Phase 30) |
| Starting SHA | `7b75f44d58b8db0ba8b94290baf77c4d21e3d42d` |
| Feature commits | `387bb275`, `349fbd8f`, `48004459` |
| Device Verified | **No** |
| Browser Verified | **No** |
| Production Ready | **No** |
| Production Backup/Restore Proven | **No** |
| Production Payment Ready | **No** |
| FakePaymentGateway | **Still Active** |
| Real Provider / Real Money | **No** |

## Why Phase 29 / WP13

Phase 29 remained Open / Partial. WP12 delivered electronic reservation semantics with sequential race coverage. WP13 strengthens **true concurrent** PostgreSQL execution and captures **EXPLAIN (ANALYZE, BUFFERS)** for scoped hot paths. Builds WP08 concurrency evidence without opening Phase 30.

## Scope

1. Barrier-synchronized concurrent HTTP races against Testcontainers PostgreSQL
2. DB-truth assertions (Npgsql): sale/attempt/reservation/inventory/stock_movements
3. EXPLAIN evidence for representative application-shaped queries
4. Minimal concurrency fixes discovered by races (no speculative redesign)

## Concurrent scenarios (how synchronized)

| Scenario | Sync | Final invariant |
|---|---|---|
| Paid vs Cancel | `Barrier(2)` + `Task.WhenAll` | Provider Paid wins; Sale Completed once; Consumed; one SaleDeduction |
| Paid vs Expire | `Barrier(2)` | Paid wins; no release+consume corruption |
| Duplicate Paid storm | `Barrier(10)` | Exactly-once Paid/Complete/Consume/movement |
| Last-stock two buyers | `Barrier(2)` concurrent Card checkouts | Exactly one reserves; loser InsufficientStock; Paid → on_hand 0 |
| Retry re-reserve vs competing buyer | `Barrier(2)` after A Released | Exactly one obtains stock; no negatives |
| Reconcile vs Paid webhook | `Barrier(2)` | Paid + Completed once; one movement |

## Concurrency fixes (evidence-backed)

True overlapping Paid/Cancel exposed races. Minimal fixes:

- Cancel / webhook paths use **serializable** transactions via existing UoW helper
- Paid webhook **retries** after serialization conflict; refuses downgrade of Paid attempts
- Sale update guards refuse Completed/Consumed downgrades
- Map EF concurrency / serialization failures to `PersistenceConflictException` (safe HTTP conflict)
- Domain: authoritative Paid may override Failed/Cancelled/Expired on equal event sequence when needed for race recovery

No new migration. No Redis / global mutex as correctness authority.

## EXPLAIN evidence

Captured by `P29Wp13PostgresqlExplainEvidenceTests` (~representative seeded rows; snippets: [P29-WP13-explain-plan-snippets.md](P29-WP13-explain-plan-snippets.md)).

| Query | Relevant index | Observed plan (SMOKE) | Assessment |
|---|---|---|---|
| Sales history org + recorded_at DESC LIMIT | `ix_sales_org_recorded_at` | Index Scan Backward | Acceptable |
| PaymentAttempt idempotency | `ux_payment_attempts_org_idempotency` exists | Org index + filter at ~400 rows/org | Acceptable at SMOKE; composite available for larger orgs |
| Provider reference | `ux_payment_attempts_provider_reference` | Unique Index Scan | Acceptable |
| Active attempts for sale | `ix_payment_attempts_org_sale_status` exists | Org index + filter at SMOKE | Acceptable at SMOKE |
| Inventory org+product | `ux_inventory_accounts_org_product` | Unique Index Scan | Acceptable |
| CustomerOrder buyer history | Phase 29 buyer partial indexes | Index path | Acceptable |
| Dashboard payment breakdown aggregate | `ix_sales_org_*` | Aggregate over org filter | Acceptable (server-side) |

**Performance fix required:** No. Seq/org-index choices at SMOKE scale are not defects; no speculative indexes added.

## Validation

| Suite | Result |
|---|---|
| `FullyQualifiedName~P29Wp13` | **PASS** (7) |
| `FullyQualifiedName~P29Wp12` regression | **PASS** (11) |
| Pos Api/Domain/Application/Infrastructure Release build | **PASS** |

## Explicit exclusions / residuals

- Production backup/restore still unproven (Phase 14)
- No STANDARD 100k latency p50/p95 claim
- No Device/Browser verification
- WP03 CustomerOrder→Sale residual remains
- Phase 29 remains **Open / Partial Closeout**

## Exact next

Optional WP08 broader load harness; keep Phase 14 Production backup incomplete; do **not** integrate a real payment provider; do **not** open Phase 30.
