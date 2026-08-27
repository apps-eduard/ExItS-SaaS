# Pinoy Buy Now Pay Later — Architecture

> Foundation: [exits-product-foundation-reference.md](../../../../docs/Product-Foundation/exits-product-foundation-reference.md)  
> Decisions: [risks-and-decisions.md](risks-and-decisions.md)

| Field | Value |
|---|---|
| Product | Pinoy Buy Now Pay Later |
| Status | Planning baseline (BNPL-00); Implementation Not Started |
| Last updated | 2026-08-27 |
| Implementation present | No |

## System context

```text
┌─────────────┐     identity / entitlement      ┌──────────────────┐
│  Platform   │◄────────────────────────────────┤  BNPL Product    │
└─────────────┘                                 │  (financing)     │
                                                └────────┬─────────┘
                                                         │ approved contracts
                                                         │ (no DB reads)
                                                ┌────────▼─────────┐
                                                │ POS / Commerce   │
                                                │ catalog, stock,  │
                                                │ authoritative    │
                                                │ sale             │
                                                └──────────────────┘
```

BNPL coordinates a financed purchase. It does **not** become the system of record for inventory or commercial sale lines.

## Isolation contract (required)

| Rule | Binding |
|---|---|
| Separate logical BNPL database | Required (name Open — BNPL-D-00-04) |
| No cross-product foreign keys | Required |
| No direct BNPL → POS DB reads | Required |
| No direct BNPL → Platform operational table reads | Required |
| No direct POS → BNPL DB writes | Required |
| OrganizationId / BranchId / ProductId / SaleId as identifiers only | Required |
| Independent product subscription | Required |
| Platform product access ≠ BNPL operational permission | Required |

Detail: [Architecture/persistence-and-database-boundary.md](Architecture/persistence-and-database-boundary.md).

## Inventory architecture (critical)

There must **never** be:

```text
POS inventory = 9
BNPL inventory = 10
```

for the same Organization + Branch + Product.

**Permanent rule (BNPL-D-00-05, BNPL-D-00-06):**

```text
Same Organization
+ Same Branch
+ Same Product
= Same authoritative inventory
```

Example:

1. iPhone 17, Branch A, qty = 10  
2. POS sells 1 cash → authoritative stock = 9  
3. BNPL UI shows available = 9  
4. BNPL-financed purchase completes for qty 2 → authoritative stock = 7  
5. POS sees 7  

BNPL **must not** maintain an independent stock ledger. Availability is obtained through approved Commerce/POS contracts. Initial UI display is **not** a reservation and **not** a final stock guarantee. Final stock validation occurs during authoritative commerce sale finalization. Detail: [Architecture/inventory-boundary.md](Architecture/inventory-boundary.md).

## Financed purchase orchestration

Canonical lifecycle principle:

```text
Customer + Org + Branch selected
→ products selected (availability displayed)
→ purchase amount calculated
→ BNPL financing request created
→ eligibility / terms evaluated
→ customer accepts terms if required
→ status = APPROVED_PENDING_SALE (or equivalent)
→ authoritative final stock check + commerce sale
→ inventory movements committed (Commerce)
→ CommerceSaleId recorded
→ BNPL financing ACTIVE
→ installment schedule collectible
```

**Critical rule (BNPL-D-00-07):** Financing must **not** become ACTIVE merely because credit was approved. Commerce sale success is required.

Both entry paths converge on the **same** authoritative commerce sale/inventory logic:

| Path | Description |
|---|---|
| **A — POS first** | Checkout → payment method BNPL → financing request → approval → commerce completion → ACTIVE |
| **B — BNPL first** | BNPL browse/select → financing → approval → invoke commerce sale → inventory update → ACTIVE |

Do **not** build competing POS Sale Engine and BNPL Sale Engine. Detail: [Architecture/commerce-pos-boundary.md](Architecture/commerce-pos-boundary.md), [Product/commerce-and-financed-purchase-model.md](Product/commerce-and-financed-purchase-model.md).

## Service dependency / outage boundary

| Operation class | Depends on Commerce/POS? | Behavior |
|---|---|---|
| New financed purchase (sale finalization) | **Yes** | If Commerce unavailable before sale: no ACTIVE financing; pending/cancel per policy |
| Catalog/availability browse | **Yes** (read contract) | Degrade or block browse when Commerce unavailable |
| Repayments / schedule / balances / overdue / collections / history | **No** | Continue for existing ACTIVE plans |
| Merchant settlement (when modeled) | Policy-dependent | Must not require POS stock APIs for customer balance math |

Detail: [Architecture/failure-and-reconciliation.md](Architecture/failure-and-reconciliation.md).

## Idempotency and distributed safety

Stable identities required for: financing request, approval/acceptance, commerce sale intent, sale finalization reference, financing activation, repayment, merchant settlement.

Network ambiguity must not cause duplicate financing, duplicate sale, double inventory deduction, duplicate repayment, or duplicate settlement.

Patterns: stable client/server IDs, idempotency keys, GET/status reconciliation, target-state operations where suitable. Do not copy POS offline outbox blindly. Detail: [Architecture/idempotency-model.md](Architecture/idempotency-model.md).

## Financed purchase snapshot

On activation, BNPL stores an **immutable** historical snapshot of financed items (name, SKU reference, qty, unit price, line totals, discounts as applicable) plus financing plan references (Org, Branch, customer refs, CommerceSaleId, amounts, terms). Later POS catalog/price/name changes must **not** mutate signed history (BNPL-D-00-09).

## Client runtime (proposed)

Web/PWA baseline: **ONLINE-ONLY** for business/financial mutations (BNPL-D-00-11). PWA may be installable with static-shell cache. No offline financing mutation queue in BNPL-00. Native/Capacitor offline is a future separate decision. Detail: [Architecture/web-pwa-runtime-policy.md](Architecture/web-pwa-runtime-policy.md).

## Explicit non-goals (architecture)

- BNPL-owned inventory ledger
- Direct cross-product EF/SQL
- Treating sale and financing as one aggregate that collapses ownership
- Claiming production auth or commercial transport beyond portfolio decisions
- Implementing BNPL-00 as code
