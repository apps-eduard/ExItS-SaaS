# Commerce / POS Boundary

**Status:** Planning baseline (BNPL-00)  
**Implementation present:** No  
**Related:** BNPL-D-00-10, BNPL-D-00-12

## Authoritative commerce owner

**PinoyBusinessPOS** (current ExItS commerce product) owns catalog, inventory, and authoritative sales for BNPL-financed retail purchases unless a future approved commerce surface is explicitly authorized.

## Contract-only integration

```text
BNPL  ←→  approved Commerce APIs / contracts  ←→  POS operational DB
         (no direct DB access either direction for operational ownership)
```

## Required capabilities (intent)

| Capability | Direction | Notes |
|---|---|---|
| Catalog / product details | Commerce → BNPL | Read |
| Branch availability | Commerce → BNPL | Read; not a reservation |
| Finalize financed sale | BNPL → Commerce | Idempotent; stock check; returns SaleId |
| Sale status | Commerce → BNPL | Reconciliation |
| Return / restore stock | Commerce owns | BNPL notified via contract for financing impact |

## Dual paths

Path A (POS first) and Path B (BNPL first) converge on the same finalize-sale capability. See [Product/commerce-and-financed-purchase-model.md](../Product/commerce-and-financed-purchase-model.md).

## Visibility

| Direction | Documented intent |
|---|---|
| POS sale visible to BNPL | Via CommerceSaleId + status contract after finalize |
| BNPL-initiated sale visible to POS | Same authoritative sale record in Commerce — not a shadow sale |

## Forbidden

- BNPL duplicating POS sale posting logic against POS tables  
- POS writing BNPL financing rows directly  
- Shared EF DbContext across products  
