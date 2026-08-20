# POS-REACT-READINESS-03 — API, Auth, and Browser Security Readiness

**Package:** POS-REACT-READINESS-03  
**Status:** Documentation only. No API, Platform, PWEB, or client implementation.  
**Evidence base:** `origin/main` `5979a9ce008bb24a3257abd28ae79bc1a5a9b569`  
**Depends on:** [02-feature-parity-matrix.md](02-feature-parity-matrix.md), [offline-sync-auth-and-security.md](../offline-sync-auth-and-security.md)

Question this package answers: can **existing** Platform and POS HTTP contracts support a future React Browser/PWA and later Capacitor client **without inventing new authority**?

Answer: **yes for native-style Bearer (current MAUI analogue) and for same-origin browser session**, provided browser auth waits for PWEB-20 CSRF review and typed clients are contract-tested. **No** if the plan is to dump Bearer tokens into `localStorage` or to “turn on CORS” as the access model.

This file does **not** copy authoritative business rules into a React design. Server Application/Domain remain the rule owners.

---

## 0. Dual API bases (current)

MAUI and Organization Web share `ExItS.PinoyBusinessPOS.ApiClient`:

| Config | Host | Typical Local Validation |
|---|---|---|
| `PosApi:BaseUrl` | Platform API | `:8091` |
| `PosBusinessApi:BaseUrl` | PinoyBusinessPOS API | `:8092` |

Release MAUI requires HTTPS URLs (`Security:RequireHttpsApiUrls`). Cookies are **not** used by the MAUI ApiClient. Organization Web adds its own cookie/session shell on top of the same typed clients.

---

## 1. Authentication model (current, must be reused)

Locked access chain (unchanged):

```text
Platform User
  → Organization Membership
  → Product Access / Entitlement
  → Product-Local Role and Grants
```

### 1.1 MAUI (current Mobile Client)

| Step | Contract |
|---|---|
| Login | `POST /api/v1/platform/auth/login` |
| Access token | `POST /api/v1/platform/auth/token` (password or session grant) |
| Storage | `MauiSecureTokenStore` (MAUI **SecureStorage**). Never passwords. Never Preferences for tokens |
| Platform calls | `Authorization: Bearer` and/or `Authorization: PlatformSession` + `X-ExItS-Session-Token` (path-dependent; see `PlatformSessionHeaderHandler`) |
| POS calls | `Authorization: Bearer` + forwarded `X-ExItS-Session-Token` + `X-Pos-Organization-Id` + `X-Pos-Branch-Id` + `X-Pos-Installation-Device-Id` + optional commercial headers |
| POS enforcement | `PosPlatformBearerMiddleware` introspects via Platform `POST /api/v1/platform/auth/introspect` |
| Recovery | `PlatformAccessTokenRecoveryHandler` one 401 retry; skips auth bootstrap paths; skips when offline |
| Device recovery | `POST .../auth/recovery/enroll|exchange|revoke` |
| Dev header | `X-Dev-Platform-User-Id` — Dev/Testing only; Production fail-closed |

### 1.2 Organization Web / Personal Web (current browser hosts)

Cookie/session (ADR-022 / P25). Not LocalStore. Antiforgery exists on current Blazor forms. These hosts prove that **browser session is already a Platform contract**, not a new identity.

A future React Mobile Client must consume the **same Platform identity**. It must not create a second login universe.

---

## 2. API inventory (contracts used by current MAUI / Org Web)

Inventory is from typed `ApiClient` plus POS endpoint maps. Do not treat this as permission to call unlisted server routes from React without a later audit.

**Shared HTTP facts**

| Fact | Current evidence |
|---|---|
| Errors | `application/problem+json` (`title`, `detail`, `errorCode`, `status`) |
| Paging | Query `page` + `pageSize` → paged DTOs |
| Idempotency (POS mutations) | `Idempotency-Key`, `X-Pos-Payload-Hash`, `X-Pos-Operation-Id`, `X-Pos-Operation-Type` |
| Client-generated ids | Yes for offline-capable mutations (notably `SaleId` on checkout) |
| PATCH | Rare/absent on the typed client; prefer GET/POST/PUT/DELETE as mapped |
| Auth cookies on MAUI client | **Not used** |

Connectivity class in the tables is the **client capability class** from `PosOfflineCapabilityPolicy` / DOC-05, not an HTTP cache hint. Unknown POS routes fail closed to OnlineRequired.

### 2.1 Platform identity / session

| Verb | Path group | Auth | Idempotency | Paging | Client ids | Connectivity |
|---|---|---|---|---|---|---|
| POST | `/api/v1/platform/auth/login\|register\|activate-account\|forgot-password\|logout` | Anonymous or session | No | No | No | OnlineRequired (login) |
| GET | `/api/v1/platform/auth/me` | Session/Bearer | No | No | No | Online |
| POST | `/api/v1/platform/auth/token`, `/token/bind`, `/token/revoke`, `/introspect` | Mixed (token/bind/revoke skip session header) | No | No | No | Online |
| POST | `/api/v1/platform/auth/recovery/*` | Session | No | No | No | Online |
| POST | `/api/v1/platform/auth/account-profiles/select\|ensure` | Session | No | No | No | Online |

**PWEB-20:** every **state-changing** browser call in this group needs CSRF compatibility review (`PWEB20_CSRF_COMPATIBILITY_REVIEW_REQUIRED`).

### 2.2 Workspace / org discovery

| Verb | Path group | Auth | Notes |
|---|---|---|---|
| GET | `/api/v1/platform/auth/organizations` | Session | Eligible orgs |
| PUT | `/api/v1/platform/auth/organization-context` | Session | Bind org — **PWEB-20** |
| GET | `/api/v1/platform/users/{id}/memberships` | Bearer/session | Paged |
| GET | `/api/v1/platform/organizations/{id}` | Bearer/session | |
| GET | `/api/v1/platform/access/evaluate?...` | Bearer/session | Entitlement probe |

### 2.3 Branch context

| Verb | Path group | Auth | Notes |
|---|---|---|---|
| GET/POST/PUT | `/api/v1/platform/organizations/{id}/branches*` | Bearer/session | Capacity, CRUD, suspend/archive, hours, delivery — mutations **PWEB-20** |
| PUT | `/api/v1/platform/organizations/{id}/branch-context` | Bearer/session | **PWEB-20** |
| PUT | `/api/v1/pos/operational-branch` | POS Bearer + org/branch headers | Product operational branch |

### 2.4 Product access / entitlement

| Verb | Path group | Auth | Notes |
|---|---|---|---|
| GET | Platform entitlements / subscriptions / commercial plans (`/api/v1/commercial/plans`) | Session/Bearer | Platform commercial SoR |
| Headers | `X-Pos-Subscription-Status`, `X-Pos-Feature-Grants` | POS | Client-forwarded; **server still authoritative** |

Do not implement entitlement math in TypeScript.

### 2.5 POS role / grants

| Verb | Path group | Auth | Idempotency | Connectivity |
|---|---|---|---|---|
| GET/POST/… | `/api/v1/pos/permissions/*` | POS Bearer + org | Assign/revoke use idempotency helpers | OnlineRequired |
| GET | `/api/v1/organizations/{orgId}/product-local-roles` (Platform) | Session/Bearer | | Online |

### 2.6 Catalog

| Verb | Path group | Auth | Idempotency | Client ids | Connectivity |
|---|---|---|---|---|---|
| GET/POST/PUT | `/api/v1/pos/catalog/categories`, `/catalog/products*` | POS Bearer + org | Create/update as mapped | Product create may queue | Catalog pages OnlineRequired; `catalog.product.create` Queueable |
| GET | `/api/v1/catalog/templates|products|categories|business-types` | Platform session (discovery) | No | No | Online |
| POST/GET | `/api/v1/pos/catalog-imports*` | POS Bearer | Import mutations | Job ids server | OnlineRequired |

### 2.7 Selling / sales history / payments

| Verb | Path group | Auth | Idempotency | Client ids | Connectivity |
|---|---|---|---|---|---|
| GET/POST | `/api/v1/pos/sales` | POS Bearer + org + branch for checkout | Checkout **yes** | **SaleId client-generated** | `sale.checkout.cash` Queueable; non-cash OnlineRequired |
| GET | `/api/v1/pos/sales/{saleId}` | POS Bearer | No | Server/client | History OnlineRequired |
| POST | `/api/v1/pos/sales/{saleId}/void` | POS Bearer | As mapped | No | Online |
| GET/POST | `/api/v1/pos/sale-returns*` | POS Bearer | Create idempotent | | Online |
| POST/GET | `/api/v1/pos/sales/{saleId}/payment-attempts`, `/payment-attempts` | POS Bearer | Attempt idempotent | Attempt ids | OnlineRequired |
| POST | `/api/v1/pos/payment-webhooks/{provider}` | Server-facing | n/a | n/a | **Not a Mobile Client API** |

Typed client does **not** wrap all payment-attempt reconcile / verify-manual-gcash / webhook routes. Do not invent those in React until a later contract audit.

### 2.8 Customers / credit / Utang

| Verb | Path group | Auth | Idempotency | Paging | Connectivity |
|---|---|---|---|---|---|
| GET/POST/PUT | `/api/v1/pos/customers*` | POS Bearer + org | Create/update | Yes | Queueable create/update |
| POST | credit-entries, repayments, reverse, due-date | POS Bearer | Yes | | Queueable creates |
| GET | ledger, statement, overdue, aged | POS Bearer | No | Yes | OnlineRequired subpaths |
| GET | `/api/v1/pos/sync/customers\|credit-entries\|repayments` | POS Bearer | No | Checkpoint | Reconnect pull |

### 2.9 Inventory

| Verb | Path group | Auth | Idempotency | Connectivity |
|---|---|---|---|---|
| GET/POST | `/api/v1/pos/inventory*`, transfers, stock-counts, direct-purchase-receipts | POS Bearer + org (+ branch for transfers) | Transfer mutations | **OnlineRequired** (`InventoryManage`) |

Local catalog deduction after cash sale is a **projection**, not inventory SoR.

### 2.10 Shifts / registers

| Verb | Path group | Auth | Idempotency | Connectivity |
|---|---|---|---|---|
| GET/POST | `/api/v1/pos/registers*` | POS Bearer | Activate/deactivate | OnlineRequired |
| GET/POST | `/api/v1/pos/cashier-shifts*` | POS Bearer | Shift open | OnlineRequired |

### 2.11 Suppliers / purchasing

| Verb | Path group | Auth | Idempotency | Connectivity |
|---|---|---|---|---|
| GET/POST/PUT | `/api/v1/pos/suppliers*` | POS Bearer | As mapped | OnlineRequired |
| GET/POST | `/api/v1/pos/purchase-orders*`, `/goods-receipts*` | POS Bearer | Submit/receive | OnlineRequired; PO **draft** may be local |
| GET/POST | `/api/v1/pos/connected-suppliers*` | POS Bearer | As mapped | Mostly OnlineRequired; linked-products OfflineCapable; draft Queueable |
| GET/POST | `/api/v1/pos/direct-purchase-receipts*` | POS Bearer | As mapped | OnlineRequired |

### 2.12 Reports / expenses / ops / privacy

| Verb | Path group | Auth | Connectivity |
|---|---|---|---|
| GET | `/api/v1/pos/dashboard`, `/management/overview`, `/reports/*` | POS Bearer | OnlineRequired |
| GET/POST | `/api/v1/pos/expenses*`, `/expense-categories*` | POS Bearer | OnlineRequired |
| GET/PUT | `/api/v1/pos/operational-setup` | POS Bearer | OnlineRequired |
| GET | `/api/v1/pos/privacy-readiness` | POS Bearer | OnlineRequired |

### 2.13 Device registration (Platform, not POS API)

| Verb | Path group | Auth | Notes |
|---|---|---|---|
| GET/POST/PUT | `/api/v1/platform/organizations/{id}/pos-devices*` | Session/Bearer | Register/rename/revoke/authorize, capacity — mutations **PWEB-20** |
| POST/GET | registration-tokens create/redeem | Session/Bearer | **PWEB-20** |

### 2.14 Sync / idempotency / probes

| Verb | Path group | Auth | Notes |
|---|---|---|---|
| GET | `/api/v1/pos/sync/*` | POS Bearer | Reconnect projections |
| POST | `/api/v1/pos/dev/offline-probe` | POS Bearer | **Dev/Testing only** — not a production React feature |
| GET | `/health` (both APIs) | Anonymous | Reachability |

### 2.15 Personal mobile contracts

Platform (session header required on `/api/v1/personal*`):

| Verb | Path group | Auth | Connectivity | PWEB-20 |
|---|---|---|---|---|
| GET/PUT | `/api/v1/personal/settings`, dashboard, profile | Session | Mixed | Mutations **yes** |
| POST | `/api/v1/personal/start-business` | Session | OnlineRequired | **Yes** |
| POST/GET | `/api/v1/personal/utang/*` | Session | Queueable contacts/entries | Mutations **yes** |
| POST | `/api/v1/qr/resolve`, `/api/v1/users/resolve-public-id` | Session | Online | POST = **yes** |
| GET/POST | invitations, notifications, rewards, ownership, customer links | Session | OnlineRequired | Mutations **yes** |

POS Personal-linked:

| Verb | Path group | Auth |
|---|---|---|
| GET | `/api/v1/pos/personal/linked-customers/{id}/statement\|activity\|receipts/*` | Bearer (personal scope) |
| GET/POST | `/api/v1/pos/customer-orders/mine*`, org storefront/quote, seller org customer-orders | Bearer |

### 2.16 Customer orders (seller + buyer)

Covered above. Buyer shop is Personal, not POS checkout. Seller `/orders` is POS Operations.

---

## 3. TypeScript client readiness

Searched current main for Swashbuckle / NSwag / `AddOpenApi` / `MapOpenApi` / swagger.json on Platform and POS APIs used by MAUI.

**TYPED_CLIENT_GENERATION_CONTRACT_MISSING**

There is **no** machine-readable OpenAPI artifact for these APIs on this SHA. Platform Admin planning recorded the same gap.

This package does **not** add OpenAPI.

### 3.1 Safe interim rule (when React is later authorized)

1. Hand-written TypeScript DTOs and client functions must **mirror audited HTTP contracts** in `ExItS.PinoyBusinessPOS.ApiClient` (and the matching endpoint maps).
2. Every client method needs **contract tests** (verb, path, headers, problem+json, idempotency, paging).
3. Do not generate types from guessed JSON.
4. Do not port Domain/Application rules into Zod “because it is convenient.” Zod validates **wire shape**, not pricing/tax/entitlement.
5. Adding OpenAPI later is a separate authorized backend package, not implied here.

---

## 4. Browser / PWA auth (target)

Preserve MOBILE-D-037:

- Browser-safe session
- **No reusable token in ordinary `localStorage` / `sessionStorage`**
- Prefer HttpOnly cookie on the **web origin** when compatible with existing Platform browser session
- If a Bearer is ever held in the browser, it must use Web Crypto / credential-style storage — not a planning choice to make in this package

Do **not** finalize an independent CSRF design here.

### 4.1 Integration checkpoint

**PWEB20_CSRF_COMPATIBILITY_REVIEW_REQUIRED**

`feat/platform-admin-web-v2` is establishing Platform browser mutation antiforgery (PWEB-IMPL-20). This documentation queue:

- does **not** read or merge that branch
- does **not** modify Platform
- records that Gate D browser auth may start **only after** that CSRF contract is reviewed against Mobile React’s Platform mutations

Every Platform **state-changing** browser call listed in §2 is in scope for that review (login/logout/token, organization-context, branch mutations, device register, Personal settings/Utang/start-business, QR resolve POSTs, memberships, etc.).

POS API mutations today are Bearer-introspected. If the browser client uses cookies against POS, that also needs an explicit later CSRF/same-origin design. Preferred path: **do not** send cookie-authenticated cross-origin POS calls.

Current MAUI OnlineRequired UX vs future toast/dialog split remains a **future host** rule (AMEND-01); it does not change APIs.

---

## 5. Capacitor auth (direction only)

Preserve existing direction (MOBILE-D-037):

- Native secure storage analogue of `MauiSecureTokenStore`
- Bearer + session-header transport as today
- Server introspection remains the authority

**Do not choose a Capacitor secure-storage plugin in this package.**

---

## 6. CORS / origin

Current evidence:

| API | Empty `Cors:AllowedOrigins` | If origins are later listed |
|---|---|---|
| POS | `SetIsOriginAllowed(_ => false)` — deny all browser origins. Comment: MAUI is not CORS-bound. **No** `AllowCredentials`. | Explicit allowlist; GET/POST/PUT/DELETE/OPTIONS |
| Platform | Same deny-by-default | Explicit allowlist + **`AllowCredentials()`** |

**Do not solve browser access by broadly enabling CORS.**

Preferred options, consistent with existing architecture (P14 reverse-proxy HTTPS, ADR-022 cookie web hosts):

```text
1. Same-origin reverse proxy
   Browser origin serves the React app
   /api/platform/*  → Platform API
   /api/pos/*       → POS API
   cookies stay first-party

2. Backend-for-frontend (BFF) on the web origin
   Browser talks only to the origin
   BFF holds the session and calls APIs server-side
   no token in JS storage

3. Capacitor
   Not CORS-bound in the MAUI sense; uses Bearer + native storage
```

Production host / exact nginx location map is an **open decision** (package 06). This package does not implement proxy routes.

Cross-origin SPA + `AllowCredentials` + wildcard is forbidden by current pipelines and must stay forbidden.

---

## 7. First-slice API subset (online only)

When Gate E is later authorized, the slice should call **existing** contracts only:

| Need | Contract |
|---|---|
| Login/session | Platform auth login/me/token/introspect/logout |
| Workspace | auth/organizations, organization-context, branches, access/evaluate |
| Catalog browse | POS catalog products GET (search/sku/barcode) |
| Cash checkout | POS `POST /api/v1/pos/sales` + idempotency + client SaleId |
| Receipt | POS `GET /api/v1/pos/sales/{id}` |
| Health/reachability | `/health` |

No new endpoints. No offline replay in that slice. No OpenAPI addition.

---

## 8. Readiness verdict

| Question | Verdict |
|---|---|
| Can existing APIs support React without new authority? | **YES**, if the client mirrors current contracts |
| OpenAPI generation ready? | **NO** — `TYPED_CLIENT_GENERATION_CONTRACT_MISSING` |
| Browser auth ready to implement? | **NO** until `PWEB20_CSRF_COMPATIBILITY_REVIEW_REQUIRED` |
| Capacitor auth direction ready? | **YES** as planning (plugin unselected) |
| CORS as the browser strategy? | **NO** — same-origin/BFF preferred |
| Business rules in the client? | **NO** — server remains authoritative |

---

## 9. Authorization lock

No Platform change. No POS API change. No React client. No CSRF implementation in this package.
