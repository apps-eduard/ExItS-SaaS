# P27-WP04 — Connected PO Cancellation & Withdrawal

Package: **P27-WP04 — Connected PO Cancellation & Withdrawal**
Phase: [Phase 27 — Connected Supplier Commerce & Purchasing](../phases/phase-27-connected-supplier-commerce-and-purchasing.md)
Design: [connected-exits-suppliers.md](../engineering/connected-exits-suppliers.md)

## Status

**Code Complete.** Phase 27 remains **Open / In Progress**.

| Gate | Value |
|---|---|
| Device Verified | **No** |
| Browser Verified | **No** |
| Production Ready | **No** |

**Starting SHA:** `f92428d1d78a02d61103dc0bbe3836eddf921caa`

## Scope

Align buyer cancellation with connected-order withdrawal, allowing a buyer to withdraw only before the supplier responds and preventing contradictory Accept/Withdraw outcomes.

## Implementation summary

- Buyer cancellation finds the correlated connected order and withdraws it only while its status is `New`.
- A successful withdrawal transitions the buyer PO to Cancelled and the connected order to `Withdrawn`.
- Buyer list/detail surfaces expose `CanWithdrawConnected`, the withdrawn timestamp, and the derived Withdrawn display status.
- Supplier incoming-order views reflect withdrawal and can no longer accept or decline the withdrawn order.
- Withdrawal publishes a supplier notification through `IOrganizationBusinessNotificationPublisher`.

## Domain decisions

- Buyer withdrawal is allowed only for `New`.
- Supplier Accept, supplier Decline, and buyer Withdraw compete for the same `New` transition.
- The persistence transition matrix allows `New → Accepted`, `New → Declined`, or `New → Withdrawn` and rejects all contradictory terminal races.
- Accepted/Preparing/Fulfilled connected orders cannot be cancelled through buyer withdrawal.
- Cancellation/withdrawal never mutates inventory.
- WP01 eligibility and pricing remain unchanged.

## API changes

- Existing `POST /api/v1/pos/purchase-orders/{purchaseOrderId}/cancel` performs connected withdrawal when a correlated connected order exists.
- Purchase-order DTOs add `CanWithdrawConnected`, `ConnectedStatus`, `WithdrawnAtUtc`, and derived `DisplayStatus`.
- Supplier incoming-order list/detail returns Withdrawn status and disables invalid supplier actions.

## Persistence / migration

Migration: `20260816094758_AddConnectedPoLifecycleAndReceivingDiscrepancies`.

It adds `withdrawn_at_utc`, expands the connected status constraint through `Withdrawn`, and preserves optimistic concurrency through the existing row-version token. Migration apply/rollback/re-apply evidence is not yet recorded.

## Security

- Cancellation remains organization-scoped and requires purchasing-management capability.
- The server resolves the buyer-owned PO and correlated connected order; clients cannot withdraw another organization's order.
- Domain and persistence transition checks provide defense in depth for Accept/Withdraw races.
- Notifications are best-effort and disclose only business-order lifecycle context.
- This code-complete state is not production-ready.

## Validation evidence

| Evidence | Result |
|---|---|
| Targeted unit tests | **Run** — New-only withdrawal and Accept/Withdraw transition-matrix tests passed |
| MAUI guard tests | **Run** — buyer cancellation and supplier incoming-order guards passed |
| Device validation | **Not run** |
| Browser validation | **Not run** |
| Migration apply / rollback / re-apply | **Not recorded** |

Device Verified: **No**. Browser Verified: **No**. Production Ready: **No**.

## Implementation commits

Commit hashes:
- `eef33cb8fb4b4afa00a7bc185d20de1664d9f4b4` — feat(purchasing): add connected PO lifecycle and receiving discrepancy model
- `d39b096021c0e45d20f3d4718ffed62fa6a77701` — feat(suppliers): synchronize connected PO response and fulfillment APIs
- `0a1ab1bb90d77de949df5300f35787697b60e480` — feat(maui): add mobile connected PO lifecycle UX
- `b38be37c6e777105820887353db6d90b0188ccdd` — test(p27): cover connected PO lifecycle and receiving
- `d273553f76d3e7fd0c737f1703fea2cf63ad2832` — docs(p27): document connected PO lifecycle and discrepancies

## Residuals

- Physical race testing against PostgreSQL and owner workflow validation remain outstanding.
- Full solution Release build/test and migration lifecycle evidence remain required.
- Post-accept cancellation/reversal is intentionally unsupported; operational handling remains outside this WP.
- Phase 27 remains open.

## Exact next

**P27-WP06 — Connected Purchasing UX & Notifications.**
