# Pinoy Pawn Manager — BNPL Boundary

> Architecture index: [README.md](README.md)  
> Product definition: [../product-definition.md](../product-definition.md)

| Field | Value |
|---|---|
| Status | PPM-00 planning |
| Implementation | None |
| Last updated | 2026-08-27 |
| Principle | `PPM_IS_BNPL_MODULE` = **NO** |

## Verdict

**PPM is not BNPL with collateral.** Goods **direction** is opposite.

| Product | Goods direction | Financing object |
|---|---|---|
| **BNPL** (future / separate) | Goods go **to** the customer | Finance a **purchase** |
| **PPM** | Goods come **into** pawnshop custody | Finance against **pledged collateral** |

Do not model PPM as “BNPL + hold the item.” That inverts custody, risk, redemption, and disposition.

## Domain contrast

| Concern | BNPL | PPM |
|---|---|---|
| Customer receives goods | Yes (purchase delivery) | No — shop takes custody of pledged item |
| Collateral / pledge | Not the defining model | Defining model |
| Appraisal of customer-owned item | Not core | Required before offer |
| Vault / storage locations | Not core | Required |
| Redemption | Pay installments for purchased goods | Pay to recover pledged item |
| Unredeemed outcome | Collection / write-off patterns differ | Disposition / possible Commerce handoff (Open) |

## Isolation rules

- `DIRECT_BNPL_DB_ACCESS` = **NO**  
- No shared EF entities or cross-product FKs  
- No BNPL module nesting under PPM (or vice versa)  
- No assumption that BNPL installment engines equal pawn renewal/redemption  

## Allowed future relationships (contract only)

Any later cross-product reference must use approved contracts and Guids only. None are authorized in PPM-00.

## Related

- [plm-boundary.md](plm-boundary.md)
- [pos-commerce-boundary.md](pos-commerce-boundary.md)
- [../product-definition.md](../product-definition.md)
- [../risks-and-decisions.md](../risks-and-decisions.md)
