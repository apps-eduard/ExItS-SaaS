# React/PWA Offline Capability Matrix (RMAP-21)

**Status:** LOCKED for Master Run 01 (historical). **Current Organization Web channel policy: ORG-PWA-ONLINE-ONLY-01.**
**Authority:** Backend truth → MAUI offline contracts → this matrix → React implementation  
**Start SHA:** `86ded4380c6c1d45ef89ef08855c20fb00f17d38`  
**Unknown actions:** fail closed as **OnlineRequired**

## Current channel policy (authoritative)

| Channel | Organization business | Personal Web | Offline engine |
| --- | --- | --- | --- |
| **Organization Web/PWA** | **ONLINE-ONLY** — no offline session, no offline money transactions, no new outbox enqueue, no offline business mutations | N/A | Preserved for future Capacitor/native; not activated on Web |
| **Personal Web/PWA** | N/A | **ONLINE-ONLY** (PERS-WEB-ONLINE-ONLY-01) — no offline session/PIN gate, no new Todo/Utang/People outbox enqueue | Preserved for future Capacitor/native; not activated on Web |
| PWA installability + static shell cache | Yes | Yes | N/A |
| Business/Personal API data | Server-authoritative (`NetworkOnly` in service worker) | Server-authoritative (`NetworkOnly`) | N/A |
| Backend offline contracts (grants, idempotency, reconciliation) | Preserved | Preserved (PERS-IDEM-01) | Preserved |

Runtime sources of truth: `organizationWebRuntimePolicy`, `personalWebRuntimePolicy`.

Historical rows below remain evidence of the RMAP-21 engine design. For **Organization Web/PWA** and **Personal Web/PWA**, treat OfflineCapable / Queueable rows as **disabled at Web activation** (engine code retained).

## Classification

| Label | Meaning |
| --- | --- |
| OfflineCapable | Safe read/UX from LocalStore / warm session without mutation |
| Queueable | May mutate locally + outbox; sync with server idempotency |
| OnlineRequired | Blocked offline with Internet required guard |

## Application shell

| Action | Class | Notes |
| --- | --- | --- |
| PWA static shell from precache | OfflineCapable | API remains NetworkOnly |
| Connection & Sync panel | OfflineCapable | Real outbox counts only (21C+) |
| Personal / Org top bars | OfflineCapable | |
| Switch workspace / org | OnlineRequired | |
| Personal ↔ Organization switch | OnlineRequired | |
| Sign-in (first / cold) | OnlineRequired | |
| Cold-start unlock of protected LocalStore | Queueable | Server-signed offline operating grant (RMAP-21-FIX02) + offline PIN-wrapped DEK; requires prior online branch bind, authorized POS device, and PIN enrollment |

## POS Sell / checkout

| Action | Class | Notes |
| --- | --- | --- |
| Browse cached catalog (category/product/UOM/price) | OfflineCapable | Warm session + prior online refresh |
| Product search (barcode/SKU/name) | OfflineCapable | |
| SellReadinessGate device snapshot | OfflineCapable | No offline device register |
| SellReadinessGate open-shift snapshot | OfflineCapable | No offline open/close shift |
| Cash checkout | Queueable | Requires durable saleId + server replay/idempotency, and a live server-signed price lease for every line (Review Repair 01) |
| Offline Cash line pricing | Server-authoritative | Billed from the signed lease, not the catalog at sync and not any price the client asserts |
| Cash checkout with no lease for a line | OnlineRequired | "Connect to refresh prices before selling." |
| ManualGCash / GCash | OnlineRequired | |
| Business Utang checkout | OnlineRequired | Not assumed from customer-credit queue |
| Commercial Discount | OnlineRequired | Server fail-closed offline |
| Price Override | OnlineRequired | Server fail-closed offline |
| Lot/expiry-sensitive allocation | OnlineRequired | Unless proven snapshot contract |
| Void / returns / refunds | OnlineRequired | |

## Device / shift / admin

| Action | Class |
| --- | --- |
| Device register / code / revoke | OnlineRequired |
| Open shift / close shift | OnlineRequired |
| Staff invite / roles / permissions | OnlineRequired |
| Branch fulfillment admin | OnlineRequired |
| Inventory admin / count / adjust | OnlineRequired |
| Purchasing / GRN / suppliers | OnlineRequired |
| Reports | OnlineRequired |
| Billing / Start Business | OnlineRequired |

## Business customers

| Action | Class | Notes |
| --- | --- | --- |
| Cached customer list/detail | OfflineCapable | Minimize sensitive fields |
| Create/edit customer | Queueable | Only with proven idempotency headers |
| Credit / repayment mutations | Queueable | MAUI contract; inspect before enable |
| Business Utang as sale payment | OnlineRequired | |

## Personal

| Action | Class | Notes |
| --- | --- | --- |
| People / lent / borrowed local projection | OfflineCapable | Personal LocalStore only |
| Contact upsert | Queueable | No silent linking |
| Relationship create | Queueable | Dependency after contact |
| Utang entry record | Queueable | |
| Invitations / accept / QR / public ID | OnlineRequired | |
| Customer-link / My Orders / store order | OnlineRequired | |
| Personal To-do CRUD | Queueable | Private-by-default; no fake push |
| Staff principal reading Personal LocalStore | Forbidden | Isolation |

## Security notes

- Session grant / POS bearer: memory only (warm session).
- No password / refresh / antiforgery persistence for offline.
- Diagnostics: origin+pathname only; no query/fragment; no payload dumps (21A.0).
- Personal vs Organization LocalStore must never merge for same human Owner.
- Offline Cash prices are server-signed leases bound to organization, branch, product, and sell unit, with a bounded window (default 8 hours). The client stores and replays the server's bytes; it can never mint, extend, or edit a lease, and an unreadable lease cache blocks the sale.
