# Pinoy Loan Manager — File Manifest

**Status:** Foundation / planning only
**Implementation present:** No
**Current work package:** PLM-00-WP02 — Product Definition & Architecture Baseline

This file is the navigation map for future Cursor work. Load this product’s `Docs/` after the shared Product Foundation reference. Do not scan PinoyBusinessPOS implementation by default.

Shared contracts to load with this product:

- `.cursor/rules/exits-workflow.mdc`
- `.cursor/rules/exits-product-context.mdc`
- `docs/Product-Foundation/exits-product-foundation-reference.md`

---

## Canonical documents (PLM-00-WP02)

| Path | Purpose | Status | Implementation present |
|---|---|---|---|
| `Docs/product-definition.md` | Product identity, ownership, boundaries, exclusions | Foundation / Planning Only | No |
| `Docs/architecture.md` | Technical and data boundaries; Personal/Borrower intent | Foundation / Planning Only | No |
| `Docs/security.md` | Security, privacy, consent | Foundation / Planning Only | No |
| `Docs/authorization-matrix.md` | Access intersection; roles/grants open | Foundation / Planning Only | No |
| `Docs/development-plan.md` | Delivery buckets PLM-00–PLM-14 | Foundation / Planning Only | No |
| `Docs/roadmap.md` | Current phase and work-package sequence | Foundation / Planning Only | No |
| `Docs/risks-and-decisions.md` | Open risks and decisions | Foundation / Planning Only | No |

## Workspace indexes (PLM-00-WP01, updated in WP02)

| Path | Purpose | Status | Implementation present |
|---|---|---|---|
| `src/Products/PinoyLoanManager/` | Product workspace root | Foundation / Planning Only | No |
| `src/Products/PinoyLoanManager/Docs/` | Authoritative product documentation root (D-P12-02) | Foundation / Planning Only | No |
| `Docs/README.md` | Index to canonical documents | Foundation / Planning Only | No |
| `Docs/FILE-MANIFEST.md` | This navigation map | Foundation / Planning Only | No |
| `Docs/Product/README.md` | Index for product-policy docs | Foundation / Planning Only | No |
| `Docs/Architecture/README.md` | Index for architecture docs | Foundation / Planning Only | No |
| `Docs/Security/README.md` | Index for security docs | Foundation / Planning Only | No |
| `Docs/Decisions/README.md` | Index for future ADRs | Foundation / Planning Only | No |
| `Docs/Phases/README.md` | Index for phase sequencing | Foundation / Planning Only | No |
| `Docs/Reports/README.md` | Index for WP evidence | Foundation / Planning Only | No |
| `Docs/Validation/README.md` | Index for validation evidence | Foundation / Planning Only | No |
| `Docs/Operations/README.md` | Index for operations docs | Foundation / Planning Only | No |

---

## Not present (intentionally)

| Item | Reason |
|---|---|
| `ExItS.PinoyLoanManager.Domain` (and other .NET projects) | Code projects not authorized |
| Test projects | Not authorized |
| Database / migration folders | Persistence not authorized |
| Docker / deploy implementation | Not authorized |
| `ExItS.slnx` entries | Not authorized |
| `Docs/deployment-notes.md` | Optional until packaging |
| `Docs/Reports/<WP-id>.md` | In-tree WP report not required for this docs-only WP |
| Filled Loan policy (interest, amortization, penalties, …) | Owner decision (PLM-D-00-08) |
