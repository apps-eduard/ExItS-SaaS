# Phase 19 — Mobile POS Operations and Cashier Experience Completion

[Client experience boundaries](../architecture/client-experience-boundaries.md) | [Portfolio](../portfolio-progress.md) | [Phase 18](phase-18-mobile-personal-organization-and-pos-experience.md)

## Status

**Open**

Phase 19 completes the Mobile POS operational and Cashier experience that remained after Phase 18 closeout. Reuse existing Phase 8–18 APIs and screens; finish MAUI ops UX. Phase remains **Open** until user phone confirmation after P19-WP08.

The application remains **not production-ready**. Phase 14 remains **In Progress**. Do **not** start P14-WP03 under this phase. **Not Device Verified.** **Not Complete.**

Offline operability (cold-start PIN unlock + offline cash sale foundation) is **Code Complete** with physical Android A–S **incomplete** — see [P19-offline-operability-foundation](../reports/P19-offline-operability-foundation.md). Do **not** treat offline physical validation as Device Verified.

| Field | Value |
|---|---|
| Phase | 19 — **Open** |
| Production-ready | **No** |
| Device Verified | **No** |
| User mobile validation | Pending (after P19-WP08) |
| Offline physical A–S | **Incomplete** ([report](../reports/P19-offline-operability-foundation.md)) |
| Predecessor | [Phase 18](phase-18-mobile-personal-organization-and-pos-experience.md) — Complete (implementation/scope); partial phone validation |

## Objective

Deliver complete Mobile POS operations and Cashier selling UX by finishing the surfaces deferred from Phase 18:

- Inventory
- Registers
- Shift operations
- Cashier selling experience completion
- Sales and receipt history
- Customers
- Reports, authorization, navigation, and UX hardening
- End-to-end validation and user closeout checklist

Reuse existing Phase 8–18 Platform and POS APIs/screens wherever possible. Prefer completing MAUI operational UX over inventing new backend contracts.

## Work packages

| WP | Focus | Documented status |
|---|---|---|
| [P19-WP01](../reports/P19-WP01-mobile-inventory-ui.md) | Mobile Inventory UI | **Code Complete** |
| [P19-WP02](../reports/P19-WP02-mobile-registers-ui.md) | Mobile Registers UI | **Code Complete** |
| [P19-WP03](../reports/P19-WP03-mobile-shift-operations-ui.md) | Mobile Shift Operations UI | **Code Complete** |
| [P19-WP04](../reports/P19-WP04-mobile-cashier-selling-experience.md) | Mobile Cashier Selling Experience | **Code Complete** |
| [P19-WP05](../reports/P19-WP05-mobile-sales-and-receipt-history-ui.md) | Mobile Sales and Receipt History UI | **Code Complete** |
| [P19-WP06](../reports/P19-WP06-mobile-customers-ui.md) | Mobile Customers UI | **Code Complete** |
| [P19-WP07](../reports/P19-WP07-mobile-reports-authorization-navigation-and-ux-hardening.md) | Mobile Reports, Authorization, Navigation, and UX Hardening | **Code Complete** |
| [P19-WP08](../reports/P19-WP08-end-to-end-validation-and-closeout.md) | End-to-End Validation and User Closeout Checklist | **Retest** (awaiting phone confirmation) |

Supplemental delivery (Card/GCash simulated payments): [P19-card-gcash-payment-ui-and-simulation](../reports/P19-card-gcash-payment-ui-and-simulation.md) — **Code Complete**, phone **Retest**, `FakePaymentGateway` only, **not** production-ready.

Supplemental delivery (offline operability foundation): [P19-offline-operability-foundation](../reports/P19-offline-operability-foundation.md) — **Code Complete**; physical Android A–S **incomplete** (pending sync confirmation + PIN lockout + user confirmation); **Not Device Verified**.

Supplemental delivery (MAUI list-load performance): [P19-maui-list-load-performance](../reports/P19-maui-list-load-performance.md) — Customers / Sell / Catalog first paint; sign-in no longer double-binds org under the login spinner; feature `9287de75`; **Not Device Verified**.

Supplemental delivery (connectivity / offline capability matrix): [P19-offline-connectivity-capability-matrix](../reports/P19-offline-connectivity-capability-matrix.md) — central `OfflineCapable` / `Queueable` / `OnlineRequired` policy + shared Internet-required dialog; physical validation **incomplete**.

Supplemental delivery (Personal-scope offline): [P19-personal-scope-offline-operability](../reports/P19-personal-scope-offline-operability.md) — Personal Utang local-first grant/store/policy separate from Organization POS; sync recovery + email uniqueness tip `f3d87be`; physical validation **incomplete**.

Supplemental delivery (support diagnostics): [P19-support-diagnostics](../reports/P19-support-diagnostics.md) — shared Personal/Organization Settings → Support → Diagnostics (device-local, Owner-gated for org); physical validation **incomplete**.

Supplemental delivery (organization-scoped staff identities): [P19-organization-scoped-staff-identities](../reports/P19-organization-scoped-staff-identities.md) — staff login `local@ORG######` separate from Personal/Owner; physical validation **not performed**.

## Scope notes

| In scope | Out of scope / unchanged |
|---|---|
| Complete MAUI operational UIs listed above | Claiming Device Verified before user phone confirmation after WP08 |
| Reuse of existing Phase 8–18 APIs and screens | Starting P14-WP03 or other Phase 14 production work |
| Phone validation and closeout checklist in WP08 | Production readiness claims |
| Auth / nav / UX hardening for Mobile ops | Platform Admin Web redesign; cross-product PHI |
| Offline cold-start PIN + offline cash sale foundation (supplemental) | Marking offline physical A–S complete before full checklist + user confirmation |

## Approved client boundaries (unchanged)

| Experience | Client |
|---|---|
| Platform Administration | Web only |
| Personal Account | Mobile |
| Organization Owner essentials | Mobile |
| Full Organization Administration | Web |
| POS operations | Mobile |

## Closure rule

Do **not** mark Phase 19 Complete, close P19-WP08 as Device Verified, or claim production readiness without the user’s explicit phone confirmation after mobile validation. Phase stays **Open** until that confirmation.

Offline operability does **not** change this rule: physical Android A–S for cold-start PIN / offline cash / outbox sync must remain **incomplete** until the checklist in [P19-offline-operability-foundation](../reports/P19-offline-operability-foundation.md) §12 and P19-WP08 pass with explicit user confirmation.
