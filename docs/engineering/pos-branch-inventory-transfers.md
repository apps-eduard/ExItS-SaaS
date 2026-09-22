# POS branch inventory transfers

Intra-organization branch-to-branch inventory transfer is an accountability workflow, not a quantity copy and not a second product catalog.

## Audit result

Existing POS inventory was organization-wide (`InventoryAccount` per org+product). Platform already owns `OrganizationBranch`. There was no transfer aggregate, no per-branch sellable account, and no POS `branches` table (architecture forbids one).

## Model

```text
Organization
  Product (shared definition)
    InventoryAccount          org sellable on-hand (sales / PO / counts)
    InventoryBranchBalance    per-branch accountability overlay
    InventoryTransfer         draft → in transit → partial receipts → received / closed / cancelled
    InventoryTransferReceipt  per receive wave (multi-receipt)
```

Do not duplicate `CatalogProduct` because stock exists in more than one branch.

`PosBranchId` is an opaque Platform organization-branch GUID. POS stores it; it does not FK across databases.

## Lifecycle

| Status | Meaning | Stock effect |
|---|---|---|
| Draft | Not dispatched | None |
| InTransit | Dispatched, nothing received yet | Source dispatched: org sellable on-hand decreases (`TransferOut`); destination unchanged |
| PartiallyReceived | At least one receipt; outstanding quantity remains | Each receipt wave credits destination for **receive-now** qty only (`TransferIn` with `SourceId = receiptId`) |
| Received | All sent quantity received | Final; no outstanding |
| ClosedWithDiscrepancy | Remaining outstanding closed with reason | Final; shortage on lines via `ClosedQty` + discrepancy fields |
| Cancelled | Draft or in-transit cancel | Draft: no stock. In-transit: `TransferCancelRestore`. Not allowed after receiving has started |

**PartiallyReceived is open and actionable** while `OutstandingQty > 0`. It is not grouped with completed history in dashboards or list filters.

There is no per-line reject. Fewer units arrived is recorded across one or more receipts, then optionally **Close remainder** with `ShortShipment`, `Damaged`, `LostInTransit`, `WrongItem`, or `Other`.

## Source dispatch

Authorized source-branch users with `ManageInventory` create a draft, then dispatch.

Rules: same organization, source ≠ destination, quantity > 0, product belongs to the organization, acting branch must be the source, insufficient available stock is rejected. Duplicate product lines are rejected (not silently merged).

On dispatch:

- Transfer number `TR-YYYYMMDD-NNNNNN` is allocated under an advisory lock inside a serializable transaction.
- Sent quantities freeze.
- Source `InventoryBranchBalance` is seeded if missing as `orgOnHand − other branch balances`.
- Org `InventoryAccount` decreases so in-transit stock cannot be sold.
- Destination on-hand is not increased.

## Destination receiving (multi-receipt)

The destination branch receives in **waves**. Each API call sends **receive-now** quantities (not cumulative totals).

Per line at receive time:

- `SentQty` (immutable)
- `ReceivedQty` (cumulative across receipts)
- `OutstandingQty` = sent − received − closed
- Request line `ReceivedQty` = quantity for **this receipt only** (must be &gt; 0 and ≤ outstanding)

While status is `InTransit` or `PartiallyReceived`, destination may submit additional receipts until outstanding is zero (→ `Received`) or call **Close remainder** (→ `ClosedWithDiscrepancy`).

Close remainder (`POST .../{id}/close-remainder`):

- Allowed only in `PartiallyReceived` with outstanding &gt; 0.
- Requires a discrepancy reason per outstanding line (or transfer-level default).
- Sets `ClosedQty` on lines; does not post extra `TransferIn` for closed quantity.

Only actual received quantities become destination stock. Missing quantities are not auto-returned to the source. Resolve leftovers via adjustment/reconciliation or close remainder.

Receiving a product that has no destination branch balance initializes that balance for the **same** organization product.

## Stock requests (multi-transfer fulfillment)

A stock request may be fulfilled by **multiple** inventory transfers linked via `StockRequestId`.

Authoritative per-product dispatchability:

```text
ReceivedQty        = Σ ReceivedQty across non-cancelled linked transfers
OpenInTransitQty   = Σ OutstandingQty on transfers in InTransit or PartiallyReceived
RemainingToDispatch = MAX(0, ApprovedQty − ReceivedQty − OpenInTransitQty)
```

**Quantity still outstanding on an open InTransit or PartiallyReceived transfer counts as already committed toward the stock request and cannot be dispatched again.**

Examples:

| Situation | Received | Open in transit | Remaining to dispatch |
|---|---:|---:|---:|
| Sent 100, received 70, PartiallyReceived | 70 | 30 | **0** (blocked) |
| Close remaining 30 → ClosedWithDiscrepancy | 70 | 0 | **30** (may dispatch Transfer #2) |
| Transfer #2 received 30 | 100 | 0 | 0 → Fulfilled |

- Status (`PartiallyFulfilled`) is based on **actual received** qty, not dispatched qty. It does **not** mean “safe to dispatch all outstanding.”
- `ClosedWithDiscrepancy` shortages do **not** count as received and are **not** open in-transit.
- Draft transfers do not reduce RemainingToDispatch (no stock effect); canceling an InTransit transfer releases its coverage.
- `LinkedInventoryTransferId` remains the first dispatched transfer for backward compatibility; reads and recalculation use `ListByStockRequestId`.
- `DispatchStockRequest` builds lines from RemainingToDispatch only and rejects when that is zero for all lines (except idempotent re-dispatch before any receipt).

## Authorization

- Same `OrganizationId` on every row.
- Source membership/permission for create/dispatch/cancel.
- Destination membership/permission for receive and close remainder.
- No cross-organization transfers.
- Personal users have no organization/branch scope for these APIs.
- Cashiers follow existing `ViewInventory` / `ManageInventory` grants.
- Server never trusts client-supplied organization/branch without `X-Pos-Organization-Id` / bearer + `X-Pos-Branch-Id` authorization.

## Offline and idempotency

Transfers are **online-only**, matching current inventory writes. Drafts are not queued for offline sync. Correctness beats pretending a transfer exists on another branch's device.

Mutations accept optional `Idempotency-Key` + `X-Pos-Payload-Hash`. Dispatch of an already in-transit transfer is a no-op. Additional receive waves are allowed while status is actionable; completed transfers reject receive. Unique filtered index `ux_stock_movements_inventory_transfer_source` plus serializable transactions prevent double stock effects. Each receipt uses its own `receiptId` as the `TransferIn` movement source id.

## Ledger

Every quantity change is a `StockMovement`:

| Type | Effect |
|---|---|
| TransferOut | Source / org −sent |
| TransferIn | Destination / org +received for this receipt wave (received &gt; 0 only); source id = receipt id |
| TransferCancelRestore | Source / org +sent when cancelling in-transit |

Shortage is **not** a zero-effect movement (`ck_stock_movements_quantity_effect_nonzero`). It remains on `inventory_transfer_lines` as `ClosedQty` / difference after close remainder.

## API / UI

- `GET/POST /api/v1/pos/inventory/transfers`
- `POST .../{id}/dispatch|receive|cancel|close-remainder`
- React: Inventory → Transfers — list filters include PartiallyReceived (open) and ClosedWithDiscrepancy (final); detail shows receive remaining, close remaining, per-line outstanding; receive screen shows sent / previously received / outstanding / receive now.

## Notifications

POS does not write Platform notifications. `IInventoryTransferAlertSink` records scoped alerts (destination on dispatch, source on receive/partial). Production registration is `NoOpInventoryTransferAlertSink`.

## Owner acceptance checklist

Device Verified: **No** until the owner performs this on a real device.

### Full receipt (single wave)

1. Create Branch A and Branch B under the same organization.
2. Product Coke exists.
3. Branch A Coke stock = 100.
4. Branch B Coke stock = 20.
5. Branch A creates a transfer of 30 Coke to Branch B.
6. Confirm Branch B does **not** immediately become 50.
7. Dispatch the transfer.
8. Confirm status is In Transit.
9. Switch to Branch B user context.
10. Open Incoming Transfers.
11. Confirm the TR appears.
12. Receive 30 (receive now = outstanding).
13. Confirm Branch A = 70.
14. Confirm Branch B = 50.
15. Confirm history shows transfer out/in (one receipt).

### Partial receipt with second wave

1. Branch A sends Coke 20, Sprite 10, Water 30.
2. Branch B receives Coke 20, Sprite 8, Water 30 in one receipt (Sprite receive now = 8).
3. UI shows Sprite outstanding 2; status Partially Received.
4. Branch B receives Sprite 2 in a second receipt **or** closes remainder 2 with reason.
5. Destination gains only 20 / 8 / 30 (plus any second-wave Sprite).
6. No automatic +2 at source.

### Close remainder

1. After partial receipt with outstanding, choose Close remaining.
2. Enter reason per line (e.g. Short shipment).
3. Status becomes Closed with Discrepancy; outstanding zero.

### Zero received line

1. Send Product A = 10.
2. Destination omits line or receive now = 0 on all lines (submit fails validation).
3. To record total loss, receive 0 is not valid in one wave — close remainder after a partial workflow or receive other lines first as applicable.

### Idempotency

1. Complete a receipt wave.
2. Retry the same receive payload with the same idempotency key.
3. Stock does not increase again.

### Isolation

1. Transfer to a branch of another organization is rejected.
2. Receiving as an unauthorized branch/user is rejected.

Later overlay: when a product tracks expiration, transfer lines carry `SourceLotId` and snapshotted expiry/lot number so destination receiving does not collapse lots. See [pos-expiration-aware-inventory.md](pos-expiration-aware-inventory.md).

## Explicit exclusions

Peer-to-peer branch sync, claims module, automatic return of shortages, inventory-by-register, POS branches table, Redis/message broker, production-ready auth, Device Verified.
