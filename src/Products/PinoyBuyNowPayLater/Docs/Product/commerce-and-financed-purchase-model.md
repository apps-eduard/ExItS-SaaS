# Commerce and Financed Purchase Model

**Status:** Planning baseline (BNPL-00)  
**Implementation present:** No  
**Related:** BNPL-D-00-05–07, BNPL-D-00-09–10, BNPL-D-00-21–24

## Ownership split

| Concern | Owner |
|---|---|
| Product catalog, prices (current), SKU | Commerce / POS |
| Branch stock levels and movements | Commerce / POS |
| Authoritative commercial sale + sale lines | Commerce / POS |
| Financing application, approval, agreement | BNPL |
| Financed-purchase immutable snapshot | BNPL |
| Installment schedule, repayments, overdue | BNPL |
| Merchant settlement state | BNPL (funding model Open — BNPL-D-00-08) |

BNPL obtains product details, current price (where appropriate), branch availability, and stock validation **only** through approved Commerce/POS contracts/APIs. **No direct POS database reads.**

## Availability is not a reservation (BNPL-D-00-24)

Example:

1. BNPL UI loads iPhone 17 available = 9  
2. Another cashier sells 8 → actual available = 1  
3. BNPL customer attempts qty 2  

Final backend commerce sale must **fail** or require quantity correction. Initial stock display ≠ final stock guarantee. BNPL must not independently decrement stock.

## Canonical financed purchase flow

```text
Customer selected
↓
Organization + Branch selected
↓
products selected
↓
current availability displayed (informational)
↓
purchase amount calculated
↓
BNPL financing request created
↓
eligibility / terms evaluated
↓
customer accepts terms if required
↓
BNPL status = APPROVED_PENDING_SALE (name may refine)
↓
authoritative final stock check (Commerce)
↓
Commerce sale created/finalized
↓
inventory stock movements committed (Commerce)
↓
commerce SaleId recorded on financing
↓
BNPL financing becomes ACTIVE
↓
installment schedule becomes collectible
```

**Rule:** ACTIVE requires successful commerce sale — not approval alone (BNPL-D-00-07).

## Dual entry paths (same sale engine)

### Path A — POS first

```text
POS Checkout → Customer → Payment method = BNPL
→ hand off financing request → BNPL approval
→ commerce sale completion → financing ACTIVE
```

### Path B — BNPL first

```text
BNPL experience → merchant/branch/customer
→ browse eligible commerce products → financing request
→ approval → invoke authoritative commerce sale
→ inventory update → financing ACTIVE
```

Both paths **must** converge on the same authoritative commerce sale/inventory logic. Do not build competing sale engines (BNPL-D-00-10).

## Contract boundary (required intent)

Commerce should expose (names illustrative):

- Product/catalog read for org/branch-eligible items  
- Branch availability read  
- Sale intent / finalize financed sale (idempotent) with stock check  
- Sale status query for reconciliation  

BNPL should expose:

- Create/accept financing request  
- Approval decision  
- Activation bound to CommerceSaleId  
- Status query for reconciliation  

Exact API shapes are deferred to BNPL-06/07; ownership rules are not.

## Financed purchase snapshot (BNPL-D-00-09)

Conceptual (not final schema):

**FinancingPlan**

- FinancingPlanId  
- OrganizationId  
- BranchId (originating branch for physical goods)  
- Customer / Personal identity references (contracts)  
- CommerceSaleId  
- Financed amount, down payment, financed principal  
- Agreement date, terms, schedule reference  

**Financed item snapshot**

- ProductId / reference  
- Product name at purchase  
- SKU/reference where appropriate  
- Quantity, unit  
- Unit price at purchase, line total  
- Discounts if relevant  

If POS later changes name/price/SKU presentation, signed BNPL history must remain unchanged.

## BNPL vs Utang

| | Business Utang | BNPL |
|---|---|---|
| Nature | Merchant-managed informal/store credit | Structured financing product |
| Agreement | Simpler debt / relationship | Defined financing agreement |
| Eligibility | Merchant judgment in POS flow | Explicit eligibility / approval lifecycle |
| Schedule | Often simple balance | Installment schedule |
| Settlement | Merchant credit ledger in POS | Separate settlement concern (model Open) |
| Domain merge? | **Forbidden** | **Forbidden** |

Shared primitives (decimal money, idempotency) may be evaluated later — not shared operational tables.

## BNPL vs Pinoy Loan Manager

| | PLM | BNPL |
|---|---|---|
| Origin | Loan / financing **release** | Commerce **purchase** |
| Inventory | Not retail POS inventory SoR | Relies on Commerce stock |
| Collections | PLM collector architecture | Evaluate separately — do not auto-import |
| Domain merge? | **Forbidden** | **Forbidden** |

BNPL ≠ PLM with a shopping screen. Do not reference PLM projects/domain entities merely to reuse code. Generic primitives can be evaluated later.

## Organization and branch

- BNPL entitlement belongs to an **Organization**.  
- Branches matter for physical-product sales: Branch A stock ≠ Branch B stock.  
- Financing plans retain both OrganizationId and originating BranchId.  
- Do not use organization-wide inventory totals as branch availability.
