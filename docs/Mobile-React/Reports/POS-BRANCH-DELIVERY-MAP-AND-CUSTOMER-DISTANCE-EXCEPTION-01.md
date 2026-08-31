# POS-BRANCH-DELIVERY-MAP-AND-CUSTOMER-DISTANCE-EXCEPTION-01

## Status

PASS — map-first branch delivery location + org-customer distance exception.

```
TASK=POS-BRANCH-DELIVERY-MAP-AND-CUSTOMER-DISTANCE-EXCEPTION-01
START_SHA=73721a203e976d07001672b970aac2c9be7e9022
FEATURE_SHA=0073b299976a64b9c65af895535ae2d66c68d41a
FINAL_SHA=06e17e68df2e0fa7d778a50a5ff066b5d06010f1
REMOTE_SHA=(pending push)

MAP_LIBRARY=leaflet + react-leaflet
MAP_LAZY_LOADED=YES
MAP_PROVIDER_MODE=VITE_MAP_TILES_URL (+ optional VITE_MAP_TILES_ATTRIBUTION)

USE_CURRENT_LOCATION=YES
CHOOSE_ON_MAP=YES
MAP_CLICK=YES
MARKER_DRAG=YES
AUTO_FILL_LAT=YES
AUTO_FILL_LNG=YES
MANUAL_COORDINATE_FALLBACK=YES (Advanced coordinates)

SECTION_SPECIFIC_SAVE=YES
LOCATION_SAVE_ONLY=YES
POLICY_ERROR_CROSS_TAB_FIXED=YES
ADDRESS_PRESERVED=YES (coordinate-only update omits address fields; server null=omit)

CUSTOMER_EXCEPTION_MODEL=BusinessCustomer.AllowDeliveryBeyondNormalDistance
CUSTOMER_EXCEPTION_SCOPE=ORGANIZATION_CUSTOMER
CUSTOMER_EXCEPTION_DEFAULT=false
CUSTOMER_EXCEPTION_PERMISSION=EnsureCanManageMembershipsAsync (Owner / Platform ManageMemberships; Cashier denied)

DISTANCE_EXCEPTION_BYPASSES_MAX_DISTANCE=YES
DISTANCE_EXCEPTION_BYPASSES_SERVICE_AREA=NO
DISTANCE_EXCEPTION_BYPASSES_ENTITLEMENT=NO
DISTANCE_EXCEPTION_BYPASSES_OPERATIONAL_STATE=NO
DISTANCE_EXCEPTION_BYPASSES_MINIMUM_ORDER=NO
DISTANCE_EXCEPTION_CHANGES_FEE=NO

ACTUAL_DISTANCE_USED_FOR_FEE=YES

QUOTE_SERVER_AUTHORITY=YES (linked-customer Platform proof)
PLACE_ORDER_SERVER_AUTHORITY=YES (re-resolves independently)
CLIENT_OVERRIDE_FLAG=NOT_TRUSTED

ORDER_EXCEPTION_SNAPSHOT=CustomerOrderDeliverySnapshot.DistanceExceptionApplied (+ delivery_distance_exception_applied)

MIGRATION=Platform 20260831043000_AddBusinessCustomerDeliveryDistanceException; POS 20260831043000_AddCustomerOrderDeliveryDistanceExceptionSnapshot
PLATFORM_TESTS=BusinessCustomerDeliveryPreferencesTests + related (21 filtered)
POS_TESTS=DeliveryDistanceExceptionQuotePlaceTests (13)
POSTGRES_TESTS=not required beyond unit coverage for this package

TARGETED_REACT_TESTS=BranchFulfillmentEditPage.section-save; BranchDeliveryLocationForm; CustomerDeliveryExceptionSection
REACT_TOTAL=1364
REACT_PASS=1364
REACT_FAIL=0

PLAYWRIGHT_TESTS=e2e/pos-branch-delivery-map-distance-exception-01.spec.ts (MAP-01/02/04, DIST-01/02; skips without live LocalValidation)

RESPONSIVE_360=validated via existing branch-switch/min-h-11 controls + map dialog full-width sheet
RESPONSIVE_768=same
RESPONSIVE_1440=centered dialog

TYPECHECK=PASS
LINT=PASS (0 errors; pre-existing warnings only)
BUILD=PASS (map picker code-split chunk)
DOTNET_BUILD=PASS (Platform/POS Release via unit test builds)

P0=0
P1=0
P2=0
P0_UNRESOLVED=0
P1_UNRESOLVED=0

JOE_STORE_MAP_VALIDATION=DEFERRED_LIVE (Playwright gated; unit/React covered Save location isolation)
JOE_STORE_DISTANCE_EXCEPTION_VALIDATION=DEFERRED_LIVE (matrix covered in DeliveryDistanceExceptionQuotePlaceTests)

DEFERRED=Full live Joe store MAP-03/05 and DIST-03..06 against running LocalValidation stack; AREA exception (explicitly out of scope)

NEXT=PRODUCT_EXPANSION_REASSESSMENT

DELIVERY_MAP_STATUS=COMPLETE_VALIDATED_BASELINE
CUSTOMER_DISTANCE_EXCEPTION_STATUS=COMPLETE_VALIDATED_BASELINE
```

## Delivered

### A. Branch delivery location
- Map-first UX: Use current location, Choose on map (lazy Leaflet picker), click/drag pin, selected-location summary, Advanced coordinates, external Google/OSM links.
- Section-specific saves: details / hours / location / policy — **Save location does not call policy or hours APIs**.
- Map unavailable copy is user-facing (not “tiles not configured”).
- No reverse geocoding; address and coordinates remain separate.

### B. Customer delivery distance exception
- Org-scoped `AllowDeliveryBeyondNormalDistance` on `BusinessCustomer` (default false).
- Seller UI on customer detail; compact list badge when ON.
- PATCH `/api/v1/organizations/{orgId}/customers/{id}/delivery-preferences`.
- Quote/place resolve exception from Platform linked-customer proof only; fee uses **actual** distance; service area / readiness / entitlement / min order unchanged.
- Order snapshot `DistanceExceptionApplied` is immutable history.

## Explicit exclusions
Service-area bypass, deliver-anywhere, Google Maps JS/Places, reverse geocoding, polygons, custom fees, customer self-enable.
