# P19 — MAUI list-load performance

| Field | Value |
|---|---|
| Status | **Code Complete / Validation Pending** |
| Phase | [Phase 19](../phases/phase-19-mobile-pos-operations-and-cashier-experience.md) — **Open** |
| Migration | **No** |
| Device Verified | **No** |
| Browser Verified | **No** |
| Production Ready | **No** |
| Date | 2026-08-18 |

## 1. Problem

Physical-device Customers stayed on **Loading customers...** for ~50 seconds. Catalog / Sell product browse felt similarly slow. Local Validation Platform and POS `/health` on the host were a few milliseconds; the delay was the MAUI client waiting on work that is not required to paint the page.

## 2. Root cause

- **Customers:** `CustomersList` awaited `DownloadIncrementalAsync()` before `ListAsync`. Incremental download pages customers, credit entries, and repayments. Failed GETs retry once after a 15s HTTP timeout (~30s per hanging call). Credit/repayment pages then called `GetCreditSummaryAsync` per customer (N+1).
- **Sell:** `SaleCheckout` loaded shift, setup, categories, and the first product page, then **awaited** a full local catalog cache refresh (up to 20 pages of 100) before `_ready`.
- **Catalog list:** categories then products ran sequentially. Each product list also waited on Platform image-meta (10s HttpClient timeout if Platform was slow).
- **Request storm / Products error:** header components restarted Platform calls on every parent re-render (notifications `OnParametersSetAsync`, org logo cancel/restart, role label `ListEligibleOrganizations`). Each Platform call also queries memberships. Sync status `Refresh()` re-rendered the shell even when the snapshot was unchanged. That flood can make `/catalog` fail as **Something went wrong**.
- **Sign-in timeout on role chooser:** password login succeeded, then Sign-In awaited a 20s `SelectOrganization` bind under the button spinner. Failure fell through to organization-select, which showed **Loading...** through a second bind while the auth header also fetched unread/sync/logo. The second bind hit the 15s HTTP timeout and showed **The sign-in request timed out.**

## 3. Delivered

- Customers list/search/paging call `ListAsync` immediately. Incremental offline download runs in the background on first load and Refresh only (not on every search keystroke).
- Incremental credit/repayment download rebuilds local optimistic balances instead of per-customer credit-summary HTTP. Single-flight so reconnect auto-sync and the list cannot stack duplicate downloads.
- Sell paints after the first browse page; full catalog cache refresh is background. Categories and browse products load in parallel.
- Catalog product list loads categories and products in parallel.
- Product **list** mapping uses local image metadata only (no live Platform image-meta). Product detail and storefront still resolve live versions (storefront 2s cap).
- Header logo / notifications / role label do not cancel-and-restart the same in-flight request. Sync status notifies the shell only when the snapshot changes. POS bottom-nav setup check is not repeated after it is known complete.
- Catalog HTTP 429 is classified as rate-limited and shown as a retryable load failure, not a generic unexpected error.
- Sign-in lists memberships then goes to organization-select; it does not bind the organization under the login spinner and does not re-run RestoreSession (`ResolveStartRouteAsync`) after password success. Eligible-organization listing is single-flight cached for 45s so login, org-select, and owner probes share one GET `/auth/organizations`. Auth header sync/unread/logo HTTP is suppressed on sign-in and organization-select so bind is not starved.

Offline cache, reconnect sync, credit-detail server refresh, and mutation-time summary refresh are unchanged in purpose.

## 4. Explicit non-changes

No WP11/WP12 commerce rules, identity model, entitlement, or production-auth claims. HTTP timeout remains 15s. GET retry-once remains. Not Device Verified.

## 5. Tests

`MauiListLoadPerformanceGuardTests` — customers list does not await incremental download before `ListAsync`; checkout does not await `RefreshFromServerAsync`; catalog list uses `Task.WhenAll`; incremental download does not N+1 credit summaries; product list mapping does not call live Platform image-meta; sign-in does not bind under the login spinner; organization-select clears Loading... before auto-bind; AuthShell quiets header HTTP on sign-in/org-select.

`ShellNotificationBellGuardTests` / `ShellContextIdentityGuardTests` — notification unread and org logo do not refetch on every parent render.

## 6. How to validate on the phone

Rebuild/reinstall MAUI Debug against Local Validation (`100.120.79.81`). Open Customers, Catalog, and Sell. First paint should follow a single list round-trip (seconds, not ~50s). Cloud/sync chip may still catch up in the background.

## 7. Git

| Commit | Hash |
|---|---|
| Feature | `9287de75` |
