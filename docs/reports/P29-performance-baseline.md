# P29 Performance Baseline (Development Evidence)

> Development/Testcontainers evidence only. **Not** Production proven.

| Field | Value |
|---|---|
| Environment | Developer workstation + unit-test fakes; PostgreSQL Testcontainers where noted |
| Dataset | Synthetic unit fixtures (not STANDARD 100k profile) |
| Date | 2026-08-16 |

## Improvements with code evidence

| Scenario | Before | After |
|---|---|---|
| ListBranches + policies | 1 + N policy queries | 1 branches + 1 bulk policies |
| Dashboard period totals / payment breakdown | Hydrate sales (+lines path) then GroupBy | SQL aggregate query methods |
| Customer “My Orders” | Existing org-scoped indexes only | Partial indexes on buyer ids + created_at DESC |

## Limitations

- No EXPLAIN (ANALYZE, BUFFERS) captured on STANDARD profile in this pass.
- Sales report product/category detail still materializes lines.
- Latency p50/p95 not claimed.

## WP13 SMOKE EXPLAIN follow-up

[P29-WP13](P29-WP13-concurrency-and-postgresql-execution-plan-validation.md) / [snippets](P29-WP13-explain-plan-snippets.md) captured Testcontainers `EXPLAIN (ANALYZE, BUFFERS)` for sales history, payment-attempt lookups, inventory, buyer orders, and dashboard aggregates. **Still not** Production / STANDARD latency proof.

## Exact next

Optional larger STANDARD profile EXPLAIN; keep Production backup incomplete.
