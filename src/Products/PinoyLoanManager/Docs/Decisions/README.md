# Decisions

**Purpose:** Architecture Decision Records (ADRs) explaining important architectural or business choices and **WHY**.
**Canonical register:** [../risks-and-decisions.md](../risks-and-decisions.md)
**Status:** Foundation / planning only
**Implementation present:** No

PLM-00-WP03 through WP10 record agreed **product direction** in Product / Architecture / Security docs; ADRs close irreversible identity and linking choices where explicitly approved.

## Approved ADRs (PLM-DOC-01)

| ADR | Subject | Decisions closed |
|---|---|---|
| [ADR-001-product-identity-and-database-name.md](ADR-001-product-identity-and-database-name.md) | Product code and logical database name | PLM-D-00-01; PLM-D-00-02 (logical name only) |
| [ADR-002-borrower-personal-cardinality-and-consent.md](ADR-002-borrower-personal-cardinality-and-consent.md) | Borrower/Personal cardinality, MVP linking, consent | Product behavior accepted; contract requirements closed in PLM-DOC-10 (**PLM-D-00-05 Closed**); PLM-D-00-04, PLM-D-00-11 remain open |

## Approved ADRs (PLM-DOC-02)

| ADR | Subject | Decisions closed |
|---|---|---|
| [ADR-003-supported-interest-and-schedule-methods.md](ADR-003-supported-interest-and-schedule-methods.md) | MVP interest/schedule methods | Product methods accepted; **PLM-D-00-08 Closed for MVP** |
| [ADR-004-rounding-fees-and-payment-allocation.md](ADR-004-rounding-fees-and-payment-allocation.md) | Rounding, fees, allocation | **PLM-D-00-12 Closed**; **PLM-D-00-07 / PLM-D-00-08 Closed for MVP** |

## Approved ADRs (PLM-DOC-03)

| ADR | Subject | Decisions closed |
|---|---|---|
| [ADR-005-schedule-calendar-and-exception-treatment.md](ADR-005-schedule-calendar-and-exception-treatment.md) | Calendar, frequencies, exception defaults | Product calendar accepted; **PLM-D-00-08 Closed for MVP** |
| [ADR-006-delinquency-penalty-and-maturity-policy.md](ADR-006-delinquency-penalty-and-maturity-policy.md) | DPD, penalties, maturity | Product engine accepted; no default amounts; PLM-D-00-11 Open |

## Approved ADRs (PLM-DOC-04)

| ADR | Subject | Decisions closed |
|---|---|---|
| [ADR-007-early-settlement-and-prepayment-policy.md](ADR-007-early-settlement-and-prepayment-policy.md) | Settlement Quote, rebate, principal prepayment | Product settlement/prepayment accepted; **PLM-D-00-08 Closed for MVP**; PLM-D-00-11 Open |
| [ADR-008-reversals-refunds-variance-and-accounting-boundary.md](ADR-008-reversals-refunds-variance-and-accounting-boundary.md) | Reversals, refunds, variance, GL boundary | **PLM-D-00-13 Closed**; **PLM-D-00-07 Closed for MVP financial model** |

## Approved ADRs (PLM-DOC-07)

| ADR | Subject |
|---|---|
| [ADR-013-borrower-onboarding-and-application-minimums.md](ADR-013-borrower-onboarding-and-application-minimums.md) | Borrower and application minimums |
| [ADR-014-assessment-approval-and-disbursement-readiness.md](ADR-014-assessment-approval-and-disbursement-readiness.md) | Assessment, approval, readiness |

## Approved ADRs (PLM-DOC-08)

| ADR | Subject | Decisions closed |
|---|---|---|
| [ADR-015-documents-receipts-and-reporting-policy.md](ADR-015-documents-receipts-and-reporting-policy.md) | Documents, receipts, reporting | Document catalog, receipt identity, KPI/PAR/aging formulas |
| [ADR-016-notification-privacy-retention-and-audit-policy.md](ADR-016-notification-privacy-retention-and-audit-policy.md) | Notification, privacy, retention, audit | Notification direction, classification, retention architecture, audit coverage |

## Approved ADRs (PLM-DOC-09)

| ADR | Subject | Decisions closed |
|---|---|---|
| [ADR-017-mobile-offline-route-and-device-policy.md](ADR-017-mobile-offline-route-and-device-policy.md) | Mobile, offline, route, device | MVP online authority; cache/drafts; deferred offline posting; routes; GPS; device requirements |
| [ADR-018-branch-treasury-float-and-ui-sharing-policy.md](ADR-018-branch-treasury-float-and-ui-sharing-policy.md) | Branch treasury, float ack, UI sharing | **PLM-D-00-09 Closed** |

## Approved ADRs (PLM-DOC-10)

| ADR | Subject | Decisions closed |
|---|---|---|
| [ADR-019-platform-personal-contract-requirements.md](ADR-019-platform-personal-contract-requirements.md) | Platform access, Personal link, Personal-facing API contracts | **PLM-D-00-05 Closed for PLM behavior/contract**; PLM-D-00-04, D-P12-03 remain open |

| [ADR-020-usage-metering-and-tenant-placement-contracts.md](ADR-020-usage-metering-and-tenant-placement-contracts.md) | Usage metering and tenant placement contracts | Contract requirements accepted; pre-release cancellation emits no usage event; D-P12-03 transport open |

Major future irreversible or cross-product choices must receive an ADR here when explicitly approved.

Do not close PLM-D-00-04, PLM-D-00-11, or D-P12-03 without explicit approval. **R-091 Closed for Phase 13 scope.** **D-P12-05 Closed / satisfied for authentication honesty.** **PLM-D-00-03/05/06/07/08/09 Closed** as documented in [PLM-decision-status-summary.md](PLM-decision-status-summary.md). PLM-D-00-13 is **Closed**.
