# POS warehouse stock requests (V1)

Retail branches request replenishment from a configured supply warehouse. The stock request is a workflow document only — it never mutates on-hand quantities by itself.

## Flow

```text
Retail branch                Supply warehouse              Destination branch
─────────────                ────────────────              ──────────────────
Create StockRequest
  status = Pending
        ──────────────────►  Review / approve lines
                             (approvedQuantity per line)
                             Approve → Approved
                             Start preparing → Preparing
                             Dispatch stock
                               creates InventoryTransfer
                               dispatches transfer
                               StockRequest → InTransit
                                               ──────────────────► Receive transfer
                                                                    StockRequest →
                                                                    Fulfilled /
                                                                    PartiallyFulfilled
```

Optional warehouse path: **Approve & prepare** runs approve then start-preparing in one UI action.

Decline (reject) and cancel remain available before dispatch:

| Actor | Allowed statuses |
|---|---|
| Warehouse (source) | Decline while `Pending` |
| Retail (destination) | Cancel while `Pending`, `Approved`, or `Preparing` |

## Explicit non-effect

**StockRequest does not mutate stock.**

Quantity changes happen only when the linked `InventoryTransfer` is:

1. **Dispatched** — source / org sellable decreases (`TransferOut`)
2. **Received** — destination credited for actual received qty (`TransferIn`)

Approved quantities on the request only constrain what the dispatch step puts on the transfer.

## Status map (UI tabs)

| Retail tab | Statuses |
|---|---|
| Submitted | `Pending` |
| In progress | `Approved`, `Preparing` (legacy `InProgress`) |
| In transit | `InTransit` |
| Completed | `Fulfilled`, `PartiallyFulfilled`, `Rejected`, `Cancelled` |

| Warehouse tab | Statuses |
|---|---|
| Incoming | `Pending` |
| Preparing | `Approved`, `Preparing` |
| Dispatched | `InTransit` |
| History | completed statuses above |

## API surface (React client)

| Method | Path | Effect |
|---|---|---|
| POST | `/api/v1/pos/inventory/stock-requests` | Create |
| POST | `.../{id}/approve` | Body `{ lineApprovals: [{ productId, approvedQuantity }] }` |
| POST | `.../{id}/prepare` | Start preparing |
| POST | `.../{id}/dispatch` | Create+dispatch transfer; returns `InventoryTransferDto` |
| POST | `.../{id}/reject` | Decline with reason |
| POST | `.../{id}/cancel` | Destination cancel |
| POST | `.../{id}/fulfill-transfer` | Legacy; delegates to dispatch |

DTO fields used by the detail UI include `approvedQuantity` on lines, `approvedBy` / `approvedAtUtc`, `preparingStartedBy` / `preparingStartedAtUtc`, `dispatchedBy` / `dispatchedAtUtc`, and `linkedInventoryTransferId`.

## Prerequisites

- Destination branch has at least one **active warehouse** supply route.
- Acting branch on approve/prepare/dispatch must be the requested source warehouse.
- Mutations are online-only with POS idempotency headers (`stock_request.approve` / `.prepare` / `.dispatch` / …).
