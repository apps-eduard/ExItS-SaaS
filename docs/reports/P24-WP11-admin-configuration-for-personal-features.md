# P24-WP11 — Admin Configuration for Personal Features

[Phase 24](../phases/phase-24-linked-customer-statements-and-personal-monetization.md) | [WP10](P24-WP10-entitlement-aware-older-settled-history.md) | [ADR-021](../decisions/ADR-021-linked-customer-statements-and-personal-monetization.md) | [Portfolio](../portfolio-progress.md)

| Field | Value |
|---|---|
| Status | **Complete** (Platform Admin Personal feature commercial configuration) |
| Date | 2026-08-12 |
| Starting SHA | `4ce595376202dbff2b28053ddf422d0000a88010` on `main` |
| Implementation commit | `f9f479dbe784103d74803160132ae28c510eb69f` |
| Docs commit | *(stamped after docs commit)* |
| Docs/hash-stamp commit | *(stamped after stamp commit)* |
| Device Verified | **No** |
| Production Ready | **No** |
| Migration | **Yes** — `20260812171442_AddPersonalFeatureDefinitionDuration` |

## Status legend

WP11 adds Platform Admin Ant Design configuration for Personal feature definitions so reward-point costs and default entitlement durations are server-authoritative and not hard-coded in the UI. **Not Device Verified. Not Production Ready.**

## Canonical WP11 scope (Phase-24)

```text
WP11 | Admin configuration for Personal features | Ant Design; costs/durations not hard-coded in UI
```

## Admin UI

| Item | Value |
|---|---|
| Route | `/admin/personal-features` (+ `/{FeatureCode}` detail) |
| Pattern | Plans-like list + detail Descriptions + inline edit Card (Ant Design) |
| Nav | Commercial submenu → Personal Features (`ManageCatalog` / `ViewPortfolio`) |
| Shown | Feature code, display name, enabled, reward points cost, default duration days, updated-at |
| Editable | Display name, enabled, reward points cost, default duration days |
| Read-only | Feature code (immutable) |

UI loads all commercial values from `GET /api/v1/platform/personal/features`. No Razor/JS hard-coded prices or durations.

## Admin API / application

| Method | Route | Authz |
|---|---|---|
| GET | `/api/v1/platform/personal/features` | `ViewPortfolio` |
| GET | `/api/v1/platform/personal/features/{featureCode}` | `ViewPortfolio` |
| PATCH | `/api/v1/platform/personal/features/{featureCode}` | `ManageCatalog` |

Commands/queries: `ListPersonalFeatureDefinitions`, `GetPersonalFeatureDefinition`, `UpdatePersonalFeatureDefinition`, `EnsureKnownPersonalFeatureDefinitions`.

Audit: `platform.personal.feature_definition.updated` on successful PATCH.

## Personal features administered

| Feature code | Seed reward price (if missing) | Duration default |
|---|---|---|
| `personal-digital-records-extended` | 100 (dev seed only) | Indefinite (`null`) |
| `personal-ad-free` | 150 (dev seed only) | Indefinite (`null`) |

Unknown / non-known codes cannot be updated through this admin surface (404). No create/delete of arbitrary feature codes.

## Server authority

- Admin UI binds form fields to API responses only.
- Redemption continues to load `PersonalFeatureDefinition` and debit `RewardPointsPrice` server-side.
- Client cannot choose debit amount or entitlement end date on redeem.
- `DefaultEntitlementDurationDays` drives future reward-redemption `EndsAtUtc` only.

## Existing entitlements / history

- Changing price/duration does **not** rewrite historical reward transactions or already-issued entitlement windows.
- WP10 free-history / open-debt / receipt privacy policy unchanged.
- Ad-Free eligibility remains entitlement-driven.

## Security

- Platform Admin permissions only (`ViewPortfolio` / `ManageCatalog`).
- Personal session APIs remain under `/api/v1/personal/...` and cannot update catalog economics.
- Organization / POS contexts are not granted these Platform permissions by org ownership alone.
- Feature-code updates are isolated to known Personal codes (not org catalog features).

## Migration

`20260812171442_AddPersonalFeatureDefinitionDuration`

Adds nullable `default_entitlement_duration_days` + check constraint (null or 1–3650). Required so duration is admin-configurable without hard-coding in UI/redeem.

## Tests / builds

| Suite | Result |
|---|---|
| `PersonalFeatureDefinitionAdmin` | **Passed 10**, failed 0, skipped 0 |
| PersonalFeatureEntitlement + PersonalReward* + PersonalAds (+ Admin WP11) filter | **Passed 56**, failed 0, skipped 0 |
| POS LinkedCustomer + PersonalSettledHistoryPolicy | **Passed 71**, failed 0, skipped 0 |
| `ExItS.Platform.UnitTests` Release | **Passed 821**, failed 0, skipped 0 |
| `ExItS.PinoyBusinessPOS.UnitTests` Release | **Passed 566**, failed 0, skipped 0 |
| Admin Localization + P20Wp03 nav (targeted) | **Passed 8**, failed 0 |
| Full `ExItS.Platform.Admin.UnitTests` | **129 passed / 6 failed** — 5 failures are pre-existing unrelated dashboard/payments/reporting source guards; localization fixed for WP11 keys |

## Known limitations

- No Personal cash checkout / production price catalog
- No create/delete of new Personal feature products beyond known codes
- Duration applies to reward redemption path; AdminGrant still accepts explicit windows
- No ad provider / fake playback
- Not Device Verified. Not Production Ready.

## Exact WP12 recommendation

**P24-WP12 — Regression, security, and edge-case tests**

- Authz matrix, ledger arithmetic, privacy DTOs

Do **not** start WP12 from this package.

## Checks performed

- Starting HEAD = `origin/main` = `4ce595376202dbff2b28053ddf422d0000a88010`
- No hard-coded Personal feature prices/durations in Admin UI
- Organizations remain outside Personal reward-point economy
- Focused commits only
