# P29-WP09 — Migration, Backup/Restore & DB Operations Hardening

| Field | Value |
|---|---|
| Status | **Partial** |
| Device Verified | **No** |
| Production Backup/Restore Proven | **No** |
| Production Ready | **No** |

## Migrations added this phase

- Platform: `StrengthenBranchDeliveryPolicyTenantIntegrity`
- POS: `StrengthenCustomerOrderTenantAndMoneyIntegrity`

## Validated

- Migrations generated via EF tooling; snapshots updated.
- Release builds compile against new snapshots.

## Migration verification evidence

Clean apply / rollback / re-apply and constraint corruption drills for the WP09 migrations (plus WP11 closeout migrations) are recorded in **[P29-WP11](P29-WP11-database-verification-and-constraint-closeout.md)** — not duplicated here.

## Not validated here

- Production `pg_dump` rehearsal — Phase 14 Production exit criteria remain open.
- No `Database.Migrate()` on production startup paths introduced.

## Exact next

Keep Phase 14 Production backup incomplete until its own criteria; see WP11 for Testcontainers migration evidence.
