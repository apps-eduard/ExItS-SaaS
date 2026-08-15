# ExITS SaaS Portfolio Progress Dashboard

> Primary status page. Cursor must update this file after every completed work package. Percentages are calculated from approved work packages, never estimated.

[Documentation Home](index.md) | [Approved architecture](engineering/approved-architecture-summary.md) | [P10-WP01 scope ambiguity](reports/P10-WP01-scope-ambiguity.md)

## Current status

| Field | Value |
|---|---|
| Portfolio | ExITS SaaS |
| Existing product | Historical HealthCare SaaS MVP (separate product; **not** in this workspace ? do not restore) |
| New product | PinoyBusinessPOS (SME retail; initial focus Sari-Sari / mini grocery) |
| Current phase | Phase 26 **Open — WP01–WP05 Code Complete / Owner Validation Pending**; Phase 25 remains **Open — Owner Validation Pending**; Phase 22/24/19/20/21 remain Open; Phase 14 still open |
| Current work package | **P26-WP05** Integration Hardening & Validation Readiness ([phase](phases/phase-26-sales-documents-compliance-readiness.md); [report](reports/P26-WP05-sales-document-compliance-integration-hardening.md); [checklist](validation/phase-26-owner-validation-checklist.md); [roadmap](compliance/bir-compliance-activation-roadmap.md)). **Owner validation pending.** Phase 26 remains **OPEN**. Phase 25 is **not closed**. |
| Overall status | **P26-WP01–WP05 Code Complete / Owner Validation Pending**; Transaction Summary remains the current sales document; Owner acknowledgment is a soft education prompt; Platform owns compliance eligibility, issuance capability, and org compliance profile anchor; TaxDocument remains unavailable (`ImplementationAvailable=false`); no BIR claim. Next: Owner validation; future confirmed BIR implementation deferred. Phase 25 WP01–WP09 owner validation remains pending. **Not Device Verified**. Production remains **Blocked**. **Not production-ready.** |
| Latest verified commit | `15eeb660` (P21-WP11 post-Phase-21 privacy impact refresh) |
| Open blockers | TLS-PROD; MAUI-HTTPS; R-109; R-129 / NU1903; auth email vendor; MFA deferred; Phase 19/20/22/24/25/26 owner/physical validation pending |
| Last updated | 2026-08-15 |

## Delivery sequence

```text
P9-WP01 ? Security and Privacy Hardening (complete with risks)
        ?
P9-WP02 ? Performance and Reliability (complete with risks)
        ?
P9-WP03 ? Backup and Restore (complete with risks)
        ?
P9-WP04 ? Accessibility, Localization and Theme QA (complete with risks)
        ?
P9-WP05 ? Pilot and Deployment (complete with risks)
        ?
P9-WP06 ? Commercial MVP Closeout (complete with risks  -  Phase 9 closed)
        ?
P10-WP01 ? Suppliers (Option A  -  master data only)
        ?
P10-WP02 ? Purchasing
        ?
P10-WP03 ? Advanced Inventory
        ?
P10-WP04 ? Cashier Shifts
        ?
P10-WP05 ? Returns and Refunds
        ?
P10-WP06 ? Advanced Permissions and Operational Reports
        ?
P10-WP07 ? Multiple Registers
        ?
P10-WP08 ? Phase 10 Closeout (complete with risks  -  Phase 10 closed)
        ?
P11-WP01 ? Web UI Audit and Component Inventory
        ?
P11-WP02 ? Global Web Layout and Navigation
        ?
P11-WP03 ? Shared Forms, Validation, and Dialogs
        ?
P11-WP04 ? Shared Tables, Lists, Cards, and Status Components
        ?
P11-WP05 ? Shared Reporting Framework
        ?
P11-WP06 ? Dashboard and Report Refactoring
        ?
P11-WP07 ? Localization, Theme, Accessibility, and Responsive QA
        ?
P11-WP08 ? Phase 11 Closeout (complete with risks  -  Phase 11 closed)
        ?
P12-WP01 ? Platform-Product Contract Audit
        ?
P12-WP02 ? Authoritative Product Foundation Reference
        ?
P12-WP03 ? Product Documentation Templates
        ?
P12-WP04 ? Cursor Product Context Rule
        ?
P12-WP05 ? Product Bootstrap Prompt
        ?
P12-WP06 ? Reference Product Dry Run
        ?
P12-WP07 ? Foundation Hardening and Closeout (complete with risks  -  Phase 12 closed)
        ?
P13-WP01 ? Authentication Architecture and Threat Model
        ?
P13-WP02 ? Identity Credentials and Auth Persistence
        ?
P13-WP03 ? Platform Login, Logout, and Browser Session
        ?
P13-WP04 ? Password Lifecycle, Lockout, and Verification
        ?
P13-WP05 ? Trusted API Actor and Organization Context
        ?
P13-WP06 ? Product Client Auth Integration (Admin + MAUI/POS)
        ?
P13-WP07 ? MFA Readiness and Auth Hardening
        ?
P13-WP08 ? Google and Facebook External Authentication
        ?
P13-WP09 ? Phase 13 Closeout (complete with residuals  -  Phase 13 closed)
        ?
P14-WP01 ? Deployment Architecture and Production Readiness Audit
        ?
P14-WP02 ? Production Packaging and Compose Baseline
        ?
P14-WP02 ? Gap Fix  -  Separate Live Preview Stack
        ?
P14-WP02A ? Live Preview Test Users and Quick Login
        ?
P14-WP03 ? Reverse Proxy, TLS, and Network Hardening
        ?
P14-WP04 ? Backup, Restore, and Ops Evidence (do not begin until authorized)
        ?
P15-WP01 ? Ant Design Admin Foundation
        ?
P15-WP02 ? Users and Organization Memberships
        ?
P15-WP03 ? Organization Lifecycle
        ?
P15-WP04 ? Product Catalog and Plan CRUD
        ?
P15-WP05 ? Subscriptions and Product Entitlements
        ?
P15-WP06 ? Users, Roles, Permissions, and RBAC
        ?
P15-WP07 ? Closeout (Phase 15 complete)
        ?
P16-WP01 ? Architecture and Domain Reconciliation
        ?
P16-WP02 ? Account Profiles and Session Isolation
        ?
P16-WP03 ? Organization Context and Navigation
        ?
P16-WP04 ? Personal Account Foundation
        ?
P16-WP05 ? Personal Utang Core
        ?
P16-WP06 ? Invitations, Linking, Reminders, Notifications
        ?
P16-WP07 ? Organization Staff and Customer Separation
        ?
P16-WP08 ? Start a Business and Utang Migration
        ?
P16-WP09 ? Product Access and Navigation Integration
        ?
P16-WP10 ? Security, Privacy, UX Hardening, and Closeout (Phase 16 complete)
        ?
P17-WP01 ? POS Access Handoff
        ?
P17-WP02 ? Initial POS Setup
        ?
P17-WP03 ? Product and Inventory Setup
        ?
P17-WP04 ? POS Staff and Role Access
        ?
P17-WP05 ? Register and Shift Operations
        ?
P17-WP06 ? Cash Sale and Receipt
        ?
P17-WP07 ? Void, Refund, and Audit
        ?
P17-WP08 ? Reports, Hardening, and Closeout (Phase 17 complete)
        ?
P18-WP01 ? Mobile Foundation and Authentication
        ?
P18-WP02 ? Personal Account and Start a Business
        ?
P18-WP03 ? Organization Selection and Owner Essentials
        ?
P18-WP04 ? POS Role Routing and Navigation
        ?
P18-WP05 ? POS Owner and Manager Mobile Experience
        ?
P18-WP06 ? Cashier Selling Experience
        ?
P18-WP07 ? Mobile Security, Resilience, and Localization
        ?
P18-WP08 ? End-to-End Validation and Closeout (Complete � closeout recorded; partial phone validation; Phase 18 Complete (implementation/scope); Not Device Verified)
        ?
P19-WP01 ? Mobile Inventory UI (Code Complete)
        ?
P19-WP02 ? Mobile Registers UI (Code Complete)
        ?
P19-WP03 ? Mobile Shift Operations UI (Code Complete)
        ?
P19-WP04 ? Mobile Cashier Selling Experience (Code Complete)
        ?
P19-WP05 ? Mobile Sales and Receipt History UI (Code Complete)
        ?
P19-WP06 ? Mobile Customers UI (Code Complete)
        ?
P19-WP07 ? Mobile Reports, Authorization, Navigation, and UX Hardening (Code Complete)
        ?
P19-WP08 ? End-to-End Validation and User Closeout Checklist (Retest � awaiting phone confirmation; Phase 19 Open; Not Device Verified)
```

## Phase progress

| Phase | Name | Status | Completed | Total | Progress | Link |
|---:|---|---|---:|---:|---:|---|
| 0 | Existing HealthCare Assessment | **Complete with documented risks** | 4 | 4 | 100% | [Open](reports/phase-00-final-assessment-and-recommendation.md) |
| 1 | Platform Boundary and Architecture | **Complete with documented risks** | 4 | 4 | 100% | [Open](phases/phase-01-platform-boundary.md) |
| 2 | Platform Extraction and HealthCare Reconnection | **Complete with documented risks** | 6 | 6 | 100% | [Open](phases/phase-02-platform-extraction.md) |
| 3 | Portfolio Billing, Plans and Entitlements | **Complete with documented risks** | 5 | 5 | 100% | [Open](phases/phase-03-billing-entitlements.md) |
| 4 | Platform Admin Expansion | **Complete with documented risks** | 4 | 4 | 100% | [Open](phases/phase-04-platform-admin.md) |
| 5 | PinoyBusinessPOS MAUI Foundation | **Complete with documented risks** | 5 | 5 | 100% | [Open](phases/phase-05-pos-maui-foundation.md) |
| 6 | Utang MVP | **Complete with documented risks** | 6 | 6 | 100% | [Open](phases/phase-06-utang-mvp.md) |
| 7 | Offline Synchronization | **Complete with documented risks** | 5 | 5 | 100% | [Open](phases/phase-07-offline-sync.md) |
| 8 | Basic Store | **Complete with documented risks** | 7 | 7 | 100% | [Open](phases/phase-08-basic-store.md) |
| 9 | MVP Hardening and Release | **Complete with documented risks** | 6 | 6 | 100% | [Open](phases/phase-09-mvp-hardening.md) |
| 10 | Full POS | **Complete with documented risks** | 8 | 8 | 100% | [Open](phases/phase-10-full-pos.md) |
| 11 | Web UI and Reporting Design System | **Complete with documented risks** | 8 | 8 | 100% | [Open](phases/phase-11-web-ui-reporting-design-system.md) |
| 12 | Reusable SaaS Product Foundation and Bootstrap | **Complete with documented open decisions** | 7 | 7 | 100% | [Open](phases/phase-12-product-foundation-and-bootstrap.md) |
| 13 | Production Authentication and Identity | **Complete with documented residuals** | 9 | 9 | 100% | [Open](phases/phase-13-production-authentication-and-identity.md) |
| 14 | Production Deployment and Operations | **In progress** | 3 | 7 |  -  | [Open](phases/phase-14-production-deployment-and-operations.md) |
| 15 | Ant Design Platform Administration | **Complete** | 7 | 7 | 100% | [Open](phases/phase-15-ant-design-platform-admin.md) |
| 16 | Isolated Account Profiles, Personal Utang, Business Upgrade | **Complete with documented residuals** | 10 | 10 | 100% | [Open](phases/phase-16-isolated-account-profiles-personal-utang-and-business-upgrade.md) |
| 17 | POS MVP Operational Onboarding and First Sale | **Complete with documented residuals** | 8 | 8 | 100% | [Open](phases/phase-17-pos-mvp-operational-onboarding-and-first-sale.md) |
| 18 | Mobile Personal, Organization, and POS Experience | **Complete (implementation/scope)** � partial phone validation; Not Device Verified | 8 | 8 | 100% | [Open](phases/phase-18-mobile-personal-organization-and-pos-experience.md) |
| 19 | Mobile POS Operations and Cashier Experience Completion | **Open** (WP01�WP07 Code Complete; WP08 Retest) | 7 | 8 |  -  | [Open](phases/phase-19-mobile-pos-operations-and-cashier-experience.md) |
| 24 | Linked Customer Statements and Personal Monetization | **Open** (WP01–WP23 Complete; WP24 Awaiting Owner Validation) | 23 | 24 |  -  | [Open](phases/phase-24-linked-customer-statements-and-personal-monetization.md) |
| 25 | Organization Web Admin, AntDesign hosts, unified web auth + identity/org management | **Open** (WP01–WP09 Code Complete; Owner Validation Pending; not Device Verified; **no closeout**) | 9 | 9 |  -  | [Open](phases/phase-25-organization-web-admin.md) |

**MVP phases 0-9:** 52 / 52 = **100%** (with documented risks; not Production-ready).
**Phase 10 Full POS:** 8 / 8 = **100%** (with documented risks; not Production-ready).
**Phase 11 Web UI / Reporting Design System:** 8 / 8 = **100%** (with documented risks; not Production-ready; no formal WCAG certification).
**Phase 12 Product Foundation:** 7 / 7 = **100%** (with documented open decisions; not Production-ready; no real product scaffold).
**Phase 13 Production Authentication:** 9 / 9 = **100%** (R-091 closed for Phase 13 scope; residuals documented; not Production-ready).
**Phase 14 Production Deployment:** 3 / 7 WPs complete (through P14-WP03 reverse-proxy/TLS template; Production blocked).
**Phase 15 Ant Design Platform Admin:** 7 / 7 WPs complete (closeout [P15-WP07](reports/P15-WP07-phase-15-closeout.md); Fluent UI direction cancelled/superseded).
**Phase 16 Account Profiles / Personal Utang:** 10 / 10 WPs complete (closeout [P16-WP10](reports/P16-WP10-phase-16-closeout.md); Phase 14 unchanged; not Production-ready).
**Phase 17 POS MVP Operational Onboarding:** 8 / 8 WPs complete (closeout [P17-WP08](reports/P17-WP08-reports-hardening-and-closeout.md); Phase 14 unchanged; not Production-ready).
**Phase 18 Mobile Personal / Org / POS Experience:** **Complete (implementation/scope)** � WP01�WP08 closed ([checklist](reports/P18-WP08-end-to-end-validation-and-closeout.md)); Products/Categories phone-validated; Quick Login pending final retest; PhysicalDevice Tailscale APK delivered; **Not Device Verified**; Inventory/Registers/Shifts/Sales/Customers/Reports/full Cashier UI ? Phase 19; Phase 14 unchanged; not Production-ready.
**Phase 19 Mobile POS Operations / Cashier Experience:** **Open** ([phase](phases/phase-19-mobile-pos-operations-and-cashier-experience.md); [P19-WP08](reports/P19-WP08-end-to-end-validation-and-closeout.md)) — WP01–WP07 Code Complete; WP08 Retest awaiting phone confirmation; offline operability foundation Code Complete with physical A–S incomplete ([report](reports/P19-offline-operability-foundation.md); commits `f476172`, `cc64ba3`, `10a1fc5`); Personal-scope offline sync hardening Code Complete ([report](reports/P19-personal-scope-offline-operability.md); tip `f3d87be`); Phase 14 unchanged; not Production-ready; **Not Device Verified**; **Not Complete**.
**Phase 21 Privacy, Compliance, and Regulatory Readiness:** **Open** ([phase](phases/phase-21-privacy-compliance-and-regulatory-readiness.md); [foundation report](reports/P21-foundation-privacy-compliance-workspace.md); [P21-WP11](reports/P21-WP11-post-phase21-privacy-impact-refresh.md); [privacy delta](compliance/post-phase21-privacy-impact-refresh.md)) — Foundation + Post–Phase-21 (P25/P26) privacy delta updated; readiness tooling only; **not** legal/NPC certification; DPO/legal review pending; Phase 25/26 remain **Open**; Phase 14/19/20 unchanged.
**Phase 23 Multi-Business Entitlements / Variable-Quantity Selling:** **Open** ([phase](phases/phase-23-multi-business-entitlements-and-variable-quantity-selling.md)) — WP01–WP11 done; WP12 in progress; WP13 closeout **not started**; **Not Device Verified**. Phase 24 does not close Phase 23.
**Phase 24 Linked Customer Statements / Personal Monetization:** **Open** — Implementation Complete / Owner Validation Pending ([phase](phases/phase-24-linked-customer-statements-and-personal-monetization.md); reports WP01–WP24; [ADR-021](decisions/ADR-021-linked-customer-statements-and-personal-monetization.md)) — WP24 Awaiting Owner Validation; **Device Verified: No**; **Production Ready: No**; Phase 24 **not** Closed.
**Phase 25 Organization Web Admin / web hosts / SSO / identity:** **Open** ([phase](phases/phase-25-organization-web-admin.md); [P25-WP01](reports/P25-WP01-organization-web-admin-management-center.md)–[P25-WP09](reports/P25-WP09-organization-ownership-transfer.md); [role matrix](engineering/organization-web-role-and-workflow-matrix.md); [responsive UI standard](engineering/organization-web-ui-responsive-standard.md); [ADR-022](decisions/ADR-022-separated-antdesign-web-hosts-and-unified-auth.md)) — WP01–WP09 Code Complete; Development Test User is username-only (manual password); Owner/Administrator post-login → Organization Web; Cashier excluded from Org workspaces; PlatformSession preserved for Org Web Platform APIs; Owner Validation Pending; **Device Verified: No**; **Production Ready: No**. **No Phase 25 closeout.**

## Phase 25 work packages

| WP | Status | Key commit |
|---|---|---|
| P25-WP01 — Organization Web Admin management center | **Code Complete / Owner Validation Pending** | [report](reports/P25-WP01-organization-web-admin-management-center.md) |
| P25-WP02 — AntDesign web standardization and host separation | **Code Complete / Owner Validation Pending** | [report](reports/P25-WP02-antdesign-web-standardization-and-host-separation.md) |
| P25-WP03 — Unified web authentication, SSO, workspace routing | **Code Complete / Owner Validation Pending** | [report](reports/P25-WP03-unified-web-authentication-sso-and-workspace-routing.md) |
| P25-WP04 — Web host legacy cleanup and Local Validation identity determinism | **Code Complete / Owner Validation Pending** | `9df02401` — [report](reports/P25-WP04-web-host-legacy-cleanup-and-local-validation-identity-determinism.md) |
| P25-WP05 — Cash count policy simplification and denomination-assisted reconciliation | **Code Complete / Owner Validation Pending** | `cbcdb8a9` (feat), `8869a179` (test), `528de183` (docs), `a50413bc` (PHP centavo defaults) — [report](reports/P25-WP05-cash-count-policy-simplification-and-denomination-assisted-reconciliation.md) |
| P25-WP06 — Personal / Organization identity isolation + typed QR | **Code Complete / Owner Validation Pending** | `3e1515b9`, `8f3875f5`, `66a972f8` — [report](reports/P25-WP06-personal-organization-identity-isolation.md) |
| P25-WP07 — POS sales buyer-party / QR purpose isolation | **Code Complete / Owner Validation Pending** | `95f5744b`, `710426a9` — [report](reports/P25-WP07-sales-buyer-party-isolation.md) |
| P25-WP08 — Organization profile independence + multi-org ownership | **Code Complete / Owner Validation Pending** | `a3dfda28`, `5fd997e0` — [report](reports/P25-WP08-organization-profile-independence.md) |
| P25-WP09 — Organization ownership transfer | **Code Complete / Owner Validation Pending** | `67bd59bd`, `f20b6dc3`, `5f51e35a` — [report](reports/P25-WP09-organization-ownership-transfer.md) |

**Phase 26 Sales Documents / Compliance Readiness:** **Open** ([phase](phases/phase-26-sales-documents-compliance-readiness.md); [roadmap](compliance/bir-compliance-activation-roadmap.md); [checklist](validation/phase-26-owner-validation-checklist.md)) — WP01–WP05 Code Complete / Owner Validation Pending; TaxDocument unavailable; no BIR claim; Phase 25 remains Open.

## Phase 26 work packages

| WP | Status | Key commit |
|---|---|---|
| P26-WP01 — Sales-document kinds / capability foundation | **Code Complete / Validation Pending** | [report](reports/P26-WP01-sales-document-compliance-readiness-foundation.md) |
| P26-WP02 — Owner education and acknowledgment | **Code Complete / Validation Pending** | [report](reports/P26-WP02-organization-compliance-education-and-acknowledgment.md) |
| P26-WP03 — Platform-controlled compliance eligibility / grant-revoke | **Code Complete / Validation Pending** | `73b5822c` — [report](reports/P26-WP03-platform-controlled-compliance-capability-and-eligibility.md) |
| P26-WP04 — Organization Tax/Compliance Profile & Future Activation Foundation | **Code Complete / Validation Pending** | `c794707e` — [report](reports/P26-WP04-organization-tax-compliance-profile-and-activation-foundation.md); [roadmap](compliance/bir-compliance-activation-roadmap.md) |
| P26-WP05 — Integration Hardening & Validation Readiness | **Code Complete / Owner Validation Pending** | `254424df` — [report](reports/P26-WP05-sales-document-compliance-integration-hardening.md); [checklist](validation/phase-26-owner-validation-checklist.md) |

## Phase 24 work packages

| WP | Status | Key commit |
|---|---|---|
| P24-WP01 — Current-state audit + architecture contract | **Complete** (architecture contract) | `1351bef72f9ba04030495785767cc9bb609c5f8d` — [report](reports/P24-WP01-current-state-and-architecture-contract.md) |
| P24-WP02 — Customer-link completeness + POS↔Platform correlation | **Complete** | `19786a3d2a19fdf0131c5ca315a272e012ab2926` (feat), `b99809cd3a46d28983a663e0ca9bbe9488a5ffbb` (docs) — [report](reports/P24-WP02-customer-link-and-pos-correlation.md) |
| P24-WP03 — Linked-customer authorization contract | **Complete** | `d8c90f0c46fe8d70efb93970fb93d96412c5fc39` (feat), `ef947f5ee274bc7cda1d09b7af5b6a65682ebfe7` (docs) — [report](reports/P24-WP03-linked-customer-authorization-contract.md) |
| P24-WP04 — Lightweight linked Business Utang statement projection | **Complete** | `cd24b28ad29a5a4ddc3af9e49021884d7640a520` (feat), `b8cf30964fec6270f5fefd8435488bdf806ca4d1` (docs) — [report](reports/P24-WP04-lightweight-linked-business-utang-statement.md) |
| P24-WP05 — Receipt summary/detail and lazy loading | **Complete** | `b819914aa8403af02db3015a3eb47f681e25ec01` (feat), `d0e9254ff042125932a351eebb3fe85c761e6e0f` (docs) — [report](reports/P24-WP05-receipt-summary-detail-and-lazy-loading.md) |
| P24-WP06 — Free vs Paid Personal history entitlement | **Complete** | `7a9a8301de1489c75ccc3a4a9890d0a7d3b54196` (feat), `cdd991b51e02fbb1de576c6337793e2b809503e8` (docs) — [report](reports/P24-WP06-free-vs-paid-personal-history-entitlement.md) |
| P24-WP07 — Personal reward points ledger + feature redemption | **Complete** | `d41106acd32bd950bb6638bc769539ab22abd99a` (feat), `20a61d99afbfca3587b4d688fff5161b508a66ca` (docs) — [report](reports/P24-WP07-personal-reward-points-and-redemption.md) |
| P24-WP08 — Reward ledger foundation | **Complete** | `f18a9ea76b34e4f265dcc29af7b0220cc3c8a625` (feat), `0d0d6595d1e57b756bf30599bd801cd19544419d` (docs) — [report](reports/P24-WP08-reward-ledger-foundation.md) |
| P24-WP09 — Ads abstraction + Ad-Free entitlement | **Complete** | `ea9bac0db464f369238d40957789b6b3d4188a4f` (feat), `50a517b9591d77fc75fb8dc46044fb96010f496f` (docs) — [report](reports/P24-WP09-ads-abstraction-and-ad-free-entitlement.md) |
| P24-WP10 — Entitlement-aware older/settled history | **Complete** | `40d6da229e2473c347664a79c91463770f1547ee` (feat), `59af882023428f4961c97297c4c80633c517f731` (docs) — [report](reports/P24-WP10-entitlement-aware-older-settled-history.md) |
| P24-WP11 — Admin configuration for Personal features | **Complete** | `f9f479dbe784103d74803160132ae28c510eb69f` (feat), `fc046aa8f1278b4d902488adb75f55480bfef814` (docs) — [report](reports/P24-WP11-admin-configuration-for-personal-features.md) |
| P24-WP12 — Regression, security, and edge-case tests | **Complete** | `5ccf12e2a1a420ae4ff9ef3cdbc586868f33126c` (test), `c6f013ace3e1800bfbcb9a5179fae4ffb1df005b` (docs) — [report](reports/P24-WP12-regression-security-and-edge-case-tests.md) |
| P24-WP13 — Dispute/request architecture (optional) | **Complete** (architecture; implementation deferred) | `6a10dbf503c35e086e72c00f8c503bb005facfae` (docs) — [report](reports/P24-WP13-dispute-request-architecture.md) |
| P24-WP14 — Documentation / backend closeout preparation | **Complete** | `d5b25e6cc197d4f4cf7955051282e6293df52655` (docs) — [report](reports/P24-WP14-documentation-backend-closeout-preparation.md) |
| P24-WP15 — Physical Android validation preparation | **Complete** (prep ≠ Device Verified) | `b1200f314c02c7573224899c6f1f516d5d9a32b9` (docs) — [report](reports/P24-WP15-physical-android-validation-preparation.md) |
| P24-WP16 — Personal mobile linked-customer statement experience | **Complete** | `de568ae08f17b11c2c14823f4e2b4c3e9f337c78` (feat) — [report](reports/P24-WP16-personal-mobile-linked-customer-statement-experience.md) |
| P24-WP17 — Mobile receipts and older-history entitlement UX | **Complete** | `cf81e15b8bd9d6ff27b66db6f105a880fe23a96d` (feat) — [report](reports/P24-WP17-mobile-receipts-and-older-history-entitlement-ux.md) |
| P24-WP18 — Mobile rewards and Personal feature redemption | **Complete** | `ab3dd06fdc604dc385450222d2c762927968aa3e` — [report](reports/P24-WP18-mobile-rewards-and-personal-feature-redemption.md) |
| P24-WP19 — Mobile ads/ad-free UX abstraction | **Complete** | `ab3dd06fdc604dc385450222d2c762927968aa3e` — [report](reports/P24-WP19-mobile-ads-ad-free-ux-abstraction.md) |
| P24-WP20 — Android integration and end-to-end mobile flows | **Complete** | `ab3dd06fdc604dc385450222d2c762927968aa3e` — [report](reports/P24-WP20-android-integration-and-e2e-mobile-flows.md) |
| P24-WP21 — Physical Android device validation and fix pass | **Complete** (Device Verified **No**) | `ab3dd06fdc604dc385450222d2c762927968aa3e` — [report](reports/P24-WP21-physical-android-device-validation-and-fix-pass.md) |
| P24-WP22 — Mobile regression, privacy, security, and resilience hardening | **Complete** | `ab3dd06fdc604dc385450222d2c762927968aa3e` — [report](reports/P24-WP22-mobile-regression-privacy-security-resilience.md) |
| P24-WP23 — Phase-24 implementation closeout preparation | **Complete** | Owner Validation Pending — [report](reports/P24-WP23-phase-24-implementation-closeout-preparation.md) |
| P24-WP24 — Owner/User Final Validation and Acceptance | **Awaiting Owner Validation** | Hard user gate — [report](reports/P24-WP24-owner-user-final-validation-and-acceptance.md) |

## Phase 21 work packages

| WP | Status | Key commit |
|---|---|---|
| P21 foundation (WP01–WP10 slice) | **Code Complete** (readiness tooling) | `7f6795b` (feat), `26ec821` (test) — [report](reports/P21-foundation-privacy-compliance-workspace.md) |
| P21-WP01 Requirements & privacy inventory | **Code Complete** | `7f6795b` — [report](reports/P21-WP01-requirements-and-privacy-inventory.md) |
| P21-WP11 Post–Phase-21 privacy impact refresh | **Code Complete / Validation Pending** | `15eeb660` — [report](reports/P21-WP11-post-phase21-privacy-impact-refresh.md); [delta](compliance/post-phase21-privacy-impact-refresh.md) |

## Phase 19 work packages

| WP | Status | Key commit |
|---|---|---|
| P19-WP01  -  Mobile Inventory UI | **Code Complete** | `01f7a87`  -  [report](reports/P19-WP01-mobile-inventory-ui.md) |
| P19-WP02  -  Mobile Registers UI | **Code Complete** | `ee2ffb6`  -  [report](reports/P19-WP02-mobile-registers-ui.md) |
| P19-WP03  -  Mobile Shift Operations UI | **Code Complete** | `1c86c49`  -  [report](reports/P19-WP03-mobile-shift-operations-ui.md) |
| P19-WP04  -  Mobile Cashier Selling Experience | **Code Complete** | `94a354d`  -  [report](reports/P19-WP04-mobile-cashier-selling-experience.md) |
| P19-WP05  -  Mobile Sales and Receipt History UI | **Code Complete** | `43564e6`  -  [report](reports/P19-WP05-mobile-sales-and-receipt-history-ui.md) |
| P19-WP06  -  Mobile Customers UI | **Code Complete** | `7361d2c`  -  [report](reports/P19-WP06-mobile-customers-ui.md) |
| P19-WP07  -  Mobile Reports, Authorization, Navigation, and UX Hardening | **Code Complete** | `1dad55a`  -  [report](reports/P19-WP07-mobile-reports-authorization-navigation-and-ux-hardening.md) |
| P19-WP08  -  End-to-End Validation and User Closeout Checklist | **Retest** | `817e72c`  -  [report](reports/P19-WP08-end-to-end-validation-and-closeout.md) |
| Supplemental — Offline operability foundation | **Code Complete**; physical A–S **incomplete** | `10a1fc5` (tip) / `f476172` / `cc64ba3` — [report](reports/P19-offline-operability-foundation.md) |
| Supplemental — Personal-scope offline operability | **Code Complete**; device incomplete; manual Retry supported | `f3d87be` — [report](reports/P19-personal-scope-offline-operability.md) |

## Phase 18 work packages

| WP | Status | Key commit |
|---|---|---|
| P18-WP01  -  Mobile Foundation and Authentication | Code Complete and Build Verified | `4b8b727`  -  [report](reports/P18-WP01-mobile-foundation-and-authentication.md) |
| P18-WP02  -  Personal Account and Start a Business | Code Complete and Build Verified | `4b8b727`  -  [report](reports/P18-WP02-personal-account-and-start-business.md) |
| P18-WP03  -  Organization Selection and Owner Essentials | Code Complete and Build Verified | `4b8b727`  -  [report](reports/P18-WP03-organization-selection-and-owner-essentials.md) |
| P18-WP04  -  POS Role Routing and Navigation | Code Complete and Build Verified | `4b8b727`  -  [report](reports/P18-WP04-pos-role-routing-and-navigation.md) |
| P18-WP05  -  POS Owner and Manager Mobile Experience | Code Complete and Build Verified | `4b8b727`  -  [report](reports/P18-WP05-pos-owner-and-manager-mobile-experience.md) |
| P18-WP06  -  Cashier Selling Experience | Code Complete and Build Verified | `4b8b727`  -  [report](reports/P18-WP06-cashier-selling-experience.md) |
| P18-WP07  -  Mobile Security, Resilience, and Localization | Code Complete and Build Verified | `4b8b727`  -  [report](reports/P18-WP07-mobile-security-resilience-and-localization.md) |
| P18-WP08  -  End-to-End Validation and Closeout | **Complete** (closeout recorded; partial phone validation) | baseline `f86dcd2`  -  [report](reports/P18-WP08-end-to-end-validation-and-closeout.md) |

## Phase 17 work packages

| WP | Status | Key commit |
|---|---|---|
| P17-WP01  -  POS Access Handoff | Complete | See Phase 17 commit  -  [report](reports/P17-WP01-pos-access-handoff.md) |
| P17-WP02  -  Initial POS Setup | Complete | See Phase 17 commit  -  [report](reports/P17-WP02-initial-pos-setup.md) |
| P17-WP03  -  Product and Inventory Setup | Complete | See Phase 17 commit  -  [report](reports/P17-WP03-product-and-inventory-setup.md) |
| P17-WP04  -  POS Staff and Role Access | Complete | See Phase 17 commit  -  [report](reports/P17-WP04-pos-staff-and-role-access.md) |
| P17-WP05  -  Register and Shift Operations | Complete | See Phase 17 commit  -  [report](reports/P17-WP05-register-and-shift-operations.md) |
| P17-WP06  -  Cash Sale and Receipt | Complete | See Phase 17 commit  -  [report](reports/P17-WP06-cash-sale-and-receipt.md) |
| P17-WP07  -  Void, Refund, and Audit | Complete | See Phase 17 commit  -  [report](reports/P17-WP07-void-refund-and-audit.md) |
| P17-WP08  -  Reports, Hardening, and Closeout | Complete | See Phase 17 commit  -  [report](reports/P17-WP08-reports-hardening-and-closeout.md) |

## Phase 16 work packages

| WP | Status | Key commit |
|---|---|---|
| P16-WP01  -  Architecture and Domain Reconciliation | Complete | `d1e0096caac1b5aa0e47721938635a1e9766c66b`  -  [report](reports/P16-WP01-architecture-and-domain-reconciliation.md) |
| P16-WP02  -  Account Profiles and Session Isolation | Complete | `f0bb6c9ec87e75e7505087404cad463f931f5a67`  -  [report](reports/P16-WP02-account-profiles-and-session-isolation.md) |
| P16-WP03  -  Organization Context and Navigation | Complete | `3454a7e6caa0d307d03a03d91abe7250ccad96a1`  -  [report](reports/P16-WP03-organization-context-and-navigation.md) |
| P16-WP04  -  Personal Account Foundation | Complete | `17f53e204243844b86602eaf12369495ffd8db01`  -  [report](reports/P16-WP04-personal-account-foundation.md) |
| P16-WP05  -  Personal Utang Core | Complete | `4b7b4d5c223bf4e293248881df14c970e76e80d1`  -  [report](reports/P16-WP05-personal-utang-core.md) |
| P16-WP06  -  Invitations, Linking, Reminders, Notifications | Complete | `6f85bd3fb324a93fc8eadf2f82426be0178b064e`  -  [report](reports/P16-WP06-invitations-linking-reminders-notifications.md) |
| P16-WP07  -  Organization Staff and Customer Separation | Complete | `ae39e9f7084f44c6c5a9a5e598767fc91987feae`  -  [report](reports/P16-WP07-organization-staff-customer-separation.md) |
| P16-WP08  -  Start a Business and Utang Migration | Complete | `cb3f3585e07e6b0865df1a40175b9f5b99a22a78`  -  [report](reports/P16-WP08-start-a-business-and-utang-migration.md) |
| P16-WP09  -  Product Access and Navigation Integration | Complete | `9ae47bc635eb30b357c6f8317c9025ad850e054e`  -  [report](reports/P16-WP09-product-access-and-navigation-integration.md) |
| P16-WP10  -  Security, Privacy, UX Hardening, and Closeout | Complete | `4118797ed3555640cccca8e0c7bb15458035dd75`  -  [report](reports/P16-WP10-phase-16-closeout.md) |
| P16-WP11  -  Validation, Stabilization, and User Acceptance | **In progress** |  -   -  [report](reports/P16-WP11-local-validation-replaces-live-preview.md) |
| P16-WP12  -  Final Closeout | **Not started** |  - |

## Phase 15 work packages

| WP | Status | Key commit |
|---|---|---|
| P15-WP01  -  Ant Design Admin Foundation | Complete | `0ee125487cba83747f36fd260c404249700ae858` |
| P15-WP02  -  Users and Organization Memberships | Complete | `e607a10a8712a5e326e42b3a6bf56a38ac1abe4c` |
| P15-WP03  -  Organization Lifecycle | Complete | `81d19733864c4f0756d061120b156f0390d458f0`  -  [report](reports/P15-WP03-organization-lifecycle.md) |
| P15-WP04  -  Product Catalog and Plan CRUD | Complete | `d0e2ad3bd211607b59e16278fbb94e4fc73589f3`  -  [report](reports/P15-WP04-product-catalog-and-plan-crud.md) |
| P15-WP05  -  Subscriptions and Product Entitlements | Complete | `8f664c80eea267a5b538e7424668ba4e1af0e247`  -  [report](reports/P15-WP05-subscriptions-and-entitlements.md) |
| P15-WP06  -  Users, Roles, Permissions, and RBAC | Complete | `2b9657bbb4c0e597c2098ef1a2fa5bb1e630ba52`  -  [report](reports/P15-WP06-users-roles-permissions-rbac.md) |
| P15-WP07  -  Closeout | Complete | `77f4030fa6110a20f854d0e146132aad0ec5e31c`  -  [report](reports/P15-WP07-phase-15-closeout.md) |

## Phase 13 work packages

| WP | Status | Key commit |
|---|---|---|
| P13-WP01  -  Authentication Architecture and Threat Model | Complete | `40a48349ae2a42e9dc267bde0df64afb004af3ae` |
| P13-WP02  -  Identity Credentials and Auth Persistence | Complete | `51ace5b90fc6c0bcb33fe483826481529bdfeb77` |
| P13-WP03  -  Platform Login, Logout, and Browser Session | Complete | `6298b668c5d0555a84eb206b2a2313b138c9b892` |
| P13-WP04  -  Password Lifecycle, Lockout, and Verification | Complete | `65b261eca7353a7efea2f8f1899c252f0dcee6dc` |
| P13-WP05  -  Trusted API Actor and Organization Context | Complete | `e64f352161bb20447a99ae762d1a69ec1a3846fe` |
| P13-WP06  -  Product Client Auth Integration (Admin + MAUI/POS) | Complete | `68f13c0a4281071087e526ecf8e51414f2a78b12` |
| P13-WP07  -  MFA Readiness and Auth Hardening | Complete | `7b767f664e63c5c296e0444062129acd7ee36727` |
| P13-WP08  -  Google and Facebook External Authentication | Complete | `7c9338090f55b0fc2e289fe3b95fb3b4ce5d7938` |
| P13-WP09  -  Phase 13 Closeout | Complete | `ef949b78c2a8e2271dfd7f1e5b54e72092db74d1` |

## Phase 14 work packages

| WP | Status | Key commit |
|---|---|---|
| P14-WP01  -  Deployment Architecture and Production Readiness Audit | Complete | `e0e2da2d03babc01dd6efab9d44c6c2a2668457a` |
| P14-WP02  -  Production Packaging and Compose Baseline | Complete | `fa04ee2e9decd200b4dc1407f4f1b88f91f93afe` |
| P14-WP02 Gap Fix  -  Separate Live Preview Stack | Complete | `16342195ff4999f7c0fc99fa15306fc3fa530074` |
| P14-WP02A  -  Live Preview Test Users and Quick Login | Complete | `ffe12b1ffe73f8e202079c3ed76b7c1f39bd6e9d` |
| P14-WP03  -  Reverse Proxy, TLS, and Network Hardening | Complete | `a015d0afad0ab20c4a2a9f019615970c82b3f3d6`  -  [report](reports/P14-WP03-reverse-proxy-tls-network-hardening.md) |
| P14-WP04  -  Production Backup, Restore, and Ops Evidence | Not started |  -  |
| P14-WP05  -  Monitoring, Alerting, and Support Model | Not started |  -  |
| P14-WP06  -  Deployment Readiness Evaluator Alignment | Not started |  -  |
| P14-WP07  -  Phase 14 Closeout | Not started |  -  |

## Phase 12 work packages

| WP | Status | Key commit |
|---|---|---|
| P12-WP01  -  Platform-Product Contract Audit | Complete | `32889be0851fa0969e8abfa6b7c66784b12e9e8b` |
| P12-WP02  -  Authoritative Product Foundation Reference | Complete | `8f151d658011a3ad0854aab9f8774361f8a788a6` |
| P12-WP03  -  Product Documentation Templates | Complete | `65b02a1dd9336b39b79fc41527969f6289ad7072` |
| P12-WP04  -  Cursor Product Context Rule | Complete | `1243c78d65e347b23949b19ce2edf564fe972aad` |
| P12-WP05  -  Product Bootstrap Prompt | Complete | `d57b7be48639e30ffa9fa86624da916ef63a563f` |
| P12-WP06  -  Reference Product Dry Run | Complete | `5debab509c52ecdbed1cf9bba1ec02147ece693b` |
| P12-WP07  -  Foundation Hardening and Closeout | Complete | `2a3de32cb3bcc1c30db34771843c054e74f6a29e` |

## Phase 11 work packages

| WP | Status | Key commit |
|---|---|---|
| P11-WP01  -  Web UI Audit and Component Inventory | Complete | `221fe69ab179956e8a73411cf3eb58fd6f199c3c` |
| P11-WP02  -  Global Web Layout and Navigation | Complete | `7ce7df139a9494c9aab7d189900e96d5e43fdc1d` |
| P11-WP03  -  Shared Forms, Validation, and Dialogs | Complete | `6825b8eb423e73cd5d3dc24e393e7201b04232bc` |
| P11-WP04  -  Shared Tables, Lists, Cards, and Status Components | Complete | `0351f547457522a97a168b802ec050ef6f37ee83` |
| P11-WP05  -  Shared Reporting Framework | Complete | `4d832b39d85d7f8db55234f609188666035f34c5` |
| P11-WP06  -  Dashboard and Report Refactoring | Complete | `6688fa674e5edc139a931dae3faefeb8b25a806b` |
| P11-WP07  -  Localization, Theme, Accessibility, and Responsive QA | Complete | `24ee744fa15152bc325568ba6c5a99de78359921` |
| P11-WP08  -  Phase 11 Closeout | Complete | `ff2ad9e2e756f6e011fcf60f14e6350a3c15e32e` |

## Phase 10 work packages

| WP | Status | Key commit |
|---|---|---|
| P10-WP01  -  Suppliers | Complete (Option A) | 6f92dd43b2f66709891d82079f9d3fbd0b5c450e |
| P10-WP02  -  Purchasing | Complete | c0f8130ef99e958bceaee98024a69339b7e8e41a |
| P10-WP03  -  Advanced Inventory | Complete | 5c62133 (+ gap-fix 31d809c) |
| P10-WP04  -  Cashier Shifts | Complete | 4076485 |
| P10-WP05  -  Returns and Refunds | Complete | 58dd6bf (+ Android using fix 6cb06cc) |
| P10-WP06  -  Advanced Permissions and Operational Reports | Complete | 1e46f6eb142d1c14455f954e7c8286abeb1ddff3 |
| P10-WP07  -  Multiple Registers | Complete | 7dda3baedd452b39cb5d4fab55fb700ef67a9639 |
| P10-WP08  -  Phase 10 Closeout | Complete | validation `32395ff1?`; docs tip `de09f97b0045636f9da004f1b7cc95bf7be17441` |

## Phase 9 work packages

| WP | Status | Key commit |
|---|---|---|
| P9-WP01 | Complete with risks | de4fac64739f5b368a6b1f2490223fa032201b65 |
| P9-WP02 | Complete with risks | 46a4ac7bacfad0736fba4741817958862fadf9e2 |
| P9-WP03 | Complete with risks | 3bbb0c716da60bd7d87a191c35bd0eced1bde380 |
| P9-WP04 | Complete with risks | f7b3aecec614eea8b1de601cd08e843f4aea91f8 |
| P9-WP05 | Complete with risks | 9c1bbd0557e252758a772b985c907233da3f5214 |
| P9-WP06 | Complete with risks | f6117c59e9c63d629af5805cf2d4ae7f8ea61225 |

## Permanent workflow rules

Follow `.cursor/rules/exits-workflow.mdc`. Do not begin **P14-WP04** until explicitly authorized. Do not start **P14-WP03** under Phase 19. Platform Admin UI direction is **Ant Design Blazor (ADR-015)**; Fluent UI Admin work is cancelled. Phase 15 is complete. Phase 17 is **complete**. Phase 18 is **Complete (implementation/scope)** (partial phone validation; **Not Device Verified**). Phase 19 is **Open**. Phase 16 feature closeout is complete with validation residuals (P16-WP11/WP12). Phase 14 remains in progress; Production remains Blocked.
