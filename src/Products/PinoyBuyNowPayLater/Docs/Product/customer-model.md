# Customer Model

**Status:** Planning baseline (BNPL-00)  
**Implementation present:** No  
**Related:** BNPL-D-00-13, identity model rules

## Principles

- BNPL must **not** create an incompatible duplicate ExItS identity system.  
- Platform owns Personal user identity and public user identifiers.  
- BNPL may maintain a **product-local customer profile** that references Platform and/or merchant-local customer identifiers via **stable contracts**, not cross-product FKs.  
- Organization/customer isolation is mandatory.

## Reference types (planning)

| Reference | Owner | BNPL usage |
|---|---|---|
| Platform Personal / public user id | Platform | Optional linked identity for customer experience and consent |
| Merchant-local commerce customer | Commerce / POS | May correlate for Path A checkout continuity via contract |
| BNPL customer profile | BNPL | Product-owned operational customer record for financing |

Linking Personal identity to a BNPL customer is **optional**, consent-aware where Platform policy requires, and never creates organization Staff membership.

## Consent and authorization

- Customer consent for Personal linking follows Platform/product contracts (do not invent a parallel consent ledger that conflicts with Platform).  
- Staff authorization to view/edit customers is BNPL product-local.  
- Customers (future Personal UX) see **own** plans only (BNPL-D-00-13).

## What BNPL stores vs references

| Data | Guidance |
|---|---|
| Display name / contact for financing ops | Product-local copy or snapshot as needed for operations |
| Login credentials | **Never** — Platform only |
| HomeOrganizationId / staff identity | **Never** for customers |
| Cross-DB FK to PlatformUsers / PosCustomers | **Forbidden** |

## Explicit non-goals

- Auto-creating Organization staff from BNPL customers  
- Treating contact email as unique org-authorization key  
- Copying POS Customer or PLM Borrower tables by project reference
