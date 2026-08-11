# Reset products and business templates (Local Validation)

**Local Validation only. Not Production.**  
There is **no separate** “wipe products only” script. Merchant products, POS data, and Admin-created **business templates** are cleared by the same destructive Local Validation volume reset that leaves **2 Platform users**.

## Command

From the repository root:

```powershell
cd C:\Users\speed\Desktop\ExItS-SaaS
.\tools\Reset-LocalValidation.ps1 -ConfirmReset
```

## Cleared

| Area | Result |
|---|---|
| POS DB (`exits_pos`) | Empty → migrate on next start |
| Organizations / memberships / invites | Cleared |
| Business templates (`catalog.catalog_templates` and related test rows) | Cleared via volume wipe |
| Extra Platform users | Cleared |
| Seed Platform admins | Recreated: Olivia + Rafael only |

## Retained / reseeded

| Area | Result |
|---|---|
| Platform SaaS catalog products / plans / features | Recreated by migrate + seed |
| Built-in Platform roles | Recreated by migrate + seed |
| Philippine default Business Types + starter templates (WP10A) | Recreated by Local Validation seed (`EnsurePhilippinePosStarterCatalog`) after migrate/start |

## Development disposable reset (optional)

`scripts/dev/Reset-DisposableCustomerData.ps1` preserves `catalog.business_types` definitions and deletes disposable global catalog merchandise (categories/products/templates). After that script:

1. Restart Local Validation / Platform API with seeding enabled.
2. `EnsurePhilippinePosStarterCatalog` re-creates starter categories, products, and the 16 optional `*-starter` templates without duplicating Business Type codes.

Do **not** run disposable reset casually against shared or production databases.

## Ordinary Start does **not** wipe data

```powershell
.\tools\Start-LocalValidation.ps1 -PublicHost 100.120.79.81
```

Preserves volumes. Use Reset when you intentionally want a clean test database.

## Related

- [Reset-LocalValidation.md](Reset-LocalValidation.md) — full reset details + verification
- [Start-LocalValidation.md](Start-LocalValidation.md)
