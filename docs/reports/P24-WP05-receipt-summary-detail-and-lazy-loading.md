# P24-WP05 — Receipt Summary/Detail and Lazy Loading

[Phase 24](../phases/phase-24-linked-customer-statements-and-personal-monetization.md) | [WP01](P24-WP01-current-state-and-architecture-contract.md) | [WP02](P24-WP02-customer-link-and-pos-correlation.md) | [WP03](P24-WP03-linked-customer-authorization-contract.md) | [WP04](P24-WP04-lightweight-linked-business-utang-statement.md) | [ADR-021](../decisions/ADR-021-linked-customer-statements-and-personal-monetization.md) | [Portfolio](../portfolio-progress.md)

| Field | Value |
|---|---|
| Status | **Complete** (lazy linked-customer sale receipt detail; no migration) |
| Date | 2026-08-12 |
| Starting SHA | `86c17b7243400f8c2a0b83c2e6730bb658704415` on `main` |
| Implementation commit | `b819914aa8403af02db3015a3eb47f681e25ec01` |
| Docs/hash-stamp commit | *(filled after docs/stamp commits)* |
| Device Verified | **No** |
| Production Ready | **No** |
| Migration | **No** |

## Status legend

WP05 adds authorized **one-sale** receipt detail for linked Personal customers. WP04 activity stays line-free; lines load only after an explicit open. No separate receipt-summary endpoint (activity already carries collapsed summary fields). **Not Device Verified. Not Production Ready.**

## Endpoints

No redundant summary route. Activity (WP04) remains the collapsed list.

```text
GET /api/v1/pos/personal/linked-customers/{platformBusinessCustomerId}/receipts/{saleId}?organizationId={guid}&currency=PHP
```

- Requires query `organizationId`
- Every request runs `AuthorizeLinkedCustomerStatementAccess` (WP03)
- Then loads `Sale` by org + saleId and requires `sale.CustomerId == authorized PosCustomerId`
- Fail closed (`pos.receipt.not_found` **404**) when missing, guessed, or owned by another POS customer
- Authz failures remain WP03 codes (`denied` **403** / `not_found` **404**)

## Activity DTO (minimal WP05 addition)

`LinkedCustomerActivityItemDto` gained:

```text
SourceSaleId   (Guid?; present when credit row has SourceSaleId)
HasDetails     (true only when SourceSaleId is present — not for repayments)
```

Still **no** nested lines, products, tax breakdowns, cashiers, or branches on activity rows.

Collapsed UI shape (client):

```text
Aug 12
Purchase #000184
P1,250
[View details]  → GET .../receipts/{SourceSaleId}
```

## Receipt DTO fields

```text
OrganizationId
PlatformBusinessCustomerId
PosCustomerId
SaleId
ReceiptNumber          (= SaleNumber)
OccurredAtUtc
Status                 (Completed | Voided | …)
PaymentMethod          (Cash | Utang | …)
Currency
MerchantDisplayName    (null — use Platform linked-merchants)
BranchDisplayName      (null — no cheap register lookup in WP05)
Subtotal
DiscountAmount         (null — Sale has no discount field)
TaxAmount
Total
UtangAmount            (Total when Utang; else null)
PaidAmount             (0 when Utang; else Total)
OutstandingEffect      (Total when Completed Utang; else 0)
Lines[]
  LineNumber
  ProductNameSnapshot
  Quantity
  UnitOfMeasure
  SellingMode
  UnitPriceSnapshot
  LineTotal
```

Not exposed: cost, margin, remarks, RecordedBy, VoidReason, RegisterId/CashierShiftId, SKU/barcode, ProductId, device ids, inventory notes.

## Historical snapshot sources

All line display fields come from `SaleLine` checkout snapshots (`NameSnapshot`, `UnitOfMeasureSnapshot`, `SellingModeSnapshot`, `UnitPrice`, `Quantity`, `LineTotal`). Live catalog is never consulted. Weighted example remains exact: `0.350 kg × P120/kg = P42`.

## Lazy-loading contract

| Surface | Behavior |
|---|---|
| Statement summary | Balance only (WP04) |
| Activity page | ≤20 tiny rows; optional `SourceSaleId` + `HasDetails` |
| Receipt detail | One `saleId` per request |
| Prefetch / batch | **Not** implemented; no batch route |

Personal MAUI is out of scope for WP05; later UI must fetch detail only after explicit tap/expand.

## Security proof

Authorization chain:

```text
Personal session → WP03 linked-customer authorization
→ authorized PosCustomerId
→ sale belongs to that PosCustomerId + Organization
→ privacy-safe receipt DTO
```

Covered by unit tests: own receipt success; wrong POS customer / guessed id → `ReceiptNotFound`; wrong org / wrong platform customer / Platform unreachable → `LinkedCustomerNotFound`; Platform denied → `LinkedCustomerDenied`; DTO property set excludes internal fields.

Staff / Platform Admin paths continue to use existing staff sale/statement APIs (not this Personal route). WP03 still rejects non-Personal sessions at Platform authorization.

## Void / refund behavior

Voided sales remain visible with `Status = Voided`. Historical lines and totals are preserved. `OutstandingEffect = 0` so the receipt is not presented as an active charge. No new returns/refund reconciliation model. Partial payments remain activity feed rows (not nested into sale receipts).

## Bandwidth behavior

```text
Merchant statement → summary only
Activity → default 10 / max 20 tiny rows
Receipt detail → only the selected sale
No receipt prefetch / batch download / nested lines in activity
```

## Tests / builds

| Suite | Result |
|---|---|
| POS unit `FullyQualifiedName~LinkedCustomer` | **Passed 47**, failed 0, skipped 0 |
| POS unit Sales\|Statement\|Receipt\|LinkedCustomer filter | **Passed 128**, failed 0, skipped 0 |
| `ExItS.PinoyBusinessPOS.UnitTests` Release | **Passed 542**, failed 0, skipped 0 |
| `ExItS.Platform.UnitTests` Release | **Passed 765**, failed 0, skipped 0 |
| POS API Release build | Succeeded (pre-existing NU1510 / NU1903 warnings) |
| Platform API Release build | Succeeded (pre-existing CS0618 warnings) |

Covered: PerItem + ByWeight snapshots; decimal quantity; void status; one-receipt payload; activity has no `Lines` and exposes `SourceSaleId`; repayment `HasDetails=false`; ownership / org / Platform deny matrix; WP04 statement regression.

Not run: full `ExItS.slnx`; live POS↔Platform HTTP integration; device/UI validation.

## Migration

**No.** Existing sale-line snapshots are sufficient for historically correct receipt display.

## Known limitations

- `MerchantDisplayName` / `BranchDisplayName` null on receipt (Platform linked-merchants for merchant identity; no register name lookup).
- `DiscountAmount` always null (domain has no sale discount).
- Cash sales without `CustomerId` are not customer-visible via this endpoint (Utang sales set `CustomerId`).
- `BalanceAfter` remains page-1-only on activity (WP04); receipt detail does not reconstruct historical running balance.
- Offset activity pagination unchanged (not keyset).
- Development-stage POS APIs remain not production-secure.
- Not Device Verified. Not Production Ready.

## Explicit exclusions (not started)

Paid/free older-history gating; Personal premium entitlements; reward points; ads; PDF/export; annual statements; disputes; receipt bulk download; automatic prefetch; keyset conversion; Personal MAUI statement UX.

## Exact WP06 recommendation

**P24-WP06 — Free vs Paid Personal history entitlement**

Introduce:

- free recent-history window
- extended settled-history entitlement
- open-debt exception (never paywall current outstanding understanding)
- Personal-only premium entitlement foundation
- no reward points yet unless intentionally combined later

Do **not** start WP06 automatically from this package.

(Phase table formerly listed MAUI statement UX as WP06; that UX should follow after entitlement rules exist, or land as a subsequent package.)

## Files / docs changed

- `LinkedCustomerReceiptUseCases.cs` (new)
- `LinkedCustomerStatementUseCases.cs` (`SourceSaleId` / `HasDetails`)
- `LinkedCustomerStatementEndpoints.cs` (receipt route)
- POS `Program.cs` DI
- Unit tests (statement + receipt)
- This report + phase/portfolio/report indexes + `FILE-MANIFEST.md`

## Checks performed

- Starting HEAD = `origin/main` = `86c17b7243400f8c2a0b83c2e6730bb658704415`
- No stash, reset, rebase, amend, squash, or force-push
- Focused commits only (no `git add .`)
- Migration: **No**
