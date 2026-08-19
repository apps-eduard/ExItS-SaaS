# Architecture

**Purpose:** Authoritative **HOW** — technical structure and product boundaries.
**Canonical document:** [../architecture.md](../architecture.md)
**Status:** PLM-01 scaffold; PLM-01A approved; Gates B–C Client/PWA present
**Implementation present:** Product shell + React Client + online-first PWA — no lending/auth/Capacitor

Do not treat this folder as a second architecture document.

| Doc | Subject |
|---|---|
| [application-surface-model.md](application-surface-model.md) | Platform Admin, Organization Web/PWA, Capacitor Android, ExItS Personal |
| [react-pwa-capacitor-client.md](react-pwa-capacitor-client.md) | Shared React + PWA + Capacitor architecture (PLM-D-00-09) |
| [loan-ledger-and-balance-model.md](loan-ledger-and-balance-model.md) | Operational Loan subledger and multi-component balances |
| [personal-integration-boundary.md](personal-integration-boundary.md) | Personal vs PLM authority; no table access |
| [source-and-project-layout.md](source-and-project-layout.md) | Physical `ExItS.PinoyLoanManager.*` layout; Client future; LocalStore deferred |
| [api-and-contract-boundary.md](api-and-contract-boundary.md) | API consumers and Personal contracts |
| [persistence-and-database-boundary.md](persistence-and-database-boundary.md) | Separate database isolation |
| [mobile-offline-boundary.md](mobile-offline-boundary.md) | Online-first; LocalStore not authorized |
| [platform-commercial-integration.md](platform-commercial-integration.md) | Platform commercial/identity contracts |

Physical layout is proven in PLM-01 (PLM-D-00-03 Closed). Client strategy is approved in PLM-01A (PLM-D-00-09 Closed). LocalStore remains deferred. Open linking questions: [../risks-and-decisions.md](../risks-and-decisions.md) (PLM-D-00-04, PLM-D-00-05). Authorization and cash-control planning: [../Security/role-and-grant-baseline.md](../Security/role-and-grant-baseline.md), [../Product/cashier-and-collector-control-model.md](../Product/cashier-and-collector-control-model.md).
