# P10-WP01 — Scope Ambiguity (documentation decision)

Date: 2026-07-31  
Phase tip accepted closed: `9c1b86b4488005e81bb9d78b1dafaea66a8e6e4d`  
Status: **Stopped for authorization** — no Phase 10 functionality implemented

## Decision asked

Resolve the first authorized Phase 10 work package before production-code changes.

## Finding

| Question | Answer |
|---|---|
| Exact first WP title | **P10-WP01 — Suppliers** |
| Formally numbered `P10-WP01`? | **Yes** — authoritative `docs/phases/phase-10-full-pos.md` |
| Approved implementation scope clear? | **No** — only generic stub outcomes existed |
| Conflicting wording? | Product docs say “Suppliers and purchasing” as one Full POS bullet; roadmap splits **Suppliers** (`P10-WP01`) and **Purchasing** (`P10-WP02`). Release plan R5 listed purchasing/inventory/shifts/returns/registers without naming Suppliers first. |

## What is authoritative

- Phase roadmap work-package order: Suppliers → Purchasing → Advanced Inventory → Cashier Shifts → Returns and Refunds → Advanced Permissions and Reports → Multiple Registers → Full POS Closeout
- Data authority: Supplier is POS-owned (`SupplierId`)
- Platform must not own supplier operational data

## What is missing (blocks implementation)

No approved document defines for `P10-WP01` alone:

- Domain fields and lifecycle
- Authorization feature codes / POS roles interaction
- Persistence/migration expectations
- API and MAUI surfaces
- Online/offline rules
- Exact exclusions relative to `P10-WP02 — Purchasing`
- Acceptance tests and Definition of Done beyond generic stubs

Inventing that scope would violate permanent workflow rules (“do not invent policy”).

## Proposed options (choose one to authorize)

| Option | Description | Implication |
|---|---|---|
| **A** | Supplier master data only (org-scoped identity/contact/status; no PO/receiving/stock) | Aligns with separate `P10-WP02 — Purchasing`; recommended default |
| **B** | Suppliers + non-posting PO drafts | Partial overlap with WP02; needs clear cut line |
| **C** | Combine suppliers & purchasing into WP01 | Requires rewriting/removing or redefining `P10-WP02` |
| **D** | Reorder Phase 10 (different first WP) | Requires explicit roadmap rewrite authorization |

## Actions taken (documentation only)

- Recorded exact title **P10-WP01 — Suppliers** on `docs/phases/phase-10-full-pos.md`
- Marked implementation blocked pending scope clarification
- Reconciled portfolio / release-plan wording to name Suppliers as first WP
- **No** schema, API, UI, or test implementation for Phase 10

## Required next authorization

Authorize one of options A–D (or a written custom scope) as the approved scope for **P10-WP01 — Suppliers**, then re-issue an implementation command for that WP only.

Do **not** begin `P10-WP02` or later until authorized.
