# POS-REACT-CHECKOUT-RECORD-SALE-FIX01

## Status

Fixed and pushed on `feat/pos-react-client`.

## Symptom

After device-auth pause, React checkout showed **“Could not record the sale. Try again.”** for a valid Utang cart (Kangkong/Pechay/Okra, PHP 205).

## Root cause

**SUCCESS_RESPONSE_PARSE_FAILURE**

Server `SaleQueryService.Map` returns `PriceOverrides = null` when none applied. ASP.NET serializes that as `"priceOverrides": null`. React `posSaleDtoSchema` used `z.array(...).optional()`, which rejects JSON `null` (quote schema already allowed null). `parseSale` threw `ZodError`; `describeCheckoutSaleError` mapped non-`PosApiError` to the generic message.

The sale **was** persisted: `SALE-20260825-000001` / Utang / 205.00 with linked credit entry.

## Fix

- `priceOverrides: z.array(...).nullable().optional()` on `posSaleDtoSchema`
- Dev console diagnostics for Zod contract mismatches and non-PosApiError checkout failures
- Regression test with real Map()-shaped body including `priceOverrides: null`

## Non-changes

Device enforcement Local Validation remains paused; Production default unchanged. No auth/shift/capability bypass.
