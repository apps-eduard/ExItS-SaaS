# PLATFORM-WEB-DOC-FINAL-AMEND-01 — UX, Authentication, Navigation, Visual-Quality, and Release-Policy Amendment

**Package:** PLATFORM-WEB-DOC-FINAL-AMEND-01  
**Branch:** `docs/platform-admin-web-v2`  
**Status:** Complete  
**Starting HEAD:** `e0efe0be43c9c0efd99fc3adc9414a3992ad9f8f`  
**API audit SHA:** `618a7b61711a2baee5a1589bd49bbd3312eb4eec` (origin/main unchanged since DOC-09)

---

## Delivered amendments

### 1. Canonical Navigation Registry

Created `navigation-registry.md` with:
- 33 primary sidebar `PWEB-NAV-*` entries across 10 sections (Home, Organizations, People & Access, Products & Commercial, Billing, Global Catalog, Governance, Operations, Settings, Development)
- 10 Privacy & Compliance workspace entries
- 10 Organization workspace entries
- Five lifecycle states: AVAILABLE, PLANNED_DISABLED, CONTEXT_REQUIRED, DEV_TEST_ONLY, UNAUTHORIZED
- Filipino localization keys for every entry
- Lucide icon concepts
- Permission and capability dependency mappings

### 2. Authentication Screen Specifications

Created `Screens/authentication-screens.md` with:
- Sign In (with information hierarchy, credential semantics, social auth)
- Create Account / Register (two-step: register + activate)
- Account Activation
- Forgot Password
- Reset Password
- Session Expired handling
- Development Test User (separated, subdued, collapsible, never in Production)
- Mobile-first auth requirement (phone-sized screens are first-class)
- 12 new `PWEB-CAP-AUTH-*` capability IDs mapped to verified API routes

### 3. Shell Responsibility Boundaries

Updated `application-shell-and-global-interactions.md` with:
- Explicit surface responsibilities (Sidebar / Top bar / Breadcrumb / Page header / Workspace navigation)
- Enhanced sidebar spec: icon-only collapsed mode, lifecycle state behaviors, keyboard interaction, EN/fil-PH resilience, light/dark behavior, no clipping
- Enhanced top bar: language selector, theme selector, environment indicator with Production vs non-Production distinction, notification constraint, responsive collapse

### 4. Localization / Theme / Density

Updated `design-system-and-ux-foundation.md` with:
- English (en) default, Filipino (fil-PH) secondary
- Localization rules (no hard-coded strings, locale-aware formatting)
- Three-level density: Comfortable / Balanced / Compact (Balanced is the approved React Admin default; does not yet exist in shared C# DesignSystem)
- User preferences: language, theme, density, sidebar, table display, regional/time-zone
- Responsive authentication requirement

### 5. WCAG Normalization

Normalized all documents to WCAG 2.2 AA design target. Removed stale WCAG 2.1 AA wording from product-vision document.

### 6. Dashboard Strengthening

Updated core-administration-screens.md Dashboard specification with:
- Evidence-backed widget table (backing capability, authorization, navigation target per widget)
- Absolute rule: no fake KPIs, no unbounded dataset loading for card calculations
- Independent widget loading
- Additional required capabilities listed

### 7. Visual Definition of Done

Added §7 to migration-testing-and-implementation-gates.md with comprehensive UI package verification checklist (32 items across structure, tokens, typography, viewports, themes, languages, density, states, keyboard, accessibility, and code quality).

### 8. Playwright Visual QA / Screenshots

Added §8 with representative screenshot matrix for Platform shell/Dashboard (4 variants) and Authentication (6 variants including phone/Filipino stress cases). Documented screenshot workflow and Product Owner approval requirements.

### 9. Visual Reference Screens

Added §9 designating Sign In, Dashboard, Organizations List, and Organization Workspace as the four initial visual reference surfaces.

### 10. First Visual Implementation Checkpoint

Added §10 documenting the mandatory PWEB Visual Foundation checkpoint (auth + shell + dashboard) with verification sequence and mandatory STOP after visual foundation for Product Owner review.

### 11. Frontend Release / Cache / Version Policy

Added §11 with entry HTML revalidation, content-hashed immutable assets, no service worker by default, deployment safety, version awareness, and API compatibility requirements.

### 12. API Capability Matrix Update

Updated `api-capability-matrix.md` with 12 new `PWEB-CAP-AUTH-*` capability IDs (all EXISTS), raising total from 63 to 75 capabilities.

### 13. Decisions

Added PWEB-D-035 through PWEB-D-043 (9 new decisions). Updated PWEB-D-016 to reflect Balanced density default.

---

## Cross-document consistency checks

| Check | Result |
|---|---|
| WCAG 2.1 references | None remaining (all normalized to 2.2 AA) |
| Compact as React Admin default | Corrected to Balanced (PWEB-D-016, PWEB-D-037) |
| Only Compact/Comfortable density | Corrected: Comfortable/Balanced/Compact documented |
| Mobile-phone forms not supported | Corrected: auth screens require mobile-first polishing |
| Old sidebar structure treated as final | Replaced with canonical navigation-registry.md |
| Theme/language omissions | Language (en/fil-PH) and theme (System/Light/Dark) documented in design-system and shell |
| Fake enabled search capability | Documented: global search only when capability-backed; Planned state otherwise |
| Implementation continues past visual checkpoint | Mandatory STOP documented |
| Cache clearing expected | Release/cache policy explicitly states manual cache clearing is NOT the normal procedure |
| New PWEB-CAP-* IDs unique | Verified: 12 new AUTH IDs, no duplicates with existing 63 |
| New PWEB-NAV-* IDs unique | Verified: 53 unique NAV IDs across all sections |

---

## Explicit exclusions

- No React implementation created
- No package.json or lock files created
- No backend, database, or migration changes
- No existing Admin, PLM, POS, or .cursor/rules modifications
- No Git merge, rebase, reset, amend, or force push operations
