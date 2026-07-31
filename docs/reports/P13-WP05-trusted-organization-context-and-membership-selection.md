# P13-WP05 — Trusted Organization Context and Membership Selection

Phase marker: `P13-WP05-trusted-organization-context-and-membership-selection`

Package: **P13-WP05 — Trusted API Actor and Organization Context** (user objective: Trusted Organization Context and Membership Selection)
Prior tip: `1148040833b8ec1e6d14d290cba3cd56da322254`
Feature tip: `e64f352161bb20447a99ae762d1a69ec1a3846fe`

## Status

**Complete.** After Platform login, trusted server-side organization context is established from **active** organization memberships (and active organizations). Supports none / one (auto-select) / many (selection required), select/switch/clear, and invalidation when membership or organization eligibility ends. **R-091 remains open** (bearer tokens, product-client wiring, MFA, closeout remain).

Exact next: **P13-WP07 — MFA Readiness and Auth Hardening** when authorized (do **not** begin).

## 1. Delivered capability

| Area | Evidence |
|---|---|
| Session org context | Nullable `selected_organization_id` on `platform_auth_sessions` (FK SetNull); domain `SelectOrganization` / `ClearSelectedOrganization` |
| Login resolution | 0 active → `None`; 1 active → auto-select `Selected`; many → `SelectionRequired` |
| Revalidation | `ValidateAndRenewPlatformSession` clears stale selected org; auto-selects when exactly one eligible remains |
| APIs | `GET /auth/me` includes org fields; `GET /auth/organizations`; `PUT /auth/organization-context` |
| Actor claim | Session claim `exits_organization_id`; `PlatformActorContext.OrganizationId` from session only (never client org header) |
| Invalidation | Membership suspend/revoke and organization suspend clear matching session org context |
| Admin UI | Header `OrganizationContextSwitcher` (none / chip / select+switch) |
| Audit | `platform.auth.organization_context_changed` |
| Migration | `AddSelectedOrganizationToAuthSessions` apply/rollback/reapply |

## 2. Locked access chain preserved

```text
Platform User → Organization Membership → Product Access → Product-Local Role
```

Organization context is membership-checked server state — not a substitute for Product Access or product-local roles.

## 3. Explicit exclusions

Product launch protection; bearer tokens; MFA; external IdP; broad PlatformAuthz redesign; P13-WP06+ product-client wiring.

## 4. Validation

| Check | Result |
|---|---|
| Full Release tests | **1227 passed / 0 failed / 0 skipped** |
| Org context API scenarios | `ApiOrganizationContextTests` |
| Migration apply/rollback/reapply | `SelectedOrganizationMigrationTests` |

## Exact next work package

**P13-WP07 — MFA Readiness and Auth Hardening** when explicitly authorized. Do not begin P13-WP07.
