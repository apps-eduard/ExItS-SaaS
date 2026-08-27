# Idempotency Model

**Status:** Planning baseline (BNPL-00)  
**Implementation present:** No  
**Related:** BNPL-D-00-23; portfolio async/idempotency guidance

## Mandatory stable identities

| Operation | Stable identity (intent) |
|---|---|
| Financing request | FinancingRequestId + client idempotency key |
| Approval / acceptance | Decision id / acceptance id |
| Commerce sale intent | SaleIntentId shared across BNPL ↔ Commerce |
| Sale finalization | Commerce SaleId + finalize idempotency key |
| Financing activation | Activation bound to CommerceSaleId (1:1 intent) |
| Repayment | RepaymentId + idempotency key |
| Merchant settlement | SettlementId + idempotency key |

## Forbidden outcomes under retry / timeout

- Duplicate financing  
- Duplicate POS sale  
- Double inventory deduction  
- Duplicate repayment  
- Duplicate settlement  

## Patterns (do not copy POS blindly)

| Pattern | Use |
|---|---|
| Client-generated idempotency keys | Mutations |
| Server durable idempotency records | Exact replay / conflict detection |
| GET / status reconciliation | Ambiguous network outcomes |
| Target-state operations | Where suitable (e.g. ensure activated for SaleId) |

## Ambiguous outcome reconciliation

When the caller does not know if finalize succeeded:

1. Do not blindly retry a new sale intent  
2. Query status by SaleIntentId / idempotency key  
3. Activate financing only when Commerce reports committed sale  
4. If sale committed and financing missing, complete activation idempotently  
5. If financing ACTIVE and sale missing, escalate as inconsistency (should be prevented by orchestration order)

Reference portfolio guidance: `docs/Product-Foundation/async-events-idempotency-and-resilience.md` (planning — adapt, do not violate BNPL ownership).
