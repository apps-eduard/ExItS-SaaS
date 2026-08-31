# Branch Customer and Supplier Access

**Program:** POS-MULTI-BRANCH-COMMERCE-V2
**Status:** TARGET_LOCKED (MB2-00)
**Parent:** [multi-branch-commerce-v2.md](multi-branch-commerce-v2.md)
**Implements in:** MB2-04

---

## 1. CURRENT_PROVEN

- Platform `BusinessCustomer` and POS `POSCustomer`: **organization-scoped**; no BranchId; no branch-access table.
- `Supplier`: organization-owned master; no branch-access table.
- WP12: org owns customer relationship; no per-branch cloned “Paul-Main / Paul-BranchA” rows.
- Staff branch ACL exists for **membership→branch**, not for customer/supplier visibility.

---

## 2. TARGET invariant — LOCKED

```
Organization-owned ≠ visible to every branch.
Canonical identity remains one record.
Visibility requires branch access (or org governance).
```

Do **not** clone customers or suppliers per branch.

---

## 3. Conceptual models

### BusinessCustomerBranchAccess (name illustrative)

| Field | Purpose |
|-------|---------|
| OrganizationId | Tenant |
| BranchId | Location |
| CustomerId / BusinessCustomerId | Canonical party |
| GrantedAtUtc / GrantedBy | Audit |
| Source | ExplicitAssign \| CreateAtBranch \| Transaction \| SetupCopy \| Policy |

**Uniqueness:** `(OrganizationId, BranchId, CustomerId)`.

### SupplierBranchAccess

Same pattern for `SupplierId`.

Owner/Admin: organization-wide governance visibility (may bypass access rows for management surfaces; still audit).

Normal branch staff: search/view/use **only** parties with access for acting branch.

---

## 4. Privacy invariants — LOCKED

Without access, APIs must not return:

name, mobile, email, address, Utang, purchase history, notes, other-branch transactions.

No reliance on React row hiding. Avoid leakage via suggestions, counts, exact-match errors, pickers, checkout, Utang screens.

**Identity access ≠ organization-wide transaction-history access.** After grant, define which history is visible (recommend: branch-attributed transactions by default; org-wide history Owner/Admin or explicit capability).

---

## 5. Access grant paths — TARGET

Legitimate sources:

- Owner/Admin explicit assign
- Authorized branch staff creates **new** customer/supplier at branch (grants that branch)
- Customer completes transaction/order with branch where policy establishes relationship
- Explicit copy-access during branch setup (access only — not balances/history clone)

Not automatic: expose all org customers/suppliers to every new branch.

---

## 6. Migration strategy — OPEN_DECISION (OD-02) with recommendation

Do **not** default to “grant all to every branch.”

**Recommended direction to validate on pilot data:**

1. Owner/Admin retain org-wide governance visibility.
2. Derive branch access from reliable branch-attributed sales/orders/receipts where possible.
3. Legacy records **without** reliable provenance → default access to **Primary/Main only**.
4. No destructive moves; owner expands access later.

If history cannot support inference safely → keep **OPEN_DECISION**; implement explicit Owner tools before silent fan-out.

---

## 7. Acceptance IDs

| ID | Expectation |
|----|-------------|
| PRIVACY-01 | Remote staff cannot search Main-only customer Maria |
| PRIVACY-02 | Remote staff cannot query Main-only supplier |
| PRIVACY-03 | Owner can see Maria |
| PRIVACY-04 | Granting Remote access does not copy Main purchase history by default |
| PRIVACY-05 | New branch wizard defaults to **no** customer/supplier access |

---

## 8. Joe Store scenario

- Maria: Main access only → Remote cashier denied.
- ABC Wholesale: Main + Remote → Remote purchasing may use ABC.
- Owner sees all.
