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
