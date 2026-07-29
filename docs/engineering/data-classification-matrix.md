# Data Classification Matrix

[Contracts](platform-product-contracts.md) | [Data ownership](data-ownership.md) | [Security](security.md)

| Data Element | Classification | Authoritative Owner | May Cross Boundary? | Allowed Consumers | Logging Rule | Retention Owner |
|---|---|---|---|---|---|---|
| ProductCode / PlanCode | Public / internal metadata | Platform | Yes | Products, Admin | OK | Platform |
| PlatformOrganizationId / PlatformUserId | Internal operational | Platform | Yes (IDs) | Products | OK | Platform |
| Org display name | Internal / personal-adjacent | Platform | Yes (projection) | Products | OK | Platform |
| User display name / email | Personal data | Platform | Minimize | Products needing it | Redact email in verbose logs | Platform (+ product snapshot policy) |
| Password / MFA / refresh material | Security-sensitive | Platform | **No** | Platform auth only | Never | Platform |
| Subscription status / EntitlementVersion | Internal / financial-adjacent | Platform | Yes | Products | OK | Platform + product projection |
| Feature limits | Internal operational | Platform | Yes | Products | OK | Platform |
| SaaSPayment amount / reference | Financial | Platform | Admin + status to product | Billing Admin; product status only | Redact instrument data | Platform |
| Payment card / secrets | Security + financial | Platform / PSP | **No** in product contracts | Payment processor / Platform vault | Never | Platform / PSP |
| Clinic / Patient / Note content | Clinical-sensitive | HealthCare | **No** to Platform contracts | HC only | Redact PHI | HealthCare (OD-10) |
| POSCustomer / credit remarks | Product-confidential / personal | POS | **No** to Platform | POS | Redact remarks | POS (OD-10) |
| Sale lines / inventory | Product-confidential | POS | **No** | POS | Aggregate OK; no dumps | POS |
| RetailPayment / CreditPayment | Financial (retail) | POS | **No** as SaaSPayment | POS | Redact tender details as needed | POS |
| CorrelationId / EventId | Internal operational | Publisher | Yes | All audit peers | OK | Publisher retention |
| Support break-glass reason | Internal / sensitive | Platform (when exists) | Limited | Support tools | Controlled | Platform |
| DeviceId | Internal operational | POS | POS↔POS sync later | POS | OK | POS |
