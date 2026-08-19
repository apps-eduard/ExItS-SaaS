# Final Portfolio Boundaries

[Home](../index.md) | [Phase 1 architecture approval](../reports/phase-01-architecture-approval.md) | [Approved summary](approved-architecture-summary.md) | [Capability boundary](platform-product-capability-boundary.md) | [Contracts](platform-product-contracts.md) | [Data ownership](data-ownership.md)

The ExItS portfolio consists of the Platform and PinoyBusinessPOS. Platform owns commercial authority and shared identity; PinoyBusinessPOS owns retail operations. Boundaries use versioned contracts and independently deployable databases.

| Capability | Platform | PinoyBusinessPOS | Shared contract / rule |
|---|---|---|---|
| Identity and users | Own | Consume trusted identity | Stable user identifiers and auth claims |
| Organizations and memberships | Own | Project organization access | Stable organization and membership identifiers |
| Product access | Own | Enforce before product roles | Commercial access projection |
| Product-operational roles | No | Own | Product-local permission catalog |
| Catalog, plans, trials | Own commercial catalog | Own retail catalog | Product/plan identifiers only |
| Subscriptions and SaaS payments | Own | Consume status | Subscription projection |
| Entitlements and overrides | Own and publish | Enforce local snapshot | Versioned entitlement snapshot |
| Stores, branches, registers | No | Own | None |
| Customers and Utang | No | Own | No Platform operational payload |
| Sales, inventory, expenses | No | Own | No cross-database access |
| Suppliers and purchasing | No | Own | Product-owned |
| Shifts, returns, reports | No | Own | Product-owned |
| Offline database and sync | No | Own | Idempotency and contract rules |
| Audit | Platform authority and administration | Retail operational audit | Correlation identifiers, not shared entities |
| Platform Admin UI | Own; Ant Design Blazor | No | No Tailwind or Fluent UI |
| POS UI | No | Own; native CSS / DesignSystem on **current MAUI** | Shared token semantics only. Future React/PWA/Capacitor Mobile Client planning: [docs/Mobile-React](../Mobile-React/README.md) (not authorized; does not change this row today) |

## Entitlement behavior

Product APIs use validated local entitlement snapshots with versions, effective timestamps, refresh/expiry policy, fail-safe behavior, grace periods, and audit trails. Ordinary product operations must not synchronously depend on Platform availability.

## Identity and data rules

- Platform identity and organization identifiers are the cross-product correlation keys.
- Product databases may store controlled projections, never Platform system-of-record entities.
- No cross-product database access, cross-database foreign keys, shared `DbContext`, or shared operational entities.
- Product operational and sensitive payloads do not enter Platform commercial contracts.
- Contract consumers are idempotent and tolerate at-least-once delivery.
