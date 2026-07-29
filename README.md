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
