# Pinoy Loan Manager — Platform Access Context Contract

**Status:** Accepted product contract requirements (PLM-DOC-10); transport **not** selected
**Implementation present:** No
**Last updated:** 2026-08-19

Required **context facts** Pinoy Loan Manager must receive from ExItS Platform before authorizing product work. This document defines **what** must be knowable, not **how** it is delivered.

**D-P12-03 remains Open.** Do not select JWT, cookie, header injection, lease, cache, introspection endpoint, or shared-database reads here.

Related: [platform-commercial-integration.md](platform-commercial-integration.md), [api-and-contract-boundary.md](api-and-contract-boundary.md), [../authorization-matrix.md](../authorization-matrix.md), [../Decisions/ADR-019-platform-personal-contract-requirements.md](../Decisions/ADR-019-platform-personal-contract-requirements.md).

---

## Purpose

Every PLM entry gate — Organization Web, MAUI, Personal-facing PLM APIs, and future background workers — must operate on a **single, server-derived Platform access context** for the current request or job.

PLM must **not**:

- read Platform database tables directly
- trust client-supplied organization, product-access, or commercial facts without Platform validation
- treat Platform product entitlement as a substitute for PLM operational grants

---

## Required context facts

The approved contract must make the following facts available to PLM in a tamper-evident, server-validated form.

### Actor facts

| Fact | Requirement |
|---|---|
| Platform user identifier | Stable Guid for the authenticated actor |
| Identity kind | Personal, Organization staff, or Platform staff (as applicable to the surface) |
| Normalized login key | As defined by the ExItS identity model for the actor kind |
| Actor display name | Minimum needed for audit and UI; not a substitute for authorization |
| Session validity | Current request is authenticated under an active Platform session or approved service identity |

For Organization staff: `HomeOrganizationId` must match the organization being accessed. Personal actors do not carry org-staff home-organization semantics.

### Organization facts

| Fact | Requirement |
|---|---|
| Organization identifier | Guid |
| Organization display name | Minimum needed for audit/UI |
| Organization active state | Inactive or suspended organizations must fail closed for write authority |

### Product access facts

| Fact | Requirement |
|---|---|
| Product code | Must include `pinoy-loan-manager` when PLM is being accessed |
| Product access state | Active, trial, suspended, or denied as defined by Platform catalog/subscription |
| Access scope | Organization-scoped product access only; no cross-organization inference |

Platform product access alone does **not** grant PLM operational permissions. PLM still applies its own grant catalog and resource scope.

### Commercial / entitlement facts

| Fact | Requirement |
|---|---|
| Commercial authorization state | Whether the organization may perform billable or gated PLM work under current subscription/plan |
| Entitlement identifiers | Opaque Platform entitlement/plan references needed for fail-closed gating and audit |
| Effective as-of | Timestamp or version marker for the commercial snapshot used on this request |

Exact entitlement vocabulary remains Platform-owned. PLM consumes facts; it does not define Platform billing tables.

### Request correlation facts

| Fact | Requirement |
|---|---|
| Correlation / trace identifier | For audit and support across Platform and PLM |
| Source surface | Organization Web, MAUI, Personal, internal job, or approved integration |

---

## Fail-closed rules

PLM must deny **write authority** when any required fact is:

- missing
- unknown
- expired (when freshness is part of the contract)
- inconsistent (for example, staff `HomeOrganizationId` ≠ requested organization)
- denied by commercial state

Read-only behavior for unknown commercial state is **not** authorized here unless a later explicit product decision says otherwise. Default posture: **fail closed**.

Dev/Testing shortcuts, if any, must be labeled and must not become the Production design (**D-P12-05 Closed / satisfied for authentication honesty**; **R-091 Closed for Phase 13 scope**).

---

## What this contract does **not** decide

| Topic | Status |
|---|---|
| Transport mechanism (header, cookie, token, lease, API projection) | **D-P12-03 Open** |
| Cache TTL / revocation propagation | **D-P12-03 Open** |
| Platform persistence schema | **PLM-D-00-04 Open** (External Platform) |
| PLM grant catalog or workflow guards | Closed for MVP in PLM-DOC-05 |
| Authentication implementation | **R-091 Closed for Phase 13 scope** — residual step-up/MFA are separate gates |

---

## Consumer surfaces

| Surface | Uses access context for |
|---|---|
| Organization Web | Org selection, commercial gate, PLM grant enforcement |
| MAUI | Same, with reduced API surface |
| Personal-facing PLM APIs | Personal actor + linked-borrower authorization only |
| Future async jobs | Service identity + organization/product facts for metering and notifications |

---

## Explicit non-goals

- Selecting JWT vs cookie vs header vs lease
- Designing Platform EF entities or SQL
- Copying PinoyBusinessPOS Dev commercial headers as Production design
- Closing **D-P12-03** or **PLM-D-00-04** (residual step-up/MFA are separate Platform gates; **R-091 Closed for Phase 13 scope**)
