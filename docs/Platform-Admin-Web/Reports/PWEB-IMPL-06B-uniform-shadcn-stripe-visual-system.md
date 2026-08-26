# PWEB-IMPL-06B — Uniform shadcn / Stripe visual system

**Status:** VISUAL DIRECTION ACCEPTED 
**Branch:** `feat/platform-admin-web-v2`  
**Starting HEAD:** `5f22978673f46942d154516a41578b35c49c296e`

## PLATFORM ADMIN VISUAL STANDARD

This is the permanent Platform Admin Web visual standard unless the Product Owner explicitly changes it.

| Layer | Source | Role |
|---|---|---|
| Structure | shadcn/ui dashboard, sidebar, header, cards, table, dropdown, auth, settings, mobile nav **patterns** | Composition only. Adapted to existing ExItS components. Not a dump of generated shadcn source. |
| Density / hierarchy | Stripe Dashboard | Operational control-plane density, compact metrics, quiet surfaces, table-first lists |
| Shell / responsive | Vercel Dashboard | Sidebar + top bar + drawer behavior |
| Polish | Linear | Typography, dark-mode layering, restrained controls |
| Brand | ExItS Platform Admin tokens | Authoritative color, type, radius, motion. **Do not copy Stripe blue or third-party assets.** |

Do not invent an unrelated visual language per feature. Future packages reuse this standard.

## What this package changed

Visual system and existing screen refinement only:

- Shared dashboard shell: ~248px sidebar, compact grouped nav, inset active state (not a giant green pill)
- Operational top bar (~48px): breadcrumbs left; environment marker when applicable; Preferences menu; account menu
- Shared `PageHeader` (title + muted subtitle + optional actions; no card wrapper)
- Login: restrained split brand/auth layout; compact form (~420px); Local Validation separated
- Overview: Stripe-style metric row, compact attention/audit tables, quiet health
- Shared `AdminTable`, restrained badges for **status only**, compact buttons/inputs
- Dark mode: layered surfaces, muted secondary text, ExItS green retained without neon treatment

## Preserved (PWEB-IMPL-06A)

- 8095 Test User via runtime `localValidationToolsEnabled` **or** Vite development/test
- Production selector hidden
- Olivia selection fills email/username only
- Password remains empty
- No auto-submit
- Login remains `POST /api/v1/platform/auth/login`
- HttpOnly cookie session unchanged

## Explicitly not changed

Backend APIs, DB/migrations, Blazor Admin, POS, PLM, CORS, cookies, CSRF, new Dashboard APIs, Organizations/Subscriptions/Account mutations, logout wiring.

## Validation

| Check | Result |
|---|---|
| `npm run typecheck` / `lint` / `format:check` | PASS |
| `npm run test` | 142 PASS |
| `npm run build` | PASS |
| `npm run test:e2e` | 35 PASS |
| `npm run test:e2e:container` (8095) | 3 PASS |
| 8090 / 8091 / 8095 | PASS |
| `/config.js` runtime + Local Validation flag | PASS |
| Olivia email fill / password empty / no auto-submit | PASS |
| Dashboard real API data | PASS |


## Visual approval

**AWAITING PRODUCT OWNER + CHATGPT**

Queue: **STOPPED AFTER PWEB-IMPL-06B**. No next package.
