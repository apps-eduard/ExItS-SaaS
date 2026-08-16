# P12-WP03 — Product Documentation Templates

Phase marker: `P12-WP03-product-documentation-templates`

Package: **P12-WP03 — Product Documentation Templates**
Prior tip: `c19605cc27e8711d08eede0256ef4efd20df0774`
Docs tip: `65b02a1dd9336b39b79fc41527969f6289ad7072`

## Status

**Complete.** Documentation-only. Reusable Markdown templates for future ExItS products are under `docs/Product-Foundation/Templates/`. No application code, scaffolds, Docker, CI/CD, or `src/Products/<NewProduct>/` trees were added.

Exact next: **P12-WP04 — Cursor Product Context Rule** (do not begin until authorized).

## Templates created

| File | Purpose |
|---|---|
| `Templates/README.md` | Usage, mandatory/optional, copy target, placeholders |
| `product-definition.md` | Overview / boundaries |
| `architecture.md` | System / data / deploy boundary |
| `development-plan.md` | Phases, WP format, tests, readiness |
| `security.md` | Security and privacy |
| `authorization-matrix.md` | Roles, grants, commercial intersection |
| `roadmap.md` | Phase / WP plan |
| `work-package-report.md` | Per-WP completion report |
| `risks-and-decisions.md` | Risk / decision register |
| `deployment-notes.md` | Packaging notes (docs only) |
| `FILE-MANIFEST.md` | Doc / path inventory |
| `product-docs-readme.md` | Optional product Docs index |

Copy target for new products: `src/Products/<Name>/Docs/` (D-P12-02). No product created in this WP.

## Mandatory vs optional

| Mandatory | Optional |
|---|---|
| product-definition, architecture, security, authorization-matrix, development-plan, roadmap, FILE-MANIFEST, risks-and-decisions, work-package-report (per WP) | deployment-notes (until packaging WP), product-docs-readme |

## Safeguards embedded

Independent subscription; separate DB; no Platform table reads / cross-product FKs; product-local roles; SaaS ≠ operational money; org Guid only; PHI default none; no customer forks; server-authoritative rules; foundation links instead of duplicated prose; `{{PLACEHOLDER}}` / `DECISION:` gates.

## Unresolved decisions preserved

| ID | Handling in templates |
|---|---|
| **D-P12-03** | Marked DECISION; provisional note placeholder only |
| **R-091** | Listed open; no fake production auth |
| Product-specific policy | Scope gate — stop and record; do not invent |

## Validation

| Check | Result |
|---|---|
| Links to foundation resolve from Templates | Pass |
| Aligns with P12-WP01 / WP02 | Pass |
| No app / infra / scaffold | Pass |
| No unrelated product dependency | Pass |
| Release tests | **1186 passed / 0 failed / 0 skipped** |

## Files changed

- `docs/Product-Foundation/Templates/**` (new)
- `docs/Product-Foundation/README.md`
- `docs/reports/P12-WP03-product-documentation-templates.md`
- Phase 12 / portfolio / manifests / indexes as required

## Exact next

**P12-WP04 — Cursor Product Context Rule** when explicitly authorized. Do not begin P12-WP04.
