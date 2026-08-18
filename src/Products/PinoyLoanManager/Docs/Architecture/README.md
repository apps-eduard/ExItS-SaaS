# Architecture

**Purpose:** Authoritative **HOW** — technical structure and product boundaries.
**Canonical document:** [../architecture.md](../architecture.md)
**Status:** PLM-01 scaffold for layout; remaining architecture is planning
**Implementation present:** Product shell only

Do not treat this folder as a second architecture document.

| Doc | Subject |
|---|---|
| [application-surface-model.md](application-surface-model.md) | Platform Admin, Organization Web, MAUI field app, ExItS Personal |
| [loan-ledger-and-balance-model.md](loan-ledger-and-balance-model.md) | Operational Loan subledger and multi-component balances |
| [personal-integration-boundary.md](personal-integration-boundary.md) | Personal vs PLM authority; no table access |
| [source-and-project-layout.md](source-and-project-layout.md) | Physical `ExItS.PinoyLoanManager.*` layout; MAUI/LocalStore deferred |
| [api-and-contract-boundary.md](api-and-contract-boundary.md) | API consumers and Personal contracts |
| [persistence-and-database-boundary.md](persistence-and-database-boundary.md) | Separate database isolation |
| [mobile-offline-boundary.md](mobile-offline-boundary.md) | Online-first MAUI; offline not authorized |
| [platform-commercial-integration.md](platform-commercial-integration.md) | Platform commercial/identity contracts |

Future ADRs for irreversible structure belong in [../Decisions/](../Decisions/README.md). Physical layout is proven in PLM-01 (PLM-D-00-03 Closed); MAUI/LocalStore remain deferred. Open linking and client-sharing questions: [../risks-and-decisions.md](../risks-and-decisions.md) (PLM-D-00-04, PLM-D-00-05, PLM-D-00-09). Authorization and cash-control planning: [../Security/role-and-grant-baseline.md](../Security/role-and-grant-baseline.md), [../Product/cashier-and-collector-control-model.md](../Product/cashier-and-collector-control-model.md).
