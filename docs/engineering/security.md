# Security

[Home](../index.md) | [Authorization](authorization-matrix.md) | [Capability boundary](platform-product-capability-boundary.md) | [Contracts](platform-product-contracts.md) | [Extraction sequence](../reuse/extraction-sequence.md) | [Data classification](data-classification-matrix.md) | [ADR-011](../decisions/ADR-011-platform-authority-and-product-local-projections.md) | [ADR-012](../decisions/ADR-012-versioned-platform-contracts-and-local-projections.md) | [ADR-013](../decisions/ADR-013-build-new-platform-before-healthcare-reconnection.md)

## Invariants

1. PlatformOrganizationId and product tenant context are server-controlled.
2. One product cannot access another product’s operational database.
3. Product APIs enforce their own roles and permissions.
4. Subscription and feature checks are server-side.
5. Entitlement snapshots are signed/validated and time-bounded.
6. Posted financial records are append-only and corrected by reversal/adjustment.
7. HealthCare patient self-scope remains HealthCare-specific.
8. Secrets and tokens never appear in logs.
9. Localization cannot expose untranslated internal keys or sensitive debug details.
10. Theme selection cannot weaken focus visibility or contrast.
11. Clinical PHI must not flow into Platform audit or entitlement payloads.
12. SaaS subscription payments are distinct from POS retail sale payments and from POS customer-credit payments.
13. POS MVP GCash is manually recorded; do not store GCash credentials, PINs, OTPs, or access tokens. Platform SaaS GCash (if added later) uses Platform payment entities only.
14. **P2-WP02:** Platform User is not Patient/Customer; Platform Organization is not Clinic/Store; organization membership roles are not product-local roles; no credentials in domain models; user suspension and membership suspension are separate.
15. **P2-WP03:** Commercial entitlements are not operational settings; snapshots must not contain clinical/retail records; published plan versions are immutable; SaaS payments are not modeled in this foundation.
16. **P2-WP04:** Outbound contracts exclude credentials and clinical payloads; unsupported contract majors fail closed; Platform organization roles must not be treated as clinical roles; contracts ≠ completed HealthCare integration.
17. **P2-WP05:** Migration dry-run models exclude credentials/PHI; sensitive metadata probe fails closed; dry-run ≠ production migration; rollback evidence required before cutover (R-027/R-044).
18. **P3-WP01/P3-WP02:** Catalog, organization, and subscription APIs are unauthenticated (development-stage only). Do not expose publicly without Platform authentication. Persistence exceptions must not leak provider details. Commercial `ActivateSubscription` does **not** collect or verify payment.
19. **P3-WP03:** Manual SaaS payment APIs are unauthenticated. Actor references (`confirmedBy`, `rejectedBy`, `voidedBy`) accept plain strings — **production blocker** requiring authenticated Platform operator with payment-confirm permission. No payment credentials, GCash PINs, OTPs, card data, or gateway tokens stored. Manual GCash means an operator confirms externally and records the reference; Platform does not call GCash API, generate QR, or receive webhooks.
20. **P3-WP04/P3-WP05:** Entitlement snapshot and feature-override APIs are unauthenticated. Snapshots must not contain clinical/retail operational data. Overrides require reason and actor GUID (no auth yet — production blocker). Provisional 24h refresh policy is **not** a final R-022 decision. Snapshots are not product delivery. Phase 3 is closed with these limitations explicit.
21. **P4-WP01:** Platform Admin UI is unauthenticated and must display a development-stage security warning. Dev operator context is a display label only — not authorization. Admin must not call Infrastructure/EF directly. Manual-payment and entitlement-delivery warnings are mandatory on those screens. Do not describe Admin as production-secure.
22. **P4-WP02:** Platform user/membership/product-access APIs remain unauthenticated. Product-access assignment is commercial entry eligibility only — never product-local roles. Suspended/disabled users and ineligible subscriptions fail closed. No passwords, JWT, MFA, SSO, or AspNet Identity tables in this work package.
23. **P4-WP03:** Subscription and manual SaaS payment Admin mutations remain without production authentication. Manual confirmation is not provider verification. No card numbers, CVV, PIN, OTP, gateway SDKs, webhooks, or invoice engines. Subscription changes do not deliver entitlements or assign product-local roles.
24. **P4-WP04:** Sensitive Platform mutations enforce server-side `PlatformAuthz` (fail closed → 403 + denied audit). Append-only `platform.audit_records` must never store passwords, tokens, card/GCash secrets, PHI, raw payloads, or exception dumps. Admin UI permission visibility is not authorization. DevelopmentOperator full access is Development/Testing-only and is not production authentication. Themes must preserve focus/contrast; localization must not leak keys or sensitive debug text.
