# Authorization Matrix

[Security](security.md) | [Home](../index.md) | [Capability boundary §12](platform-product-capability-boundary.md) | [Extraction sequence](../reuse/extraction-sequence.md) | [P4-WP04 report](../reports/P4-WP04-audit-authorization-and-closeout.md)

Platform grants **product access**; each product owns **operational permissions**. Platform Administrator does not automatically receive unrestricted clinical or POS operational access (break-glass deferred).

**P3-WP02 note:** Organization and subscription REST endpoints are development-stage regarding **authentication** (no JWT/passwords/MFA/SSO/AD).

**P6-WP01 / P6-WP02 / P6-WP03 / P6-WP04 / P6-WP05 note:** POS customer, credit, repayment, ledger, due-date, overdue, statement, and receipt routes require `X-Pos-Organization-Id`. Commercial capability gates use Development-stage `X-Pos-Subscription-Status` and `X-Pos-Feature-Grants` (not production-secure). Repayment record/reverse and due-date set/clear also require Development/Testing actor header `X-Dev-Platform-User-Id`. Cross-organization access returns 404. Product entry vs feature authorization are separate (Suspended denies; PastDue/Cancelled/Expired continuity only). Development/Testing Platform identity remains the MAUI auth path.

**P10-WP06 / P12-WP01 update:** Product-local POS operational roles (Owner, Admin, StoreManager, Cashier, InventoryStaff, ReportingUser) and grants are **implemented** via `PosRole` / `PosRoleMatrix`. Platform product access still does **not** grant operational permission. Production authentication remains open (**R-091**).

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

Product-local operational roles live in `ExItS_PinoyBusinessPOS` / schema `pos` (P10-WP06). They are **not** Platform roles. Authoritative matrix: `PosRoleMatrix` + `store-*` feature grants + commercial state.

Roles: **Owner**, **Admin**, **StoreManager**, **Cashier**, **InventoryStaff**, **ReportingUser**.

| Concern | Status |
|---|---|
| Product-local POS roles | Implemented (P10-WP06) |
| First-owner bootstrap / last-owner protection | Implemented |
| Register permissions | `store-registers-view/manage` (P10-WP07) |
| Production authentication (JWT/MFA/SSO) | **Open — R-091** |

Platform Admin never assigns POS operational roles. Exact capability intersections are enforced by API/unit tests; UI hiding is not authorization.

Historical note: older “Manager / View profit” rows below are superseded by the product-local matrix (no P&L/profit reports in Full POS).

| Capability (illustrative) | Owner/Admin | StoreManager | Cashier | InventoryStaff | ReportingUser |
|---|---:|---:|---:|---:|---:|
| Manage POS role assignments | Yes | No | No | No | No |
| Catalog / sales / shifts / returns (ops) | Yes | Yes | Partial | Partial | View-oriented |
| Inventory manage / stock counts | Yes | Yes | No | Yes | View |
| Purchasing manage (receive-scoped for InventoryStaff) | Yes | Yes | No | Partial | View |
| Registers manage | Yes | Yes | No | No | No |
| Operational reports | All | All | Own shift/cash variance | Inventory/purchasing family | All |
