# Organization — Enterprise Hybrid Glass (UI Standards live reference)

**Status:** ACTIVE (DEV / local validation only)  
**Route:** `/ui-standards`  
**Isolation:** `.org-ehg` / `[data-org-ehg]` / `data-exits-product="organization"`  
**Does not change:** Personal Soft Glass, production Organization page layouts

## Product separation

| Product | Visual identity | Scope |
| --- | --- | --- |
| **Personal** | Soft Glass | `.personal-*`, Soft Glass locks — unchanged |
| **Organization** (reference) | Enterprise Hybrid Glass | UI Standards live pages only until migration is approved |

## UI Standards navigation

Parent menu: **Organization — Enterprise Hybrid Glass** (only sidenav group on `/ui-standards`).

| Page | `?category=` | Live source |
| --- | --- | --- |
| Organization Dashboard | `org-ehg-dashboard` (default) | `DashboardRoutePage` → POS reporting |
| Inventory & Warehouse | `org-ehg-inventory` | `InventoryListPage` → POS inventory |
| Customer Orders & Payments | `org-ehg-orders` | `SellerOrdersPage` → POS customer orders |
| Stock Requests & Transfers | `org-ehg-stock-requests` | `StockRequestListPage` → POS stock requests |
| Branch Settings & Connected Commerce | `org-ehg-branch-settings` | Branch fulfillment + Connected Commerce + payment methods |
| Button glass | `org-ehg-button-glass` | Glass button gallery + playground |
| Input glass | `org-ehg-input-glass` | Glass input gallery + playground |
| Card glass → Menu card | `org-ehg-card-glass-menu` | Menu glass card gallery + playground |
| Card glass → Soft KPI | `org-ehg-card-glass-soft-kpi` | Soft KPI glass card samples |
| Card glass → Hybrid KPI | `org-ehg-card-glass-hybrid-kpi` | Hybrid KPI glass card samples |
| Card glass → Solid data | `org-ehg-card-glass-solid` | Solid data glass card samples |
| Card glass → Activity timeline | `org-ehg-card-glass-timeline` | Timeline glass card samples |
| Card glass → Orders table | `org-ehg-card-glass-table` | Orders table glass card samples |
| Card glass → Settings form | `org-ehg-card-glass-settings` | Settings form glass card samples |

Legacy sample ids (`org-ehg-overview`, `org-ehg-components`, `org-ehg-forms`, `org-ehg-appearance`) redirect to the live set.

## Playground background options (experimental)

**Status:** EXPERIMENTAL — Background dropdown in Button / Input / Menu glass playgrounds paints that playground card and the entire `/ui-standards` shell. Light/dark follows Preferences (playground theme toggle beside Reset). Do **not** apply to production Organization or Personal UI until approved.

| Option | Id | Notes |
| --- | --- | --- |
| Soft Ambient Gradient (recommended) | `soft-ambient` | Soft edge lighting; works in light and dark |
| Subtle Mesh Gradient | `subtle-mesh` | Soft multi-blob depth; restrained |
| Soft Aurora Background | `soft-aurora` | Flowing blue/violet bands; stronger in dark |
| Neutral Solid Surface | `neutral-solid` | Clean ramp; least glass contrast |
| Soft Vignette | `soft-vignette` | Clear center, soft edge shade |
| Horizon Wash | `horizon-wash` | Cool top / soft floor gradient |
| Spotlight Glow | `spotlight-glow` | Single top-center primary wash |

Shared helper: `orgEhgPlaygroundBackground.ts`. CSS: `.org-ehg-pg-bg` + `.org-ehg-bg-lab__*` in `org-enterprise-hybrid-glass.css`. Personal Soft Glass unchanged.

## Real-data mode

- Uses bound Organization / branch from Workspace.
- Platform API (org/branch/fulfillment) and POS API (dashboard, inventory, orders, stock requests, commerce) via existing clients.
- Loading / error / empty states when APIs or permissions fail — no fabricated live metrics.
- Mutations are intentional, permission-gated, and affect shared development databases (5178 and Docker preview share PostgreSQL).

## Sidenav / topbar chrome

Hybrid Glass **look** on UI Standards sidenav and `.app-top-bar--ui-standards` only.

**Data bindings preserved:** ExItS brand / “ExItS UI Standard” title, sidenav open/toggle, connection / preferences / account. No StockPro mock org switchers or fake user chrome.

## CSS isolation

1. Every Org Hybrid Glass rule is under `.org-ehg` / `[data-org-ehg]` (plus UI Standards-only sidenav modifiers in `ui-standards.css`).
2. Do **not** add `.org-ehg` rules to `globals.css`.
3. Do **not** redefine `--soft-glass-*` or `.personal-*` in Org CSS.
4. Shared components keep shared behavior; wrap live pages in `OrgEhgLiveFrame`.
5. Prefer semi-opaque glass for nav — avoid heavy fullscreen `backdrop-filter`.

## Backend ownership

| Concern | API |
| --- | --- |
| Branch fulfillment, hours, delivery areas | Platform |
| Dashboard, inventory, orders, stock requests, payments, Connected Commerce | POS |

## Migration gate

Do **not** redesign live Organization production pages until this reference set is reviewed and approved. When migrating, keep Personal Soft Glass untouched and move approved recipes into an Organization-scoped stylesheet.

## Tests

- `org-ehg-isolation.test.ts` — five-page nav, selector scope, no globals leak, no Personal rewrite, no product imports
- `UiStandardsPage.test.tsx` — Org Hybrid Glass shell + sidenav toggle (legacy playground suite skipped)
- Lock/playground unit tests remain for canonical component standards
