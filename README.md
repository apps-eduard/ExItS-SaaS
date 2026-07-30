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

**Phase 7** is **complete** with documented risks ([P7-WP05 closeout](docs/reports/P7-WP05-offline-closeout.md)). Offline subsystem (DeviceId/SQLite isolation, encrypted queue + idempotency, customer/credit/payment sync, recovery/UX closeout) is closed.

**Phase 8** is **complete** with documented risks ([P8-WP07 closeout](docs/reports/P8-WP07-basic-store-closeout.md)). Online-only Basic Store MVP is closed.

**Phase 9** is **in progress** — P9-WP01 Security and Privacy Hardening and P9-WP02 Performance and Reliability are **complete** with documented risks ([P9-WP01](docs/reports/P9-WP01-security-and-privacy-hardening.md), [P9-WP02](docs/reports/P9-WP02-performance-and-reliability.md)). **Not production-ready** while R-091, R-109, R-129, Production HTTPS/MAUI cleartext replacement, and related blockers remain open. Next authorized WP: **P9-WP03 — Backup and Restore** (do not begin until authorized).

Permanent Cursor rules live at `.cursor/rules/exits-workflow.mdc`. HealthCare remains frozen. Next: **P9-WP02 — Performance and Reliability** when authorized.

### Platform database (local)

```powershell
docker run -d --name exits-platform-pg-test -e POSTGRES_PASSWORD=exits_platform_dev_only -e POSTGRES_DB=ExItS_Platform -p 5434:5432 postgres:18
dotnet ef database update --project src/Platform/ExItS.Platform.Infrastructure --startup-project src/Platform/ExItS.Platform.Api
dotnet run --project src/Platform/ExItS.Platform.Api --urls http://127.0.0.1:5288
```

Connection string key: `ConnectionStrings:PlatformDatabase` (see `appsettings.Development.json`). Do **not** auto-migrate at API startup.
