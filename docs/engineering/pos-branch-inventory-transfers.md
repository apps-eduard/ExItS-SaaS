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
    InventoryTransfer         draft → in transit → received / partial / cancelled
```

Do not duplicate `CatalogProduct` because stock exists in more than one branch.

`PosBranchId` is an opaque Platform organization-branch GUID. POS stores it; it does not FK across databases.

## Lifecycle

| Status | Stock effect |
|---|---|
| Draft | None |
| InTransit | Source dispatched: org sellable on-hand decreases (`TransferOut`); destination sellable stock unchanged |
| Received | Destination credited only for actual received qty (`TransferIn`) |
| PartiallyReceived | Same as received; shortage stays on the transfer line |
| Cancelled | Draft: no stock. In-transit: `TransferCancelRestore` returns source qty. Not allowed after receiving has started |

There is no per-line reject. Fewer units arrived is `ReceivedQty` + shortage, not a product rejection.

## Source dispatch

Authorized source-branch users with `ManageInventory` create a draft, then dispatch.

Rules: same organization, source ≠ destination, quantity > 0, product belongs to the organization, acting branch must be the source, insufficient available stock is rejected. Duplicate product lines are rejected (not silently merged).

On dispatch:

- Transfer number `TR-YYYYMMDD-NNNNNN` is allocated under an advisory lock inside a serializable transaction.
- Sent quantities freeze.
- Source `InventoryBranchBalance` is seeded if missing as `orgOnHand − other branch balances`.
- Org `InventoryAccount` decreases so in-transit stock cannot be sold.
- Destination on-hand is not increased.

## Destination receiving

The destination branch must explicitly receive. Each line records SentQty (immutable), ReceivedQty (`0 ≤ received ≤ sent`), DifferenceQty, optional reason (`ShortShipment`, `Damaged`, `LostInTransit`, `WrongItem`, `Other`), ReceivedBy, ReceivedAt.

Only actual received quantities become destination stock. Missing quantities are not auto-returned to the source and are not invented at the destination. Resolve leftovers later with existing adjustment/reconciliation.

Receiving a product that has no destination branch balance initializes that balance for the **same** organization product.

## Authorization

- Same `OrganizationId` on every row.
- Source membership/permission for create/dispatch/cancel.
- Destination membership/permission for receive.
- No cross-organization transfers.
- Personal users have no organization/branch scope for these APIs.
- Cashiers follow existing `ViewInventory` / `ManageInventory` grants.
- Server never trusts client-supplied organization/branch without `X-Pos-Organization-Id` / bearer + `X-Pos-Branch-Id` authorization.

## Offline and idempotency

Transfers are **online-only**, matching current inventory writes. Drafts are not queued for offline sync. Correctness beats pretending a transfer exists on another branch's device.

Mutations accept optional `Idempotency-Key` + `X-Pos-Payload-Hash`. Dispatch of an already in-transit transfer is a no-op. A second receive is rejected (`InventoryTransferAlreadyReceived`). Unique filtered index `ux_stock_movements_inventory_transfer_source` plus serializable transactions prevent double stock effects.

## Ledger

Every quantity change is a `StockMovement`:

| Type | Effect |
|---|---|
| TransferOut | Source / org −sent |
| TransferIn | Destination / org +received (received > 0 only) |
| TransferCancelRestore | Source / org +sent when cancelling in-transit |

Shortage is **not** a zero-effect movement (`ck_stock_movements_quantity_effect_nonzero`). It remains on `inventory_transfer_lines`.

## API / UI

- `GET/POST /api/v1/pos/inventory/transfers`
- `POST .../{id}/dispatch|receive|cancel`
- MAUI: Inventory → Transfers (`/inventory/transfers`), create, detail, receive with confirm summary

## Notifications

POS does not write Platform notifications. `IInventoryTransferAlertSink` records scoped alerts (destination on dispatch, source on receive/partial). Production registration is `NoOpInventoryTransferAlertSink`.

## Owner acceptance checklist

Device Verified: **No** until the owner performs this on a real device.

### Full receipt

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
12. Receive 30.
13. Confirm Branch A = 70.
14. Confirm Branch B = 50.
15. Confirm history shows transfer out/in.

### Partial receipt

1. Branch A sends Coke 20, Sprite 10, Water 30.
2. Branch B receives Coke 20, Sprite 8, Water 30.
3. UI shows Sprite shortage 2.
4. Destination gains only 20 / 8 / 30.
5. Transfer is Partially Received.
6. Shortage remains auditable.
7. No automatic +2 appears.

### Zero received line

1. Send Product A = 10.
2. Destination enters Received = 0.
3. Destination stock does not increase.
4. Shortage = 10.

### Idempotency

1. Complete receipt.
2. Refresh/retry the same receive.
3. Stock does not increase again.

### Isolation

1. Transfer to a branch of another organization is rejected.
2. Receiving as an unauthorized branch/user is rejected.

Later overlay: when a product tracks expiration, transfer lines carry `SourceLotId` and snapshotted expiry/lot number so destination receiving does not collapse lots. See [pos-expiration-aware-inventory.md](pos-expiration-aware-inventory.md).

## Explicit exclusions

Peer-to-peer branch sync, claims module, automatic return of shortages, inventory-by-register, POS branches table, Redis/message broker, production-ready auth, Device Verified.
