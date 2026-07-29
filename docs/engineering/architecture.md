# Target Architecture

[Home](../index.md) | [Dashboard](../portfolio-progress.md)

## Portfolio architecture

```text
                         ExITS Platform
        Identity · Organizations · Products · Billing · Entitlements
                              │
              versioned contracts / events / snapshots
                 ┌────────────┴────────────┐
                 │                         │
        HealthCare Product        PinoyBusinessPOS Product
        API + Web/Mobile          API + MAUI Blazor Hybrid
                 │                         │
        HealthCare PostgreSQL      POS PostgreSQL + device SQLite
```

## Repository direction

A controlled monorepo is used initially for coordinated extraction and development. Product boundaries remain deployable independently.

## UI choices

- Existing HealthCare Staff Web: keep Ant Design Blazor (no rewrite in current ExITS MVP work).
- Existing HealthCare PatientWeb / MAUI: keep their current native implementations.
- **New ExITS Platform Admin:** Blazor Web App with **native CSS** / CSS isolation / Razor components — **no Ant Design**, **no Tailwind**.
- PinoyBusinessPOS: same native CSS foundation (MAUI Blazor Hybrid); shared token/localization/model conventions with Platform Admin.
- Shared layer: models, validation, formatting, localization keys, design-token semantics and contracts; not one framework-dependent component library.

## Product availability

Product APIs cache validated entitlement snapshots. Daily HealthCare/POS operations do not synchronously call Platform for every request.
