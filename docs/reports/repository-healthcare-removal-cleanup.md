# Repository HealthCare Removal Cleanup

| Field | Value |
|---|---|
| Status | **Complete** |
| Date | 2026-08-11 |
| WP11 | **Not started** |
| Device Verified | **No** |
| Production Ready | **No** |

## Goal

Remove obsolete HealthCare product references so ExItS Platform / Pinoy Business POS is the clear active repository direction, without breaking portfolio-independence safety guards or migration history.

## What was found

~200 paths mentioned HealthCare/healthcare historically, spanning:

1. Unused Platform Integration stubs under `Integration/HealthCare/`
2. HC-titled assessment/reuse/ADR/report docs
3. Admin UI warning strings naming HealthCare
4. Second-product test fixtures using code `healthcare`
5. Architecture/deployment guards that **forbid** nesting or targeting HealthCare (keep)
6. Historical signed-off phase reports describing early extraction context

## Code removed

| Path | Reason |
|---|---|
| `src/Platform/.../Integration/HealthCare/HealthCareIntegrationAbstractions.cs` | Unused stub delivery/reconciliation contracts; no DI |
| `src/Platform/.../Integration/HealthCare/RequestProjectionReconciliation.cs` | Unused stub use case; only contract tests |

## Code rewritten (product-neutral)

| Path | Change |
|---|---|
| `ProductCode.cs` | Removed `HealthCare` constant |
| `OrganizationMappingProjection.cs` | Any product code allowed; XML docs neutralized |
| `MigrationValidationServices.cs` | Removed healthcare-only product gate |
| `MigrationValidationModels.cs` | Comment neutralized |
| Contract projection XML docs | Product-facing wording |
| `ProductAccessAssignment.cs` | Drop HealthCare/Doctor wording |
| POS domain `*Id.cs` comments | “Not a Platform identity” |
| Admin `AdminResources.resx` + `fil-PH` | Subscription/Payments/Entitlements/Org warnings product-neutral |
| `.cursor/rules/exits-workflow.mdc` | Drop Integration/HealthCare tracked-contract rule; keep no nested product tree |
| `.cursor/rules/exits-product-context.mdc` | Drop Integration/HealthCare exception |
| `.gitignore` | Ensure `/HealthCare/` ignore |

## Docs removed

- `docs/reuse/healthcare-reuse-assessment.md`
- `docs/reuse/healthcare-runtime-baseline.md`
- `docs/reuse/healthcare-ui-reuse-assessment.md`
- `docs/phases/phase-00-healthcare-assessment.md`
- `docs/reports/P0-WP01-healthcare-reuse-assessment.md`
- `docs/reports/P2-WP04-healthcare-contract-adaptation.md`
- `docs/reports/P10-WP02-healthcare-workspace-cleanup.md`
- `docs/decisions/ADR-013-build-new-platform-before-healthcare-reconnection.md`

Inbound links updated in `docs/index.md`, `docs/reports/README.md`, `docs/decisions/README.md`, `docs/phases/README.md`, `FILE-MANIFEST.md`, `README.md`, `docs/portfolio-progress.md`, and related active engineering docs.

Historical mixed reports: freeze boilerplate softened (“HealthCare remains frozen” → portfolio independence wording) without mass-deleting signed-off evidence packs.

## UI wording changed

| Key | New intent |
|---|---|
| `Subscriptions_Warning` | Commercial eligibility only; no product-local roles |
| `Payments_DetailWarning` | No gateway/card/invoice behavior |
| `Entitlements_DetailWarning` | Snapshot ≠ product application delivery proof |
| Org membership / product-access hints | POS role examples only (Cashier/Store Manager) |

`Maui-Emulator-Install.md`: AVD example `HealthCare_Pixel_API34` → `ExItS_Pixel_API34`.

## Tests updated

- Removed Integration/HealthCare existence assertions
- Contract/migration validation tests product-agnostic
- Fixture product codes `healthcare` → `other-product` where used as a second catalog product
- Forbidden API probes `/api/v1/healthcare/entitlements` → `/api/v1/other-product/entitlements`
- Retained architecture `DoesNotContain("HealthCare")` independence guards

## References intentionally retained

| Reference | Reason |
|---|---|
| `DeploymentCore` / `HealthCareExclusion` | Block accidental deploy against HealthCare-named DBs |
| `RestoreValidator.EnsureNoHealthCareTablesAsync` | Reject restore payloads with HC-named schemas/tables (PHI safety) |
| `CommercialMvpCloseout.ForbidsHealthCareCoupling` | Explicit portfolio boundary flag |
| Architecture/deploy/compose/Maui `DoesNotContain("HealthCare")` tests | Prove nested HC product does not re-enter |
| `.dockerignore` `**/HealthCare/` | Prevent image context contamination |
| Historical ADR-010 / early phase reports that still mention HealthCare as past context | Signed-off history; indexes no longer promote deleted HC-titled docs |
| Deploy tests that assert rejection of `Database=HealthCare` | Guard realism |

## Migration impact

**None.** No EF migrations deleted or altered. No schema change.

## Tests / build results

| Suite | Result |
|---|---|
| Platform UnitTests | **727 passed** / 0 failed |
| Admin UI / Domain / Application source search | **zero** HealthCare matches |
| ArchitectureTests (RepositorySafety) | Blocked this run by local file lock (`ExItS.Platform.Api` PID holding Api `bin` outputs); prior focused safety filter passed earlier in cleanup |
| Admin project full rebuild | Blocked by running `ExItS.Platform.Admin` process lock on apphost (compile of Razor/sources succeeded before copy) |

WP11: **not started**

## Implementation commit hash

`<pending>`
