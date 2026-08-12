# ADR-021 — Linked Customer Statements and Personal Monetization

[Decisions](README.md) | [Phase 24](../phases/phase-24-linked-customer-statements-and-personal-monetization.md) | [ADR-019](ADR-019-personal-utang-versus-business-credit-ownership.md)

| Field | Value |
|---|---|
| Status | **Accepted** (architecture contract; not implemented) |
| Date | 2026-08-12 |
| Related | ADR-003, ADR-011, ADR-012, ADR-016, ADR-017, ADR-019, ADR-020, Phase 16, Phase 24 |

## Context

A Personal user can already accept a Customer Link onto an organization `BusinessCustomer` without becoming staff (P16-WP07). POS Business Utang already exists as the organization-owned credit ledger. Personal Utang is a separate free ledger (ADR-019). There is no customer-facing view of merchant Business Utang, and there is no Personal-only paid entitlement, ads, or reward-points model.

Two forbidden shortcuts would violate existing ownership:

1. Copying POS Business Utang into Personal Utang (continuous sync / second balance).
2. Granting Organization membership or POS product roles so the customer can call staff statement APIs.

Personal monetization (ads, cash unlock, reward points) must not reuse Organization subscriptions as the subject, and Organizations must never participate in a reward-point economy.

## Decision

### 1. Ownership (unchanged from ADR-019)

```text
POS Business Utang     = organization/product owned authoritative ledger
CustomerLink           = connects BusinessCustomer to Personal identity
Personal Statement     = authorized read projection of POS Business Utang
Personal Utang         = separate Personal-owned ledger (unchanged)
```

Never implement `POS Utang → copied/synchronized Personal Utang balance`.

Outstanding balance remains **derived** in POS: active `credit_entries` minus active `repayments`. Partial payments stay customer-level lumps; do not reimplement allocation as a second authority. FIFO remaining unpaid stays a **read model**.

### 2. Read path (no Platform query of the POS database)

- Platform owns identity, `BusinessCustomer`, `CustomerLinkRequest`, `LinkedCustomerAppUser`, Personal feature entitlements, and the Personal reward ledger.
- POS owns `POSCustomer`, `CreditEntry`, `Repayment`, sales/receipts, and the statement projection.
- Cross-boundary references use stable IDs only (ADR-003 / ADR-012). No cross-database FKs.
- **POS hosts** linked-customer statement/activity/receipt-detail APIs (product owns the ledger).
- **Platform hosts** “my linked merchants” (link metadata only; **no balances**), Personal feature entitlements, and reward wallet APIs.
- POS authorizes a Personal caller by verifying an **active** `LinkedCustomerAppUser` for that user + organization + business customer (Platform HTTP / token contract), then resolving the correlated `POSCustomer`.
- Platform never opens the POS database. POS never treats a linked customer as staff.

### 3. Correlation (required; missing today)

`BusinessCustomerId` (Platform) and `POSCustomerId` (POS) are distinct. WP02 must add an explicit POS-side correlation: optional `PlatformBusinessCustomerId` **value** (GUID, not a FK) on `POSCustomer`, unique per organization when set. Do not join by email/phone.

### 4. Authorization

A Personal user may view business-credit information only when all of the following hold:

- Authenticated as the correct **active Personal** identity (`AccountClass.Personal`, `HomeOrganizationId` null).
- An **active** `LinkedCustomerAppUser` binds that `PlatformUserId` to the requested `BusinessCustomer` and organization.
- The `BusinessCustomer` is not archived; the link is not revoked.
- The POS credit account is the correlated `POSCustomer` for that organization.

Denied (fail closed): unrelated Personal user; different organization; unlinked / revoked / expired link; staff identity; Organization Owner impersonating the Personal statement; identifier guessing.

Linking still grants **no** Organization membership, staff role, or product-local role (`CustomerStaffSeparationGuard`).

### 5. Bandwidth and free vs paid records

Default Personal statement responses must not include full history or receipt lines.

| Surface | Rule |
|---|---|
| Statement summary | Merchant display identity + current outstanding only |
| Recent activity | Small page (default 10–20; server max ≤ 20) |
| Receipt summary | Date, reference, total, payment/utang effect, running/resulting balance where appropriate |
| Receipt detail | Separate request after explicit open; item lines lazy-loaded |
| Older settled history | Explicit request + Personal entitlement |

**Open-debt exception:** never paywall information needed to understand a **currently outstanding** balance. If an older transaction still contributes to open debt, include enough credit/payment provenance even outside the free window.

Prefer **cursor/keyset** pagination for Personal activity (`RecordedAtUtc`, `EntryId`). Do not implement infinite full-history loading. Do not delete financial records to save bandwidth. Cold archive may be added later behind the same APIs.

### 6. Personal monetization (new subject; not an Organization subscription)

Three Personal access paths:

```text
1. Free Personal     → ads allowed later; basic/recent features
2. Cash payment      → remove ads and/or unlock selected Personal premium features
3. Reward points     → Personal-only; redeemable only for eligible Personal features
```

Organizations: cash/payment only. **No** reward points. **No** rewarded-ad economy. Organization subscription/add-on redemption **must reject** `RewardPoints`.

Reward points: Personal only; no cash value; cannot cash out, transfer, pay merchant Utang, pay Organization subscriptions/add-ons, or convert to pesos. Immutable/append-style `RewardTransaction` ledger; idempotent claims; concurrent-redemption safe; auditable.

Feature codes must match existing `FeatureCode` rules (**hyphens, not dots**), for example:

```text
personal-ad-free
personal-digital-records-extended
personal-statements-export
personal-history-extended
```

Do not hard-code production prices or point costs in UI. Platform/Admin configuration owns costs, durations, and active flags. Development/test defaults must be clearly marked.

Do not invent a fake Organization in order to reuse `Subscription.OrganizationId`. Personal entitlements need a **Personal subject** (user/account), not an org snapshot.

### 7. Ads

No real ad network in the first implementation. Provider-neutral abstractions only. Critical debt/security information must never require watching an ad. Do not build fake ad playback.

## Consequences

### Positive

- Linked customers can see merchant Utang without becoming staff or duplicating the ledger.
- ADR-019/020 remain intact.
- Personal paid features can exist without contaminating Organization commercial billing.

### Negative / Follow-on

- Requires POS↔Platform customer correlation (absent today).
- Requires a new Personal entitlement subject (today all snapshots are org-scoped).
- Statement projection must compose ledger rows + optional sale lines via `SourceSaleId` without exposing staff-only fields.

## Rejected alternatives

- Copying POS balances into Personal Utang or Platform tables as a second authority.
- Letting linked customers call existing staff `/api/v1/pos/customers/{id}/statement` APIs.
- Matching customers by email/phone instead of an explicit correlation id.
- Platform DbContext reading POS tables.
- Assigning an Organization Plan to a Personal account.
- Allowing reward points to pay Organization subscriptions, add-ons, or merchant Utang.
- Dotted `personal.*` feature codes (invalid under `FeatureCode`).
