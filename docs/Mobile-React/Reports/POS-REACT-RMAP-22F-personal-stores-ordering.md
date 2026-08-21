# POS-REACT-RMAP-22F — Personal stores, customer links, and orders

## Status

**PASS** (Personal polish on existing RMAP-19 ordering + Platform customer-link APIs)

**Base tip:** `519fa41bee8d9f3991a19bd0fda7edfd80efc1d5` (RMAP-22E2 Personal To-do React UX)

## Already present (RMAP-19 / Personal shell)

- `LinkedMerchantsPage`, `MerchantShopPage`, checkout, `MyOrdersPage` / detail under PersonalShell routes
- Personal More → Stores (`/personal/linked-merchants`)
- Personal bottom nav Orders → `/personal/orders`
- Platform APIs: `GET/POST …/personal/customer-link-requests` (list, accept-by-id, decline-by-id)
- Linked merchants + ordering-capability clients

## Newly added (this package)

- React client: `customer-link-requests-client.ts` (list / accept / decline + PascalCase normalize)
- Personal UX: `/personal/customer-links` list + Accept + Decline
- More menu link to customer link requests; Stores page cross-link; notification deep-link when `RelatedType=CustomerLinkRequest`
- User wording: Personal surfaces prefer **Stores** / connected stores (5 locales; message-key parity)
- Unit tests: customer-link + linked-merchants clients; Personal shell route coverage; message-parity

## Explicit non-claims

- RMAP-B04 statement / buyer purchase projection expansion
- Offline / LocalStore / outbox (RMAP-21)
- Rebuilding storefront ordering
- Native-speaker certification of PH locales

## Tests

| Suite | Result |
| --- | --- |
| `customer-link-requests-client.test.ts` | PASS |
| `linked-merchants-client.test.ts` | PASS |
| `message-parity.test.ts` | PASS |
| `personal-shell-home.test.tsx` | PASS |
| `typecheck` | PASS |

Native-speaker certification: **PENDING**

## Next

**RMAP-22G — Start Business + trial/subscription + workspace** (do not start until authorized)
