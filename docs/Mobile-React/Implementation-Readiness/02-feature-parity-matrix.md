# POS-REACT-READINESS-02 — Feature Parity and UX Migration Matrix

**Package:** POS-REACT-READINESS-02  
**Status:** Documentation only. React implementation is **NOT AUTHORIZED**.  
**Evidence base:** `origin/main` `5979a9ce008bb24a3257abd28ae79bc1a5a9b569`  
**Depends on:** [01-current-maui-implementation-refresh.md](01-current-maui-implementation-refresh.md), [product-surfaces-and-ux.md](../product-surfaces-and-ux.md), [migration-testing-and-implementation-gates.md](../migration-testing-and-implementation-gates.md), [offline-sync-auth-and-security.md](../offline-sync-auth-and-security.md)

This matrix is **feature-based**. It is not a 171-route clone plan (MOBILE-D-046). URL shapes may change. Completing a first selling slice does **not** retire MAUI.

---

## 0. Legend

### 0.1 Migration status

| Value | Meaning |
|---|---|
| **READY** | Current capability is understood; browser/PWA can implement without new authority, after Gate C/D/E authorization. |
| **READY_WITH_CONTRACT_CHECK** | HTTP/auth contract exists but must be revalidated (browser session, CSRF, OpenAPI typing, entitlements). |
| **OFFLINE_PARITY_REQUIRED** | Online UI may ship first; LocalStore-equivalent behavior is required before that feature is parity-complete. |
| **CAPACITOR_REQUIRED** | Browser/PWA can degrade; native adapter is required for current MAUI-level device behavior. |
| **PRODUCT_DECISION_REQUIRED** | Blocked on Product Owner (includes MOBILE-D-060). |
| **DEFERRED** | Tracked, not in the first slice, not silently dropped. |
| **WONT_PORT** | Written reason; must not appear on this client. |

### 0.2 Connectivity class (current `PosOfflineCapabilityPolicy`)

Unknown routes/actions fail closed to **OnlineRequired**.

| Class | Meaning |
|---|---|
| OfflineCapable | Shell/local UX may work without API |
| Queueable | Mutations may enter the encrypted outbox (current cash-only checkout, selected customer/credit/catalog-create/personal Utang) |
| OnlineRequired | Blocked offline with ordinary or sensitive Internet-required UX (MOBILE-D-058) |

### 0.3 Repeated column defaults

Unless a row overrides:

| Column | Default for this client |
|---|---|
| EN/fil-PH | Required before parity (`en` default, `fil-PH` secondary) — MOBILE-D-064 |
| Theme | System / Light / Dark; System default — MOBILE-D-064 |
| Authorization | Server-authoritative. UI never grants permission — MOBILE-D-018 |
| Automated tests | Typecheck + Vitest/Testing Library; Playwright for journeys; API client tests for mutations |
| Physical device | Required when camera, Share sheet, SecureStorage analogue, HID radio, or real network loss is in scope |

### 0.4 Browser/PWA feasibility shorthand

| Value | Meaning |
|---|---|
| Feasible | Works in browser/PWA with existing APIs or degrade |
| Feasible-degrade | Works with a weaker adapter (copy instead of share, typed barcode instead of camera) |
| Native-later | Capacitor adapter required for current MAUI parity |
| No | Out of this client |

---

## 1. First implementation slice (Gate E)

Recommended **first React vertical slice** after Gates C and D. POS selling may go first. Personal and Owner remain in the matrix for eventual retirement.

```text
Auth / session shell
→ workspace resolver (AMEND-03 skip vs chooser)
→ product context (skip when exactly one launchable Mobile product)
→ POS sell-floor shell
→ product browse / search
→ session cart
→ cash checkout ONLINE
→ receipt + share fallback
→ connectivity + sync presentation shell
```

**Not in the first UI slice**

- Offline financial outbox / cash-offline sale
- Manual GCash / Utang / card checkout (may be **shown** as online-required later; not first-slice payment)
- Inventory, purchasing, reports, staff admin
- Personal Utang and Owner governance beyond whatever chrome is needed to prove the shell
- Capacitor Android packaging
- Printer / drawer / NFC / terminal

**Visual checkpoint (existing Gate E)** — phone, tablet portrait, tablet landscape, desktop/PWA. Human Product Owner approval required. Cursor/agent cannot self-approve screenshots.

| Surface | Viewport notes |
|---|---|
| Login / session | Phone-first; enrolled-user chooser when trusted |
| Workspace / product context | Skip chooser when exactly one authorized choice (MOBILE-D-065…070) |
| Sell floor | **Tablet landscape is the reference selling layout** |
| Phone selling | Usable reduction (sticky cart sheet), not a shrunk three-column floor |
| Desktop/PWA | Operational POS sell floor (product + cart + pay). **Must not** become a Platform Admin clone |
| Cart | Session-persistent; orientation/search/category must not clear it |
| Cash pay (online) | Immediate success/error; no offline queue in this slice |
| Receipt | On-screen; share or copy fallback |
| Sync chrome | Online / Offline / Pending presentation even if Pending stays empty in slice 1 |

---

## 2. Eventual MAUI retirement parity (Gate J)

MAUI cannot retire after checkout parity alone.

| Experience | Eventual disposition required | First-slice inclusion |
|---|---|---|
| **Auth** | Parity or explicit replacement of PIN/lock/reconnect/device register | Session shell **yes**; PIN policy **PRODUCT_DECISION_REQUIRED** (MOBILE-D-060) |
| **Personal Mobile** | Parity or Product Owner split/retirement of Personal-on-this-host | **DEFERRED** from slice 1; **must remain tracked** |
| **Organization Owner Mobile** | Practical essentials parity (`/org/*`, manage-business subset) or explicit Web-only disposition | **DEFERRED** from slice 1; **must remain tracked** |
| **POS Operations** | Selling + catalog + customers + shifts/registers + purchasing + reports as current MAUI capabilities | Selling slice first; remainder **DEFERRED** but tracked |

Organization Web full administration and Platform Admin stay **WONT_PORT**.

Until Gate J, MAUI remains the production-path Mobile Client (MOBILE-D-002).

---

## 3. Feature matrix

API names are **contract groups** used by current MAUI/`ApiClient`. They are not a license to invent endpoints. Browser mutations that hit Platform need `PWEB20_CSRF_COMPATIBILITY_REVIEW_REQUIRED` (package 03).

### 3.1 Auth / session / device

| Experience group | Current user capability | Current MAUI route/component | Current APIs | Authorization | Connectivity class | Current offline behavior | Current device requirement | Future React feature | Browser/PWA feasibility | Capacitor requirement | EN/fil-PH | Theme | Responsive target | Automated test requirement | Physical-device requirement | Migration status | Notes/blockers |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Auth | Cold-start routing | `/` `Boot.razor` + `NavigationGate` | Session restore, introspect | Signed-in vs anonymous | OfflineCapable | Restore grant-bound context or send to PIN/reconnect | SecureStorage | Boot resolver | Feasible | Native secure storage later | Yes | Yes | Phone-first | Vitest resolver + Playwright boot | Yes for secure storage | READY_WITH_CONTRACT_CHECK | Browser must not restore Bearer from localStorage |
| Auth | Password sign-in | `/signin` `SignIn.razor` | Platform `POST /auth/login`, token issue | Anonymous → Platform user | OfflineCapable UI; login OnlineRequired in practice | Cached enrolled users; login needs network | None | Session shell login | Feasible | Native secure storage | Yes | Yes | Phone | Playwright login; API client | No for happy path | READY_WITH_CONTRACT_CHECK | PWEB-20 for browser Platform mutations |
| Auth | Register / activate / forgot password | `/register` `/activate` `/forgot-password` | Platform register/activate/forgot | Anonymous | OnlineRequired | Blocked offline | None | Same flows | Feasible | None | Yes | Yes | Phone | Playwright + API | No | READY_WITH_CONTRACT_CHECK | |
| Auth | Onboarding language/theme/density | `/onboarding/*` | Preference stores only | Any | OfflineCapable | Local preferences | Preferences | Preference onboarding | Feasible | None | Yes | Yes | Phone | Testing Library | No | READY | Defaults already locked MOBILE-D-064 |
| Auth | Workspace chooser | `/workspace-select` | Platform orgs + branches | Membership | OnlineRequired | Sensitive Internet-required; no silent last-used enter | None | Smart workspace resolver | Feasible | None | Yes | Yes | Phone/tablet | Vitest AMEND-03 cases | No | READY | Skip when exactly one authorized workspace |
| Auth | Organization bind | `/organization-select` | Platform organization-context | Membership | OnlineRequired | Blocked offline | None | Org bind | Feasible | None | Yes | Yes | Phone | API + Playwright | No | READY_WITH_CONTRACT_CHECK | |
| Auth | Reconnect wall | `/reconnect` | Token reissue / introspect | Previously signed in | OnlineRequired | Must reconnect | None | Reconnect | Feasible | None | Yes | Yes | Phone | Playwright offline→online | Optional radios | READY_WITH_CONTRACT_CHECK | |
| Auth | Offline PIN unlock / enroll | `/offline-pin` `/offline-pin-setup` `/setup-pin` | Local grant; recovery enroll/exchange | Enrolled device user | OfflineCapable | Bounded grant; server denial wins when learned | SecureStorage | PIN UX | Feasible-degrade | Native secure storage | Yes | Yes | Phone | Isolation tests | Yes | PRODUCT_DECISION_REQUIRED | **MOBILE-D-060 OPEN** (length/weak PIN/shared values) |
| Auth | Lock / Sign Out / Remove | Settings + auth chrome | Session revoke; local enrollment | Signed-in | Mixed | Sign Out must not silently delete outbox (MOBILE-D-056) | SecureStorage | Distinct Lock/Sign Out/Remove | Feasible | Native secure storage | Yes | Yes | Phone | Vitest + Playwright | Yes | OFFLINE_PARITY_REQUIRED | Auto-lock timeout not numeric (MOBILE-D-057) |
| Auth | POS device register | `/devices/register` `PosDeviceRegister.razor` | Platform POS device tokens redeem/register | Entitled org user | OnlineRequired | Blocked offline | Camera/QR generate | Device registration | Feasible-degrade | Camera/QR + device identity | Yes | Yes | Phone | API + QR fixture | Yes for camera | CAPACITOR_REQUIRED | Browser can paste token; camera is native-later |
| Auth | Access denied | `/access-denied` | None (local) | Fail-closed | OfflineCapable | Show denial | None | Access denied | Feasible | None | Yes | Yes | Phone | Testing Library | No | READY | |
| Auth | Local Validation quick login | `/signin` Debug | Local Validation client | DEBUG only | OnlineRequired | Dev-only | None | Dev helper optional | Feasible | None | n/a | n/a | n/a | Must not ship production | No | WONT_PORT | Production path must not embed Debug credential. Flag: `DEBUG_LOCAL_VALIDATION_CREDENTIAL_EMBEDDED` |
| Auth | Dev component showcase | `/dev/components` | None | Dev/Testing environment | OfflineCapable | Hidden in Release | None | Optional Story/dev route | Feasible | None | Yes | Yes | All | Optional | No | DEFERRED | Not a product capability |

### 3.2 Workspace / product context

| Experience group | Current user capability | Current MAUI route/component | Current APIs | Authorization | Connectivity class | Current offline behavior | Current device requirement | Future React feature | Browser/PWA feasibility | Capacitor requirement | EN/fil-PH | Theme | Responsive target | Automated test requirement | Physical-device requirement | Migration status | Notes/blockers |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Workspace | Auto-select one org + one Active branch | `WorkspaceSelectionService` | Platform orgs/branches | Membership | Online to bind | Offline PIN restores grant-bound workspace only | None | Smart resolver | Feasible | None | Yes | Yes | All | Vitest AMEND-03 | No | READY | MOBILE-D-065…068 |
| Workspace | Switch workspace | Header / chooser | Platform organization-context + branch | Membership | OnlineRequired | Blocked offline; confirm if cart non-empty | None | Adaptive Switch | Feasible | None | Yes | Yes | All | Playwright cart-confirm | No | READY_WITH_CONTRACT_CHECK | No invented branch |
| Product context | Pinoy Business POS hard-coded launch | `ProductAccessResolver` | Platform access/evaluate + entitlements | Entitlement | OnlineRequired | Snapshot does not permanently override server | None | Product-aware launch | Feasible | None | Yes | Yes | All | Vitest skip/chooser | No | READY | Current single-product hard-code is evidence, not a generic contract (MOBILE-D-069) |
| Header | Org/branch/product in AppTopBar | `StoreHeader` | Session facts | Signed-in | OfflineCapable display | Labels from session/grant | None | Shared AppTopBar | Feasible | None | Yes | Yes | All | Testing Library | No | READY | MOBILE-D-070; pages must not rebuild chrome |

### 3.3 POS selling (first slice core)

| Experience group | Current user capability | Current MAUI route/component | Current APIs | Authorization | Connectivity class | Current offline behavior | Current device requirement | Future React feature | Browser/PWA feasibility | Capacitor requirement | EN/fil-PH | Theme | Responsive target | Automated test requirement | Physical-device requirement | Migration status | Notes/blockers |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| POS selling | Role home | `/owner` `/manager` `/cashier` `/home` | Role routing; operational setup probe | POS role + entitlement | OfflineCapable shell | Homes render; mutations may block | None | Role home + sell entry | Feasible | None | Yes | Yes | Phone + tablet | Playwright | No | READY | Owner without POS role must not checkout |
| POS selling | Sell floor | `/sales/new` `SaleCheckout.razor` | Catalog search/get-by-barcode/SKU; shift/register context | `CreateSale` / store-sales-create | Queueable route; **non-cash OnlineRequired** | Cash may queue today; first React slice is **online cash only** | Keyboard/HID into search | Sell-floor shell | Feasible | HID still keyboard | Yes | Yes | **Tablet landscape reference**; phone usable; desktop operational POS | Playwright + visual review | HID optional | READY_WITH_CONTRACT_CHECK | Gate E visual checkpoint |
| POS selling | Browse / search products | Checkout search + `/catalog` | `PosCatalogClient` list/search/by-sku/by-barcode | POS catalog read | Catalog pages OnlineRequired; sell search uses APIs when online | Local selling catalog projection exists for offline cash | None | Browse/search | Feasible | None | Yes | Yes | Tablet grid / phone list | Vitest debounce + Playwright | No | READY | First slice may use online catalog only |
| POS selling | Session cart | `SaleCartService` | None (memory) | Same as sell floor | Local | In-memory; clears on sale success, sign-out, org switch; **not** SQLite | None | Session-persistent cart | Feasible | None | Yes | Yes | Persistent in landscape; sheet on phone | Vitest cart | No | READY | Lock must not discard cart (MOBILE-D-057) |
| POS selling | Cash checkout **online** | `SaleCheckout` cash path | `POST /api/v1/pos/sales` + idempotency headers; client `SaleId` | `CreateSale` | Queueable action `sale.checkout.cash` | Current MAUI can queue cash; **first slice must not implement offline finance** | None | Online cash checkout | Feasible | None | Yes | Yes | All sell viewports | API client idempotency + Playwright pay | No | READY_WITH_CONTRACT_CHECK | Server remains pricing/tax authority |
| POS selling | Cash checkout **offline** | Same + LocalStore cash sale | Same POST on replay | Same + offline grant | Queueable | Encrypted outbox FIFO; cash only | SecureStorage + SQLite | Offline cash parity | No (needs approved storage) | Native or approved browser durable store | Yes | Yes | Same | Sync/outbox tests | Yes | OFFLINE_PARITY_REQUIRED | Gate F; not slice 1 |
| POS selling | Manual GCash checkout | `SaleCheckout` | Sales + payment attempt APIs | `CreateSale` | OnlineRequired (`SaleNonCashPayment`) | **Not queued** | None | Online Manual GCash | Feasible | None | Yes | Yes | Sell viewports | API + Playwright | No | DEFERRED | Not first slice. Duplicate-GCash uniqueness not in current schema |
| POS selling | Utang / customer-credit sale | `SaleCheckout` | Sales + customers + credit | Entitlement + capability | OnlineRequired | **Not queued** | None | Online Utang checkout | Feasible | None | Yes | Yes | Sell viewports | API | No | DEFERRED | |
| POS selling | Simulated Card/GCash | `HandleTerminalAttemptAsync` + Fake gateway | Payment attempts | Dev/Testing | OnlineRequired | Identifiers in Preferences only | None | Dev simulation only | Feasible | None | n/a | n/a | n/a | Must not be production UX | No | WONT_PORT | MOBILE-D-020 / D-043 — not production parity |
| POS selling | Receipt | `/sales/{id}/receipt` `/sales/local/{id}/receipt` | `GET sale` | Sale access | History OnlineRequired; local receipt OfflineCapable | Local receipt for queued cash | Share | Receipt + share fallback | Feasible-degrade | Native share | Yes | Yes | Phone + tablet | Playwright receipt | Yes for native share | READY | Browser: Web Share or copy. Not print success |
| POS selling | Sales list / detail / void | `/sales` `/sales/{id}` | `GET/POST sales`, void | Role-gated | History OnlineRequired | No offline rewrite of completed sales | None | Sales history | Feasible | None | Yes | Yes | Phone list / desktop table | API + Playwright | No | DEFERRED | After slice 1 |
| POS selling | Return | `/sales/{id}/return` | Return client | Role-gated | OnlineRequired | None | None | Returns | Feasible | None | Yes | Yes | Phone/tablet | API | No | DEFERRED | |
| POS selling | Seller orders | `/orders` | Customer-order client | Role-gated | OnlineRequired | None | None | Seller orders | Feasible | None | Yes | Yes | Phone/tablet | API | No | DEFERRED | |
| POS selling | Connectivity / sync chrome | `PosShell` + `PosStatusState` | Sync status / queue counts | Signed-in | OfflineCapable | Shows Pending/Failed even when selling online | Connectivity | Sync presentation shell | Feasible | Connectivity adapter | Yes | Yes | All | Testing Library | Optional airplane | READY | Slice 1 shows chrome; queue may stay empty |

### 3.4 Catalog / inventory

| Experience group | Current user capability | Current MAUI route/component | Current APIs | Authorization | Connectivity class | Current offline behavior | Current device requirement | Future React feature | Browser/PWA feasibility | Capacitor requirement | EN/fil-PH | Theme | Responsive target | Automated test requirement | Physical-device requirement | Migration status | Notes/blockers |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Catalog | Product list / detail / edit | `/catalog` `/catalog/products/*` | `PosCatalogClient` | Catalog grants | OnlineRequired; create Queueable | Create metadata may queue; photos are files not SQLite bytes | Camera for image | Catalog CRUD | Feasible-degrade | Camera picker | Yes | Yes | Phone/tablet | API + Playwright | Yes for camera | DEFERRED | Image pick: PWA file input; camera CAPACITOR_REQUIRED for parity |
| Catalog | Categories / prices / barcode lookup | `/catalog/categories` `/todays-prices` `/barcode-lookup` | Catalog APIs | Catalog grants | OnlineRequired | Typed barcode only | Keyboard | Lookup + categories | Feasible | None | Yes | Yes | Phone | API | No | DEFERRED | HID is keyboard, not SDK |
| Catalog | Global / connected-buyer availability / import | `/catalog/global` `/connected-buyer-availability` `/catalog/import*` | Catalog + import + merchant discovery | Role-gated | OnlineRequired | None | None | Import/browse extras | Feasible | None | Yes | Yes | Tablet/desktop lists | API | No | DEFERRED | Import entry currently AuthShell |
| Inventory | Stock, adjust, transfers, counts, expiration | `/inventory*` | `PosInventoryClient` | Inventory grants | OnlineRequired (`InventoryManage`) | **No** inventory mutation outbox; local deduction is projection only | None | Inventory ops | Feasible | None | Yes | Yes | Phone/tablet | API | No | DEFERRED | Do not fake offline inventory |

### 3.5 Customers / credit

| Experience group | Current user capability | Current MAUI route/component | Current APIs | Authorization | Connectivity class | Current offline behavior | Current device requirement | Future React feature | Browser/PWA feasibility | Capacitor requirement | EN/fil-PH | Theme | Responsive target | Automated test requirement | Physical-device requirement | Migration status | Notes/blockers |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Customers | List / create / edit | `/customers*` | `PosCustomerClient` | Customer grants | Queueable list/create | Create/update may queue | None | Customers | Feasible | None | Yes | Yes | Phone | API + outbox later | No | DEFERRED | Offline create is Gate F, not slice 1 |
| Credit / Utang | Credit, repayment, ledger, statement, overdue | `/customers/{id}/credit*` `/ledger` `/statement` `/overdue` | Customers + credit + repayments + sync pull | Utang capabilities | Ledger/statement/overdue OnlineRequired; create Queueable | Encrypted projections + outbox | Share for statement/receipt | Credit ops | Feasible-degrade | Share | Yes | Yes | Phone | API + sync tests | Yes for share | OFFLINE_PARITY_REQUIRED | Eventual POS parity; not slice 1 |

### 3.6 Shifts / registers

| Experience group | Current user capability | Current MAUI route/component | Current APIs | Authorization | Connectivity class | Current offline behavior | Current device requirement | Future React feature | Browser/PWA feasibility | Capacitor requirement | EN/fil-PH | Theme | Responsive target | Automated test requirement | Physical-device requirement | Migration status | Notes/blockers |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Shifts | Open / close / list | `/shifts*` | `PosCashierShiftClient` | Shift grants | OnlineRequired (`ShiftsManage`) | None | None | Shifts | Feasible | None | Yes | Yes | Phone | API | No | DEFERRED | Logical cash drawer only |
| Registers | CRUD / assign | `/registers*` | `PosRegisterClient` | Register grants | OnlineRequired | None | None | Registers | Feasible | None | Yes | Yes | Phone | API | No | DEFERRED | Register ≠ hardware |
| Settings | Cash denomination UI | `/settings/cash-handling` | Operational setup | Setup grants | OnlineRequired | None | None | Cash-handling settings | Feasible | None | Yes | Yes | Phone | API | No | DEFERRED | Not a physical drawer |

### 3.7 Purchasing / suppliers

| Experience group | Current user capability | Current MAUI route/component | Current APIs | Authorization | Connectivity class | Current offline behavior | Current device requirement | Future React feature | Browser/PWA feasibility | Capacitor requirement | EN/fil-PH | Theme | Responsive target | Automated test requirement | Physical-device requirement | Migration status | Notes/blockers |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Purchasing | PO / receive / direct purchase / goods receipts | `/purchasing*` | PO + direct-purchase clients | Purchasing grants | OnlineRequired; `/purchasing/new` Queueable **draft** | Draft save is not submit | None | Purchasing | Feasible | None | Yes | Yes | Phone/tablet | API | No | DEFERRED | |
| Suppliers | Supplier CRUD | `/suppliers*` | `PosSupplierClient` | Supplier grants | OnlineRequired | None | None | Suppliers | Feasible | None | Yes | Yes | Phone | API | No | DEFERRED | |
| Connected suppliers | Request / buyers / incoming / QR request | `/suppliers/connected*` `/connected-suppliers/incoming*` | Connected-supplier client; QR scan | Role-gated | Mostly OnlineRequired; linked-products OfflineCapable; draft Queueable | Selective local cache | Camera/QR | Connected suppliers | Feasible-degrade | QR camera | Yes | Yes | Phone | API | Yes for QR | DEFERRED | QR request: CAPACITOR_REQUIRED for camera parity |

### 3.8 Reports / expenses / dashboard

| Experience group | Current user capability | Current MAUI route/component | Current APIs | Authorization | Connectivity class | Current offline behavior | Current device requirement | Future React feature | Browser/PWA feasibility | Capacitor requirement | EN/fil-PH | Theme | Responsive target | Automated test requirement | Physical-device requirement | Migration status | Notes/blockers |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Reports | Hubs + sales/inventory/expenses/utang/operational | `/reports*` `/dashboard` | `PosReportingClient` | Reports view | OnlineRequired | None | None | Reports | Feasible | None | Yes | Yes | Tablet/desktop tables ok; not Admin clone | API | No | DEFERRED | |
| Expenses | Expense CRUD / categories / summary | `/expenses*` | `PosExpenseClient` | Expenses grants | OnlineRequired | None | None | Expenses | Feasible | None | Yes | Yes | Phone | API | No | DEFERRED | |

### 3.9 Organization Owner Mobile

| Experience group | Current user capability | Current MAUI route/component | Current APIs | Authorization | Connectivity class | Current offline behavior | Current device requirement | Future React feature | Browser/PWA feasibility | Capacitor requirement | EN/fil-PH | Theme | Responsive target | Automated test requirement | Physical-device requirement | Migration status | Notes/blockers |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Owner | Org essentials / enter POS | `/org` `OrgSummary.razor` | Platform org + access evaluate | Org membership; POS entry needs entitlement+role | OnlineRequired | Blocked offline | None | Owner essentials home | Feasible | None | Yes | Yes | Phone-first | Playwright | No | DEFERRED | Required for Gate J |
| Owner | Manage business hub | `/manage-business` | Mixed Platform + POS | Owner/governance | OnlineRequired | Blocked | None | Manage-business subset | Feasible | None | Yes | Yes | Phone | Playwright | No | DEFERRED | Full admin remains Organization Web |
| Owner | Profile / subscription / business types / staff invite-assign | `/org/profile` `/subscription` `/business-types` `/staff*` | Platform users/orgs/subscriptions; POS permissions | Owner | OnlineRequired | Blocked | None | Owner staff/subscription | Feasible | None | Yes | Yes | Phone | API | No | DEFERRED | PWEB-20 on Platform mutations |
| Owner | Branches / branch settings / devices inventory | `/organization/branches*` `/branch-settings` `/org/devices` | Platform branches + POS devices | Owner | OnlineRequired | Blocked | None | Branch + device inventory | Feasible | None | Yes | Yes | Phone | API | No | DEFERRED | |
| Owner | Business QR / notifications / privacy / tax / sales-doc education | `/org/business-qr` `/org/notifications` `/org/privacy` `/organization/tax-compliance` `/sales-document-education` | Platform + privacy client | Owner | OnlineRequired | Share for QR | Share | Owner extras | Feasible-degrade | Share | Yes | Yes | Phone | API | Yes for share | DEFERRED | |
| Owner | Permissions hub / assignments / my access | `/permissions*` | `PosPermissionClient` | Permissions manage | OnlineRequired | None | None | Product-local roles UI | Feasible | None | Yes | Yes | Phone | API | No | DEFERRED | UI is not permission |
| Owner | Operational first-run setup | `/setup` | Operational setup client | Entitled | OnlineRequired | Blocked | None | Setup wizard | Feasible | None | Yes | Yes | Phone | Playwright | No | DEFERRED | |
| Owner | Start Selling overlay | `SellingModeService` | None | POS role | Same as sell floor | Same as selling | None | Selling mode | Feasible | None | Yes | Yes | Tablet/phone | Testing Library | No | READY | Does not change POS role |
| Owner | Full Organization Administration | Organization Web `:8093` | Same ApiClient, different host | Owner/Manager on Web; Cashier denied | n/a | n/a | n/a | **Stay on Organization Web** | No | No | n/a | n/a | n/a | n/a | n/a | WONT_PORT | Not this client (MOBILE-D-003) |

### 3.10 Personal Mobile

| Experience group | Current user capability | Current MAUI route/component | Current APIs | Authorization | Connectivity class | Current offline behavior | Current device requirement | Future React feature | Browser/PWA feasibility | Capacitor requirement | EN/fil-PH | Theme | Responsive target | Automated test requirement | Physical-device requirement | Migration status | Notes/blockers |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Personal | Home / More / profile / settings | `/personal` `/personal/more` `/profile` `/settings` | Platform personal dashboard/profile/settings | Personal identity | OfflineCapable shells | Local prefs | None | Personal shell | Feasible | None | Yes | Yes | Phone-first | Playwright | No | DEFERRED | Required for Gate J |
| Personal | Utang people / lent / borrowed | `/personal/utang/*` | Platform Personal Utang | Personal | Queueable | LocalPersonalUtangStore + outbox | None | Personal Utang | Feasible | Approved storage later | Yes | Yes | Phone | Sync tests | No | OFFLINE_PARITY_REQUIRED | Not slice 1 |
| Personal | Invitations / link requests / notifications / rewards / ownership transfers | `/personal/utang/invitations` `/customer-link-requests` `/notifications` `/rewards` `/ownership-transfers` | Platform personal APIs | Personal | OnlineRequired | Blocked | None | Personal extras | Feasible | None | Yes | Yes | Phone | API | No | DEFERRED | |
| Personal | My QR / resolve user / accept invite | `/personal/my-qr` `/resolve-user` `/invitations/accept` | Platform QR + invitations | Personal | OnlineRequired | Generate/share QR | Camera/Share/QR | QR identity | Feasible-degrade | Camera + share | Yes | Yes | Phone | API | Yes | CAPACITOR_REQUIRED | Camera scan native-later; render is Feasible |
| Personal | Linked merchants shop / statement / receipts / orders | `/personal/linked-merchants*` `/personal/orders*` | Platform + POS linked-customer / customer-orders | Personal + merchant link | OnlineRequired | PersonalMerchantCart in memory | Share | Merchant shop | Feasible-degrade | Share | Yes | Yes | Phone | API + Playwright | Optional | DEFERRED | Buyer shop is not POS checkout |
| Personal | Explore POS / Start a Business | `/personal/explore-pos` `/start-business` | Platform org create | Personal; no staff attach to Personal (identity model) | OnlineRequired | Blocked | None | Start a Business | Feasible | None | Yes | Yes | Phone | API | No | DEFERRED | Preserve P19 identity rules |
| Personal | Personal diagnostics | `/personal/settings/support/diagnostics` | Copy Diagnostics | Personal | OfflineCapable | Redacted clipboard | None | Copy Diagnostics | Feasible | None | Yes | Yes | Phone | Redaction tests | No | READY | MOBILE-D-059; include in Gate E chrome |
| Personal | Personal Web full browser product | `ExItS.Personal.Web` | Personal APIs | Personal | n/a | No LocalStore | n/a | Remain a separate host | No | No | n/a | n/a | n/a | n/a | n/a | WONT_PORT | Additional host, not this Mobile Client |

### 3.11 Settings / support / language / theme

| Experience group | Current user capability | Current MAUI route/component | Current APIs | Authorization | Connectivity class | Current offline behavior | Current device requirement | Future React feature | Browser/PWA feasibility | Capacitor requirement | EN/fil-PH | Theme | Responsive target | Automated test requirement | Physical-device requirement | Migration status | Notes/blockers |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Settings | App settings | `/settings` | Preference stores | Signed-in | OfflineCapable | Local | None | Settings | Feasible | None | Yes | Yes | Phone | Testing Library | No | READY | Slice 1 needs locale/theme at least |
| Settings | Org diagnostics | `/settings/support/diagnostics` | Copy Diagnostics | Org session | OfflineCapable | Redaction | None | Copy Diagnostics | Feasible | None | Yes | Yes | Phone | Redaction tests | No | READY | Gate E |
| Settings | Offline foundation diagnostics | `/dev/offline-foundation` | LocalStore | Dev/support | OfflineCapable | SQLite diagnostics | FileSystem | Optional support | Feasible later | Native DB | n/a | n/a | n/a | Must not leak secrets | Yes | DEFERRED | Not product UX |
| Settings | Language + theme + density | Controllers + preference stores | None | Signed-in | OfflineCapable | Immediate, no restart, no lost cart | Preferences | `en` / `fil-PH`; System/Light/Dark; Compact default for cashier | Feasible | None | Yes | Yes | All | Playwright theme/locale | No | READY | MOBILE-D-064 |

### 3.12 Explicit WONT_PORT / not-assumed hardware

| Experience group | Current user capability | Current MAUI route/component | Current APIs | Authorization | Connectivity class | Current offline behavior | Current device requirement | Future React feature | Browser/PWA feasibility | Capacitor requirement | EN/fil-PH | Theme | Responsive target | Automated test requirement | Physical-device requirement | Migration status | Notes/blockers |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Out of client | Platform Administration | Platform Admin host | Platform operator APIs | Platform staff | n/a | n/a | n/a | Never on Mobile | No | No | n/a | n/a | n/a | n/a | n/a | WONT_PORT | MOBILE-D-014 |
| Hardware | Thermal printer | **Absent** | None | n/a | n/a | n/a | **Absent** | Not assumed | No native promise on PWA | Separate auth | n/a | n/a | n/a | Do not fake in CI | If authorized later | DEFERRED | Absence is not a parity blocker |
| Hardware | Physical cash drawer | **Absent** (logical shift cash only) | Shift movements | n/a | n/a | n/a | **Absent** | Not assumed | No | Separate auth | n/a | n/a | n/a | n/a | If authorized later | DEFERRED | |
| Hardware | NFC / real terminal | **Absent** | Fake payment attempts only | n/a | n/a | n/a | **Absent** | Not assumed | No | Separate auth | n/a | n/a | n/a | n/a | If authorized later | DEFERRED | MOBILE-D-044 |
| Hardware | Live product-barcode camera | **Absent** (QR identity still-image only) | Catalog by-barcode typed | n/a | n/a | n/a | **Absent** | Optional later adapter | Degrade to type | Optional plugin | n/a | n/a | n/a | n/a | If authorized | DEFERRED | Do not block Gate E |
| iOS native | Capacitor iOS | MAUI not shipping iOS TFM | n/a | n/a | n/a | n/a | n/a | Gate K | Browser/PWA interim | Later | n/a | n/a | n/a | Gate K | Gate K | DEFERRED | MOBILE-D-030 |

---

## 4. First-slice row index (implement later, not now)

| Feature | Status for slice 1 |
|---|---|
| Auth/session shell (login, boot, reconnect chrome) | READY_WITH_CONTRACT_CHECK |
| Workspace resolver + AppTopBar context | READY |
| Product context skip/chooser | READY |
| POS sell-floor shell | READY_WITH_CONTRACT_CHECK |
| Product browse/search | READY |
| Session cart | READY |
| Online cash checkout | READY_WITH_CONTRACT_CHECK |
| Receipt + share/copy fallback | READY (native share CAPACITOR_REQUIRED for MAUI-level sheet) |
| Connectivity/sync presentation | READY |
| Copy Diagnostics | READY |
| Locale + theme | READY |
| Offline cash / outbox | **Excluded** — OFFLINE_PARITY_REQUIRED (Gate F) |
| Personal / Owner product surfaces | **Excluded** — DEFERRED, still Gate J blockers |

---

## 5. Authorization lock (repeat)

React implementation **NOT AUTHORIZED**. Filling this matrix does not pass Gate C, D, or E.
