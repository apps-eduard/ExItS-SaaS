# Sales buyer party model

[Identity boundaries](../architecture/personal-organization-identity-boundaries.md) · [QR spec](../specs/identity/public-user-id-and-qr.md) · [Connected suppliers](connected-exits-suppliers.md)

## Rule

Every POS sale is owned by the **selling organization** (`Sale.OrganizationId`).

The buyer is a **counterparty**, never the transaction owner.

| Buyer kind | Meaning |
|---|---|
| WalkIn | No customer / anonymous checkout |
| ExternalCustomer | Seller-owned `POSCustomer` without ExItS identity on the sale |
| Personal | ExItS Personal public identity (`EX-…`) as buyer |
| Organization | ExItS Business identity (`ORG######`) as buyer |

Actor (`RecordedBy`) is who operated the till. It does not own the sale.

Operational mutations (sales, inventory, orders, shifts) store actor GUIDs on authoritative aggregates — not client-supplied ids. Fulfillment handoffs store `ReadyBy`, `DeliveredBy`, etc. Provider payment finalization uses `ProviderFinalizedBySystem` instead of attributing gateway work to a fake user. See [P28-WP15D operational actor traceability](../reports/P28-WP15D-operational-actor-traceability.md).

## Customer record vs ExItS identity

A seller may keep an org-owned `POSCustomer` profile and optionally link:

- Personal public user id, or
- Buyer organization id + public organization id

Those links do **not** grant the seller access to Personal Utang, Personal contacts, or the buyer organization's private POS data.

Do not auto-merge customers by name/phone/email.

## QR purpose matrix (checkout / suppliers)

| Flow | Personal QR | Business QR | Device QR |
|---|---|---|---|
| Sale customer selection | Allow | Allow | Reject |
| Connected supplier | Reject | Allow | Reject |
| Device registration | Reject | Reject | Allow |
| Personal contact flow | Allow | Reject / explicit business action | Reject |

Server-side connected-supplier requests require Business QR / `ORG######` and reject Personal/device payloads even if a Guid is forged by the client.

## Business vs Personal Utang

POS Product-Based Utang remains seller-organization owned via `POSCustomer` + `CreditEntry`.

Linking a Personal ExItS buyer on a credit sale does **not** write Personal Utang.

## Offline / LocalStore

LocalStore remains **v9** (file per user/org/product). Buyer party fields travel on `CheckoutSaleRequest` / receipt snapshots; no LocalStore schema bump.

Org switch continues to clear SaleCart and selling/device context so Org A buyer selection cannot bleed into Org B.

## Ownership transfer readiness

Historical sales keep `OrganizationId` and buyer snapshots. Buyer Organization identity stays on `BuyerOrganizationId` / public org id, not the current owner user.

## Future — Linked ExItS buyer purchase projection (RMAP-B04 — NOT STARTED)

Current backend reports already defer Personal purchase history of merchant sales and B2B buyer-organization views of seller sales.

**Future rule (owner-confirmed intent; not implemented):**

A Completed sale with Personal (`EX-…`) or Organization (`ORG######`) `SaleBuyerParty` may be projected **read-only** into the authenticated buyer's purchase history.

| Rule | Requirement |
|------|-------------|
| Authority | Seller `Sale` remains authoritative; do not transfer transaction ownership |
| Scope | Personal sees only purchases linked to that Personal identity; Organization sees only purchases linked to that Organization |
| Privacy | Seller internal notes / private customer fields not exposed |
| Status | Void/refund status reflected |
| Isolation | No cross-org DB access shortcut; authorization enforced before projection |
| Review | Privacy/retention review required before implementation |
| Documents | Transaction Summary vs future tax document wording preserved |

UI may later land in RMAP-13 / RMAP-22 / Organization purchase-history surfaces. Cashier customer selection remains optional; walk-in remains valid. Buyer identity never grants seller access to Personal private data, buyer Organization POS data, membership, role, or cross-org authorization.
