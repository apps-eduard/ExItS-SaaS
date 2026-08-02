# P16-WP11 — Ant Design Blazor UI Cleanup (Stabilization)

> **Superseded for operator stack naming.** Live Preview identity selector / SWA materializer work below was removed; Admin now uses normal credential login against **Local Validation**.
> See [P16-WP11 Local Validation replaces Live Preview](P16-WP11-local-validation-replaces-live-preview.md).

| Field | Value |
|---|---|
| Status | **Complete** for Ant Design cleanup pass (stabilization after P16-WP10; does not close P16-WP11) |
| Scope | Platform Admin validation flows + shared shell/login infrastructure |
| Date | 2026-08-02 |

## Purpose

Remove obsolete native CSS/JS/HTML that conflicted with Ant Design Blazor on login, organization switcher, enabled-products, and shared shell chrome — without a blind global rewrite or product UI redesign. The former Live Preview identity selector and SWA materializer were removed in favor of Local Validation + normal `/auth/login`.

## Frontend cleanup inventory

| File or component | Previous issue | Action | Retained reason, if any | Validation |
|---|---|---|---|---|
| `Components/Pages/Login.razor` | Native SSR inputs/`select`; dual stack with Ant chrome | Converted to Ant `Input`, `InputPassword`, `Button`, `Select`, `Alert`, `Spin`, `Empty`, `Card` | SSR POST routes in `Program.cs` retained for architecture guards / non-interactive fallbacks | Guard test + build |
| Live Preview identity selector (on Login) | Native `<select>` styling risk; unclear Dev banner | Ant `Select` + Dev/Live Preview `Alert`; gated by `LivePreview:Enabled`; labels = display name + summary | Catalog currently **5** identities (not 8) — see debt | Guard test; catalog unit tests |
| `OrganizationContextSwitcher.razor` | Native `<select class="org-context-select">` | Ant `Select` + localized loading/empty/error | Organization-shell only; Platform Administration excluded | Guard test |
| `OrganizationEnabledProducts.razor` | Native role `<select>`; plain warning | Ant `Select` + `Alert`; launch uses `ant-btn-*` | Residual `AdminInput` on assign-role form | Guard test |
| `wwwroot/app.css` | Dead login native/`org-context-select` rules; missing residual wrapper styles | Removed obsolete native login/select rules; added documented residual `.form-control` / `.btn-primary` | Shell branding, density, login layout, StatusBadge, dark tokens | QA + architecture CSS guards |
| `wwwroot/admin-a11y.js` | Dead drawer/sidebar a11y | Removed drawer logic; kept ConfirmDialog focus trap | ConfirmDialog still needs focus trap | QA hardening test |
| `wwwroot/theme-boot.js` | Dead `exitsAdminShell.closeDrawer` | Removed | Theme/culture boot + Ant dark CSS swap remains required | Architecture guard |
| Live Preview Admin identity selector / SWA materializer | Staging SWA junctions + Dev identity login | **Removed** from Admin (credential login only; Development for local assets) | API/deploy Live Preview packaging retained for Phase 14 | Build + Admin unit tests |
| `Start-LivePreviewLocal.ps1` Admin host | Staging + `LivePreview__Enabled` | Admin starts as **Development**, `--no-launch-profile` | Platform/POS API may still enable Live Preview seed | Script start |
| Account recovery / password pages | Native `form-control` / `btn-primary` | **Not migrated** this WP | Out of Phase 16 validation chrome; residual CSS added | Documented debt |
| Payments / Audit / AdminInput wrappers | Dual native wrappers vs Ant | **Not migrated** this WP | Broader Admin form migration | Documented debt |
| Personal shell | N/A in Admin | No separate Personal shell layout in Admin | Limited/organization/platform shells via `AdminShellContext` | Reviewed |

## Removals summary

| Category | Items |
|---|---|
| Legacy CSS removed | `.exits-native-input`, `.exits-native-select`, login shell native overrides, `.org-context-select` |
| Legacy JavaScript removed | Drawer a11y in `admin-a11y.js`; `exitsAdminShell.closeDrawer` in `theme-boot.js` |
| Obsolete Razor components removed | None (no unreachable shell duplicates found; `AppShell` already absent) |
| Unused package references removed | None (AntDesign only; Fluent already forbidden) |
| Global selectors narrowed | No new `button`/`input`/`select` element-wide rules; residual classes are class-scoped |
| Ant Design overrides retained | `.exits-admin-menu.ant-menu-*` density; `.exits-nav-drawer .ant-drawer-body` padding; `.exits-login-form .ant-form-item*` spacing — scoped under app-owned roots for compact Admin density |

## Ant Design overrides (justification)

| Selector | Reason |
|---|---|
| `.exits-admin-menu.ant-menu-inline` / item heights | Compact Platform Admin sider density (Phase 15) |
| `.exits-nav-drawer .ant-drawer-body` | Mobile nav drawer content flush with menu |
| `.exits-login-form .ant-form-item*` | Login card field spacing under owned login shell |

## Remaining known frontend debt

1. **Residual native wrappers**: `AdminInput`, `AdminSelect`, `AdminTextArea`, account recovery/password pages, Payments/Audit form sections still use `form-control` / `btn-primary`.
2. **Dual notification stacks**: `ToastService` vs Ant `Message`/`Notification` on some pages.
3. **Personal shell**: Admin uses Platform / Organization / limited contexts; no dedicated Personal Utang Admin shell (product/Personal surfaces may live elsewhere).
4. **Local Validation Admin host** uses Development for static assets (no SWA materializer). Production packaging remains separate.

## Automated checks

- `AdminArchitectureGuardTests` Ant Design / no-materializer / no-LivePreview guards
- `AdminArchitectureGuardTests.Enabled_products_page_uses_antdesign_select_and_alert`
- Updated org-switcher + QA CSS guards
- `LocalValidationIdentityCatalogTests` (8 approved named identities)
- Fluent / Tailwind / MudBlazor package absences already guarded

## Manual / browser validation

| Check | Result |
|---|---|
| Login renders with Ant controls | Verify via Local Validation stack |
| Credential login only | No identity quick-login selector |
| Platform / Organization shells | Unchanged MainLayout + AdminNav |
| Organization switcher Ant Select | Converted |
| Menus / z-index | No new overlay JS; Ant Drawer/Dropdown retained |
| Console / missing assets | Verify after Local Validation start (Development Admin host) |
| Authorization | Unchanged server-side session + policies |

**Claim boundary:** This review covered Platform Admin validation-related UI and shared shell/login assets. It does **not** claim all legacy UI is removed solution-wide (POS/MAUI and residual Admin form pages remain).

## Build / test evidence

```powershell
dotnet build src/Platform/ExItS.Platform.Admin/ExItS.Platform.Admin.csproj -c Debug
dotnet test tests/ExItS.Platform.Admin.UnitTests/ExItS.Platform.Admin.UnitTests.csproj -c Release
# Result: Passed 70 / Failed 0 (Release)
```
