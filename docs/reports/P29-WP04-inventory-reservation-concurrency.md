# P29-WP04 — Inventory Reservation Concurrency

| Field | Value |
|---|---|
| Status | **Implementation Complete / Validation Pending** |
| Phase | Phase 29 |
| Starting SHA | `fcc5eee1de074baadf5b2644ab1d6d1a3af22163` |
| Device Verified | **No** |
| Browser Verified | **No** |
| Production Ready | **No** |

## Delivered

- `IInventoryRepository.ExecuteWithProductReservationLocksAsync` — transaction + `pg_advisory_xact_lock` keyed by org+product, reload accounts inside lock.
- `CustomerOrderStockService` Accept/Release/Consume use locked reload path; order-level `StockReservationState` idempotency preserved.
- Accept / Reject / Cancel / Complete wrap stock + order update in `IPosUnitOfWork.ExecuteInSerializableTransactionAsync`.
- Unit tests assert lock acquisition and reserve idempotency.

## Residuals

- Concurrent accept under Testcontainers not executed in this pass (best-effort unit coverage only).
