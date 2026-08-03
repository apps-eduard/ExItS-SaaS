# P16-WP11 — SaaS payment linking and Admin Payments visibility

**Status:** Open (P16-WP11 In Progress)  
**Phase:** Phase 16 — Implementation Complete, Under Validation  
**Work package:** P16-WP11 — Validation, Stabilization, and User Acceptance  
**Date:** 2026-08-03
**Commit SHA:** `8855da8376e02129e2f415aaa318f117c0b8a1b2`  
**Related:** [P16-WP11-pricing-payments-and-subscription-changes.md](P16-WP11-pricing-payments-and-subscription-changes.md), [P16-WP11-admin-table-sizing-display-and-sorting.md](P16-WP11-admin-table-sizing-display-and-sorting.md)

## Title

Link successful provider charges into `saas_payments`, close unpaid paid-activation paths, and make Platform Admin Payments / tables show linked payments without overlap or misleading filters

## Root causes

1. **Two payment tables** — Local Validation / provider charge persisted `provider_payments` only. Admin Payments queries `saas_payments` → empty after successful Subscribe/PayNow.
2. **Trial ≠ payment** — Start Free Trial creates Trialing subscription with no charge; operators expected a Payments row.
3. **List filter vs summary** — Payments page defaulted Status to Pending Confirmation while summary showed Confirmed totals → “Confirmed 1” with empty table.
4. **Table overlap** — Global CSS forced Ant Design tables to `width: 100% !important`, defeating `ScrollX` and crushing columns.

## Fixes

| Area | Change |
|---|---|
| Domain | `SaaSPaymentMethod.Online`; `SaaSPayment.CreateConfirmedLinkedFromProvider` |
| Application | `RecordLinkedSuccessfulProviderPayment` after successful charges (Start Business PayNow, initial/renewal, commercial start, upgrade) |
| Paid activation | Require confirmed linked payment; reject bare paid activate / Admin paid create without `paymentId` |
| Catalog / orgs | Runtime HTTP POST create product/org forbidden outside Testing |
| Admin Payments | Default Status=Confirmed; clickable summary cards; clearer empty copy |
| Admin tables | `app.css` + `ScrollX` / fixed widths across list and nested tables; UI standards §12 |

## Explicit non-goals

- No production payment gateway, card capture, or GCash API verification
- No backfill of historical `provider_payments` into `saas_payments`
- No cardifying Ant Design tables on mobile in this pass

## Validation

- Unit: `SaaSPaymentTests`, focused `Wp11PricingPaymentsPlanChangeTests`
- Integration: lifecycle / entitlements / SaaS payments / catalog filters previously exercised under LV
- Manual: Platform Admin → Payments with Status=Confirmed after Personal Subscribe Now; Organization Staff table scrolls horizontally without header overlap

## Phase status

Phase 16 remains **Implementation Complete, Under Validation**.  
**P16-WP11 — In Progress.**  
**P16-WP12 — Not Started.**
