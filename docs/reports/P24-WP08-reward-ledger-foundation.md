# P24-WP08 — Reward Ledger Foundation (Org Rejection + Idempotent Claims)

[Phase 24](../phases/phase-24-linked-customer-statements-and-personal-monetization.md) | [WP07](P24-WP07-personal-reward-points-and-redemption.md) | [ADR-021](../decisions/ADR-021-linked-customer-statements-and-personal-monetization.md) | [Portfolio](../portfolio-progress.md)

| Field | Value |
|---|---|
| Status | **Complete** (org redemption rejection + idempotent AdRewardClaim) |
| Date | 2026-08-12 |
| Starting SHA | `766f6166e5075d9a5296c00af573ccf0bc5b6762` on `main` |
| Implementation commit | `f18a9ea76b34e4f265dcc29af7b0220cc3c8a625` |
| Docs commit | `0d0d6595d1e57b756bf30599bd801cd19544419d` |
| Docs/hash-stamp commit | `f423d340df9d15579917e268c19fbec86538ffe0` |
| Device Verified | **No** |
| Production Ready | **No** |
| Migration | **Yes** — `20260812162143_AddPersonalRewardClaims` (Platform only) |

## Canonical WP08 scope (Phase-24)

```text
WP08 | Reward ledger foundation | Personal-only; org redemption rejected; idempotent claims
```

Suggested concepts from Phase-24: `RewardRedemption / AdRewardClaim`, `IPersonalAdEligibility / IRewardedAdClaimVerifier (null provider first)`.

WP07 already delivered the append ledger + feature redemption. WP08 completes the remaining foundation: **organization rejection** and **idempotent AdReward earning claims**. Real ad-network / Ad-Free product UX remains **WP09**.

## Organization redemption rejection

| Mechanism | Behavior |
|---|---|
| `OrganizationRewardRedemptionGuard.EnsurePersonalOnly(organizationId)` | Any non-empty `OrganizationId` → `application.personal.reward_points.organization_redemption_unsupported` |
| `EnsurePersonalIdentity(user)` | Org-scoped staff / `HomeOrganizationId` / staff number → same error |
| `RejectOrganizationFeatureRewardPoints(unlockSource)` | Org plan/add-on unlock must never accept `RewardPoints` (ADR-021) |
| Applied to | `RedeemPersonalFeatureWithRewardPoints`, `ClaimPersonalAdReward` |

No organization reward balances. No silent `OrganizationId` → `PersonalUserId` mapping. Personal redeem continues for Personal identities with `organizationId: null`.

## Idempotent AdReward claims

| Concept | Detail |
|---|---|
| Entity | `PersonalRewardClaim` → `platform.personal_reward_claims` |
| Claim type | `AdReward` |
| Idempotency key | Unique `(personal_user_id, claim_type, claim_key)` |
| Ledger link | Unique `reward_transaction_id`; transaction `IdempotencyKey = AdReward:{claimKey}` |
| Points source | Server `PersonalRewardClaims:AdRewardPoints` (dev default **10**) — never client-controlled |
| Verifier | `IRewardedAdClaimVerifier` / `NullRewardedAdClaimVerifier` (no real ad network) |
| Eligibility | `IPersonalAdEligibility` / `DefaultPersonalAdEligibility` — ineligible when `personal-ad-free` active |

### Transaction behavior

First valid claim (single `SaveChanges`):

1. Eligibility + verifier
2. Credit `PersonalRewardBalance` / append `PersonalRewardTransaction` (`Source=AdReward`)
3. Insert `PersonalRewardClaim`

Duplicate / concurrent race: unique constraint or pre-check → `AlreadyClaimed=true`, **no second credit**.

## API

| Method | Route | Notes |
|---|---|---|
| `POST` | `/api/v1/personal/reward-points/ad-claims` | Body `{ claimKey }` only; session PersonalUserId |
| Unchanged | WP07 balance / activity / redeem routes | |

No generic self-award / add-points endpoint. Trusted admin award remains `AwardPersonalRewardPoints`.

## Tests / builds

| Suite | Result |
|---|---|
| Platform `PersonalReward*` | **Passed 28**, failed 0, skipped 0 |
| Platform `PersonalFeatureEntitlement` | **Passed 9**, failed 0, skipped 0 |
| POS `FullyQualifiedName~LinkedCustomer` | **Passed 54**, failed 0, skipped 0 |
| `ExItS.Platform.UnitTests` Release | **Passed 802**, failed 0, skipped 0 |
| `ExItS.PinoyBusinessPOS.UnitTests` Release | **Passed 549**, failed 0, skipped 0 |

## Known limitations

- Null ad verifier is foundation-only; real provider is WP09
- Ad-Free entitlement feature exists as code constant; Admin/product UX is WP09/WP11
- Development default AdReward points (10) — not production economics
- No points expiration, transfers, org earning, or cash purchase
- Not Device Verified. Not Production Ready.

## Exact WP09 recommendation

**P24-WP09 — Ads abstraction + Ad-Free entitlement**

- No real ad network; no fake playback
- Wire provider-neutral ads abstractions onto WP08 claim/eligibility interfaces
- Personal `personal-ad-free` entitlement surface

Do **not** start WP09 from this package.

## Post-completion note

**P24-WP09** completed separately — see [P24-WP09](P24-WP09-ads-abstraction-and-ad-free-entitlement.md).

## Checks performed

- Starting HEAD = `origin/main` = `766f6166e5075d9a5296c00af573ccf0bc5b6762`
- No stash, reset, rebase, amend, squash, or force-push
- Focused commits only
- Migration: **Yes** (Platform `AddPersonalRewardClaims`)
