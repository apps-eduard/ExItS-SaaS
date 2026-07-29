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

- Existing HealthCare UI: keep Ant Design Blazor unless a separate approved migration exists.
- ExITS Platform Admin: keep/adapt existing Ant Design Blazor to reduce rewrite risk.
- PinoyBusinessPOS: native CSS, CSS isolation and product-specific reusable Razor components.
- Shared layer: models, validation, formatting, localization keys, design-token semantics and contracts; not one framework-dependent component library.

## Product availability

Product APIs cache validated entitlement snapshots. Daily HealthCare/POS operations do not synchronously call Platform for every request.
