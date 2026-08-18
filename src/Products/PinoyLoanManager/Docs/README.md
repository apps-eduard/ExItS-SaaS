# Pinoy Loan Manager — Product Documentation

**Status:** Foundation / planning only
**Implementation present:** No
**Product directory:** `src/Products/PinoyLoanManager/`
**Documentation root:** `src/Products/PinoyLoanManager/Docs/` (D-P12-02)

Pinoy Loan Manager is a **separate first-class ExItS SaaS product**. It is a sibling of PinoyBusinessPOS, not a POS module, POS feature, or POS database extension.

```text
ExItS Platform
├── PinoyBusinessPOS
├── Pinoy Loan Manager
└── future products
```

No Loan implementation exists yet. This tree holds documentation structure and recorded architectural intent only.

---

## Product identity (proposed)

| Item | Value | Status |
|---|---|---|
| Display name | Pinoy Loan Manager | Recorded |
| Repository directory | `PinoyLoanManager` | Recorded |
| Product code / slug | `pinoy-loan-manager` | Proposed — Platform catalog registration is future work |
| Future database | `ExItS_PinoyLoanManager` | Proposed — not created |

---

## Ownership boundary

### Platform owns (shared SaaS concerns)

- identity
- organizations
- product catalog
- subscriptions
- entitlements
- SaaS billing

### Pinoy Loan Manager will own (future operational product)

- operational domain
- borrowers
- loans
- operational financial records
- product-local authorization
- product database
- product migrations
- API
- Web UI
- MAUI UI
- reports
- product audit / history

---

## Permanent boundary intentions

These are already-approved planning constraints. They are **not implemented** in this work package.

- Independent product subscription (PinoyBusinessPOS subscription does not unlock Pinoy Loan Manager, and vice versa)
- Separate product database (`ExItS_PinoyLoanManager` when persistence is authorized)
- No cross-product foreign keys
- No direct Pinoy Loan Manager reads of PinoyBusinessPOS tables
- No direct Pinoy Loan Manager reads of Platform tables
- Platform integration only through approved contracts / APIs
- SaaS billing money remains Platform-owned
- Loan operational money remains Loan-product-owned
- Organization IDs may appear only as identifiers/contracts (`Guid`), never as cross-database foreign keys
- Platform product access is not product operational permission
- Do not copy PinoyBusinessPOS domain entities, roles, or financial models into this product

Portfolio-open items remain open and must not be invented here: **D-P12-03** (Platform→product commercial-state transport) and **R-091** (production authentication).

---

## Documentation map

| Directory | Purpose |
|---|---|
| [Product/](Product/README.md) | **WHAT** the loan product does and its business rules |
| [Architecture/](Architecture/README.md) | **HOW** the system is technically structured |
| [Security/](Security/README.md) | Access, privacy, authorization, consent, and data protection |
| [Decisions/](Decisions/README.md) | ADRs explaining important choices and **WHY** |
| [Phases/](Phases/README.md) | Planned implementation phases and work-package sequencing |
| [Reports/](Reports/README.md) | Completed work-package evidence |
| [Validation/](Validation/README.md) | Owner, device, browser, and financial-calculation validation evidence |
| [Operations/](Operations/README.md) | Deployment, migrations, backup/restore, and production operations |

Navigation index: [FILE-MANIFEST.md](FILE-MANIFEST.md)

Do not scatter Pinoy Loan Manager documentation into the repository-root `docs/` tree unless the content is genuinely portfolio-wide.

---

## Client direction (proposed — not implemented)

| Surface | Proposed technology |
|---|---|
| Web | Blazor business / administration UI |
| Mobile / Desktop | .NET MAUI Blazor Hybrid |

Possible future native MAUI capabilities (not authorized in this package):

- secure storage
- camera / document capture
- biometrics
- connectivity
- notifications
- SQLite / offline support

---

## Personal identity (foundation intent only)

ExItS Personal is a Platform-owned, product-neutral person identity. The same Personal identity may later participate independently as a POS Customer, a Loan Borrower, and a future BNPL customer. POS Customer ≠ Loan Borrower. Linking is optional, requires explicit consent, and must never auto-activate from EX ID / QR resolution alone. Details: [Architecture/README.md](Architecture/README.md).

---

## Explicit exclusions (this work package)

This package does **not**:

- implement loan functionality, entities, calculations, workflows, or business rules
- create .NET projects, tests, solution entries, migrations, or deployables
- fill Product Foundation templates (`product-definition.md`, `architecture.md`, and related mandatory templates)
- register the product in the Platform catalog
- modify shared Product Foundation rules, PinoyBusinessPOS, Platform, or `ExItS.slnx`

Loan policy subjects such as interest, amortization, penalties, payment allocation, delinquency, restructuring, write-off, credit scoring, lending limits, and approval rules remain **future product-owner decisions**.
