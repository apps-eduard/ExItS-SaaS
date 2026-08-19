# PLATFORM-WEB-DOC-08 Report — Governance, Operations + Settings Screen Specifications

**Date:** 2026-08-19  
**Branch:** `docs/platform-admin-web-v2`  
**Status:** Complete  
**Type:** Documentation only — no implementation

---

## Delivered

1. **Screens/governance-operations-settings-screens.md** — screen specifications for:
   - A) Audit Explorer
   - B) Identity / Authentication Administration
   - C) Access / Governance (roles and permissions)
   - D) Platform Operations (health, events, jobs — future/evidence-gated)
   - E) Platform Settings (global, organization-scoped, product-local reference)

2. **Capability requirement IDs** — 21 stable `PWEB-CAP-*` identifiers covering audit, authentication administration, governance roles, operations health, and settings. Backend availability is not claimed; DOC-09 will verify.

3. **Security UX principles** — §0 codifies nine security UX rules including confirmation requirements, step-up auth hooks, minimum-disclosure forbidden states, no secret display, last-admin protection, and no impersonation invention.

4. **Evidence alignment** — all screens are grounded in existing repository evidence:
   - Audit: `platform.audit_records` infrastructure (P4-WP04, WP15E).
   - Auth: credentials (P13-WP02), browser sessions (P13-WP03), access tokens (P13-WP06), MFA readiness (P13-WP07), external login (P13-WP08), recovery email (P13-WP09).
   - Governance: `PlatformAuthz` role assignments, Platform permission matrix from authorization-matrix.md.
   - Operations: health endpoints and async events documented in Product Foundation; exact backend availability deferred to DOC-09.
   - Settings: trial/grace/branding configuration evidence; product-local settings explicitly excluded.

---

## Alignment

| Source | Alignment |
|---|---|
| Authorization matrix v2.0 | Platform permission matrix respected; no invented permissions |
| Authentication architecture | Session/token/credential/MFA/external login model reflected accurately |
| P28-WP15E governance audit | `platform.audit_records`, action codes, org-scoped query |
| Product Foundation | Async events, financial boundary, no product operational leakage |
| DOC-06 core screens | Cross-references without duplication (user detail hosts auth admin tab) |
| DOC-07 commercial screens | No overlap; billing/entitlement remain in DOC-07 |

---

## Exclusions

- No implementation, no frontend/backend code, no migrations.
- No impersonation or support-login behavior invented.
- No POS/PLM operational monitoring or configuration.
- No fabricated health data or infrastructure monitoring.
- No secret/token/credential display.
- No duplicate authorization model.

---

## Files changed

- `docs/Platform-Admin-Web/Screens/governance-operations-settings-screens.md` — new
- `docs/Platform-Admin-Web/Reports/PLATFORM-WEB-DOC-08-governance-operations-settings.md` — new
- `docs/Platform-Admin-Web/README.md` — updated
- `docs/Platform-Admin-Web/documentation-status.md` — updated
- `docs/Platform-Admin-Web/decisions.md` — updated
