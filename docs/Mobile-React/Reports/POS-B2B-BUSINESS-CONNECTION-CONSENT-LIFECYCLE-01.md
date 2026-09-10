# POS-B2B-BUSINESS-CONNECTION-CONSENT-LIFECYCLE-01

## STATUS

STATUS=CODE_COMPLETE (targeted tests PASS; manual owner acceptance of MICA↔KIZY scenarios still recommended)

START_SHA=595b2ffd (package start; DP fix preserved as `6da698e8`)

FINAL_SHA=0e851ef32601dc5220761895a7af435b86050986
FEATURE_SHA=ba5234d6e284dd53ed649a75e0aef114b173bce6

## Decisions

| Key | Value |
|---|---|
| RELATIONSHIP_MODEL | `ConnectedSupplierRelationship` only |
| INITIATOR_FIELD | `InitiatedByParty` (`Buyer` \| `Supplier`); derived `InitiatorOrganizationId` / `RecipientOrganizationId` |
| BUYER_INITIATED_FLOW | SUPPORTED — Suppliers → Connect → Pending → supplier Accept/Decline |
| SELLER_INITIATED_FLOW | SUPPORTED — Customers → Add business → `POST …/invite-buyer` → Pending → buyer Accept/Decline |
| SELLER_ADD_CREATES_POSCUSTOMER | NO |
| PENDING_STATUS_VISIBLE | YES — Customers → Businesses + Checkout → Businesses |
| BUYER_NOTIFICATION | `BusinessCustomerConnection*` for seller-initiated; classic `SupplierConnection*` for buyer-initiated |
| SUPPLIER_NOTIFICATION | Same split by initiation direction |
| ACCEPT_SUPPORTED | YES — recipient only |
| DECLINE_SUPPORTED | YES — recipient only |
| CANCEL_SUPPORTED | YES — initiator only (Pending → Declined) |
| ACTIVE_CHECKOUT | Selectable Organization buyer |
| PENDING_CHECKOUT | Visible; informational toast; not selected |
| SALE_SERVER_GUARD | `B2bCheckoutBuyerAuthorization` Active-only |
| SUPPLIER_MASTER_ON_BUYER_ACCEPT | YES — exactly one buyer-side connected Supplier on seller-invite Accept |
| NOTIFICATION_DELIVERY_MODEL | Best-effort Platform `OrganizationInAppNotification` after Persist; not transactional outbox |
| AUTHORITATIVE_REQUEST_INBOX | `GET …/relationships/incoming` + Connection requests UI |
| BRANCH_SCOPE | Buyer-initiated respond remains supplier-branch gated; seller invite uses supplier location; buyer Accept is org-level |
| PERMISSION_MODEL | ManageSuppliers for invite/respond/cancel; CreateSale for checkout list |
| DUPLICATE_OPEN_RELATIONSHIP_GUARD | Existing unique open index preserved; friendly duplicate / pending-buyer-request errors |
| LEGACY_ORG_CUSTOMER_HANDLING | Retain historical POSCustomer; prefer open B2B relationship when present; no consent bypass |
| TESTS | `BusinessConnectionConsentLifecycleTests` (+ existing cancel/request lifecycle suite) |

## Flags

```text
B2B_RELATIONSHIP_MODEL=CONNECTED_SUPPLIER_RELATIONSHIP
BUYER_INITIATED_REQUEST=SUPPORTED
SUPPLIER_INITIATED_BUSINESS_INVITATION=SUPPORTED
CONSENT_REQUIRED=YES
PENDING_CHECKOUT_VISIBLE=YES
PENDING_CHECKOUT_SELECTABLE=NO
ACTIVE_CHECKOUT_SELECTABLE=YES
B2B_DUPLICATE_POSCUSTOMER=NO
SELLER_ADD_BUSINESS_CREATES_ACTIVE=NO
NOTIFICATION_INBOX=PLATFORM_ORGANIZATION_IN_APP_NOTIFICATION
ACCEPT_DECLINE_REQUIRED=YES
REQUEST_READ_DOES_NOT_ACCEPT=YES
```

## Explicit exclusions

- No KIZY Direct Purchases B2B history work in this package (already delivered separately)
- No new message broker / durable outbox for notifications
- No WebSocket/SignalR requirement
- No automatic Accept
- No second relationship model / CustomerOrder path for B2B Direct

## Evidence

- Migration `20260910140000_AddConnectedSupplierInitiatedByParty`
- Unit: `BusinessConnectionConsentLifecycleTests` (13) PASS with related ConnectedSupplier suites
- React: invite UI, incoming inbox, checkout Pending toast, notification deep links
