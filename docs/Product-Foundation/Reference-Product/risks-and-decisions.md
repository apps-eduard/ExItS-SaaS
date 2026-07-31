# ReferenceLoan — Risks and Decisions

> **FICTIONAL** P12-WP06. Foundation: [exits-product-foundation-reference.md](../exits-product-foundation-reference.md)  
> Close items only with evidence. Do not invent answers for portfolio-open items.

| Field | Value |
|---|---|
| Product | ReferenceLoan |
| Last updated | 2026-07-31 |

## Portfolio items (always preserve until closed upstream)

| ID | Type | Description | Current state | Impact | Decision point | Resolution criteria |
|---|---|---|---|---|---|---|
| R-091 | Risk | Production authentication missing | Open | No production-secure identity | Portfolio auth roadmap | Real Platform auth shipped + evidenced |
| D-P12-03 | Decision | Commercial-state transport to products | Open / provisional | How product learns subscription/entitlements without Platform table reads | Commercial/integration WP | Approved contract + implementation; no direct Platform EF/SQL |

## Product register

| ID | Type (Risk/Decision/Assumption) | Description | Current state | Impact | Owner / decision point | Evidence | Resolution criteria |
|---|---|---|---|---|---|---|---|
| RL-D-01 | Decision | Real lending domain / regulatory policy | Open | Blocks any real MVP | Product owner | None — intentionally blank | Written product-owner policy |
| RL-A-01 | Assumption | This pack is fiction for foundation validation | Open | Misread as real product | Maintainers | This file + README | Keep banner; never add src tree without WP |
| RL-R-01 | Risk | Premature implementation from dry-run docs | Mitigated | Accidental scaffold | P12-WP06 scope gate | No src/Products/ReferenceLoan | Keep docs-only |
