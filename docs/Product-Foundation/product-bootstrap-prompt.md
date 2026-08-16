# ExItS Product Bootstrap Prompt

**Copy everything below the line into Cursor** when authorized to start a **new** product’s documentation baseline.
Do not run this prompt until product bootstrap is explicitly authorized.

---

```text
Follow:
- `.cursor/rules/exits-workflow.mdc`
- `.cursor/rules/exits-product-context.mdc`
- `docs/Product-Foundation/exits-product-foundation-reference.md`

## Task

Bootstrap documentation only for a new ExItS product.

Create product docs under `src/Products/<ProductName>/Docs/` by copying and filling templates from `docs/Product-Foundation/Templates/`.

Stop after documentation. Do not implement code, projects, solution entries, entities, APIs, migrations, UI, Docker, CI/CD, or infrastructure unless a later prompt explicitly authorizes them.

## Context to load (only)

1. The three files above
2. `docs/Product-Foundation/Templates/` (README + templates you will copy)
3. Files you create under the new product Docs/
4. Nothing else by default

Do not scan: PinoyBusinessPOS / other products, `docs/reports/` history, full Platform, all migrations/tests, removed foreign product content, build artifacts.

Do not copy POS domain entities, roles, money models, or architecture into the new product. Use templates + product-owner inputs only.

## Required inputs (ask only what is missing)

Product name:
Product identifier / Platform product code / slug:
Purpose and target users:
Independent subscription boundary (confirm separate from other products):
Database name and schema:
API / web / mobile ownership:
Product-local roles and grants (summary):
Operational-money definition (not Platform SaaS billing):
Privacy / data classification (PHI default: none):
External integrations:
Deployment image name (planned):
Explicit MVP inclusions:
Explicit exclusions:
Unresolved decisions:

If any required input is missing, ask the minimum questions, then continue. Do not invent Loan/Pawnshop/BNPL/retail policy.

## Safeguards (enforce)

- Platform owns SaaS administration; product owns operational domain/workflows
- Independent subscription; separate DB + migrations; Guid org refs only
- No direct Platform table reads; no cross-product FKs
- Product-local roles/grants; Platform access ≠ operational permission
- SaaS billing money ≠ product operational money
- PHI defaults to none unless explicitly authorized
- No customer-specific source forks; server-authoritative rules
- Do not invent answers for D-P12-03 (commercial-state transport) or R-091 (production auth) — record as open decisions

## Create these docs (from templates)

Mandatory (replace every `{{PLACEHOLDER}}`):

- `product-definition.md` ← Templates/product-definition.md
- `architecture.md` ← Templates/architecture.md
- `security.md` ← Templates/security.md
- `authorization-matrix.md` ← Templates/authorization-matrix.md
- `development-plan.md` ← Templates/development-plan.md
- `roadmap.md` ← Templates/roadmap.md
- `risks-and-decisions.md` ← Templates/risks-and-decisions.md (include R-091 + D-P12-03)
- `FILE-MANIFEST.md` ← Templates/FILE-MANIFEST.md
- `README.md` ← Templates/product-docs-readme.md

Optional: `deployment-notes.md` only if deployment is in the provided inputs/scope.

Do not create `Docs/reports/<WP>.md` unless a first implementation work package is explicitly authorized in this same prompt.

## Scope gate

After docs are written:

1. Validate no leftover required `{{PLACEHOLDER}}` without an open decision entry
2. Propose the first implementation work package name/objective only
3. Stop — do not start implementation
4. Report: files created; open decisions; proposed first WP; confirmation that no code/scaffold was added; Git status if commits were authorized

## Fill-in block (user)

Product name:
Product code/slug:
Purpose / users:
DB name / schema:
Surfaces (API/web/mobile):
Roles/grants summary:
Operational money:
Privacy (PHI?):
Integrations:
Image name:
MVP in:
MVP out:
Open decisions:
Authorize docs-only bootstrap: yes
Authorize implementation: no
```

---

## Usage notes

- Paste the fenced prompt into Cursor; complete the fill-in block first when possible.
- Default outcome: docs only + proposed first WP + open decisions preserved.
- Implementation, solution projects, and folder scaffolding beyond `Docs/` require a separate authorized work package.
