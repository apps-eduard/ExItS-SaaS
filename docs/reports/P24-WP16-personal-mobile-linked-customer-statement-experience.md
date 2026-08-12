# P24-WP16 — Personal Mobile Linked-Customer Statement Experience

[Phase 24](../phases/phase-24-linked-customer-statements-and-personal-monetization.md) | [WP15](P24-WP15-physical-android-validation-preparation.md) | [ADR-021](../decisions/ADR-021-linked-customer-statements-and-personal-monetization.md) | [Portfolio](../portfolio-progress.md)

| Field | Value |
|---|---|
| Status | **Complete** (Personal mobile linked-merchant read projection) |
| Date | 2026-08-12 |
| Starting SHA | `9214443f562244e5fd3a749d166691f1aee17d9b` on `main` |
| Implementation commit | `de568ae08f17b11c2c14823f4e2b4c3e9f337c78` |
| Docs commit | `de568ae08f17b11c2c14823f4e2b4c3e9f337c78` (included with feat) |
| Docs/hash-stamp commit | _(pending stamp)_ |
| Device Verified | **No** |
| Production Ready | **No** |
| Migration | **None** |

## Status legend

WP16 delivers the Personal MAUI linked-merchant list and statement surfaces as an authorized **read projection** of POS Business Utang. Does not copy balances into Personal Utang. Receipts / older-history entitlement / rewards / ads are WP17–WP19. **Not Device Verified. Not Production Ready.**

## Canonical WP16 scope

```text
WP16 | Personal mobile linked-customer statement experience | Linked merchants, outstanding, recent/open-debt activity; read projection only
```

## Delivered

| Surface | Detail |
|---|---|
| Platform ApiClient | `GetLinkedMerchantsAsync` → `GET /api/v1/personal/linked-merchants` |
| POS ApiClient | New `IPosLinkedCustomerClient` / `PosLinkedCustomerClient` for statement, recent activity, open-debt activity |
| Navigation | Personal More → Linked merchants |
| Pages | `/personal/linked-merchants`, `/personal/linked-merchants/{org}/{businessCustomerId}` |
| Localization | EN + `fil-PH` keys for merchants/statement states |
| UX states | Loading, empty, error, 403, 404, pagination load-more |

## Architecture

- Platform owns link metadata (no balances on merchant list).
- POS owns outstanding + activity projection; `organizationId` always passed as query.
- No ledger mutation; no `ILocalPersonalUtangStore` for this path.
- Staff `IPosCustomerClient` statement APIs remain separate.

## Tests

| Suite | Result |
|---|---|
| `ExItS.PinoyBusinessPOS.Maui.Tests` Release | **Passed 347**, failed 0, skipped 0 |

## Known limitations

- Receipts / older-history / rewards / ads UI not in this WP
- Customer-link accept token UX still More/out-of-band as previously documented
- Device Verified: No

## Exact next WP

**P24-WP17 — Mobile receipts and older-history entitlement UX**

## Checks performed

- Starting HEAD = `origin/main` = `9214443f562244e5fd3a749d166691f1aee17d9b`
- Migration: None
