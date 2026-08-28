# POS-REACT-ACTOR-TRACEABILITY-UI-01 — Transaction actor attribution (detail/history)

| Field | Value |
| --- | --- |
| Status | **Complete** |
| Branch | `feat/organization` |
| Depends on | [P28-WP15D](../../reports/P28-WP15D-operational-actor-traceability.md) immutable actor IDs |
| Does not change | Actor persistence, generic audit tables, primary list pages, customer-facing staff disclosure |

## Goal

Authorized organization users see **who / what / when** on internal POS detail and history surfaces, without cluttering list rows or exposing raw GUIDs.

## Architecture choice

**Option B — org-scoped batch actor resolver + shared React hook/cache**

| Layer | Choice |
| --- | --- |
| Name resolution | Platform `POST /api/v1/platform/organizations/{organizationId}/actor-display-names` |
| Auth | Any active org member via `EnsureCanViewOrganizationAsync` — **not** ManageMemberships |
| Batch | Up to 100 actor IDs; membership + user load in two queries (N+1 prohibited) |
| React | `useActorDirectory` → React Query key `["pos-actor-directory", organizationId, ...sortedIds]` |
| UI | Shared `ActorAttribution` / `ActorName` |
| Semantics | `ACTOR_NAME_SEMANTICS=CURRENT_DISPLAY_NAME` (immutable ActorId remains audit authority) |

Detail DTOs retain authoritative actor IDs. Display names are resolved separately so cashiers who can view a sale do not need Manage Staff.

### Explicit rules

| Rule | Value |
| --- | --- |
| DETAIL/HISTORY | SHOW ACTOR |
| PRIMARY LIST | DO NOT SHOW ACTOR BY DEFAULT |
| CUSTOMER/PERSONAL PUBLIC SURFACES | NO STAFF DISCLOSURE BY DEFAULT |
| RAW GUID NORMAL UI | NO |
| N+1 RESOLUTION | NO |

## Implementation matrix

| Area | Backend actor | DTO actor | Name resolution | React detail | List | Customer-facing |
| --- | --- | --- | --- | --- | --- | --- |
| Sale | `RecordedBy` | `recordedBy` | Batch directory | Sold by | No | Unchanged |
| Void sale | `VoidedBy` | `voidedBy` | Batch directory | Voided by (separate from Sold by) | No | Unchanged |
| Return | `CreatedBy` | `createdBy` | Batch directory | Processed by | No | N/A |
| Inventory movement | `RecordedBy` | `recordedBy` | Batch directory | Movement history | No | N/A |
| Opening stock | via movement `RecordedBy` | same | Batch directory | Movement history | No | N/A |
| Direct Buy | `CreatedByUserId` | `createdByUserId` | Batch directory | Recorded by | No | N/A |
| Purchase order | `OrderedBy` | `orderedBy` | Batch directory | Ordered by | No | N/A |
| Goods receipt | `ReceivedBy` | `receivedBy` | Batch directory | PO detail receipt history | No | N/A |
| Shift open/close/cancel | `OpenedBy` / `ClosedBy` / `CancelledBy` | same | Batch directory | Shift detail | No | N/A |
| Repayment / reversal | `RecordedBy` / `ReversedBy` | same | Batch directory | Customer detail payment history | No | Statement unchanged |
| Seller order fulfillment | `ReadyBy` / `OutForDeliveryBy` / `DeliveredBy` / `CollectedBy` | same | Batch directory | Activity timeline | No | Buyer order UI unchanged |
| Seller order accept/reject/cancel/complete | Domain had actors; detail DTO gap closed | `acceptedBy`, `rejectedBy`, `cancelledBy`, `completedBy` (+ timestamps) | Batch directory | Activity timeline | List DTO lean | Unchanged |
| Stock count | Domain actors | — | — | **DEFERRED** — no React detail route | — | — |
| Inventory transfer | Domain dispatch/receive | — | — | **DEFERRED** — no React detail route | — | — |
| System provider finalization | `ProviderFinalizedBySystem` | bool | UI `System` label | Component supports `isSystem` | — | — |

## Former staff

Membership status `Removed` → display name retained when resolvable + `Former staff` hint. Cross-org / unknown actors → `Not available` (no GUID).

## Privacy

- No email, phone, Personal EX-ID, or Personal profile in actor projection
- Cross-org arbitrary actor IDs do not enumerate global users
- `CUSTOMER_FACING_ACTOR_DISCLOSURE=UNCHANGED`
- `CUSTOMER_STATEMENT_STAFF_DISCLOSURE=UNCHANGED`

## Goods receipt history

Minimal `GET /api/v1/pos/goods-receipts?purchaseOrderId=` wired to existing `GoodsReceiptQueryService.ListForPurchaseOrderAsync`, surfaced on `PurchaseOrderDetailPage`.

## Files (primary)

- Platform: `OrganizationActorDisplayNameUseCases.cs`, membership/user repository batch methods, MembershipEndpoints actor-display-names
- POS: CustomerOrder detail DTO projection gap; goods-receipts list-by-PO endpoint
- React: `actor-directory-client`, `useActorDirectory`, `ActorAttribution`, wired detail pages, locale keys (en, fil-PH, ceb-PH, ilo-PH, hil-PH)
- Tests: Platform resolver unit tests; React ActorAttribution / TransactionSummary / SellerOrderDetail actor tests
