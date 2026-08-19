# Pinoy Loan Manager — File Manifest

**Status:** Foundation / planning only
**Implementation present:** No
**Current work package:** PLM-DOC-04 — Early Settlement, Refunds, Reversals, Cash Variance & Accounting Boundaries

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
| `Docs/Architecture/application-surface-model.md` | Platform Admin, Org Web, MAUI, Personal | Agreed direction / not a spec | No |

## Financial / lifecycle planning (PLM-00-WP04)

| Path | Purpose | Status | Implementation present |
|---|---|---|---|
| `Docs/Product/financial-calculation-baseline.md` | Money terms; pointer to PLM-DOC-02 policies | Planning baseline / not a spec | No |
| `Docs/Product/interest-and-finance-charge-policy.md` | MVP methods, formulas, treatments | Accepted product rules (PLM-DOC-02) | No |
| `Docs/Product/fees-and-net-proceeds-policy.md` | Fee bases/treatments; Net Proceeds | Accepted product rules (PLM-DOC-02) | No |
| `Docs/Product/payment-allocation-and-prepayment-policy.md` | Allocation, advance, overpayment | Accepted product rules (PLM-DOC-02) | No |
| `Docs/Product/early-settlement-and-principal-prepayment-policy.md` | Settlement Quote, rebate, principal prepayment | Accepted product rules (PLM-DOC-04) | No |
| `Docs/Product/money-precision-and-rounding-policy.md` | Decimal money; To Even; reconciliation | Accepted product rules (PLM-DOC-02) | No |
| `Docs/Product/payment-and-allocation-model.md` | Payments, posting notes, reversals, idempotency | Planning baseline / not a spec | No |
| `Docs/Product/schedule-maturity-and-settlement.md` | Schedule, calendar, maturity, settlement (index) | Planning baseline / not a spec | No |
| `Docs/Product/schedule-and-collection-calendar-policy.md` | Frequencies, calendar, first due, exceptions | Accepted product rules (PLM-DOC-03) | No |
| `Docs/Product/delinquency-and-missed-payment-policy.md` | Past Due, DPD, missed-day counter, grace | Accepted product rules (PLM-DOC-03) | No |
| `Docs/Product/penalty-assessment-and-cap-policy.md` | Tiers, bases, caps, waiver vs reversal | Accepted product rules (PLM-DOC-03) | No |
| `Docs/Product/maturity-and-post-maturity-policy.md` | Maturity and post-maturity modes | Accepted product rules (PLM-DOC-03) | No |
| `Docs/Product/loan-lifecycle-model.md` | Origination vs lifecycle vs delinquency | Planning baseline / not a spec | No |
| `Docs/Architecture/loan-ledger-and-balance-model.md` | Operational subledger and balances | Planning baseline / not a spec | No |
| `Docs/Architecture/operational-subledger-and-accounting-boundary.md` | Loan vs cash ledgers; not a complete GL | Accepted architecture policy (PLM-DOC-04) | No |

## Authorization / cash / operational workflow (PLM-00-WP05)

| Path | Purpose | Status | Implementation present |
|---|---|---|---|
| `Docs/Security/role-and-grant-baseline.md` | Presets, grant catalog intent, scope, SoD | Planning baseline / not a spec | No |
| `Docs/Product/daily-operational-workflow.md` | Common operating day, assignments, offline boundary | Planning baseline / not a spec | No |
| `Docs/Product/cashier-and-collector-control-model.md` | Cashier Session, float, remittance, cash availability | Planning baseline / not a spec | No |
| `Docs/Product/disbursement-and-payment-controls.md` | Office/field disbursement and cash payment | Planning baseline / not a spec | No |
| `Docs/Product/exception-reversal-and-variance-workflow.md` | Exceptions, waivers, reversals vs cash refund, variance | Planning baseline / not a spec | No |
| `Docs/Product/reversal-refund-and-correction-policy.md` | Payment reversal, Refund Payable, cash refund | Accepted product rules (PLM-DOC-04) | No |
| `Docs/Product/cash-variance-and-session-close-policy.md` | Expected vs actual; close-with-variance | Accepted product rules (PLM-DOC-04) | No |
| `Docs/Product/disbursement-cancellation-and-reversal-policy.md` | Cancel before release; reverse after recovery | Accepted product rules (PLM-DOC-04) | No |

## Borrower / Personal / publishing (PLM-00-WP06)

| Path | Purpose | Status | Implementation present |
|---|---|---|---|
| `Docs/Product/borrower-model.md` | PLM-owned Borrower; independent of POS/Personal | Planning baseline / not a spec | No |
| `Docs/Product/borrower-identity-and-duplicate-policy.md` | Ownership, cardinality, duplicate handling | Accepted product rules (PLM-DOC-01) | No |
| `Docs/Product/personal-borrower-linking.md` | Optional consent-based linking and unlink | Planning baseline / not a spec | No |
| `Docs/Product/personal-linking-lifecycle-and-visibility.md` | Link lifecycle, MVP flow, unlink/relink, visibility | Accepted product rules (PLM-DOC-01) | No |
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
| `Docs/Architecture/source-and-project-layout.md` | Future project tree; not created | Planning target / not a spec | No |
| `Docs/Architecture/api-and-contract-boundary.md` | API consumers; Personal contracts | Planning target / not a spec | No |
| `Docs/Architecture/persistence-and-database-boundary.md` | Separate DB isolation | Planning target / not a spec | No |
| `Docs/Architecture/mobile-offline-boundary.md` | Online-first MAUI; offline not authorized | Planning target / not a spec | No |
| `Docs/Architecture/platform-commercial-integration.md` | Platform contracts; D-P12-03 open | Planning target / not a spec | No |

## Foundation closeout (PLM-00-WP10)

| Path | Purpose | Status | Implementation present |
|---|---|---|---|
| `Docs/Reports/PLM-00-foundation-closeout.md` | PLM-00 vision, gates, next phase | Planning closeout | No |
| `Docs/Reports/PLM-DOC-01-product-identity-and-personal-linking.md` | PLM-DOC-01 identity and Personal linking | Documentation closeout | No |
| `Docs/Reports/PLM-DOC-02-financial-calculation-and-allocation.md` | PLM-DOC-02 calculation, fees, rounding, allocation | Documentation closeout | No |
| `Docs/Reports/PLM-DOC-03-schedule-delinquency-penalty-and-maturity.md` | PLM-DOC-03 calendar, delinquency, penalty, maturity | Documentation closeout | No |
| `Docs/Reports/PLM-DOC-04-settlement-reversals-variance-and-accounting.md` | PLM-DOC-04 settlement, reversals, variance, accounting | Documentation closeout | No |
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
| `Docs/Decisions/ADR-001-product-identity-and-database-name.md` | Product code and logical database name | Accepted (PLM-DOC-01) | No |
| `Docs/Decisions/ADR-002-borrower-personal-cardinality-and-consent.md` | Borrower/Personal cardinality and consent | Accepted product behavior (PLM-DOC-01) | No |
| `Docs/Decisions/ADR-003-supported-interest-and-schedule-methods.md` | MVP interest/schedule methods | Accepted product policy (PLM-DOC-02) | No |
| `Docs/Decisions/ADR-004-rounding-fees-and-payment-allocation.md` | Rounding, fees, allocation | Accepted; PLM-D-00-12 Closed | No |
| `Docs/Decisions/ADR-005-schedule-calendar-and-exception-treatment.md` | Calendar, frequencies, exception defaults | Accepted product policy (PLM-DOC-03) | No |
| `Docs/Decisions/ADR-006-delinquency-penalty-and-maturity-policy.md` | DPD, penalties, maturity | Accepted product policy (PLM-DOC-03) | No |
| `Docs/Decisions/ADR-007-early-settlement-and-prepayment-policy.md` | Early settlement and principal prepayment | Accepted product policy (PLM-DOC-04) | No |
| `Docs/Decisions/ADR-008-reversals-refunds-variance-and-accounting-boundary.md` | Reversals, refunds, variance, GL boundary | Accepted; **PLM-D-00-13 Closed** | No |
| `Docs/Phases/README.md` | Index for phase sequencing | Foundation / Planning Only | No |
| `Docs/Reports/README.md` | Index for WP evidence | Foundation / Planning Only | No |
| `Docs/Validation/README.md` | Index for validation evidence | Foundation / Planning Only | No |
| `Docs/Operations/README.md` | Index for operations docs | Foundation / Planning Only | No |

---

## Not present (intentionally)

| Item | Reason |
|---|---|
| `ExItS.PinoyLoanManager.Domain` (and other .NET projects) | Code projects not authorized |
| Test projects | Not authorized |
| Database / migration folders | Persistence not authorized |
| Docker / deploy implementation | Not authorized |
| `ExItS.slnx` entries | Not authorized |
| `Docs/deployment-notes.md` | Optional until packaging |
| `Docs/Reports/<WP-id>.md` | In-tree WP report not required except PLM-00 closeout |
| Exact grant identifiers / custom roles | Open (PLM-D-00-06) |
| Small-org vs two-person high-risk approval | **Closed** (PLM-D-00-13) — maker/checker + controlled Owner Override |
| Default interest rates / fee amounts / penalty amounts | Not defined; never invent |
| Penalty rates/amounts, grace `N`, caps as numbers | Engine accepted; no defaults (PLM-DOC-03) |
| Write-off/recovery accounting, restructuring, journal/export | Open (PLM-D-00-07 / PLM-D-00-08 remainder) |
| Legal/compliance validation | Open (PLM-D-00-11) |
