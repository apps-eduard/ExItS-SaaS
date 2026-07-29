# Data Ownership

[Architecture](architecture.md) | [Security](security.md) | [Data authority matrix](data-authority-matrix.md) | [Capability boundary](platform-product-capability-boundary.md) | [Contracts](platform-product-contracts.md) | [Classification](data-classification-matrix.md) | [Extraction sequence](../reuse/extraction-sequence.md) | [ADR-012](../decisions/ADR-012-versioned-platform-contracts-and-local-projections.md) | [ADR-013](../decisions/ADR-013-build-new-platform-before-healthcare-reconnection.md)

**Work package:** P1-WP02
**Status:** Authoritative ownership + projection rules (documentation)

Authoritative field-level matrix: [data-authority-matrix.md](data-authority-matrix.md). Contract shapes: [platform-product-contracts.md](platform-product-contracts.md).

## Global rules

1. One system of record per data type.
2. Replication is **Platform → product projection** for commercial/identity facts only (unless a future approved product→Platform contract exists — none for operational domain today).
3. No cross-database foreign keys; no direct cross-product DB access; no shared EF entities/DbContexts.
4. Update authority = system of record (except products **apply** projection rows from Platform events/snapshots).
5. Deletion of Platform entities does **not** cascade into product DBs; use explicit workflows.
6. Legal retention periods: **pending compliance (OD-10)**.

---

## Platform database (summary)

Users, organizations, memberships, products, plans, subscriptions, SaaS payments, entitlements/overrides, Platform audit.

## HealthCare database (summary)

Clinics, staff assignments, patients, appointments, medical notes, clinical authz, HC audit.

## PinoyBusinessPOS database (summary)

Businesses, stores/branches/registers, customers, credit, retail payments, catalog, sales, inventory, expenses, suppliers, offline device state, POS audit, **entitlement projection rows**.

---

## Ownership catalog

### Platform user

| Aspect | Rule |
|---|---|
| System of record | Platform |
| Stable ID | PlatformUserId |
| Replication | → products: DisplayName, status, access facts (see identity projection) |
| Prohibited replication | Credentials, tokens, MFA, login history dumps |
| Update / audit / deletion | Platform |
| Retention | Platform (+ OD-10); products keep historical actor refs |
| Projection | Optional identity cache; not auth SoR |

### Platform organization

| Aspect | Rule |
|---|---|
| System of record | Platform |
| Stable ID | PlatformOrganizationId |
| Replication | → products: name, slug, status, coarse subscription status |
| Prohibited | Product clinic/store trees as Platform SoR |
| Update / audit / deletion | Platform |
| Retention | Platform |
| Projection | Org projection row per product |

### Membership

| Aspect | Rule |
|---|---|
| System of record | Platform |
| Stable ID | OrganizationMembershipId |
| Replication | → products: membership/access status needed for gate |
| Prohibited | Mixing HC StaffMember as Platform SoR |
| Update / audit / deletion | Platform |
| Projection | Membership/access summary |

### Product / plan / subscription / entitlement / SaaS payment

| Aspect | Rule |
|---|---|
| System of record | Platform |
| Stable IDs | ProductCode, PlanCode+PlanVersion, SubscriptionId, EntitlementVersion/SnapshotId, SaaSPaymentId |
| Replication | → products: status, plan, feature map/limits, grace/suspend, payment **status** refs only |
| Prohibited | Product-owned billing ledger; POS retail/credit payments as SaaSPayment |
| Update / audit / deletion | Platform |
| Projection | Full commercial entitlement snapshot locally |

### Clinic / patient / appointment / medical note

| Aspect | Rule |
|---|---|
| System of record | HealthCare |
| Stable IDs | ClinicId, PatientId, AppointmentId, NoteId |
| Replication | **None** to Platform commercial contracts |
| Prohibited | Clinical payloads in Platform events/audit |
| Update / audit / deletion / retention | HealthCare |
| Projection | N/A (domain-local) |

### POS business / store / branch / register / customer / credit / catalog / sale / inventory / supplier / retail payment / device state

| Aspect | Rule |
|---|---|
| System of record | PinoyBusinessPOS |
| Stable IDs | POSBusinessId, StoreId, BranchId, RegisterId, POSCustomerId, Credit*/Sale*/RetailPaymentId/CreditPaymentId, DeviceId later |
| Replication | **None** to Platform except correlation metadata |
| Prohibited | Treating Customer as Platform User; SaaSPayment confusion; storing GCash secrets |
| Update / audit / deletion / retention | POS |
| Projection | May store PlatformOrganizationId / UserId as values; entitlement projection separate |
| MVP payment methods | Sale: `cash`, `gcash`, `customer-credit`. Credit repayment: `cash`, `gcash`. GCash = manual verification + required normalized reference. See [pinoy-business-pos-requirements.md](../product/pinoy-business-pos-requirements.md). |

### Entitlement projection row

| Aspect | Rule |
|---|---|
| System of record | **Commercial facts:** Platform; **row storage:** Product |
| Stable ID | (PlatformOrganizationId, ProductCode, EntitlementVersion) / SnapshotId |
| Replication | Platform → product only |
| Update authority | Product applies Platform events/snapshots only |
| Audit | Product apply audit + Platform source EventId |
| Deletion | Product may prune old versions per policy; must not invent entitlements |
| Behavior | See [entitlement-state-matrix.md](entitlement-state-matrix.md) |

---

## Prohibited coupling (ownership)

- Cross-DB FKs / shared DbContext
- Product as SaaS billing SoR
- Platform as clinical or retail operational SoR
- Silent cascade deletes across boundaries
