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

**Phase 6** is **complete** with documented risks ([P6-WP06 closeout](docs/reports/P6-WP06-utang-mvp-closeout.md)). Utang MVP (customers, credit, repayments/ledger, due dates/overdue, statements/receipts, trial/continuity rules) is closed.

**Phase 7** is **in progress**. [P7-WP01](docs/reports/P7-WP01-sqlite-and-device-identity.md) delivered SQLite foundation + DeviceId. [P7-WP02](docs/reports/P7-WP02-offline-queue-and-idempotency.md) delivered encrypted offline queue + server idempotency. [P7-WP03](docs/reports/P7-WP03-customer-and-credit-sync.md) delivered encrypted customer/credit offline sync. [P7-WP04](docs/reports/P7-WP04-payment-sync-and-recovery.md) delivered encrypted payment offline sync (repayments, reversals, due dates; no offline statements/receipts). Production auth, POS operational roles, sales/inventory remain open.

Permanent Cursor rules live at `.cursor/rules/exits-workflow.mdc`. HealthCare remains frozen. Next: **P7-WP05 — Offline Closeout** when authorized.

### Platform database (local)

```powershell
docker run -d --name exits-platform-pg-test -e POSTGRES_PASSWORD=exits_platform_dev_only -e POSTGRES_DB=ExItS_Platform -p 5434:5432 postgres:18
dotnet ef database update --project src/Platform/ExItS.Platform.Infrastructure --startup-project src/Platform/ExItS.Platform.Api
dotnet run --project src/Platform/ExItS.Platform.Api --urls http://127.0.0.1:5288
```

Connection string key: `ConnectionStrings:PlatformDatabase` (see `appsettings.Development.json`). Do **not** auto-migrate at API startup.
