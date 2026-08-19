# PWEB-IMPL-06 — First Visual Checkpoint

**Status:** AWAITING VISUAL REVIEW  
**Branch:** `feat/platform-admin-web-v2`  
**Predecessor:** PWEB-IMPL-05 (`24f8921e0b6c39700d6d735e752eff06b52a668d`)

Cursor **does not** mark visual quality APPROVED.

## 8095 integrated auth

**PASS**

A gitignored `deploy/docker/.env.local-validation` was copied from the local `ExItS-SaaS` worktree (not committed, secrets not printed). Local Validation was started from this worktree with `-Build -PublicHost localhost` so 04C CORS and the current React image apply to `http://localhost:8095`.

| Check | Result |
|---|---|
| `http://localhost:8090/admin/login` | HTTP 200 |
| `http://localhost:8091/health` | HTTP 200 `Healthy` |
| `http://localhost:8095/health` | HTTP 200 |
| `http://localhost:8095/config.js` | `platformApiBaseUrl:"http://localhost:8091"` |
| `http://localhost:8095/admin/login` | HTTP 200 |
| Seeded Platform Admin login (Olivia) | Redirects to `/admin` |
| Cookie | HttpOnly `.ExItS.Platform.Auth`; credentials included |
| Refresh `/admin` | Remains authenticated |
| Dashboard | Real API totals and audit rows (not mocks) |
| `/admin/organizations` | Under development |
| CORS | `OPTIONS` from `http://localhost:8095` → `Access-Control-Allow-Origin: http://localhost:8095`, credentials true |
| Token in URL / `localStorage` / `sessionStorage` | None |
| Development Test User on 8095 | Hidden (production nginx `MODE=production`) |

Launcher note: an existing Tailscale `PUBLIC_HOST` would publish API/CORS as that host. This checkpoint used `-PublicHost localhost` (launcher parameter, not a CORS/code change) so the required localhost URLs match the allowlist.

## Automated validation

| Command | Result |
|---|---|
| `npm run typecheck` | PASS |
| `npm run lint` | PASS |
| `npm run format:check` | PASS |
| `npm run test` | 117 PASS |
| `npm run build` | PASS |
| `npm run test:e2e` | 27 PASS |
| Real 8095 axe serious/critical | none |
| Horizontal overflow 1440 / 1280 / 1024 / 768 / 375 / 320 | none |

## Screenshot matrix

COMPLETE under `docs/Platform-Admin-Web/Reports/impl-06-visual-checkpoint/`:

| File | Surface |
|---|---|
| `01-login-1440x900-en-light.png` | Login |
| `02-login-1440x900-en-dark.png` | Login |
| `03-login-375x812-en-light.png` | Login |
| `04-login-375x812-fil-PH.png` | Login |
| `05-dashboard-1440x900-en-light.png` | Dashboard (authenticated) |
| `06-dashboard-1440x900-en-dark.png` | Dashboard |
| `07-dashboard-1280x800-en-light.png` | Dashboard |
| `08-dashboard-768x1024-en-light.png` | Dashboard |
| `09-dashboard-375x812-en-light.png` | Dashboard |
| `10-dashboard-375x812-fil-PH.png` | Dashboard |
| `11-dashboard-320x568-en-light.png` | Dashboard |

## Visual polish applied (not approved)

- Login: branded split panel on desktop; compact form column; theme/language chips in the header; no Test User on production 8095
- Shell: tighter sidebar/topbar, Ex mark, content `max-w-[86rem]`, quieter desktop topbar
- Dashboard: slightly tighter header/gap; real data unchanged

## Visual status

**AWAITING PRODUCT OWNER + CHATGPT REVIEW**

## Explicitly not claimed

- Visual quality APPROVED
- Gate complete
- Logout, CSRF, CORS/cookie policy code changes
- Backend / DB / Blazor Admin / POS / PLM / Docker topology / ports
