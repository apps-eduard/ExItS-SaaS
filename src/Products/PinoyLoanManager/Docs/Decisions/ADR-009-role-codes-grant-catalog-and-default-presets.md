# ADR-009 — Role codes, grant catalog, and default presets

**Status:** Accepted product policy (PLM-DOC-05); **PLM-D-00-06 Closed for MVP**; not implemented
**Date:** 2026-08-19
**Decisions:** **PLM-D-00-06 Closed for MVP**

---

## Context

PLM needed stable MVP role preset codes, exact grant identifiers, and default Owner/Manager/Cashier/Collector assignments. Prior docs recorded grant **intent** only (PLM-D-00-06 Open).

---

## Decision

1. Role preset codes: `plm.owner`, `plm.manager`, `plm.cashier`, `plm.collector` with stable lowercase codes and localizable display labels.
2. **PLM Authorization Policy v1** — stable grant identifiers using `plm.<resource>.<action>`; no wildcard; no implicit hierarchy.
3. Full MVP grant catalog documented in [../Security/authorization-grant-catalog.md](../Security/authorization-grant-catalog.md).
4. Custom organization-defined roles are **not** supported in MVP.
5. Multiple active preset assignments per user are allowed; effective grants are the union subject to scope and maker/checker rules.
6. Default preset grant matrix in [../Security/default-role-preset-policy.md](../Security/default-role-preset-policy.md) and [../authorization-matrix.md](../authorization-matrix.md).
7. Owner default excludes Cashier/Collector execution grants unless separately assigned those presets.
8. **PLM-D-00-06 Closed for MVP** for role codes, grant catalog, default presets, and multiple-role union behavior. Custom roles deferred to a future explicit decision/package.

---

## Consequences

PLM-03 and operational WPs have an approved authorization contract for documentation and future implementation.

**Still open:** D-P12-03 commercial transport, PLM-D-00-11 legal/compliance, custom roles (future).

No authorization engine or schema is authorized by this ADR.
