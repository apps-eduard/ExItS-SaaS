# ADR-001 — Product identity and database name

**Status:** Accepted (PLM-DOC-01)
**Date:** 2026-08-19
**Decisions:** **PLM-D-00-01** Closed; **PLM-D-00-02** Closed for **logical** database name only

---

## Context

Pinoy Loan Manager needed a stable product code for future Platform catalog registration and a stable logical database name for product data authority. Hosting modes (hosted SaaS, dedicated, on-prem) must not create separate product identities or source forks.

---

## Decision

| Item | Value |
|---|---|
| Display name | Pinoy Loan Manager |
| Repository directory | `PinoyLoanManager` |
| Product code / slug | `pinoy-loan-manager` |
| Logical product database name | `ExItS_PinoyLoanManager` |

The product code is approved for **future** Platform catalog registration. Catalog registration itself is **not** performed in this package.

The database name is approved as the **logical PLM database authority**.

The same product code and logical database name apply to hosted, dedicated, and on-prem modes. Hosting mode does not change the product name or data authority.

---

## Consequences

**Closed**

- PLM-D-00-01 — product code / slug
- PLM-D-00-02 naming portion — logical database name `ExItS_PinoyLoanManager`

**Still deferred (PLM-D-00-02 remainder and later WPs)**

- actual database creation
- PostgreSQL schema
- connection configuration
- partitioning, stamps, placement
- backup implementation
- migrations

This ADR is **not** implementation evidence. No database is created here.
