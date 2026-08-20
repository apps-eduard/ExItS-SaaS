# PinoyServicePro — Architecture

> Template: P12-WP03. Do not duplicate the foundation; link it.
> Foundation: [exits-product-foundation-reference.md](../../../../docs/Product-Foundation/exits-product-foundation-reference.md)

| Field | Value |
|---|---|
| Product | PinoyServicePro / `pinoy-service-pro` (proposed) |
| Database | `ExItS_PinoyServicePro` (proposed) / schema **Open** (PSP-D-00-02) |
| Status | Draft — PSP-00 documentation; Implementation Not Started |
| Last updated | 2026-08-20 |
| Implementation present | No |

## System context

```text
[Actors] → Platform (identity, org, subscription, entitlements, SaaS billing)
                ↓ commercial access (contract — see D-P12-03; do not invent)
         PinoyServicePro API / UI (not created)
                ↓
         ExItS_PinoyServicePro (product only — not created)
```

## Responsibility boundary

| Area | Platform | This product |
|---|---|---|
| Identity / future prod auth | Yes (R-091 open) | Consume trusted actor only |
| Org membership / account context | Yes | Guid reference + isolation |
| Subscription / entitlements | Yes | Enforce; no Platform table reads |
| SaaS payments | Yes | No |
| Domain workflows (booking, jobs, etc.) | No | Yes |
| Product roles / grants | No | Yes |
| Operational money | No | Yes |
| Product DB / migrations | No | Yes (when authorized) |
| Business templates / capabilities | No | Yes |

## Product modules (planning)

| Module | Responsibility | Notes |
|---|---|---|
| Business templates & capabilities | Enable/disable capabilities, terminology, presentation | Not EAV schema generation |
| Customers | Service-business customer records | Product-owned; ≠ POS Customer; ≠ Loan Borrower |
| Bookings & scheduling | Appointments, availability concerns | First-class; ≠ completed transaction |
| Walk-in / check-in | Unscheduled intake and arrival | Capability-driven |
| Service jobs / work orders | Execution of service work | Lines, labor, optional parts |
| Service catalog & pricing | ServiceOffering definitions | Not POS catalog reuse |
| Estimates | Optional quotes / approvals | Mechanic-heavy; barber often off |
| Customer assets | Optional vehicles/devices/etc. | Capability-controlled |
| Staff / resources | Assignment and scheduling | Labels may be template terminology |
| Payments | Operational tenders/receipts | ≠ SaaS billing |
| History / audit / reports / notifications | Durable trail and ops visibility | Channels open (PSP-D-00-14) |

## Data ownership

| Data | SoR | Cross-boundary |
|---|---|---|
| Platform Org / User ids | Platform | Guid only — no FK |
| ServicePro operational entities | Product DB (future) | Never in Platform DB |
| Commercial subscription state | Platform | Via approved contract only |
| POS / Loan operational data | Those products | **No reads, no FKs** |

## Organization and branch isolation

- Server derives/validates org context; do not trust client org ids as authority alone.
- Cross-org access: conceal (planning default 404) — exact behavior open until implementation.
- Branch scope planned from the beginning; single-branch orgs use one default branch.
- Cross-branch scheduling / resource sharing: **Open** (PSP-D-00-12). Do not create cross-branch behavior implicitly.
- No shared operational DB with other products.

## Isolation rules (non-negotiable)

Required intent — not implemented:

- [x] No cross-product FKs
- [x] No direct Platform table reads from this product
- [x] No Platform reads of this product’s operational tables
- [x] No shared authoritative operational database
- [x] No PinoyBusinessPOS DB reads
- [x] No PinoyLoanManager DB reads

## Dynamic template architecture (boundary)

Templates may configure enabled capabilities, terminology, required vs optional fields, defaults, supported workflow variants, navigation visibility, and presentation.

Templates must **not**:

- dynamically generate arbitrary database schemas
- use Entity/Field/Value EAV as the primary operational model
- weaken authorization, tenant isolation, audit, money integrity, or server-authoritative rules

Detail: [Product/business-template-and-capability-model.md](Product/business-template-and-capability-model.md).

## External integrations

| System | Direction | Contract | Notes |
|---|---|---|---|
| Platform commercial / identity | in | Approved contract only | D-P12-03 open; R-091 open |
| Payment providers | out (future) | Not authorized | PSP-D-00-14 adjacent; payment providers not in PSP-00 |
| Notification vendors | out (future) | Not authorized | PSP-D-00-14 |
| POS / Loan products | none | Forbidden direct DB | No project reference authorized |

## Deployment boundary

| Artifact | Name / notes |
|---|---|
| Product image | **Open / Product Owner Decision Required** (PSP-D-00-03) — independently versioned when authorized |
| Platform images | Separate — do not fork per customer |
| Persistent DB | `ExItS_PinoyServicePro` proposed; not created |
| Config | Environment / secrets — not source forks; templates are configuration |

## Observability and background work

| Concern | Approach |
|---|---|
| Logging / correlation | Product-owned when implemented; no secrets/PII dumps |
| Metrics / health | Product health endpoints when authorized |
| Background jobs | Product-owned workers only; no shared Hangfire DB with other products |

## Explicit non-goals

- Implementing product code, projects, migrations, or catalog registration in PSP-00
- Copying PinoyBusinessPOS offline, inventory, or payment systems by default
- Creating PinoyBarber / PinoySalon / PinoyMechanic as separate products
- Claiming BIR / tax / accounting compliance
- Final offline policy (PSP-D-00-04)
- Public anonymous booking security design as if MVP-approved (PSP-D-00-05, PSP-D-00-13)

## Related architecture docs

- [Architecture/application-surface-model.md](Architecture/application-surface-model.md)
- [Architecture/persistence-and-database-boundary.md](Architecture/persistence-and-database-boundary.md)
- [Architecture/api-and-contract-boundary.md](Architecture/api-and-contract-boundary.md)
- [Architecture/mobile-offline-boundary.md](Architecture/mobile-offline-boundary.md)
- [Architecture/platform-commercial-integration.md](Architecture/platform-commercial-integration.md)
