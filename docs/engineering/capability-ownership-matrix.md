# Capability Ownership Matrix

[Capability boundary](platform-product-capability-boundary.md) | [Data authority](data-authority-matrix.md) | [Contracts](platform-product-contracts.md) | [Contract matrix](platform-product-contract-matrix.md)

Primary ownership only. A projection is not a system of record.

| Capability | System of Record | Platform Responsibility | POS Responsibility | Shared Contract | Prohibited Coupling |
|---|---|---|---|---|---|
| User identity and credentials | Platform | Authenticate, suspend, verify, revoke | Reference trusted UserId | User and claim contracts | Product-owned passwords |
| Platform organization | Platform | Lifecycle and membership | Link by PlatformOrganizationId | Organization DTO | POS as SaaS account authority |
| Product access | Platform | Commercial grant and entitlement | Enforce before operational roles | Access projection | Commercial grant as POS role |
| POS business/store/branch/register | POS | None | Own | Stable references where required | Platform store tables |
| Platform roles | Platform | Define and assign | None | Platform role codes | Automatic POS powers |
| POS roles and permissions | POS | Product access only | Own catalog and enforcement | Product-local contracts | Shared permission mega-catalog |
| Product catalog/plans/trials | Platform | Own commercial catalog | Consume feature codes | Product/Plan DTOs | POS pricing as Platform authority |
| Subscriptions and SaaS payments | Platform | Own | Show and enforce projected status | Subscription/payment status | POS sale as SaaS payment |
| Retail and credit payments | POS | None | Own | None | Reusing retail entities for SaaS billing |
| Entitlements and overrides | Platform | Authoritative composition | Enforce local snapshot | EntitlementSnapshot | Platform call on every transaction |
| Platform Admin UI | Platform | Own Ant Design surface | None | Admin APIs | POS UI dependencies |
| POS UI | POS | None | Own native DesignSystem surface | Token/i18n conventions | Ant or Tailwind in POS |
| Platform audit | Platform | Own commercial/security audit | Emit product correlation where needed | Correlation fields | Product payloads in Platform |
| POS operational audit | POS | None | Own | Correlation fields | Mixed SaaS/retail audit authority |
| Notifications and jobs | Owning system | Platform commercial triggers | POS operational triggers and sync | Optional delivery contracts | Shared operational database |
| Validation/ProblemDetails/pagination | Convention | Use | Use | DTO shapes | Shared DbContext |
| Offline sync/device state | POS | None | Own | Sync policy and idempotency | Platform-owned device database |
| Customers, Utang, sales, inventory | POS | None | Own | Product-owned | Platform operational entities |
