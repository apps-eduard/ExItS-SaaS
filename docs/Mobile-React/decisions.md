# Mobile React / PWA / Capacitor — Decisions

Accepted decision identifiers for this planning track (documentation-only).
These are planning decisions. They do not change current implementation.

| ID | Decision | Status |
|---|---|---|
| MOBILE-D-001 | This track is documentation-only. Completing these documents does not authorize React, PWA, Capacitor, or MAUI retirement work. | Accepted |
| MOBILE-D-002 | Current `src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Maui` remains the active Mobile Client until an explicit cutover is separately authorized. | Accepted |
| MOBILE-D-003 | Current Organization Web (`ExItS.PinoyBusinessPOS.Web`) and Personal Web (`ExItS.Personal.Web`) remain unchanged by this track. | Accepted |
| MOBILE-D-004 | Current .NET backends remain the system of record path: Platform API, PinoyBusinessPOS API, PostgreSQL product databases. No backend rewrite is implied. | Accepted |
| MOBILE-D-005 | The current MAUI project is a **client host**, not the POS product domain. Evidence shows it hosts Personal Mobile, Organization Owner Mobile, and POS Operations in one BlazorWebView. | Accepted |
| MOBILE-D-006 | Canonical planning terms are defined in [current-state-and-replacement-boundaries.md](current-state-and-replacement-boundaries.md). Do not use “mobile” as a synonym for POS only. | Accepted |
| MOBILE-D-007 | Distinguish **CLIENT HOST** from **PRODUCT DOMAIN**. A host may present multiple experiences; POS operational data stays in PinoyBusinessPOS; identity/org/commercial data stays in Platform. | Accepted |
| MOBILE-D-008 | The POS requirement “Native CSS / Razor components (no Ant Design, no Tailwind)” is a **CURRENT_IMPLEMENTATION_REQUIREMENT** for the MAUI/Razor client. This track does not delete, rewrite, or silently reinterpret that requirement. | Accepted |
| MOBILE-D-009 | The future client stack is recorded as a **PROPOSED_REPLACEMENT_CLIENT_ARCHITECTURE** only: React + TypeScript; Web/PWA where appropriate; Capacitor; Android first; iOS later. | Accepted |
| MOBILE-D-010 | Library, design-token, and Capacitor plugin choices are deferred to later DOC packages. DOC-01 does not authorize Tailwind, Ant Design React, a second design system, or a production service worker. | Accepted |
| MOBILE-D-011 | Current DesignSystem / native CSS rules continue to govern MAUI until cutover. Specs that forbid React/Tailwind on the current MAUI host remain valid for that host. | Accepted |
| MOBILE-D-012 | Offline/sync currently lives in `ExItS.PinoyBusinessPOS.LocalStore` (SQLite + encrypted outbox) consumed by MAUI. A future React client must not invent a second operational database or bypass product/Platform APIs. Replacement of LocalStore is not authorized here. | Accepted |
| MOBILE-D-013 | Authentication remains Platform-authoritative (password + Bearer introspect for MAUI; browser session for web hosts). A future React client must consume existing auth contracts; it must not create a separate identity. | Accepted |
| MOBILE-D-014 | Platform Administration remains Web-only. This mobile planning track must not add Platform Admin screens to Mobile Client. | Accepted |

Later DOC packages may add IDs. They must not weaken MOBILE-D-001 through MOBILE-D-014 without Product Owner review.
