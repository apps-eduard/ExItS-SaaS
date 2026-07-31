# ReferenceLoan — Roadmap / Phase Plan

> **FICTIONAL** P12-WP06. Foundation: [exits-product-foundation-reference.md](../exits-product-foundation-reference.md)

| Field | Value |
|---|---|
| Product | ReferenceLoan |
| Current phase | RL-P0 Docs (complete for dry run) |
| Status | Draft — fictional validation only |

## Phase objective

Validate that Product Foundation templates and bootstrap prompt can produce a coherent documentation pack without POS copying or policy invention.

## Scope

### Included

- Documentation-only dry run under `docs/Product-Foundation/Reference-Product/`

### Excluded

- Source projects, APIs, migrations, UI, Docker, CI/CD
- Production authentication (R-091) unless explicitly delivered later
- Final commercial-state transport (D-P12-03) unless explicitly authorized
- Real lending product commitment

## Work packages

| WP | Name | Status | Depends on |
|---|---|---|---|
| RL-WP00 | Documentation baseline (this dry run) | Complete (fictional) | Foundation WP01–WP05 |
| RL-WP01 | Domain baseline / persistence skeleton | Not started — **do not begin** | Product-owner policy + separate authorization |

## Dependencies

| Dependency | Notes |
|---|---|
| Platform subscription for `reference-loan` | Required if ever productized |
| Product-owner lending policy | Missing — stop rather than invent |

## Acceptance criteria (phase)

- [x] Coherent docs from templates
- [x] Isolation contract preserved in documentation
- [x] No `src/Products/ReferenceLoan/`
- [x] R-091 / D-P12-03 remain open

## Risks

| ID | Risk | Mitigation |
|---|---|---|
| RL-R-01 | Readers mistake fiction for a real product | README banner + no src folder |

## Exact next package

**RL-WP01 — Domain baseline / persistence skeleton** (do not begin until separately authorized; not part of P12-WP06)

## Phase closeout requirements

- [x] Dry-run findings recorded in P12-WP06 report
- [x] Remaining debt honest
- [x] No invented unresolved policy
