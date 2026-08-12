# P24-WP09 — Ads Abstraction + Ad-Free Entitlement

[Phase 24](../phases/phase-24-linked-customer-statements-and-personal-monetization.md) | [WP08](P24-WP08-reward-ledger-foundation.md) | [ADR-021](../decisions/ADR-021-linked-customer-statements-and-personal-monetization.md) | [Portfolio](../portfolio-progress.md)

| Field | Value |
|---|---|
| Status | **Complete** (ads eligibility abstraction + Ad-Free Personal entitlement) |
| Date | 2026-08-12 |
| Starting SHA | `9507bc036b65757543e8c4132aff4f514ab055ae` on `main` |
| Implementation commit | `ea9bac0db464f369238d40957789b6b3d4188a4f` |
| Docs commit | `50a517b9591d77fc75fb8dc46044fb96010f496f` |
| Docs/hash-stamp commit | `f4b5095a3ac05be6513982637efb7811ca21bc2c` |
| Device Verified | **No** |
| Production Ready | **No** |
| Migration | **None** (no schema change; definition seeds on grant/redeem) |

## Canonical WP09 scope (Phase-24)

```text
WP09 | Ads abstraction + Ad-Free entitlement | No real ad network; no fake playback
```

## Ad-Free feature

| Field | Value |
|---|---|
| Feature code | `personal-ad-free` |
| Ownership | `PersonalUserId` only |
| Active semantics | Existing `PersonalFeatureEntitlement` window (`StartsAtUtc` / optional `EndsAtUtc`) |
| Grant sources | `CashPurchase`, `RewardPoints`, `Promotion`, `AdminGrant` |
| Reward price | Dev default **150** (`PersonalFeatureCodes.AdFreeDefaultRewardPoints`) — not production economics |
| Redeem | Reuses `RedeemPersonalFeatureWithRewardPoints` |

## Ads abstraction

| Component | Behavior |
|---|---|
| `IPersonalAdEligibility` / `DefaultPersonalAdEligibility` | Active Ad-Free ⇒ ineligible (`application.personal.ads.ad_free_active`); expired Ad-Free restores eligibility; respects `PersonalAds:SurfaceEnabled` |
| `GetPersonalAdEligibility` | Personal session API use case; org context / org staff rejected |
| `IRewardedAdClaimVerifier` / `NullRewardedAdClaimVerifier` | Provider-neutral; **runtime default does not fabricate success** (`NullProviderClaimsEnabled=false`) |
| `PersonalAdsOptions` | `ProviderMode=None`, `SurfaceEnabled=true` |

**No real ad network integrated. No fake/simulated ad playback. No auto-credit from eligibility request.**

## Ad reward claim flow (preserved + hardened)

1. PersonalUserId only; org guard
2. `IPersonalAdEligibility` (Ad-Free blocks)
3. `IRewardedAdClaimVerifier` (null/disabled ⇒ no credit)
4. Server-side points (`PersonalRewardClaims:AdRewardPoints`)
5. Idempotent `PersonalRewardClaim` + ledger credit

Duplicate/concurrent behavior unchanged from WP08.

## API

| Method | Route | Notes |
|---|---|---|
| `GET` | `/api/v1/personal/ads/eligibility` | `{ eligible, adFreeActive, providerConfigured, reasonCode, reasonMessage }` |
| Unchanged | WP07/WP08 reward + ad-claims routes | Claim still requires verified claim; null provider rejects |

## Tests / builds

| Suite | Result |
|---|---|
| Platform PersonalReward* + PersonalAds + Entitlement | **Passed 46**, failed 0, skipped 0 |
| POS LinkedCustomer | **Passed 54**, failed 0, skipped 0 |
| `ExItS.Platform.UnitTests` Release | **Passed 811**, failed 0, skipped 0 |
| `ExItS.PinoyBusinessPOS.UnitTests` Release | **Passed 549**, failed 0, skipped 0 |

## Known limitations

- No real ad SDK / network / provider callbacks
- No UI ads rendering
- Null verifier remains non-verifying by default (test doubles only for verified-path unit tests)
- Ad-Free Admin catalog UX is WP11
- Not Device Verified. Not Production Ready.

## Exact WP10 recommendation

**P24-WP10 — Entitlement-aware older/settled history**

- Digital records lock; open debt remains visible

Do **not** start WP10 from this package.

## Checks performed

- Starting HEAD = `origin/main` = `9507bc036b65757543e8c4132aff4f514ab055ae`
- No real ad network; no fake playback
- Focused commits only
- Migration: **None**
