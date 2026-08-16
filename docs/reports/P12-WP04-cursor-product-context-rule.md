# P12-WP04 — Cursor Product Context Rule

Phase marker: `P12-WP04-cursor-product-context-rule`

Package: **P12-WP04 — Cursor Product Context Rule**
Prior tip: `5ca2c86fe97d32d0e37fe632a53a65d581e87671`
Docs tip: `1243c78d65e347b23949b19ce2edf564fe972aad`

## Status

**Complete.** Documentation and Cursor-rule only. Added a permanent product-context rule that limits routine loading to the foundation + active product docs + files needed for the task. No application code, scaffolds, Docker, CI/CD, or new products.

Exact next: **P12-WP05 — Product Bootstrap Prompt** (do not begin until authorized).

## Rule path and scope

| Item | Value |
|---|---|
| Path | `.cursor/rules/exits-product-context.mdc` |
| `alwaysApply` | `false` |
| `globs` | `src/Products/**`, `docs/Product-Foundation/**`, `docs/phases/phase-12*` |
| References | `exits-workflow.mdc`, `docs/Product-Foundation/exits-product-foundation-reference.md`, `Templates/` |

Platform-only work outside these globs is not forced to load Product Foundation context.

## Context-loading sequence

1. `exits-workflow.mdc`
2. `exits-product-context.mdc`
3. Product Foundation reference
4. `src/Products/<ProductName>/Docs/`
5. Current phase/WP document or prompt
6. Only task-needed source/tests/contracts/migrations/reports

## Expansion and conflict rules

- **Expansion gate:** shared contracts, arch/security validation, migration chains, cross-cutting regressions, design-system changes, in-scope deploy/integration — state missing info, target files, and why before broadening.
- **Precedence:** user/WP auth → workflow rule → foundation → active product docs → historical reports (evidence only).
- **New products:** require definition pack from templates; do not copy POS as architecture template; no product tree creation in this WP.

## Validation

| Check | Result |
|---|---|
| Concise; no full workflow/foundation duplication | Pass |
| Metadata/globs correct; links resolve | Pass |
| Active portfolio products only; no `{{placeholders}}` | Pass |
| No app/scaffold code | Pass |
| `.mdc` unit tests | None in repo — not added |
| Release tests | **1186 passed / 0 failed / 0 skipped** |

## Files changed

- `.cursor/rules/exits-product-context.mdc` (new)
- `docs/reports/P12-WP04-cursor-product-context-rule.md`
- Phase 12 / portfolio / manifests / Product Foundation README / foundation §10 cross-link as needed
- `docs/cursor/README.md` (index pointer)

## Exact next

**P12-WP05 — Product Bootstrap Prompt** when explicitly authorized. Do not begin P12-WP05.
