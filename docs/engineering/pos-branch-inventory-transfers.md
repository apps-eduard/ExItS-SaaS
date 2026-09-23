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

- Transfer number `TR-YYMMDD-NNN` (see [pos-transaction-reference-numbering.md](./pos-transaction-reference-numbering.md)) is allocated under an advisory lock inside a serializable transaction. Replacement children use `{root}-Rn` (e.g. `TR-260922-001-R1`); `RootTransferId` is authoritative for family membership.
- Sent quantities freeze.
- Source `InventoryBranchBalance` is seeded if missing as `orgOnHand − other branch balances`.
- Org `InventoryAccount` decreases so in-transit stock cannot be sold.
- Destination on-hand is not increased.

## Destination receiving (multi-receipt)

The destination branch receives in **waves**. Each API call sends **receive-now** quantities (not cumulative totals).

**Receipt classification is immutable.** Once a wave records Good / Damaged / Missing / Other for a quantity, that classification is not rewritten by later custody or inspection.

Per line at receive time:

- `SentQty` (immutable)
- `ReceivedQty` (cumulative **good/sellable** qty across receipts — never includes damaged)
- `ClosedQty` (cumulative damaged + missing closed on receive or via close remainder)
- `WaivedQty` (accepted shortage / accepted damage / accepted other — fulfillment waiver, not “received good”)
- `OutstandingQty` = sent − received − closed
- Each receive wave classifies **this wave only** against current outstanding:
  - `GoodQty` → increases `ReceivedQty`; posts destination `TransferIn` (physical + sellable)
  - `DamagedQty` → increases `ClosedQty`; posts `TransferDamageHold` + parks `DamagedQuantity` (physical + damaged, **sellable +0**)
  - `MissingQty` + `ExpectedLater` → stays open in transit (`OpenInTransit`)
  - `MissingQty` + `RequestReplacement` / `AcceptShortage` → closes missing; Accept increases `WaivedQty`
  - When damaged or missing is used, `GoodQty + DamagedQty + MissingQty` (+ Other) must equal outstanding for that line in that wave

When outstanding reaches zero: all good → `Received`; any closed qty → `ClosedWithDiscrepancy` (**UI: “Received with discrepancy”**). Completing one physical shipment does **not** complete the StockRequest / transfer family when replacement quantity remains.

Only **good** received quantities become destination sellable stock. Damaged received is physical inventory but never sellable inventory.

Close remainder (`POST .../{id}/close-remainder`):

- Allowed only in `PartiallyReceived` with outstanding &gt; 0.
- Requires a discrepancy reason per outstanding line (or transfer-level default).
- Sets `ClosedQty` on lines; does not post `TransferIn` for closed quantity.

Receiving a product that has no destination branch balance initializes that balance for the **same** organization product.

## Inventory buckets

Branch `InventoryBranchBalance`:

| Bucket | Meaning |
|---|---|
| OnHand (Physical) | Physical units at the branch |
| Available (Sellable) | OnHand − Reserved − PendingReturn − InspectionHold − Damaged |
| Damaged | Confirmed non-sellable physical stock |
| InspectionHold | Temporary non-sellable hold (e.g. returned damage at **source**) |

Invariant: `Reserved + PendingReturn + Damaged + InspectionHold ≤ OnHand`. All buckets ≥ 0.

## Good vs damaged receive semantics

| Classification | Physical | Sellable | Movement |
|---|---|---|---|
| GOOD | +qty | +qty | `TransferIn` |
| DAMAGED | +qty | +0 | `TransferDamageHold` + `DamagedQuantity` |
| MISSING | no change | no change | none (disposition on receipt/line) |
| OTHER | discrepancy; not sellable unless an existing authoritative rule says so | | |

**Damaged received is physical inventory but never sellable inventory.**

## Damage acceptance vs replacement (fulfillment)

Separate from custody. At receive time the receiver chooses:

| Fulfillment | Effect |
|---|---|
| Accept damage / no replacement | `WaivedQty` += damaged; replacement demand = 0 |
| Request replacement | replacement demand = damaged qty; RemainingToDispatch includes it immediately |

Do **not** wait for destination inspection before exposing replacement demand. Do **not** convert accepted damage to sellable. Do **not** remove damage history.

## Keep vs return custody

| Custody | After receive | Next step |
|---|---|---|
| Keep at destination | Physical + Damaged at destination; no dest re-inspect | Remains damaged until a separate audited reclassification (out of scope) |
| Return to source | Same buckets at destination until return dispatch | Dest: `TransferDamageReturnOut` (Physical −, Damaged −). Source receive: `TransferDamageReturnIn` (Physical +, InspectionHold +) |

**CUSTODY** (Keep vs Return) and **FULFILLMENT** (Replace vs Accept) are independent.

## Source inspection after physical return

Only the **source** inspects returned damaged goods.

Example: return 5 → RecoveredSellable 2 + ConfirmedDamaged 3:

- Recovered: InspectionHold −2, Sellable +2 (`TransferDamageRecovery`)
- Confirmed: InspectionHold −3, Damaged +3 (`TransferDamageWriteOff`)

**Source inspection of returned damage does not satisfy destination demand.**

## Replacement family

Root transfer `TR-YYMMDD-NNN`. Children `TR-…-Rn` with `RootTransferId`. Preparing remaining StockRequest qty creates the next child. Family coverage uses the same formula as StockRequest dispatch.

## Stock requests (multi-transfer fulfillment)

A stock request may be fulfilled by **multiple** inventory transfers linked via `StockRequestId`.

Authoritative per-product coverage (query **and** prepare/dispatch — `StockRequestDispatchCoverage`):

```text
SatisfiedGood      = Σ ReceivedQty (good only) across non-cancelled linked transfers
OpenInTransit      = Σ OutstandingQty on InTransit / PartiallyReceived members
Waived             = Σ WaivedQty (accepted shortage + accepted damage + accepted other)
RemainingToDispatch = MAX(0, FulfillmentTarget − SatisfiedGood − OpenInTransit − Waived)
```

**Removed from replacement blocking:** UninspectedKeepHold, destination damaged hold, source recovered sellable, returned/confirmed damaged quantities. Damage physically present never counts as destination fulfillment.

Examples:

| Situation | Good | Open | Waived | Remaining |
|---|---:|---:|---:|---:|
| Sent 10, good 5, damaged accept 5 | 5 | 0 | 5 | **0** → Fulfilled |
| Sent 10, good 5, damaged replace 5 | 5 | 0 | 0 | **5** → Needs fulfillment |
| R1 in transit 5 | 5 | 5 | 0 | **0** (blocked until R1 settles) |
| Missing ExpectedLater 5 | … | 5 | 0 | blocked via OpenInTransit |
| Missing RequestReplacement 5 | … | 0 | 0 | Remaining includes 5 |

Status uses good + waived (not “pretend damaged was good received”):

- Target 10, Good 5, Damaged accepted 5 → **Fulfilled**
- Target 10, Good 5, Damaged replace 5 → **PartiallyFulfilled**
- Target 10, Good 5 + R1 good 5 → **Fulfilled**

### Prepare → dispatch (preferred source workflow)

1. `POST /api/v1/pos/inventory/stock-requests/{id}/prepare-transfer` — source branch + `ManageInventory`. Uses the same coverage service as detail queries. Starts preparing when approved; returns existing **Draft** when linked (idempotent); otherwise creates draft **without** dispatch.
2. `POST /api/v1/pos/inventory/transfers/{id}/dispatch` — explicit dispatch.
3. `POST .../stock-requests/{id}/dispatch` — **legacy** one-shot.

Source UI shows **Fulfill remaining {qty}** when `RemainingToDispatch > 0` on the source branch — for stock-request linked transfers and for direct branch transfers (prepare-remaining creates `…-Rn` draft).

`GET /api/v1/pos/inventory/stock-requests/{id}/activity` — chronological audit from request fields, linked transfers, receipts, and damage custody timeline.

## Movement / audit traceability

| Type | Physical | Sellable | Damaged / Hold |
|---|---|---|---|
| TransferOut | − | − | |
| TransferIn | + | + | good only |
| TransferDamageHold | + | 0 | Damaged + |
| TransferDamageReturnOut | − | 0 | Damaged − |
| TransferDamageReturnIn | + | 0 | InspectionHold + |
| TransferDamageRecovery | 0 | + | Hold − |
| TransferDamageWriteOff | 0 | 0 | Hold −, Damaged + |
| TransferCancelRestore | + | + | in-transit cancel |

UI presents bucket lines so `TransferDamageHold +5` is never shown as +5 sellable.

Fulfillment decisions (accept damage / request replacement) are on immutable receipt lines + custody rows (actor/timestamp) — not fake zero-qty movements (DB forbids zero quantity_effect).

## Reporting facts

Persisted / queryable without ambiguous inference:

| Fact | Source |
|---|---|
| Good received | Receipt line `QuantityReceived` / line `ReceivedQty` |
| Damaged received | Receipt line `QuantityDamaged` |
| Damaged accepted | `DamagedFollowUp=AcceptShortage` + line/custody `WaivedQty` |
| Replacement requested | `DamagedFollowUp=RequestReplacement` / custody `ReplacementDemandQty` |
| Missing / ExpectedLater / Accept | Receipt `QuantityMissing` + `MissingDisposition` |
| Damaged returned | Custody return timestamps + `TransferDamageReturnOut/In` |
| Recovered / confirmed at source | Custody `RecoveredSellableQty` / `ConfirmedDamagedQty` |
| Replacement dispatched / received | Child transfer Sent / Received |
| Outstanding replacement | Family `RemainingToDispatchQty` |
| Scope keys | org, branch, product, root transfer, physical transfer, date |

Family DTO also exposes: `SatisfiedAtDestinationQty`, `OpenInTransitQty`, `WaivedQty`, `RemainingToDispatchQty`, `DamageCustodies`, `FamilyMembers`.

## Worked example — Scenario 1 (Request replacement + Keep)

Source Apple = 100. Receiver Apple = 0. Send = 10.

After source dispatch: Source sellable = 90.

Receiver classifies Good = 5, Damaged = 5, **Request replacement**, **Keep at destination**:

| Bucket | Qty |
|---|---:|
| Physical | 10 |
| Sellable | 5 |
| Damaged | 5 |

Movements at receiver:

- Transfer in (good): Physical +5, Sellable +5
- Damaged transfer received: Physical +5, Sellable +0, Damaged +5

Original TR = Received with discrepancy. Stock request Needs fulfillment = 5. Source sees **Fulfill remaining 5** on Stock Request detail and on Transfer summary (plus **View stock request**).

Source prepares/dispatches `TR-…-R1` qty 5 → Source sellable = 85.

Receiver receives R1 all good 5:

| Bucket | Qty |
|---|---:|
| Physical | 15 |
| Sellable | 10 |
| Damaged | 5 |

Stock request = Fulfilled. Final: Source sellable 85; Receiver sellable 10, damaged 5, physical 15.

## Worked example — Apple 10 (coverage variants)

Same dispatch (Good 5 + Damaged 5), other fulfillment/custody choices:

**Accept damage / Keep:** Waived = 5, RemainingToDispatch = 0, StockRequest Fulfilled. Damaged 5 stays at destination. Damage history retained.

**Request replacement / Return to source:** RemainingToDispatch = 5 (same as Scenario 1). After return dispatch/receive, source inspects recovered vs confirmed damaged; source recovery never satisfies destination demand.

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

Mutations accept optional `Idempotency-Key` + `X-Pos-Payload-Hash`. Dispatch of an already in-transit transfer is a no-op. Additional receive waves are allowed while status is actionable; completed transfers reject receive. Unique filtered index `ux_stock_movements_inventory_transfer_source` plus serializable transactions prevent double stock effects. Each receipt uses its own `receiptId` as the `TransferIn` / damage hold movement source id.

## Ledger

Every quantity change is a `StockMovement` (see Movement / audit table above). Shortage is **not** a zero-effect movement (`ck_stock_movements_quantity_effect_nonzero`). It remains on receipt/lines as dispositions + `WaivedQty` / `ClosedQty`.

## API / UI

- `GET/POST /api/v1/pos/inventory/transfers`
- `POST .../{id}/dispatch|receive|cancel|close-remainder`
- Damage return: dispatch/receive return + source inspect endpoints
- React: Inventory → Transfers — family coverage Fulfillment (Good received / Still in transit / Needs fulfillment / Accepted·waived) + source-only **Fulfill remaining** and **View stock request**; status “Received with discrepancy”; Stock request detail → Needs fulfillment summary + **Fulfill remaining**
- Movement history shows Physical / Sellable / Damaged (/ Hold) for transfer damage types

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
3. Status becomes Received with discrepancy; outstanding zero.

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

Peer-to-peer branch sync, claims module, automatic return of shortages, inventory-by-register, POS branches table, Redis/message broker, production-ready auth, Device Verified, destination re-inspection of damaged transfer goods, full damaged-repair workflow.
