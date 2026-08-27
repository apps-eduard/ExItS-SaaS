# EXITS-V1-COMPLETENESS-02 — Ordering Readiness + Operational Notifications + Durable Personal Cart

**Status:** COMPLETE
**Branch:** `feat/personal`
**Package:** EXITS-V1-COMPLETENESS-02
**Baseline:** `31be4feec954e0a5d43ab56416c4f6b5c7a9cb05`

## Summary

Delivers three bounded v1 completeness items in one package (not an audit):

1. **Public store ordering readiness correction** — `OrderingAvailable` uses Platform branch fulfillment readiness, not “active Organization ⇒ ready”.
2. **Operational in-app notifications** for ownership-transfer requests and customer-order lifecycle (seller + Personal buyer).
3. **Durable Personal shopping cart** via account-namespaced `localStorage` (non-authoritative convenience state).

## A. PUBLIC STORE ORDERING READINESS

| Item | Decision |
|---|---|
| Hardcoded `OrderingAvailable: true` | **Removed** |
| Source | Platform `IBranchFulfillmentReadinessEvaluator` + entitlements + Active branches |
| True when | StoreCustomerOrdering entitlement **and** ≥1 Active branch with `CustomerOrderingEnabled`, not paused, and `CustomerOrderingReady` |
| False when | Active org lacks entitlement, ready branch, or ordering enabled |
| Unknown / inactive | Generic unavailable (unchanged) |
| Cross-product | No POS DB access; no new FKs; reuses existing Platform fulfillment readiness |
| Open-now | Not required for anonymous flag; authenticated storefront remains operational authority |
| Landing UX | Name + Continue in ExItS; shows unavailable hint only when `orderingAvailable === false` — never falsely promises ordering |

## B. NOTIFICATIONS

### relatedTypes / recipients / deep links

| relatedType | Inbox | Producer | Recipient | Deep link |
|---|---|---|---|---|
| `OrganizationOwnershipTransfer` | Personal | `RequestOwnershipTransfer` (Platform) | Target Personal user | `/personal/ownership-transfers` |
| `CustomerOrderSubmitted` | Organization | `PlaceCustomerOrder` (POS → Platform business-notifications) | Seller org Owner/Admin | `/orders/{orderId}` |
| `CustomerOrderAccepted` | Personal (or buyer org) | Accept mutation | Buyer | `/personal/orders/{orderId}` or buyer-org inbox |
| `CustomerOrderRejected` | Personal (or buyer org) | Reject mutation | Buyer | same |
| `CustomerOrderCancelled` | Personal (or buyer org) + seller org local | Cancel mutation | Buyer (+ seller local) | same |
| `CustomerOrderReady` | Personal (or buyer org) | Mark ready | Buyer | same |
| `CustomerOrderOutForDelivery` | Personal (or buyer org) | Out for delivery | Buyer | same |
| `CustomerOrderDelivered` | Personal (or buyer org) | Delivered | Buyer | same |
| `CustomerOrderCollected` | Personal (or buyer org) | Collected | Buyer | same |
| `CustomerOrderCompleted` | Personal (or buyer org) | Complete | Buyer | same |

`StartPreparing` intentionally does **not** emit a buyer notification (noise).

### Duplicate protection

- Organization: `(RecipientUserIdentityId, RelatedType, RelatedId)` via `FindByRecipientRelatedAsync` (existing).
- Personal ownership: same pattern on `OrganizationOwnershipTransfer` + transfer id.
- Personal customer-order: `PublishPersonalBusinessNotification` dedupes the same way.
- Same-organization seller publish for `CustomerOrder*` is allowlisted (`OrganizationBusinessNotificationTypes.AllowsSameOrganization`).

### Delivery timing / consistency

- Ownership notification is created in the same Platform UoW as the transfer insert (after mutation succeeds in-memory, before `SaveChanges`).
- POS → Platform notification publish remains **best-effort post-commit** (HTTP client swallows failures so order mutations stay authoritative).

### Cancelled ownership

- Original Personal notification remains historical/readable.
- Deep link always opens `/personal/ownership-transfers` (authoritative list; no pending row after cancel).

### Intentionally deferred

Push / FCM / APNS / OneSignal / SMS / email campaigns; marketing notifications.

## C. CART

| Item | Decision |
|---|---|
| Storage | `localStorage` key `exits.personal.cart.v1:{platformUserId}` |
| Schema version | `1` |
| Account isolation | Namespaced by authenticated `session.userId` |
| Persisted fields | sellerOrganizationId, organizationDisplayName, lines (productId, name, sku, unitOfMeasure, unitPrice snapshot, quantity) |
| Authoritative price/stock/fulfillment | Server storefront/checkout only; snapshot is display convenience |
| Sensitive data | Not persisted (no tokens, email, address, balances, credentials) |
| Order success | `clearAll()` after successful place response |
| Order failure / ambiguous | Cart preserved until authoritative success |
| Malformed storage | Fail-safe empty cart (no crash) |
| Multi-store | Single merchant; `ensureMerchantCart` clears lines on switch |
| Server cart | **Not** created |
| Offline order queue | **Not** created (`NEW_PERSONAL_WEB_OUTBOX_ENQUEUE=NO`) |

## Online-only boundary

`PERSONAL_WEB_POLICY=ONLINE_ONLY`
`ORGANIZATION_WEB_POLICY=ONLINE_ONLY`
Notification list/read and order/ownership actions remain online-only. Cart durability is local convenience only.

## Migrations

`MIGRATION_CREATED=NO` — reuses existing notification + branch readiness persistence.

## Explicit non-goals

Push notifications, SMS/email infra, server-side cart, multi-device sync, multi-store cart, offline checkout/payment, public-store CMS, branding polish.

## Evidence pointers

- Platform: `LookupPublicStoreLanding` (fulfillment readiness), `OwnershipTransferUseCases`, `PublishPersonalBusinessNotification`, `OrganizationBusinessNotificationTypes.AllowsSameOrganization`
- POS: `CustomerOrderLifecycleNotifier`, `PlatformPersonalBusinessNotificationClient`
- React: `personal-notifications.ts`, `org-notifications.ts`, `personal-merchant-cart-storage.ts`, `PersonalMerchantCartProvider`, `PublicStoreLandingPage`
- Tests: `PublicStoreLandingLookupTests`; Platform ownership/business notification unit tests; Vitest cart + deep-link tests; Playwright `e2e/exits-v1-completeness-02.spec.ts`

## Next

`CONTINUE_V1_FEATURE_COMPLETION`
