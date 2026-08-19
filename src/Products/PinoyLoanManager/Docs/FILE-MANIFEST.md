# Pinoy Loan Manager — File Manifest

**Status:** PLM-01 scaffold; Gates B–D2 complete; PLM-D3-PRE Platform product registration + current-session access
**Implementation present:** Product shell + React Client + online-first PWA + cookie Sign In + Personal account lifecycle; Platform catalog code `pinoy-loan-manager` + Local Validation fixture + current-session access API; no Gate D3 React org selector/lending/Capacitor
**Current work package:** PLM-D3-PRE product registration + current-session product access

This file is the navigation map for future Cursor work. Load this product’s `Docs/` after the shared Product Foundation reference. Do not scan PinoyBusinessPOS implementation by default.

Shared contracts to load with this product:

- `.cursor/rules/exits-workflow.mdc`
- `.cursor/rules/exits-product-context.mdc`
- `docs/Product-Foundation/exits-product-foundation-reference.md`

---

## Canonical documents

| Path | Purpose | Status | Implementation present |
|---|---|---|---|
| `Docs/product-definition.md` | Product identity, ownership, boundaries, exclusions | Foundation / Planning Only | No |
| `Docs/architecture.md` | Technical and data boundaries; Personal/Borrower intent | Foundation / Planning Only | No |
| `Docs/security.md` | Security, privacy, consent | Foundation / Planning Only | No |
| `Docs/authorization-matrix.md` | Access intersection; role presets; grant intent; identifiers open | Foundation / Planning Only | No |
| `Docs/development-plan.md` | Delivery buckets PLM-00–PLM-14 | Foundation / Planning Only | No |
| `Docs/roadmap.md` | Current phase and work-package sequence | Foundation / Planning Only | No |
| `Docs/risks-and-decisions.md` | Open risks and decisions | Foundation / Planning Only | No |

## Operating-model direction (PLM-00-WP03)

| Path | Purpose | Status | Implementation present |
|---|---|---|---|
| `Docs/Product/lending-operating-model.md` | Origination paths, shared Loan core, roles, branch, PHP, Platform usage | Agreed direction / not a spec | No |
| `Docs/Product/quick-loan-model.md` | Templates, snapshot, eligibility, Personal flow | Agreed direction / not a spec | No |
| `Docs/Product/collector-cash-and-reconciliation.md` | Loan ledger vs collector cash | Agreed direction / not a spec | No |
| `Docs/Product/penalty-exception-and-waiver-model.md` | Penalty, exception, waiver, reversal, post-maturity | Agreed direction / not a spec | No |
| `Docs/Architecture/application-surface-model.md` | Platform Admin, Org Web/PWA, Capacitor Android, Personal | Agreed direction / not a spec | No |

## Financial / lifecycle planning (PLM-00-WP04)

| Path | Purpose | Status | Implementation present |
|---|---|---|---|
| `Docs/Product/financial-calculation-baseline.md` | Money terms, interest-treatment modes, precision | Planning baseline / not a spec | No |
| `Docs/Product/payment-and-allocation-model.md` | Payments, allocation, reversals, idempotency | Planning baseline / not a spec | No |
| `Docs/Product/schedule-maturity-and-settlement.md` | Schedule, calendar, maturity, settlement | Planning baseline / not a spec | No |
| `Docs/Product/loan-lifecycle-model.md` | Origination vs lifecycle vs delinquency | Planning baseline / not a spec | No |
| `Docs/Architecture/loan-ledger-and-balance-model.md` | Operational subledger and balances | Planning baseline / not a spec | No |

## Authorization / cash / operational workflow (PLM-00-WP05)

| Path | Purpose | Status | Implementation present |
|---|---|---|---|
| `Docs/Security/role-and-grant-baseline.md` | Presets, grant catalog intent, scope, SoD | Planning baseline / not a spec | No |
| `Docs/Product/daily-operational-workflow.md` | Common operating day, assignments, offline boundary | Planning baseline / not a spec | No |
| `Docs/Product/cashier-and-collector-control-model.md` | Cashier Session, float, remittance, cash availability | Planning baseline / not a spec | No |
| `Docs/Product/disbursement-and-payment-controls.md` | Office/field disbursement and cash payment | Planning baseline / not a spec | No |
| `Docs/Product/exception-reversal-and-variance-workflow.md` | Exceptions, waivers, reversals vs cash refund, variance | Planning baseline / not a spec | No |

## Borrower / Personal / publishing (PLM-00-WP06)

| Path | Purpose | Status | Implementation present |
|---|---|---|---|
| `Docs/Product/borrower-model.md` | PLM-owned Borrower; independent of POS/Personal | Planning baseline / not a spec | No |
| `Docs/Product/personal-borrower-linking.md` | Optional consent-based linking and unlink | Planning baseline / not a spec | No |
| `Docs/Product/quick-loan-publishing-and-eligibility.md` | Publishing audiences; eligibility ≠ approval | Planning baseline / not a spec | No |
| `Docs/Product/borrower-groups-and-targeting.md` | Organization-owned groups | Planning baseline / not a spec | No |
| `Docs/Architecture/personal-integration-boundary.md` | Personal vs PLM authority | Planning baseline / not a spec | No |

## Traditional origination (PLM-00-WP07)

| Path | Purpose | Status | Implementation present |
|---|---|---|---|
| `Docs/Product/traditional-loan-model.md` | Traditional origination path; converges after disbursement | Planning baseline / not a spec | No |
| `Docs/Product/loan-application-and-approval.md` | Application capture, approval, rejection, term changes | Planning baseline / not a spec | No |
| `Docs/Product/loan-product-configuration.md` | Reusable Loan Product (not a Loan) | Planning baseline / not a spec | No |
| `Docs/Product/disbursement-readiness-model.md` | Pre-release checks; approval ≠ disbursement | Planning baseline / not a spec | No |

## Reporting / documents / notifications (PLM-00-WP08)

| Path | Purpose | Status | Implementation present |
|---|---|---|---|
| `Docs/Product/reporting-baseline.md` | Dashboard and operational reporting areas | Planning baseline / not a spec | No |
| `Docs/Product/loan-documents-and-receipts.md` | Documents, snapshot, durable receipts | Planning baseline / not a spec | No |
| `Docs/Product/notification-model.md` | Personal and staff notification intent | Planning baseline / not a spec | No |
| `Docs/Product/personal-loan-experience.md` | Personal Loan area; distinct from P2P | Planning baseline / not a spec | No |
| `Docs/Security/audit-and-history-baseline.md` | High-risk history; not editable notes | Planning baseline / not a spec | No |

## Technical layout / integration (PLM-00-WP09)

| Path | Purpose | Status | Implementation present |
|---|---|---|---|
| `Docs/Architecture/source-and-project-layout.md` | Physical layout; Client Gate B scaffold | PLM-01 + Gate B | Product shell + React Client |
| `Docs/Architecture/react-pwa-capacitor-client.md` | Shared React + PWA + Capacitor architecture | Accepted (PLM-D-00-09); Gate B scaffold | React foundation |
| `Docs/Architecture/api-and-contract-boundary.md` | API consumers; Personal contracts | Planning target / not a spec | No |
| `Docs/Architecture/persistence-and-database-boundary.md` | Separate DB isolation | Planning target / not a spec | No |
| `Docs/Architecture/mobile-offline-boundary.md` | Online-first; LocalStore not authorized | Planning target / not a spec | No |
| `Docs/Architecture/platform-commercial-integration.md` | Platform contracts; D-P12-03 open | Planning target / not a spec | No |
| `Docs/Decisions/PLM-D-00-09-react-pwa-capacitor-client-strategy.md` | ADR: one React + PWA + Capacitor client | Accepted / Product Owner Approved | No |
| `Docs/Reports/PLM-01A-react-pwa-capacitor-architecture-decision.md` | PLM-01A evidence | Architecture decision complete | No |
| `Docs/Reports/PLM-CLIENT-GATE-B-react-client-scaffold.md` | Gate B React Client scaffold | Complete after validation | React foundation |
| `Docs/Reports/PLM-CLIENT-GATE-C-browser-pwa-foundation.md` | Gate C Browser + PWA foundation | Complete after validation | Online-first PWA |
| `Docs/Reports/PLM-CLIENT-GATE-D0-browser-auth-transport.md` | Gate D0 browser session auth transport | Complete after validation | Same-origin `/platform-api` + cookie policy |
| `Docs/Reports/PLM-CLIENT-GATE-D1-mobile-sign-in-session.md` | Gate D1 Sign In + session UI | Complete after validation | Cookie Sign In; Test User double-gated |
| `Docs/Reports/impl-gate-d2-account-lifecycle/` | Gate D2 screenshots | Complete after validation | No tokens/passwords in frames |

## Foundation closeout (PLM-00-WP10)

| Path | Purpose | Status | Implementation present |
|---|---|---|---|
| `Docs/Reports/PLM-00-foundation-closeout.md` | PLM-00 vision, gates, next phase | Planning closeout | No |
| `Docs/Validation/PLM-00-readiness-checklist.md` | Docs-only readiness gates | Planning closeout | No |

## Workspace indexes (PLM-00-WP01, updated in WP02–WP10)

| Path | Purpose | Status | Implementation present |
|---|---|---|---|
| `src/Products/PinoyLoanManager/` | Product workspace root | Foundation / Planning Only | No |
| `src/Products/PinoyLoanManager/Docs/` | Authoritative product documentation root (D-P12-02) | Foundation / Planning Only | No |
| `Docs/README.md` | Index to canonical documents | Foundation / Planning Only | No |
| `Docs/FILE-MANIFEST.md` | This navigation map | Foundation / Planning Only | No |
| `Docs/Product/README.md` | Index for product-policy docs | Foundation / Planning Only | No |
| `Docs/Architecture/README.md` | Index for architecture docs | Foundation / Planning Only | No |
| `Docs/Security/README.md` | Index for security docs | Foundation / Planning Only | No |
| `Docs/Decisions/README.md` | Index for ADRs | Foundation / Planning Only | No |
| `Docs/Phases/README.md` | Index for phase sequencing | Foundation / Planning Only | No |
| `Docs/Reports/README.md` | Index for WP evidence | Foundation / Planning Only | No |
| `Docs/Reports/PLM-01-product-scaffold-and-isolation.md` | PLM-01 scaffold evidence | Scaffold complete / no lending domain | Product shell |
| `Docs/Reports/PLM-D3-PRE-product-registration-self-access.md` | Product code + Local Validation fixture + current-session access API | PLM-D3-PRE complete; Gate D3 React not started | Platform prerequisite |
| `Docs/Validation/README.md` | Index for validation evidence | Foundation / Planning Only | No |
| `Docs/Operations/README.md` | Index for operations docs | Foundation / Planning Only | No |

---

## Implemented projects (PLM-01 shell)

| Item | Status |
|---|---|
| `ExItS.PinoyLoanManager.Domain` | Created — no lending entities |
| `ExItS.PinoyLoanManager.Application` | Created — no use cases |
| `ExItS.PinoyLoanManager.Infrastructure` | Created — no EF/Npgsql/DbContext |
| `ExItS.PinoyLoanManager.Api` | Created — `/health` only |
| `ExItS.PinoyLoanManager.ApiClient` | Created — marker only |
| `ExItS.PinoyLoanManager.Web` | Created — identity shell only; future host/BFF |
| `ExItS.PinoyLoanManager.Client` | Created — Gates B–D2 React + PWA + cookie Sign In + Personal account lifecycle; Gate D3 React not started |
| `tests/ExItS.PinoyLoanManager.UnitTests` | Created |
| `ExItS.slnx` PLM entries | Registered |

## Not present (intentionally)

| Item | Reason |
|---|---|
| `ExItS.PinoyLoanManager.Maui` | Preferred path superseded (PLM-D-00-09); not created |
| `ExItS.PinoyLoanManager.LocalStore` | Not justified until offline is authorized |
| Database / migration folders | Persistence not authorized (PLM-D-00-02 remains open) |
| Docker / deploy implementation | Not authorized |
| Platform catalog code `pinoy-loan-manager` | **FINAL / PRODUCT OWNER APPROVED** (PLM-D-00-01). Local Validation fixture is test-only. D-P12-03 remains open. |
| `Docs/deployment-notes.md` | Optional until packaging |
| Exact grant identifiers / custom roles | Open (PLM-D-00-06) |
| Small-org vs two-person high-risk approval | Open (PLM-D-00-13) |
| Exact rounding mode | Open (PLM-D-00-12) |
| Final calculation algorithms / peso or percent rates | Owner decision (PLM-D-00-08) |
| Legal/compliance validation | Open (PLM-D-00-11) |
