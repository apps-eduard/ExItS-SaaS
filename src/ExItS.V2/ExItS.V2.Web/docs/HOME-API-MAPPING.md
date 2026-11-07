# ExItS V2 Home — existing API mapping

See also: [HOME-PARITY-MATRIX.md](./HOME-PARITY-MATRIX.md).

## Personal Home

| Section | Endpoint | Fields |
|---|---|---|
| Tracker | `GET /api/v1/personal/dashboard` | `totalLentBalance`, `totalBorrowedBalance`, `sharedWithMe*`, counts, `pendingConfirmationCount` |
| Attention | dashboard + `…/utang/relationships/lent\|borrowed` + `…/utang/contacts` | due buckets ≤3 days = due soon |
| Stores to pay | `GET /api/v1/personal/linked-merchants` + buyer session token + `GET /api/v1/pos/personal/linked-customers/{id}/statement` | outstanding balances |
| Businesses | `GET /api/v1/personal/product-affiliations`, `GET /api/v1/personal/workplaces` | owned vs staff |
| To-do | `GET /api/v1/personal/todos` | Today / Upcoming / Overdue counts |
| Guide | localStorage `exits.v2.personal.guide.v1:{userId}` | dismissible |

## Business Home

| Section | Endpoint | Fields |
|---|---|---|
| Today | `GET /api/v1/pos/management/overview` after org/branch bind + session grant | sales, sale count, open utang, low stock, open shifts |
| Attention | same overview | low stock, near expiry, expired, utang, open shifts |

## Not wired yet (documented gaps)

- Platform plan / branch / area / device capacity meters (Manage Business glance)
- V2 destination routes for Utang, Todo, People, Stores, Org admin shortcuts
