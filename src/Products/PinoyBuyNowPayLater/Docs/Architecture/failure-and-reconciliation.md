# Failure and Reconciliation

**Status:** Planning baseline (BNPL-00)  
**Implementation present:** No  
**Related:** BNPL-D-00-07, BNPL-D-00-22–23, BNPL-R-00-02, BNPL-R-00-07–08

## Failure matrix

### Case A — BNPL declined

- No completed commerce sale  
- No inventory deduction  
- No ACTIVE financing  

### Case B — Approved but final stock check fails

- No completed sale  
- Financing must not become ACTIVE  
- Approval must expire/cancel/release safely  

### Case C — Approved but Commerce unavailable before sale finalization

- No ACTIVE financing  
- Preserve safe pending state **or** cancel per documented policy (TTL Open)  
- Do not invent ACTIVE offline  

### Case D — Commerce commits sale but response to BNPL is lost

- Reconcile using stable transaction identity (sale intent / SaleId / idempotency key)  
- Never create duplicate sale or duplicate financing  
- GET/status reconciliation required  

### Case E — BNPL ACTIVE and POS later unavailable

Continue where POS data is not required:

- Installment schedules  
- Repayments  
- Balances  
- Overdue calculation  
- Collection workflows  
- Financing history / statements  

### Case F — POS changes product name or price later

- Existing financing agreement and snapshot must **not** mutate historically  

## Dependency matrix

| Operation | Commerce-dependent? |
|---|---|
| Browse availability / start financed purchase | Yes |
| Finalize sale / activate financing | Yes |
| Repayment posting | No |
| Schedule / balance / overdue | No |
| Collections queue | No |
| Settlement (when modeled) | Policy; not stock APIs |

## POS outage summary

| Scenario | Behavior |
|---|---|
| New financed purchase | Block or keep pending; never ACTIVE without sale |
| Existing ACTIVE plan | Financing-independent ops continue |
