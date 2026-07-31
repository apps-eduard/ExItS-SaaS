# {{PRODUCT_NAME}} — Roadmap / Phase Plan

> Template: P12-WP03. Foundation: [exits-product-foundation-reference.md](../../exits-product-foundation-reference.md)  
> Replace phase names for **this** product only — do not copy another product’s phases.

| Field | Value |
|---|---|
| Product | {{PRODUCT_NAME}} |
| Current phase | {{CURRENT_PHASE}} |
| Status | Draft / Approved |

## Phase objective

{{PHASE_OBJECTIVE}}

## Scope

### Included

- {{SCOPE_IN_1}}

### Excluded

- {{SCOPE_OUT_1}}
- Production authentication (R-091) unless this phase explicitly delivers it
- Final commercial-state transport (D-P12-03) unless explicitly authorized

## Work packages

| WP | Name | Status | Depends on |
|---|---|---|---|
| {{WP_ID}} | {{WP_NAME}} | Not started / … | {{WP_DEPS}} |

## Dependencies

| Dependency | Notes |
|---|---|
| Platform subscription for {{PRODUCT_CODE}} | Required |
| {{DEP}} | {{DEP_NOTES}} |

## Acceptance criteria (phase)

- [ ] {{ACCEPT_1}}
- [ ] Isolation contract preserved (separate DB; no Platform table reads; product-local roles)
- [ ] Docs match implementation
- [ ] Tests green; `main = origin/main`

## Risks

| ID | Risk | Mitigation |
|---|---|---|
| {{RISK_ID}} | {{RISK}} | {{MITIGATION}} |

## Exact next package

**{{NEXT_WP_ID}} — {{NEXT_WP_NAME}}** (do not begin until authorized)

## Phase closeout requirements

- [ ] WP matrix complete
- [ ] Remaining debt honest
- [ ] No invented unresolved policy
- [ ] Closeout report filed
- [ ] Portfolio / phase status updated
