# PWEB-IMPL-15 — Organization Activity / Audit

**Status:** COMPLETE after validation  
**Branch:** `feat/platform-admin-web-v2`  
**Starting HEAD:** `6ee6494d00eeda795e5338d7806a2dad3d0d817f`  
**Commit:** `feat(platform-web): add organization activity audit`

## Problem / objective

Add the read-only **Activity / Audit** tab to the existing organization workspace using the real org-scoped audit contract.

## Delivered

- Route: `/admin/organizations/:organizationId/activity`
- Workspace tab: **Activity / Audit** (EN) / **Aktibidad / Audit** (fil-PH)
- Client: `GET /api/v1/platform/organizations/{organizationId}/audit`
- Supported URL/shareable filters only:
  - `fromUtc`, `toUtc`, `actor`, `action`, `targetType`, `outcome`, `branchId`, `page` (`pageSize` fixed at 20)
- Display: timestamp, actor, action, target/type (+ branch context when `OrganizationBranch`), outcome, summary/reason
- Reuses dashboard audit presentation helpers (`presentAuditAction` / `presentAuditType` / `presentAuditActor`) with accessible raw values via `title`
- Desktop table + mobile cards; 320 / 375 / 1440 coverage
- 401/403 fail-closed (no audit disclosure)
- GET-only; no export; no mutations

## Explicitly unchanged

- Platform backend / Application / Domain / Infrastructure
- DB / migrations
- Blazor Admin
- POS / PLM / Personal
- Global `/admin/audit` (still under-development)
- PWA / Capacitor
- CSRF blocker / social-auth cutover blocker

## Evidence

Screenshots: `docs/Platform-Admin-Web/Reports/impl-15-organization-activity-audit/`

## Visual approval

**AWAITING PRODUCT OWNER + CHATGPT**
