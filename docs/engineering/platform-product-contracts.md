# Platform–Product Contracts

[Architecture](architecture.md) | [Data Ownership](data-ownership.md)

## Platform provides

- Stable global UserId
- PlatformOrganizationId
- Product access status
- Plan and entitlement snapshot
- Subscription lifecycle events
- Account suspension state

## Product provides

- Product tenant/profile linked to PlatformOrganizationId
- Product roles, permissions and assignments
- Product-specific audit and operational data
- Local entitlement projection

## Token guidance

Tokens contain identity, organization context and coarse product access. Detailed product permissions remain in the product to avoid large or stale tokens.

## Contract requirements

- Versioned DTOs/events
- Idempotent entitlement updates
- Correlation IDs
- Retry-safe consumers
- Contract tests
- Explicit compatibility and deprecation policy
