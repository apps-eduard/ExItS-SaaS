# {{PRODUCT_NAME}} — File Manifest / Documentation Index

> Template: P12-WP03. List authoritative product docs and tracked source roots.
> Foundation: [exits-product-foundation-reference.md](../exits-product-foundation-reference.md)

| Field | Value |
|---|---|
| Product | {{PRODUCT_NAME}} |
| Last updated | {{DATE}} |

## Authoritative docs (`src/Products/{{PRODUCT_NAME}}/Docs/` or documented dry-run path)

| Path | Purpose | Status |
|---|---|---|
| `product-definition.md` | Overview / boundaries | {{STATUS}} |
| `architecture.md` | Architecture | {{STATUS}} |
| `security.md` | Security / privacy | {{STATUS}} |
| `authorization-matrix.md` | Roles / grants | {{STATUS}} |
| `development-plan.md` | Delivery plan | {{STATUS}} |
| `roadmap.md` | Phases / WPs | {{STATUS}} |
| `risks-and-decisions.md` | Risks / decisions | {{STATUS}} |
| `deployment-notes.md` | Deploy notes | {{STATUS}} / N/A |
| `README.md` | Doc index | {{STATUS}} / N/A |
| `reports/` | WP reports | {{STATUS}} |

## Source roots (high level)

| Path | Role |
|---|---|
| `src/Products/{{PRODUCT_NAME}}/` | Product code (omit / mark forbidden for docs-only dry runs) |
| {{EXTRA_PATH}} | {{EXTRA_ROLE}} |

## Explicitly not in this product tree

- Platform operational ownership
- Other products’ databases / domains
- Customer-specific forks

## Notes

{{MANIFEST_NOTES}}
