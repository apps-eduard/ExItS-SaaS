# Pinoy Loan Manager — API and Contract Boundary

**Status:** Planning / architecture baseline (documentation only)
**Implementation present:** No
**Last updated:** 2026-08-19

How future PLM APIs relate to Organization Web, MAUI, Personal, and Platform. Not an endpoint catalog.

Related: [source-and-project-layout.md](source-and-project-layout.md), [personal-integration-boundary.md](personal-integration-boundary.md), [platform-commercial-integration.md](platform-commercial-integration.md).

---

## Authority

Server-authoritative business rules remain in Pinoy Loan Manager. UI / API clients must not become a second source of truth.

API DTOs must not expose EF entities.

---

## Consumers (future)

| Consumer | Access |
|---|---|
| Organization Web | Full operational API surface as granted |
| MAUI | Limited field operational API surface |
| ExItS Personal | PLM-authoritative contracts only — never PLM database |
| Platform | Commercial / identity contracts only — never PLM operational tables |

---

## Personal-facing contracts (later)

Plan server-authorized use cases later for:

- available Quick Loan offers
- submit request
- application status
- Loans
- schedules
- payments
- receipts

No endpoint implementation in this package.

---

## Isolation

- no direct Platform table reads
- no direct POS reads
- no cross-product FKs
- OrganizationId as Guid identity / contract only

---

## Explicit non-goals

- OpenAPI / route design
- Authentication scheme (**R-091 Closed for Phase 13 scope**; consume trusted Platform actor only)
- Inventing D-P12-03 transport
