# API and Contract Boundary

**Status:** Planning baseline (BNPL-00)  
**Implementation present:** No  

## Product API (intent)

BNPL owns a product API for financing operations (applications, approvals, plans, repayments, reports). No API project is created in BNPL-00.

## External contracts (intent)

| Contract | Counterparty |
|---|---|
| Identity / session / entitlement | Platform |
| Catalog / availability / financed sale finalize / sale status | Commerce / POS |
| Optional Personal presentation | Platform Personal surfaces |

## Rules

- DTOs must not expose EF entities across boundaries.  
- Contracts carry stable identifiers, not foreign DB rows.  
- Versioning and authn for inter-product calls are Open design details for BNPL-06/07 — ownership is fixed.  
- Fail closed when required context missing.
