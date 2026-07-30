# Authorization Matrix

[Security](security.md) | [Home](../index.md) | [Capability boundary §12](platform-product-capability-boundary.md) | [Extraction sequence](../reuse/extraction-sequence.md) | [P4-WP04 report](../reports/P4-WP04-audit-authorization-and-closeout.md)

Platform grants **product access**; each product owns **operational permissions**. Platform Administrator does not automatically receive unrestricted clinical or POS operational access (break-glass deferred).

**P3-WP02 note:** Organization and subscription REST endpoints are development-stage regarding **authentication** (no JWT/passwords/MFA/SSO/AD).

**P6-WP01 note:** POS customer routes require `X-Pos-Organization-Id`. Cross-organization access returns 404. Product-local Cashier/Store roles are still not implemented. Development/Testing Platform identity remains the MAUI auth path.

## Authorization layers (fail closed)

```text
Platform-wide system role
→ organization membership role
→ product-access assignment
→ product-local authorization (never granted by Platform Admin)
```

## Platform organization membership roles (P2-WP02)

Modeled in Domain as `OrganizationRole`:

| Role | Scope |
|---|---|
| OrganizationOwner | Platform organization ownership |
| OrganizationAdministrator | Platform organization administration |
| OrganizationMember | Platform organization participation |

These do **not** grant product-local permissions (Doctor, Cashier, etc.). Platform system roles remain separate from organization membership roles.

## Platform system roles and permissions (P4-WP04)

Assignments may be **platform-wide** (`OrganizationId` null) or **organization-scoped**. Cross-organization access fails closed.

| Permission code | PlatformAdministrator | BillingAdministrator | PlatformSupport |
|---|---:|---:|---:|
| `platform.permission.view_portfolio` | Yes | Yes | Yes |
| `platform.permission.manage_organizations` | Yes | Yes | No |
| `platform.permission.manage_platform_users` | Yes | No | No |
| `platform.permission.manage_memberships` | Yes | No | Yes |
| `platform.permission.manage_product_access` | Yes | No | Yes |
| `platform.permission.manage_subscriptions` | Yes | Yes | No |
| `platform.permission.manage_manual_payments` | Yes | Yes | No |
| `platform.permission.manage_entitlement_overrides` | Yes | No | No |
| `platform.permission.view_audit_records` | Yes | Yes | Yes |

Source of truth: `PlatformPermission` + `PlatformRolePermissionCatalog` in Domain.

### Capability summary (legacy matrix view)

| Capability | Platform Admin | Billing Admin | Support Agent |
|---|---:|---:|---:|
| View organizations / portfolio | Yes | Yes | Yes |
| Manage products/plans | Yes | No | No |
| Manage Platform users | Yes | No | No |
| Manage memberships / product access | Yes | No | Yes |
| Activate / manage subscriptions | Yes | Yes | No |
| Confirm manual SaaS payments | Yes | Yes | No |
| Manage entitlement overrides | Yes | No | No |
| View platform audit | Yes | Yes | Yes |

## Development operator limitation (not production authentication)

- APIs remain without JWT / passwords / MFA / SSO / Active Directory.
- `DevelopmentOperator` receives full Platform permissions only when `DevelopmentAuthorizationOptions.GrantDevelopmentOperatorFullAccess` is true (**Development/Testing hosts only**).
- Optional header `X-Dev-Platform-User-Id` selects a Platform User principal whose permissions come **strictly** from role assignments (for denial and scope tests).
- Never enable development-operator full access in production configuration.

## Server-side enforcement

- Authoritative check: `PlatformAuthz.EnsureAsync` on sensitive mutation endpoints.
- Denial: fail closed → `403` ProblemDetails + append-only denied audit record.
- UI hiding of nav items / buttons must not be treated as authorization.

## PinoyBusinessPOS roles

| Capability | Owner | Manager | Cashier | Inventory Staff |
|---|---:|---:|---:|---:|
| Manage subscription | Yes | No | No | No |
| Record Utang/payment | Yes | Yes | Yes | No |
| Manage products | Yes | Yes | Conditional | Yes |
| View profit | Yes | Yes | No | No |
| Refund completed sale | Yes | Yes | No | No |
| Adjust inventory | Yes | Yes | No | Yes |

Exact permissions are finalized in product phases and enforced by API tests. Platform Admin never assigns these roles.
