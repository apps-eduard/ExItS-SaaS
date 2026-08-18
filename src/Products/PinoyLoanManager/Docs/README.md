# Pinoy Loan Manager — Product Documentation

Authoritative product docs for **Pinoy Loan Manager** (`pinoy-loan-manager`, proposed).

Always load with:

1. `.cursor/rules/exits-workflow.mdc`
2. `.cursor/rules/exits-product-context.mdc`
3. `docs/Product-Foundation/exits-product-foundation-reference.md` (repo path)
4. Docs in this folder
5. The active work-package prompt/report
6. Files required for the task only

**Status:** Draft — PLM-00 documentation baseline
**Implementation present:** No
**Documentation root:** `src/Products/PinoyLoanManager/Docs/` (D-P12-02)

Pinoy Loan Manager is a **separate first-class ExItS SaaS product**, a sibling of PinoyBusinessPOS, not a POS module, feature, or database extension.

---

## Canonical documents

| Doc | Description |
|---|---|
| [product-definition.md](product-definition.md) | Purpose, ownership, boundaries, exclusions |
| [architecture.md](architecture.md) | System, data, Personal/Borrower, and client boundaries |
| [security.md](security.md) | Security, privacy, consent |
| [authorization-matrix.md](authorization-matrix.md) | Access layers; roles/grants remain open |
| [development-plan.md](development-plan.md) | Delivery buckets and testing expectations |
| [roadmap.md](roadmap.md) | Phases and work packages |
| [risks-and-decisions.md](risks-and-decisions.md) | Open risks and decisions |
| [FILE-MANIFEST.md](FILE-MANIFEST.md) | Path inventory |

Category folders below are indexes only. They must not become a second source of truth.

---

## Category indexes

| Directory | Purpose |
|---|---|
| [Product/](Product/README.md) | **WHAT** — points to [product-definition.md](product-definition.md) |
| [Architecture/](Architecture/README.md) | **HOW** — points to [architecture.md](architecture.md) |
| [Security/](Security/README.md) | Access and privacy — points to [security.md](security.md) and [authorization-matrix.md](authorization-matrix.md) |
| [Decisions/](Decisions/README.md) | Future ADRs — register is [risks-and-decisions.md](risks-and-decisions.md) |
| [Phases/](Phases/README.md) | Sequencing — points to [roadmap.md](roadmap.md) and [development-plan.md](development-plan.md) |
| [Reports/](Reports/README.md) | Completed work-package evidence |
| [Validation/](Validation/README.md) | Owner/device/browser/calculation evidence |
| [Operations/](Operations/README.md) | Deployment and production operations |

Do not scatter Pinoy Loan Manager documentation into the repository-root `docs/` tree unless the content is genuinely portfolio-wide.

---

## Identity (proposed)

| Item | Value | Status |
|---|---|---|
| Display name | Pinoy Loan Manager | Recorded |
| Repository directory | `PinoyLoanManager` | Recorded |
| Product code / slug | `pinoy-loan-manager` | Open (PLM-D-00-01) |
| Future database | `ExItS_PinoyLoanManager` | Open (PLM-D-00-02) |

---

## Ownership (summary)

Platform owns identity, organizations, memberships, catalog, plans, subscriptions, entitlements, SaaS billing, Platform administration, and Platform audit.

Pinoy Loan Manager will own borrower records, loan-domain state, operational financial state, product-local authorization, product database/migrations, API, Web UI, MAUI UI, reports, and product audit/history.

Isolation: independent subscription; separate database; no cross-product FKs; no direct POS or Platform table reads; OrganizationId as identifier only; approved contracts/APIs only; SaaS billing ≠ Loan operational money.

Authoritative text: [product-definition.md](product-definition.md) and [architecture.md](architecture.md).

---

## Personal / Borrower

ExItS Personal is Platform-owned and product-neutral. POS Customer ≠ Loan Borrower. Linking is optional, consent-required, and never auto-activated from EX ID / QR resolution. Authoritative text: [architecture.md](architecture.md).

---

## Client direction (proposed)

Web: Blazor Web. Mobile/Desktop: .NET MAUI Blazor Hybrid. No client project is authorized.

---

## Explicit exclusions

No implementation exists. Loan calculation and collections policy are not defined (PLM-D-00-08). Do not copy PinoyBusinessPOS roles or money models.
