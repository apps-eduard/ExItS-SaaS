# PLATFORM-WEB-DOC-02 — Product and Information Architecture Report

**Status:** Complete  
**Branch:** `docs/platform-admin-web-v2`  
**Prerequisite:** DOC-01 (current-state audit and replacement boundaries)

---

## 1. Delivered Capability

This package defines the future ExItS Platform Admin Web application as the **ExItS Platform SaaS Control Center** and documents:

- Product vision with measurable goals (clear, fast, data-dense, discoverable, keyboard-friendly, accessible, responsive, safe, consistent, excellent loading/error/empty states)
- Four UX personas based on actual Platform responsibilities (Platform Administrator, Operations/Support Operator, Commercial/Billing Operator, Security/Governance Operator)
- Proposed primary navigation structure derived from the existing Admin navigation (`AdminNav.razor`)
- Organization drill-down architecture (Overview, Branches, People/Memberships, Products/Access, Subscription, Entitlements, Billing, Activity/Audit)
- Explicit exclusions: POS checkout, inventory, cash operations, loan processing, collections, and all product-domain operational workflows
- Navigation principles: entity-first navigation, breadcrumbs, global navigation, global search, permission-aware navigation, direct-link/bookmark support, back-navigation, route-not-found behavior, and the principle that navigation visibility is not authorization

## 2. Personas and Authorization

UX personas are design artifacts describing usage patterns. They are **not** new authorization roles.

Existing Platform roles referenced:
- Platform Administrator (full permission set)
- Platform Support (view-oriented, limited management)

Optional future roles noted in the authorization matrix (not yet defined): Billing Administrator, Platform Auditor, Platform Operations.

Each persona documents its authorization dependency by referencing existing permission codes from the authorization matrix. No new permissions were invented.

## 3. Platform / Product Boundaries

All content in this package is scoped to Platform responsibilities as defined in the Product Foundation reference:

- Platform owns: identity, organizations, catalog, plans, subscriptions, entitlements, SaaS billing, Platform Admin, Platform audit, privacy compliance
- Products own: operational domain models, workflows, product-local roles, product API/UI, product databases, operational financial records

The information architecture explicitly excludes product-operational workflows from the SaaS Control Center navigation.

## 4. Files Changed

| File | Action |
|---|---|
| `docs/Platform-Admin-Web/product-vision-personas-information-architecture.md` | Created |
| `docs/Platform-Admin-Web/Reports/PLATFORM-WEB-DOC-02-product-and-information-architecture.md` | Created |
| `docs/Platform-Admin-Web/README.md` | Updated (index entry) |
| `docs/Platform-Admin-Web/documentation-status.md` | Updated (DOC-02 marked Complete) |
| `docs/Platform-Admin-Web/decisions.md` | Updated (new decisions) |

## 5. Code Changed

No. Documentation only.

## 6. Exclusions

- No implementation artifacts (no React, no packages, no code)
- No existing Admin edits
- No backend edits
- No PLM edits
- No .cursor/rules changes
- Frontend library decisions deferred to DOC-03
