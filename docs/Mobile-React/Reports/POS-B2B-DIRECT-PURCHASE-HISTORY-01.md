# POS-B2B-DIRECT-PURCHASE-HISTORY-01

## STATUS

**DONE**

## START_SHA

`09a486601fdc2d88442b50b19edc0734808199dc`

## FINAL_SHA

3a939ad05f27cc8347fdb7f29a816b53eb1d5180

## DIRECT_SALE_AUTHORITY

SELLER_SALE

## BUYER_HISTORY_PROJECTION

READ_ONLY

## LOCAL_SOURCE

DIRECT_PURCHASE_RECEIPT (`OrganizationId` = authenticated buyer org)

## B2B_SOURCE

SALE where `BuyerPartyKind = Organization` AND `BuyerOrganizationId` = authenticated buyer org

## UNIFIED_PAGE

`/purchasing/direct-purchases`

## BUYER_INVENTORY_MUTATION

NO

## CUSTOMER_ORDER_USED

NO

## DUPLICATE_PURCHASE_RECORD

NO — seller Sale is not copied into a buyer DirectPurchaseReceipt

## AUTHORIZATION_MODEL

POS workspace organization from session/headers; `ViewInventory` capability; B2B detail fails closed (404) unless buyer org matches Sale.BuyerOrganizationId

## FILTERS

Server-backed: source (All/B2B/Local), date range, search (seller name / receipt / sale number / ORG public id / reference), status (All/Completed/Voided)

## PAGINATION_MODEL

Single SQL `UNION ALL` over Local + B2B, global `ORDER BY occurred_at_utc DESC`, `COUNT(*)`, `OFFSET`/`LIMIT` (no separate page-then-concatenate)

## PRIVACY_FIELDS_EXCLUDED

UnitCostSnapshot, LineCostSnapshot, TotalCostSnapshot, GrossProfit, GrossMarginPercent, RecordedBy, VoidedBy, internal notes, seller inventory, other customers

## INDEX_CHANGE

Migration `AddSaleBuyerOrganizationHistoryIndex` → `ix_sales_buyer_organization_party_recorded` on `(buyer_organization_id, buyer_party_kind, recorded_at_utc)` WHERE `buyer_organization_id IS NOT NULL`

## APIs

| Method | Route |
|---|---|
| GET | `/api/v1/pos/purchasing/direct-purchases` |
| GET | `/api/v1/pos/purchasing/direct-purchases/b2b/{saleId}` |

Existing `/api/v1/pos/direct-purchase-receipts` unchanged for Local write/detail.

## UI

- Desktop table + mobile stacked rows
- Source badges: B2B / Local (one badge)
- Action: **Record direct purchase** → existing receive-stock flow
- B2B detail: `/purchasing/direct-purchases/b2b/{saleId}` Transaction Summary + disclaimer

## TESTS

| Suite | Result |
|---|---|
| `DirectPurchaseHistoryQueryServiceTests` | PASS |
| `DirectPurchaseHistoryQueryIntegrationTests` | PASS (PostgreSQL global pagination / filters / auth / void / disconnected relationship / no duplicate receipt) |
| `DirectPurchasesListPage.test.tsx` | PASS |
| React `tsc --noEmit` | PASS |
| Existing B2B Sell Organization buyer domain tests | preserved |

## DOCUMENTATION

- Updated: `docs/engineering/sales-buyer-party-model.md`
- Updated: `docs/Mobile-React/Authoritative/Migration/react-migration-roadmap.md` (RMAP-B04)
- Historical append-only notes: RMAP-B04 report, POS-ORG-B2B-CUSTOMER-RELATIONSHIP-01, POS-ORGANIZATION-REMAINING-GAPS-AUDIT-02
