# Financing Lifecycle

**Status:** Planning baseline (BNPL-00)  
**Implementation present:** No  
**Related:** BNPL-D-00-07, BNPL-D-00-22–24

State **names** below are proposed for clarity. Implementation may refine identifiers after domain modeling; the **principles** are binding.

## Proposed minimal state machine

| State | Meaning |
|---|---|
| `DRAFT` | Application started; not submitted |
| `PENDING_ELIGIBILITY` | Awaiting eligibility evaluation |
| `OFFERED` | Terms offered to customer / merchant |
| `CUSTOMER_ACCEPTED` | Customer accepted offer (if required) |
| `APPROVED_PENDING_SALE` | Financing approved; commerce sale not yet finalized |
| `ACTIVE` | Commerce sale succeeded; schedule collectible |
| `PAID` | Financed obligation settled |
| `OVERDUE` | Active with past-due installments (may be flag + status — Open) |
| `CANCELLED` | Cancelled before ACTIVE (or per policy) |
| `VOIDED` | Voided after invalid/erroneous activation path (policy Open) |
| `DEFAULTED` / `WRITTEN_OFF` | Future / Open — do not implement without policy |

## Transition rules (intent)

| From | To | Condition |
|---|---|---|
| DRAFT | PENDING_ELIGIBILITY | Submit application |
| PENDING_ELIGIBILITY | OFFERED / CANCELLED / declined terminal | Eligibility result |
| OFFERED | CUSTOMER_ACCEPTED / CANCELLED | Acceptance or expiry |
| CUSTOMER_ACCEPTED | APPROVED_PENDING_SALE | Merchant/system approval if still required |
| APPROVED_PENDING_SALE | ACTIVE | **Commerce sale success** + snapshot recorded |
| APPROVED_PENDING_SALE | CANCELLED | Stock fail, timeout, Commerce unavailable per policy |
| ACTIVE | PAID | Outstanding balance zero per policy |
| ACTIVE | OVERDUE | Past-due detection |
| OVERDUE | ACTIVE / PAID | Cure / pay off |
| * | DEFAULTED / WRITTEN_OFF | **Open** policy only |

### Prohibited

- DRAFT/OFFERED → ACTIVE (skipping commerce)  
- APPROVED_PENDING_SALE → ACTIVE without CommerceSaleId  
- Reactivating VOIDED without controlled correction workflow  
- Mutating immutable snapshot after ACTIVE

## Per-state properties

| State | Money owed? | Commerce sale exists? | Inventory changed? | Repayments allowed? |
|---|---|---|---|---|
| DRAFT | No | No | No | No |
| PENDING_ELIGIBILITY | No | No | No | No |
| OFFERED | No | No | No | No |
| CUSTOMER_ACCEPTED | No | No | No | No |
| APPROVED_PENDING_SALE | No (commitment only) | No (yet) | No | No |
| ACTIVE | Yes (outstanding) | Yes | Yes (at activation) | Yes |
| PAID | No | Yes (historical) | Historical | No (except corrections Open) |
| OVERDUE | Yes | Yes | Historical | Yes |
| CANCELLED | No | No | No | No |
| VOIDED | Policy | Policy | Must reconcile with Commerce | Policy |

## Expiry of APPROVED_PENDING_SALE

If approved but sale cannot complete (stock fail, outage, abandonment), approval must **expire/cancel/release safely** so it cannot later activate against a different unintended sale. Exact TTL **Open** (product policy).
