# P29-WP02 — Tenant Isolation & Relational Integrity (Platform + POS)

| Field | Value |
|---|---|
| Status | **Implementation Complete / Validation Pending** |
| Phase | Phase 29 |
| Starting SHA | `fcc5eee1de074baadf5b2644ab1d6d1a3af22163` |
| Device Verified | **No** |
| Browser Verified | **No** |
| Production Ready | **No** |

## Platform

Migration: `StrengthenBranchDeliveryPolicyTenantIntegrity`

- Unique index / alternate key on `organization_branches (id, organization_id)`.
- `branch_delivery_policies` FK is now composite `(branch_id, organization_id)` → `organization_branches (id, organization_id)`.
- CHECK `ck_organization_branches_lat_long_pair` (both-or-neither coordinates).

## POS

Migration: `StrengthenCustomerOrderTenantAndMoneyIntegrity`

- Party XOR CHECK (`ck_customer_orders_party_xor`).
- Money identity CHECK (`ck_customer_orders_money_identity`: `total = merchandise_subtotal + delivery_fee`).
- Delivery destination / branch lat-long pair CHECKs.
- Customer-order buyer partial indexes (also WP07).

## Residuals

- No additive fulfillment **address** snapshot columns beyond existing delivery destination / branch coordinate snapshots (already on `CustomerOrderDeliverySnapshot`). Documented skip — not invasive domain/Rehydrate change; no UI work.
- Migration apply / rollback / re-apply against PostgreSQL pending.
- TaxDocument runtime unchanged.
