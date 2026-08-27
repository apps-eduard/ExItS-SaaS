# PERS-E2E-22H-REPAIR — Buyer → Seller Multi-User Commerce Continuation

**Package:** PERS-E2E-22H-REPAIR  
**Status:** COMPLETE  
**Branch:** `feat/personal`  
**Date:** 2026-08-27  
**Classification:** `MOCK_BOUND_MULTI_USER_E2E=PASS` · `LIVE_DOCKER_MULTI_USER_E2E=NOT_APPLICABLE`

## Old RMAP-22H limitation

Historical RMAP-22H (`e2e/rmap-22h-personal-business-e2e.spec.ts`) covered Personal utang/todo/invite, Start Business → `/onboarding`, responsive shell, and Org-staff denied Personal.

Commerce mocks existed, but **seller continuation was deferred**: no automated proof that one Personal buyer order is the same logical record processed by a separate Organization seller session through the lifecycle and observed again by the buyer.

Treat `docs/Mobile-React/Reports/POS-REACT-RMAP-22H-personal-business-e2e.md` as historical reference only.

## Stale vs current routes (audit)

| Surface | Current route / API |
| --- | --- |
| Buyer stores | `/personal/linked-merchants` |
| Storefront | `/personal/linked-merchants/:orgId/shop` |
| Checkout | `…/shop/checkout` |
| Place order | `POST /api/v1/pos/customer-orders/organizations/{sellerOrg}` |
| Buyer order detail | `/personal/orders/:orderId` · `GET …/customer-orders/mine/{orderId}` |
| Seller queue | `/orders` |
| Seller detail | `/orders/:orderId` |
| Seller transitions | `POST …/organizations/{orgId}/customer-orders/{orderId}/{accept\|start-preparing\|mark-ready\|mark-collected\|complete}` |
| Start Business handoff | `/personal/start-business` → `/onboarding` (not immediate seller workspace) |

Seller lifecycle exercised (pickup, current UI/`availableSellerActions` contract):

`Submitted` → accept → `Accepted`/`Pending` → start-preparing → `Preparing` → mark-ready → `ReadyForPickup` → mark-collected → `Collected` → complete → `Completed`

## Buyer / seller session design

| Actor | Session | Playwright |
| --- | --- | --- |
| User B | Personal (`accountClass=Personal`, empty organizations list) | Dedicated `BrowserContext` |
| User A | Organization Owner (bound ops workspace) | Separate `BrowserContext` |

Session isolation proven: B cannot open `/orders` (account-class denied); A cannot open `/personal` (account-class denied).

## Shared logical order state

Companion: `e2e/pers-e2e-22h-buyer-seller-continuation.spec.ts`

One mutable `SharedOrderState` object is closed over by both contexts’ mock handlers. Buyer `POST` creates that record; seller `GET`/transition mutates the same object; buyer `GET` after full navigation refetch observes updated status. Not two unrelated static fixtures.

## Customer link safety

Accept contract returns `createdOrganizationMembership: false`, `grantedProductRole: null`. Buyer `GET /auth/organizations` remains `[]`. Customer link ≠ staff membership.

## Isolation evidence

| Check | Result |
| --- | --- |
| Cross-user privacy | Unrelated Personal actor cannot see buyer order lines/number |
| Cross-org | Other-org seller gets 403 on Org A routes; other-org list 404; no transitions recorded |
| Branch | `branchId` query filter returns empty for wrong branch; order carries `fulfillmentBranchId` Main Branch |
| Invalid transition | Skip to `complete` while `Accepted` → 409 |
| Double-submit | Accept button leaves UI; retry `POST /accept` converges; single `accept` transition |
| Online-only | Buyer checkout while offline → `merchant-checkout-offline`; no place |

## Mock / live classification

- **MOCK_BOUND_MULTI_USER_E2E:** PASS (7 companion tests)
- **LIVE_DOCKER_MULTI_USER_E2E:** NOT_APPLICABLE — no deterministic SAFE Local Validation two-user fixtures wired for this React client against live Platform/POS APIs in this package. Do not invent production credentials.

## Production code

**PRODUCTION_CODE_CHANGED=NO.** Product commerce already supported the story; this package is E2E repair + documentation.

## Test counts

| Suite | Result |
| --- | --- |
| Companion `pers-e2e-22h-buyer-seller-continuation` | **7 passed** |
| Existing `rmap-22h-personal-business-e2e` | **7 passed** (unchanged scope; pointer comment updated) |
| Full React Vitest | **956 passed** / 172 files |
| Typecheck / Lint / Build | PASS (lint warnings only, 0 errors) |

## Related gate hygiene (test-only)

- `e2e/rmap-21-personal-offline-queue.spec.ts` — stub linked-merchants / soft Personal GETs + dismiss overlay so offline-enqueue assert is not blocked by error overlay (flake control).
- `e2e/auth-session.spec.ts` — align assertions with current Sign-in page (`sign-in-page`) and local-lock-on-failed-remote-logout contract.

## Resolution

P1 **RMAP-22H seller continuation** → **RESOLVED by PERS-E2E-22H-REPAIR**.
