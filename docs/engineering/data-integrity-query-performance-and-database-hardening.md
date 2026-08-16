# Data Integrity, Query Performance & Database Hardening

Cross-cutting engineering guidance for Phase 29. Complements [data-ownership](data-ownership.md) and [data-authority-matrix](data-authority-matrix.md).

## Boundaries (unchanged)

- **Platform DB** owns identity, organizations, **OrganizationBranch**, plans, subscriptions, compliance readiness, Platform audit.
- **POS DB** owns operational catalog, sales, inventory, purchasing, customer orders, etc.
- POS stores Platform branch GUIDs as **opaque references** — no POS branch master table, no cross-database FKs.

## Hardening themes

1. Tenant and aggregate relational integrity (same-database composite FKs / CHECKs where safe).
2. Financial snapshot immutability and money as `numeric`/decimal.
3. Inventory reservation concurrency (no duplicate reserve/consume).
4. Eliminate N+1 and over-fetch on hot paths.
5. Database-side reporting aggregates.
6. Evidence-based indexes and pagination.
7. Concurrency + migration validation with PostgreSQL/Testcontainers (apply/rollback/re-apply evidence in [P29-WP11](../reports/P29-WP11-database-verification-and-constraint-closeout.md)).
8. Backup/restore evidence without falsely closing Phase 14 Production criteria.

## FUTURE tenant composite FK candidates

Do **not** silently expand composite tenant FKs beyond authorized packages. Remaining candidates (application-filtered today): `SaleLine`, inventory transfer/count lines, purchase-order / goods-receipt lines, and similar child rows.

## Performance honesty

Record Development/Testcontainers evidence. Do not claim Production-proven latency from a developer machine.
