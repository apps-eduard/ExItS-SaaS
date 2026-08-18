# Pinoy Loan Manager — Platform Commercial Integration

**Status:** Planning / architecture baseline (documentation only)
**Implementation present:** No
**Last updated:** 2026-08-19

Platform ↔ PLM commercial and identity contracts. **D-P12-03 remains open.** Do not invent shared-database integration.

Related: [api-and-contract-boundary.md](api-and-contract-boundary.md), [personal-integration-boundary.md](personal-integration-boundary.md), [../architecture.md](../architecture.md).

---

## Platform → PLM (future)

Through approved contracts only:

- authenticated identity / context
- organization identity
- product access
- entitlement / commercial authorization

Platform product access / entitlement alone does **not** grant operational PLM permissions.

Until D-P12-03 is closed: do not copy PinoyBusinessPOS Dev/Testing commercial headers as production design. Any later Dev/Testing gate must fail closed outside approved environments.

---

## PLM → Platform (future)

Through approved contracts only:

- Personal identity relationship interactions (consent / link) — not Platform storage of Loan history
- commercial usage event for **billable disbursement** (preferred event: LOAN DISBURSED)

No direct Loan database writes into Platform billing tables. No direct Platform table reads from PLM.

---

## Ownership reminder

| Owner | Owns |
|---|---|
| Platform | SaaS subscription, entitlement, Platform billing, Platform usage billing |
| Pinoy Loan Manager | Lending operations, Loan ledger, borrower data, cash accountability, operational audit |

---

## Explicit non-goals

- Closing D-P12-03
- Shared DB
- Platform Admin as loan operations UI
