# Connected PO Return Policy

**Status:** Implemented (local commit pending push)  
**Scope:** Pinoy Business POS voluntary merchandise returns after good quantity is received on connected purchase orders.

## Policy model

Hierarchy (most specific wins):

1. Supplier organization default (`OrganizationConnectedCommerceSettings`)
2. Category override (`organization_connected_commerce_category_return_rules`)
3. Catalog product override (`products.return_policy_*`)

Modes: `UseDefault=0`, `Custom=1`, `NonReturnable=2`.

### Organization defaults (compatibility)

| Field | Default | Notes |
|-------|---------|-------|
| ReturnsAllowed | true | Voluntary returns allowed |
| ReturnWindowDays | null | Unlimited |
| ReceivingIssueWindowDays | 2 | Stored only — **never** blocks Goods Receipt posting |
| RequireReturnApproval | true | Informational; physical ReturnBatch still awaits seller receipt |

## Snapshot at GRN

On connected `ReceivePurchaseOrder`, each goods-receipt line with good `QuantityReceived > 0` creates a `connected_po_return_eligibility_buckets` row with snapshotted policy + `ReceivedAtUtc`. Partial receipts create independent buckets/windows.

## Voluntary return eligibility

```text
AvailableReturnQty =
  sum(remaining qty on voluntarily eligible buckets)
  capped by (ReceivedQty − SumReturnedByPurchaseOrderLine)
```

FIFO allocation by `ReceivedAtUtc` when requesting a return. Allocations persist in `connected_po_return_allocations`.

## Receiving issues (unchanged)

Damaged / missing / wrong / expired remain receiving-discrepancy / seller-review flow.  
`ReturnRequested` from that flow **bypasses** NonReturnable and return window (not voluntary policy). ReturnBatch remains physical-return authority.

## Explicit exclusions

- No redesign of receiving-issue flow
- ReceivingIssueWindowDays not enforced against GRN posting
- RequireReturnApproval does not skip AwaitingSellerReceipt
- Non-connected POs unchanged
