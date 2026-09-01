# POS-MULTI-BRANCH-V2 MB2-05 — Guided Branch Setup

**Program:** POS-MULTI-BRANCH-COMMERCE-V2  
**Package:** MB2-05  
**Branch:** `feat/organization`  
**Status:** IMPLEMENTED (pending parent validation)  
**Depends on:** MB2-04-H1

---

## Delivered

### Branch readiness API

- `GET /api/v1/pos/branches/{branchId}/readiness`
- Derived sections: Details, Staff, Products, Pricing, Inventory, Parties, Fulfillment, Device
- Side-effect free batched metrics via `IBranchReadinessMetricsRepository`

### ExplicitAssign API (closes deferred MB2-04)

- `POST/DELETE /api/v1/pos/parties/customers/{customerId}/branch-access`
- `POST/DELETE /api/v1/pos/parties/suppliers/{supplierId}/branch-access`
- Owner/Admin governance required; foreign org/customer rejected

### Setup progress metadata

- Migration `20260901185015_SyncMb2PartyAccessAndSetupProgress` (includes `branch_setup_progress`)
- Table `pos.branch_setup_progress` — last visited step + timestamps only (no duplicate completion booleans)

### React guided setup

- Route `/org/branches/:branchId/setup`
- `BranchGuidedSetupPage` + `branch-readiness-client`
- i18n keys under `branches.setup.*` (en, fil-PH, ceb-PH, ilo-PH, hil-PH)
- `WorkspaceProvider` invalidates `branch-readiness` on branch switch

### Integration proofs

- `BranchSetupPartyAccessIntegrationTests` — SETUP-PARTY-01…08
- `BranchGuidedSetupIntegrationTests` — Mica C readiness + setup progress round-trip

---

## Gaps / follow-ups

- Staff/device readiness counts rely on POS-side heuristics; Platform staff/device assignment counts not yet joined into readiness API.
- Guided setup page links to existing management surfaces; no inline product/stock/party pickers in V1.

---

## Next

**MB2-06** cross-surface + offline hardening.
