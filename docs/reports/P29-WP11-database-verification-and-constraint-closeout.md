# P29-WP11 — Database Verification & Constraint Closeout

| Field | Value |
|---|---|
| Status | **Code Complete / Validation Evidence Recorded** |
| Phase | Phase 29 |
| Starting SHA | `390178f39d74e37af8f8aa44d29b38612ca9d0cd` |
| Feature commits | `1212dcd0` (platform), `512f8749` (orders), `7a866a5b` (tests) |
| Docs commit | `5b25c586` (+ portfolio encoding repair stamp) |
| Cross-link | Migration verification residual from [P29-WP09](P29-WP09-migration-backup-restore-and-db-operations.md) |
| Device Verified | **No** |
| Browser Verified | **No** |
| Production Ready | **No** |
| Production Backup/Restore Proven | **No** (Phase 14 criteria remain open) |

## Delivered

### Platform migration `CloseoutBranchDeliveryPolicyConstraints`

- CHECK `ck_branch_delivery_policies_free_threshold_nonneg`: `free_delivery_threshold IS NULL OR free_delivery_threshold >= 0`
- Dropped redundant unique index `ux_organization_branches_id_organization_id` while retaining alternate key `AK_organization_branches_id_organization_id` for composite tenant FKs

### POS migration `StrengthenCustomerOrderLineTenantForeignKeys`

- Alternate key `AK_customer_orders_id_seller_organization_id`
- Alternate key `AK_products_id_organization_id`
- Composite FK `fk_customer_order_lines_orders_tenant` `(order_id, seller_organization_id)` → `customer_orders (id, seller_organization_id)`
- Composite FK `fk_customer_order_lines_products_tenant` `(product_id, seller_organization_id)` → `products (id, organization_id)`

### Integration tests (PostgreSQL Testcontainers)

| Suite | Result | Counts |
|---|---|---|
| Platform `FullyQualifiedName~P29Wp11` | **PASS** | Failed 0, Passed 3, Skipped 0, Total 3 |
| POS `FullyQualifiedName~P29Wp11` | **PASS** | Failed 0, Passed 3, Skipped 0, Total 3 |
| Platform.Api / Pos.Api Release build | **PASS** | 0 errors |

### POSTGRESQL MIGRATION EVIDENCE

| Scenario | Platform | POS |
|---|---|---|
| clean → latest | **PASS** | **PASS** |
| pre-P29 → latest (via Strengthen then closeout / line-tenant) | **PASS** | **PASS** |
| latest → rollback (before Strengthen / money strengthen) | **PASS** | **PASS** |
| rollback → latest (re-apply) | **PASS** | **PASS** |

Migration names: Platform `StrengthenBranchDeliveryPolicyTenantIntegrity`, `CloseoutBranchDeliveryPolicyConstraints`; POS `StrengthenCustomerOrderTenantAndMoneyIntegrity`, `StrengthenCustomerOrderLineTenantForeignKeys`. Pre-P29 anchors: `AddBirRegistrationReadinessProfiles` / pre-money-strengthen POS migration (see Testcontainers classes).

### CONSTRAINT EVIDENCE

| Constraint | Result |
|---|---|
| CustomerOrder party XOR | **PASS** |
| money identity | **PASS** |
| coordinate pairs | **PASS** |
| Branch policy tenant FK | **PASS** |
| FreeDeliveryThreshold (NULL/0 accept; negative reject) | **PASS** |
| CustomerOrderLine order tenant FK | **PASS** |
| CustomerOrderLine product tenant FK | **PASS** |
| Valid policy / order / line inserts | **PASS** |

### INDEX EVIDENCE

| Index | Result |
|---|---|
| Branch `(id, organization_id)` equivalent physical indexes BEFORE | **2** (`AK_…` + `ux_…`) |
| Branch `(id, organization_id)` equivalent physical indexes AFTER | **1** (`AK_…` only) |
| Customer order history indexes (`customer_user_created_at`, `customer_buyer_org_created_at`) | **Yes** (schema verification; not production latency proof) |

## Redundant-index finding

WP09 `StrengthenBranchDeliveryPolicyTenantIntegrity` created both:

1. Unique constraint / alternate key `AK_organization_branches_id_organization_id`
2. Explicit unique index `ux_organization_branches_id_organization_id` on the same `(id, organization_id)` columns

WP11 removed the redundant physical index via `CloseoutBranchDeliveryPolicyConstraints` and switched the EF model to `HasAlternateKey` only (no duplicate `HasIndex`).

## Explicit exclusions / FUTURE

- **Do not refactor** other line aggregates yet. Documented FUTURE tenant composite-FK candidates: `SaleLine`, inventory transfer/count lines, purchase-order/goods-receipt lines, and similar same-database child rows that today rely on single-column product/order FKs plus application filters.
- Production `pg_dump` / restore rehearsal remains Phase 14 — not closed here.
- No `Database.Migrate()` on production API startup paths introduced.
- Device / Browser / Production Ready gates unchanged (**No**).

## Residuals

- Broader requested filter strings also match unrelated historical `*Migration*` API tests (pre-existing failures outside WP11 scope).
- WP03 CustomerOrder→Sale money residual, WP08 load harness, and SMOKE EXPLAIN latency baselines remain open.
- Phase 29 remains **Open / Partial Closeout**.

## Exact next

Optional concurrent Accept integration; EXPLAIN baselines on SMOKE; keep Phase 14 Production backup incomplete until its own criteria.
