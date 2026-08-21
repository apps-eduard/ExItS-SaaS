# Suppliers and Connected Suppliers

## Manual / local suppliers

| Topic | Status | Evidence |
|-------|--------|----------|
| `Supplier` aggregate | PROVEN_CURRENT | Domain + `/api/v1/pos/suppliers` |
| Code / contact / active | PROVEN_CURRENT | Supplier fields + MAUI suppliers pages |
| Table | `pos.suppliers` | migration `AddPosSuppliers` |

## Connected ExItS suppliers (Organization ↔ Organization)

| Topic | Status | Evidence |
|-------|--------|----------|
| Relationship request / accept / decline / disconnect | PROVEN_CURRENT | Domain `ConnectedSuppliers`; statuses Pending, Active, Declined, Disconnected |
| Buyer vs supplier org | PROVEN_CURRENT | Relationship sides + counterparty snapshots |
| Exposure eligibility (Level 1) | PROVEN_CURRENT | `CanExposeToConnectedBuyers` / `SupplierProductExposure.IsExposed` |
| Sharing (Level 2) | PROVEN_CURRENT | `ConnectedBuyerProductShare.IsShared` |
| **EXPOSABLE ≠ SHARED** | PROVEN_CURRENT | Explicit two-level model; accept connection does **not** silently share all products |
| Default connected PO price | PROVEN_CURRENT | Product `DefaultConnectedPoPrice` |
| Buyer-specific price | PROVEN_CURRENT | `BuyerSpecificPoPrice` on share |
| Linked product adoption | PROVEN_CURRENT | `BuyerSupplierProductLink` + conversion metadata |
| Notifications | PROVEN_CURRENT | Org notification integration paths |
| Offline selective projection | PROVEN_CURRENT | Linked products + conversion in LocalStore; full catalog online/paged |

API: `/api/v1/pos/connected-suppliers/...`
Tests: ConnectedSuppliers unit tests, LocalStore connected supplier tests, MAUI guards.

## OWNER-CONFIRMED / invariants to preserve

1. EXPOSABLE ≠ SHARED
2. Accepting a supplier connection must not silently share all products
3. Inventory never changes on expose/share/price/connection events

## React

**PARTIAL** — RMAP-15 manual suppliers + RMAP-16 connected suppliers (relationship/share/catalog/links). Purchasing PO receive remains RMAP-17.
