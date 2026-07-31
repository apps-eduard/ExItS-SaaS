# Product Documentation Templates

Copy templates into a **new** product’s docs root:

```text
src/Products/<ProductName>/Docs/
```

Authoritative contract (always load with these docs):

- [../exits-product-foundation-reference.md](../exits-product-foundation-reference.md)
- `.cursor/rules/exits-workflow.mdc`

Do **not** create product folders or fill templates for a product until that product is authorized. Do **not** copy PinoyBusinessPOS source or history to invent policy.

Authorized docs-only bootstrap: use [../product-bootstrap-prompt.md](../product-bootstrap-prompt.md).

## Placeholder convention

| Form | Meaning |
|---|---|
| `{{LIKE_THIS}}` | Required replacement before the doc is approved |
| `[Optional: …]` | Fill only when applicable |
| `DECISION:` | Must be resolved by product owner — do not invent |

Approved product docs must not retain unresolved `{{…}}` or open `DECISION:` items without an explicit open-decision entry.

## Mandatory vs optional

| Template | Usage | Copied product path |
|---|---|---|
| [product-definition.md](product-definition.md) | **Mandatory** | `Docs/product-definition.md` |
| [architecture.md](architecture.md) | **Mandatory** | `Docs/architecture.md` |
| [security.md](security.md) | **Mandatory** | `Docs/security.md` |
| [authorization-matrix.md](authorization-matrix.md) | **Mandatory** | `Docs/authorization-matrix.md` |
| [development-plan.md](development-plan.md) | **Mandatory** | `Docs/development-plan.md` |
| [roadmap.md](roadmap.md) | **Mandatory** | `Docs/roadmap.md` |
| [FILE-MANIFEST.md](FILE-MANIFEST.md) | **Mandatory** | `Docs/FILE-MANIFEST.md` |
| [risks-and-decisions.md](risks-and-decisions.md) | **Mandatory** | `Docs/risks-and-decisions.md` |
| [work-package-report.md](work-package-report.md) | **Mandatory** per WP (copy per report) | `Docs/reports/<WP-id>.md` |
| [deployment-notes.md](deployment-notes.md) | **Optional** until first deployable packaging WP | `Docs/deployment-notes.md` |
| [product-docs-readme.md](product-docs-readme.md) | **Optional** index | `Docs/README.md` |

## Context loading

For product work, load only: workflow rules → `.cursor/rules/exits-product-context.mdc` → foundation reference → this product’s filled docs → current WP prompt → files needed for the task. Do not scan unrelated products or historical reports by default.

## Safeguards embedded in every template

Independent subscription; separate product DB; no Platform table reads; no cross-product FKs; product-local roles; SaaS billing ≠ operational money; org Guid as identifier only; PHI default none; no customer forks; server-authoritative rules; open items for **D-P12-03** and **R-091**.
