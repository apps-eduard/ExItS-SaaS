# Branch Guided Setup

**Program:** POS-MULTI-BRANCH-COMMERCE-V2
**Status:** TARGET_LOCKED (MB2-00)
**Parent:** [multi-branch-commerce-v2.md](multi-branch-commerce-v2.md)
**Implements in:** MB2-05 (consumes MB2-01…04)

---

## 1. CURRENT_PROVEN

- Branch create exists (Platform); capacity MaxBranches; new secondary Active non-primary.
- Create does **not** clone catalog, staff, devices, inventory, fulfillment enablement (WP12 / Branch Management).
- New branch inventory starts zero/unallocated to primary (Model A).
- Staff not auto-assigned; devices not moved; fulfillment flags default off for safe creates.
- No resumable multi-step setup wizard today.

Lifecycle remains: Active / Inactive / Archived — **do not invent Draft branch status** without strong evidence.

---

## 2. TARGET flow — LOCKED

```
CREATE BRANCH
  ↓
SETUP WORKFLOW (resumable)

1 Products
2 Prices
3 Starting stock
4 Customers
5 Suppliers
6 Staff access
7 POS devices / registers
8 Fulfillment
9 Review & finish
```

Not one giant DB transaction. Each step: safe, idempotent mutations. Resume later.

**Setup progress ≠ Branch Status.** Branch may be Active while Setup = In progress.

**OD-03 recommended:** Hybrid — derive checklist from data; optional persisted progress for UX resume.

---

## 3. Template / reference branch — LOCKED

Default template: Main/Primary. Future: copy reference from Main, another Active branch, or defaults.

Template means **configuration reference**, never:

clone DB, history, stock quantities, customer balances, devices, shifts, sales.

---

## 4. Step contracts

### 1 — Products

- OrganizationStandard: **default checked** (offered).
- Owner/Admin may uncheck → not offered (availability false).
- Search, categories, select all, clear all, selected count.
- BranchLocal from template: **do not auto-inherit**. Informational: “Branch-only products are not copied.”
- Promotion outside wizard in V1 (`LOCAL_PROMOTION_INSIDE_BRANCH_WIZARD=DEFERRED`).

### 2 — Prices

- Only offered products.
- Baseline = organization default.
- Default: inherit (no override row).
- Override only when user changes value.
- UX pattern: per-product Save (Today’s Prices interaction).

### 3 — Starting stock

- Never copy Main quantities.
- Options: start zero (default), transfer from branch, opening stock, receive later.
- Transfers use existing transfer domain.

### 4 — Customers

- Default: **no** existing customers exposed.
- Options: none / select / copy **access** from template / privileged org-wide policy.
- Wording: “Grant branch access,” not “migrate customers.”

### 5 — Suppliers

- Same as customers for access defaults and copy-access.

### 6 — Staff

- Reuse `organization_membership_branch_assignments`.
- No second ACL table.
- Owner/Admin implicit; normal staff explicit.
- Do not auto-assign all staff.
- Role = WHAT; branch assignment = WHERE.

### 7 — Devices / registers

- No device clone. Empty by default.
- Register/setup actions only if domain supports; no fake reassignment UI.

### 8 — Fulfillment

- Reuse Branch Fulfillment editor.
- Defaults: Customer ordering / Pickup / Delivery **OFF**.
- Link existing readiness (hours, location, policy, areas, entitlement).

### 9 — Review

Example Remote summary: products enabled/not offered, prices inherited vs overrides, stock plan, customers/suppliers accessible, staff, devices, fulfillment readiness. Finish must not fabricate readiness.

---

## 5. New branch owner acceptance — Remote North

| Step | Action | Expectation |
|------|--------|-------------|
| Products | Uncheck Ice Cream | Not offered |
| Prices | Coke 50→65; Rice inherit 340 | Override only Coke |
| Stock | Transfer Coke 20; Rice zero | No silent copy |
| Customers | Default none; optional Maria | No leak |
| Suppliers | Select ABC | Access only |
| Staff | Assign 2 | Explicit |
| Devices | None | Empty |
| Fulfillment | Off | Off |
| Finish | Complete | Other branches unchanged |

---

## 6. Acceptance IDs

| ID | Expectation |
|----|-------------|
| WIZARD-01 | Unselected product not offered |
| WIZARD-02 | Stock starts zero unless transfer/opening |
| WIZARD-03 | No customer/supplier access by default |
| WIZARD-04 | Resume mid-setup without data loss |
| WIZARD-05 | Finish does not enable fulfillment without readiness |
