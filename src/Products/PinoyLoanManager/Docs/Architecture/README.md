# Architecture

**Purpose:** Authoritative **HOW** — technical structure and product boundaries.
**Status:** Foundation / planning only
**Implementation present:** No

This directory will hold Pinoy Loan Manager architecture documents. No .NET projects, APIs, databases, or clients exist yet.

Physical code/project layout (Domain, Application, Infrastructure, Api, clients, tests, deploy) will be decided in a later architecture package. Naming, when authorized, is expected to follow the repository convention `ExItS.PinoyLoanManager.<Layer>` beside this `Docs/` root, matching the sibling product folder style under `src/Products/`. That is a convention observation only — this package does not copy PinoyBusinessPOS architecture.

---

## Portfolio position

```text
ExItS Platform
├── PinoyBusinessPOS
├── Pinoy Loan Manager
└── future products such as BNPL
```

Pinoy Loan Manager is independently subscribed. It is not a PinoyBusinessPOS module and must never take a project dependency on PinoyBusinessPOS.

---

## Future product surfaces (intent only)

When later packages authorize implementation, Pinoy Loan Manager is expected to have its own:

- operational domain
- database and migrations
- product-local authorization
- API
- Web client
- MAUI Blazor Hybrid client
- operational financial records
- audit / history
- reports

---

## Isolation (already-approved intent)

- Separate product database: proposed name `ExItS_PinoyLoanManager` (not created)
- No cross-product foreign keys
- No direct Pinoy Loan Manager reads of PinoyBusinessPOS tables
- No direct Pinoy Loan Manager reads of Platform tables
- No direct Platform reads of Pinoy Loan Manager operational tables
- Integration with Platform only through approved contracts / APIs
- Organization references as `Guid` identifiers/contracts only
- SaaS billing money stays in Platform; loan operational money stays in Pinoy Loan Manager
- Product-local roles/grants will be authoritative inside this product; Platform access is not operational permission

Do not invent the production Platform→product commercial-state transport (**D-P12-03** remains open). Do not invent production authentication (**R-091** remains open).

---

## Personal / cross-product identity (foundation intent only)

ExItS Personal is a **Platform-owned, product-neutral** person identity.

The same Personal identity may eventually participate independently in multiple products:

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
             +-- BNPL customer relationship
```

Planning rules already agreed:

- one ExItS Personal identity
- separate product-local relationships
- POS Customer ≠ Loan Borrower
- a POS Customer does **not** automatically become a Loan Borrower
- Pinoy Loan Manager must not read POS Customer tables
- Personal identity belongs to Platform
- Borrower belongs to Pinoy Loan Manager
- Loan borrower linking to Personal is optional
- a borrower may exist without an ExItS Personal account
- scanning / resolving an EX ID must never auto-link
- explicit consent is required before activating a Personal relationship
- no cross-product operational data access
- loan operational data remains owned by Pinoy Loan Manager

Do **not** design the final generic Platform relationship schema in this package. That is an open architectural decision.

---

## Client direction (proposed — not implemented)

| Surface | Proposed direction |
|---|---|
| Web | Blazor business / administration interface |
| Mobile / Desktop | .NET MAUI Blazor Hybrid |

Native MAUI services may later provide:

- secure storage
- camera / document capture
- biometrics
- connectivity
- notifications
- local SQLite / offline support

None of these clients or native services are scaffolded in this package.

---

## Future architecture subjects (not designed here)

- product project/solution layout
- Platform integration contracts
- Personal identity linking implementation
- database schema ownership details
- API architecture
- Web architecture
- MAUI Blazor Hybrid architecture
- offline / local-store architecture
