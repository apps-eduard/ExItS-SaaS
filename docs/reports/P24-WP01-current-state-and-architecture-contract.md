# P24-WP01 — Current State and Architecture Contract

[Phase 24](../phases/phase-24-linked-customer-statements-and-personal-monetization.md) | [ADR-021](../decisions/ADR-021-linked-customer-statements-and-personal-monetization.md) | [Portfolio](../portfolio-progress.md)

| Field | Value |
|---|---|
| Status | **Complete** (architecture contract; no application code in WP01) |
| Date | 2026-08-12 |
| Repo HEAD at audit | `2fdcc8ab86f8a1df516053930885b6df04b0e436` on `main` |
| Device Verified | **No** |
| Production Ready | **No** |
| Migration added | **No** |
| Application code changed | **No** |

## Status legend

This report is the Phase 24 contract. It is **Complete** as an architecture contract (no application code in WP01). It is **not** Device Verified and **not** Production Ready. Phase 23 remains Open (WP12 in progress; WP13 **not started**). WP01 does not mix P23 closeout.

## Confirmed exists today (reusable)

### Customer link (Platform)

| Type | Path | Reuse |
|---|---|---|
| `BusinessCustomer` | `src/Platform/ExItS.Platform.Domain/Organizations/BusinessCustomer.cs` | Org-owned customer; `LinkedUserIdentityId`; `LinkAppUser` / `UnlinkAppUser`; never staff |
| `CustomerLinkRequest` | `.../CustomerLinkRequest.cs` | Token + email; statuses Pending/Active/Declined/Revoked/Expired; 7-day default |
| `LinkedCustomerAppUser` | `.../LinkedCustomerAppUser.cs` | Created on accept; `IsOrganizationStaff => false`; `GrantsProductRole => false`; `Revoke()` exists |
| `CustomerStaffSeparationGuard` | `.../CustomerStaffSeparationGuard.cs` | Hard deny customer→staff and link-created membership |
| Use cases | `.../Application/Organizations/CustomerLinkUseCases.cs` | Create/resend/revoke **pending** request; accept/decline |
| APIs | `.../Api/Organizations/BusinessCustomerEndpoints.cs` | Org staff manage; `POST /api/v1/organizations/customer-link-requests/accept\|decline` (scope-guard exempt) |
| Persistence | `platform.business_customers`, `customer_link_requests`, `linked_customer_app_users` | Unique active link per customer |

Accept does **not** create `OrganizationMembership` or product roles. Integration coverage: `ApiOrganizationStaffCustomerSeparationTests`.

### POS Business Utang (authoritative ledger)

| Piece | Reality |
|---|---|
| No credit-account table | Account = `POSCustomer` + derived outstanding |
| `CreditEntry` | `pos.credit_entries`; optional `SourceSaleId` for product-based Utang |
| `Repayment` | `pos.repayments`; **not** allocated to a specific credit row |
| Outstanding | `Sum(active credits) − Sum(active repayments)` in `OutstandingBalanceService` |
| Partial payment | `CreateRepayment` with amount `< outstanding`; existing overpayment rejection stays authoritative |
| FIFO remaining | `CreditFifoAging` **read model only** — do not persist as a second truth |
| Ledger query | `UtangLedgerQuery` UNION of credits + repayments; running balance in memory |
| Staff statement | `GET /api/v1/pos/customers/{id}/statement` — **full period ledger lines**, no sale SKUs |
| Sale receipt lines | `PosSaleDto.Lines` via sale GET; not on the statement |
| Entitlement | `customer-credit-view` / `repay` / `create` — **org staff**, not linked customers |
| Offline cache | Cashier-device encrypted SQLite; not the customer’s Personal phone |

Do **not** reimplement partial-payment calculation. Project repayment rows as ledger activity.

### Personal scope

- Personal APIs: `/api/v1/personal/*` (Utang contacts/relationships/history, reminders, notifications, start-business).
- MAUI Personal shell: Home / People / Lent / Borrowed / More — **Personal Utang only**.
- No “my linked merchants” list. No customer-link accept page (staff invite page is different).
- `IPlatformAccessClient` has **no** customer-link methods.
- Personal lists are mostly **unpaged** (notifications `take: 50`).

### Entitlements / billing

- `FeatureGrantSpec` + `FeatureCode` (hyphenated `^[a-z0-9]+(?:-[a-z0-9]+)*$`, **no dots**).
- `Subscription`, `EntitlementSnapshot`, `FeatureOverride`, `SaaSPayment` all require **`OrganizationId`**.
- Personal Utang is explicitly **free** (ADR-019). Paid path today = Start a Business → **new Organization** plan.
- Manual cash/bank/GCash SaaS payments exist for **org** subscriptions. No Stripe. No Personal paid subject.
- Admin Plans/Entitlements (Ant Design) are org/product scoped. Feature CRUD API exists; no dedicated Features page.

### Notifications / pagination / ads / rewards

- Personal in-app + reminder foundation exists; push sink is **null**; no customer-link notifications or email send of link tokens.
- Platform/POS list APIs: **offset** `page`/`pageSize` (default 20, max 100). **No cursor/keyset** today.
- **No** ads, AdMob, RewardPoint, wallet, or Personal paywall code.

### Identity

- Personal/Owner: real email login; `HomeOrganizationId` null.
- Org staff: `<local>@ORG######`; `HomeOrganizationId` set; `OrganizationContextLocked`.
- Accept currently matches `NormalizedEmail` only — **no** explicit `AccountClass.Personal` / `HomeOrganizationId` guard (staff are practically blocked by login shape, not by a hard rule).
- Public User ID / QR (`EX-####-####`) is display/scan only; customer link is email+token, not QR.

## Actually missing

| Capability | Gap |
|---|---|
| POS ↔ Platform customer join | `POSCustomer` has **no** `BusinessCustomerId` / `PlatformUserId`. Notes may contain `exits-id:` tags — not an authority. |
| Personal list of linked merchants | `ILinkedCustomerAppUserRepository` has `FindActiveByUserAndOrganizationAsync`; **no** list-by-user API |
| Revoke accepted link | Domain `Revoke()` unused; `UnlinkAppUser` unused; audit action `platform.linked_customer_app_user.revoked` unused |
| Customer-facing POS statement | All POS credit APIs are staff + org header + Utang capability |
| Receipt lines on a customer statement | Staff statement is ledger remarks only; SKUs require `SourceSaleId` → sale GET |
| Platform→POS read | Platform has no POS HTTP client. Existing pattern is **POS → Platform** (catalog, token introspect) |
| Personal entitlement subject | Cannot grant features to a Personal user without a new subject type |
| `personal-*` feature codes | None. Dotted `personal.*` strings in-repo are **error/audit** codes, not `FeatureCode` |
| Reward ledger / ads | Greenfield |
| MAUI customer-link accept UX | Token returned only to org staff create/resend |
| Opening-balance vs POS ledger | Platform `BusinessCreditOpeningBalance` is **not** written into `pos.credit_entries` (owner migration, not linked-customer statements) |

## Architecture decision (frozen)

See [ADR-021](../decisions/ADR-021-linked-customer-statements-and-personal-monetization.md).

### Business Utang source of truth

POS `credit_entries` + `repayments`. Outstanding = active credits − active repayments. Personal Utang (`PersonalDebtRelationship.CurrentBalance`) is a **different** ledger and must not receive copies.

### CustomerLink authorization path

```text
Personal session
  → Platform: active LinkedCustomerAppUser (user + org + BusinessCustomer)
  → POS: correlate POSCustomer via PlatformBusinessCustomerId value
  → POS: project outstanding + activity from POS ledger
```

Linking never grants staff/product access. Server-side checks are mandatory.

### Read-model / data strategy

- Do **not** duplicate full POS history into Platform.
- POS hosts statement/activity/detail APIs; Platform hosts link directory + Personal entitlements + rewards.
- If a cached projection is added later: rebuildable, POS ledger remains authority, no second outstanding, minimal payloads (ADR-012).
- Opening balances from Personal→Business **owner** migration stay out of this linked-customer path unless/until they exist as POS ledger rows.

### Free vs paid + open-debt exception

Free: current outstanding, recent page, receipt summaries, lazy detail, enough provenance for **open** debt even outside the free window.

Paid/unlockable: older **settled** history, extended search, historical statements, export later.

Never hide/paywall information necessary to understand a currently outstanding debt.

### Bandwidth strategy

Small summaries; server-enforced page size (default 10–20, max 20); lazy detail; explicit older-history requests. Prefer cursor/keyset for Personal activity. Do not infinite-scroll full history. Do not delete records to save bandwidth.

### Lazy receipt strategy

Activity/summary DTOs **must not** include sale line items. `GET` detail by activity/sale/credit id after tap. Join `CreditEntry.SourceSaleId` → `Sale.Lines` only on that path. Omit staff notes, cost/margin, private audit fields.

### Reward-point rules

Personal only; no cash value; no cash-out; no transfer; cannot pay merchant Utang; cannot pay Organization subscriptions/add-ons; cannot convert to pesos; redemption only for configured eligible Personal features; append-only `RewardTransaction` ledger; idempotent claims; concurrent-redemption safe; anti-abuse limits later; expiration only if explicitly designed.

### Personal-only monetization / Organization exclusion

New Personal product + Personal entitlement subject. Do **not** assign an Organization Plan to a Personal account. Organization features reject `RewardPoints`. Cash unlock for Personal is allowed later via a Personal payment subject — not by creating a dummy org.

### Ads integration boundary

No SDK in this phase. Abstractions: free-Personal eligibility, Ad-Free entitlement, optional rewarded-ad claim, provider-neutral verifier, daily limits. Critical debt and account-security info must never require an ad. No fake playback.

### Cold archive boundary

Keep query contracts (summary / recent / older / detail) stable so a later store split can be transparent. Do not build archive tables or delete history in Phase 24.

## Recommended DTO / API sketch (not implemented)

Names may be adjusted in WP04 to match existing `Pos*` client DTO conventions.

```text
Platform (Personal session)
  GET  /api/v1/personal/linked-merchants
       → org display name, businessCustomerId, link status, owning product code
       → NO outstanding, NO ledger

POS (Personal session + link verify)
  GET  /api/v1/pos/linked-customer/statements/{businessCustomerId}
       → merchant display, outstanding, currency
  GET  /api/v1/pos/linked-customer/statements/{businessCustomerId}/activity
       → cursor, pageSize≤20, receipt summaries only
  GET  /api/v1/pos/linked-customer/activity/{entryId}/receipt
       → lines only here
  GET  /api/v1/pos/linked-customer/statements/{businessCustomerId}/older
       → entitlement-aware settled history
```

Staff endpoints `/api/v1/pos/customers/{customerId}/statement` remain unchanged.

## Explicit exclusions (WP01)

- No application/domain/API/UI code
- No migrations
- No ad network
- No production prices/points
- No dispute implementation
- No Phase 23 closeout
- No Device Verified / Production Ready claim

## Risks / open decisions

1. **Correlation is the blocker.** Without `POSCustomer.PlatformBusinessCustomerId` (or equivalent), authorization cannot bind a linked Personal user to a POS ledger. WP02 must define who writes the correlation (staff action vs create-from-Platform-customer) and uniqueness.
2. **Two customer masters** can diverge (name/phone/status) until correlation + sync policy exist.
3. **Accept lacks an explicit Personal-only guard**; add in WP02 (`AccountClass.Personal`, `HomeOrganizationId == null`).
4. **Revoke/unlink of an accepted link is unimplemented**; statement authz must treat missing/revoked as denied.
5. **Staff statement loads the full ledger** in memory; Personal APIs must not copy that pattern.
6. **Sale-return reductions** mutate credit face amount without a repayment row — projection must use the same outstanding formula, not “credits minus repayments plus copied opening balance.”
7. **Personal entitlement subject** is a commercial-model change; do not fake it with a hidden Organization.
8. **FeatureCode cannot be dotted** (`personal.ad_free` is invalid). Use hyphens.
9. **No email delivery** of customer-link tokens (pre-existing). MAUI accept UX needs a token entry path or a later notification WP.
10. **Opening balances** on Platform are not POS ledger; linked statements must not pretend they are.
11. Phase 14/19/20/21/22/23 remain open; this phase does not close them.

## Recommended WP sequence

WP01 (this) → **WP02 correlation + link completeness** → WP03 authz contract → WP04 summary/activity APIs → WP05 lazy detail + open-debt + cursors → WP06 Personal UX → WP07 Personal entitlements → WP08 reward ledger → WP09 ads abstractions → WP10 older history gates → WP11 Admin config → WP12 tests → WP13 disputes (or defer) → WP14 docs → WP15 device prep only.

**Exact next package: WP02** — Customer-link completeness + POS↔Platform customer correlation. Do not start statement APIs before the identity join and Personal-only accept/revoke rules exist.

## Files / projects most affected (future WPs)

- `ExItS.Platform.Domain|Application|Infrastructure|Api|Admin` — link list-by-user, revoke, Personal product/features/rewards
- `ExItS.PinoyBusinessPOS.Domain|Application|Infrastructure|Api|ApiClient|Maui` — customer correlation, linked-customer projection APIs, Personal statement UI
- Tests under `tests/ExItS.Platform.*` and `tests/ExItS.PinoyBusinessPOS.*`

## Checks performed (WP01)

- `git status` clean; branch `main`; local HEAD = `origin/main` = `2fdcc8ab86f8a1df516053930885b6df04b0e436`
- Prompt cited GitHub `main` `6a9a938e…` (older); local Cursor work already included the later weight-entry commits on `main`
- Read ADR-019/020, P16 customer-separation, FeatureCode, BusinessCustomer/link types, POSCustomer, Utang ledger/statement/repayment paths, Personal endpoints/UI, entitlement/subscription/payment model
- No application code changes
- No migrations
- No physical device run

## Tests executed

None (documentation-only WP). Prior unrelated suite results are not claimed for P24.

## Commit hash

`1351bef72f9ba04030495785767cc9bb609c5f8d` — `docs(p24): define linked customer statements and personal monetization`
