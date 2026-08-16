# Target Architecture

[Home](../index.md) | [Dashboard](../portfolio-progress.md) | [Approved summary](approved-architecture-summary.md) | [Capability boundary](platform-product-capability-boundary.md) | [Contracts](platform-product-contracts.md) | [Extraction sequence](../reuse/extraction-sequence.md) | [ADR-011](../decisions/ADR-011-platform-authority-and-product-local-projections.md) | [ADR-012](../decisions/ADR-012-versioned-platform-contracts-and-local-projections.md) | [ADR-014](../decisions/ADR-014-approve-exits-portfolio-architecture-for-controlled-implementation.md)

## Portfolio architecture

```text
                         ExITS Platform
        Identity · Organizations · Products · Billing · Entitlements
                              │
              versioned contracts / events / snapshots
                              │
                    PinoyBusinessPOS Product
                    API + MAUI Blazor Hybrid
                              │
                    POS PostgreSQL (`ExItS_PinoyBusinessPOS`, schema `pos` + idempotency_records)
                    + device SQLite foundation/outbox (P7-WP01/P7-WP02; business cache deferred)
```

Active portfolio products: **Platform** + **PinoyBusinessPOS** only. Do not nest or reconnect an external foreign product tree in this workspace.

## Repository direction

A controlled monorepo coordinates Platform and PinoyBusinessPOS development. Product boundaries remain deployable independently. Sequence history: [extraction-sequence.md](../reuse/extraction-sequence.md).

**P2-WP01:** root `ExItS.slnx` + `src/Platform/{Domain,Application,Infrastructure,Api}` + architecture/unit tests exist. Dependency direction enforced by tests. No legacy product project references.

**P2-WP02:** Domain identity/organization boundary (`PlatformUser`, `PlatformOrganization`, `OrganizationMembership`, `ProductCode`) plus Application contracts/use cases. No persistence, authentication, or business API routes.

**P2-WP03:** Commercial catalog and entitlement foundation (`Product`, `Plan`, `PlanVersion`, `TrialDefinition`, `Subscription`, `FeatureOverride`, `EntitlementSnapshot`, composer). No persistence, payments, or business API routes.

**P2-WP04:** Platform-side product contract adaptation (envelopes, projections, apply policy). No legacy product Integration folder; no transport/persistence in that WP.

**P2-WP05:** Migration dry-run / regression validation models in Application (`MigrationValidation`). Simulation only — product-agnostic preflight; no real migration or SQL cutover.

**P3-WP01:** Platform catalog persistence (`PlatformDbContext`, schema `platform`) + `/api/v1/platform/catalog` API. Catalog endpoints are unauthenticated (development-stage).

**P3-WP02:** Platform organization + subscription persistence; lifecycle API under `/api/v1/platform/organizations` and `/subscriptions`. Paid activation is commercial-only (no payment collection). Payments/Admin/entitlement delivery not implemented. Routes remain unauthenticated (development-stage).

## UI choices

- **ExITS Platform Admin:** Blazor Web App with **Ant Design Blazor** (ADR-015) — **no Tailwind**, **no Fluent UI**.
- **PinoyBusinessPOS:** native CSS / DesignSystem (MAUI Blazor Hybrid); shared token/localization/model conventions where applicable.
- Shared layer: models, validation, formatting, localization keys, design-token semantics and contracts; not one framework-dependent component library across Admin and POS.

## Product availability

Product APIs cache validated entitlement snapshots. Daily POS operations do not synchronously call Platform for every request. Authority: [ADR-011](../decisions/ADR-011-platform-authority-and-product-local-projections.md). Contract mechanics and projection states: [ADR-012](../decisions/ADR-012-versioned-platform-contracts-and-local-projections.md), [platform-product-contracts.md](platform-product-contracts.md), [entitlement-state-matrix.md](entitlement-state-matrix.md).
