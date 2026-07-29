# Entitlement Projection State Matrix

[Contracts](platform-product-contracts.md) | [Subscriptions](../product/subscriptions-and-billing.md) | [ADR-012](../decisions/ADR-012-versioned-platform-contracts-and-local-projections.md)

Exact refresh/stale **durations are not fixed** in P1-WP02 (R-022). Behavior is categorical.

**P2-WP03:** Authoritative `EntitlementSnapshot` + `EntitlementSnapshotComposer` exist in Platform Domain/Application. Product-local projection states (Never initialized, Invalid, Unsupported version, etc.) remain product concerns. Snapshot composition encodes Trialing/Active/Grace/Suspended/Cancelled/Expired commercial feature grants; RefreshBy is set with a default 24h window pending R-022 numeric policy.

**Trial length:** PinoyBusinessPOS Utang trial product requirement is **three calendar months** (not 90 days). Platform Domain stores only a configured positive duration / resulting UTC trial end on the subscription; calendar-month computation is not implemented in the generic aggregate.

**P3-WP02:** Subscription lifecycle states (Trialing/Active/GracePeriod/PastDue/Suspended/Cancelled/Expired) are now **persisted** with one active-like slot per organization+product. Entitlement **projection delivery** remains out of scope.

| Projection State | Trusted? | Reads Allowed | Writes Allowed | Restricted Operations | Refresh Action | Audit Requirement |
|---|---|---|---|---|---|---|
| Current | Yes | Yes | Per entitlements | None beyond normal authz | Background refresh before RefreshBy | Normal product audit |
| Refresh due | Yes | Yes | Per entitlements | Prefer refresh before risky admin changes | Schedule/perform refresh | Log refresh attempt |
| Temporarily stale | Conditional | Yes (incl. balances/history) | Only ops allowed under last trusted snapshot + stale policy | Block ops requiring fresher commercial truth if policy says so | Attempt refresh when Platform reachable | Log stale use + reason |
| Grace period | Yes (grace facts) | Yes | Per grace entitlements | Features outside grace | Refresh; surface billing UX | Log grace enforcement |
| Suspended | Status trusted | Limited / historical | Block entitlement-protected writes | New credit, new paid features, admin expansions | Refresh; allow recovery paths | Mandatory on blocked attempts |
| Expired | Status trusted | Historical + existing debt views | Cash/GCash payments on **existing** debt; block new credit | New credit/debt; non-allowed features | Refresh; upgrade/renew | Mandatory on blocked credit |
| Invalid | No | Safe metadata only | No protected writes | All entitlement-gated features | Reconcile / re-init | Alert + audit |
| Unsupported version | No | Safe metadata only | No unknown/paid features | Anything needing schema understanding | Upgrade consumer or reconcil. | Alert administrators |
| Reconciliation required | No / partial | Prefer read-only commercial views | Block risky commercial writes | Entitlement-gated expansions | Pull authoritative snapshot | Audit reconcil. actor/reason |
| Never initialized | No | No paid-feature UX | **No** paid features by default | All commercial features | Initialize from Platform | Audit first init |

## Fail-open vs fail-closed (by category)

| Category | Default |
|---|---|
| Financial writes (new credit, sales gated by plan, refunds) | **Fail closed** |
| Privacy / clinical / admin elevation | **Fail closed** |
| View existing customers, balances, history (within last trusted snapshot) | May continue under Temporarily stale / Expired read rules |
| Platform completely unreachable + Never initialized | **Fail closed** (no invented Active) |
