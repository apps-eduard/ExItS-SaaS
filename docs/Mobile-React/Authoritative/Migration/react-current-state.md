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
| `/sell` | Sell floor | PARTIAL (browse + cart; pay disabled); Organization AccountClass required |
| `/role/{owner\|manager\|cashier}` | Role homes | COMPLETE for experience eligibility (RMAP-02R); Organization AccountClass required |
| `/org` | Org essentials | Admin experience only (Owner/OrganizationAdministrator); invite Owner-only (RMAP-02R) |
| `/org/staff/invite` | Staff invite | Owner membership authority required (RMAP-02R) |
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
| Sell floor | `SellFloorPage` | catalog client, cart provider | sell-floor e2e | Browse/search/categories/cart UI | ByWeight, sell units, stock gates, pay | High |
| Catalog admin | — | read-only `pos-catalog-client` | catalog-cart tests | Read for sell | CRUD/units/Today’s Prices/import | High |
| Cart | `SessionCartProvider` | in-memory | cart tests | Session cart | Persist/outbox/server cart | High |
| Checkout | disabled copy | no sale POST client | | Explicit non-implementation | Entire sale pipeline | Critical |
| Org/staff/branches | `/org` shell + invite | experience + invite guards | RMAP-02R e2e | Admin experience; Owner invite | Full Org Web CRUD | Medium |
| Experience model | Owner chooser; role homes | `pos-capabilities` | RMAP-02R | Admin/Ops/Sell without role mutation | Custom roles | Low |
| Inventory/purchasing/suppliers/shifts/returns/reports/orders | — | — | | None | All | Critical |
| PWA/SW | `pwa/*` | validate-pwa script | e2e pwa | Prod SW; blocked in dev | LocalStore offline ops | Medium |
| CSRF | `antiforgery.ts` | platform-http | antiforgery tests | Token handling | — | Low |
| API client | Platform complete; POS catalog only | `api/platform`, `api/pos` | | Limited | Sales/inventory/customers/… clients | Critical |

## Explicit non-claims

- Screen presence ≠ feature completeness.
- PWA cache ≠ encrypted LocalStore outbox.
- Role home routes ≠ role operational parity.

## Starting points for future WPs

Reuse: session, workspace binding, Platform HTTP+CSRF, sell-floor shell, cart provider patterns, i18n/preferences, Playwright harness.
