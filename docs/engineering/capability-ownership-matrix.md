# Capability Ownership Matrix

[Capability boundary](platform-product-capability-boundary.md) | [Data authority](data-authority-matrix.md)

Primary ownership only. Projection ≠ system of record.

| Capability | System of Record | Platform Responsibility | Product Responsibility | Local Projection | Shared Contract | Prohibited Coupling |
|---|---|---|---|---|---|---|
| Global user ID / email / credentials | Platform | Authenticate, suspend, verify | Reference UserId; product profiles | Optional user display cache | UserId, claims shape | Product-owned passwords |
| Password reset / verification / MFA later | Platform | Own flows | Deep links/UI only | — | Status events | Product identity stores |
| Login attempts / sessions / refresh / revoke | Platform | Own | Consume tokens/sessions | Session hints only | Token contracts | Product refresh-token tables as SoR |
| User security events | Platform | Own | May emit product security events | — | Correlation | PHI in Platform |
| Dev-only test identities | Dev/Test config | Policy | Must disable outside Dev/Test | — | — | Prod seed users |
| Platform Organization | Platform | CRUD, status | Link via PlatformOrganizationId | Org name/status cache | OrgId DTO | Product as SaaS account SoR |
| Organization membership | Platform | Org↔user↔product access | Assign operational roles locally | Membership summary | Membership DTO | Mixing HC StaffMember as Platform SoR |
| Clinic | HealthCare | — | Own | — | — | Platform clinic tables |
| POS Business / Store / Branch / Register | POS | — | Own | — | — | Platform store tables |
| Platform roles | Platform | Define/assign | — | — | Role codes | Auto clinical/POS powers |
| HC clinical roles / permissions | HealthCare | Product access only | Own catalog & enforcement | — | — | Shared permission mega-catalog |
| POS roles / permissions | POS | Product access only | Own catalog & enforcement | — | — | Patient self-scope copy |
| Patient self-scope | HealthCare | — | Own | — | — | POS customer rule |
| Product catalog / plans / trials | Platform | Own | Consume feature codes | Plan display | Product/Plan DTOs | Product pricing SoR |
| Subscriptions / grace / suspend | Platform | Own | Enforce via projection | Full commercial snapshot | Subscription status | Product billing ledger |
| SaaS payments / invoices | Platform | Own | Show status only | Payment status refs | Payment status DTO | POS sale as SaaS payment |
| Entitlements / overrides | Platform | Authoritative | Enforce locally | EntitlementSnapshot | Snapshot schema | Sync call every txn |
| Feature vs local setting | Split | Feature codes | Operational settings | Features only | Feature code list | Settings as entitlements |
| Platform admin UI | Platform | Native Admin | — | — | Admin APIs | Ant Design Admin |
| HC Staff / Patient / Mobile UI | HealthCare | — | Own stacks | — | — | Shared Ant to POS |
| POS UI | POS | — | Native MAUI | — | Token/i18n conventions | Ant / Tailwind |
| Platform audit | Platform | Own | — | — | Correlation fields | Clinical payloads |
| HC clinical audit | HealthCare | — | Own | — | Correlation | Platform PHI store |
| POS operational audit | POS | — | Own | — | Correlation | Mixed SaaS/retail payment audit |
| Platform notifications | Platform | Own triggers | — | — | Optional delivery contract later | Product content in Platform |
| HC / POS notifications | Product | — | Own | — | — | Shared mega-notifier now |
| Platform jobs | Platform | Own | — | — | — | One Hangfire DB for all |
| HC reminder/summary jobs | HealthCare | — | Own | — | — | Shared worker with POS |
| POS sync / offline jobs | POS | — | Own | — | Sync contracts later | Platform owns device DB |
| Validation / ProblemDetails / pagination | Convention | Use | Use | — | DTO shapes | Shared DbContext |
| BFF / session patterns | Pattern | Admin may use | HC BFF; MAUI session | — | Pattern docs | Shared BFF host for all |
| Offline sync / device state | POS | — | Own | — | Sync policy later | HC offline assumptions |
| Appointments / notes / patients | HealthCare | — | Own | — | — | POS domain rename |
| Customers / CustomerCredit / sales / inventory | POS | — | Own | — | — | HC clinical reuse |
