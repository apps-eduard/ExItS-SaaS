# PLM-DOC-01 — Product Identity, Borrower Identity & Personal Linking

**Status:** Documentation package complete (planning only)
**Implementation present:** No
**Last updated:** 2026-08-19
**Branch:** `docs/plm-final-decisions`

Runtime / browser / device / database / production validation: **Not Applicable**.

---

## Scope

Finalize Pinoy Loan Manager **product identity**, **logical database name**, **Borrower ownership**, **Personal cardinality**, **organization-initiated MVP linking**, **consent lifecycle**, **unlink/relink**, **duplicate handling**, and **Personal data minimization**.

Explicitly **out of scope:** code, database creation, migrations, APIs, UI, solution changes, BNPL, financial formulas, Platform relationship schema, D-P12-03, legal compliance claims.

---

## Accepted decisions

| Topic | Outcome |
|---|---|
| Display name | Pinoy Loan Manager |
| Directory | `PinoyLoanManager` |
| Product code / slug | `pinoy-loan-manager` |
| Logical database name | `ExItS_PinoyLoanManager` |
| Hosting | Same product code and data authority across hosted / dedicated / on-prem |
| Borrower | PLM-owned, one Organization, optional Branch, may exist without Personal/POS/Loan |
| Cardinality | One active Personal per Borrower per org; one active Borrower per Personal per org; separate Borrowers across orgs |
| MVP linking | Organization-initiated; Personal consent required; Personal self-claim not MVP |
| Auto-link | **Not allowed** |
| Unlink | Does not delete operational/financial records; blocks new Personal-delivered offers |
| Relink | New request + new consent; identity change is high-risk |
| Duplicates | Warn/review; **no auto-merge** in MVP |
| Cross-lender visibility | **Not allowed** |

Canonical: [../Product/borrower-identity-and-duplicate-policy.md](../Product/borrower-identity-and-duplicate-policy.md), [../Product/personal-linking-lifecycle-and-visibility.md](../Product/personal-linking-lifecycle-and-visibility.md), [../Decisions/ADR-001-product-identity-and-database-name.md](../Decisions/ADR-001-product-identity-and-database-name.md), [../Decisions/ADR-002-borrower-personal-cardinality-and-consent.md](../Decisions/ADR-002-borrower-personal-cardinality-and-consent.md).

---

## Explicitly deferred implementation

- Platform catalog registration of `pinoy-loan-manager`
- Database creation, schema, connections, partitions, stamps, backups, migrations
- Platform relationship tables / APIs (PLM-D-00-04, PLM-D-00-05)
- Preview field list, expiry duration, grant identifiers
- Borrower merge workflow
- Personal self-service claim
- Legal visibility/retention basis (PLM-D-00-11)
- Two-person approval policy (PLM-D-00-13)
- Product implementation (remains **paused**; `feat/plm-01-scaffold` unmerged)

---

## Decision register

| ID | Outcome |
|---|---|
| PLM-D-00-01 | **Closed** — `pinoy-loan-manager` |
| PLM-D-00-02 | **Closed for logical name** — `ExItS_PinoyLoanManager`; creation/placement deferred |
| PLM-D-00-03 | **Open** — parked scaffold is not mainline |
| PLM-D-00-04 | **Open** — Platform relationship contract/schema not designed |
| PLM-D-00-05 | **Open** — product behavior defined; transport/persistence/integration not designed |
| PLM-D-00-10 | **Closed / Product Owner Accepted** (documentation baseline) |
| PLM-D-00-11 | **Open** |
| PLM-D-00-13 | **Open** |

Other PLM-D-00 items and D-P12-03 / R-091 / D-P12-05 remain as previously recorded.

---

## Files changed

Created: Product identity/linking docs, ADR-001, ADR-002, this report.

Updated: product definition, architecture, security, authorization, development plan, roadmap, risks, README, FILE-MANIFEST, indexes, borrower/linking/publishing/personal-boundary docs, persistence naming.

BNPL removed from **active** PLM product-plan wording. Historical closeout reports not rewritten.

---

## Validation

Documentation only. `git diff --check` recorded at commit time.

No `.cs`, `.csproj`, `ExItS.slnx`, migrations, APIs, UI, tests, POS, Platform implementation, or parked-scaffold changes.

---

## Git evidence

Recorded in the PLM-DOC-01 commit on `docs/plm-final-decisions`. Parked scaffold `feat/plm-01-scaffold` @ `4ec9e96e9149cd8d014adde3d694872a6d5ef576` not modified.

---

## Exact next documentation package

**PLM-DOC-02 — Financial Calculation, Fees & Payment Allocation Decisions**

Do not start PLM-DOC-02 in this package. Implementation remains paused.
