# P24-WP04 — Lightweight Linked Business Utang Statement Projection

[Phase 24](../phases/phase-24-linked-customer-statements-and-personal-monetization.md) | [WP01](P24-WP01-current-state-and-architecture-contract.md) | [WP02](P24-WP02-customer-link-and-pos-correlation.md) | [WP03](P24-WP03-linked-customer-authorization-contract.md) | [ADR-021](../decisions/ADR-021-linked-customer-statements-and-personal-monetization.md) | [Portfolio](../portfolio-progress.md)

| Field | Value |
|---|---|
| Status | **Complete** (lightweight statement projection; no receipt lines) |
| Date | 2026-08-12 |
| Starting SHA | `e911c59b81e7768cc50a86d537360227304fe9ec` on `main` |
| Implementation commit | `cd24b28ad29a5a4ddc3af9e49021884d7640a520` |
| Docs/hash-stamp commit | `b8cf30964fec6270f5fefd8435488bdf806ca4d1` |
| Device Verified | **No** |
| Production Ready | **No** |
| Migration | **No** |

## Status legend

WP04 exposes the first Personal-facing Business Utang statement data: outstanding balance + small recent activity. It reuses WP03 authorization on every request and does **not** implement receipt item details, older-history entitlements, or MAUI UX. **Not Device Verified. Not Production Ready.**

## Platform authorization adapter

`LinkedCustomerPlatformAuthorizationClient` implements `ILinkedCustomerPlatformAuthorization`.

- Forwards inbound Personal `X-ExItS-Session-Token` / `Authorization: PlatformSession …` to Platform
- Calls `GET /api/v1/personal/linked-merchants/authorization?organizationId=&businessCustomerId=`
- Maps **200** → `Authorized` (+ proof ids must match request)
- Maps **403** → `Denied`
- Maps **401 / 404 / 409 / 4xx / 5xx / timeout / HttpRequestException / malformed JSON / BaseUrl missing** → `NotFound` (fail closed)
- HttpClient timeout **3 seconds**
- Does **not** reuse the support-key catalog client

DI registers the client + `AuthorizeLinkedCustomerStatementAccess` + statement use cases in POS `Program.cs`.

## Statement endpoint

```text
GET /api/v1/pos/personal/linked-customers/{platformBusinessCustomerId}/statement?organizationId={guid}&currency=PHP
```

- Requires query `organizationId` (not trusted alone via `X-Pos-Organization-Id`)
- Every request runs `AuthorizeLinkedCustomerStatementAccess` (WP03)
- Balance from existing `IOutstandingBalanceService.GetOutstandingAsync` (active credits − active repayments)
- Does **not** call staff `CustomerStatementService` (full-history path)

### Summary payload

```text
OrganizationId
PlatformBusinessCustomerId
PosCustomerId
LinkedCustomerAppUserId
MerchantDisplayName   (null in WP04 — client uses Platform linked-merchants list)
CustomerDisplayName   (from POSCustomer)
OutstandingBalance
Currency
AsOfUtc
```

No history lines, remarks, cost/margin, or staff metadata.

## Activity endpoint

```text
GET /api/v1/pos/personal/linked-customers/{platformBusinessCustomerId}/activity?organizationId={guid}&page=1&pageSize=10
```

| Limit | Value |
|---|---|
| Default page size | **10** |
| Maximum page size | **20** (requests above 20 are clamped) |

### Query limiting strategy

New `ILinkedCustomerRecentActivityQuery` / `LinkedCustomerRecentActivityQuery`:

```sql
ORDER BY recorded_at_utc DESC, id DESC
OFFSET @skip
LIMIT @take
```

on the credits ∪ repayments union. Take is hard-capped at `MaxPageSize + 1` (for `HasMore`) in the repository. **Does not** use `IUtangLedgerQuery.ListAsync` / `ListAllChronologicalAsync` (those load full history into memory).

Pagination is **offset** (`page` / `pageSize`) because existing POS credit/repayment repositories and staff ledger use offset, and WP04 only needs a tiny newest window. Keyset/cursor remains preferred for WP05+ when older history expands.

`BalanceAfter` is computed cheaply on **page 1 only** by walking newest→oldest from current outstanding. Later pages omit `BalanceAfter` rather than loading all newer rows.

### Activity payload (summary only)

```text
ActivityId
OccurredAtUtc
Type                  (UtangCharge | UtangChargeReversal | Payment | PartialPayment | PaymentReversal | Adjustment)
ReferenceNumber
ChargeAmount
PaymentAmount
AdjustmentAmount
BalanceAfter          (page 1 only when available)
Status
HasDetails            (hint for WP05 receipt/sale detail)
```

No product/receipt lines. No merchant `Remarks`.

### Partial payment projection

Existing POS repayments appear as activity. When page-1 `BalanceAfter > 0` after an active repayment, type is `PartialPayment`; when zero, `Payment`. Reversed credits/repayments project as reversal types. Outstanding still uses authoritative `SumActiveAmount` arithmetic (reversals have zero signed effect).

## Balance source of truth

```text
POS Business Utang (IOutstandingBalanceService)
= Sum(active CreditEntry amounts) − Sum(active Repayment amounts)
```

No Platform copy. No Personal Utang derivation. No second balance store.

## Security / fail-closed

| Case | Behavior |
|---|---|
| Valid linked Personal + correlated POS customer | 200 summary/activity |
| Platform 403 / staff/Admin/wrong class | `pos.linked_customer.denied` **403** |
| Missing/revoked/wrong org/customer / no correlation / Platform unreachable / malformed | `pos.linked_customer.not_found` **404** |
| Guessed identifiers | generic 404 |
| Org header alone | not used as proof; `organizationId` query required and verified via WP03 |

## Tests / builds

| Suite | Result |
|---|---|
| POS unit `FullyQualifiedName~LinkedCustomer` | **Passed 35**, failed 0, skipped 0 |
| `ExItS.PinoyBusinessPOS.UnitTests` Release | **Passed 530**, failed 0, skipped 0 |
| Platform unit `LinkedCustomerAuthorizationTests\|CustomerLinkCompletenessTests` | **Passed 31**, failed 0, skipped 0 |
| `ExItS.Platform.UnitTests` Release | **Passed 765**, failed 0, skipped 0 |
| POS Statements/Credit/Repayment filter | **Passed 19**, failed 0, skipped 0 |
| POS API Release build | Succeeded (pre-existing NU1510 warnings) |
| Platform API Release build | Succeeded (pre-existing CS0618 warnings) |

Covered: authorized summary/outstanding; zero/full/partial balances; platform denied/unreachable; wrong org/customer; newest-first activity; default 10; pageSize 5; pageSize 50→20 clamp; page-2 no duplicates; unrelated customer exclusion; reversal types; adapter 200/403/401/404/5xx/malformed/HttpException mapping; WP03 authz regression.

Not run: full `ExItS.slnx`; POS HTTP integration against live Platform; device/UI validation.

## Known limitations

- `MerchantDisplayName` is null on summary (use Platform linked-merchants list).
- `BalanceAfter` only on activity page 1.
- Offset pagination (not keyset); fine for ≤20 rows, revisit for older history.
- No POS↔Platform integration test for the end-to-end Personal statement path in WP04.
- Development-stage POS APIs remain not production-secure.
- Not Device Verified. Not Production Ready.

## Explicit exclusions (not started)

Receipt product lines; lazy receipt detail endpoint; older-history entitlement; Digital Records; points; ads; disputes; PDF/export; archive; Personal MAUI statement UX.

## Exact WP05 recommendation

**P24-WP05 — Receipt summary/detail and lazy loading.**

Fetch receipt/product detail only when explicitly opened (`HasDetails`). Keep summary/activity payloads line-free. Prefer keyset pagination if older history expands. Do **not** start WP05 automatically from this package.

## Files / docs changed

Adapter, statement/activity use cases, SQL-limited activity query, Personal POS endpoints, DI, unit tests, this report, phase/portfolio indexes.

## Checks performed

- Starting HEAD = `origin/main` = `e911c59b81e7768cc50a86d537360227304fe9ec`
- No stash, reset, rebase, amend, squash, or force-push
- Focused commits only (no `git add .`)
- Migration: **No**
