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
├── ExItS.slnx                  # root Platform solution (P2-WP01)
├── global.json                 # SDK 10.0.302
├── src/Platform/               # Domain, Application, Infrastructure, Api
├── tests/                      # Unit + architecture/safety tests
├── docs/                       # portfolio architecture and tracking
├── HealthCare/                 # nested independent Git repo — ignored by root
└── README.md
```

`HealthCare/` remains frozen and outside root Git. Do not import it without an approved work package.

**Phase 3** is closed with documented risks ([P3-WP05 closeout](docs/reports/P3-WP05-billing-closeout.md)): catalog, subscriptions, manual SaaS payments, and entitlement snapshots persist under `/api/v1/platform/...` (development-stage, unauthenticated). No product delivery, gateways, or Admin UI. HealthCare remains frozen. Next: Phase 4 / P4-WP01 when authorized.

### Platform database (local)

```powershell
docker run -d --name exits-platform-pg-test -e POSTGRES_PASSWORD=exits_platform_dev_only -e POSTGRES_DB=ExItS_Platform -p 5434:5432 postgres:18
dotnet ef database update --project src/Platform/ExItS.Platform.Infrastructure --startup-project src/Platform/ExItS.Platform.Api
dotnet run --project src/Platform/ExItS.Platform.Api --urls http://127.0.0.1:5288
```

Connection string key: `ConnectionStrings:PlatformDatabase` (see `appsettings.Development.json`). Do **not** auto-migrate at API startup.
