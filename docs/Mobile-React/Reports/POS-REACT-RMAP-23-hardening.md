# POS React — RMAP-23 Parity / Security / UX Hardening

**Package:** RMAP-23  
**Branch:** `feat/pos-react-client`  
**Starting HEAD (after B04):** `ef4aba01dfd1de75a1d0bbae86adf48e55dd0cf6`  
**Status:** COMPLETE (automated gates; accepted non-production gaps documented)

---

## Summary

Final hardening pass across React POS + Personal + Organization essential flows. No new product features beyond defects required to close verified parity/security/UX gaps.

---

## A. Authorization matrix

| Area | Evidence | Result |
| ---- | -------- | ------ |
| Personal / Organization / staff roles | Existing `pos-capabilities.test.ts`, `SessionGuards.*`, RMAP-02R E2E | PASS (regression re-run) |
| Device registration vs sale execution | RMAP-10b E2E + `sell-readiness` + `AuthorizeForTransactions` unit tests | PASS — unregistered/revoked blocked for financial sale |
| Linked buyer history | B04 client tests + Phase-24 backend auth chain | PASS — fail-closed read projection |
| Cross-org | Workspace guards + integration/unit patterns | PASS (no new leakage found) |

UI hiding is not treated as authorization proof; backend fail-closed patterns preserved.

---

## B. Device hardening

| Check | Evidence | Result |
| ----- | -------- | ------ |
| Login on unregistered device allowed | RMAP-10b + device presentation tests | PASS |
| Financial sale requires Active registered device | `sell-readiness`, checkout gates, offline cash tests | PASS |
| Final-slot concurrency | **NEW** `PosDeviceConcurrentRegistrationIntegrationTests` (PostgreSQL Testcontainers, `pinoy-business-pos` plan, max 1 device, 2 parallel scoped registrations) | PASS — exactly 1 success, 1 `application.pos_device.capacity_exceeded`, active count = 1 |
| Org advisory lock | Existing `RegisterCurrentDevice_serializes_capacity_under_organization_lock` + `ExecuteWithOrganizationLockAsync` in use case | PASS |

---

## C. Device audit history — reactivation

**Gap closed:** Revoked device re-registration previously logged only `platform.pos_device.registered`.

| Change | Detail |
| ------ | ------ |
| New audit action | `platform.pos_device.reactivated` |
| Use case | `RegisterPosDeviceOutcome` with `PosDeviceRegisterKind` (`New`, `Reload`, `Reactivated`) |
| API | Register endpoint writes Registered vs Reactivated; reload touch does not duplicate lifecycle audit |
| Display | `GovernanceAuditDisplay` — “Device reactivated” |
| Unit test | `Register_reactivates_revoked_device_with_reactivated_kind` |

Revocation history remains on immutable Platform audit events; live `PosDevice` row may clear revoke fields on reactivation.

---

## D. Offline security regression (RMAP-21)

Full Vitest offline suite re-run (**PASS** 547/547 total client tests). Preserved:

- Encrypted IndexedDB outbox, server-authoritative price leases, Today’s Price snapshot
- Weighted/UOM fidelity, immutable local total, cash received/change, idempotency
- Unregistered device cannot execute offline Cash; GCash/Utang/discount/override/lot offline blocked

**Accepted gap (unchanged):** Cold-start IndexedDB unlock remains documented security gap — not falsely resolved.

---

## E. QR / identity hardening

| Surface | Evidence | Result |
| ------- | -------- | ------ |
| Personal My QR + Business QR | **NEW** `e2e/rmap-23-qr-responsive.spec.ts` — 5 viewports × 2 surfaces (10 tests) | PASS |
| Purpose guards / malformed QR | `checkout-personal-customer-picker.test.tsx`, `envelope.test.ts` | PASS (regression) |
| Live camera | **NOT IMPLEMENTED** — still-image/file decode + manual ExItS ID fallback only | `LIVE_CAMERA_VERIFIED=NO` |

---

## F. Account / workspace switching

| Fix | Detail |
| --- | ------ |
| `dropdown-menu.tsx` | `MenuItem` forwards `...rest` (incl. `data-testid`) |
| `personal-switch-to-business.test.tsx` | Offline avatar test scopes hint to open menu; waits for More button disabled first |

Vitest: **8/8** personal switch tests PASS.

---

## G. Wording / terminology

Customer-facing React copy audit: no “Register browser”, “Browser slot”, or “activation token” in user-visible strings (backend compatibility preserved). Device terminology uses **Device / POS Device / Register this device / Active devices / Device limit**.

Internal parse error string in `envelope.ts` remains developer-facing only.

---

## H. Accessibility

- `foundation.spec.ts` axe smoke (sign-in/preferences) — existing PASS
- Structural patterns: skip link, dialog/sheet focus, touch targets ≥44px on primary actions — spot review on QR pages and linked-merchant surfaces

---

## I. Responsive / UI debt

Existing RMAP-00 + package E2E matrices re-validated. RMAP-23 QR spec adds 375×812, 390×844, 768×1024, 1024×768, 1440×900 with `assertNoHorizontalOverflow`.

Playwright green supplemented by QR viewport checks (not a substitute for full manual visual sign-off).

---

## J. Performance / quality

Spot audit: linked-merchant and statement clients use paged/cursor contracts; no new unbounded list fetches in B04 surfaces. No service-worker cache-first financial API behavior observed.

---

## K. Full regression results

| Gate | Result |
| ---- | ------ |
| Vitest | **547 / 547 PASS** |
| TypeScript `tsc -b` | PASS |
| ESLint | PASS (17 pre-existing warnings, 0 errors) |
| Production `vite build` | PASS |
| Platform unit (RegisterCurrentDevice filter) | **8 / 8 PASS** |
| Platform integration (device concurrency) | **1 / 1 PASS** |
| Playwright RMAP-23 QR | **10 / 10 PASS** |

Pre-existing Prettier drift in `PersonalHubPages.tsx` / `RoleHomePages.tsx` (unrelated WIP) — not introduced by RMAP-23.

---

## Known accepted gaps (non-production)

| Gap | Status |
| --- | ------ |
| Cold-start offline store unlock | ACCEPTED documented gap |
| Live browser QR camera | NOT IMPLEMENTED |
| Organization buyer purchase history | NO API contract (B04) |
| RMAP-TAX | NOT AUTHORIZED |
| Native speaker locale certification | PENDING flags honest where applicable |

---

## Files changed (RMAP-23)

**Platform:** `PosDeviceUseCases.cs`, `PlatformAuditActions.cs`, `BranchAndDeviceEndpoints.cs`, `GovernanceAuditDisplay.cs`  
**Tests:** `RegisterCurrentDeviceCapacityTests.cs`, `RegisterCurrentDeviceBranchConflictTests.cs`, `PosDeviceConcurrentRegistrationIntegrationTests.cs`  
**React:** `dropdown-menu.tsx`, `personal-switch-to-business.test.tsx`, `ops-ux-encoding-hygiene.test.ts`, `e2e/rmap-23-qr-responsive.spec.ts`

---

## Flags

```
RMAP_23=COMPLETE
RMAP_TAX_AUTHORIZED=NO
TAX_UI_EXPOSED=NO
LIVE_CAMERA_VERIFIED=NO
PRODUCTION_READY=NO
```

**Next:** RMAP-24 final validation matrix execution.
