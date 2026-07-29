# Platform–Product Capability Boundary

[Home](../index.md) | [Ownership matrix](capability-ownership-matrix.md) | [Data authority](data-authority-matrix.md) | [ADR-011](../decisions/ADR-011-platform-authority-and-product-local-projections.md) | [Final portfolio boundaries](final-portfolio-boundaries.md)

**Work package:** P1-WP01  
**Status:** Authoritative for Phase 1+ (documentation)  
**Date:** 2026-07-29

---

## 1. Purpose

Define the authoritative capability boundary between ExITS Platform, HealthCare, PinoyBusinessPOS, shared contracts, and engineering conventions so later implementation does not put product logic in Platform, duplicate commercial ownership, couple products, or require synchronous Platform calls for normal product operations.

## 2. Definitions

| Term | Meaning |
|---|---|
| **Platform Organization** | Global SaaS customer/account boundary |
| **Product** | Deployable SaaS offering (HealthCare, PinoyBusinessPOS) |
| **Subscription** | Commercial right for an organization to use a product under a plan |
| **Entitlement** | Feature codes and limits granted by subscription/override |
| **Local projection** | Product-owned cache of Platform commercial/identity facts needed for offline-safe decisions |
| **Operational permission** | Product-local authorization to perform domain actions |
| **Product access** | Platform decision that an organization/user may use a product at all |

## 3. Boundary principles

1. One primary owner per capability (system of record).
2. Products may project Platform data; projection ≠ ownership.
3. No cross-database foreign keys; no direct cross-product DB access.
4. No shared EF entities across bounded contexts.
5. Normal product operations must not synchronously call Platform on every request.
6. Clinical payloads never enter Platform contracts; POS operational payloads never enter entitlement contracts.
7. Framework-specific UI is product-local (ADR-010).
8. Shared code only after two verified consumers and product-neutral design.
9. Do not rename Patient↔Customer or Clinic↔Store.
10. HealthCare remains frozen/ignored until an approved import WP.

## 4. Platform ownership

Global identity and users; credentials and session/refresh lifecycle; Platform Organizations and memberships; product catalog; plans/trials/subscriptions; SaaS payments and billing status; entitlements and organization feature overrides; Platform Admin (native UI); Platform support ops; Platform audit and security events; subscription lifecycle jobs; Platform notifications (trial/payment/security).

## 5. HealthCare ownership

Clinics; clinical workforce (doctor/nurse/receptionist/clinic admin); Patients and patient self-scope; appointments, availability, reminders; medical notes and amendments; clinical permissions and authorization; clinical and HC product audit details; HC product workflows; Staff Web Ant Design UI; PatientWeb and MAUI clinical UX; HC Hangfire jobs (reminders/summaries).

## 6. PinoyBusinessPOS ownership

POS business/operating profile; stores, branches, registers; POS roles/memberships; Customers; CustomerCredit / Utang, credit entries/payments, ledgers; catalog products, barcodes; sales; inventory; expenses; suppliers; purchasing; shifts; returns/refunds; POS reports; offline storage and sync state; POS notifications and jobs; native MAUI UI.

## 7. Shared contracts

Versioned DTOs/events for: UserId, PlatformOrganizationId, product code, plan code/version, subscription id/status, entitlement snapshot, correlation IDs, UTC timestamps, explicit status enums. Idempotent entitlement updates. Contract tests. Additive evolution and deprecation policy.

## 8. Shared conventions

ProblemDetails shape; pagination/filter request models; validation conventions; localization key conventions (`en`/`fil`); semantic design-token **names**; accessibility and motion standards; logging correlation; UTC time handling. Prefer conventions and patterns before packages.

## 9. Explicit non-shared areas

| Prohibited | Reason |
|---|---|
| Cross-DB FKs / shared DbContext | Coupling and extraction risk |
| Shared domain entity bases / generic repositories | Fake reuse |
| Shared product permission catalogs | Roles differ by product |
| Ant Design / Tailwind in Platform Admin or POS | ADR-010 |
| Shared UI pages / Ant↔native switcher | Framework coupling |
| Clinical data in Platform audit/events | Privacy / regulation |
| SaaS billing ledger inside products | Wrong system of record |
| POS retail payment entities as Platform payments | Different business |

## 10. Identity boundary

| Concern | Owner |
|---|---|
| Global UserId, email, credentials, verification, activation/suspension | Platform |
| Login attempts, refresh tokens, sessions, revocation, user security events | Platform |
| User profile (global) | Platform |
| Product access (may use product) | Platform |
| HealthCare Patient profile | HealthCare |
| POS Customer | POS |
| Product-local employee/staff profiles | Product |
| Dev-only test identities | Dev/test only; disabled outside Development/Testing |

**Rules:** Platform owns global authentication identity. Products reference stable Platform UserId. Patient ≠ global user. POS Customer is **not** automatically a Platform User. Store customers may exist **without** login accounts. Future customer login (Customer ↔ Platform User link) is **deferred** (open decision). Do not merge Customer and ApplicationUser.

## 11. Organization boundary

```text
Platform Organization
├── HealthCare subscription → clinics (1..n)
└── PinoyBusinessPOS subscription → POS business → stores/branches (1..n) → registers
```

- Platform Organization = SaaS customer boundary (**yes**).
- One organization **may** subscribe to multiple products (**yes**).
- One user **may** belong to multiple organizations (**yes**, target; HC MVP today is limited — generalize on extraction).
- One organization **may** have multiple clinics/stores/branches (**yes**).
- Clinic / Store / Branch / Register are product-local operational entities.
- Mapping: products store `PlatformOrganizationId`; Platform does not store clinic/store tables as system of record.

## 12. Role and permission boundary

### Platform roles (examples)

Platform Administrator, Platform Support, Billing Administrator, Organization Owner, Organization Administrator.

### HealthCare roles

Clinic Administrator, Doctor, Nurse, Receptionist, Patient (+ existing HC staff roles).

### POS roles (documentation-level; do not over-finalize)

Business Owner, Business Administrator, Store Manager, Cashier, Inventory Staff, Reporting User.

| Question | Answer |
|---|---|
| Who owns role definitions? | Platform owns Platform roles; each product owns its operational roles |
| Where stored? | Platform memberships for org/product access; product DBs for operational assignments |
| Product access vs operational permission? | **Separated** — Platform grants access; product grants operations |
| Platform Admin unrestricted clinical/POS access? | **No** — requires explicit support/break-glass (break-glass **deferred**) |

## 13. Product catalog and billing boundary

Platform owns: Product, product code/status/version metadata, Plan, plan version, trial definition, Subscription, billing period, Payment (SaaS), Entitlement, feature codes, organization feature override, grace, suspension, cancellation, upgrade/downgrade.

Products may publish known feature identifiers via versioned contracts; Platform owns commercial assignment.

**Not** Platform entitlements: ordinary product settings (e.g. “require stock confirmation before sale”).

```text
Platform payment → org pays ExITS for software
POS sale payment → retail customer pays store for goods
```

Separate entities, services, permissions, and audit trails.

## 14. Entitlement boundary

- **Authoritative:** Platform.
- **Projection:** each product stores what it needs: PlatformOrganizationId, product code, subscription id, plan code/version, entitlement version, feature codes/limits, effective/expiry/refresh times, subscription/grace/suspension state, last sync, source event/version.

**Principle:** Normal product operations must not require a synchronous Platform request on every transaction.

High-level behaviors (detail in P1-WP02 / Phase 3): fresh snapshot; temporarily stale OK within policy; Platform unavailable → use fail-safe + grace; trial expiry / suspension / downgrade / feature removed with existing local data; duplicate/out-of-order events; manual override via Platform. Transport/messaging **not** specified here.

## 15. Audit boundary

| Owner | Events |
|---|---|
| Platform | Auth, sessions, org/membership, plan/subscription/payment/entitlement, support/admin |
| HealthCare | Clinical workflows, patient access, appointments, notes, HC authorization |
| POS | Credit, sales, voids, refunds, inventory, expenses, shifts, store permissions, sync conflicts |

Cross-boundary correlation fields (not combined DBs): CorrelationId, PlatformUserId, PlatformOrganizationId, ProductCode, product-local actor/resource IDs, UTC timestamp, DeviceId (later), request/event id. **No PHI in Platform audit.**

## 16. Notification boundary

Platform: trial/payment/suspension, account security, announcements.  
HealthCare: appointments/reminders/clinical.  
POS: credit due, low stock, sync failure, shift/sales/inventory alerts.  

Content, consent, templates, triggers = domain-owned. Shared delivery abstraction **deferred**.

## 17. Background-job boundary

Each service owns its jobs and preferably its job storage. Sharing Hangfire *patterns* ≠ one shared worker/DB for all products. Platform: subscription lifecycle, entitlement publish. HC: reminders/summaries. POS: sync, offline retries, product notifications.

## 18. UI boundary

| Surface | Technology |
|---|---|
| HC Staff Web | Ant Design Blazor (retain) |
| HC PatientWeb / MAUI | Existing native (retain) |
| New Platform Admin | Blazor Web App + native CSS/Razor; no Ant; no Tailwind |
| POS | MAUI Blazor Hybrid + same native foundation |

Share: models, validation/localization conventions, token names, a11y/motion, pagination/filter models.  
Local: shells, navigation, pages, workflows, framework adapters, HC Ant, POS cashier UX.  
No Ant↔native conditional component.

## 19. Data ownership table

See [data-authority-matrix.md](data-authority-matrix.md).

## 20. Failure behavior

| Situation | Expectation |
|---|---|
| Platform temporarily unavailable | Products continue with valid local projection within policy |
| Auth unavailable | Existing sessions may continue until expiry/revoke policy; new logins fail |
| Entitlement stale | Allowed within refresh window; then constrained fail-safe |
| Subscription suspended | Product enforces projection state (e.g. block new credit) |
| Product DB unavailable | Product outage; Platform may still manage subscriptions |
| Offline POS device | Operate on local DB + queued sync (Phase 7 detail) |
| Delayed/duplicate events | Idempotent projection apply |
| Contract version mismatch | Compatibility policy; reject unsafe payloads |

Exact SLAs **not** promised here.

## 21. Open decisions (do not guess)

| ID | Question | Status |
|---|---|---|
| OD-01 | Customer ↔ Platform User login linkage | Deferred |
| OD-02 | Break-glass Platform support into product ops | Deferred |
| OD-03 | Exact entitlement event transport (HTTP poll vs bus) | P1-WP02 / Phase 3 |
| OD-04 | MFA | Deferred |
| OD-05 | When/how to import HealthCare into monorepo | After Platform foundation (Phase 1–2) |
| OD-06 | Multi-org membership migration from HC single StaffMember | Phase 2 extraction design |

## 22. Enforcement rules

- Reviewers reject PRs that put product domain in Platform or Ant in Platform Admin/POS.
- Contract changes require version bump + consumer tests.
- Shared packages need two consumers and ADR/governance check.
- Client-supplied OrganizationId never authoritative.

## Required decisions (P1-WP01 answers)

| # | Decision | Answer |
|---|---|---|
| 1 | Platform Organization = global SaaS customer boundary? | **Yes** |
| 2 | One org multiple products? | **Yes** |
| 3 | One user multiple orgs? | **Yes** (target) |
| 4 | Multiple clinics/stores/branches? | **Yes** |
| 5 | Product roles only in product? | **Yes** (operational roles) |
| 6 | Platform access vs product permissions? | **Separated** |
| 7 | Platform authoritative for subscriptions/entitlements? | **Yes** |
| 8 | Local entitlement projections? | **Yes** |
| 9 | Cross-DB FKs prohibited? | **Yes** |
| 10 | Clinical records prohibited from Platform payloads? | **Yes** |
| 11 | POS Customer ≠ Platform User? | **Yes** |
| 12 | SaaS payments ≠ POS retail payments? | **Yes** |
| 13 | Framework UI product-local? | **Yes** |
| 14 | Shared code only after two consumers? | **Yes** |
