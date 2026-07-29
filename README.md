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
- [HealthCare reuse assessment](docs/reuse/healthcare-reuse-assessment.md)
- [UI system and reusable components](docs/engineering/ui-design-system.md)
- [All phases](docs/phases/README.md)
- [First Cursor command](docs/cursor/first-cursor-command.md)

## Repository intent

```text
ExITS-SaaS/
├── HealthCare SaaS/            # copied completed MVP; exact existing name is discovered by Cursor
├── Platform/                   # created only after assessment and approved extraction plan
├── Products/
│   └── PinoyBusinessPOS/       # created after platform boundary is approved
├── Shared/                     # only genuinely cross-product code
├── docs/
└── README.md
```

The first Cursor task is assessment-only. It must not move, rename, or refactor the completed HealthCare MVP before the reuse boundary is documented and approved.
