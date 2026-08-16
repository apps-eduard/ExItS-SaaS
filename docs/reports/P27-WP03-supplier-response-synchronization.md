# P27-WP03 — Supplier Response Synchronization

Package: **P27-WP03 — Supplier Response Synchronization**
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

Synchronize supplier Accept/Decline and fulfillment responses into buyer-visible connected PO state, with clear display statuses, decline context, and no inventory mutation.

## Implementation summary

- `ConnectedPurchaseOrderStatus` now supports `New`, `Accepted`, `Declined`, `Preparing`, `Fulfilled`, and `Withdrawn`.
- Supplier Accept/Decline records lifecycle timestamps; Decline can include a bounded reason and note.
- Supplier incoming-order detail supports Accept, Decline, Preparing, and Fulfilled actions.
- Buyer purchasing list/detail consume connected state and a derived `DisplayStatus` instead of treating supplier state as buyer inventory state.
- Accepted, declined, preparing, and fulfilled transitions publish buyer notifications through `IOrganizationBusinessNotificationPublisher`.

## Domain decisions

- Authoritative supplier transition path: `New → Accepted → Preparing → Fulfilled`; direct `Accepted → Fulfilled` is allowed and records preparation time.
- `New → Declined` is terminal.
- Derived buyer labels include Waiting for Supplier, Supplier Accepted, Supplier Declined, Preparing, Ready, Partially Received, Received, and Received With Issues.
- Preparing/Fulfilled indicates supplier progress only and never mutates buyer inventory.
- WP01 product eligibility, explicit sharing, and effective pricing remain unchanged.

## API changes

- `GET /api/v1/pos/connected-suppliers/incoming-orders/{id}`
- `POST /api/v1/pos/connected-suppliers/incoming-orders/{id}/accept`
- `POST /api/v1/pos/connected-suppliers/incoming-orders/{id}/decline`
- `POST /api/v1/pos/connected-suppliers/incoming-orders/{id}/prepare`
- `POST /api/v1/pos/connected-suppliers/incoming-orders/{id}/fulfill`
- Purchase-order and incoming-order DTOs expose connected status, transition timestamps, decline reason/note, and derived display status.

## Persistence / migration

Migration: `20260816094758_AddConnectedPoLifecycleAndReceivingDiscrepancies`.

It expands the connected-order status constraint to values 0–5 and adds `preparing_at_utc`, `fulfilled_at_utc`, `withdrawn_at_utc`, `decline_reason`, and `decline_note`. Migration apply/rollback/re-apply evidence is not yet recorded.

## Security

- Supplier reads require purchasing-view capability; lifecycle mutations require purchasing-management capability.
- Supplier organization ownership and relationship state are checked server-side.
- Repository concurrency tokens and the allowed-transition matrix reject contradictory or stale lifecycle writes.
- Notification publication is best-effort and does not bypass domain authorization or persistence.
- No production-readiness claim is made.

## Validation evidence

| Evidence | Result |
|---|---|
| Targeted unit tests | **Run** — lifecycle, decline metadata, transition matrix, and display status tests passed |
| MAUI guard tests | **Run** — incoming-order detail/list guards passed |
| Device validation | **Not run** |
| Browser validation | **Not run** |
| Migration apply / rollback / re-apply | **Not recorded** |

Device Verified: **No**. Browser Verified: **No**. Production Ready: **No**.

## Implementation commits

Commit hashes: **TBD — recorded after commit**.

## Residuals

- Owner validation of supplier and buyer views remains outstanding.
- Full solution Release build/test and PostgreSQL migration lifecycle evidence remain required.
- Notifications use the existing unified organization inbox; realtime push is not included.
- Phase 27 remains open.

## Exact next

**P27-WP04 — Connected PO Cancellation & Withdrawal.**
