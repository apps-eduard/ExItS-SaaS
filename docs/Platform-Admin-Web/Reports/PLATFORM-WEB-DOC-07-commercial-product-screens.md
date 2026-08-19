# PLATFORM-WEB-DOC-07 Report — Product + Commercial Administration Screen Specifications

**Date:** 2026-08-19  
**Branch:** `docs/platform-admin-web-v2`  
**Status:** Complete  
**Type:** Documentation only — no implementation

---

## Delivered

1. **Screens/commercial-and-product-screens.md** — screen specifications for:
   - A) Product Catalog
   - B) Product Detail
   - C) Plans / Pricing Administration
   - D) Subscriptions Administration
   - E) Entitlements Administration
   - F) Billing / Invoice Administration
   - G) Usage / Metering Administration (future capability)

2. **Capability requirement IDs** — 25 stable `PWEB-CAP-*` identifiers covering product catalog, plans, trials, subscriptions, entitlements, billing, usage, and personal features. Backend availability is not claimed; DOC-09 will verify.

3. **Money ownership boundary** — §0 repeats the authoritative boundary. Each billing/usage screen explicitly excludes POS operational money and PLM operational money.

4. **High-risk action UX** — documented for subscription state changes, plan changes, entitlement overrides, manual payment recording, payment confirmation/rejection/voiding, and usage corrections.

5. **PLM usage contract concepts** — LOAN_DISBURSED and LOAN_DISBURSEMENT_REVERSED documented as future Platform usage billable events per PLM product definition. Transport remains D-P12-03 (unresolved). Pre-release cancellation explicitly excluded.

---

## Alignment

| Source | Alignment |
|---|---|
| Product Foundation §5/§6 | Independent subscriptions, financial boundary, no product writes Platform billing |
| Unified control-plane model | Product usage signals via approved contracts only |
| DOC-02 IA | Commercial group placement consistent |
| DOC-05 shell | Standard page templates applied |
| DOC-06 template | Same screen-spec structure and capability ID pattern |
| PLM product definition | LOAN_DISBURSED referenced; PLM rules not duplicated |

---

## Exclusions

- No implementation, no API additions, no migrations.
- No POS/PLM business logic.
- No billing gateway or payment processor integration.
- No D-P12-03 usage transport design.

---

## Files changed

- `docs/Platform-Admin-Web/Screens/commercial-and-product-screens.md` — new
- `docs/Platform-Admin-Web/Reports/PLATFORM-WEB-DOC-07-commercial-product-screens.md` — new
- `docs/Platform-Admin-Web/README.md` — updated
- `docs/Platform-Admin-Web/documentation-status.md` — updated
- `docs/Platform-Admin-Web/decisions.md` — updated
