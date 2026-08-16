# Platform-Controlled Organization Tax Configuration

## Status

Delivered as a Phase 26 technical control improvement. **Phase 26 remains OPEN — Owner Validation Pending.** This is not regulatory certification and does not close Phase 21, 25, or 26.

## Canonical rule

Tax configuration is **hidden by default**.

```
Compliance eligibility == Approved
AND
Platform TaxConfigurationEnabled == true
=
Organization may view/edit Tax Configuration (and new checkouts may apply tax)
```

Three distinct concepts:

| Concept | Owner | Meaning |
|---|---|---|
| A. Compliance eligibility/readiness | Platform | Review lifecycle (`NotRequested` … `Approved` …) |
| B. TaxConfigurationEnabled | Platform Admin only | Product authorization to configure/apply tax |
| C. TaxRatePercent / TaxPricingMode | POS OperationalSetup | Stored calculation values |

Tax values do **not** prove compliance. Eligibility alone does **not** enable tax settings. Enablement is **not** BIR/NPC/government certification.

## Capability

- Name: `TaxConfigurationEnabled` on `OrganizationSalesDocumentCapability`
- Default: `false` (no blanket backfill)
- Enable only when `ComplianceEligibilityStatus == Approved`
- Leaving Approved (reject/suspend/revoke/…) forces `TaxConfigurationEnabled = false`
- Audit: `platform.organization.tax_configuration_capability_enabled|disabled`
- API: `POST /api/v1/platform/organizations/{id}/compliance/tax-configuration-capability`
- Admin UI: Organizations → Compliance → Tax configuration card

## POS behavior

- Operational Setup / Org Web Settings: Tax section rendered only when enabled
- Currency remains always available
- Write endpoints reject tax changes when disabled (`pos.operational_setup.tax_configuration_not_enabled`)
- Checkout applies tax only when capability enabled; stored rates preserved when disabled
- Historical sale tax snapshots unchanged when capability later disabled
- Offline: operational/tax capability is not in LocalStore; after config refresh, disabled capability stops applying tax (fail-closed if Platform unread)

## Migration

`20260816120000_AddOrganizationTaxConfigurationEnabled` — `tax_configuration_enabled boolean NOT NULL DEFAULT false`

LocalStore version: unchanged.
