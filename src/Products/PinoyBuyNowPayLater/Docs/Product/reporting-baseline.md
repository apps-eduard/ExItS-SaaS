# Reporting Baseline

**Status:** Planning baseline (BNPL-00)  
**Implementation present:** No  

## Merchant reports (intent)

- Financed sales  
- Approvals / declines  
- Active plans  
- Due today  
- Overdue  
- Repayments  
- Settlement status (when settlement model exists)  

## Customer reports (intent)

- Active plans  
- Next due  
- Outstanding balance  
- Payment history  
- Completed plans  

## Platform / BNPL audit views (intent)

- Application  
- Approval  
- Customer acceptance  
- Activation (with CommerceSaleId)  
- Repayments  
- Reversals / corrections  
- Settlement  
- Status transitions  

## Rules

- Audit history ≠ editable business state.  
- Report access requires BNPL grants (and customer self-scope for customer views).  
- Do not pull POS inventory ledgers into BNPL reports as SoR; may show CommerceSaleId references.
