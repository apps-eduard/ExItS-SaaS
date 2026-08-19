# PLATFORM-WEB-DOC-06 — Core Administration Screens Report

**Status:** Complete  
**Branch:** `docs/platform-admin-web-v2`  
**Prerequisite:** DOC-01..05 complete; DOC-06 pending

---

## 1. Delivered capability

This package defines the **core Platform Administration screen specifications** for the future Platform Admin Web (ExItS Platform SaaS Control Center) as documentation-only artifacts.

It delivers:

- A screen specification template including the required fields for:
  - purpose
  - route concept
  - primary personas
  - authorization expectations
  - data displayed
  - primary/secondary actions
  - search/filter/sort
  - pagination policy
  - table/card behavior
  - loading/empty/zero-result/error/partial/forbidden states
  - destructive actions and confirmation behavior
  - audit implications
  - responsive behavior
  - accessibility considerations
  - required backend capabilities (as stable capability requirement IDs)
  - explicit non-goals

- Six core screen specifications:
  - Platform Overview / Dashboard
  - Organizations List
  - Organization Workspace / Detail
  - Branches Administration (within organization detail)
  - Platform Users / Identity Administration
  - Membership / Access Management (within organization detail)

- Stable capability requirement IDs for backend existence to be verified later:
  - `PWEB-CAP-ORG-LIST`, `PWEB-CAP-ORG-GET`, `PWEB-CAP-ORG-CREATE`
  - `PWEB-CAP-BRANCH-LIST`
  - `PWEB-CAP-IDENTITY-LIST`
  - `PWEB-CAP-MEMBERSHIP-LIST`
  - plus additional supporting IDs used by the screens.

---

## 2. Evidence alignment

The screen specs are aligned with:

- DOC-02 information architecture and organization drill-down tabs
- DOC-05 application shell and global interactions (breadcrumbs, URL-driven state, keyboard model, forbidden/not-found behavior)
- Platform/Product ownership boundaries from Product Foundation:
  - Platform owns control-plane configuration and audit
  - Screens avoid POS/PLM operational workflows

---

## 3. Exclusions

No implementation artifacts:
- No frontend/backend code
- No mocks of application runtime
- No screenshots or image assets
- No CSS changes
- No Admin edits and no PLM changes

---

## 4. Files delivered

- `docs/Platform-Admin-Web/Screens/core-administration-screens.md`
- `docs/Platform-Admin-Web/Reports/PLATFORM-WEB-DOC-06-core-administration-screens.md`

