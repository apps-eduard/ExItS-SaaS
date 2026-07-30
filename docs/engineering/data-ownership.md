# Data Ownership

[Architecture](architecture.md) | [Security](security.md) | [Data authority matrix](data-authority-matrix.md) | [Capability boundary](platform-product-capability-boundary.md) | [Contracts](platform-product-contracts.md) | [Classification](data-classification-matrix.md) | [Extraction sequence](../reuse/extraction-sequence.md) | [ADR-012](../decisions/ADR-012-versioned-platform-contracts-and-local-projections.md) | [ADR-013](../decisions/ADR-013-build-new-platform-before-healthcare-reconnection.md)

**Work package:** P1-WP02 (ownership docs); **P2-WP02** identity; **P2-WP03** commercial; **P2-WP04** outbound HC projection contracts (Platform-side only); **P2-WP05** migration dry-run validation (no real migration); **P3-WP02** organization + subscription persistence; **P4-WP02** Platform users, organization memberships, and product-access assignments (no product-local roles); **P4-WP03** Admin subscription/payment/trial workflows over existing Phase 3 persistence (no new commercial migration); **P4-WP04** Platform role assignments + append-only audit records (`platform.platform_role_assignments`, `platform.audit_records`).
**Status:** Authoritative ownership + projection rules; identity/commercial domain + contract adaptation foundation in code; org/subscription rows for commercial lifecycle; authorization/audit tables for Platform Admin closeout

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

Users, organizations, memberships, products, plans, subscriptions, SaaS payments, entitlements/overrides, Platform system role assignments, Platform audit records.

## HealthCare database (summary)

Clinics, staff assignments, patients, appointments, medical notes, clinical authz, HC audit.

## PinoyBusinessPOS database (summary)

**P6-WP01 (customers):** Database `ExItS_PinoyBusinessPOS`, schema `pos`, table `customers`. OrganizationId is a Platform organization GUID value only (no cross-database FK). Soft deactivate; no physical delete. Notes are identification only — not credit records.

**P6-WP02 (remarks-based credit):** Same database/schema, table `credit_entries`. Organization-owned append-only credit history with explicit reversal. Outstanding was initially active credits only.

**P6-WP03 (payments and ledger):** Same database/schema, table `repayments`. Append-only repayments with explicit reversal and actor metadata. Unified ledger is a read model (UNION), not a persisted ledger table. Outstanding = active credits − active repayments. Inactive customers may repay existing debt. SaaS subscription payments remain distinct.

**P6-WP04 (due dates and overdue):** Same database/schema. Nullable `current_due_date` on `credit_entries`; append-only `credit_due_date_changes` history (reason, actor, UTC). FIFO aging and overdue status are read models only — no persisted payment allocations. Effective business date = server UTC calendar day (org timezone not defined). Outstanding formula unchanged.

Later POS ownership (not yet implemented): businesses, stores/branches/registers, statements/receipts, retail payments, catalog, sales, inventory, expenses, suppliers, offline device state, POS audit, **entitlement projection rows**.

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

**P3-WP04:** Platform persists authoritative `feature_overrides` and immutable `entitlement_snapshots` (+ grants). Product-local projection storage and delivery remain product-owned and out of scope.

### Platform role assignment (P4-WP04)

| Aspect | Rule |
|---|---|
| System of record | Platform |
| Stable ID | PlatformRoleAssignmentId |
| Table | `platform.platform_role_assignments` |
| Scope | Platform-wide (`OrganizationId` null) or organization-scoped |
| Replication | None to products (Platform Admin operations only) |
| Prohibited | Product-local roles (Doctor, Cashier, etc.); clinical/POS permissions |
| Update / audit / deletion | Platform (revoke is status change; mutations audited) |
| Retention | Platform (+ OD-10) |

### Platform audit record (P4-WP04)

| Aspect | Rule |
|---|---|
| System of record | Platform |
| Stable ID | AuditRecordId |
| Table | `platform.audit_records` (append-only) |
| Contents | Actor, action code, target, organization, product code, correlation id, outcome, reason, safe summary, UTC |
| Prohibited | Passwords, tokens, card/GCash secrets, PHI, raw payloads, exception dumps |
| Replication | None to products |
| Retention | Platform; archival policy pending (R-096 / OD-10) |

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
