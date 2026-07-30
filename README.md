# ExITS SaaS

ExITS SaaS is a multi-product SaaS ecosystem.

Initial products:

- **HealthCare SaaS** — completed MVP already present in this repository and the primary source for reusable platform capabilities.
- **PinoyBusinessPOS** — new offline-capable SaaS for Sari-Sari Stores and Mini Groceries.

The shared **ExITS Platform** will manage global identity, organizations, product subscriptions, plans, trials, payments, product entitlements, platform administrators, and platform-wide audit/support operations.

Product-specific workflows and data remain inside each product.

## Start here

- [Documentation home](docs/index.md)
- [Portfolio progress dashboard](docs/portfolio-progress.md)
- [Approved architecture summary](docs/engineering/approved-architecture-summary.md)
- [Root Platform solution](ExItS.slnx) (`dotnet restore/build/test ExItS.slnx`)
- [All phases](docs/phases/README.md)

## Repository layout (current)

```text
ExItS-SaaS/
├── ExItS.slnx                  # root Platform + POS foundation solution
├── global.json                 # SDK 10.0.302
├── src/Platform/               # Domain, Application, Infrastructure, Api, Admin
├── src/Shared/                 # ExItS.DesignSystem
├── src/Products/PinoyBusinessPOS/  # Domain, Application, Infrastructure, Api, ApiClient, Maui (Android-first)
├── tests/                      # Unit + architecture + Admin + DesignSystem + POS tests
├── docs/                       # portfolio architecture and tracking
├── HealthCare/                 # nested independent Git repo — ignored by root
└── README.md
```

`HealthCare/` remains frozen and outside root Git. Do not import it without an approved work package.

**Phase 6** is **in progress**. [P6-WP05 — Statements, Receipts and Trial Rules](docs/reports/P6-WP05-statements-receipts-and-trial-rules.md) is complete (`271c518cb8c4051502d6370ec71e6498fbbfd6b5`) after [P6-WP04 due dates/overdue](docs/reports/P6-WP04-due-dates-and-overdue-monitoring.md), [P6-WP03 payments/ledger](docs/reports/P6-WP03-payments-and-ledger.md), [P6-WP02 credit](docs/reports/P6-WP02-remarks-based-credit.md), and [P6-WP01 customers](docs/reports/P6-WP01-customers.md). Production auth, POS operational roles, sales/inventory, and offline sync remain open. Permanent Cursor rules live at `.cursor/rules/exits-workflow.mdc`. HealthCare remains frozen. Next: **P6-WP06 — Utang MVP Closeout** when authorized.

### Platform database (local)

```powershell
docker run -d --name exits-platform-pg-test -e POSTGRES_PASSWORD=exits_platform_dev_only -e POSTGRES_DB=ExItS_Platform -p 5434:5432 postgres:18
dotnet ef database update --project src/Platform/ExItS.Platform.Infrastructure --startup-project src/Platform/ExItS.Platform.Api
dotnet run --project src/Platform/ExItS.Platform.Api --urls http://127.0.0.1:5288
```

Connection string key: `ConnectionStrings:PlatformDatabase` (see `appsettings.Development.json`). Do **not** auto-migrate at API startup.
