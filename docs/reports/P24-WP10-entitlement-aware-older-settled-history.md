# P24-WP10 — Entitlement-Aware Older/Settled History

[Phase 24](../phases/phase-24-linked-customer-statements-and-personal-monetization.md) | [WP09](P24-WP09-ads-abstraction-and-ad-free-entitlement.md) | [WP06](P24-WP06-free-vs-paid-personal-history-entitlement.md) | [ADR-021](../decisions/ADR-021-linked-customer-statements-and-personal-monetization.md) | [Portfolio](../portfolio-progress.md)

| Field | Value |
|---|---|
| Status | **Complete** (entitlement-aware older/settled Personal history) |
| Date | 2026-08-12 |
| Starting SHA | `9e3c7646bf93c8c327be58ca21eccef1ab3737b4` on `main` |
| Implementation commit | `40d6da229e2473c347664a79c91463770f1547ee` |
| Docs commit | *(stamped after docs commit)* |
| Docs/hash-stamp commit | *(stamped after stamp commit)* |
| Device Verified | **No** |
| Production Ready | **No** |
| Migration | **None** |

## Status legend

WP10 completes the Phase-24 package for locking older settled digital records behind `personal-digital-records-extended` while preserving open-debt visibility. Builds on WP06 free-window / entitlement foundations; does not invent a second history-gating system. **Not Device Verified. Not Production Ready.**

## Canonical WP10 scope (Phase-24)

```text
WP10 | Entitlement-aware older/settled history | Digital records lock; open debt remains visible
```

Payload rule preserved:

```text
Older settled history | Separate explicit request; entitlement-aware
```

## Free-history rule

| Setting | Value |
|---|---|
| Config | `PersonalStatements:FreeRecentMonths = 3` |
| Meaning | Current UTC calendar month + previous 2 UTC calendar months |
| Helper | `PersonalHistoryWindows.ComputeFreeWindowStart` |
| Not | Rolling 90 days / local time / 3×30 days |

Example: as of 2026-08-13 UTC → free window starts **2026-06-01T00:00:00Z**.

## Entitlement rule

| Field | Value |
|---|---|
| Feature code | `personal-digital-records-extended` |
| Subject | `PersonalUserId` only |
| Active | Existing Platform entitlement window; POS checks via `IPersonalFeatureEntitlementClient` (fail-closed) |
| Expired/inactive | Free-window + open-debt restrictions return |

No new history entitlement. Org context cannot bypass PersonalUserId restrictions.

## Shared history policy

`PersonalSettledHistoryPolicy` (Application) centralizes detail decisions **after** ownership:

1. Free window → allow  
2. Specific open-debt receipt exception → allow  
3. Active extended entitlement → allow  
4. Else → `pos.personal.extended_history_required`

Receipt evaluation order remains:

1. WP03 / ownership / privacy (404)  
2. Free-window check  
3. Specific Active linked-credit open-debt exception while outstanding &gt; 0  
4. Entitlement check  
5. Premium denial **403**

## Open-debt exception

| Rule | Behavior |
|---|---|
| Endpoint | `GET .../open-debt-activity` |
| Predicate | Outstanding &gt; 0 → SQL `status = 'Active'` credits ∪ repayments, paged |
| Outstanding = 0 | Empty page — does **not** unlock settled-old history |
| Scope | Active obligation evidence only; reversed/unrelated settled rows stay locked |
| Receipt exception | Utang sale + linked credit Active + outstanding &gt; 0; settlement to zero removes exception |

Open debt does **not** unlock all historical activity for a user who has any open debt.

## Settled-history / older activity

| Surface | Policy |
|---|---|
| `.../activity` | Unchanged WP06: free users SQL `notBefore = freeStart`; entitled users no date floor; max page 20 |
| `.../older-activity` | **New explicit request.** Requires active extended entitlement else **403**. SQL `recorded_at_utc < freeStart`; max page 20 |
| `.../receipts/{saleId}` | Policy helper; premium **403**; non-owned **404** |
| Exports / statements PDF | Not in WP10 |

## API impact

| Method | Route | Notes |
|---|---|---|
| `GET` | `/api/v1/pos/personal/linked-customers/{id}/older-activity` | Entitlement-required older/settled ledger page |
| Unchanged | `.../statement`, `.../activity`, `.../open-debt-activity`, `.../receipts/{saleId}` | Entitlement-aware as above |

No client-supplied entitlement state, cutoff dates, or PersonalUserId.

## Query efficiency

- Personal/org/customer filters in SQL  
- Free-window `>= @not_before` and older ` < @before` in SQL  
- Active open-debt filter in SQL  
- OFFSET/LIMIT; deterministic `ORDER BY recorded_at_utc DESC, id DESC`  
- No full-ledger materialization  
- Migration/indexes: **None** (existing schema sufficient)

## Tests / builds

| Suite | Result |
|---|---|
| POS `PersonalSettledHistoryPolicy` | **Passed 10**, failed 0, skipped 0 |
| POS `FullyQualifiedName~LinkedCustomer` | **Passed 61**, failed 0, skipped 0 |
| Platform PersonalReward* + Ads + Entitlement filter | **Passed 46**, failed 0, skipped 0 |
| `ExItS.Platform.UnitTests` Release | **Passed 811**, failed 0, skipped 0 |
| `ExItS.PinoyBusinessPOS.UnitTests` Release | **Passed 566**, failed 0, skipped 0 |

## Known limitations

- Open-debt explanation remains lump-sum Active credit/repayment set (not FIFO allocation)  
- Entitled `/activity` still returns full chronological pages (WP06); `/older-activity` is the explicit pre-window settled request  
- No Admin catalog UX (WP11)  
- No export/PDF  
- Platform entitlement unreachable ⇒ fail-closed (not entitled)  
- Not Device Verified. Not Production Ready.

## Exact WP11 recommendation

**P24-WP11 — Admin configuration for Personal features**

- Ant Design; costs/durations not hard-coded in UI

Do **not** start WP11 from this package.

## Checks performed

- Starting HEAD = `origin/main` = `9e3c7646bf93c8c327be58ca21eccef1ab3737b4`
- No second history-gating system; reuses `personal-digital-records-extended`
- Open debt does not unlock unrelated settled history
- Migration: **None**
- Focused commits only
