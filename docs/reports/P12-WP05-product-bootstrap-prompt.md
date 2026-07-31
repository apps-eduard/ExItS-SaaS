# P12-WP05 — Product Bootstrap Prompt

Phase marker: `P12-WP05-product-bootstrap-prompt`

Package: **P12-WP05 — Product Bootstrap Prompt**
Prior tip: `c348b4a3cbc049d330e5d5aa5e878a2cdcf676ef`
Docs tip: *(recorded after docs commit)*

## Status

**Complete.** Documentation only. Published a copy-paste Cursor bootstrap prompt for future product documentation baselines. No product folders, code, scaffolds, or infrastructure were created.

Exact next: **P12-WP06 — Reference Product Dry Run** (do not begin until authorized).

## Prompt path

`docs/Product-Foundation/product-bootstrap-prompt.md`

Indexed from `docs/Product-Foundation/README.md` and Phase 12.

## Required inputs

Product name; identifier/slug; purpose/users; independent subscription; DB name/schema; API/web/mobile ownership; roles/grants; operational money; privacy classification; integrations; deployment image name; MVP inclusions/exclusions; unresolved decisions. Prompt asks only for missing items.

## Default output

Under `src/Products/<ProductName>/Docs/` (when a future authorized bootstrap runs):

- product-definition, architecture, security, authorization-matrix, development-plan, roadmap, risks-and-decisions, FILE-MANIFEST, README
- deployment-notes only if in scope
- WP report only if first WP explicitly authorized
- Proposed first implementation WP — **no code**

## Scope gate

Docs → validate placeholders/decisions → propose first WP → **stop**. Forbidden unless separately authorized: `.csproj`, solution entries, entities, APIs, migrations, UI, Docker, CI/CD.

## Dry-run result (SampleProduct — documentation exercise only)

Fictional product: **SampleProduct** / code `SampleProduct`.  
**Did not** create `src/Products/SampleProduct/` or any files under `src/Products/`.

| Check | Result |
|---|---|
| Missing decisions would be requested | Pass — prompt requires fill-in; inventing policy forbidden |
| Template paths correct | Pass — `docs/Product-Foundation/Templates/*` → `Docs/*` |
| Stops before implementation | Pass — Authorize implementation: no; scope gate stop |
| Does not copy POS | Pass — explicit prohibition; no POS scan |
| Preserves D-P12-03 and R-091 | Pass — must appear in risks-and-decisions as open |
| First-WP recommendation | Pass — propose only; example would be `SP-WP01 — Domain baseline and persistence skeleton` (not executed) |
| Context efficiency | Pass — load workflow + product-context + foundation + templates only |

Hypothetical first WP (not started): documentation-approved domain baseline / persistence skeleton when separately authorized — still no POS copy.

## Unresolved decisions preserved

| ID | Handling |
|---|---|
| D-P12-03 | Open — record in product risks; do not invent transport |
| R-091 | Open — do not invent production auth |

## Validation

| Check | Result |
|---|---|
| No code / scaffold / SampleProduct folder | Pass (`Test-Path src/Products/SampleProduct` = False) |
| No Phase 11 / HealthCare dependency | Pass |
| Links resolve | Pass |
| Release tests | **1186 passed / 0 failed / 0 skipped** |

## Files changed

- `docs/Product-Foundation/product-bootstrap-prompt.md`
- `docs/Product-Foundation/README.md` (+ Templates/foundation cross-links as needed)
- `docs/reports/P12-WP05-product-bootstrap-prompt.md`
- Phase 12 / portfolio / manifests / indexes

## Exact next

**P12-WP06 — Reference Product Dry Run** when explicitly authorized. Do not begin P12-WP06.
