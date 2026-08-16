# P27-WP02 — Connected PO Delivery & Reliability

Package: **P27-WP02 — Connected PO Delivery & Reliability**
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

Deliver connected purchase orders reliably from the buyer PO submission path to the supplier incoming-order workflow while preserving atomic validation and the buyer inventory invariant.

## Implementation summary

- Connected submit validates the active relationship, active product links, WP01 sharing eligibility, and effective connected PO prices before the buyer PO is ordered.
- Buyer PO and supplier-facing `ConnectedPurchaseOrder` correlation is retained through the buyer PO identifier and connected order identifier.
- Supplier incoming-order list/detail surfaces expose the submitted order and price snapshots.
- Submission publishes a best-effort organization business notification through `IOrganizationBusinessNotificationPublisher`.
- WP01 eligibility and pricing rules are unchanged: exposable is not shared, and effective price remains buyer-specific override → Default PO Price.

## Domain decisions

- Connected order creation starts at `ConnectedPurchaseOrderStatus.New`.
- Submission and supplier delivery do not mutate buyer inventory.
- Connected line names, units, quantities, and effective prices are snapshots; later catalog changes do not rewrite an already submitted order.
- External supplier purchase orders remain on their existing path.

## API changes

- Existing purchase-order submit now creates the connected supplier order only after all connected lines pass revalidation.
- `GET /api/v1/pos/connected-suppliers/incoming-orders`
- `GET /api/v1/pos/connected-suppliers/incoming-orders/{id}`
- Connected PO DTOs now carry lifecycle and derived display-state fields needed by buyer and supplier clients.

## Persistence / migration

Migration shared by P27-WP02–WP05: `20260816094758_AddConnectedPoLifecycleAndReceivingDiscrepancies`.

The delivery correlation remains in POS-owned tables; there is no cross-product database access or foreign key. Migration apply/rollback/re-apply evidence is not yet recorded for this code-complete checkpoint.

## Security

- Buyer and supplier organization scope is resolved server-side.
- View operations require purchasing-view capability; mutations remain behind purchasing-management capability.
- Connected relationship, sharing, and price eligibility are revalidated on the server; clients cannot submit foreign relationship/product identifiers to bypass WP01 rules.
- Notifications are best-effort and do not become the transaction source of truth.
- Development-stage validation does not establish production security readiness.

## Validation evidence

| Evidence | Result |
|---|---|
| Targeted unit tests | **Run** — connected PO delivery/lifecycle guards passed |
| MAUI guard tests | **Run** — incoming-order and purchasing UI guards passed |
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

- Physical-device and browser workflow validation remain outstanding.
- Full solution Release build/test and PostgreSQL migration apply/rollback/re-apply evidence must be recorded before closeout.
- Notification delivery is best-effort; no realtime push or broker was added.
- Phase 27 remains open.

## Exact next

**P27-WP06 — Connected Purchasing UX & Notifications.**
