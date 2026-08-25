# ExItS Personal Product Blueprint (React / PWA)

**Status:** Authoritative for React/PWA Personal experience (reconciled)  
**Source order:** (1) backend/domain truth, (2) existing Personal docs, (3) MAUI behavior, (4) Product Owner blueprint, (5) React implementation  
**Primary product decision:** **Personal Utang is the main Personal feature.**  
**New first-class requirement:** **Personal To-dos** (private-by-default; new domain — see implementation roadmap).

## Personal shell (top bar)

```text
[ Identity ]     [ Connection ] [ Notifications ] [ Menu ]
```

- **Notifications:** first-class top-bar entry (bell + unread badge from Personal notification list).
- **Connection:** real browser/application Online / Offline indication + **Refresh data** (query invalidation). Not “Connection & Sync”.
- **Connection & Sync:** deferred to **RMAP-21** when LocalStore/outbox exist. Do not claim “All changes synced” until then.

`RMAP_22_PERSONAL_MASTER_RUN_01=APPROVED`

## Relationship to other docs

| Document | Role |
| --- | --- |
| [personal-utang-tracking-domain.md](../../../product/personal-utang-tracking-domain.md) | **Authoritative** for Personal Utang security, linking, privacy, and domain rules. Do not weaken. |
| Product Owner attachment `exits-personal-product-blueprint.md` | Broader Personal experience design source; reconciled here. |
| ADR-019 / ADR-020 / ADR-021 | Identity, client boundaries, linked-customer statements / monetization. |
| This file | React/PWA Personal product experience + navigation + MVP scope. |

Utang contract conflicts resolve in favor of `docs/product/personal-utang-tracking-domain.md`.

## Product purpose

Personal is the user-owned side of ExItS. It must work without Organization, POS subscription, staff membership, or business role.

Priority needs:

1. Personal Utang
2. Personal To-dos
3. Reminders / in-app notifications
4. Connected stores + My Orders (customer link)
5. My Businesses (Start a Business / enter owned org)
6. Profile / ExItS ID / QR / settings
7. Later: linked Business Utang statement / My Purchases (RMAP-B04 gated), rewards / ad-free

## Authority boundaries (never merge)

```text
Personal Utang            != Organization customer credit
Personal User             != Organization staff principal
Personal To-do            != Business / staff task
Linked business statement != copied Personal Utang
Customer order            != POS sale ownership
```

## Navigation (React/PWA)

Primary (mobile-first):

```text
Home | Utang | To-do | Orders | More
```

Do not expose POS business chrome to a Personal-only session.

### Home (Utang-first)

- Utang summary: Owed to me, I owe, Due soon, Overdue
- To-do summary: Due today, Overdue, Upcoming (after RMAP-22E)
- Quick actions: Money I lent, Money I owe, Record payment, Add person, Add To-do
- Recent activity / upcoming reminders / active order status when useful
- No organization administration on Home

### Terminology (user-facing)

Prefer: Money I lent, Money I owe, Owed to me, I owe, People, Record payment, Reminder, To-do, Stores, My Orders, Start a Business.

Avoid: DebtRelationship, PersonalUtangEntry, CreditorUserIdentityId, LinkedCustomerAppUser, DTO, GUID, backend/contract jargon.

## Personal Utang

Reuse Platform `/api/v1/personal/utang/*`. Do not invent a second Utang backend.

Required React surfaces: People, Money I lent, Money I owe, relationship detail, payment, adjustment (less prominent), due dates, history/balance from server, invitations, reminders, notifications.

Linking: explicit invitation accept only. Never silent link by email/phone/name/QR alone. QR/Public ID assists identification only.

Payments are ledger records — not payment-gateway money movement.

Optimistic concurrency remains authoritative (409 → refresh/reconcile).

Proposed later (not RMAP-22): acknowledgment / dispute workflow.

## Personal To-dos

**Backend:** NOT FOUND in repository at RMAP-22A inspection — new domain required (RMAP-22E1).

Default: private to owning Personal user. Related-entity metadata does not grant authorization.

Statuses: Open | Completed | Cancelled. Priorities: None | Low | Normal | High.

`PersonalTodoReminder != PersonalUtangReminder` (shared delivery OK; domains separate).

Offline To-do: design for future RMAP-21; **do not implement offline in RMAP-22**.

## Stores / ordering

Reuse RMAP-19 customer ordering + Platform customer-link APIs. User wording: Stores / Connected stores (not LinkedMerchants).

Customer link grants customer-safe access only — never staff membership or POS role.

## Business Utang / My Purchases (gated)

Phase-24 already delivers MAUI linked-merchant statement read projection (Platform link metadata + POS statement APIs). RMAP-B04 remains separately gated for React buyer purchase projection. Do not copy Business Utang into Personal Utang. Do not expand B04 inside RMAP-22.

## Start Business / subscription

Use existing `/api/v1/personal/start-business` and catalog/trial/subscription contracts. No fake production payment provider. Owner Personal ↔ Organization switch follows approved identity model; staff principals cannot impersonate Personal.

## Offline

RMAP-22 is **online-first**. MAUI Personal offline is reference only. RMAP-21 remains planned and unauthorized (`RMAP_21_AUTHORIZED=NO`).

## Explicit exclusions

Loan SaaS, interest engines, collector routes, underwriting, POS inventory/accounting, staff task systems, real ad/reward providers, production cutover.

## Security acceptance (summary)

- Utang without organization
- Cross-user Personal denial
- Org roles / staff LinkedPersonalUserId do not open Personal data
- Unlinked contacts do not leak account existence
- Explicit invitation acceptance
- Minimized notification debt previews
- To-dos owner-private
- Customer link fail-closed; no accidental staff grant
- Five locales: en, fil-PH, ceb-PH, ilo-PH, hil-PH (native-speaker certification PENDING)

## Core statement

> ExItS Personal helps you track personal Utang, remember what you need to do, and keep your customer records with businesses you use — without needing to own a business.
