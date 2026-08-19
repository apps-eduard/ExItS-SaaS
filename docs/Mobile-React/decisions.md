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
| MOBILE-D-010 | Library choices were deferred from DOC-01. DOC-03 freezes the replacement-host planning stack (React/TS/Vite, Tailwind, shadcn/ui, Lucide, TanStack Query/Table, RHF+Zod, Motion, Capacitor, PWA). Versions remain unpinned. This does not authorize Tailwind on current MAUI. | Accepted |
| MOBILE-D-011 | Current DesignSystem / native CSS rules continue to govern MAUI until cutover. Specs that forbid React/Tailwind on the current MAUI host remain valid for that host. | Accepted |
| MOBILE-D-012 | Offline/sync currently lives in `ExItS.PinoyBusinessPOS.LocalStore` (SQLite + encrypted outbox) consumed by MAUI. A future React client must not invent a second operational database or bypass product/Platform APIs. Replacement of LocalStore is not authorized here. | Accepted |
| MOBILE-D-013 | Authentication remains Platform-authoritative (password + Bearer introspect for MAUI; browser session for web hosts). A future React client must consume existing auth contracts; it must not create a separate identity. | Accepted |
| MOBILE-D-014 | Platform Administration remains Web-only. This mobile planning track must not add Platform Admin screens to Mobile Client. | Accepted |
| MOBILE-D-015 | Future client shares ExItS identity (brand, locales, theme, a11y) but must not copy Platform Admin presentation or information architecture. | Accepted |
| MOBILE-D-016 | Device-class UX: phone is primary for Personal and quick Owner tasks; tablet landscape is primary for cashier selling; desktop/PWA may use side nav/tables/keyboard but must not become a dense admin console. | Accepted |
| MOBILE-D-017 | UX principles for the replacement client: mobile-first, tablet-first selling, barcode-first, session-persistent cart, visible offline/sync, skeleton loading, immediate feedback, EN + fil-PH, Light/Dark/System. | Accepted |
| MOBILE-D-018 | Role/experience matrix preserves current access chain. UI presentation does not grant permission. Cashiers do not receive Organization Administration. Organization Owner without a POS role does not receive checkout. | Accepted |
| MOBILE-D-019 | Target selling workflow is workspace → scan/search → cart → customer by rule → payment → receipt → sync status. Cart is session-persistent, not a second ledger. | Accepted |
| MOBILE-D-020 | Payment UX in this track stays on current retail boundaries: cash, manual GCash (reference required, not gateway-verified), customer-credit/Utang when entitled. No new wallets, split tender, or live card collection. Simulated P19 Card/GCash is not treated as production gateway UX. | Accepted |
| MOBILE-D-021 | Visual quality target is WCAG 2.2 AA as a design bar (not a certification claim). No cramped controls, no shrunk-desktop-on-phone, no decorative heroes, reduced motion, safe areas, fil-PH wrapping, phone portrait + tablet landscape/portrait + desktop/browser. | Accepted |
| MOBILE-D-022 | Future React host is a single codebase delivered as Browser Web, PWA, and Capacitor (Android first, iOS later). Backend remains existing .NET Platform API + POS API + PostgreSQL. No frontend database access. | Accepted |
| MOBILE-D-023 | Recommended future project path is `src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Client/` (not created). Must not overwrite `.Maui` or `.Web`. Must not live under Platform or a new `src/Clients` tree. Folder ownership does not mean POS-only UX. | Accepted |
| MOBILE-D-024 | Reuse A: share ExItS tokens, theme, i18n, formatting, HTTP/error, and a11y conventions with Platform Admin React. Reuse B: never share Platform admin/billing/entitlement/governance/operator UI. Reuse C: Web/PWA/Capacitor share selling and product feature modules. | Accepted |
| MOBILE-D-025 | Device capabilities are conceptual adapter contracts (scanner, camera, storage, connectivity, share/export, printer, NFC, payment terminal) with Web/PWA vs Capacitor implementations. No TypeScript interfaces in this package. Unimplemented adapters degrade; they do not invent terminal/NFC products. | Accepted |
| MOBILE-D-026 | Code ownership: features orchestrate; typed HTTP clients; TanStack Query for server state; RHF+Zod for forms; dedicated offline coordination layer; adapters for devices; .NET APIs remain authoritative. Do not port Domain rules into JavaScript. No Redux unless a later package proves need. | Accepted |
| MOBILE-D-027 | One React client, three deliveries: browser, PWA, Capacitor. PWA and Capacitor are not feature-identical. Android Capacitor is first native target; iOS Capacitor is later. | Accepted |
| MOBILE-D-028 | STATIC APP CACHE (service worker / hashed assets) is separate from AUTHORITATIVE LOCAL OFFLINE DATA (LocalStore-equivalent outbox). Service worker must not cache-first API/financial data and is not the offline database. | Accepted |
| MOBILE-D-029 | PWA: installable manifest, standalone display, HTML revalidation, hashed immutable assets, no manual cache-clear as the normal update, stale-version prompt that does not destroy unsaved cart/checkout. Production PWA rollout remains unauthorized. | Accepted |
| MOBILE-D-030 | iPhone/iPad may use browser/PWA before native iOS. Do not promise parity for background, NFC, printers, payment terminals, or store install UX. | Accepted |
| MOBILE-D-031 | Capacitor is a thin native host. Plugins may supply camera, scanner, secure storage, share/file, and later printer/Bluetooth/NFC/vendor SDKs. POS business rules stay out of native plugin code. Store-packaged assets; no assumed OTA live update. | Accepted |
| MOBILE-D-032 | Desktop default is browser/PWA. Capacitor Windows native packaging is not claimed. True native Windows would be a separate evaluation. | Accepted |
| MOBILE-D-033 | Release channels are independent: web/PWA deployment, signed Android packages, later iOS TestFlight/App Store. Frontends must tolerate a supported API compatibility window. | Accepted |
| MOBILE-D-034 | Future React offline architecture mirrors current LocalStore: encrypted outbox, FIFO, idempotent POS API, access revalidation, OD-10 retention. Service-worker cache is not this layer. | Accepted |
| MOBILE-D-035 | Current implemented offline checkout is **cash only** (`sale.checkout` dispatcher rejects non-Cash). Manual GCash remains online in current MAUI. React must not enable GCash/Utang/card offline ahead of that evidence. Product duplicate-GCash checks are required when that path is authorized; uniqueness was not found in current POS schema. | Accepted |
| MOBILE-D-036 | Completed financial records are never silently rewritten. Conflicts retain work for review. Inventory management stays online-required; local catalog deduction is a projection only. | Accepted |
| MOBILE-D-037 | Browser/PWA uses a browser-safe session (no tokens in ordinary localStorage). Capacitor uses native secure storage for Bearer, matching current MAUI. On reconnect, server authority wins. Offline snapshots do not permanently override entitlements. | Accepted |
| MOBILE-D-038 | PWA and Capacitor may use different physical storage; they share logical outbox/repository contracts. IndexedDB/SQLite libraries are not pinned in planning docs. | Accepted |

Later DOC packages may add IDs. They must not weaken MOBILE-D-001 through MOBILE-D-038 without Product Owner review.
