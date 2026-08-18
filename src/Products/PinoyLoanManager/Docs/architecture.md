# Pinoy Loan Manager — Architecture

> Template: P12-WP03. Do not duplicate the foundation; link it.
> Foundation: [exits-product-foundation-reference.md](../../../../docs/Product-Foundation/exits-product-foundation-reference.md)

| Field | Value |
|---|---|
| Product | Pinoy Loan Manager / `pinoy-loan-manager` (proposed, PLM-D-00-01) |
| Database | `ExItS_PinoyLoanManager` (proposed) / schema **Status: Open / Product Owner Decision Required** (PLM-D-00-02) |
| Status | Draft — documentation baseline only; not product-owner approved |
| Implementation present | No |

## System context

```text
[Actors] → Platform (identity, org, subscription, entitlements, SaaS billing)
                ↓ commercial access (contract — see D-P12-03; do not invent)
         Pinoy Loan Manager API / UI (not implemented)
                ↓
         ExItS_PinoyLoanManager (product only; not created)
```

Pinoy Loan Manager must never take a project or database dependency on PinoyBusinessPOS.

## Responsibility boundary

| Area | Platform | This product |
|---|---|---|
| Identity / accounts / future prod auth | Yes (R-091 open) | Consume trusted actor only |
| Organizations / memberships | Yes | Guid reference + isolation |
| Product catalog / plans / subscriptions / entitlements | Yes | Enforce; no Platform table reads |
| SaaS billing / Platform administration / Platform audit | Yes | No |
| Borrower operational records | No | Yes (future) |
| Loan-domain state and workflows | No | Yes (future) |
| Loan operational financial state | No | Yes (future) |
| Product-local authorization | No | Yes (future; roles open) |
| Product DB / migrations | No | Yes (future) |
| Product API / Web UI / MAUI UI / reports / product audit | No | Yes (future) |

## Personal / Borrower model (requirement — not implemented)

ExItS Personal is Platform-owned and product-neutral.

```text
ExItS Personal
      |
      +-- PinoyBusinessPOS
      |      +-- Customer relationship
      |
      +-- Pinoy Loan Manager
      |      +-- Borrower relationship
      |
      +-- future BNPL
             +-- BNPL Customer relationship
```

Rules (planning; not designed as schema):

- one Personal identity may participate in multiple products
- each product owns its own local relationship
- POS Customer != Loan Borrower
- POS customer status never auto-creates a Loan borrower
- Pinoy Loan Manager never reads POS Customer tables
- a borrower may exist without ExItS Personal
- Personal linking is optional
- EX ID / QR resolution identifies only
- resolution alone never links
- an active Personal relationship requires explicit Personal consent
- Loan data remains Loan-product-owned
- Personal may eventually consume authorized Loan information through Loan APIs/contracts only

Do **not** design the final generic Platform relationship schema here (**Status: Open / Product Owner Decision Required**, PLM-D-00-04, PLM-D-00-05).

## Product modules

Planning modules only. None are designed or implemented. Loan policy inside each module is open (PLM-D-00-08).

| Module | Responsibility | Notes |
|---|---|---|
| Product access / isolation | Independent subscription, org isolation, commercial gate | Depends on D-P12-03; no Platform table reads |
| Product-local authorization | Loan roles and grants | PLM-D-00-06 open; do not copy POS roles |
| Borrower foundation | Product-local borrower records | Optional Personal link; PLM-D-00-04 / PLM-D-00-05 open |
| Loan product configuration | Configurable loan products | Rules not defined |
| Application / approval | Intake and decisioning | Approval policy open |
| Origination / disbursement | Starting a loan and releasing funds | Operational money model open (PLM-D-00-07) |
| Schedule / calculation engine | Schedules and calculations | Interest, amortization, rounding open |
| Payment posting | Applying receipts | Allocation order open |
| Collections / delinquency | Arrears handling | Penalties, delinquency, write-off open |
| Reporting / documents | Product reports and documents | Contents open |
| Security / audit / privacy | Product audit, consent, classification | See [security.md](security.md) |
| Offline / MAUI field capabilities | Later native/offline support | Not authorized |

## Data ownership

| Data | SoR | Cross-boundary |
|---|---|---|
| Platform Org / User / Personal ids | Platform | Guid / contract only — no FK |
| Product operational entities (borrower, loan-domain, operational money, product audit) | Product DB | Never in Platform DB; never in POS DB |
| Commercial subscription state | Platform | Via approved contract only (D-P12-03 open) |
| POS Customer / POS operational data | PinoyBusinessPOS | Pinoy Loan Manager must not read |

## Organization isolation

- Server will derive/validate org context; do not trust client org ids as authority alone.
- Cross-org access: conceal using the Product Foundation default (404). This is isolation behavior, not a Loan business rule.
- No shared operational DB with other products.

## Isolation rules (non-negotiable)

Recorded as required intent. Not implemented.

- [x] No cross-product FKs
- [x] No direct Platform table reads from this product
- [x] No Platform reads of this product’s operational tables
- [x] No shared authoritative operational database
- [x] No direct POS database reads (additional PLM rule)

## Client direction (proposed — not authorized)

| Surface | Proposed direction |
|---|---|
| Web | Blazor Web |
| Mobile / Desktop | .NET MAUI Blazor Hybrid |

Possible later native MAUI capabilities (not designed):

- secure storage
- camera / document capture
- biometrics
- connectivity
- notifications
- SQLite / offline support

Web / MAUI component-sharing strategy is **Status: Open / Product Owner Decision Required** (PLM-D-00-09). No client project is authorized.

## External integrations

| System | Direction | Contract | Notes |
|---|---|---|---|
| ExItS Platform | both (future) | Approved APIs/contracts only | Identity, org context, catalog/subscription/entitlements. Transport open (D-P12-03). |
| ExItS Personal | both (future) | Approved APIs/contracts only | Optional linking; consent required; no auto-link from EX ID / QR. Schema open (PLM-D-00-04, PLM-D-00-05). |
| PinoyBusinessPOS | none | None | No project dependency; no table reads; no FKs. |

## Deployment boundary

| Artifact | Name / notes |
|---|---|
| Product image | **Status: Open / Product Owner Decision Required** — independently versioned when packaging is authorized |
| Platform images | Separate — do not fork per customer |
| Persistent DB | `ExItS_PinoyLoanManager` (proposed, PLM-D-00-02) |
| Config | Environment / secrets — not source forks |
| Physical layout | **Status: Open / Product Owner Decision Required** (PLM-D-00-03) |

Detail: `deployment-notes.md` when packaging begins. Not created in this package.

## Observability and background work

| Concern | Approach |
|---|---|
| Logging / correlation | **Status: Open / Product Owner Decision Required** — no secrets, card data, or PHI in logs |
| Metrics / health | **Status: Open / Product Owner Decision Required** |
| Background jobs | Product-owned workers only when authorized; no shared Hangfire DB with other products |

## Explicit non-goals

- Implementing code, projects, databases, migrations, APIs, UI, Docker, or solution entries in PLM-00
- Inventing Loan calculation or collections policy
- Designing the generic Platform relationship schema
- Copying PinoyBusinessPOS architecture, roles, or money models
- Claiming production-secure authentication
- Treating Dev/Testing commercial shortcuts as the production design
