# Pinoy Pawn Manager — Platform Integration

> Architecture index: [README.md](README.md)  
> Product definition: [../product-definition.md](../product-definition.md)

| Field | Value |
|---|---|
| Status | PPM-01 Local Validation / Dev catalog fixture registered |
| Implementation | Platform product code + Local Validation fixture; no PPM operational APIs |
| Last updated | 2026-08-27 |

## Intent

PPM integrates with **ExItS Platform** for identity, organization membership, branch facts, product catalog/subscription, and entitlement facts. PPM does **not** become a second authentication system and does **not** read Platform tables directly.

## Ownership split

| Concern | Owner | PPM usage |
|---|---|---|
| Personal / Organization staff identity | Platform | Staff act under Platform identity |
| Production authentication maturity | Platform (**R-091**) | No fake production-secure claims (**D-P12-05**) |
| Organizations / memberships | Platform | Store `OrganizationId` as Guid/contract only |
| Branch facts | Platform | Store `BranchId` on operational records; vault layout is PPM-owned |
| SaaS catalog / plans / subscription | Platform | Independent PPM subscription. PPM-01 registers a **non-production** Local Validation catalog/trial fixture only. |
| Entitlement / commercial-state transport | Platform facts (**D-P12-03** Open) | Consume approved contracts only—no Platform DB reads |
| SaaS billing | Platform | Never store pawn operational money as Platform SaaS payments |
| Pawn ops, custody, appraisal, tickets, payments | **PPM** | Product-local domain (not implemented) |
| Platform Admin UI | Platform | Catalog/subscription admin only—not pawn ops |

## Trusted context (when implemented)

Server-side enforcement must require:

1. Authenticated Platform principal  
2. Organization membership / access for the selected org  
3. Active product entitlement for PPM (transport TBD)  
4. Branch context for operational mutations where branch-scoped  
5. PPM product-local grants for the action  

Client-selected org/branch/product context is never authoritative alone.

## Catalog identity (provisionally approved for implementation)

| Field | Value | Decision |
|---|---|---|
| Display name | Pinoy Pawn Manager | **PPM-D-00-01** Provisionally Approved for Implementation — not final marketing |
| Product code / slug | `pinoy-pawn-manager` | **PPM-D-00-02** Provisionally Approved for Implementation — not final marketing |
| Product directory | `PinoyPawnManager` | **PPM-D-00-03** Provisionally Approved for Implementation — not final marketing |

PPM-01 registered these values as a Local Validation / Dev fixture (`EnsurePpmLocalValidationCatalog`). This is **not** production commercial catalog completeness and does **not** close **PPM-D-00-04**.

## Personal surface (optional later)

ExItS Personal may later present ticket/status views (**PPM-15**). Presentation does not transfer operational authority: PPM remains source of truth for pawn/custody data. Personal must not become a second login for org staff.

## Explicit non-claims

- Platform integration does **not** authorize pawnshop operation under Philippine law  
- `LEGAL_AUTHORIZATION_CLAIMED=NO`  
- Development-stage shortcuts must never be described as production-secure  

## Related

- [persistence-boundary.md](persistence-boundary.md)
- [api-contract-boundary.md](api-contract-boundary.md)
- [../security.md](../security.md)
- [../risks-and-decisions.md](../risks-and-decisions.md)
