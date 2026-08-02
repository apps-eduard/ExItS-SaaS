# P16-WP03 — Organization Context and Navigation

| Field | Value |
|---|---|
| Status | **Complete** |
| Starting commit | `914746f` (after P16-WP02 tip-hash) |
| Feature commit | *(recorded after commit)* |
| Date | 2026-08-02 |

## Scope completed

- Server-side last-active organization preference (`organization_context_preferences`)
- Preference restore on Organization login / session resolve when multiple memberships
- Organization switcher limited to Organization-shell (Platform Admin never listed)
- Cache clear on switch (`PlatformPermissionState.RefreshAsync` + `AdminShellContext.RefreshAsync` + navigate)
- Deep-link UI guard (`OrganizationDeepLinkGuard`) on Organization Members
- Organization listing/select require Organization account session
- Cross-organization denial + last-active integration tests

## Schema

- `platform.organization_context_preferences` (user_identity_id PK, last_active_organization_id)

## Tests

- Platform unit: **316 passed**
- Platform integration: **154 passed** (includes last-active restore + cross-org deny in `ApiOrganizationContextTests`)
- Migration: `AddOrganizationContextPreferences`

## Residual

- Org APIs still under `/api/v1/platform/organizations` (full remap remains progressive)
- Personal Utang / account foundation land in WP04–WP05 (separate migrations)

## Phase 14

Unchanged. App remains **not production-ready**.
