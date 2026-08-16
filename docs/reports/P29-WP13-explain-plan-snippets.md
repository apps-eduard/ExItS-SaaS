# P29-WP13 — EXPLAIN plan snippets

Captured by `P29Wp13PostgresqlExplainEvidenceTests` (ANALYZE, BUFFERS).
Seq Scan on small/medium sets is acceptable evidence, not a failure.

## sales_history

```
Limit  (cost=0.27..8.29 rows=1 width=1692) (actual time=0.009..0.019 rows=50.00 loops=1)
Buffers: shared hit=5
->  Index Scan Backward using ix_sales_org_recorded_at on sales  (cost=0.27..8.29 rows=1 width=1692) (actual time=0.008..0.017 rows=50.00 loops=1)
Index Cond: (organization_id = '99f73058-74be-4df3-8100-e3cad547316e'::uuid)
Index Searches: 1
Buffers: shared hit=5
Planning:
Buffers: shared hit=4
Planning Time: 0.077 ms
Execution Time: 0.028 ms
```

## payment_attempt_idempotency

```
Index Scan using ix_payment_attempts_organization_id on payment_attempts  (cost=0.14..8.16 rows=1 width=4052) (actual time=0.005..0.042 rows=1.00 loops=1)
Index Cond: (organization_id = '99f73058-74be-4df3-8100-e3cad547316e'::uuid)
Filter: ((idempotency_key)::text = 'idem-99f7305874be4df38100e3cad547316e-0'::text)
Rows Removed by Filter: 399
Index Searches: 1
Buffers: shared hit=13
Planning:
Buffers: shared hit=10 read=1
Planning Time: 0.087 ms
Execution Time: 0.047 ms
```

## payment_attempt_provider_reference

```
Index Scan using ux_payment_attempts_provider_reference on payment_attempts  (cost=0.27..8.29 rows=1 width=4052) (actual time=0.010..0.010 rows=1.00 loops=1)
Index Cond: (((provider)::text = 'Fake'::text) AND ((provider_reference)::text = 'fake-ref-99f7305874be4df38100e3cad547316e-0'::text))
Index Searches: 1
Buffers: shared hit=3
Planning:
Buffers: shared hit=1
Planning Time: 0.038 ms
Execution Time: 0.015 ms
```

## active_attempts_for_sale

```
Index Scan using ix_payment_attempts_organization_id on payment_attempts  (cost=0.14..8.17 rows=1 width=4052) (actual time=0.004..0.037 rows=1.00 loops=1)
Index Cond: (organization_id = '99f73058-74be-4df3-8100-e3cad547316e'::uuid)
Filter: ((sale_id = 'bf42f0cc-2907-4ff6-85ae-2a0f04c3a992'::uuid) AND ((status)::text = ANY ('{Created,Pending,RequiresCustomerAction,Processing,PendingManualVerification}'::text[])))
Rows Removed by Filter: 399
Index Searches: 1
Buffers: shared hit=13
Planning:
Buffers: shared hit=40
Planning Time: 0.093 ms
Execution Time: 0.042 ms
```

## inventory_account_org_product

```
Index Scan using ux_inventory_accounts_org_product on inventory_accounts  (cost=0.15..8.17 rows=1 width=145) (actual time=0.004..0.004 rows=1.00 loops=1)
Index Cond: ((organization_id = '99f73058-74be-4df3-8100-e3cad547316e'::uuid) AND (product_id = '932595cc-a974-4d7c-94e3-b9bbf8e94fba'::uuid))
Index Searches: 1
Buffers: shared hit=2
Planning:
Buffers: shared hit=6
Planning Time: 0.043 ms
Execution Time: 0.009 ms
```

## customer_orders_buyer_history

```
Limit  (cost=0.26..8.28 rows=1 width=4501) (actual time=0.006..0.011 rows=20.00 loops=1)
Buffers: shared hit=22
->  Index Scan using ix_customer_orders_customer_user_created_at on customer_orders  (cost=0.26..8.28 rows=1 width=4501) (actual time=0.006..0.010 rows=20.00 loops=1)
Index Cond: (customer_platform_user_id = '76af4dfa-0a71-42b1-9032-33c252344abc'::uuid)
Index Searches: 1
Buffers: shared hit=22
Planning:
Buffers: shared hit=5
Planning Time: 0.057 ms
Execution Time: 0.018 ms
```

## dashboard_payment_method_aggregate

```
GroupAggregate  (cost=0.27..8.31 rows=1 width=122) (actual time=0.061..0.154 rows=3.00 loops=1)
Group Key: payment_method
Buffers: shared hit=54
->  Index Scan using ix_sales_org_payment_method on sales  (cost=0.27..8.29 rows=1 width=102) (actual time=0.005..0.103 rows=723.00 loops=1)
Index Cond: (organization_id = '99f73058-74be-4df3-8100-e3cad547316e'::uuid)
Filter: ((recorded_at_utc >= '2026-07-17 13:46:13.961533+00'::timestamp with time zone) AND ((status)::text = 'Completed'::text))
Rows Removed by Filter: 77
Index Searches: 1
Buffers: shared hit=54
Planning:
Buffers: shared hit=36 read=2
Planning Time: 0.099 ms
...
```

