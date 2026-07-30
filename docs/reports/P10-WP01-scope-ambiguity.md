# P10-WP01 — Scope Ambiguity (documentation decision)

Date: 2026-07-31  
Phase tip accepted closed: `9c1b86b4488005e81bb9d78b1dafaea66a8e6e4d`  
Ambiguity tip: `97e17c248ddd1c0af588eafaa41ac7ab6910ec2f`  
Status: **Resolved — Option A authorized and implemented** (see [P10-WP01-suppliers.md](P10-WP01-suppliers.md))

## Decision asked

Resolve the first authorized Phase 10 work package before production-code changes.

## Finding

| Question | Answer |
|---|---|
| Exact first WP title | **P10-WP01 — Suppliers** |
| Formally numbered `P10-WP01`? | **Yes** — authoritative `docs/phases/phase-10-full-pos.md` |
| Approved implementation scope clear? | **Yes (after authorization)** — Option A |
| Conflicting wording? | Product docs say “Suppliers and purchasing” as one Full POS bullet; roadmap splits **Suppliers** (`P10-WP01`) and **Purchasing** (`P10-WP02`). |

## Authorization outcome

| Field | Value |
|---|---|
| Selected option | **A — Supplier master data only** |
| Authorization | Explicit user command after ambiguity tip `97e17c2…` |
| Scope | Org-owned supplier identity/contact/status; Active/Inactive; no PO/receiving/stock/AP/cost |
| Explicitly deferred | **P10-WP02 — Purchasing** and later |

## What is authoritative

- Phase roadmap work-package order: Suppliers → Purchasing → …
- Data authority: Supplier is POS-owned (`SupplierId`)
- Platform must not own supplier operational data
- Detailed Option A outcomes: `docs/phases/phase-10-full-pos.md` + `docs/reports/P10-WP01-suppliers.md`

## Proposed options (historical)

| Option | Description | Outcome |
|---|---|---|
| **A** | Supplier master data only | **Selected** |
| **B** | Suppliers + non-posting PO drafts | Not selected |
| **C** | Combine suppliers & purchasing into WP01 | Not selected |
| **D** | Reorder Phase 10 | Not selected |

## Actions taken

- Documented ambiguity (docs-only tip `97e17c2…`)
- Authorized Option A and implemented P10-WP01 only
- Did **not** begin P10-WP02
