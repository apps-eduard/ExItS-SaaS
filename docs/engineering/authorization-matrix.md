# Authorization Matrix

[Security](security.md) | [Home](../index.md) | [Capability boundary §12](platform-product-capability-boundary.md) | [Extraction sequence](../reuse/extraction-sequence.md)

Platform grants **product access**; each product owns **operational permissions**. Platform Administrator does not automatically receive unrestricted clinical or POS operational access (break-glass deferred).

**P3-WP02 note:** Organization and subscription REST endpoints are development-stage and **unauthenticated**. Do not treat them as production authorization.

**P4-WP01 note:** Platform Admin UI is likewise unauthenticated. The development operator footer label is **not** authorization. Production Platform Admin requires real authentication and role checks before any mutation workflows (P4-WP02+).

## Platform organization membership roles (P2-WP02)

Modeled in Domain as `OrganizationRole`:

| Role | Scope |
|---|---|
| OrganizationOwner | Platform organization ownership |
| OrganizationAdministrator | Platform organization administration |
| OrganizationMember | Platform organization participation |

These do **not** grant product-local permissions (Doctor, Cashier, etc.). Platform system roles (Platform Admin / Support) remain separate from organization membership roles.

## Platform roles

| Capability | Platform Admin | Billing Admin | Support Agent |
|---|---:|---:|---:|
| View organizations | Yes | Yes | Yes |
| Manage products/plans | Yes | No | No |
| Activate subscription | Yes | Yes | No |
| Suspend organization | Yes | Conditional | No |
| View platform audit | Yes | Billing scope | Support scope |

## PinoyBusinessPOS roles

| Capability | Owner | Manager | Cashier | Inventory Staff |
|---|---:|---:|---:|---:|
| Manage subscription | Yes | No | No | No |
| Record Utang/payment | Yes | Yes | Yes | No |
| Manage products | Yes | Yes | Conditional | Yes |
| View profit | Yes | Yes | No | No |
| Refund completed sale | Yes | Yes | No | No |
| Adjust inventory | Yes | Yes | No | Yes |

Exact permissions are finalized in product phases and enforced by API tests.
