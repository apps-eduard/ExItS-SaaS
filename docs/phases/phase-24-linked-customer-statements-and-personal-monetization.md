# Phase 24 — Linked Customer Statements and Personal Monetization

[Phases](README.md) | [Portfolio](../portfolio-progress.md) | [WP01 audit](../reports/P24-WP01-current-state-and-architecture-contract.md) | [WP02](../reports/P24-WP02-customer-link-and-pos-correlation.md) | [WP03](../reports/P24-WP03-linked-customer-authorization-contract.md) | [WP04](../reports/P24-WP04-lightweight-linked-business-utang-statement.md) | [WP05](../reports/P24-WP05-receipt-summary-detail-and-lazy-loading.md) | [WP06](../reports/P24-WP06-free-vs-paid-personal-history-entitlement.md) | [ADR-021](../decisions/ADR-021-linked-customer-statements-and-personal-monetization.md)

| Field | Value |
|---|---|
| Status | **Open** — WP01–WP11 Complete · WP12+ not started |
| Branch / HEAD at open | `main` @ `2fdcc8ab86f8a1df516053930885b6df04b0e436` |
| Device Verified | **No** |
| Production Ready | **No** |
| Phase 23 coexistence | Phase 23 remains **Open** (WP12 in progress; WP13 closeout **not started**). This phase does **not** close or mix with P23-WP13. |

## Problem statement

A Personal user who is linked to an organization `BusinessCustomer` cannot see that merchant’s Business Utang. POS staff can generate statements; the linked person cannot. Copying POS credit into Personal Utang would violate [ADR-019](../decisions/ADR-019-personal-utang-versus-business-credit-ownership.md). Granting staff/product roles would violate customer/staff separation (P16-WP07).

Separately, Personal is free-only today. Organization commercial plans, snapshots, and payments all require `OrganizationId`. There is no Personal ad-free, digital-records, or reward-points capability — and Organizations must never use reward points.

## Goals

1. Linked Personal users can securely view a **read projection** of that merchant’s POS Business Utang.
2. POS Business Utang remains the **only** authoritative business-credit ledger. Existing partial-payment logic stays unchanged.
3. Do **not** copy Business Utang into Personal Utang.
4. Keep Free Personal lightweight: summaries, small recent pages, lazy receipt detail, explicit older-history requests.
5. Never paywall information needed to understand a **currently outstanding** debt.
6. Prepare Personal-only paid/ad/reward entitlements with Admin-configurable feature codes (no production prices in UI).
7. Organizations remain cash/payment only — **no** reward-point economy.

## Non-goals

- Phase 23 WP13 closeout or Device Verified claims.
- Real ad-network SDK integration (abstractions only unless a provider already exists — it does not).
- Fake ad playback.
- Production prices or production point values.
- Paying merchant Utang or Organization subscriptions with points.
- Direct Personal mutation of POS ledger rows.
- Full dispute workflow in WP01–WP06 (architecture only; dedicated later WP).
- Cold-archive infrastructure (API-shaped for later; no premature archive tables).
- Infinite full-history loading.
- Cross-database FKs or Platform querying the POS database.
- Converting linked customers into staff.

## Frozen architecture

Authoritative detail: [ADR-021](../decisions/ADR-021-linked-customer-statements-and-personal-monetization.md) and [P24-WP01](../reports/P24-WP01-current-state-and-architecture-contract.md).

```text
POS Business Utang
= organization/product owned authoritative ledger

CustomerLink
= connects BusinessCustomer to Personal identity

Personal Statement
= authorized read projection of POS Business Utang
```

Never:

```text
POS Utang -> copied/synchronized Personal Utang balance
```

### Personal statement payload rules

| Surface | Contents |
|---|---|
| Statement summary | Merchant/org display identity + current outstanding |
| Recent activity | Lightweight page; default 10–20 rows; server max **20** |
| Receipt summary | Transaction date; receipt/reference; total; payment/utang effect; resulting balance where appropriate |
| Receipt detail | Fetched only on explicit open; item lines lazy-loaded |
| Older settled history | Separate explicit request; entitlement-aware |

Prefer cursor/keyset pagination `(RecordedAtUtc DESC, EntryId DESC)` for Personal activity. Staff offset pagination on existing POS APIs is unchanged.

### Free versus paid

**FREE Personal**

- Current outstanding merchant balance
- Enough history to understand any **current open** debt (open-debt exception)
- Recent lightweight activity and payment summaries
- Receipt summary; detailed receipt only when opened
- Ads may be shown later (never required for critical debt/security info)

**PAID / UNLOCKABLE Personal** (feature-gated)

- Older **settled** transaction history
- Older detailed receipts outside the free window
- Extended date-range search
- Historical / monthly / annual statements
- Export/PDF later

### Linked-customer authorization

Required: active Personal identity + active `LinkedCustomerAppUser` for that exact user, organization, and `BusinessCustomer` + correlated `POSCustomer` in that organization. Fail closed on guessing. No staff role, no product role, no org membership.

### Monetization

```text
Free Personal  → ads allowed later
Cash payment   → remove ads and/or unlock Personal premium features
Reward points  → Personal only; eligible Personal features only
```

Organizations: cash/payment only. Reward points cannot cash out, transfer, pay Utang, pay org subscriptions/add-ons, or convert to pesos.

Conceptual feature codes (hyphenated; `FeatureCode` forbids dots):

```text
personal-ad-free
personal-digital-records-extended
personal-statements-export
personal-history-extended
```

Unlock sources (Personal): `CashPurchase`, `RewardPoints`, `Promotion`, `AdminGrant`. Organization features **reject** `RewardPoints`.

### Suggested domain concepts (names may shift after WP02)

Reuse existing types where they already fit (`BusinessCustomer`, `CustomerLinkRequest`, `LinkedCustomerAppUser`, `CreditEntry`, `Repayment`, `FeatureDefinition`). New concepts only when needed:

```text
POSCustomer.PlatformBusinessCustomerId     (correlation value, not FK)
LinkedBusinessCreditStatement
LinkedBusinessCreditActivityDto
LinkedBusinessCreditReceiptSummaryDto
LinkedBusinessCreditReceiptDetailDto

PersonalFeatureDefinition / PersonalFeatureEntitlement / PersonalFeatureUnlock
RewardTransaction (immutable ledger) + derived or reconciled RewardBalance
RewardRedemption / AdRewardClaim
IPersonalAdEligibility / IRewardedAdClaimVerifier (null provider first)
```

### Reconciliation / disputes

Personal must not modify financial ledger entries. A later WP may add “I don’t recognize this / amount incorrect / payment missing / item incorrect / other”. Disputes do not change balances; merchants resolve through existing payment/adjustment/correction paths.

## Work packages

| WP | Title | Notes |
|---|---|---|
| **WP01** | Current-state audit + architecture/commercial contract | **Complete** (architecture contract) — [P24-WP01](../reports/P24-WP01-current-state-and-architecture-contract.md) |
| **WP02** | Customer-link completeness + POS↔Platform customer correlation | **Complete** — [P24-WP02](../reports/P24-WP02-customer-link-and-pos-correlation.md) |
| **WP03** | Linked-customer authorization contract | **Complete** — [P24-WP03](../reports/P24-WP03-linked-customer-authorization-contract.md) |
| **WP04** | Business-credit statement projection APIs | **Complete** — [P24-WP04](../reports/P24-WP04-lightweight-linked-business-utang-statement.md) |
| **WP05** | Lazy receipt detail + summary/lazy loading | **Complete** — [P24-WP05](../reports/P24-WP05-receipt-summary-detail-and-lazy-loading.md) |
| **WP06** | Free vs Paid Personal history entitlement | **Complete** — [P24-WP06](../reports/P24-WP06-free-vs-paid-personal-history-entitlement.md) |
| **WP07** | Personal reward points ledger + feature redemption | **Complete** — [P24-WP07](../reports/P24-WP07-personal-reward-points-and-redemption.md) |
| **WP08** | Reward ledger foundation | **Complete** — [P24-WP08](../reports/P24-WP08-reward-ledger-foundation.md) |
| **WP09** | Ads abstraction + Ad-Free entitlement | **Complete** — [P24-WP09](../reports/P24-WP09-ads-abstraction-and-ad-free-entitlement.md) |
| **WP10** | Entitlement-aware older/settled history | **Complete** — [P24-WP10](../reports/P24-WP10-entitlement-aware-older-settled-history.md) |
| **WP11** | Admin configuration for Personal features | **Complete** — [P24-WP11](../reports/P24-WP11-admin-configuration-for-personal-features.md) |
| **WP12** | Regression, security, and edge-case tests | Authz matrix, ledger arithmetic, privacy DTOs |
| **WP13** | Dispute/request architecture (optional) | Skip implementation if it expands; document if deferred |
| **WP14** | Documentation/closeout | No Device Verified claim from tests alone |
| **WP15** | Physical Android validation prep only | Not run unless asked |

## Migration strategy

- Prefer additive columns/tables. No destructive reset.
- POS: optional `PlatformBusinessCustomerId` on `pos.customers` (value, unique per org when set).
- Platform: Personal entitlement/reward/ads foundation — WP06–WP11 landed; WP12 regression/security next.
- No production auto-`Migrate()`.
- Do not physically archive/delete financial records in this phase.

## Authorization / security

- Server-side authorization is mandatory. UI hide is insufficient.
- Personal session APIs remain Personal-scoped (ADR-016/017).
- Staff POS statement/ledger endpoints remain staff + `customer-credit-view`.
- Linked-customer POS routes are a **new principal**, not a reuse of cashier/manager capabilities.
- Privacy-safe DTOs: no internal merchant notes (unless explicitly customer-visible), no cost/margin, no staff audit metadata, no unrelated customers, prefer public-safe IDs.

## Explicit deferred work

- Real ad network / rewarded-ad vendor.
- Export/PDF generation.
- Production price and point catalogs.
- Cold archive storage.
- Push notification vendor (Personal push sink is still null).
- Customer-link email delivery (already a P16-WP07 exclusion).
- Phase 23 closeout.

## Validation (target)

See [P24-WP01](../reports/P24-WP01-current-state-and-architecture-contract.md). Later WPs must cover: linked success; unrelated/unlinked/revoked/cross-org denied; staff cannot use customer statement; owner cannot impersonate; open debt visible outside free window; settled history entitlement; page limits; no lines in summary; separate detail endpoint; reward arithmetic/idempotency/concurrency; points cannot pay org or Utang.
