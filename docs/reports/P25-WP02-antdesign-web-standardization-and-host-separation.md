# P25-WP02 — AntDesign Web Standardization and Web Host Separation

## 1. Assignment

| Field | Value |
|---|---|
| Phase | 25 |
| Work package | P25-WP02 AntDesign Web Standardization and Web Host Separation |
| Status | Code Complete / Ready for Owner Validation |
| Branch | `main` |
| Date | 2026-08-13 |
| Device Verified | **No** |
| Production Ready | **No** |

## 2. Architecture decision

All ExItS **browser** applications use **AntDesign Blazor 1.6.2**, centralized in `Directory.Packages.props`. Shared conventions live in `ExItS.Web.UI`. POS/MAUI remains DesignSystem ([ADR-022](../decisions/ADR-022-separated-antdesign-web-hosts-and-unified-auth.md)).

```text
AntDesign Blazor 1.6.2
        │
        ▼
ExItS.Web.UI (shell conventions, theme, culture, pager, safe return)
        │
   ┌────┼─────────────┐
   │    │             │
Admin  Org Web    Personal Web
:8090  :8093         :8094
```

## 3. Central package version

| Control | Value |
|---|---|
| Source of truth | `Directory.Packages.props` `Include="AntDesign" Version="1.6.2"` |
| Hosts | Platform.Admin, PinoyBusinessPOS.Web, Personal.Web, ExItS.Web.UI |
| Upgrade | Not part of this WP |

## 4. Before / after project ownership

| Surface | Before | After |
|---|---|---|
| Platform operator console | Platform.Admin AntDesign | **Same** |
| Organization management | Org Web DesignSystem :8093 | Org Web **AntDesign** :8093 |
| Personal product UI | Platform.Admin Personal pages | **Personal.Web** AntDesign :8094 |
| POS checkout / cashier | MAUI DesignSystem | **Unchanged** |

## 5. Route migration matrix

| Route | Was | Scope | AntDesign? | Target | Replacement | Status |
|---|---|---|---|---|---|---|
| `/admin/*` platform users, orgs, plans, payments, catalog, privacy | Admin | Platform | Yes | Admin | remain | Live |
| `/admin/personal-features` | Admin | Platform config | Yes | Admin | remain | Live |
| `/admin/personal/*` utang/people/lent/borrowed/invitations/notifications/profile/settings | Admin | Personal | Yes | Personal Web | `/utang/*`, `/notifications`, `/profile`, `/settings` | Compatibility redirect |
| `/admin/personal/start-business` | Admin | Personal | Yes | Admin form + Personal CTA | form remains Admin (catalog deps); Personal `/start-business` forceLoads | Split |
| `/overview` `/products` `/inventory` `/customers` `/staff` `/organization/*` `/reports/*` `/operations/*` `/settings` `/notifications` `/account/subscription` | Org Web | Organization | **Now yes** | Org Web | same routes | Migrated in place |
| Checkout / cart / New Sale | none on web | forbidden | — | none | none | Guarded |

Authorization is taken from shell/account class, not from the route name alone. Linked customers do not receive Organization Admin.

## 6. Organization functionality preserved

P25-WP01 management surfaces remain: Overview, profile, branches, staff/invites/roles, products/categories, Global Catalog, inventory, adjustments, transfers, expiration lots, customers/link status, devices/registers, cashier shifts (inspect), read-only sales, reports, Utang, CashCountMode, settings, subscription, notifications.

Preserved: API clients, server guards, pagination, bounded overview query, **no checkout**.

## 7. Personal functionality preserved

Migrated from Admin Personal pages: Home, Contacts/People, I Lent, I Borrowed, invitations, notifications, profile, settings, Start a Business CTA.

Not fabricated: linked-merchant statements/receipts/rewards remain MAUI-only (they were not Admin Personal browser pages).

## 8. Platform Admin cleanup

Platform-only navigation remains. Organization shell in Admin still has operator org pages plus **Open Organization Web**. Personal product routes redirect to Personal Web. Duplicate live Personal tables are not the production path once redirected.

## 9. Shared web components

`ExItS.Web.UI`: `ExitsPageHeader`, `ExitsEmptyState`, `ExitsAccessDenied`, `ExitsPager`, `ExitsThemeSelector`, `ExitsLanguageSelector`, `exits-web.css`, `theme-boot.js` (`exits-web-theme` Light/Dark/System).

## 10. Local ports / production 443

| Local | Service |
|---|---|
| 8090 | Platform Admin |
| 8091 | Platform API |
| 8092 | POS API |
| 8093 | Organization Web |
| 8094 | Personal Web |

Production: public HTTPS :443. Conceptual mapping `platform.` / `org.` / `personal.` hostnames (or approved path routing) → private app ports. Do not expose 8090/8093/8094 as public UX. No real production domain is hard-coded.

## 11. Migrations

**No.** UI/hosting only.

## 12. Tests (this WP)

Architecture: AntDesign required on all three hosts; version centralized; Org/Personal have no Infra/EF; Org/Personal have no checkout; solution contains three web projects; LV 8094.

## 13. Known issues

- Admin organization-shell pages still exist as compatibility; product UX is Org Web.
- Start a Business full form remains on Admin.
- In-memory handoff store is single-process (documented in WP03).
- Owner browser validation pending.

## 14. Owner checklist

See [P25-WP03](P25-WP03-unified-web-authentication-sso-and-workspace-routing.md) combined browser checklist (items 1–31 cover WP02 UI). **Device Verified: No.**

## 15. Git

Starting SHA: `9a3be47879dc89cf392ae3a0ef84d209cc52e2ef`

| Commit | Message |
|---|---|
| `86938a1d` | chore(web): add shared AntDesign web UI conventions |
| `1d723eeb` | refactor(org-web): migrate organization admin to AntDesign |
| `d743128a` | feat(personal-web): add dedicated AntDesign personal web host |
| `9f4be5b` | feat(auth): add unified web workspace routing |
| `4fdddfe5` | test(web): cover web host and SSO boundaries |
