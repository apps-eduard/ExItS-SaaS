# P24-WP07 — Personal Reward Points Ledger + Feature Redemption

[Phase 24](../phases/phase-24-linked-customer-statements-and-personal-monetization.md) | [WP06](P24-WP06-free-vs-paid-personal-history-entitlement.md) | [ADR-021](../decisions/ADR-021-linked-customer-statements-and-personal-monetization.md) | [Portfolio](../portfolio-progress.md)

| Field | Value |
|---|---|
| Status | **Complete** (Personal reward ledger + redeem for personal features) |
| Date | 2026-08-12 |
| Starting SHA | `8a7689e42a511571e704356a95e53465e3291921` on `main` |
| Implementation commit | `d41106acd32bd950bb6638bc769539ab22abd99a` |
| Docs commit | _(filled after docs commit)_ |
| Docs/hash-stamp commit | _(filled after stamp)_ |
| Device Verified | **No** |
| Production Ready | **No** |
| Migration | **Yes** — `20260812153929_AddPersonalRewardPoints` (Platform only) |

## Status legend

WP07 adds a **PersonalUserId-scoped** append-oriented reward-points ledger and atomic redemption into `PersonalFeatureEntitlement` with `GrantSource = RewardPoints`. WP06 free-window / open-debt / extended-history authorization is unchanged except that a successful redemption naturally satisfies the existing entitlement check. **Not Device Verified. Not Production Ready.**

## Domain / data model

| Concept | Location |
|---|---|
| `PersonalRewardBalance` | Platform Domain + `platform.personal_reward_balances` |
| `PersonalRewardTransaction` | Platform Domain + `platform.personal_reward_transactions` (append-only) |
| `PersonalFeatureDefinition.RewardPointsPrice` | Nullable int; null = not reward-redeemable |
| Subject | `PersonalUserId` (never `OrganizationId`) |

### Ledger semantics

- Integer points only (no floating point).
- Credits / debits are immutable transactions with source, optional reason/reference/idempotency key, UTC timestamp, and `BalanceAfter`.
- Maintained balance row uses optimistic concurrency (`version` + PostgreSQL `xmin`).
- Balance must never go negative; insufficient debit raises `platform.personal.reward_points.insufficient`.
- Sources include `AdminAward`, `FeatureRedemption`, `Promotion`, `AdReward` (AdReward reserved for later earning paths).

### Reward price decision

Price lives on `PersonalFeatureDefinition.RewardPointsPrice` (not a separate marketplace table). Development default for `personal-digital-records-extended` is **100** points (`PersonalFeatureCodes.DigitalRecordsExtendedDefaultRewardPoints`). Not a production launch price — Admin/config economics remain WP11.

## Redemption transaction semantics

Use case: `RedeemPersonalFeatureWithRewardPoints`

1. Resolve feature; reject unknown / inactive / non-redeemable.
2. If already active entitlement → success `AlreadyActive=true`, **no debit**.
3. Otherwise atomically in one `SaveChanges`:
   - debit ledger (`FeatureRedemption`)
   - grant `PersonalFeatureEntitlement` with `GrantSource=RewardPoints`
4. Client never supplies price or target `PersonalUserId`.

### Concurrency / idempotency

- Balance `version` + `xmin` concurrency tokens; `DbUpdateConcurrencyException` mapped via `PlatformUnitOfWork`.
- Unique partial index `ux_personal_reward_transactions_user_idempotency` for award idempotency keys.
- Concurrent redeem: loser sees conflict → re-check active entitlement → return `AlreadyActive` without a second successful debit when peer already won.
- Award use case supports optional idempotency key (trusted path only).

## API routes

| Method | Route | Notes |
|---|---|---|
| `GET` | `/api/v1/personal/reward-points/balance` | Session `PersonalUserId` only |
| `GET` | `/api/v1/personal/reward-points/activity?page&pageSize` | SQL-filtered + CatalogPagination (default 20 / max 100) |
| `POST` | `/api/v1/personal/features/{featureCode}/redeem` | Session user only; no client price |
| `GET` | `/api/v1/personal/features/{featureCode}/active` | Unchanged (WP06) |

**No** public Personal self-award / self-credit endpoint. Trusted funding: `AwardPersonalRewardPoints` application use case for future Admin/internal callers.

### Error codes (stable)

| Code | Meaning |
|---|---|
| `application.personal.reward_points.insufficient` | Not enough points |
| `application.personal.feature.not_reward_redeemable` | Feature has no positive reward price |
| `application.personal.feature.definition.not_found` | Unknown feature |
| `application.personal.reward_points.concurrency_conflict` | Concurrent balance mutation |

WP06 premium denial `pos.personal.extended_history_required` (403) unchanged for users without entitlement.

## Migration

```text
20260812153929_AddPersonalRewardPoints
```

- `personal_reward_balances`, `personal_reward_transactions`
- `reward_points_price` on `personal_feature_definitions`
- Seeds price `100` for existing `personal-digital-records-extended` rows when null

## Tests / builds

| Suite | Result |
|---|---|
| Platform `PersonalRewardPoints` + `PersonalFeatureEntitlement` | **Passed 24**, failed 0, skipped 0 |
| POS `FullyQualifiedName~LinkedCustomer` | **Passed 54**, failed 0, skipped 0 |
| `ExItS.Platform.UnitTests` Release | **Passed 789**, failed 0, skipped 0 |
| `ExItS.PinoyBusinessPOS.UnitTests` Release | **Passed 549**, failed 0, skipped 0 |

Pre-existing: Platform CS0618 check-constraint warnings; POS NU1510 / NU1903 warnings.

Not run: full `ExItS.slnx`; live Platform↔POS HTTP; device/UI.

## Security

- Balance / activity / redeem bound to authenticated Personal session user only
- No client-controlled `PersonalUserId` or reward price
- No public self-award
- No negative balance; double-spend blocked by concurrency + already-active short-circuit
- Cross-user ledger isolation covered by tests
- WP06 history authorization remains authoritative after redemption

## Known limitations

- No Admin UI for awarding points or editing prices
- No cash purchase / payment gateway
- No ad-reward earning path (source reserved)
- No points expiration / transfer / org usage
- Development default price only — not production economics
- Development-stage APIs not production-secure
- Not Device Verified. Not Production Ready.

## Explicit exclusions

Admin UI, reward-store UI, cash checkout, payment gateway, ads network, point transfers, org points, tiers/badges, Synology/RAG/MCP, production security claims.

## Exact WP08 recommendation

**P24-WP08 — Reward ledger foundation** (phase table)

- Personal-only ledger hardening / remaining foundation pieces after WP07 landed ledger + redeem
- Org redemption rejected
- Idempotent claims (e.g. future `AdRewardClaim` / earning claim paths)
- Do **not** invent a parallel ledger if WP07 already covers balance + transactions

Do **not** start WP08 from this package.

## Checks performed

- Starting HEAD = `origin/main` = `8a7689e42a511571e704356a95e53465e3291921`
- No stash, reset, rebase, amend, squash, or force-push
- Focused commits only (no `git add .`)
- Migration: **Yes** (Platform `AddPersonalRewardPoints`)
