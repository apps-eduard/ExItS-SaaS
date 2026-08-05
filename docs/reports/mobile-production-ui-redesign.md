# Mobile Production UI Redesign

| Field | Value |
|---|---|
| Status | **In progress** — foundation + cashier reference shipped; other surfaces incremental |
| Phase 19 | **Open** |
| Phase 20 | **Open** |
| Device Verified | **No** |
| Production Ready | **No** |
| UI scenarios | **Retest** until physical-phone approval |
| Date | 2026-08-05 |
| Feature commits | `ba1db38` (design-system tokens/stepper), `9b77de5` (sell-floor reference) |

## 1. Summary

Established a production-oriented mobile visual language on the existing ExItS Design System (no second system). Raised touch targets to **48dp**, added semantic typography tokens and `QuantityStepper`, introduced `--pos-*` aliases, and rebuilt the cashier sell floor (`/sales/new`) as the reference layout (portrait sheet + landscape three-pane).

## 2. Audit findings (pre-change)

| Area | Finding |
|---|---|
| Sell UX | Single vertical scroll; weak hierarchy; cart mixed with browse |
| Touch | `--exits-touch-target-min: 2.75rem` (44px) below 48dp target |
| Type | Scale present but semantic roles incomplete; inconsistent page sizes |
| Surfaces | `--exits-surface-muted` referenced without definition |
| Cart qty | Already in-memory (good); UI did not show steppers on product tiles |
| Framework risk | None — stay on Design System |

## 3. Delivered

### Design System

- IBM Plex Sans / Source Sans 3 stack confirmed
- Semantic type tokens: display, page title, section, body, compact, label, helper, button, monetary
- `--exits-surface-muted` (light + dark)
- Touch min **3rem**; density row heights aligned
- `QuantityStepper` + `.exds-qty-stepper*`
- `MoneyDisplay.Emphasized` → `.exds-money--display`

### POS MAUI

- `--pos-*` aliases in `app.css`
- Sell floor CSS: categories, products, cart, sticky bar, portrait sheet, landscape grid (~20/55/25)
- `SaleCheckout.razor` reference implementation
- `SaleCartPanel.razor` shared cart chrome
- Debounced search + stale-result discard; keyed product rows
- EN + fil-PH strings for cart sheet / stepper / product aria

### Tests / docs

- Design System + Accessibility architecture asserts updated for 3rem / typography / stepper
- `MobileProductionUiGuardTests` for tokens, layout, cart persistence, a11y markers, no API on qty
- Spec: [production-mobile-design-system.md](../specs/mobile/production-mobile-design-system.md)

## 4. Explicit non-claims

- **Not Device Verified**
- **Not production-ready**
- Phase 19 / 20 remain **Open**
- Before/after screenshots pending physical-device capture (placeholder checklist below)
- Remaining personal/org/ops pages adopt tokens incrementally — not a broad rewrite in this pass

## 5. Phone Retest checklist (UI)

- [ ] Portrait 360 / 412: category chips, tiles, sticky View Cart, sheet, Checkout
- [ ] Landscape phone 640–740 and tablet: three panes scroll independently; cart not cleared by category
- [ ] ± quantity updates tile + cart without network spinner
- [ ] Dark / light, large system font, Filipino strings do not clip
- [ ] Focus rings / TalkBack labels on chips, tiles, steppers
- [ ] Card / GCash / cash payment section still primary commit path

## 6. Screenshot capture plan

Capture at 360px, 412px, landscape phone, and tablet for: sell floor empty, sell floor with cart, payment selected, electronic awaiting payment. Attach under this report when Device Validation proceeds.

## 7. Follow-up sequence

1. Checkout/payment visual polish (already partially aligned)
2. Catalog / customers / inventory list density
3. Personal Home / tabs / QR / org select
4. Empty / error / access-denied consistency pass
5. PhysicalDevice approval → update Device Verified only on user confirmation

## 8. Related reports

- [P19-WP08 checklist](P19-WP08-end-to-end-validation-and-closeout.md)
- [P20-WP08 checklist](P20-WP08-end-to-end-validation-and-user-closeout.md)
- [P19 Card/GCash simulation](P19-card-gcash-payment-ui-and-simulation.md)
- [P11-WP01 inventory](P11-WP01-web-ui-audit-and-component-inventory.md)
