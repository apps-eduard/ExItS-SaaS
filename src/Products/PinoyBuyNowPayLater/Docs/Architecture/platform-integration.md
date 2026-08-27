# Platform Integration

**Status:** Planning baseline (BNPL-00)  
**Implementation present:** No  
**Related:** D-P12-03, R-091, BNPL-D-00-02

## Platform owns

- ExItS identity and Personal users  
- Organizations and memberships  
- Account / session context  
- Product catalog, plans, independent BNPL subscription  
- Entitlements and commercial access facts  
- SaaS billing  
- Platform administration and Platform audit  

## BNPL consumes

| Fact | Transport |
|---|---|
| Authenticated actor | Platform session / approved product auth |
| Organization context | Trusted org context from Platform |
| BNPL product entitlement | Commercial-state transport — **do not invent** (D-P12-03 Open) |
| Personal public identifiers | Approved identity contracts |

## BNPL must not

- Read Platform tables via EF/SQL  
- Store SaaS billing as operational financing  
- Treat Platform Admin as BNPL operations UI  
- Claim production-secure auth beyond portfolio evidence  

## Catalog registration

Product code `pinoy-buy-now-pay-later` is **proposed only** until BNPL-D-00-02 closes. BNPL-01 may register only when authorized.
