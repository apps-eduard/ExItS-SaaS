# Pinoy Pawn Manager — PLM Boundary

> Architecture index: [README.md](README.md)  
> Product definition: [../product-definition.md](../product-definition.md)

| Field | Value |
|---|---|
| Status | PPM-00 planning |
| Implementation | None |
| Last updated | 2026-08-27 |
| Principle | `PPM_IS_PLM_MODULE` = **NO** |

## Verdict

**PPM is not PinoyLoanManager with photos.**

PinoyLoanManager (PLM) is a separate first-class product focused on financing / repayment schedules. Physical pawn custody, pledged-item evidence, vault storage, and redemption-by-collateral-return are **not** PLM’s core domain.

PPM must not:

- Be implemented as a PLM module, feature flag, or nested project under PLM  
- Reuse PLM loan entities, schedules, or repayment engines as pawn tickets  
- Share PLM’s operational database or EF models  
- Read PLM tables or create cross-product foreign keys (`DIRECT_PLM_DB_ACCESS` = **NO**)

## Domain contrast

| Concern | PLM | PPM |
|---|---|---|
| Core idea | Unsecured / general lending & repayment | Collateral-secured pawn with physical custody |
| Physical item custody | Not the core domain | **Required** (`PPM_OWNS_CUSTODY` = YES) |
| Photos / evidence | Optional / not defining | Identifying + appraisal evidence for pledged items |
| Ticket / agreement | Loan agreement / schedule | Pawn agreement / pawn ticket + snapshots |
| Redemption | Pay down / settle loan | Settle obligation **and** separate physical release |
| Disposition of goods | N/A as retail handoff core | Unredeemed disposition workflow (legal Open) |

## Anti-patterns (forbidden)

| Anti-pattern | Why forbidden |
|---|---|
| “PLM loan + attach collateral photo” | Collapses distinct legal/operational domains |
| Copying PLM installment engines into PPM tickets | Wrong lifecycle; maturity/renewal/redemption differ |
| Shared `Loans` table or FK across products | Violates portfolio DB isolation |
| Staff grants that assume PLM roles authorize PPM | Product-local grants only (**PPM-D-00-18**) |

## Allowed future relationships (contract only)

If Product Owner later authorizes a **presentation** or **referral** link (e.g. “customer also has a PLM loan”), that must be:

- An approved cross-product **contract** using stable Guids  
- Never a shared database join  
- Never implicit entitlement to mutate the other product  

No such integration is in scope for PPM-00.

## Risk reference

- **PPM-R-00-01** — Confusion with PLM / reuse of PLM loan entities (mitigated in docs)

## Related

- [bnpl-boundary.md](bnpl-boundary.md)
- [pos-commerce-boundary.md](pos-commerce-boundary.md)
- [persistence-boundary.md](persistence-boundary.md)
- [../risks-and-decisions.md](../risks-and-decisions.md)
