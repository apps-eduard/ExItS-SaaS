# P12-WP06 — Reference Product Dry Run

Phase marker: `P12-WP06-reference-product-dry-run`

Package: **P12-WP06 — Reference Product Dry Run**
Prior tip: `51026a07faf7e5a69674c5c7a0f1379c240d9d7f`
Docs tip: `5debab509c52ecdbed1cf9bba1ec02147ece693b`

## Status

**Complete.** Documentation-only validation of the Phase 12 foundation using fictional **ReferenceLoan**. No `src/Products/ReferenceLoan/`, no application code, no POS copying.

Exact next: **P12-WP07 — Foundation Hardening and Closeout** (do not begin until authorized).

## Fictional input set

| Input | Value |
|---|---|
| Name | ReferenceLoan |
| Code/slug | `reference-loan` |
| Purpose | Sample lending workflow product (illustrative) |
| Users | Internal lending staff and borrowers (labels only) |
| Subscription | Independent Platform subscription |
| Database / schema | `ExItS_ReferenceLoan` / `loan` |
| Surfaces | Product-owned API / web / mobile |
| Roles | LoanOfficer, LoanViewer (sample product-local only) |
| Operational money | Principal, fees, disbursements, repayments |
| Privacy | No PHI; PII + operational financial |
| Integrations | None |
| Image | `exits-reference-loan` |
| MVP | Documentation validation only |
| Open | R-091, D-P12-03, RL-D-01 (domain policy) |

## Generated documentation set

Retained under `docs/Product-Foundation/Reference-Product/`:

- README.md, product-definition.md, architecture.md, security.md, authorization-matrix.md
- development-plan.md, roadmap.md, risks-and-decisions.md, FILE-MANIFEST.md
- Deployment notes omitted (optional)
- No WP report (implementation not authorized)

## Template / bootstrap findings

| Finding | Severity | Disposition |
|---|---|---|
| Foundation links in templates used `../../exits-product-foundation-reference.md` (resolved under `docs/`, broken) | Defect | **Fixed** → `../exits-product-foundation-reference.md` + README note for `src/Products/.../Docs/` copies |
| `development-plan` WP report path said `Templates/...` ambiguously | Minor | **Fixed** → repo path `docs/Product-Foundation/Templates/work-package-report.md` |
| Docs root / FILE-MANIFEST assumed only `src/Products/...` | Minor | **Fixed** — note dry-run / forbid src for fiction |
| Bootstrap prompt sufficient for docs-only pack | — | Pass — followed prompt; no POS scan |
| Context rule prevented POS/history scan | — | Pass — only foundation, templates, Reference-Product |
| Templates encourage premature implementation | — | Pass — scope gates + “do not invent”; dry run stopped before code |
| Mandatory templates usable without structural change | — | Pass after link fixes |
| Redundant overlap | — | development-plan vs roadmap slightly overlap by design; acceptable |

## Defects fixed

1. Template foundation relative links
2. Development-plan report template path
3. product-definition / FILE-MANIFEST docs-root notes for dry runs

## Context-efficiency result

Loaded: workflow + product-context rules, foundation, templates, generated Reference-Product docs.
Did **not** open POS source, historical reports tree, or foreign product content.

## Cleanup / retention decision

**Outcome 1 — retain** under `docs/Product-Foundation/Reference-Product/` with FICTIONAL banners and explicit “must not exist: `src/Products/ReferenceLoan/`”.
Reason: durable proof artifact for closeout; clearly not an active product tree.

## Validation

| Check | Result |
|---|---|
| No `src/Products/ReferenceLoan/` | Pass |
| No `{{PLACEHOLDER}}` in retained example | Pass |
| R-091 / D-P12-03 open | Pass |
| No legacy product / POS copy | Pass |
| Release tests | **1186 passed / 0 failed / 0 skipped** |

## Exact next

**P12-WP07 — Foundation Hardening and Closeout** when explicitly authorized. Do not begin P12-WP07.
