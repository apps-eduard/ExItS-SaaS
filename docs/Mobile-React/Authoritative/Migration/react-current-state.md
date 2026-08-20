# React Current State

**Client:** `src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Client`
**Package:** `@exits/pinoy-business-pos-client`
**Baseline SHA:** `721cc946d61ccb193c8c69b76b6f1ff726526270`

Do not treat prior WP labels as completeness. Inventory against routes and code.

## Routes (`src/app/router.tsx`)

| Route | Area | Status |
|-------|------|--------|
| `/sign-in` | Auth | COMPLETE for browser cookie login + local-validation helper |
| `/` | Home | PARTIAL shell |
| `/workspace` | Workspace chooser | COMPLETE for current resolver scope |
| `/personal` | Personal | SHELL_ONLY (`PersonalHomePage` placeholder) |
| `/no-location` | Branch binding | COMPLETE for no-branch gate |
| `/settings/preferences` | Preferences | PARTIAL (theme/language) |
| `/sell` | Sell floor | PARTIAL (browse + cart; pay disabled) |
| `/role/{owner\|manager\|cashier}` | Role homes | SHELL_ONLY / PARTIAL |
| `/org` | Org essentials | SHELL_ONLY |
| `*` | Not found | COMPLETE |

## Area inventory

| Area | Route/components | Hooks/services/API | Tests | Implemented | Missing | Parity risk |
|------|------------------|--------------------|-------|-------------|---------|-------------|
| App shell | `AppShell`, `RootLayout`, top bar/account menu | layouts/components | foundation/shell tests | Shell chrome | Full MAUI hubs | Low for shell |
| Auth | `SignInPage` | `platform-auth-client`, antiforgery | auth/e2e | Cookie login/logout/me | Desired staff person-link invite UX; register/activate parity | Medium — CURRENT staff login string may work; desired person-link BLOCKED on RMAP-B00 |
| Session | `SessionProvider`, guards | pos-access-token, pos-session-grant | session tests | Session boot | MAUI offline session | Medium |
| Workspace | `WorkspaceProvider`, chooser | workspace-resolver, Platform APIs | workspace tests | Org/branch binding | Start Business, multi-product depth | Medium |
| Shared UI kit | `components/exits`, `components/ui` | tokens in `globals.css` | `shared-ui-foundation.test.tsx` + foundation/e2e viewports | **PROVEN_CURRENT / COMPLETE** for RMAP-00 foundation primitives (SearchField, ListToolbar, EntityCard, money/qty, sheets/dialogs, form/states) | Date/DateTime, Tabs, ToggleRow deferred to first consumer; domain tiles later | Low for foundation — reuse in later WPs |
| Personal | `PersonalHomePage` | none meaningful | | Placeholder | Utang, shop, explore, start business | High if claimed complete |
| Sell floor | `SellFloorPage` | catalog client, cart provider | sell-floor e2e | Browse/search/categories/cart UI | ByWeight, sell units, stock gates, pay | High — looks like POS but checkout disabled |
| Catalog admin | — | read-only `pos-catalog-client` | catalog-cart tests | Read for sell | CRUD/units/Today’s Prices/import | High |
| Cart | `SessionCartProvider` | in-memory | cart tests | Session cart | Persist/outbox/server cart | High |
| Checkout | disabled copy | no sale POST client | | Explicit non-implementation | Entire sale pipeline | Critical |
| Org/staff/branches | `/org` shell | — | | Essentials placeholder | All admin | High |
| Inventory/purchasing/suppliers/shifts/returns/reports/orders | — | — | | None | All | Critical |
| PWA/SW | `pwa/*` | validate-pwa script | e2e pwa | Prod SW; blocked in dev | LocalStore offline ops | Medium (don’t confuse with offline POS) |
| CSRF | `antiforgery.ts` | platform-http | antiforgery tests | Token handling | — | Low |
| API client | Platform complete; POS catalog only | `api/platform`, `api/pos` | | Limited | Sales/inventory/customers/… clients | Critical |

## Explicit non-claims

- Screen presence ≠ feature completeness.
- PWA cache ≠ encrypted LocalStore outbox.
- Role home routes ≠ role operational parity.

## Starting points for future WPs

Reuse: session, workspace binding, Platform HTTP+CSRF, sell-floor shell, cart provider patterns, i18n/preferences, Playwright harness.
