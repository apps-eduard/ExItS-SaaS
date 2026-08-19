# Mobile React / PWA / Capacitor — Documentation Only

This documentation set establishes the planning baseline for a future ExItS React / TypeScript
mobile client delivered as Web/PWA where appropriate and wrapped with Capacitor
(Android first, iOS later).

It audits the current MAUI Blazor Hybrid host, related web clients, and .NET backends.
It does **not** authorize implementation.

## Status

- Planning documentation: **FINAL APPROVED** (DOC-00 … DOC-08; AMEND-01, AMEND-02, AMEND-03)
- React mobile implementation: **NOT AUTHORIZED**
- MAUI retirement: **NOT AUTHORIZED**
- PWA production rollout: **NOT AUTHORIZED**
- Capacitor production rollout: **NOT AUTHORIZED**
- Merge to `main`: **AWAITING PRODUCT OWNER AUTHORIZATION**
- MOBILE-D-060: **OPEN**
- Current MAUI, Organization Web, Personal Web, Platform APIs, and POS APIs: **unchanged**

## Worktree

- Branch: `docs/mobile-react-foundation`
- Worktree: `C:/Users/speed/Desktop/ExItS-SaaS-Mobile`
- Baseline `origin/main`: `5a9be9417b7a2217227ae93e9280102992861615`

## Contents

- [documentation-status.md](documentation-status.md) — queue state and DOC package table
- [decisions.md](decisions.md) — accepted decision identifiers for this planning track
- [current-state-and-replacement-boundaries.md](current-state-and-replacement-boundaries.md) — current clients, hosts, and replacement boundaries
- [product-surfaces-and-ux.md](product-surfaces-and-ux.md) — device classes, role matrix, selling UX, visual quality target
- [frontend-architecture-and-reuse.md](frontend-architecture-and-reuse.md) — React stack, reuse levels, adapters, future project path
- [pwa-and-capacitor-delivery.md](pwa-and-capacitor-delivery.md) — browser/PWA vs Capacitor channels, cache vs LocalStore, iOS interim
- [offline-sync-auth-and-security.md](offline-sync-auth-and-security.md) — outbox/idempotency, financial offline rules, auth, client security
- [device-and-payment-integration.md](device-and-payment-integration.md) — scanner/printer/drawer adapters, payment boundaries, capability matrix
- [migration-testing-and-implementation-gates.md](migration-testing-and-implementation-gates.md) — coexistence stages, parity, testing, visual checkpoint, gates A–K
- [Reports/MOBILE-REACT-DOC-08-final-closeout.md](Reports/MOBILE-REACT-DOC-08-final-closeout.md) — final consistency audit and closeout
- [Reports/MOBILE-REACT-DOC-AMEND-01-auth-connectivity-diagnostics.md](Reports/MOBILE-REACT-DOC-AMEND-01-auth-connectivity-diagnostics.md) — AMEND-01 PIN/lock/connectivity/diagnostics
- [Reports/MOBILE-REACT-DOC-AMEND-02-language-theme-defaults.md](Reports/MOBILE-REACT-DOC-AMEND-02-language-theme-defaults.md) — AMEND-02 `en` + System defaults
- [Reports/MOBILE-REACT-DOC-AMEND-03-smart-workspace-product-context.md](Reports/MOBILE-REACT-DOC-AMEND-03-smart-workspace-product-context.md) — AMEND-03 smart workspace + product launch context
- [Reports/MOBILE-REACT-DOC-APPROVAL-record.md](Reports/MOBILE-REACT-DOC-APPROVAL-record.md) — Product Owner documentation approval (merge still awaiting authorization)

## Canonical rule

Do not use **mobile** to mean only POS.

The current MAUI host contains Personal Mobile, Organization Owner Mobile, and POS Operations
in one BlazorWebView. POS business data remains inside the PinoyBusinessPOS product boundary.
Platform remains the system of record for identity, organizations, memberships, subscriptions,
and entitlements.

## Related current documents (unchanged)

- [Client experience boundaries](../architecture/client-experience-boundaries.md)
- [PinoyBusinessPOS requirements](../product/pinoy-business-pos-requirements.md)
- [Final portfolio boundaries](../engineering/final-portfolio-boundaries.md)
- [Platform–product capability boundary](../engineering/platform-product-capability-boundary.md)
- [Platform–product contracts](../engineering/platform-product-contracts.md)
- [Authentication architecture](../engineering/authentication-architecture.md)
- [Offline synchronization](../engineering/offline-sync-design.md)
- [UI design system](../engineering/ui-design-system.md)
- [ADR-010](../decisions/ADR-010-separate-ui-implementations-platform-and-pos.md)
- [ADR-022](../decisions/ADR-022-separated-antdesign-web-hosts-and-unified-auth.md)
