# P4-WP01 — Portfolio Navigation and Product Views

## 1. Status

**Complete.** Permanent Cursor workflow rules added; read-only Platform Admin Blazor shell delivered; focused Admin read APIs added; validation passed; Portfolio independence preserved (Platform + authorized products only).

| Field | Value |
|---|---|
| Phase | Phase 4 — Platform Admin Expansion |
| Work package | P4-WP01 — Portfolio Navigation and Product Views |
| Branch | `main` |
| Date | 2026-07-29 |
| Phase marker | `P4-WP01-portfolio-navigation-product-views` |

## 2. Delivered capability

- Permanent Cursor rules: `.cursor/rules/exits-workflow.mdc` (`alwaysApply: true`)
- `ExItS.Platform.Admin` — ASP.NET Core Blazor Web App (Interactive Server), native CSS, no Ant Design, no Tailwind
- Portfolio navigation shell with development-stage security banner
- Read-only views: Dashboard, Products (+ overview), Organizations (+ commercial summary), Subscriptions, Manual SaaS Payments, Entitlements (latest/history/detail)
- Typed `IPlatformApiClient` / `PlatformApiClient` (timeouts, ProblemDetails, cancellation, unavailable/empty/error states)
- Development-only operator footer label (“Dev Operator — not authorization”)
- Focused read-only Admin APIs under `/api/v1/platform/admin/...`
- Organization list `GET /api/v1/platform/organizations`
- Admin unit + architecture + integration API tests

## 3. Explicit exclusions

- Authentication / JWT / MFA / identity or membership persistence
- Entitlement delivery to POS
- PinoyBusinessPOS
- Invoices, payment gateways, webhooks, QR, card processing
- Catalog / subscription / payment / override mutation Admin workflows
- P4-WP02 (Organizations, Users and Product Access)
- Fake production authentication

## 4. Persistence / migrations

No new migrations. Uses existing Phase 3 `platform` schema. Local runtime validation applied existing migrations to isolated Docker Postgres on port **5434** (dev-only credentials). No production migrate-at-startup.

## 5. API / UI capability

### Admin UI (http://localhost:5289)

| Route | Purpose |
|---|---|
| `/admin` | Portfolio dashboard |
| `/admin/products`, `/admin/products/{id}` | Product list / overview |
| `/admin/organizations`, `/admin/organizations/{id}` | Org list / commercial summary |
| `/admin/subscriptions`, `/admin/subscriptions/{id}` | Filtered list / detail |
| `/admin/payments`, `/admin/payments/{id}` | Manual SaaS payments (+ verification warning) |
| `/admin/entitlements`, `/admin/entitlements/{id}`, `/admin/entitlements/history/{org}/{product}` | Latest / detail / history (+ delivery warning) |

### New / extended read APIs (http://localhost:5288)

| Method | Path |
|---|---|
| GET | `/api/v1/platform/admin/portfolio-summary` |
| GET | `/api/v1/platform/admin/products/{productCode}/overview` |
| GET | `/api/v1/platform/admin/organizations/{id}/commercial-summary` |
| GET | `/api/v1/platform/admin/entitlements/latest` |
| GET | `/api/v1/platform/organizations` (paginated list) |

UI consumes existing catalog/subscription/payment/entitlement read endpoints as well. All remain development-stage and unauthenticated.

## 6. Build / test / runtime evidence

| Check | Result |
|---|---|
| `dotnet restore ExItS.slnx` | OK |
| `dotnet build ExItS.slnx -c Release` | 0 warnings, 0 errors |
| `dotnet test ExItS.slnx -c Release` | **317** passed / 0 failed / 0 skipped |
| Unit | 200 |
| Architecture | 39 |
| Admin unit | 10 |
| Integration | 68 |
| Runtime | API phase `P4-WP01-portfolio-navigation-product-views`; portfolio-summary 200; Admin `/admin*` 200; empty states; security banner; nav labels |

Baseline maintained above prior 302 tests.

## 7. Security limitations

- Platform Admin and Platform APIs are **unauthenticated** (production blocker; R-045/R-050/R-055/R-062 remain open; new R-063/R-064).
- Dev operator context is a display label only — not authorization.
- Manual payment views warn that confirmation ≠ provider verification.
- Entitlement views warn that snapshot ≠ product delivery.
- No card/CVV/PIN/OTP/gateway secrets collected or displayed.

## 8. portfolio independence verification evidence

- No unauthorized nested product tree is tracked
- `ExItS.slnx` contains only approved ExItS Platform/Admin/test projects (no POS)

## 9. Risks / open decisions

See `docs/risks-and-issues.md`: R-063 (unauthenticated Admin UI), R-064 (dev operator misuse), R-065 (UI/API contract drift), R-066 (partial dashboard), plus prior auth/delivery/R-022/R-035 risks remain open.

## 10. Files / docs changed

- `.cursor/rules/exits-workflow.mdc`
- `src/Platform/ExItS.Platform.Admin/**`
- `src/Platform/ExItS.Platform.Application/Admin/**`
- `src/Platform/ExItS.Platform.Infrastructure/.../AdminPortfolioReadStore.cs`
- `src/Platform/ExItS.Platform.Api/Admin/AdminEndpoints.cs`, `Program.cs`, org/payment list extensions
- `tests/ExItS.Platform.Admin.UnitTests/**`, integration + architecture tests
- Docs: portfolio progress, Phase 4 page, README, FILE-MANIFEST, release plan, security, risks, reports index, this report

## 11. Git / push evidence

| Commit | Message |
|---|---|
| `4399961` | `chore(repo): add permanent Cursor workflow rules` |
| `aa340e1` | `feat(admin): add portfolio navigation and product views` |
| _(hash-record)_ | `docs(admin): record P4-WP01 commit hashes` |

Local and remote `main` must match after push; working tree clean.

## 12. Exact next work package

**P4-WP02 — Organizations, Users and Product Access**

Do not begin P4-WP02 until explicitly authorized.
