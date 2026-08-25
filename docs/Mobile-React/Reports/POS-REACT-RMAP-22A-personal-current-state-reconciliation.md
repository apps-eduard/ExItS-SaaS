# POS-REACT-RMAP-22A — Personal Current-State Reconciliation

## Status

**PASS** — reconciliation complete; canonical Personal docs created; roadmap execution order updated (Personal before Offline). No implementation code in this package.

## Preflight

| Check | Result |
| --- | --- |
| Branch | `feat/pos-react-client` |
| `PERSONAL_MASTER_RUN_01_START_SHA` | `584004b98bd6bc360dc0edfec89e6445cc920e43` |
| Local == `origin/feat/pos-react-client` | YES |
| Working tree at start | Clean |

## Baseline (before Personal work)

| Gate | Result |
| --- | --- |
| RMAP-B01 report | **BACKEND IMPLEMENTED** ([POS-REACT-RMAP-B01-sale-price-override-backend.md](./POS-REACT-RMAP-B01-sale-price-override-backend.md)) |
| RMAP-12b report | **COMPLETE** ([POS-REACT-RMAP-12b-price-override.md](./POS-REACT-RMAP-12b-price-override.md)) |
| Master Run 03 | **APPROVED** ([POS-REACT-MASTER-RUN-03.md](./POS-REACT-MASTER-RUN-03.md)); RMAP-15…20 APPROVED |
| `format:check` | PASS |
| `typecheck` | PASS |
| `lint` | PASS (0 errors; existing warnings only) |
| Vitest | **310 passed** / 64 files |
| `build` | PASS |

## Source of truth order (applied)

1. Backend/domain truth  
2. [personal-utang-tracking-domain.md](../../product/personal-utang-tracking-domain.md) (Utang rules remain authoritative)  
3. MAUI functional behavior  
4. Product Owner blueprint (pasted / Downloads attachment)  
5. React implementation  

## Canonical docs created

| Path | Purpose |
| --- | --- |
| [Authoritative/Personal/personal-product-blueprint.md](../Authoritative/Personal/personal-product-blueprint.md) | Reconciled Personal experience blueprint |
| [Authoritative/Personal/personal-implementation-roadmap.md](../Authoritative/Personal/personal-implementation-roadmap.md) | RMAP-22A…H execution map |

Utang security/domain rules were **not** replaced; blueprint expansions that conflict defer to the existing Utang domain doc.

## Capability matrix

| Capability | Backend | MAUI | React | Status | Action |
| --- | --- | --- | --- | --- | --- |
| Personal account / profile / settings | YES (`/api/v1/personal/*`) | YES | Partial (prefs link only) | GAP | RMAP-22B/More |
| Personal Home (Utang-first) | Dashboard API exists | YES (summary) | Thin shell (merchants/orders/prefs) | GAP | RMAP-22B |
| People / contacts | YES | YES | NO | GAP | RMAP-22C |
| Money I lent | YES | YES | NO | GAP | RMAP-22C |
| Money I owe | YES | YES | NO | GAP | RMAP-22C |
| Relationship detail / history / balance | YES | YES | NO | GAP | RMAP-22C |
| Payments / adjustments | YES (entries API) | Deferred primary UI (detail path exists) | NO | GAP | RMAP-22C |
| Due dates / overdue presentation | YES | Partial via lists | NO | GAP | RMAP-22C |
| Invitations lifecycle | YES | YES | Accept route stub only | GAP | RMAP-22D |
| Reminders | YES | Deferred primary UI | NO | GAP | RMAP-22D |
| In-app notifications | YES (`NullPersonalPushNotificationSink` for push) | YES | NO | GAP | RMAP-22D |
| My QR / Public identity | YES (`/api/v1/me/public-identity`) | YES | NO | GAP | RMAP-22D/More |
| Personal To-do | **NOT FOUND** | NO | NO | NEW | RMAP-22E1/E2 |
| Customer link accept/decline | YES | YES | Partial (ordering path) | GAP | RMAP-22F |
| Connected stores / storefront / cart | YES + POS ordering | YES | YES (RMAP-19) | PARTIAL | RMAP-22F polish + Personal nav |
| My Orders | YES | YES | YES (RMAP-19) | PARTIAL | RMAP-22F |
| Start Business | YES | YES | NO UI | GAP | RMAP-22G |
| Trial / subscription / entitlement | YES (Platform catalog/subscription) | Explore/Start path | NO Personal UI | GAP | RMAP-22G |
| Owner Personal ↔ org switch | YES (session model) | YES | Workspace resolver PersonalHome | PARTIAL | RMAP-22G |
| Linked Business Utang statement | Phase-24 POS projection + Platform merchants | YES (WP16) | NO React statement UI | OUT OF SCOPE | RMAP-B04 gated |
| Offline Personal queue | MAUI LocalStore | YES (selective) | NO | OUT OF SCOPE | RMAP-21 gated |

## Personal Utang backend confirmation

Inspected `ExItS.Platform.Domain/Personal`, Application use cases, and `PersonalEndpoints.cs` group `/api/v1/personal/utang/*`.

| Feature | Present |
| --- | --- |
| Contacts | YES |
| Relationships (lent/borrowed) | YES |
| Entries (loan/payment/adjustment) | YES |
| Balances | YES |
| History | YES |
| Due dates | YES (relationship model) |
| Invitations (Pending/Accepted/Declined/Revoked/Expired ops) | YES |
| Reminders (create/list/due/deliver/cancel) | YES |
| Notifications (list/mark read) | YES |
| Optimistic concurrency | YES (domain Version) |
| Audit / delivery audit | YES (`/utang/delivery-audit`) |
| Personal-only authorization | YES |

## Personal To-do

**NOT FOUND** — no `PersonalTodo` / `personal/todo` domain, API, or migration in Platform sources. New additive domain required in RMAP-22E1.

## React Personal gap

`PersonalHomePage` is a lightweight card with links to linked merchants, My Orders, and preferences — **not** Utang-first Home. Router Personal children: home, linked-merchants shop/checkout, orders (RMAP-19). No Utang/People/To-do/Start Business React surfaces.

## MAUI behavior audit

MAUI Personal pages cover Home, People, Lent, Borrowed, relationship detail, invitations, notifications, My QR, linked merchants/statement/receipts, orders/storefront, Start Business, settings/profile/More. P18 report: Payments / Reminders / History **primary UI deferred/hidden**; invite-from-detail deferred; real push deferred. Offline queue for contacts/lent/borrowed/entries — online-required for invitations, linking, QR, Start Business.

## B04 / Phase-24 overlap (do not implement B04)

| Item | Finding |
| --- | --- |
| Phase-24 WP16 | MAUI linked-merchant list + statement **read projection** of POS Business Utang; Platform owns link metadata; POS owns balances/activity |
| ADR-021 | Linked customer statements + personal monetization |
| React RMAP-B04 | Buyer purchase projection **NOT STARTED** / gated |
| Overlap | Reuse existing statement/projection contracts later; do not invent a second ledger; never copy into Personal Utang |
| This run | Document only; `RMAP_B04_AUTHORIZED=NO` |

## Roadmap update

[react-migration-roadmap.md](../Authoritative/Migration/react-migration-roadmap.md) updated so **RMAP-22 Personal runs before RMAP-21 Offline** without renumbering packages. RMAP-21 remains planned/not started.

## Explicit non-starts

RMAP-21 Offline, RMAP-23, RMAP-B04, RMAP-B05, RMAP-TAX, RMAP-24, production cutover, real ad/reward/payment providers, Loan SaaS.

## Next

**RMAP-22B — Personal shell + Home**
