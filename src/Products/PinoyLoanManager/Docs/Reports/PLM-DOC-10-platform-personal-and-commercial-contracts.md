# PLM-DOC-10 — Platform, Personal, and Commercial Contracts

**Status:** Documentation package complete (planning only)
**Implementation present:** No
**Last updated:** 2026-08-19

Runtime / browser / device / database / production validation: **Not Applicable**.

> **Historical note:** Decision statuses below reflect PLM-DOC-10 package completion. **R-091 is now Closed for Phase 13 scope.** **PLM-D-00-07/08 Closed for MVP Product policy.** Final status: [../Decisions/PLM-decision-status-summary.md](../Decisions/PLM-decision-status-summary.md).

---

## Scope

Finalize Pinoy Loan Manager **Platform access context facts**, **Personal link/consent contract**, **Personal-facing loan API operations**, **Platform usage metering events**, and **tenant placement/routing contract** — without selecting transport, Platform schema, or authentication implementation.

Explicitly **out of scope:** code, database creation, migrations, APIs, UI, solution changes, Platform implementation, POS implementation, closing **D-P12-03**, closing **PLM-D-00-04**, legal compliance claims. (**R-091 Closed for Phase 13 scope** at final review.)

---

## Delivered

| Doc | Subject |
|---|---|
| [platform-access-context-contract.md](../Architecture/platform-access-context-contract.md) | Required Platform context facts; transport not selected |
| [personal-link-and-consent-contract.md](../Architecture/personal-link-and-consent-contract.md) | Link/consent operations and facts; no Platform schema |
| [personal-facing-loan-api-contract.md](../Architecture/personal-facing-loan-api-contract.md) | Personal customer operations; no PLM table reads |
| [platform-usage-metering-contract.md](../Architecture/platform-usage-metering-contract.md) | `LOAN_DISBURSED` and related events; idempotency; no PII |
| [tenant-placement-and-routing-contract.md](../Architecture/tenant-placement-and-routing-contract.md) | Org+Product→Placement→Region/Stamp/Partition; no hard-coded DB |
| [ADR-019](../Decisions/ADR-019-platform-personal-contract-requirements.md) | Closes **PLM-D-00-05** for PLM behavior/contract |
| [ADR-020](../Decisions/ADR-020-usage-metering-and-tenant-placement-contracts.md) | Usage metering and tenant placement contracts |

Updated indexes and [platform-commercial-integration.md](../Architecture/platform-commercial-integration.md) cross-references.

---

## Accepted decisions

| Topic | Outcome |
|---|---|
| Platform access context | Actor, org, product access, commercial facts required; fail closed |
| Transport selection | **Not decided** — **D-P12-03 Open** |
| Personal link/consent | Required operations and contract facts defined |
| Platform relationship schema | **Not designed** — **PLM-D-00-04 Open External Platform** |
| Personal-facing API | Customer operation checklist defined; Personal never reads PLM tables |
| Unlink / pending offers / relink | Product-contract behavior resolved (legal basis still **PLM-D-00-11 Open**) |
| Usage metering | `LOAN_DISBURSED` primary; reversal/cancellation events; idempotent; no PII |
| Tenant placement | Abstraction required; no hard-coded DB routing |
| PLM-D-00-05 | **Closed for PLM behavior/contract**; Platform implementation external |

---

## Decision register (this package)

| ID | Outcome |
|---|---|
| PLM-D-00-05 | **Closed for PLM behavior/contract requirements** — Platform transport/persistence/implementation external |
| PLM-D-00-04 | **Open** — External Platform generic relationship model |
| D-P12-03 | **Open** — commercial-state and event transport |
| R-091 | **Closed for Phase 13 scope** (historical: Open at package completion) |
| PLM-D-00-11 | **Open** — legal/compliance including post-unlink visibility |

Other PLM-D-00 items remain as previously recorded.

---

## Resolved open areas (risks register)

Moved from [../risks-and-decisions.md](../risks-and-decisions.md) operating-model open list to product-contract docs:

- Personal / Loan API shape → [personal-facing-loan-api-contract.md](../Architecture/personal-facing-loan-api-contract.md)
- who may initiate unlink → [personal-link-and-consent-contract.md](../Architecture/personal-link-and-consent-contract.md) (Personal revoke; org suspend)
- pending Quick Loan offer treatment after unlink → [personal-link-and-consent-contract.md](../Architecture/personal-link-and-consent-contract.md)
- historical Personal visibility after unlink → contract direction recorded; **PLM-D-00-11** for legal basis
- re-linking and consent-history rules → [personal-link-and-consent-contract.md](../Architecture/personal-link-and-consent-contract.md)

---

## Explicitly deferred implementation

- Platform relationship tables / generic schema (**PLM-D-00-04**)
- JWT/cookie/header/lease/cache commercial transport (**D-P12-03**)
- OpenAPI routes and Platform auth transport (**D-P12-03**; **R-091 Closed for Phase 13 scope**)
- Message bus / outbox technology
- Stamp/partition provisioning and tenant movement tooling
- Product implementation (remains **paused**)

---

## Files changed

**Created:** five Architecture contracts, ADR-019, ADR-020, this report.

**Updated:** [risks-and-decisions.md](../risks-and-decisions.md), [roadmap.md](../roadmap.md), [FILE-MANIFEST.md](../FILE-MANIFEST.md), [README.md](../README.md), [Architecture/README.md](../Architecture/README.md), [Decisions/README.md](../Decisions/README.md), [Reports/README.md](../Reports/README.md), [platform-commercial-integration.md](../Architecture/platform-commercial-integration.md).

---

## Validation

Documentation only. No `.cs`, `.csproj`, `ExItS.slnx`, migrations, APIs, UI, tests, POS, or Platform implementation changes.

---

## Exact next documentation package

**No further PLM-DOC packages are defined.** Await explicit Product Owner authorization before **PLM-01** implementation. **D-P12-03** and **PLM-D-00-04** remain open. **R-091 Closed for Phase 13 scope.**

Do not start PLM-DOC-08 in this package. Implementation remains paused.
