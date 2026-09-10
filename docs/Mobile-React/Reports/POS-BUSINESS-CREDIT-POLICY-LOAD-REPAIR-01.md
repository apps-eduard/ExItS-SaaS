# POS-BUSINESS-CREDIT-POLICY-LOAD-REPAIR-01

## Status

**FIXED** — Business Customer → Credit & payment terms load failure caused by duplicate empty-path route registration (`AmbiguousMatchException` → HTTP 500).

## Root cause

`BusinessCustomerCreditPolicyEndpoints` (and `CreditPolicyEndpoints`) registered both `MapGet("", …)` / `MapGet("/", …)` and the same for PUT. ASP.NET Core collapses those templates to the same endpoint, so every GET/PUT to `/credit-policy` threw:

`Microsoft.AspNetCore.Routing.Matching.AmbiguousMatchException`

Observed against Local Validation POS API (`:8092`): empty `HTTP 500` body. Sibling routes (`/history`, `/approve`) returned normal `403` without auth.

## Fix

Register each leaf verb once (`MapGet("/")`, `MapPut("/")` only). Do not dual-map `""` and `"/"`.

Also hardened React query `enabled` (requires `connectionId`), DEV diagnostics for load failures, and targeted tests.

## Runtime evidence (pre/post)

| Check | Result |
| --- | --- |
| Pre | `GET …/business-customers/{id}/credit-policy` → **500** (empty body) |
| Post (Staging `:8092` restarted) | same URL without auth → **403** problem details (route resolves) |
| Kizy connectionId | `a2fc4070-b93f-4a01-8839-83374128852d` (seller Mica / buyer Kizy) |
| Authenticated GET (Testing host → LV DB 15534) | **200** `status: NotConfigured` |
| Migration `20260910180000_AddBusinessCustomerCreditPolicies` | already applied on LV `exits_pos` |
| Frontend path / connectionId | already correct (not buyerOrganizationId) |

## Explicit exclusions

- No POSCustomer stub
- No B2B Utang checkout/ledger
- No new credit-policy model

## Residuals

- Full React `tsc -b` / `npm run build` still fail on **pre-existing** inventory/shifts typing errors unrelated to this repair
- Vite React POS should hot-reload; POS API was restarted on `:8092` with the fixed Debug build
