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
| [mobile-offline-boundary.md](mobile-offline-boundary.md) | Online-first MAUI; offline posting deferred |
| [mobile-and-offline-operating-model.md](mobile-and-offline-operating-model.md) | MAUI purpose; MVP authority; cache/drafts |
| [web-maui-component-sharing-policy.md](web-maui-component-sharing-policy.md) | Web/MAUI sharing; **PLM-D-00-09 Closed** |
| [platform-commercial-integration.md](platform-commercial-integration.md) | Platform commercial/identity contracts index |
| [platform-access-context-contract.md](platform-access-context-contract.md) | Required Platform access context facts |
| [personal-link-and-consent-contract.md](personal-link-and-consent-contract.md) | Personal link/consent contract (**PLM-D-00-05 Closed**) |
| [personal-facing-loan-api-contract.md](personal-facing-loan-api-contract.md) | Personal-facing loan API operations |
| [platform-usage-metering-contract.md](platform-usage-metering-contract.md) | Usage metering; LOAN DISBURSED |
| [tenant-placement-and-routing-contract.md](tenant-placement-and-routing-contract.md) | Tenant placement and routing abstraction |

Future ADRs for irreversible structure belong in [../Decisions/](../Decisions/README.md). Layout **planning target** is recorded; projects are not created (PLM-D-00-03). Generic Platform relationship model **PLM-D-00-04 Open**; Personal linking contract **PLM-D-00-05 Closed**; transport **D-P12-03 Open**. **PLM-D-00-09 Closed** — [web-maui-component-sharing-policy.md](web-maui-component-sharing-policy.md). Mobile/offline: [mobile-and-offline-operating-model.md](mobile-and-offline-operating-model.md). Money precision and allocation: [../Product/money-precision-and-rounding-policy.md](../Product/money-precision-and-rounding-policy.md), [../Product/payment-allocation-and-prepayment-policy.md](../Product/payment-allocation-and-prepayment-policy.md). Settlement and prepayment: [../Product/early-settlement-and-principal-prepayment-policy.md](../Product/early-settlement-and-principal-prepayment-policy.md). Accounting boundary: [operational-subledger-and-accounting-boundary.md](operational-subledger-and-accounting-boundary.md). Calendar and penalties: [../Product/schedule-and-collection-calendar-policy.md](../Product/schedule-and-collection-calendar-policy.md), [../Product/penalty-assessment-and-cap-policy.md](../Product/penalty-assessment-and-cap-policy.md). Authorization and cash-control planning: [../Security/authorization-grant-catalog.md](../Security/authorization-grant-catalog.md), [../Product/workflow-authorization-policy.md](../Product/workflow-authorization-policy.md), [../Product/cashier-and-collector-control-model.md](../Product/cashier-and-collector-control-model.md), [../Product/cash-variance-and-session-close-policy.md](../Product/cash-variance-and-session-close-policy.md).

No .NET projects are authorized in PLM-00.
