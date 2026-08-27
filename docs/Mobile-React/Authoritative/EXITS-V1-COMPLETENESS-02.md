# EXITS-V1-COMPLETENESS-02 — Operational In-App Notifications + Durable Personal Cart

**Status:** COMPLETE  
**Branch:** `feat/personal`  
**Package:** EXITS-V1-COMPLETENESS-02

## Summary

Delivers two v1 completeness items without a new audit package:

1. **Operational in-app notifications** for ownership-transfer requests and customer-order lifecycle (seller + Personal buyer).
2. **Durable Personal shopping cart** via account-namespaced `localStorage` (non-authoritative convenience state).

## NOTIFICATIONS

### New / solidified relatedTypes

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
- Same-organization seller publish for `CustomerOrder*` is now allowlisted (`OrganizationBusinessNotificationTypes.AllowsSameOrganization`) so seller “New customer order” can persist.

### Delivery timing / consistency

- Ownership notification is created in the same Platform UoW as the transfer insert (after mutation succeeds in-memory, before `SaveChanges`).
- POS → Platform notification publish remains **best-effort post-commit** (HTTP client swallows failures so order mutations stay authoritative). Documented convention preserved.

### Cancelled ownership

- Original Personal notification remains historical/readable.
- Deep link always opens `/personal/ownership-transfers` (current authoritative list; no pending row after cancel).
- No physical delete of notification audit history.

### Intentionally deferred notification scope

- Push / FCM / APNS / OneSignal / SMS / email campaigns
- Marketing / promotional notifications
- Optional former-owner Organization notifications on Accept/Decline (not required for v1 completeness)
- Non-customer-order product events beyond existing producers

## CART

| Item | Decision |
|---|---|
| Storage | `localStorage` key `exits.personal.cart.v1:{platformUserId}` |
| Schema version | `1` |
| Account isolation | Namespaced by authenticated `session.userId` |
| Persisted fields | sellerOrganizationId, organizationDisplayName, lines (productId, name, sku, unitOfMeasure, unitPrice snapshot, quantity) |
| Authoritative price/stock | Server storefront/checkout only; snapshot is display convenience |
| Sensitive data | Not persisted (no tokens, email, address, balances, credentials) |
| Order success | `clearAll()` after successful place response |
| Order failure | Cart preserved |
| Malformed storage | Fail-safe empty cart (no crash) |
| Multi-store | Single merchant; `ensureMerchantCart` clears lines on switch |
| Server cart | **Not** created |
| Offline order queue | **Not** created (`NEW_PERSONAL_WEB_OUTBOX_ENQUEUE=NO`) |

## Explicit non-goals

Push notifications, SMS/email infra, server-side cart, multi-device sync, multi-store cart, offline checkout/payment, wish lists, branding polish.

## Evidence pointers

- Platform: `OwnershipTransferUseCases`, `PublishPersonalBusinessNotification`, `OrganizationBusinessNotificationTypes.AllowsSameOrganization`
- POS: `CustomerOrderLifecycleNotifier` (Personal + Organization buyers), `PlatformPersonalBusinessNotificationClient`
- React: `personal-notifications.ts`, `org-notifications.ts`, `personal-merchant-cart-storage.ts`, `PersonalMerchantCartProvider`
- Tests: Platform ownership/business notification unit tests; Vitest cart storage + deep-link tests; Playwright `e2e/exits-v1-completeness-02.spec.ts`

## Next

`CONTINUE_V1_FEATURE_COMPLETION`
