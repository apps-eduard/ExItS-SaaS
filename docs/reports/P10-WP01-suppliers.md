# P10-WP01 — Suppliers

Date: 2026-07-31  
Phase marker: `P10-WP01-suppliers`  
Status: **Complete**  
Branch: `main`  
Prior tip (ambiguity docs): `97e17c248ddd1c0af588eafaa41ac7ab6910ec2f`  
Authorized scope: **Option A — Supplier master data only**

## 1. Summary

Organization-owned supplier master/reference data for PinoyBusinessPOS: Active/Inactive lifecycle, server-generated `SUP-NNNNNN` codes, duplicate prevention, typed API, MAUI screens, PostgreSQL migration `AddPosSuppliers`, feature grants, online-only mutations. No purchasing, receiving, payables, costing, stock, or financial transactions.

## 2. Approved Option A scope

Recorded on `docs/phases/phase-10-full-pos.md` and this report. Ambiguity record updated to **Option A selected and authorized**.

## 3. Supplier model and lifecycle

| Field | Notes |
|---|---|
| SupplierId | GUID PK (not SupplierCode) |
| OrganizationId | Immutable; server context authoritative |
| SupplierCode | `SUP-NNNNNN`; server-allocated; immutable |
| Name + NormalizedName | Required; active uniqueness on normalized name |
| Contact / address / tax / notes | Optional bounded plain text |
| Status | Active ↔ Inactive; create starts Active; no hard delete |
| Timestamps + xmin | UTC + PostgreSQL concurrency token |

## 4. Duplicate rules

- **Hard:** active normalized name unique per organization (DB filtered unique + application guard)
- **Likely conflicts (when supplied):** normalized email, mobile, tax/registration among Active suppliers
- Inactive names preserved; reactivation rechecks active uniqueness
- No merge/aliasing

## 5. Authorization matrix

Grants: `store-suppliers-view`, `store-suppliers-manage`

| State | View | Manage (create/update/activate/deactivate) |
|---|---|---|
| Trialing / Active / GracePeriod | Grant-controlled | Grant-controlled |
| PastDue / Cancelled / Expired | Grant-controlled | Deny |
| Suspended / missing / stale / unknown | Deny | Deny |

Platform product access alone does not grant supplier management. No POS operational roles in this WP.

## 6. Migration and indexes

Database: `ExItS_PinoyBusinessPOS` · Schema: `pos` · Migration: `AddPosSuppliers`

Tables: `pos.suppliers`, `pos.supplier_code_sequences`

Indexes: org+code unique; org+active normalized name unique; org+status; org+normalized name; filtered org+email/mobile/tax.

Validated: apply → rollback to `AddPosPerformanceIndexes` → re-apply (integration tests).

## 7. API inventory

- `GET/POST /api/v1/pos/suppliers`
- `GET/PUT /api/v1/pos/suppliers/{supplierId}`
- `POST .../activate`, `POST .../deactivate`

Typed DTOs; ProblemDetails `errorCode`; org concealment (404); pagination; cancellation; optimistic concurrency via `ExpectedUpdatedAtUtc`.

## 8. MAUI routes

- `/suppliers`, `/suppliers/new`, `/suppliers/{id}`, `/suppliers/{id}/edit`
- Entry from `/more` (not bottom nav)
- EN + fil-PH; System/Light/Dark via DesignSystem
- Online-only reconnect UX; no purchasing controls

## 9. Online-only policy

No supplier offline queue, local mutation projections, or sync conflict policy. Existing customer/credit offline behavior unchanged.

## 10. Security and privacy

- Org from trusted server context / headers (Dev/Testing)
- Cross-org IDs concealed
- Contact details not logged in full; no secrets in supplier records
- Production rejects Development/Testing bypasses
- No HealthCare/PHI coupling

## 11. Explicit exclusions (preserved)

Purchase orders, receiving, supplier invoices, AP, payments/balances, stock increases, cost history, returns, credits, purchasing reports, attachments, import/export, offline supplier mutations, POS roles, **P10-WP02+**.

## 12. Tests and Android evidence

| Suite | Passed | Failed | Skipped |
|---|---:|---:|---:|
| Full `ExItS.slnx` (test projects) | **1047** | **0** | **0** |

Baseline preserved (1001) + focused supplier coverage (~46).

Android: Release `net10.0-android` build **succeeded**. No device/emulator available — **R-109 retained** (no interactive validation claimed). NU1903 (R-129) remains open.

## 13. Open risks

Unchanged release blockers: R-091, R-109, R-129, TLS-PROD, MAUI-HTTPS, POS-ROLES; Manual GCash unverified; online-only Basic Store limits; PITR deferred; etc.

## 14. Exact next work package

**P10-WP02 — Purchasing** — do **not** begin until explicitly authorized.

## 15. Git

| Field | Value |
|---|---|
| Feature commit | `6f92dd43b2f66709891d82079f9d3fbd0b5c450e` |
| Feature message | `feat(pos): add organization-owned supplier master data (P10-WP01 Option A)` |
| Docs commit | `55469c60802d11273669efa10494ff1632efa84d` |
| Docs message | `docs(pos): record P10-WP01 suppliers Option A completion evidence` |
| HealthCare | ignored, untracked, outside `ExItS.slnx` |

Exact next: **P10-WP02 — Purchasing** (do not begin until authorized).
