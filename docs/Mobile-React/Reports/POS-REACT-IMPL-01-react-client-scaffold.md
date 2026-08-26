# POS-REACT-IMPL-01 — React Client Scaffold

**Package:** POS-REACT-IMPL-01  
**Status:** Complete on `feat/pos-react-client`  
**Gate:** C (scaffold only)  
**Starting SHA:** `0954c1d11e5b9130f8411afb3f086c7e116d76ff`  
**Commit:** `chore(pos-react): scaffold pinoy business pos client`

## Created project

`src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.React/`

Private package name: `@exits/pinoy-business-pos-client` (ESM).

This host is the future Mobile Client candidate (browser, later PWA, later Capacitor). It is not a checkout-only forever host, and it is not a MAUI replacement in this package.

## Stack

| Item | Choice |
|---|---|
| UI | React 19.1.x |
| Language | TypeScript strict |
| Bundler | Vite 7.1.x |
| CSS | Tailwind CSS 4 + ExItS semantic tokens (canonical green `#166534` / dark `#4ade80`) |
| Primitives | shadcn-style / Radix Slot, class-variance-authority, Lucide |
| Routing | React Router 7 (`/` and 404 only) |
| Server state | TanStack Query 5 provider (no queries) |
| Forms | React Hook Form + Zod installed; no business forms |
| Tests | Vitest + Testing Library; Playwright + axe |

Redux, Ant Design, Bootstrap, Angular, Vue, Svelte, C# in browser, EF Core, and Npgsql are absent.

## Ports

| Mode | Host | Port | strictPort |
|---|---|---|---|
| Dev | 127.0.0.1 | 5177 | yes |
| Preview | 127.0.0.1 | 4177 | yes |

Platform API 8091, POS API 8092, Platform Admin 8095, PLM 5176/4176 are not owned by this client. No live APIs and no fake backend were started.

## Routes

| Path | Screen |
|---|---|
| `/` | Neutral foundation shell |
| unmatched | Not found |

Not implemented: `/sign-in`, `/workspace`, `/sales/new`, `/catalog`, `/cart`, `/checkout`, `/personal`.

## Theme and locales

- Theme: System (default), Light, Dark. One global preference. Stored at `exits.pos-client.ui-preferences.v1`.
- Locales: `en` default, `fil-PH` secondary. Resource keys only.
- Persistent storage is UI preferences only. No token/session storage.

## Foundation copy

- Pinoy Business POS
- React client foundation
- PWA foundation will be added next

No fabricated revenue, sales, products, customers, store names, user names, or checkout data.

## Scope boundaries

| Surface | Status |
|---|---|
| POS React Client | Created |
| MAUI | Unchanged |
| Organization Web | Unchanged |
| POS API / Application / Domain / Infrastructure / LocalStore | Unchanged |
| Platform / PWEB / PLM | Unchanged |
| Database migrations | None |
| API calls | None |
| Authentication | Not implemented |
| PWA / service worker | Not in this package |
| Capacitor / Android | Absent |
| Financial offline | Absent |

## Tests

Local validation inside `ExItS.PinoyBusinessPOS.React` (initial `npm install` generated `package-lock.json`; later packages use `npm ci`):

| Command | Result |
|---|---|
| `npm run typecheck` | PASS |
| `npm run lint` | PASS (0 errors; 3 `react-refresh/only-export-components` warnings on provider/hook/test helpers) |
| `npm run format:check` | PASS |
| `npm run test` | PASS — 10 Vitest tests |
| `npm run build` | PASS |
| `npm run test:e2e` | PASS — 9 Playwright tests |

Vitest covers app render, router foundation, EN default, fil-PH switch, System/Light/Dark, TanStack Query provider without queries, no privileged/business content, and min-width-safe shell structure.

Playwright covers foundation load, 320/375/768/1440 overflow, theme switch, locale switch, 404, and axe serious/critical = none. Preview used `127.0.0.1:4177`. Live Platform/POS APIs were not started.

## Notes

Dependency versions follow proven React clients in this repository (PLM Client / Platform Admin Web) without copying PLM branding, routes, auth, or business components. There is no package dependency on PLM.
