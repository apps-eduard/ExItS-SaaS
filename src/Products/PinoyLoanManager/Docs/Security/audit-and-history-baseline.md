# Pinoy Loan Manager — Audit and History Baseline

**Status:** Planning / security baseline (documentation only)
**Implementation present:** No
**Last updated:** 2026-08-19

Product-owned high-risk history. Complements [../security.md](../security.md) and [role-and-grant-baseline.md](role-and-grant-baseline.md). Not a schema or SIEM design.

Related: [../Architecture/loan-ledger-and-balance-model.md](../Architecture/loan-ledger-and-balance-model.md), [../Product/exception-reversal-and-variance-workflow.md](../Product/exception-reversal-and-variance-workflow.md).

---

## Principle

Audit records are **not** ordinary editable notes.

Posted financial events and high-risk operational actions must remain historically visible. Corrections use new auditable events, not silent deletes.

---

## High-risk history (planning)

Audit / high-risk history includes:

- application
- approval / rejection
- template publishing
- Personal consent / linking
- disbursement
- payment
- reversal
- penalty
- waiver
- collection exception
- float
- remittance
- reconciliation
- variance resolution
- restructuring / write-off later

High-risk fields (when implemented): actor, organization, branch, time, action, target resource, amount where relevant, reason where required, approval actor where applicable, correlation / reference, original transaction reference for reversal, device / channel where useful.

---

## Access

Audit view follows grants and scope. Collectors do not receive default unrestricted audit browse.

Platform audit remains Platform-owned. Do not push operational payloads that violate the product boundary.

---

## Explicit non-goals

- Editable audit notes as SoR
- Silent deletion of high-risk history
- Schema / retention schedule (retention **OPEN**)
