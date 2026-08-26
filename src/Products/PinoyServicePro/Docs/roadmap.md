# PinoyServicePro — Roadmap / Phase Plan

> Template: P12-WP03. Foundation: [exits-product-foundation-reference.md](../../../../docs/Product-Foundation/exits-product-foundation-reference.md)
> Phase names are for **this** product only — do not copy POS or Loan phases as authority.

| Field | Value |
|---|---|
| Product | PinoyServicePro |
| Current phase | PSP-00 — Product Discovery and Documentation Foundation |
| Status | Documentation Foundation Complete; Implementation Not Started; Product Owner Approval Pending |
| Last updated | 2026-08-20 |

## Phase objective (PSP-00)

Establish complete documentation-only foundation for PinoyServicePro: identity, boundaries, dynamic template/capability model, core service domain concepts, booking/scheduling, authorization intent, money/privacy boundaries, technical layout planning, and open decisions — without implementing product code.

## Scope

### Included

- Documentation under `src/Products/PinoyServicePro/Docs/`
- Conceptual domain, templates, workflows, isolation, and roadmap
- Decision register `PSP-D-00-XX`
- Barber and Auto Repair conceptual validation; sanity-check salon / appliance-computer / cleaning

### Excluded

- Implementation code, projects, migrations, databases, catalog registration
- Production authentication (R-091) unless a later phase explicitly delivers it
- Final commercial-state transport (D-P12-03) unless explicitly authorized
- Real payment providers, notification vendors, public booking, BIR claims

## PSP-00 work packages

| WP | Name | Status | Depends on |
|---|---|---|---|
| PSP-00-WP01 | Documentation workspace and product identity | Completed | — |
| PSP-00-WP02 | Product definition and Platform/Product boundaries | Completed | WP01 |
| PSP-00-WP03 | Dynamic business-template and capability model | Completed | WP02 |
| PSP-00-WP04 | Core service operating model | Completed | WP03 |
| PSP-00-WP05 | Booking, scheduling, walk-in and work-order model | Completed | WP04 |
| PSP-00-WP06 | Customer, customer-asset and service-history model | Completed | WP04 |
| PSP-00-WP07 | Services, labor, parts/materials, estimates and pricing baseline | Completed | WP04 |
| PSP-00-WP08 | Staff/resource assignment, roles, grants and authorization baseline | Completed | WP02 |
| PSP-00-WP09 | Payments, documents, reporting, notification and audit baseline | Completed | WP04–WP07 |
| PSP-00-WP10 | Technical product layout, persistence, API, UI and offline boundaries | Completed | WP02 |
| PSP-00-WP11 | Security, privacy and compliance baseline | Completed | WP08–WP10 |
| PSP-00-WP12 | Foundation closeout and implementation-readiness review | Completed | WP01–WP11 |

## Implementation roadmap after PSP-00 (planning only — not authorization)

| Phase | Objective | Notes |
|---|---|---|
| PSP-01 | Product skeleton and Platform integration | Projects/isolation; catalog still requires PSP-D-00-01 |
| PSP-02 | Product-local authorization and organization/branch foundation | Grant identifiers (PSP-D-00-18) |
| PSP-03 | Customer and service catalog foundation | Before heavy scheduling |
| PSP-04 | Booking, scheduling and walk-in operations | Booking first-class |
| PSP-05 | Service jobs / work orders | After booking/walk-in intake |
| PSP-06 | Staff and resource assignment | Can overlap early with PSP-04/05; staff/resource model open (PSP-D-00-09) |
| PSP-07 | Customer assets and service history | Capability-gated assets (PSP-D-00-10) |
| PSP-08 | Estimates, pricing, labor and materials | Inventory depth open (PSP-D-00-11) |
| PSP-09 | Payments, receipts and operational financial controls | Deposit/split/refund open |
| PSP-10 | Reporting, notifications and operational audit | Channels open (PSP-D-00-14) |
| PSP-11 | Mobile/offline capability if authorized | Only after PSP-D-00-04 |
| PSP-12 | Dynamic business-template hardening and initial vertical validation | Barber + Auto Repair |
| PSP-13 | Production/security/operational hardening | R-091 / ops |

### Sequence rationale

Booking/scheduling (PSP-04) precedes full job execution (PSP-05) because the conceptual flow is Booking → Check-in → Job → Payment → History. Customer/catalog (PSP-03) precedes booking because bookings reference customers and services. Assets/history (PSP-07) and estimates/materials (PSP-08) follow core job flow so the core domain is not mechanic-skewed before basic service execution exists. Offline (PSP-11) is late because it is a deliberate product decision, not inherited from POS.

If product owner prioritizes repair-heavy MVP, PSP-07/PSP-08 may be pulled earlier **after** PSP-03–PSP-05 foundations exist; document that change explicitly.

## Dependencies

| Dependency | Notes |
|---|---|
| Platform subscription for `pinoy-service-pro` | Required; slug open (PSP-D-00-01) |
| D-P12-03 | Commercial transport open |
| R-091 | Production auth open |
| Owner approval of docs | PSP-D-00-21 |

## Acceptance criteria (PSP-00)

- [x] Mandatory root docs present and filled (no unresolved `{{PLACEHOLDER}}`)
- [x] Isolation contract stated (separate DB; no Platform/POS/Loan table reads; product-local roles)
- [x] Business-template and booking models documented
- [x] Decision register complete with safe defaults
- [x] Closeout + readiness checklist filed
- [ ] Product owner approval (PSP-D-00-21)
- [ ] Implementation / tests — N/A for PSP-00

## Risks

| ID | Risk | Mitigation |
|---|---|---|
| R-091 | No production-secure identity | Honest Dev/Testing language; fail closed |
| Domain skew | Too POS- or mechanic-specific | Template validation + explicit core concepts |
| EAV temptation | Over-generic schema | Forbidden as primary model |
| Policy invention | Inventing deposits/offline/public booking | Decision IDs + safe defaults |

## Exact next package

**PSP-01 — Product skeleton and Platform integration** (proposed only; do **not** begin until explicitly authorized)

## Phase closeout requirements

- [x] WP matrix complete for PSP-00
- [x] Remaining debt honest via decision register
- [x] No invented unresolved policy disguised as approved
- [x] Closeout report filed
- [ ] Portfolio registration / catalog — deferred to authorized Platform WP
