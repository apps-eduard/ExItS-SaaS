# POS-B2B-IDENTITY-DISPLAY-01

**Status:** COMPLETE  
**Branch:** `feat/organization`  
**START_SHA:** `ad0df5df2e3e2b45af6c3a201d60aa155838fc0e`  
**TASK:** POS-B2B-IDENTITY-DISPLAY-01

## Audit (before)

| Field | Value |
|-------|--------|
| EXISTING_BUSINESS_CUSTOMER_MODEL | Supplier projection of `ConnectedSupplierRelationship` buyer org (not POSCustomer / not duplicate Organization) |
| EXISTING_LIST_IDENTITY_SOURCE | `BuyerDisplayNameSnapshot` / `BuyerPublicOrganizationIdSnapshot` (`DisplayNameIsLive=false`) |
| EXISTING_DETAIL_IDENTITY_SOURCE | Platform `ResolveOrganizationForConnectedSupplierAsync` when public id present + org id match (`DisplayNameIsLive=true`) |
| CURRENT_IDENTITY_ASYMMETRY | YES — list snapshot vs detail live rename |
| PLATFORM_BATCH_RESOLVE_AVAILABLE | NO — only single public-id / QR / own-org public-identity endpoints |

## Decision

| Field | Value |
|-------|--------|
| BUSINESS_CUSTOMER_DISPLAY_POLICY | **SNAPSHOT_CONSISTENT** |
| LIVE_LIST_IDENTITY | **DEFERRED_NO_BATCH_RESOLVER** |
| SNAPSHOT_IDENTITY_ROLE | Primary display + search + fallback |
| LIVE_IDENTITY_ROLE | Deferred until a safe batch Platform resolver exists |
| PLATFORM_IDENTITY_MISMATCH_POLICY | N/A for primary display (no live primary path); historical detail guard rejected mismatched org id |

**Why not live-on-list:** Unbounded per-row Platform HTTP would N+1. Prefer consistency + performance over a fake “live” list.

## After

| Field | Value |
|-------|--------|
| LIST_IDENTITY_SOURCE_AFTER | Relationship buyer snapshot |
| DETAIL_IDENTITY_SOURCE_AFTER | Same snapshot mapper (`MapFromSnapshot`) |
| DISPLAY_NAME_IS_LIVE_SEMANTICS | Always `false` under this policy (truthful: not live-resolved) |
| SEARCH_DISPLAYED_IDENTITY | Matches snapshot (what is displayed) |
| SEARCH_SNAPSHOT_IDENTITY | Same (snapshot is the search haystack) |
| PLATFORM_UNAVAILABLE_POLICY | List/detail never call Platform for identity; always available from relationship |
| BUSINESS_CUSTOMER_IDENTITY_QUERY_MODEL | POS relationships + batch share stats only |
| BUSINESS_CUSTOMER_PLATFORM_CALL_COUNT_MODEL | **0** Platform org resolves for list/detail identity |
| BUSINESS_CUSTOMER_IDENTITY_N_PLUS_ONE | **PASS** |
| BUSINESS_CUSTOMER_CREATES_POSCUSTOMER | NO |
| BUSINESS_CUSTOMER_CREATES_ORGANIZATION | NO |
| RELATIONSHIP_SNAPSHOT_MUTATED_ON_READ | NO |
| BACKEND_CHANGE_REQUIRED | YES (`GetBusinessCustomer` aligned to snapshot) |
| MIGRATION_REQUIRED | NO / N/A |

## Validation

| Check | Result |
|-------|--------|
| Unit BusinessCustomerProjection | 10 passed |
| Integration ConnectedSupplier/BusinessCustomer filter | 2 passed |
| React BusinessCustomerIdentity | 2 passed |
| REACT_FULL | 1237 total / 1171 pass / 66 fail (same Personal/Platform/session baseline) |
| B2B_IDENTITY_RELATED_FAILURES | 0 |
| OTHER_ORGANIZATION_FAILURES | 0 |
| TYPECHECK | PASS |
| LINT | PASS (0 errors) |
| BUILD | PASS |
| MIGRATION | N/A |

**NEXT:** POS-EXPIRED-STOCK-WASTE-QUICK-FLOW-01
