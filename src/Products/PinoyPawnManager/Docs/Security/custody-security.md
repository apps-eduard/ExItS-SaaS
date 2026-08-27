# Pinoy Pawn Manager — Custody Security

> Security index: [README.md](README.md)  
> Idempotency / release: [../Architecture/idempotency-and-reconciliation.md](../Architecture/idempotency-and-reconciliation.md)

| Field | Value |
|---|---|
| Status | PPM-00 planning |
| Implementation | None |
| Last updated | 2026-08-27 |
| Principles | `PPM_OWNS_CUSTODY` = YES; `CUSTODY_HISTORY_REQUIRED` = YES; `PHYSICAL_RELEASE_SEPARATE_FROM_PAYMENT` = YES |

## Intent

Custody security protects physical pledged items from wrong-item release, silent relocation, insider fraud, and cross-branch leakage. Current location alone is insufficient—**movement history** is required.

## Threat themes

| Theme | Example | Planning control |
|---|---|---|
| Wrong-item release | Release bin A item for ticket B | Identity confirmation + checklist |
| Premature release | Release before payment readiness | Separate payment vs release machines |
| Silent move | Relocate without audit | Append-only movement events |
| Cross-branch leakage | Staff sees other vault | Branch scope; transfer only if **PPM-D-00-16** allows |
| Discrepancy cover-up | Overwrite location to hide loss | Discrepancy workflow + supervisor override grants |
| Insider fraud | Collusion on fake receive | Dual control / thresholds where practical |

## Controls (planning)

| Control | Intent |
|---|---|
| Receive confirmation | Item enters custody with actor, time, location, evidence refs |
| Location model | Start simple: Branch → StorageArea → Bin/Bag (**PPM-R-00-10**) |
| Movement audit | Every move records from/to, actor, reason |
| Locate / inventory check | Read capability separate from move |
| Release checklist | Readiness + identity + recipient + confirmation |
| Exception release | Distinct grant; heightened audit |
| Cross-branch transfer | Never implicit (**PPM-D-00-16** OPEN) |

## Payment relationship

- Redemption/renewal **payment success** ≠ custody **RELEASED**  
- Release eligibility is a precondition; physical confirmation is a separate mutation  
- See [../Architecture/idempotency-and-reconciliation.md](../Architecture/idempotency-and-reconciliation.md)

## Representative redemption

Whether a third party may redeem/receive is **PPM-D-00-13** OPEN. Safe default until decided: **deny** representative redemption.

## Related risks

- **PPM-R-00-03** collapsing payment and physical release  
- **PPM-R-00-06** insider wrong-item release  
- **PPM-R-00-08** cross-org or cross-branch leakage  

## Related

- [role-and-grant-baseline.md](role-and-grant-baseline.md)
- [audit-and-history.md](audit-and-history.md)
- [../Architecture/pos-commerce-boundary.md](../Architecture/pos-commerce-boundary.md)
