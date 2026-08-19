# ADR-018 — Branch treasury, float acknowledgment, and UI sharing

**Status:** Accepted product policy (PLM-DOC-09); not implemented
**Date:** 2026-08-19

---

## Context

Branch treasury / cash vault architecture, collector float acknowledgment, and Web/MAUI component-sharing strategy were open (PLM-D-00-09 and operating-model gaps). Cashier Session and float flows existed as planning baselines without treasury funding model or two-step receipt.

---

## Decision

1. **Branch Treasury** — branch-level physical cash custody concept funds Cashier Session opening balances and receives returns per workflow ([../Product/branch-treasury-and-float-acknowledgment-policy.md](../Product/branch-treasury-and-float-acknowledgment-policy.md)).
2. **Float acknowledgment** — two-step workflow required: Cashier issue → **Pending Receipt** → Collector acknowledgment → **Received / Active** before collector expected cash increases.
3. **Web / MAUI sharing** — separate UI projects; share Domain/Application/Api/ApiClient; Web-only admin/report surfaces; conditional future RCL only when criteria met ([../Architecture/web-maui-component-sharing-policy.md](../Architecture/web-maui-component-sharing-policy.md)).
4. **PLM-D-00-09 Closed** — approved sharing/isolation approach recorded; client scaffold still gated by PLM-D-00-03.

---

## Consequences

Treasury and float accountability rules are approved for planning. Cashier/collector docs should reference Pending Receipt states. UI teams must not collapse Web and MAUI into one presentation project by default.

**Still open:** persistence schema, treasury GL projection (PLM-D-00-07 remainder), implementation, PLM-D-00-03 physical layout on mainline.

---

## Canonical documents

- [../Product/branch-treasury-and-float-acknowledgment-policy.md](../Product/branch-treasury-and-float-acknowledgment-policy.md)
- [../Architecture/web-maui-component-sharing-policy.md](../Architecture/web-maui-component-sharing-policy.md)
- [../Product/cashier-and-collector-control-model.md](../Product/cashier-and-collector-control-model.md)
