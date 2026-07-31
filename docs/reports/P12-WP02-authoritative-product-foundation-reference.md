# P12-WP02 — Authoritative Product Foundation Reference

Phase marker: `P12-WP02-authoritative-product-foundation-reference`

Package: **P12-WP02 — Authoritative Product Foundation Reference**  
Prior tip: `9f239d35ec659fcbb15edd933497031228d2537a`  
Docs tip: *(recorded after docs commit)*

## Status

**Complete.** Documentation-only. Finalized the authoritative Product Foundation reference for future ExItS products. No application code, projects, packages, migrations, APIs, UI, containers, CI/CD, or product scaffolds were added. Phase 11 Admin UI was not modified.

Exact next: **P12-WP03 — Product Documentation Templates** (do not begin until authorized).

## Foundation path

**Authoritative file:** `docs/Product-Foundation/exits-product-foundation-reference.md`  
**Index:** `docs/Product-Foundation/README.md`

## Delivered scope

- Concise Platform vs product ownership, isolation, subscription/authz, financial, shared-asset, deployment, docs-layout, context-loading, bootstrap checklist, examples/anti-patterns
- Labels distinguish **Implemented** / **Required** / **Unresolved** / **Example**
- Stale draft wording reconciled against P12-WP01 and current Platform/POS evidence
- Phase 12 roadmap path updated to the resolved file name (no longer `exits-product-foundation.md`)

## Contracts locked

1. Platform owns SaaS administration, not operational workflows  
2. Independent subscription per product  
3. Independent database per product + separate Platform DB  
4. No direct product↔Platform operational table access; Guid org IDs only  
5. Platform product access ≠ product operational permission  
6. Product-local roles/grants authoritative inside the product  
7. SaaS billing money ≠ product operational money  
8. Share technical primitives only — not authoritative domain state  
9. Independently versioned product images; config not forks  
10. Narrow context-loading rule for product work  
11. No PHI unless a product explicitly designs for it  
12. Scope gate: do not invent missing product-owner policy  

## Decisions resolved / open

| ID | Outcome |
|---|---|
| **D-P12-01** | **Closed** — `docs/Product-Foundation/exits-product-foundation-reference.md` |
| **D-P12-02** | **Closed (intent)** — new products use `src/Products/<Name>/Docs/`; POS historical docs remain under portfolio `docs/`; templates in WP03 |
| **D-P12-03** | **Open / provisional** — POS Dev commercial headers documented; final Platform→product transport not invented |
| **R-091** | **Open** — production authentication still blocked |
| **D-P12-04** | **Open** — engineering-matrix hygiene deferred |

## Context-loading rule

For product WPs, read only: workflow rules → this foundation reference → active product docs → current WP prompt → files needed for the task. Do not routinely scan unrelated products, old reports, full Platform history, or HealthCare product content. Permanent `.mdc` packaging remains **P12-WP04**.

## Validation

| Check | Result |
|---|---|
| Matches P12-WP01 audit | Pass |
| Aligned with Platform/POS isolation | Pass |
| No HealthCare product dependency | Pass |
| No Phase 11 / app / infrastructure changes | Pass |
| No scaffold / `_ProductTemplate` | Pass |
| Paths resolve under `docs/Product-Foundation/` | Pass |
| Release tests | **1186 passed / 0 failed / 0 skipped** (baseline retained) |

## Explicit exclusions

- Product documentation templates (P12-WP03)  
- Cursor context rule `.mdc` packaging (P12-WP04)  
- Bootstrap prompt / dry run / skeleton (later WPs)  
- Final commercial-state transport implementation  
- Production authentication  

## Files changed

- `docs/Product-Foundation/exits-product-foundation-reference.md` (finalized, now tracked)
- `docs/Product-Foundation/README.md` (new index)
- `docs/reports/P12-WP02-authoritative-product-foundation-reference.md` (this report)
- `docs/phases/phase-12-product-foundation-and-bootstrap.md`
- `docs/portfolio-progress.md`
- `docs/phases/README.md` / `docs/reports/README.md` / `README.md` / `FILE-MANIFEST.md` as required
- `docs/risks-and-issues.md` (brief Phase 12 note)

## Remaining drafts

None under Product-Foundation after this WP (authoritative reference + README tracked). Template files are not yet created (WP03).

## Exact next

**P12-WP03 — Product Documentation Templates** when explicitly authorized. Do not begin P12-WP03.
