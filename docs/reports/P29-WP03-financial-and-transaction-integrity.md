# P29-WP03 — Financial & Transaction Integrity

| Field | Value |
|---|---|
| Status | **Partial / Documented Residual** |
| Starting SHA | `fcc5eee1de074baadf5b2644ab1d6d1a3af22163` |
| Device Verified | **No** |
| Production Ready | **No** |

## Findings

- Sale / CustomerOrder money uses decimal/`numeric(18,2)` — preserved.
- CustomerOrder `total = merchandise_subtotal + delivery_fee` CHECK added in WP02 migration.
- Sale number and customer-order number allocation already use `pg_advisory_xact_lock`.
- Historical sale line snapshots remain authoritative for report money.

## Gap (blocking residual — not invented)

CustomerOrder completion consumes inventory reservation but does **not** automatically create a canonical POS `Sale` / retail payment / TaxDocument. Existing architecture treats Transaction Summary sales as the in-store checkout path. Converting CustomerOrder → Sale is **not** implemented in Phase 29; document as future product decision, not silent invention.

## Exact next

Keep residual open; do not invent Sale conversion without approved product semantics.
