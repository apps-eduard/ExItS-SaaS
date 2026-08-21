# React Current State

**Client:** `src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Client`
**Package:** `@exits/pinoy-business-pos-client`
**Baseline SHA:** (see branch HEAD after RMAP-01)

Do not treat prior WP labels as completeness. Inventory against routes and code.

## Routes (`src/app/router.tsx`)

| Route | Area | Status |
|-------|------|--------|
| `/sign-in` | Auth | COMPLETE — cookie login + Personal/staff login hints (no class inference) |
| `/` | Home | PARTIAL shell |
| `/no-location` | Branch binding | COMPLETE — zero Active accessible branches (RMAP-03) |
| `/workspace` | Workspace chooser | COMPLETE — multi Active branch chooser (RMAP-03) |
| `/settings/preferences` | Preferences | PARTIAL (theme/language) |
| `/sell` | Sell floor | PARTIAL (browse + cart + shift readiness; pay disabled); Organization AccountClass required |
| `/role/{owner\|manager\|cashier}` | Role homes | COMPLETE for experience eligibility (RMAP-02R); Organization AccountClass required |
| `/org` | Org essentials | Admin experience only (Owner/OrganizationAdministrator); invite Owner-only (RMAP-02R) |
| `/org/staff/invite` | Staff invite | Owner membership authority required (RMAP-02R) |
| `/catalog` | Catalog products | COMPLETE admin list (RMAP-04); ManageCatalog gated |
| `/catalog/categories` | Categories | COMPLETE CRUD-lite (RMAP-04) |
| `/catalog/products/new` | Product create | COMPLETE core fields (RMAP-04); UOM/price editors deferred |
| `/catalog/products/:productId/edit` | Product edit | COMPLETE core fields + image + concurrency (RMAP-04) |
| `/catalog/todays-prices` | Today's Prices | COMPLETE bulk price update (RMAP-06) — validation closeout complete |
| `/inventory` | Inventory list | COMPLETE tracking list (RMAP-07) — validation closeout complete; Not tracked language |
| `/inventory/:productId` | Inventory detail | COMPLETE enable/adjust/movements (RMAP-07) — validation closeout complete; lots excluded |
| `/registers` | Registers list | COMPLETE view (RMAP-10); ManageRegisters CRUD deferred |
| `/shifts` | Shifts hub | COMPLETE current + readiness (RMAP-10) |
| `/shifts/open` | Open shift | COMPLETE register + opening cash (RMAP-10); no PosDevice invented |
| `/shifts/:shiftId` | Shift detail / close | COMPLETE close + summary (RMAP-10) |
| `*` | Not found | COMPLETE |

## Area inventory

| Area | Route/components | Hooks/services/API | Tests | Implemented | Missing | Parity risk |
|------|------------------|--------------------|-------|-------------|---------|-------------|
| App shell | `AppShell`, `RootLayout`, top bar/account menu | layouts/components | foundation/shell tests | Shell chrome | Full MAUI hubs | Low for shell |
| Auth | `SignInPage`, staff invite/accept | `platform-auth-client`, `staff-invitation-client` | auth/e2e + RMAP-01/01b | Cookie login + staff invite/accept | register/activate parity | Medium |
| Session | `SessionProvider`, `RequireAccountClass`, `AllowInvitationAccept` | account-class | session tests | Session boot + class guards | MAUI offline session | Low |
| Workspace | `WorkspaceProvider`, chooser | workspace-resolver, Platform APIs | workspace tests | Org/branch binding; Personal no auto-bind | Owner ensure+select Organization (RMAP-02) | Medium |
| Shared UI kit | `components/exits`, `components/ui` | tokens in `globals.css` | `shared-ui-foundation.test.tsx` + foundation/e2e viewports | **PROVEN_CURRENT / COMPLETE** for RMAP-00 foundation primitives | Date/DateTime, Tabs, ToggleRow deferred | Low |
| Personal | `PersonalHomePage` | AccountClass guard | RMAP-01 e2e | Class-gated Personal home | Utang, shop, explore, start business | High if claimed complete |
| Sell floor | `SellFloorPage` | catalog client, cart provider, shift readiness | sell-floor + rmap-09/10 e2e | Browse/search/categories/units/weight/cart + shift banner | Pay/checkout; camera barcode | Medium |
| Catalog admin | `/catalog*` pages | `pos-catalog-client` CRUD + units + image | rmap-04/05 e2e + unit draft tests | Category/product CRUD, UOM/SellingMode/packages, SKU/barcode, concurrency | Today’s Prices/import | Medium |
| Cart | `SessionCartProvider` | in-memory | cart tests | Session cart with units/weight | Persist/outbox/server cart | Medium |
| Registers / shifts | `/registers`, `/shifts*` | registers/shifts/setup clients; ShiftContextProvider | rmap-10 e2e + readiness unit | Open/close shift; readiness gate; view registers | Register CRUD admin; sale POST | Low for gate; Medium for money |
| Checkout | disabled copy + readiness | no sale POST client | readiness unit + rmap-10 | Shift gate proven; Pay disabled | Entire sale pipeline; commercial discount UX | Critical |
| Org/staff/branches | `/org` shell + invite | experience + invite guards | RMAP-02R e2e | Admin experience; Owner invite | Full Org Web CRUD | Medium |
| Experience model | Owner chooser; role homes | `pos-capabilities` | RMAP-02R | Admin/Ops/Sell without role mutation | Custom roles | Low |
| Inventory/purchasing/suppliers/returns/reports/orders | inventory + expiry | inventory client | rmap-07/08 | Inventory + lots surfaces | Purchasing/suppliers/returns/reports/orders | Critical |
| PWA/SW | `pwa/*` | validate-pwa script | e2e pwa | Prod SW; blocked in dev | LocalStore offline ops | Medium |
| CSRF | `antiforgery.ts` | platform-http | antiforgery tests | Token handling | — | Low |
| API client | Platform + POS catalog/inventory/registers/shifts | `api/platform`, `api/pos` | | Growing POS surface | Sales/customers/… clients | Medium |

## Explicit non-claims

- Screen presence ≠ feature completeness.
- PWA cache ≠ encrypted LocalStore outbox.
- Role home routes ≠ role operational parity.

## Starting points for future WPs

Reuse: session, workspace binding, Platform HTTP+CSRF, sell-floor shell, cart provider patterns, i18n/preferences, Playwright harness.
