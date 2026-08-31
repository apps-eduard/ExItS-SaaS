# POS-PH-PSGC-DELIVERY-AREAS-01

## Status

`PSGC_DELIVERY_AREAS_STATUS=COMPLETE_VALIDATED_BASELINE`

`NEXT=PRODUCT_EXPANSION_REASSESSMENT`

## Git

| Field | Value |
| --- | --- |
| TASK | POS-PH-PSGC-DELIVERY-AREAS-01 |
| BRANCH | feat/organization |
| START_SHA | 0b5872fc970ce177abc1262de521a1043c2d39c8 |
| FEATURE_SHA | *(product commit tip — see push evidence)* |
| FINAL_SHA | *(branch tip after docs alignment)* |
| REMOTE_SHA | *(must equal LOCAL tip after push)* |

## PSGC dataset

| Field | Value |
| --- | --- |
| PSGC_SOURCE | Philippine Statistics Authority — Philippine Standard Geographic Code |
| PSGC_DATASET_VERSION | PSGC-2026-06-30 |
| PSGC_AS_OF_DATE | 2026-06-30 |
| PSGC_RELEASE | 2Q 2026 |
| PSGC_RUNTIME_EXTERNAL_DEPENDENCY | NO |
| PSGC_DIRECTORY | `src/Platform/ExItS.Platform.Infrastructure/ReferenceData/Philippines/psgc-localities-2026-06-30.json` |
| PSGC_SEARCH_API | `GET /api/v1/platform/reference/ph/localities?query=&limit=` |
| RECORD_COUNT | 1642 (City 149 + Municipality 1493) |
| GENERATOR | `tools/Generate-PsgcLocalitiesSnapshot.R` + `tools/Convert-PsgcLocalitiesCsvToJson.py` |

Official `psa.gov.ph` publication download was blocked by Cloudflare in this environment. Generation used the PSA-sourced **Q2_2026** release table bundled in the CRAN/GitHub `psgc` package (Windows binary), then filtered to City/Municipality only. Runtime never calls PSA.

## Product rules

| Field | Value |
| --- | --- |
| COUNTRY_MODE | PH_ONLY |
| COUNTRY_EDITABLE | NO |
| LOCALITY_TYPES | CITY\|MUNICIPALITY |
| PROVINCE_REQUIRED | NO |
| FREE_TEXT_CITY_REMOVED | YES |
| FREE_TEXT_PROVINCE_REMOVED | YES |
| FREE_TEXT_COUNTRY_REMOVED | YES |
| MULTI_SELECT | YES (search → click → chip) |
| CHECKBOX_LIST | NO |
| SELECTED_CHIPS | YES |
| SERVICE_AREA_IDENTITY | PSGC_CODE (`PsgcCode`; DB column `external_area_code`) |
| DUPLICATE_IDENTITY | BRANCH_PLUS_PSGC_CODE (filtered unique active) |
| LEGACY_AREA_POLICY | Keep rows; `IsVerified=false` when code null/unresolved; UI “Needs verification” + Replace |
| VERIFIED_AREA_REQUIRED_FOR_DELIVERY_READY | YES (≥1 active directory-resolved PSGC area) |
| MIGRATION | `20260831012000_RefineBranchDeliveryServiceAreaPsgcUniqueness` |
| CITY_COORDINATE_GEOMETRIC_VALIDATION | DEFERRED |

## Evidence

| Check | Result |
| --- | --- |
| PLATFORM_TESTS | PASS (directory + domain + readiness filter focused; 19 targeted) |
| POSTGRES_TESTS | Migration applied to LocalValidation platform DB (`15533`) |
| POS_TESTS | Storefront contract unchanged (`deliveryServiceAreaId` + distance) |
| TARGETED_REACT_TESTS | `BranchDeliveryAreasPanel.test.tsx` + fulfillment client PASS |
| REACT_TOTAL | 1358 |
| REACT_PASS | 1358 |
| REACT_FAIL | 0 |
| TYPECHECK | PASS |
| LINT | PASS (0 errors; existing warnings only) |
| BUILD | PASS (`npm run build`) |
| DOTNET_BUILD | PASS (Platform Api/Infrastructure Release + Debug) |
| PLAYWRIGHT_TESTS | Specs updated (`pos-ph-psgc-delivery-areas-01.spec.ts` + FUL-UI-04 PSGC flow); live run depends on restored Joe store pilot |

### Joe store (controlled validation)

During package validation, Platform API accepted:

- deactivate legacy free-text Bacolod
- `POST .../delivery-service-areas` `{ "psgcCode": "1830200000" }`
- resulting readiness: `deliveryReady=true`, `deliveryAreasComplete=true`, `5/5`

A later LocalValidation API restart left `platform.organizations` empty (admin seed only), so live Playwright against Joe store requires re-running the existing pilot recreate tools (not edited in this package).

`JOE_STORE_PSGC_AREA=City of Bacolod / 1830200000 (API-proven before env wipe)`  
`DELIVERY_READY_AFTER_PSGC=true (API-proven before env wipe)`

## Architecture

```
PSA PSGC 2Q 2026
  → generator (dev-time)
  → versioned JSON snapshot (embedded)
  → IPhilippineLocalityDirectory (in-memory)
  → GET /api/v1/platform/reference/ph/localities  (Organization account class)
  → React searchable multi-select chips
  → POST delivery-service-areas { psgcCode } (server resolves names/PH)
```

Checkout still selects only merchant-configured areas; PSGC does not replace distance validation.

## P0 / P1 / P2

| Severity | Unresolved |
| --- | --- |
| P0 | 0 |
| P1 | 0 |
| P2 | Live Joe store Playwright re-run after pilot recreate (env wipe) |

## Deferred

- Barangays, polygons/GIS/geofencing, live PSA sync, Mapbox/Google, zone pricing, customer saved addresses

## Master ledger

Branch Fulfillment closed baseline remains closed except Delivery Area authority/UI intentionally changed by this package. Unaffected Pickup / hours / location / policy / pause evidence reused.
