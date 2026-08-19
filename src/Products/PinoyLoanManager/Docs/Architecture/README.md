# Architecture

**Purpose:** Authoritative **HOW** — technical structure and product boundaries.
**Canonical document:** [../architecture.md](../architecture.md)
**Status:** Foundation / planning only
**Implementation present:** No

Do not treat this folder as a second architecture document.

| Doc | Subject |
|---|---|
| [application-surface-model.md](application-surface-model.md) | Platform Admin, Organization Web, MAUI field app, ExItS Personal |
| [loan-ledger-and-balance-model.md](loan-ledger-and-balance-model.md) | Operational Loan subledger and multi-component balances |
| [operational-subledger-and-accounting-boundary.md](operational-subledger-and-accounting-boundary.md) | Loan vs cash ledgers; PLM is not a complete GL |
| [personal-integration-boundary.md](personal-integration-boundary.md) | Personal vs PLM authority; no table access |
| [source-and-project-layout.md](source-and-project-layout.md) | Future `ExItS.PinoyLoanManager.*` projects (not created) |
| [api-and-contract-boundary.md](api-and-contract-boundary.md) | API consumers and Personal contracts |
| [persistence-and-database-boundary.md](persistence-and-database-boundary.md) | Separate database isolation; logical name `ExItS_PinoyLoanManager` |
| [mobile-offline-boundary.md](mobile-offline-boundary.md) | Online-first MAUI; offline not authorized |
| [platform-commercial-integration.md](platform-commercial-integration.md) | Platform commercial/identity contracts |

Future ADRs for irreversible structure belong in [../Decisions/](../Decisions/README.md). Layout **planning target** is recorded; projects are not created (PLM-D-00-03). Open linking and client-sharing questions: [../risks-and-decisions.md](../risks-and-decisions.md) (PLM-D-00-04, PLM-D-00-05, PLM-D-00-09). Money precision and allocation: [../Product/money-precision-and-rounding-policy.md](../Product/money-precision-and-rounding-policy.md), [../Product/payment-allocation-and-prepayment-policy.md](../Product/payment-allocation-and-prepayment-policy.md). Settlement and prepayment: [../Product/early-settlement-and-principal-prepayment-policy.md](../Product/early-settlement-and-principal-prepayment-policy.md). Accounting boundary: [operational-subledger-and-accounting-boundary.md](operational-subledger-and-accounting-boundary.md). Calendar and penalties: [../Product/schedule-and-collection-calendar-policy.md](../Product/schedule-and-collection-calendar-policy.md), [../Product/penalty-assessment-and-cap-policy.md](../Product/penalty-assessment-and-cap-policy.md). Authorization and cash-control planning: [../Security/role-and-grant-baseline.md](../Security/role-and-grant-baseline.md), [../Product/cashier-and-collector-control-model.md](../Product/cashier-and-collector-control-model.md), [../Product/cash-variance-and-session-close-policy.md](../Product/cash-variance-and-session-close-policy.md).

No .NET projects are authorized in PLM-00.
