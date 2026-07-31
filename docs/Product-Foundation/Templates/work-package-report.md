# {{WP_ID}} — {{WP_TITLE}}

> Template: P12-WP03 work-package report. Foundation: [exits-product-foundation-reference.md](../exits-product-foundation-reference.md)

| Field | Value |
|---|---|
| Product | {{PRODUCT_NAME}} |
| Package | {{WP_ID}} — {{WP_TITLE}} |
| Prior tip | `{{PRIOR_TIP}}` |
| Feature / docs tip | `{{TIP}}` |

## Status

{{STATUS}} — Complete / Complete with risks / Blocked / …

Exact next: **{{NEXT_WP}}** (do not begin until authorized).

## Delivered scope

- {{DELIVERED_1}}

## Explicit exclusions

- {{EXCLUSION_1}}

## Architecture / business boundaries preserved

| Boundary | Status |
|---|---|
| Separate product DB / no cross-product FKs | Preserved / … |
| No direct Platform table reads | Preserved / … |
| Platform access ≠ product ops permission | Preserved / … |
| SaaS billing ≠ operational money | Preserved / … |
| Server-authoritative rules | Preserved / … |
| R-091 / D-P12-03 honesty | Not falsely closed |

## Persistence / migrations

{{MIGRATIONS_OR_NONE}}

## API / UI impact

{{API_UI_IMPACT}}

## Tests

| Suite | Result |
|---|---|
| {{SUITE}} | {{PASSED}} passed / {{FAILED}} failed / {{SKIPPED}} skipped |

## Browser / device evidence

[Optional: {{EVIDENCE}} — do not claim if not run]

## Remaining risks / open decisions

| ID | Notes |
|---|---|
| {{RISK_OR_DEC}} | {{NOTES}} |

## Files changed

- {{FILE_1}}

## Git

| Field | Value |
|---|---|
| Commits | {{COMMITS}} |
| Final tip | `{{FINAL_TIP}}` |
| `main = origin/main` | Yes / No |

## Exact next

**{{NEXT_WP}}**
