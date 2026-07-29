# Data Authority Matrix

[Capability boundary](platform-product-capability-boundary.md) | [Data ownership](data-ownership.md) | [Contracts](platform-product-contracts.md)

| Data | Authoritative Owner | Stable ID | Referenced By | Replicated Data | Update Authority | Audit Owner | Deletion Owner |
|---|---|---|---|---|---|---|---|
| Platform user | Platform | PlatformUserId (Guid) | HC, POS, Admin | Display name/email cache | Platform | Platform | Platform (+ retention policy) |
| Organization | Platform | PlatformOrganizationId | HC, POS | Name, status, slug | Platform | Platform | Platform |
| Membership | Platform | OrganizationMembershipId | Products (access) | Role codes for access | Platform | Platform | Platform |
| Product | Platform | ProductCode | All | Name, status | Platform | Platform | Platform |
| Plan | Platform | PlanCode + PlanVersion | Products | Limits/features list | Platform | Platform | Platform |
| Subscription | Platform | SubscriptionId | Products | Status, period, plan | Platform | Platform | Platform |
| Entitlement | Platform | EntitlementVersion + org+product | Products | Feature map, limits, grace | Platform | Platform | Platform |
| SaaS payment | Platform | SaaSPaymentId | Admin, org billing UI | Status, reference | Platform | Platform | Platform |
| Clinic | HealthCare | ClinicId | HC only | — | HealthCare | HealthCare | HealthCare |
| Patient | HealthCare | PatientId | HC; may link UserId | — | HealthCare | HealthCare | HealthCare |
| Appointment | HealthCare | AppointmentId | HC | — | HealthCare | HealthCare | HealthCare |
| Medical note | HealthCare | NoteId | HC | — | HealthCare | HealthCare | HealthCare |
| POS business | POS | POSBusinessId | POS; links OrgId | — | POS | POS | POS |
| Store / branch | POS | StoreId / BranchId | POS | — | POS | POS | POS |
| Customer | POS | POSCustomerId | POS; optional UserId later | — | POS | POS | POS |
| CustomerCredit / entries / payments | POS | CreditId / EntryId / CreditPaymentId | POS | — | POS | POS | POS |
| Catalog product / barcode | POS | ProductId (POS) | POS | — | POS | POS | POS |
| Sale | POS | SaleId | POS | — | POS | POS | POS |
| Inventory balance | POS | per store+product | POS | — | POS | POS | POS |
| Supplier | POS | SupplierId | POS | — | POS | POS | POS |
| POS retail payment | POS | RetailPaymentId | POS | Method: cash \| gcash \| customer-credit; GCash ref when gcash | POS | POS | POS |
| POS credit payment | POS | CreditPaymentId | POS | Method: cash \| gcash; GCash ref when gcash | POS | POS | POS |
| Offline device state | POS | DeviceId (later) | POS | — | POS | POS | POS |
| Entitlement projection row | Product (storage) / Platform (facts) | (OrgId, ProductCode, EntitlementVersion) | Product runtime | Copy of Platform snapshot | Product apply from Platform | Product (+ Platform source) | Product |

**Replication:** Platform → product projections only for commercial/identity facts needed locally. No product → Platform replication of operational domain data. No cross-DB FKs. Detail: [data-ownership.md](data-ownership.md), [platform-product-contracts.md](platform-product-contracts.md).
