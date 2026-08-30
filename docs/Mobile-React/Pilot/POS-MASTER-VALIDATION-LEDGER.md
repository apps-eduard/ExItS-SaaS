# POS Master Validation Ledger

Authoritative map of what has already been tested for Organization POS (single-branch pilot → multi-branch hardening readiness).

| Field | Value |
|-------|-------|
| **TASK** | POS-PILOT-TO-MULTIBRANCH-MASTER-VALIDATION-01 |
| **START_SHA** | `ad1f9171dbafc1e71c6ca94f2732dfda787b81ce` |
| **PREVIOUS_COMPLETION** | `docs/Mobile-React/Pilot/POS-PILOT-COMPLETION-VALIDATION-01.md` |
| **PREVIOUS_COMPLETION_FINAL_SHA** | `bae70c26eedf4f25fe5044d80d39be358d9ef48a` (stamped; tip after docs chase = START_SHA) |
| **TECHNICAL_PILOT_STATUS** | PASSED_AFTER_REMEDIATION |
| **CURRENT_PATH** | REAL_OPERATOR_SINGLE_STORE_PILOT → MULTI-BRANCH-HARDENING |
| **EVIDENCE_POLICY** | PASS only from CURRENT_RUN or VALID_PRIOR_EVIDENCE. Proven unchanged paths = EVIDENCE_REUSED / RERUN_REQUIRED=NO |

### Reuse / invalidation summary (this package)

| Metric | Value |
|--------|------:|
| **TESTS_REUSED_COUNT** | See matrix; domain packages reused unless invalidated |
| **TESTS_INVALIDATED_COUNT** | 0 at package start (docs-only tip; no production delta from completion FEATURE_SHA `0abc15da`) |
| **TESTS_REQUIRED_NOW** | Real-operator UI acceptance; multi-branch hardening map + targeted Postgres re-proof for MB gaps |

---

## Ledger columns

DOMAIN · FEATURE · TEST_OR_SCENARIO · STATUS · EVIDENCE_TYPE · EVIDENCE_SOURCE · EVIDENCE_SHA · LAST_VALIDATED_SHA · CURRENT_CODE_IMPACT · RERUN_REQUIRED · RERUN_REASON · CURRENT_RESULT

---

## IDENTITY / ORGANIZATION

| DOMAIN | FEATURE | TEST_OR_SCENARIO | STATUS | EVIDENCE_TYPE | EVIDENCE_SOURCE | EVIDENCE_SHA | LAST_VALIDATED_SHA | CURRENT_CODE_IMPACT | RERUN_REQUIRED | RERUN_REASON | CURRENT_RESULT |
|--------|---------|------------------|--------|---------------|-----------------|--------------|--------------------|---------------------|----------------|--------------|----------------|
| IDENTITY | Org creation / Start Business | Controlled pilot org + completion recreate | PASS | CONTROLLED_PILOT | POS-CONTROLLED-SINGLE-BRANCH-PILOT-01; COMPLETION-01 | `54a25b0d`; `0abc15da` | `0abc15da` | UNAFFECTED | NO | — | EVIDENCE_REUSED |
| IDENTITY | Org login/session | Live owner login LocalValidation | PASS | CONTROLLED_PILOT | COMPLETION-01; master live smoke | `0abc15da` | START_SHA | UNAFFECTED | NO | — | EVIDENCE_REUSED |
| IDENTITY | Personal → org staff | Staff invite accept `@ORG######` | PASS | CONTROLLED_PILOT | COMPLETION SC19; BUG_01 fix | `0abc15da` | `0abc15da` | UNAFFECTED | NO | — | EVIDENCE_REUSED |
| IDENTITY | Branch context | Primary branch bind | PASS | CONTROLLED_PILOT | CONTROLLED + COMPLETION | `54a25b0d` | `0abc15da` | UNAFFECTED | NO | — | EVIDENCE_REUSED |

---

## RBAC

| DOMAIN | FEATURE | TEST_OR_SCENARIO | STATUS | EVIDENCE_TYPE | EVIDENCE_SOURCE | EVIDENCE_SHA | LAST_VALIDATED_SHA | CURRENT_CODE_IMPACT | RERUN_REQUIRED | RERUN_REASON | CURRENT_RESULT |
|--------|---------|------------------|--------|---------------|-----------------|--------------|--------------------|---------------------|----------------|--------------|----------------|
| RBAC | Owner | SC19 + Owner pilot | PASS | CONTROLLED_PILOT | COMPLETION-01 | `0abc15da` | `0abc15da` | UNAFFECTED | NO | — | EVIDENCE_REUSED |
| RBAC | Cashier allow/deny | Live API + UI deep-link | PASS | CONTROLLED_PILOT | COMPLETION SC19 | `0abc15da` | `0abc15da` | UNAFFECTED | NO | — | EVIDENCE_REUSED |
| RBAC | InventoryStaff | Live API gates | PASS | CONTROLLED_PILOT | COMPLETION SC19 | `0abc15da` | `0abc15da` | UNAFFECTED | NO | — | EVIDENCE_REUSED |
| RBAC | ReportingUser | Reports read; mutations 403 | PASS | CONTROLLED_PILOT | COMPLETION SC19; BUG_02 | `0abc15da` | `0abc15da` | UNAFFECTED | NO | — | EVIDENCE_REUSED |
| RBAC | StoreManager / Admin | Matrix + prep | PASS | STATIC_AUDIT | AUDIT-03; PREP-01 | `8d9835e1` | `8d9835e1` | UNAFFECTED | NO | — | EVIDENCE_REUSED |
| RBAC | Staff invite productRole | BUG_01 regression | PASS | POSTGRES_INTEGRATION | Platform Staff_invitation_persists_product_role | `0abc15da` | `0abc15da` | UNAFFECTED | NO | — | EVIDENCE_REUSED |
| RBAC | Advanced reports entitlement | BUG_02 Operational_sales_summary… | PASS | POSTGRES_INTEGRATION | PosReportingApiTests | `0abc15da` | `0abc15da` | UNAFFECTED | NO | — | EVIDENCE_REUSED |
| RBAC | UI permission visibility | Inventory permission polish | PASS | REACT_TEST | POS-INVENTORY-PERMISSION-I18N-POLISH-01 | `60fd94c6` | `60fd94c6` | UNAFFECTED | NO | — | EVIDENCE_REUSED |

---

## SELL

| DOMAIN | FEATURE | TEST_OR_SCENARIO | STATUS | EVIDENCE_TYPE | EVIDENCE_SOURCE | EVIDENCE_SHA | LAST_VALIDATED_SHA | CURRENT_CODE_IMPACT | RERUN_REQUIRED | RERUN_REASON | CURRENT_RESULT |
|--------|---------|------------------|--------|---------------|-----------------|--------------|--------------------|---------------------|----------------|--------------|----------------|
| SELL | Cash | SC02 | PASS | CONTROLLED_PILOT | CONTROLLED-SINGLE-BRANCH-PILOT-01 | `54a25b0d` | `54a25b0d` | UNAFFECTED | NO | — | EVIDENCE_REUSED |
| SELL | Manual GCash | SC03 | PASS | CONTROLLED_PILOT | CONTROLLED-01 | `54a25b0d` | `54a25b0d` | UNAFFECTED | NO | — | EVIDENCE_REUSED |
| SELL | Utang | SC04–06 | PASS | CONTROLLED_PILOT | CONTROLLED-01 | `54a25b0d` | `54a25b0d` | UNAFFECTED | NO | — | EVIDENCE_REUSED |
| SELL | Weighted / barcode / discount / override | Pilot + RMAP | PASS | CONTROLLED_PILOT | CONTROLLED-01; RMAP-11/12 | `54a25b0d` | `54a25b0d` | UNAFFECTED | NO | — | EVIDENCE_REUSED |
| SELL | Shift/register gating | SC20 | PASS | CONTROLLED_PILOT | CONTROLLED-01 | `54a25b0d` | `54a25b0d` | UNAFFECTED | NO | — | EVIDENCE_REUSED |
| SELL | Idempotency | SC22 | PASS | CONTROLLED_PILOT | CONTROLLED-01 | `54a25b0d` | `54a25b0d` | UNAFFECTED | NO | — | EVIDENCE_REUSED |
| SELL | Operator UI cash/GCash/Utang | ROP_03–07 | See real-operator report | MANUAL_UI | POS-REAL-OPERATOR-SINGLE-STORE-PILOT-01 | START_SHA | START_SHA | UNAFFECTED | YES | Operator stage | CURRENT_RUN |

---

## CUSTOMER / UTANG

| DOMAIN | FEATURE | TEST_OR_SCENARIO | STATUS | EVIDENCE_TYPE | EVIDENCE_SOURCE | EVIDENCE_SHA | LAST_VALIDATED_SHA | CURRENT_CODE_IMPACT | RERUN_REQUIRED | RERUN_REASON | CURRENT_RESULT |
|--------|---------|------------------|--------|---------------|-----------------|--------------|--------------------|---------------------|----------------|--------------|----------------|
| CUSTOMER | Create/lookup/Utang/repay | SC04–06 | PASS | CONTROLLED_PILOT | CONTROLLED-01 | `54a25b0d` | `54a25b0d` | UNAFFECTED | NO | — | EVIDENCE_REUSED |
| CUSTOMER | AR ≠ AP | Payables ADR-023 + pilot | PASS | STATIC_AUDIT | POS-SUPPLIER-PAYABLES-01 | `c045ea25` | `8366a8ec` | UNAFFECTED | NO | — | EVIDENCE_REUSED |

---

## INVENTORY

| DOMAIN | FEATURE | TEST_OR_SCENARIO | STATUS | EVIDENCE_TYPE | EVIDENCE_SOURCE | EVIDENCE_SHA | LAST_VALIDATED_SHA | CURRENT_CODE_IMPACT | RERUN_REQUIRED | RERUN_REASON | CURRENT_RESULT |
|--------|---------|------------------|--------|---------------|-----------------|--------------|--------------------|---------------------|----------------|--------------|----------------|
| INVENTORY | Opening / account / Model A | Stock Use fix report | PASS | POSTGRES_INTEGRATION | POS-PILOT-STOCK-USE-BRANCH-BALANCE-FIX-01 | `bc625cfb` | `34d6f093` | UNAFFECTED | NO | — | EVIDENCE_REUSED |
| INVENTORY | Stock Use | SC14 FAIL→PASS | PASS | CONTROLLED_PILOT | COMPLETION SC14; Stock Use fix | `0abc15da` | `0abc15da` | UNAFFECTED | NO | — | EVIDENCE_REUSED |
| INVENTORY | Waste/Loss | SC13 + package | PASS | CONTROLLED_PILOT | CONTROLLED-01; WASTE-LOSS-01 | `54a25b0d` | `54a25b0d` | UNAFFECTED | NO | — | EVIDENCE_REUSED |
| INVENTORY | Stock count / transfers | React packages + Postgres transfer | PASS | POSTGRES_INTEGRATION | PosInventoryTransferApiTests; STOCK-COUNT-01 | various | `34d6f093` | UNAFFECTED | NO | — | EVIDENCE_REUSED |
| INVENTORY | Ops Postgres StockUse/Waste/Prod | 12 API facts | PASS | POSTGRES_INTEGRATION | POS-INVENTORY-OPS-POSTGRES-INTEGRATION-TESTS-01 | `722e9634` | `722e9634` | UNAFFECTED | NO | — | EVIDENCE_REUSED |
| INVENTORY | Branch materialization / no forced negative | BranchBalanceMutation + SC14 | PASS | BACKEND_UNIT | BranchBalanceMutationTests; Stock Use API | `bc625cfb` | `34d6f093` | UNAFFECTED | NO | — | EVIDENCE_REUSED |
| INVENTORY | Multi-branch MB_01–04,09–10,12 | Hardening map | See MB report | POSTGRES_INTEGRATION | POS-MULTI-BRANCH-HARDENING-01 | START_SHA | START_SHA | UNAFFECTED | YES | Hardening stage | CURRENT_RUN |

---

## PURCHASING

| DOMAIN | FEATURE | TEST_OR_SCENARIO | STATUS | EVIDENCE_TYPE | EVIDENCE_SOURCE | EVIDENCE_SHA | LAST_VALIDATED_SHA | CURRENT_CODE_IMPACT | RERUN_REQUIRED | RERUN_REASON | CURRENT_RESULT |
|--------|---------|------------------|--------|---------------|-----------------|--------------|--------------------|---------------------|----------------|--------------|----------------|
| PURCHASING | DP / PO / receive | SC07–09 | PASS | CONTROLLED_PILOT | CONTROLLED-01 | `54a25b0d` | `54a25b0d` | UNAFFECTED | NO | — | EVIDENCE_REUSED |
| PURCHASING | Receipt reversal | Full void; block if paid | PASS | POSTGRES_INTEGRATION | POS-PURCHASE-RECEIPT-REVERSAL-01 | `3eb5a041` | `3eb5a041` | UNAFFECTED | NO | — | EVIDENCE_REUSED |
| PURCHASING | Connected create/link | CreateBuyer 13/13 | PASS | BACKEND_UNIT | POS-CONNECTED-BUYER-CREATE-LINK-TEST-REPAIR-01 | `f0156f85` | `6ec10659` | UNAFFECTED | NO | — | EVIDENCE_REUSED |
| PURCHASING | Partial receipt void | DEFERRED | NOT_APPLICABLE | STATIC_AUDIT | REVERSAL-01 | `3eb5a041` | `3eb5a041` | UNAFFECTED | NO | Out of scope | NOT_APPLICABLE |

---

## SUPPLIER PAYABLES

| DOMAIN | FEATURE | TEST_OR_SCENARIO | STATUS | EVIDENCE_TYPE | EVIDENCE_SOURCE | EVIDENCE_SHA | LAST_VALIDATED_SHA | CURRENT_CODE_IMPACT | RERUN_REQUIRED | RERUN_REASON | CURRENT_RESULT |
|--------|---------|------------------|--------|---------------|-----------------|--------------|--------------------|---------------------|----------------|--------------|----------------|
| PAYABLES | Create / pay / overdue / CSV | Postgres 7/7 + SC08/10/22 | PASS | POSTGRES_INTEGRATION | POS-SUPPLIER-PAYABLES-01 | `c045ea25` | `8366a8ec` | UNAFFECTED | NO | No payables code change | EVIDENCE_REUSED |
| PAYABLES | Org-level by design | MB_11 | PASS | STATIC_AUDIT | ADR-023; MB hardening | `8366a8ec` | START_SHA | UNAFFECTED | NO | — | EVIDENCE_REUSED |
| PAYABLES | Payment reversal | DEFERRED | NOT_APPLICABLE | STATIC_AUDIT | PAYABLES-01 | `c045ea25` | `8366a8ec` | UNAFFECTED | NO | Out of scope | NOT_APPLICABLE |

---

## COSTING / PROFIT

| DOMAIN | FEATURE | TEST_OR_SCENARIO | STATUS | EVIDENCE_TYPE | EVIDENCE_SOURCE | EVIDENCE_SHA | LAST_VALIDATED_SHA | CURRENT_CODE_IMPACT | RERUN_REQUIRED | RERUN_REASON | CURRENT_RESULT |
|--------|---------|------------------|--------|---------------|-----------------|--------------|--------------------|---------------------|----------------|--------------|----------------|
| COSTING | LAST_AUTHORITATIVE / SaleLine cost | SaleCostProfit 15 | PASS | BACKEND_UNIT | POS-INVENTORY-COST-PROFIT-HARDENING-01 | migration pkg | pkg | UNAFFECTED | NO | — | EVIDENCE_REUSED |
| COSTING | Discount / net sales | Discount reporting | PASS | BACKEND_UNIT | POS-DISCOUNT-REPORTING-HARDENING-01 | `f9985d13` | `f9985d13` | UNAFFECTED | NO | — | EVIDENCE_REUSED |
| COSTING | Product profitability | BE 24 / React 12 | PASS | REACT_TEST | POS-PRODUCT-PROFITABILITY-RANKING-01 | `00fc8ac9` | `00fc8ac9` | UNAFFECTED | NO | — | EVIDENCE_REUSED |

---

## CUSTOMER ORDERS

| DOMAIN | FEATURE | TEST_OR_SCENARIO | STATUS | EVIDENCE_TYPE | EVIDENCE_SOURCE | EVIDENCE_SHA | LAST_VALIDATED_SHA | CURRENT_CODE_IMPACT | RERUN_REQUIRED | RERUN_REASON | CURRENT_RESULT |
|--------|---------|------------------|--------|---------------|-----------------|--------------|--------------------|---------------------|----------------|--------------|----------------|
| CUSTOMER_ORDERS | Settlement COGS Cash/GCash/Utang | CustomerOrderSettlementCogs | PASS | BACKEND_UNIT | POS-CUSTOMER-ORDER-COGS-HARDENING-01 | `ea9334be` | `ea9334be` | UNAFFECTED | NO | — | EVIDENCE_REUSED |
| CUSTOMER_ORDERS | Branch decrement | MB_13 | See MB report | POSTGRES_INTEGRATION | MULTI-BRANCH-HARDENING-01 | START_SHA | START_SHA | UNKNOWN | YES | Hardening | CURRENT_RUN |

---

## REPORTING

| DOMAIN | FEATURE | TEST_OR_SCENARIO | STATUS | EVIDENCE_TYPE | EVIDENCE_SOURCE | EVIDENCE_SHA | LAST_VALIDATED_SHA | CURRENT_CODE_IMPACT | RERUN_REQUIRED | RERUN_REASON | CURRENT_RESULT |
|--------|---------|------------------|--------|---------------|-----------------|--------------|--------------------|---------------------|----------------|--------------|----------------|
| REPORTING | Operational reports | SC17 + BUG_02 | PASS | CONTROLLED_PILOT | CONTROLLED + COMPLETION | `0abc15da` | `0abc15da` | UNAFFECTED | NO | — | EVIDENCE_REUSED |
| REPORTING | CSV export | SC18 + REPORT-EXPORT-01 | PASS | CONTROLLED_PILOT | POS-REPORT-EXPORT-01 | `753f5f81` | `753f5f81` | UNAFFECTED | NO | — | EVIDENCE_REUSED |
| REPORTING | Branch/org scope honesty | Dashboard + Reports scoping | PASS | REACT_TEST | DASHBOARD-REPORT-BRANCH-CLARITY; REPORTS-BRANCH-SCOPING | `7060521f` | `7060521f` | UNAFFECTED | NO | — | EVIDENCE_REUSED |

---

## UX / SECURITY / I18N

| DOMAIN | FEATURE | TEST_OR_SCENARIO | STATUS | EVIDENCE_TYPE | EVIDENCE_SOURCE | EVIDENCE_SHA | LAST_VALIDATED_SHA | CURRENT_CODE_IMPACT | RERUN_REQUIRED | RERUN_REASON | CURRENT_RESULT |
|--------|---------|------------------|--------|---------------|-----------------|--------------|--------------------|---------------------|----------------|--------------|----------------|
| UX | Responsive 360/768/1440 | SC21 | PASS | MANUAL_UI | COMPLETION Playwright 4/4 | `0abc15da` | `0abc15da` | UNAFFECTED | NO | — | EVIDENCE_REUSED |
| UX | Onboarding / empty states | PREP-01 polish | PASS | REACT_TEST | POS-ORGANIZATION-PILOT-PREP-01 | `b3380de2` | `1ff38a55` | UNAFFECTED | NO | — | EVIDENCE_REUSED |
| SECURITY | Cross-org / branch guards / idempotency | Pilot + Stock Use isolation | PASS | POSTGRES_INTEGRATION | CONTROLLED SC22; StockUse ApiTests | `34d6f093` | `34d6f093` | UNAFFECTED | NO | — | EVIDENCE_REUSED |
| I18N | en + fil/ceb/hil/ilo parity | Locale parity 02 | PASS | REACT_TEST | POS-I18N-LOCALE-PARITY-02 | `dd775a97` | `dd775a97` | UNAFFECTED | NO | — | EVIDENCE_REUSED |
| HARNESS | Org session React suite | 74→0 fail repair | PASS | REACT_TEST | POS-REACT-TEST-HARNESS-ORG-SESSION-REPAIR-01 | `6dc05a81` | `6dc05a81` | UNAFFECTED | NO | — | EVIDENCE_REUSED |
| REACT | Full vitest | 1344/1344 | PASS | BUILD_VALIDATION | COMPLETION-01 | `0abc15da` | `0abc15da` | UNAFFECTED | NO | Docs-only tip; no React prod change | EVIDENCE_REUSED |

---

## Evidence reports read (minimum + domain)

1. POS-ORGANIZATION-PILOT-READINESS-AUDIT-03
2. POS-CONNECTED-BUYER-CREATE-LINK-TEST-REPAIR-01
3. POS-ORGANIZATION-PILOT-PREP-01
4. POS-CONTROLLED-SINGLE-BRANCH-PILOT-01
5. POS-PILOT-STOCK-USE-BRANCH-BALANCE-FIX-01
6. POS-PILOT-COMPLETION-VALIDATION-01
7. POS-PURCHASE-RECEIPT-REVERSAL-01
8. POS-SUPPLIER-PAYABLES-01
9. POS-REPORT-EXPORT-01
10. POS-DISCOUNT-REPORTING-HARDENING-01
11. POS-PRODUCT-PROFITABILITY-RANKING-01
12. POS-INVENTORY-COST-PROFIT-HARDENING-01
13. POS-CUSTOMER-ORDER-COGS-HARDENING-01
14. POS-INVENTORY-OPS-POSTGRES-INTEGRATION-TESTS-01
15. POS-WASTE-LOSS-SPOILAGE-01
16. POS-INVENTORY-PERMISSION-I18N-POLISH-01
17. POS-I18N-LOCALE-PARITY-02
18. POS-DASHBOARD-REPORT-BRANCH-CLARITY-01
19. POS-REPORTS-BRANCH-SCOPING-01
20. POS-REACT-TEST-HARNESS-ORG-SESSION-REPAIR-01
21. POS-ORGANIZATION-REMAINING-GAPS-AUDIT-01/02 (historical; superseded)

Do **not** reuse AUDIT-01/02 as current readiness.
