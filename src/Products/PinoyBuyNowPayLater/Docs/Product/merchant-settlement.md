# Merchant Settlement

**Status:** Planning baseline (BNPL-00)  
**Implementation present:** No  
**Related:** BNPL-D-00-08, BNPL-D-00-20, BNPL-R-00-05

## Separate financial concern

Customer buys a ₱10,000 product through BNPL. Architecture must separate:

| Ledger | Meaning |
|---|---|
| **Customer financing balance** | What the customer still owes under the agreement |
| **Merchant settlement balance** | What the merchant is owed / has been paid by the financing program |

Do **not** silently choose a regulated financial model.

## Open commercial / legal questions

Record — do not invent answers:

- When does the merchant consider the sale “paid” commercially?  
- Does ExItS BNPL fund/settle the merchant?  
- Does the merchant itself finance the transaction (merchant-funded BNPL)?  
- When is settlement created relative to ACTIVE?  
- What happens on cancellation/refund?  
- What happens if the customer later defaults?  

## Architecture requirement until decided

- Keep settlement entities/workflows **optional and gated** behind BNPL-D-00-08.  
- Do not implement platform lending balance sheet assumptions.  
- Customer repayment posting must remain valid even if settlement model is deferred.

## Explicit non-claims

This document does not authorize ExItS to operate as a licensed lender or payment institution.
