# {{PRODUCT_NAME}} — Risks and Decisions

> Template: P12-WP03. Foundation: [exits-product-foundation-reference.md](../../exits-product-foundation-reference.md)  
> Close items only with evidence. Do not invent answers for portfolio-open items.

| Field | Value |
|---|---|
| Product | {{PRODUCT_NAME}} |
| Last updated | {{DATE}} |

## Portfolio items (always preserve until closed upstream)

| ID | Type | Description | Current state | Impact | Decision point | Resolution criteria |
|---|---|---|---|---|---|---|
| R-091 | Risk | Production authentication missing | Open | No production-secure identity | Portfolio auth roadmap | Real Platform auth shipped + evidenced |
| D-P12-03 | Decision | Commercial-state transport to products | Open / provisional | How product learns subscription/entitlements without Platform table reads | Commercial/integration WP | Approved contract + implementation; no direct Platform EF/SQL |

## Product register

| ID | Type (Risk/Decision/Assumption) | Description | Current state | Impact | Owner / decision point | Evidence | Resolution criteria |
|---|---|---|---|---|---|---|---|
| {{ID}} | {{TYPE}} | {{DESC}} | Open / Mitigated / Closed | {{IMPACT}} | {{OWNER}} | {{EVIDENCE}} | {{CRITERIA}} |

## Instructions

- Prefer stable IDs (`R-…`, `D-…`, `A-…`).
- “Closed” requires repository or operator evidence.
- Unresolved `{{…}}` placeholders in approved docs must appear here as open decisions.
