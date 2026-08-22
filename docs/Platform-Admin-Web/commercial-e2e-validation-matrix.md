# Commercial E2E validation matrix

**Status:** PLANNING / PARTIAL — PA-COM-04 React lifecycle UI is implemented and unit/Playwright-tested. Full Admin-driven paid spine and live Platform→POS proof are **not** executed.
**Audit HEAD:** `525bae3633fb7fde1bbc9b855435a05f5f616c09`  
**POS React inspected:** `42d487d42c0d9e3e6592cafcb2259c24655dbb23`  
**Related:** [contract](./commercial-platform-pos-contract.md) · [implementation plan](./commercial-subscription-implementation-plan.md)

Step verdicts use: **PASS TODAY** · **REACT ADMIN GAP** · **BACKEND GAP** · **POS GAP** · **TEST HARNESS GAP**.

“PASS TODAY” means the **current code path exists** if the prerequisite commercial state is created by seed, Blazor Admin, Personal Start-a-Business, or API — **not** that React Admin can drive it.

---

## 1. Full spine (target)

```text
Platform Admin → configure Product/Plan
  → organization gets plan
  → subscription created
  → entitlement generated
  → organization opens POS React
  → limits/features enforced
  → device registration
  → staff/branch/features
  → upgrade/downgrade
  → suspend
  → reactivate
  → billing/history/audit
```

**Full flow possible from React Admin today: PARTIAL.** Trial + suspend/reactivate/cancel/plan-change UI exists (PA-COM-04). Paid subscribe, convert-trial, and paid Active upgrade remain PA-COM-06. Live POS proof is Agent 1 + Local Validation.

---

## 2. Device-limit target flow

Prerequisites: Pinoy Business POS product Active; Growth plan `MaxActivePosDevices = 3`; Pro = 10 (seed).

| # | Step | Verdict | Notes |
|---|---|---|---|
| 1 | Platform Admin shows Growth max devices = 3 | **PASS TODAY** | `PlanDetailPage` displays `maxActivePosDevices` |
| 2 | Admin assigns/creates Growth subscription for org | **REACT ADMIN AVAILABLE** (Start trial) | `POST .../trials` from Organization → Subscription. Paid create remains PA-COM-06 |
| 3 | Entitlement snapshot contains device limit 3 | **PASS TODAY** (if subscription exists) | grant `plan-max-active-pos-devices`; React can **read** snapshots |
| 4 | Org logs into POS React | **PASS TODAY** | POS React auth; not Admin |
| 5 | Device Management shows 0 of 3 | **PASS TODAY** | `OrgPosDevicesPage` + capacity API |
| 6 | Register device #1 → 1 of 3 | **PASS TODAY** | Platform `RegisterCurrentDevice` |
| 7 | Device #2 → 2 of 3 | **PASS TODAY** | |
| 8 | Device #3 → 3 of 3 | **PASS TODAY** | |
| 9 | Device #4 denied | **PASS TODAY** (server + unit/integration) | Playwright often **mocks** capacity → **TEST HARNESS GAP** for browser proof |
| 10 | Admin upgrade Growth → Pro | **REACT ADMIN AVAILABLE** (Trialing); **PA-COM-06** for paid Active | Trialing upgrade uses `POST .../upgrade`. Active paid upgrade returns payment-required |
| 11 | Entitlement refreshed | **PASS TODAY** on upgrade use case | snapshot regenerated server-side |
| 12 | POS shows higher device limit | **PASS TODAY** | capacity uses **live plan**, not token |
| 13 | Additional device registration possible | **PASS TODAY** after upgrade | |
| 14 | Admin suspend subscription | **REACT ADMIN AVAILABLE** | `POST .../suspend` from Organization → Subscription |
| 15 | POS commercial access changes | **PASS TODAY** (ops deny) | `CanEnterProduct` Suspended=false; UtangCapability deny. Device APIs may still be active-like → **BACKEND NUANCE** |
| 16 | Admin reactivate | **REACT ADMIN AVAILABLE** | `POST .../reactivate` for Suspended only. Cancelled/Expired have no Reactivate |
| 17 | POS commercial access restored | **PASS TODAY** after reactivate | |

**Conclusion:** React Admin can drive trial / plan-change / suspend / reactivate / cancel. Paid activation and payment management are **not** in PA-COM-04. Full Admin→POS live spine is **not** signed off until Local Validation + Agent 1.

---

## 3. Starter / Growth / Pro limit scenarios

Authoritative values: `MvpPosPlanCatalog` (DEVELOPMENT defaults).

### Starter — 1 branch, 3 staff, 1 POS device, 1 business type; credit/reports/export off

| Check | Backend enforcement | React Admin config | POS React reacts | E2E | Missing package |
|---|---|---|---|---|---|
| 1 branch | YES (create blocked) | display only | weaker UX than devices | platform tests | PA-COM-03 (edit), PA-COM-07 (verify) |
| 3 staff | PARTIAL (preview only; no invite check) | display only | explore copy | **BACKEND GAP** + PA-COM-07 | staff invite enforcement (not PA-COM UI) |
| 1 POS device | YES | display only | Device Management | unit/integration; Playwright mocked | PA-COM-04 to put org on Starter; PA-COM-07 |
| 1 business type | YES (activation) | display only | activation UX | platform tests | PA-COM-07 |
| Customer credit disabled | YES if grants not merged | plan boolean | capability (Dev merge may hide) | **TEST HARNESS GAP** | PA-COM-07 true-enforcement mode |
| Advanced reports disabled | catalog only | plan boolean | **no POS gate** | none | POS GAP (do not fake in Admin) |
| Export disabled | catalog only | plan boolean | **no POS gate** | none | POS GAP |

### Growth — 3 / 10 / 3 / 3; credit/reports/export on

Same pattern: device/branch/BT **YES**; staff invite **PARTIAL**; reports/export **catalog only**; credit **YES** unless Dev merge.

### Pro — 10 / 30 / 10 / 6; credit/reports/export on; trial not allowed

Starting a Pro **trial** must fail (`TrialAllowed=false`). Paid subscribe required (PA-COM-06).

Ordering/Delivery: **do not** expect Starter-off / Pro-on. Seed grants both on all MVP plans.

---

## 4. Subscription lifecycle scenarios (Admin-driven)

| Scenario | API | React today | After PA-COM-04 |
|---|---|---|---|
| No subscription → Trialing (Starter/Growth) | `POST .../trials` | **AVAILABLE** (PA-COM-04) | delivered |
| Trialing → Active (convert) | `POST .../convert-trial` | GAP (needs payment) | **not in 04**; PA-COM-06 |
| No subscription → Active paid | `POST .../subscriptions` + paymentId | GAP | **not in 04**; PA-COM-06 |
| Active → upgrade | `POST .../upgrade` | **AVAILABLE** while Trialing; paid Active → payment-required copy | 04 + 06 for paid |
| Active → scheduled downgrade | `POST .../downgrade` | **AVAILABLE** | delivered |
| Active → GracePeriod | `POST .../grace-period` | **AVAILABLE** (Support actions) | delivered |
| * → PastDue | `POST .../past-due` | **AVAILABLE** (Support actions) | delivered |
| * → Suspended | `POST .../suspend` | **AVAILABLE** | delivered |
| Suspended → Active | `POST .../reactivate` | **AVAILABLE** | delivered |
| * → Cancelled | `POST .../cancel` | **AVAILABLE** | delivered |
| * → Expired | `POST .../expire` | **AVAILABLE** (Support actions) | delivered |
| Cancelled → Reactivate | **unsupported** | must create new | do not invent button (**not shown**) |
| Dedicated renew | **no Admin HTTP** | — | stop / Local Validation simulate only |
| Dedicated Activate | `POST .../activate` | **not exposed** (always payment-required) | PA-COM-06 |

---

## 5. Billing / Local Validation simulations

Only these simulation outcomes are supported (see contract/plan). Production: endpoint 404.

| Simulation | Use in E2E |
|---|---|
| Succeeded | paid subscribe / upgrade happy path |
| Declined / Failed | paid path must not activate |
| Pending | do not claim Active |
| Refunded | document resulting payment/subscription server behavior at implementation time — do not invent |
| RenewalSucceeded / RenewalFailed | Local Validation only; no Admin renew route |

Manual SaaS: PendingConfirmation → Confirmed/Rejected; Confirmed → Voided. Methods: Cash, BankTransfer, GCash (manual); Online (provider).

Never mix with POS Cash / POS Manual GCash / POS Utang.

---

## 6. Cross-org / security checks

| Check | Verdict |
|---|---|
| Org A subscription not visible/mutable as Org B | BACKEND AVAILABLE; keep fail-closed in React |
| UI permission hide vs server 403 | UI convenience only |
| CSRF on mutations | Foundation PASS TODAY (PWEB-20); used by PA-COM-04 subscription lifecycle |
| Social login `sessionToken` in URL | `BLOCKS_CUTOVER` (unrelated; do not claim cutover) |

---

## 7. Recommended execution of this matrix

1. PA-COM-01 foundation  
2. Local Validation org with no POS subscription  
3. PA-COM-04: start **Growth** trial  
4. PA-COM-05: confirm snapshot device limit 3  
5. POS React Device Management: 0/3 … 3/3 … 4th denied (real capacity, not mock)  
6. PA-COM-06 + upgrade to **Pro**; confirm 4th device allowed  
7. Suspend; confirm POS commercial deny; record device-API nuance  
8. Reactivate; confirm POS restore  
9. Repeat Starter credit-off **only** in a non-grant-merging environment  
10. Verify audit events on org Activity  
11. PA-COM-08 records the signed matrix — still **not** Production Ready  

Until paid paths (PA-COM-06) and live Local Validation + Agent 1 POS proof exist, mark:

`PLATFORM_POS_COMMERCIAL_E2E=NOT_READY_UNTIL_AUDIT_GAPS_CLOSED`
