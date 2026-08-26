# Pinoy Loan Manager — Platform Commercial Integration

**Status:** Planning / architecture baseline (documentation only)
**Implementation present:** No
**Last updated:** 2026-08-19

Platform ↔ PLM commercial and identity contracts. **D-P12-03 remains open** for transport. **PLM-D-00-05 is Closed for PLM behavior/contract requirements** (PLM-DOC-10). Do not invent shared-database integration.

Related: [api-and-contract-boundary.md](api-and-contract-boundary.md), [personal-integration-boundary.md](personal-integration-boundary.md), [platform-access-context-contract.md](platform-access-context-contract.md), [personal-link-and-consent-contract.md](personal-link-and-consent-contract.md), [personal-facing-loan-api-contract.md](personal-facing-loan-api-contract.md), [platform-usage-metering-contract.md](platform-usage-metering-contract.md), [tenant-placement-and-routing-contract.md](tenant-placement-and-routing-contract.md), [../architecture.md](../architecture.md), [../Decisions/ADR-019-platform-personal-contract-requirements.md](../Decisions/ADR-019-platform-personal-contract-requirements.md), [../Decisions/ADR-020-usage-metering-and-tenant-placement-contracts.md](../Decisions/ADR-020-usage-metering-and-tenant-placement-contracts.md).

---

## Platform → PLM (future)

Through approved contracts only:

- authenticated identity / context — [platform-access-context-contract.md](platform-access-context-contract.md)
- organization identity
- product access
- entitlement / commercial authorization
- tenant placement / routing facts — [tenant-placement-and-routing-contract.md](tenant-placement-and-routing-contract.md)

Platform product access / entitlement alone does **not** grant operational PLM permissions.

Until D-P12-03 is closed: do not copy PinoyBusinessPOS Dev/Testing commercial headers as production design. Any later Dev/Testing gate must fail closed outside approved environments.

---

## PLM → Platform (future)

Through approved contracts only:

- Personal identity relationship interactions (consent / link) — [personal-link-and-consent-contract.md](personal-link-and-consent-contract.md); not Platform storage of Loan history
- commercial usage events for billable disbursement — [platform-usage-metering-contract.md](platform-usage-metering-contract.md) (preferred event: **LOAN DISBURSED**)

No direct Loan database writes into Platform billing tables. No direct Platform table reads from PLM.

---

## Personal-facing PLM APIs (future)

ExItS Personal consumes PLM-authoritative loan presentation through [personal-facing-loan-api-contract.md](personal-facing-loan-api-contract.md). Personal must never read PLM database tables.

---

## Ownership reminder

| Owner | Owns |
|---|---|
| Platform | SaaS subscription, entitlement, Platform billing, Platform usage billing, tenant placement control plane |
| Pinoy Loan Manager | Lending operations, Loan ledger, borrower data, cash accountability, operational audit, usage event emission facts |

---

## Explicit non-goals

- Closing D-P12-03 transport selection
- Designing PLM-D-00-04 generic Platform relationship schema
- Shared DB
- Platform Admin as loan operations UI
