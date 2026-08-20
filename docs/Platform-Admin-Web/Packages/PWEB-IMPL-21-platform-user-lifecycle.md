# PWEB-IMPL-21 — Platform User Lifecycle + Session Control

**Package ID:** PWEB-IMPL-21  
**Title:** Platform User Lifecycle + Session Control  
**Starting dependency:** PWEB-IMPL-20 PASS (+ CSRF compat gate)  
**Contract classification:** **PROVEN_PARTIAL**  
**Implementation:** NOT STARTED (planning only)

## 1. Objective

Convert the read-only Platform User detail surface (PWEB-17) into the first controlled **business mutation** surface after PWEB-20: account lifecycle transitions that currently exist on the Platform API, plus honest session-control behavior limited to proven contracts.

## 2. Current repository evidence

- React: `/admin/users/:userId` read-only (`UserDetailPage`); no mutation controls  
- API: `IdentityEndpoints` lifecycle POSTs; `CredentialEndpoints` unlock  
- Session invalidation as **side-effect** of suspend/deactivate/admin password set via `CredentialSessionInvalidation.RevokeAllAsync`  
- Last Platform Administrator guard: `PlatformAdministratorLifecycleGuard`  
- Self-action protection: **not found** in Application/API  
- Dedicated admin session list / revoke-by-id: **not found** (capability matrix `PWEB-CAP-AUTH-SESSION-*` gaps)

## 3. Existing APIs / contracts found

| Operation | Method + route | Classification |
|---|---|---|
| Suspend | `POST /api/v1/platform/users/{userId}/suspend` | PROVEN_EXISTING |
| Reactivate | `POST /api/v1/platform/users/{userId}/reactivate` | PROVEN_EXISTING |
| Deactivate | `POST /api/v1/platform/users/{userId}/deactivate` | PROVEN_EXISTING |
| Disable (alias) | `POST /api/v1/platform/users/{userId}/disable` | PROVEN_EXISTING (same as deactivate) |
| Move to suspended | `POST /api/v1/platform/users/{userId}/move-to-suspended` | PROVEN_EXISTING |
| Credential status | `GET /api/v1/platform/users/{userId}/credentials` | PROVEN_EXISTING |
| Unlock lockout | `POST /api/v1/platform/users/{userId}/credentials/unlock` | PROVEN_EXISTING |
| Admin “activate” distinct route | — | **MISSING** (use reactivate; personal `activate-account` is not Admin lifecycle) |
| Admin force-lock | — | **MISSING** |
| List user sessions | — | **MISSING** |
| Revoke session by id | — | **MISSING** |
| Explicit logout-all endpoint | — | **MISSING** (logout-all only as lifecycle/password side-effect) |

**Bodies (proven):** `LifecycleReasonRequest` / `ReactivateUserRequest` fields include `Reason`, `Global`, `ActorPassword`, `MfaCode` as applicable.

**Statuses (`AccountStatus`):** `Active`, `Suspended`, `Deactivated`, `PendingVerification`.

## 4. DTO / lifecycle semantics

- Response: `PlatformUserDto` (identity, status, suspension fields, account classes, orgs)  
- Invalid transitions: `platform.user.status.invalid_transition` → 409  
- Step-up for high-risk paths: `application.auth.step_up_required` / MFA variant → 409  
- Last admin: `application.role_assignment.last_platform_administrator` → 409  

**SEMANTICS UNRESOLVED:** whether React must block self-lifecycle actions client-side when server does not yet enforce self-protection.

## 5. Authorization

- Permission: `platform.permission.manage_platform_users` (`ManagePlatformUsers`)  
- UI shaping + route gate + API `EnsureAsync` + domain guards + audit  
- 401/403 fail-closed; no privileged flash

## 6. UI / route scope

- Primary: `/admin/users/:userId` (extend PWEB-17)  
- Confirmation dialogs for suspend / deactivate / move-to-suspended / unlock  
- Display only server-returned fields; unknown status → safe raw fallback  
- Responsive: 1440 / 375 / 320; EN + fil-PH; Light/Dark; density compatible

## 7. Mutation behavior

- Use centralized PWEB-20 mutation HTTP (`X-XSRF-TOKEN` + cookies)  
- Busy/disabled controls while in-flight; no double-submit  
- After success: refresh user detail (+ credentials status if shown)  
- Session effects: document that suspend/deactivate revoke sessions server-side; do **not** invent a session-management UI until list/revoke APIs exist

## 8. Audit requirements

- Rely on existing server audit for lifecycle success/denial  
- UI must not claim audit write of its own

## 9. Security / CSRF

- All POSTs through antiforgery-aware client  
- No token in URL / localStorage / sessionStorage / IndexedDB / SW cache / logs

## 10. Error states

401, 403, 404 (`application.user.not_found`), 400 domain/validation, 409 transition / last-admin / step-up

## 11. Concurrency / idempotency

- Follow server responses; no client invent of optimistic status  
- If server adds `ExpectedVersion` later, adopt it — currently reason/step-up based

## 12. A11y / i18n / responsive

Per Platform Admin Web design system; axe coverage on mutation dialogs

## 13. Explicit exclusions

- Create Platform staff user (out of scope unless separately authorized)  
- Dedicated session list/revoke UI (**BACKEND CONTRACT REQUIRED BEFORE IMPLEMENTATION**)  
- Admin lock endpoint  
- Invented “activate” button distinct from reactivate  
- POS/PLM operational auth  
- Self-protection rules as if server already enforces them (document gap)

## 14–17. Change allowances

| Area | Allowance |
|---|---|
| Backend | Only if a proven gap must be closed for safe Admin UX (e.g. self-guard); prefer UI-only if contracts suffice |
| DB/migrations | NONE unless separately authorized for a proven gap |
| POS | UNCHANGED |
| PLM | UNCHANGED |
| Blazor | UNCHANGED (remain compatible via session header) |

## 18. Tests required

Lifecycle success/deny; last-admin 409; 401/403/404; step-up surfacing; CSRF header on mutations; no session UI without API; axe; refresh after mutation

## 19. Evidence / report path

`docs/Platform-Admin-Web/Reports/PWEB-IMPL-21-platform-user-lifecycle.md` (+ screenshots folder when implemented)

## 20. Proposed commit message

`feat(platform-web): add platform user lifecycle controls`

## 21. Stop conditions

- `PWEB21_USER_MUTATION_CONTRACT_MISSING`  
- Ambiguous lifecycle enum/DTO  
- Attempt to invent session APIs  
- CSRF regression  

## 22. Definition of PASS

Authorized lifecycle mutations for **proven** endpoints only; CSRF correct; last-admin behavior preserved; no Create Org/Product; no fake session manager; tests green; report recorded.
