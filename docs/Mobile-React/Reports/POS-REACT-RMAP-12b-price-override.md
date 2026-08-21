# RMAP-12b — Transaction price override UX (React)

## Status

**COMPLETE** (React UI wired to RMAP-B01 quote/checkout `PriceOverrides`). Do **not** start RMAP-B04 / B05 / RMAP-21 / RMAP-TAX from this package.

## Locked policy (UI)

| Principal | UI |
|-----------|-----|
| Cashier | No **Change price** action |
| Manager / StoreManager | Action available; `>100%` deviation → friendly denial (no silent clamp) |
| Owner / Admin (+ unlimited grant) | Unlimited **positive** selling prices |
| Zero | “Use a discount if you want to make this item free.” (all 5 locales) |
| Wording | **Change price** / **Change selling price** — never capability enum names in UI |
| Experience ≠ authority | Session capability / role grant gates UI; server remains authoritative |
| Separate concepts | Override ≠ Today's Price ≠ Commercial Discount |
| ManualGCash | Never shown in UI |

## Contract

| Surface | Behavior |
|---------|----------|
| Intent | `SalePriceOverrideIntentRequest`: `requestedUnitPrice`, `reason`, optional `lineNumber` / `productId` / `expectedBaselineUnitPrice` |
| Quote | `POST .../sales/quote` returns `priceOverrides[]` + per-line `baselineUnitPrice` when applied |
| Checkout | Same intents on `CheckoutSaleRequest.PriceOverrides` |
| Sale GET | Additive `priceOverrides[]` on `PosSaleDto` for summary display (regular / selling / reason) |
| Order | Baseline → override UnitPrice → commercial discount → Amount to Pay |

## Implementation

- `pos-capabilities.ts` — `canOverrideSalePrice` / `canOverrideSalePriceUnlimited` (prefer `featureCodes` / `grantedFeatureCodes` when present; else PosRoleMatrix)
- `pos-sales-client.ts` — quote/checkout `priceOverrides`; parse baseline/applied
- Cart — pending override on `SessionCartLine`; **Change price** dialog; **Price changed** + **Regular price**; **Use regular price** clears pending intent
- Checkout — sends overrides with discounts; Amount to Pay from authoritative quote; note separating override vs discount
- Transaction Summary — shows price changed / regular / selling / reason when API returns overrides
- i18n — en + fil-PH + ceb-PH + hil-PH + ilo-PH
- Vitest + Playwright `e2e/rmap-12b-price-override.spec.ts` (scenarios A–L)

## Explicit exclusions

- RMAP-B04 / B05 / RMAP-21 offline outbox / RMAP-TAX
- Promotions / regulatory Senior/PWD
- Catalog / Today's Price mutation via override
- Card / provider GCash UX

## Validation

Run from Client project:

- `npm run format`
- `npm run typecheck`
- focused Vitest (capabilities, sales client, override map, checkout errors)
- `npx playwright test e2e/rmap-12b-price-override.spec.ts`

## Docs touched

- This report
- Roadmap RMAP-12b status brief update
- Capability / pricing authoritative notes as needed
