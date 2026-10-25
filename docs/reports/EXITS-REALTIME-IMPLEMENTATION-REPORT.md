# ExItS Realtime (SignalR) — Implementation Report

[Architecture](../architecture/exits-realtime-signalr-architecture.md) | [Architecture index](../architecture/README.md)

## HEAD note

| Field | Value |
|---|---|
| Documented base commit | `50041e1bf` — `feat(pos): schedule pickup and delivery while store is closed by hours` |
| Working tree | **Uncommitted** realtime work on top of `50041e1bf` (Platform + POS.Api + PinoyBusinessPOS React + docs); not pushed |
| Report status | **FINAL VERIFICATION — PARTIAL** (2026-10-24): personal / WS / security / notifications executed; org & commerce E2E blocked by missing LV org/utang seed identities in this DB |
| Production ready | **Not claimed** — no multi-instance backplane, no outbox; org multi-session E2E not executed |

When this work is committed, update the HEAD row to the merge commit hash and remove the uncommitted caveat.

---

## Executive summary

**Platform:** `ExitsRealtimeHub` at `/hubs/exits-realtime` (cookie). Post-commit publishers for org/branch/personal settings, delivery policy/areas, membership access, shared Utang preferences, Personal Utang entry/settlement/invitation, and **Personal connection-request / accept notification** events (`PersonalNotificationChanged`). `JoinStorefrontSeller` requires membership or linked customer. Unauthorized org/storefront joins soft-reject (no group add).

**POS:** `ExitsPosRealtimeHub` at `/hubs/pos-realtime` (bearer middleware; anonymous negotiate **401**). Order/payment/settings/catalog publishers; `InventoryStockChanged` on adjust/transfer/stock-use/waste/count/DPR/stock-request. **Sale checkout does not** emit a separate inventory event.

**React:** Dual providers, query maps, reconnect refetch, draft conflict notice on fulfillment edit only.

**Verification (Docker LV `:5177` / APIs `:8091`/`:8092`, Cloudflare `https://my.exitsapps.com`):** dual Playwright contexts, real cookies + SignalR CDN listeners — **not** mocked unit substitutes for the PASS rows below.

---

## Final verification matrix (TASK=EXITS_REALTIME_FINAL_VERIFICATION)

| Field | Value |
|---|---|
| **STATUS** | **PARTIAL** |
| **BRANCH** | `main` |
| **HEAD** | `50041e1bfb078b217aee2030d0bf2c8d2a6e961b` (+ uncommitted) |
| **PLATFORM_SIGNALR** | **PASS** — negotiate 200, transports include WebSockets, cookie auth |
| **POS_SIGNALR** | **PARTIAL** — anonymous negotiate **401** PASS; authenticated bearer negotiate not proven (no org POS token in this DB) |
| **ORG_SETTINGS_E2E** | **UNVERIFIED** — LV org owners (`maria.santos@exits.local`, …) return user-not-found; Personal users have `activeOrganizationCount: 0` |
| **PERSONAL_SETTINGS_E2E** | **PASS** — dual `edcel@gmail.com` sessions; profile PUT → peer `PersonalSettingsChanged` |
| **ORDERS_E2E** | **UNVERIFIED** |
| **PAYMENTS_E2E** | **UNVERIFIED** |
| **INVENTORY_E2E** | **UNVERIFIED** (producers **IMPLEMENTED** in code; sale-checkout gap remains) |
| **UTANG_E2E** | **UNVERIFIED** (ledger/invite producers **IMPLEMENTED**; Luis/Sofia seeds absent; connection notification covered under NOTIFICATIONS) |
| **NOTIFICATIONS_E2E** | **PASS** — connection request → `PersonalNotificationChanged` `{kind:connection-request}` + unread count increase (fix applied 2026-10-24) |
| **PERMISSIONS_E2E** | **UNVERIFIED** |
| **RECONNECT_E2E** | **PASS** — hub stop/start; subsequent personal event received |
| **WEBSOCKET_DOCKER** | **PASS** — nginx `Upgrade`/`Connection` on `/platform-api` and `/pos-api` (running container); Platform negotiate via `:5177` |
| **WEBSOCKET_CLOUDFLARE** | **PASS** — `my.exitsapps.com` authenticated negotiate + `HubConnection` Connected (existing tunnel; not reconfigured) |
| **SECURITY** | **PASS** — cross-user personal isolation; soft-reject foreign org/storefront join; POS anon 401; envelopes ids/kind only |
| **PERFORMANCE** | **PASS** (spot) — singleton Platform/POS connections in client code; no global invalidate observed in personal path; no full-page reload required for verified scenarios |
| **TESTS_PASSED** | 3 (`realtime-query-map.test.ts`) |
| **TESTS_FAILED** | 0 |
| **UNVERIFIED_SCENARIOS** | Org hours; Connected Commerce/payment settings; scheduled order ↔ seller; payment status to customer; inventory consumers; shared Utang entry/repayment; staff permission revoke; dirty branch draft dual-user E2E (code-only) |
| **FILES_CHANGED** | See below |
| **ARCHITECTURE_MD_UPDATED** | **YES** |
| **IMPLEMENTATION_REPORT_MD_UPDATED** | **YES** |
| **REMAINING_GAPS** | See Deferred work |

---

## Status matrix — capability vs implementation

| Capability | Status | Evidence |
|---|---|---|
| Platform envelope + event constants | **IMPLEMENTED** | `RealtimeEventEnvelope.cs`, `RealtimeEventTypes.cs` |
| Platform group naming + publish helpers | **IMPLEMENTED** | `RealtimeGroupNames.cs`, `RealtimePublish.cs` |
| `SignalRRealtimeChangePublisher` | **IMPLEMENTED** | `SignalRRealtimeChangePublisher.cs` |
| `ExitsRealtimeHub` + membership join | **IMPLEMENTED** | `ExitsRealtimeHub.cs` |
| Storefront join: member **or** linked customer | **IMPLEMENTED** | `JoinStorefrontSeller` |
| Publish: org / personal / branch hours & fulfillment | **IMPLEMENTED** | Phase 1 use cases |
| Publish: delivery policy & areas | **IMPLEMENTED** | Branch delivery use cases |
| Publish: membership revoke / suspend / role change | **IMPLEMENTED** | Membership use cases |
| Publish: shared Utang preferences | **IMPLEMENTED** | `UpdatePersonalSharedUtangPreference` |
| Personal Utang ledger / repay / invite / notification realtime | **IMPLEMENTED** | `PersonalUtangRealtime` + entry/settlement/invitation publishers |
| Personal connection request/accept → notification realtime | **IMPLEMENTED** | `RequestPersonalConnection` / `AcceptPersonalConnectionRequest` (2026-10-24 fix) |
| Antiforgery exempt `/hubs/` | **IMPLEMENTED** | `PlatformBrowserAntiforgeryMiddleware.cs` |
| Legacy dual-publish hubs | **IMPLEMENTED** | Personal photo + storefront availability |
| POS hub + bearer gate | **IMPLEMENTED** | Middleware 401 without user; `OnConnected` Abort |
| Publish: customer order / payment / commerce / catalog | **IMPLEMENTED** | POS use cases |
| Publish: `InventoryStockChanged` | **IMPLEMENTED** | Adjust, transfer, stock-use, waste, count, DPR, stock-request |
| Sale-checkout → `InventoryStockChanged` | **NOT** | Order/payment events only |
| React Platform + POS providers | **IMPLEMENTED** | `src/realtime/*` |
| `useRealtimeDraftGuard` + notice on fulfillment edit | **IMPLEMENTED** | `BranchFulfillmentEditPage` |
| Draft guard on all settings forms | **PLANNED** | |
| Redis SignalR backplane | **NOT SUPPORTED** (MVP) | Criteria in architecture §28 |
| nginx WebSocket Upgrade | **IMPLEMENTED** | Verified in running `react-pos` nginx |
| Multi-session E2E | **PARTIAL** | Personal/WS/security/notifications PASS; org/commerce UNVERIFIED |

---

## Defect fixed during final verification

| Defect | Fix |
|---|---|
| Connection request created in-app notification + unread count but **no** SignalR event | Inject `IRealtimeChangePublisher` into `RequestPersonalConnection` and `AcceptPersonalConnectionRequest`; post-commit `PersonalUtangRealtime.NotifyNotificationChangedAsync` (`kind`: `connection-request` / `connection-accepted`) |

---

## Gap audit (Priority 3)

| Area | Finding |
|---|---|
| Draft conflict handling | **Only** `BranchFulfillmentEditPage` wires dirty-skip + `RealtimeDraftConflictNotice`. Other settings forms still rely on query invalidate (may overwrite drafts). No additional wiring in this pass (no demonstrated runtime bug beyond known gap). |
| Sale-checkout inventory | `CheckoutSale` / `SaleUseCases` do **not** call `PosRealtimePublish.TryInventoryStockChangedAsync`. Inventory consumers that depend solely on `InventoryStockChanged` will not refresh from POS sale completion; order/payment events cover order boards. **Not changed** (documented gap only). |

---

## Server / React inventory

Unchanged from Phase 2 lists except:

| File | Change |
|---|---|
| `Platform.Application/Personal/PersonalConnectionUseCases.cs` | Publish `PersonalNotificationChanged` after connection request create / accept |
| `docs/architecture/exits-realtime-signalr-architecture.md` | Verification snapshot §28a |
| `docs/reports/EXITS-REALTIME-IMPLEMENTATION-REPORT.md` | This final matrix |

Prior Phase 2 inventory (hubs, publishers, React `src/realtime/`, nginx WS headers) remains as implemented.

---

## Event publish matrix — POS (inventory)

| EventType | Producer | Status |
|---|---|---|
| `InventoryStockChanged` | Adjust, transfer dispatch/receive, stock-use, waste/loss, stock-count complete, direct purchase, stock-request create/approve/reject/cancel | **IMPLEMENTED** |
| `InventoryStockChanged` from sale checkout | — | **NOT** (gap) |

---

## Tests and validation

| Suite | Status |
|---|---|
| Vitest `realtime-query-map.test.ts` | **3 passed** (2026-10-24) |
| Dual-browser Playwright harness (temp scripts under `%TEMP%`) | Executed against live LV — see matrix |
| Hub authorization integration tests | **PLANNED** |
| Full `ExItS.slnx` Release test run | **Not run** for this document |

---

## Working tree hygiene

| Observation | Recommendation |
|---|---|
| ~1574 untracked `.js` siblings under React `src` | **Do not commit** |
| `App_Data` | Leave alone |
| Temp harness scripts | `%TEMP%\exits-realtime-*.mjs` — not repo files |

---

## Deferred work (explicit)

1. Seed or restore LV org catalog + Luis/Sofia (or equivalent) and execute org hours, commerce, orders, payments, inventory, utang, permissions E2E.
2. Dual-browser dirty draft conflict on branch fulfillment (code path exists; E2E not run).
3. Optional `InventoryStockChanged` on sale-checkout stock consume.
4. Transactional outbox (**PLANNED**).
5. Redis / Azure SignalR when APIs scale to >1 instance (**NOT SUPPORTED** until criteria met — do not install).
6. Expand draft-guard to remaining long-lived settings forms.
7. Hub integration tests for join denial.
8. Authenticated POS bearer negotiate E2E once an org workspace token is available.

---

## Security and portfolio notes

- Platform cookie hub vs POS bearer hub remain separate.
- Envelopes: ids/`kind` only — no amounts, notes, or notification preview text on Utang/notification events.
- Soft-reject unauthorized joins (no HubException) — clients must not assume join implies membership.
- No Redis introduced. No production migrations applied. No commit/push.

---

## Git evidence (as of report authoring)

| Item | Value |
|---|---|
| Base `HEAD` | `50041e1bfb078b217aee2030d0bf2c8d2a6e961b` |
| Branch | `main` |
| Commit / push | **Not performed** per task instruction |

---

## Next work package (suggested)

1. Restore full Local Validation identity catalog (org owners/staff + personal utang pair) in this environment, then finish UNVERIFIED E2E rows.
2. Commit Platform + POS + React realtime + docs; exclude accidental `.js` emit and `App_Data`.
3. Optional sale-checkout inventory event if sell-floor stock freshness requires it beyond order events.
