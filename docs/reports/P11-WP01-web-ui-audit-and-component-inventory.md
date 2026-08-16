# P11-WP01 — Web UI Audit and Component Inventory

Package: **P11-WP01 — Web UI Audit and Component Inventory**
Phase: Phase 11 — Web UI and Reporting Design System
Baseline tip (Pre-P11 theme persistence): `46b99a7f6baa87977fb0ed37e678231fa1eb1344`
Feature tip (this WP): `221fe69ab179956e8a73411cf3eb58fd6f199c3c`
Docs tip: `221fe69ab179956e8a73411cf3eb58fd6f199c3c`

## Status

**Complete.** Audit and inventory only. No UI redesign, no component moves, no broad refactor. Existing Admin shell/theme baseline preserved.

## Scope delivered

1. Inventory of reusable web UI across Platform Admin, DesignSystem, and POS MAUI
2. Classification of each surface (shared / Web-only / MAUI-only / feature-specific / consolidation candidate)
3. Token, localization, theme, responsive, and accessibility gap lists
4. Recommended shared-component boundary and prioritized roadmap for later WPs
5. Runtime regression of locked Admin theme/shell behavior

## Explicit exclusions

- No DesignSystem migration of Admin
- No new shared components created
- No business workflow changes
- No report framework implementation (P11-WP06+)
- Phase 12 / Product-Foundation docs untouched and uncommitted
- Typography remains **IBM Plex Sans / Source Sans 3 / system fallbacks** (Inter not adopted)

---

## 1. Surface counts (verified)

| Surface | Razor files | Role |
|---|---:|---|
| Platform Admin `Components/` | 37 | Web Admin shell + commercial pages |
| ExItS.DesignSystem | 60 | Shared Razor primitives (consumed by POS MAUI only today) |
| PinoyBusinessPOS MAUI `Components/` | 87 | Product shell + domain pages composing DesignSystem |

Admin does **not** reference `ExItS.DesignSystem`. POS MAUI loads `_content/ExItS.DesignSystem/exits-design-system.css` plus POS `app.css`.

---

## 2. Platform Admin — page inventory

| Route(s) | Page | Notes |
|---|---|---|
| `/admin` | `AdminDashboard` | Summary cards |
| `/admin/products`, `/admin/products/{Id}` | `Products` | List + detail |
| `/admin/organizations`, `/admin/organizations/{Id}` | `Organizations` | List + detail |
| `/admin/organizations/{OrganizationId}/members` | `OrganizationMembers` | Mutations + ConfirmDialog |
| `/admin/organizations/{OrganizationId}/product-access` | `OrganizationProductAccess` | Access evaluate/grant/revoke |
| `/admin/subscriptions`, `…/{Id}` | `Subscriptions` | Filters + lifecycle |
| `/admin/payments`, `…/{Id}` | `Payments` | Manual payments |
| `/admin/users`, `…/{Id}` | `Users` | User lifecycle |
| `/admin/entitlements`, detail, history routes | `Entitlements` | Multi-route file |
| `/admin/audit`, `/admin/audit/{AuditId}` | `Audit` | Permission-gated; FilterBar |
| `/not-found` | `NotFound` | |
| `/Error` | `Error` | |

All active Admin routes above are inventoried.

---

## 3. Platform Admin — component classification

### Shell / layout (Web-only; preserve baseline)

| Component | Classification | Notes |
|---|---|---|
| `App.razor` | Web-only | `theme-boot.js`, fonts, `data-permanent` |
| `Routes.razor` | Web-only | Router + MainLayout |
| `MainLayout.razor` | Web-only | **Production shell** — sidebar 16rem / collapsed 4.25rem, header, drawer |
| `AppShell.razor` | Web-only / consolidation candidate | Alternate shell; **not** default layout — duplicate chrome |
| `AdminNav.razor` | Web-only | Permission-gated nav; JS `closeDrawer` |
| `ReconnectModal.razor` | Web-only | Circuit reconnect |

### Shared Admin components

| Component | Classification | Consolidation vs DesignSystem |
|---|---|---|
| `ThemeSelector` | Web-only (+ host adapter pattern) | Keep Admin adapter; do not weaken theme authority |
| `LanguageSelector` | Web-only | Parallel to POS culture UI |
| `EnvironmentBanner` | Web-only / Admin-specific | Keep |
| `PageHeader` | Shared candidate | **Duplicate** of DS `PageHeader` |
| `LoadingState` / `EmptyState` / `ErrorState` | Shared candidate | **Duplicate** of DS feedback |
| `StatusBadge` | Shared candidate | Overlaps DS `Badge` / `StatusCell` |
| `UtcTimestamp` | Web/Admin-specific (reusable pattern) | No DS twin; keep or promote later |
| `NavIcon` | Shared candidate (glyphs) | Overlaps DS `IconGlyphs` |
| `SummaryCard` | Shared candidate (KPI) | Partial overlap with DS `Card` |
| `ConfirmDialog` | Shared candidate | **Strong duplicate** of DS `ConfirmDialog` |
| `ToastHost` | Shared candidate | **Duplicate**; pages also use inline `_toast` |
| `FilterBar` | Shared candidate | **Duplicate**; used only on Audit |
| `SearchInput` | Shared candidate | **Unused**; overlaps DS `SearchBox` |
| `AccessStateIndicator` | Admin feature-specific | Keep |
| `UnauthorizedPanel` | Admin/Web-only | Keep |
| `AuditTimeline` | Admin feature-specific | Keep |

### Feature pages

All `Pages/*.razor` commercial/compliance pages: **feature-specific** (compose shared Admin primitives + CSS classes).

---

## 4. DesignSystem inventory (shared Web + MAUI candidate)

Authoritative primitives under `src/Shared/ExItS.DesignSystem` (60 Razor files), including:

- **Primitives:** Button, IconButton, Badge, TextInput, Select, Switch, Label, Divider, Spinner, Skeleton, Avatar
- **Layout:** Page, PageHeader, Section, SectionHeader, Toolbar, ActionBar, Stack, Grid, Card, Surface, Accordion, Dropdown
- **Overlay/feedback:** ConfirmDialog, Dialog, Drawer, ToastHost, Alert, Tabs, LoadingOverlay, EmptyState, ErrorState, InlineMessage, Progress, SearchBox
- **Forms/data:** FormField/Group/Actions, validation messages, Checkbox, RadioGroup, Currency/Number/Date/Time/TextArea, MoneyDisplay, DataTable, ResponsiveDataList, MobileRowCard, FilterBar, SearchToolbar, SortControl, Pagination*, StatusCell

**Contracts:** `IThemePreferenceStore`, `IDensityPreferenceStore`, `ICulturePreferenceStore`, `ThemePreference`, `DensityMode`.

**Consumed by:** POS MAUI only. **Not consumed by:** Platform Admin.

---

## 5. POS MAUI inventory (summary)

| Area | Classification |
|---|---|
| `PosShell` / `AuthShell` / `app.css` POS chrome | MAUI-only / product shell |
| Domain pages (catalog, customers/utang, sales, inventory, purchasing, suppliers, expenses, shifts, registers, permissions, reporting) | Feature-specific |
| Theme/Density/Culture controllers + Preferences stores | MAUI-only adapters over shared contracts |
| `Maui/wwwroot/theme-boot.js` | MAUI-only (`exits-pos-theme` PascalCase + density) |
| Device APIs (Preferences, SecureStorage, Connectivity, FileSystem, Share, BlazorWebView) | MAUI-only |

Reporting pages (`ReportsHub`, sales/utang/inventory/expenses reports, `OperationalReportPage`, dashboard) exist as **feature pages**, not a shared Admin report framework yet.

---

## 6. Design-token inventory

### Admin (`app.css`) — locked baseline

Canonical: `--color-*`, `--shadow-sm|md|sidebar`, `--radius-sm|md`, `--motion-fast|base|easing`, `--font-sans` (IBM Plex Sans / Source Sans 3), `--sidebar-width: 16rem`, `--sidebar-width-collapsed: 4.25rem`, `--header-height: 3.5rem`.

Thin `--exits-*` aliases (incomplete vs DesignSystem). Dark via `[data-theme="dark"]` and system media query.

### DesignSystem (`exits-design-system.css`)

Full `--exits-*` color/surface, shadow, radius, motion, spacing scale, type scale, breakpoints, z-index, density/touch tokens. Different palette (green-leaning) from Admin teal.

**Later (2026-08):** POS production mobile visual system extends Design System authority with semantic typography roles, 48dp touch minimum, `QuantityStepper`, and `--pos-*` aliases — see [production-mobile-design-system](../specs/mobile/production-mobile-design-system.md) and [mobile-production-ui-redesign](mobile-production-ui-redesign.md). Admin remains on its own `--color-*` stack (no Inter/IBM Plex convergence forced here).

### Recommendation (later WPs)

Keep Admin `--color-*` + typography stack authoritative for Platform Admin through shell work. Decide token convergence in P11-WP02/WP05 **without** replacing the working theme mechanism or Inter adoption.

---

## 7. Theme coverage report

| Host | Storage key | Values | Authority | Reapply |
|---|---|---|---|---|
| Admin | `exits-admin-theme` | lowercase `system\|light\|dark` | `exitsAdminTheme` + `ThemeService` | `Blazor.addEventListener('enhancedload')`, `LocationChanged`, `pageshow`, `data-permanent`, html+body |
| POS | `exits-pos-theme` | PascalCase `System\|Light\|Dark` | `exitsPosTheme` + Preferences | First paint boot + controllers |

**Do not unify storage keys or weaken Admin reapply stack in later WPs without explicit authorization.** Dual keys are correct (product separation).

Admin regression (this WP): Dark/Light/System behaviors verified against live Admin (see §12).

---

## 8. Localization coverage report

| Surface | en | fil-PH | Notes |
|---|---|---|---|
| Admin | `AdminResources` | `AdminResources.fil-PH` | Cookie culture + `LanguageSelector`; force reload |
| DesignSystem | DS / Validation / Error resx | fil-PH pairs | Used by POS |
| POS pages | Product resource sets | Present for MVP flows | Gaps remain open risks (R-008 family) |

Not a full string-by-string audit; inventory confirms infrastructure exists on both hosts.

---

## 9. Responsive-layout audit

| Surface | Breakpoints / behavior | Gaps |
|---|---|---|
| Admin | Drawer ≤1024px; responsive-table ≤800px; close-on-nav | Nested org tables may be partial; no shared pagination |
| DesignSystem | `--exits-bp-*`; ResponsiveDataList / MobileRowCard | Admin not using these |
| POS | Phone-first shell, bottom nav, safe-area | Device evidence still R-109 |

---

## 10. Accessibility gap list (inventory-level)

Open / incomplete (not newly introduced):

- Focus trap consistency for Admin ConfirmDialog vs DS Dialog
- Color-only status risk mitigated partly by text in StatusBadge — formal a11y sign-off still open (R-095 / R-008)
- Live-region usage inconsistent (ToastHost vs inline toast)
- Skip-link present in Admin CSS; landmark completeness not fully validated
- Reduced motion honored in Admin `app.css`; keep

---

## 11. Duplicate / consolidation report

### Highest-priority consolidation candidates (later WPs)

1. ConfirmDialog / PageHeader / EmptyState / ErrorState / ToastHost / FilterBar
2. StatusBadge ↔ Badge/StatusCell
3. SearchInput (unused Admin) ↔ SearchBox
4. Dual Admin shells: retire or justify `AppShell` vs `MainLayout`
5. Inline `_toast` on mutation pages vs `ToastService`/`ToastHost`
6. Theme boot **kernel** shared conceptually — **adapters must stay separate** (Admin enhanced nav vs POS Preferences)

### Explicit non-consolidation (keep separate)

- Admin sidebar/top-bar locked dimensions and theme authority
- POS bottom nav / sync chrome / safe-area
- Admin EnvironmentBanner, UnauthorizedPanel, AccessStateIndicator, AuditTimeline
- MAUI device APIs

### CSS duplication

Parallel semantic token systems (`--color-*` vs `--exits-*`); parallel dialog/toast/state class names (`.dialog` vs `.exds-dialog*`).

---

## 12. Environment dependency map

| Dependency | Admin | POS MAUI |
|---|---|---|
| Browser JS | `theme-boot.js`, `exitsAdminShell` | `theme-boot.js`, `exitsPosTheme` |
| localStorage | theme + culture keys | WebView mirror of Preferences |
| Blazor enhanced navigation | **Yes** — theme must reapply | N/A (WebView) |
| MAUI Preferences / SecureStorage / Connectivity / FileSystem / Share | No | Yes |
| Native lifecycle / BlazorWebView | No | Yes |

---

## 13. Recommended shared-component boundary

```text
ExItS.DesignSystem
  └── reusable Razor primitives + --exits-* tokens + preference contracts
        ├── consumed today by POS MAUI
        └── future optional consumer: Platform Admin (only via approved WP)

Platform Admin Web
  └── shell/layout (MainLayout, AdminNav, locked CSS tokens/typography/theme stack)
  └── Admin-specific (EnvironmentBanner, UnauthorizedPanel, AuditTimeline, …)
  └── thin adapters (ThemeSelector/ThemeService → exitsAdminTheme) — DO NOT REPLACE AUTHORITY
  └── feature pages

POS MAUI
  └── product shells (PosShell, AuthShell, POS app.css)
  └── MAUI preference/device adapters
  └── feature pages composing DesignSystem
```

### Naming / placement conventions (approved for later WPs)

| Kind | Placement | Naming |
|---|---|---|
| Shared primitive | `ExItS.DesignSystem/Components/{Primitives\|Layout\|Forms\|Data\|Overlay\|Feedback}` | PascalCase; CSS `exds-*` |
| Admin shared (Web-only) | `Platform.Admin/Components/Shared` | PascalCase; CSS in `app.css` without inventing a second design system |
| Admin shell | `Platform.Admin/Components/Layout` | Keep MainLayout as authority |
| POS feature | `Maui/Components/Pages/...` | Feature folders |
| Do not | Nest legacy product / Ant Design into Admin or DesignSystem | — |

---

## 14. Prioritized refactoring plan (roadmap only)

| Priority | Later WP target | Work |
|---:|---|---|
| 1 | P11-WP02 | Shell/nav polish **preserving** locked sizes/theme; resolve AppShell vs MainLayout |
| 2 | P11-WP03–WP04 | Shared form/table/state consolidation; wire ToastService consistently |
| 3 | P11-WP05 | Theme/token review — Admin authority untouched; optional DS alignment plan |
| 4 | P11-WP06+ | Report framework from POS reporting pages + DS data primitives |
| 5 | Deferred | Admin consumes DesignSystem project reference (only if authorized) |

---

## 15. Runtime regression evidence

Host: `http://127.0.0.1:5289/admin` (live Admin; Playwright).

| Check | Result |
|---|---|
| Dark across sidebar nav (Products→…→Audit→Dashboard) | Pass — `html`/`body` `data-theme=dark`, storage `dark` |
| Dark after refresh | Pass |
| Light across nav + refresh | Pass |
| System stored as `system` | Pass |
| No `Hello, world!` / unstyled shell | Pass (`app-shell` present) |
| Theme hook | `__exitsThemeEnhancedBound=true`; `Blazor.addEventListener` path |
| Sidebar/header tokens | Unchanged: 16rem / 4.25rem / IBM Plex stack |
| Mobile drawer close-on-nav | Code path unchanged (`exitsAdminShell.closeDrawer`); not redesigned |

Theme mechanism files were **not** modified in this WP.

---

## 16. Tests

- Full `ExItS.slnx` Release: **1160 passed / 0 failed / 0 skipped** (unchanged baseline; docs-only WP)
- Baseline entering WP: **1160 passed / 0 failed / 0 skipped**
- Runtime theme regression script: Dark/Light/System persistence **pass**; `app-shell` present; no Hello world
- Served Admin CSS contains locked tokens: `--sidebar-width: 16rem`, collapsed `4.25rem`, IBM Plex Sans, reduced-motion

## 17. Portfolio independence

- No root a nested foreign product tree
- Git tracking shows no nested foreign product tree empty
- Solution has no legacy product project

## 18. Risks / open decisions

- R-091 production auth; R-109 Android interactive; R-129 / NU1903; TLS; contrast/a11y residual risks
- Whether Admin should ever reference DesignSystem (open decision — not decided here)
- Token palette convergence Admin teal vs DS green (open)
- Unused Admin `SearchInput` and dual toast patterns (debt for later WP)

## Exact next

**P11-WP02 — Global Web Layout and Navigation** when explicitly authorized. Preserve locked shell/theme baseline.
