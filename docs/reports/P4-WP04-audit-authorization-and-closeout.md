# P4-WP04 — Audit, Authorization and Closeout

## 1. Status and Phase 4 closeout decision

**Complete.** Phase 4 Platform Admin Expansion is **closed with documented risks**.

| Field | Value |
|---|---|
| Phase | Phase 4 — Platform Admin Expansion |
| Work package | P4-WP04 — Audit, Authorization and Closeout |
| Branch | `main` |
| Date | 2026-07-30 |
| Phase marker | `P4-WP04-audit-authorization-closeout` |
| Phase 4 decision | **Complete with documented risks** — not production-ready |

## 2. Authorization model and enforcement

### Layers (fail closed)

```text
Platform-wide system role
→ organization membership role
→ product-access assignment
→ product-local authorization (never granted by Platform Admin)
```

### Platform system roles

| Role | Permissions |
|---|---|
| PlatformAdministrator | All Platform permissions |
| BillingAdministrator | ViewPortfolio, ManageOrganizations, ManageSubscriptions, ManageManualPayments, ViewAuditRecords |
| PlatformSupport | ViewPortfolio, ManageMemberships, ManageProductAccess, ViewAuditRecords |

Assignments may be platform-wide (`OrganizationId` null) or organization-scoped. Cross-organization access fails closed.

### Development identity (not production authentication)

- APIs remain without JWT/passwords/MFA/SSO/AD.
- `DevelopmentOperator` receives full Platform permissions only when `DevelopmentAuthorizationOptions.GrantDevelopmentOperatorFullAccess` is true (Development/Testing hosts only).
- Optional header `X-Dev-Platform-User-Id` selects a Platform User principal whose permissions come strictly from role assignments (for denial and scope tests).
- UI visibility is convenience only; server-side `PlatformAuthz.EnsureAsync` is authoritative → stable `403` ProblemDetails + denied audit.

## 3. Audit model and coverage

Append-only `platform.audit_records` with: audit id, UTC timestamp, actor identifier/type, action code, target type/id, organization, product code, correlation id, outcome, reason, safe summary.

Covered mutations include users, memberships, product access, subscription lifecycle, manual payments, feature overrides, role assign/revoke, and authorization denials for sensitive mutations.

Forbidden in audit: passwords, tokens, card data, GCash credentials, PHI, raw payloads, arbitrary exception text.

Admin: `/admin/audit`, `/admin/audit/{auditId}` with filters.

## 4. UI redesign and responsive behavior

Polished commercial Admin shell: collapsible sidebar (checkbox CSS), mobile drawer, sticky header, compact environment chip, shared design-system components, responsive tables/cards. Viewports targeted: 320–1920px. Keyboard-usable nav/controls; `prefers-reduced-motion` respected.

## 5. Theme result

System / Light / Dark via semantic CSS tokens (`--color-*`, `--shadow-*`, `--radius-*`, `--motion-*`). Selector in header; `localStorage` persistence; `theme-boot.js` prevents flash; no full reload on theme change.

## 6. Localization result

English (`en`) default and Tagalog (`fil-PH`) via ASP.NET Core localization + `AdminResources` resx. Language selector persists via cookie + localStorage. Terminology guide: `docs/engineering/admin-terminology-guide.md`.

## 7. Persistence, API, Admin

### Migration

`AddPlatformAuthorizationAndAudit` — tables `platform.platform_role_assignments`, `platform.audit_records`. Validated apply → rollback to `AddPlatformUsersMembershipsAndProductAccess` → re-apply. No legacy product/POS/gateway/invoice/product-local tables.

### API

| Area | Routes |
|---|---|
| Authorization | `/api/v1/platform/authorization/me`, `/roles`, `/assignments` (+ revoke) |
| Audit | `/api/v1/platform/audit`, `/api/v1/platform/audit/{id}` |
| Mutations | Existing sensitive endpoints enforce permissions + write audit |

### Admin

Redesigned shell; permission-aware nav; audit pages; themes; EN/fil-PH; preserved P4-WP01–03 workflows.

## 8. Tests, runtime, risks, docs, Git

| Suite | Passed |
|---|---:|
| Unit | 261 |
| Architecture | 39 |
| Admin unit | 27 |
| Integration | 84 |
| **Total** | **411** |

Baseline 336 not reduced.

Runtime: API phase marker `P4-WP04-audit-authorization-closeout`; `/authorization/me` DevelopmentOperator with 9 permissions; Admin `/admin` and `/admin/audit` HTTP 200.

### Explicit exclusions / open blockers

- Production authentication (JWT/MFA/SSO/AD)
- Payment gateways, invoices, entitlement delivery
- Product-local roles; legacy product/POS authorization
- Phase 5 POS MAUI

### Portfolio independence

Root a nested foreign product tree must remain absent/untracked and outside `ExItS.slnx`.

### Exact next work package

**Phase 5 — PinoyBusinessPOS MAUI Foundation → P5-WP01 — MAUI Solution and API Client**

Do not begin until explicitly authorized.

### Commits

| Kind | Message / hash |
|---|---|
| Feature | `feat(admin): audit authorization themes and closeout` — `74ed46ddca283f552f6269650e348634ddf3f0d6` |
| Docs | `docs(admin): record P4-WP04 commit hashes` — `3e43806a8d405ec559ccf71eea0b44941822738a` |
