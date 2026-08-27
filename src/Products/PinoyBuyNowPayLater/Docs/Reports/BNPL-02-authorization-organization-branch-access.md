# BNPL-02 — Authorization + Organization/Branch Access Foundation

| Field | Value |
|---|---|
| Task | BNPL-02 |
| Branch | `feat/bnpl` |
| Status | Complete |
| Date | 2026-08-27 |
| Implementation present | Access boundary only (no financing domain / no DB) |

## Access formula

```text
authenticated actor
+ trusted organization membership
+ organization BNPL entitlement
+ actor BNPL product assignment
+ branch scope (when required)
+ BNPL capability (when required)
= ALLOW

else DENY (fail closed)
```

## Delivered

- `BnplCapabilityCodes` + `BnplCapabilityPresets` (bundles only; never authorize by preset name)
- `BnplOperationalAccessGuard` + `BnplAccessContext` + `BnplBranchScope`
- API: `AddBnplAccessBoundary()`, `GET /api/v1/bnpl/access/me`, fail-closed default provider
- Local Validation fixture catalog (`BnplLocalValidationAccessFixtures`) documenting Maria/Carlo vs Ana/Daniel
- Unit + API + architecture isolation tests

## Explicit exclusions

- No financing/installment/repayment/settlement entities
- No DbContext / migrations (BNPL-D-00-04 remains OPEN)
- No D-P12-03 session transport wiring (default context unavailable → 503)
- No Commerce / POS operational project references
- Capability facts are trusted-transport-supplied; not persisted in a BNPL grant table

## BNPL-D-00-18

**Provisionally Approved / Implemented in BNPL-02** — capability identifiers listed in Domain `BnplCapabilityCodes`.

## Next package

**BNPL-03 — Customer / reference foundation**
