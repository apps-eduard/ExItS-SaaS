# Audit and History Baseline

**Status:** Planning baseline (BNPL-00)  
**Implementation present:** No  

## Auditable events (intent)

Application created · eligibility result · offer issued · customer acceptance · approval/decline · activation (with CommerceSaleId) · repayment posted · repayment reversed/corrected · overdue marked · settlement events · status transitions · grant/config changes  

## Rules

- Audit is append-oriented / immutable history for operational accountability.  
- Audit is not a substitute for business state.  
- Do not allow silent deletes of financial history.  
- Actor, org, timestamp, correlation/idempotency identifiers should be capturable when implemented.
