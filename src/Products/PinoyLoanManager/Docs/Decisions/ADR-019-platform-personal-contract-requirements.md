# ADR-019 — Platform / Personal contract requirements

**Status:** Accepted (PLM-DOC-10)
**Date:** 2026-08-19
**Decisions:** **PLM-D-00-05 Closed for PLM behavior/contract requirements**; **PLM-D-00-04 Open** (External Platform); **D-P12-03 Open**

> **Historical note (PLM-DOC-10):** R-091 was Open at package completion. **R-091 is now Closed for Phase 13 scope.** Final status: [PLM-decision-status-summary.md](PLM-decision-status-summary.md).

---

## Context

PLM-DOC-01 defined product-owner linking behavior but left Platform transport, Personal API shape, and cross-product contract facts open under **PLM-D-00-05**. Implementation could not proceed with consistent boundaries while "Personal / Loan API shape" and unlink visibility rules remained ambiguous in [../risks-and-decisions.md](../risks-and-decisions.md).

Generic Platform relationship schema (**PLM-D-00-04**) remains an External Platform decision. PLM must not invent it.

---

## Decision

1. **Close PLM-D-00-05 for PLM product behavior and contract requirements** via:
   - [../Architecture/platform-access-context-contract.md](../Architecture/platform-access-context-contract.md)
   - [../Architecture/personal-link-and-consent-contract.md](../Architecture/personal-link-and-consent-contract.md)
   - [../Architecture/personal-facing-loan-api-contract.md](../Architecture/personal-facing-loan-api-contract.md)
2. Required Platform access **context facts** are defined; transport (JWT/cookie/header/lease/cache) is **not** selected (**D-P12-03 Open**).
3. Required Personal link/consent **operations and contract facts** are defined; Platform persistence schema is **not** designed (**PLM-D-00-04 Open**).
4. Personal-facing loan **customer operations** are defined; Personal must **never** read PLM tables.
5. Unlink blocking rules, pending-offer treatment after unlink, relink/consent-history requirements, and Personal API operation groups are **resolved at product-contract level**. Legal visibility basis remains **PLM-D-00-11 Open**.
6. **Platform implementation** (APIs, tables, message delivery, Personal UI wiring) is **external** to this ADR and remains future Platform / integration work.

Product behavior from ADR-002 and PLM-DOC-01 is unchanged; this ADR adds implementable contract surfaces without selecting Platform internals.

---

## Consequences

PLM documentation and future Application-layer design may rely on stable contract checklists.

**Still open**

- **PLM-D-00-04** — generic Platform cross-product relationship model (External Platform)
- **D-P12-03** — commercial-state and cross-service transport (inbound and outbound where applicable)
- **PLM-D-00-11** — legal/compliance validation including post-unlink retention

No code, schema, migrations, or Platform PR is authorized by this ADR alone.

---

## Canonical references

- [../Product/personal-linking-lifecycle-and-visibility.md](../Product/personal-linking-lifecycle-and-visibility.md)
- [../Architecture/personal-integration-boundary.md](../Architecture/personal-integration-boundary.md)
- [../Reports/PLM-DOC-10-platform-personal-and-commercial-contracts.md](../Reports/PLM-DOC-10-platform-personal-and-commercial-contracts.md)
