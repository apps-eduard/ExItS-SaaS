import { afterEach, describe, expect, it, vi } from "vitest";
import {
  addBranchDeliveryServiceArea,
  deleteBranchDeliveryServiceArea,
  listBranchDeliveryServiceAreas,
  listOrganizationBranchesForFulfillment,
  normalizeDeliveryServiceArea,
  normalizeFulfillmentReadiness,
  normalizeOrganizationBranch,
  updateBranchFulfillmentSettings,
  updateOrganizationBranch,
  upsertBranchDeliveryPolicy,
  upsertBranchOperatingHours,
} from "@/api/platform/branch-fulfillment-client";
import { canManageBranchFulfillment } from "@/access/pos-capabilities";
import {
  isMapProviderConfigured,
  isValidLatitude,
  isValidLongitude,
  parseOptionalCoordinatePair,
} from "@/features/branches/branch-coordinates";
import { hoursFromDto, hoursToRequest } from "@/features/branches/branch-hours";
import { externalMapLinks, requestGpsAssistOnce } from "@/features/branches/branch-map-links";
import {
  deliveryEnablementLabel,
  filterRedundantReasonCodes,
  missingRequirementMessageKey,
  pickupEnablementLabel,
} from "@/features/branches/branch-readiness-labels";
import { resolveFulfillmentToggle } from "@/features/branches/fulfillment-toggle";

afterEach(() => {
  vi.unstubAllGlobals();
  vi.restoreAllMocks();
});

describe("RMAP-18 branch coordinates", () => {
  it("accepts WGS84 bounds and rejects invalid values", () => {
    expect(isValidLatitude(14.5995)).toBe(true);
    expect(isValidLatitude(90)).toBe(true);
    expect(isValidLatitude(-90)).toBe(true);
    expect(isValidLatitude(91)).toBe(false);
    expect(isValidLongitude(120.9842)).toBe(true);
    expect(isValidLongitude(181)).toBe(false);
  });

  it("parses optional coordinate pairs for clear vs invalid", () => {
    expect(parseOptionalCoordinatePair("", "")).toEqual({
      ok: true,
      latitude: null,
      longitude: null,
      clearCoordinates: true,
    });
    expect(parseOptionalCoordinatePair("14.6", "")).toEqual({
      ok: false,
      error: "pair_incomplete",
    });
    expect(parseOptionalCoordinatePair("99", "120")).toEqual({
      ok: false,
      error: "invalid_latitude",
    });
    expect(parseOptionalCoordinatePair("14.6", "120.9")).toEqual({
      ok: true,
      latitude: 14.6,
      longitude: 120.9,
      clearCoordinates: false,
    });
  });

  it("falls back safely when map provider env is missing", () => {
    expect(isMapProviderConfigured({})).toBe(false);
    expect(
      isMapProviderConfigured({ VITE_MAP_TILES_URL: "https://tiles.example/{z}/{x}/{y}.png" }),
    ).toBe(true);
  });
});

describe("RMAP-18 map links + one-shot GPS", () => {
  it("builds external map links only for valid coordinates", () => {
    expect(externalMapLinks(null, null)).toBeNull();
    const links = externalMapLinks(14.6, 120.9);
    expect(links?.google).toContain("14.6%2C120.9");
    expect(links?.osm).toContain("mlat=14.6");
  });

  it("requests geolocation once without watchPosition", async () => {
    const getCurrentPosition = vi.fn((success: PositionCallback) => {
      success({
        coords: {
          latitude: 14.5,
          longitude: 121.0,
          accuracy: 10,
          altitude: null,
          altitudeAccuracy: null,
          heading: null,
          speed: null,
        },
        timestamp: Date.now(),
      } as GeolocationPosition);
    });
    const watchPosition = vi.fn();
    const result = await requestGpsAssistOnce({
      getCurrentPosition,
      watchPosition,
      clearWatch: vi.fn(),
    } as unknown as Geolocation);
    expect(result).toEqual({ ok: true, latitude: 14.5, longitude: 121.0 });
    expect(getCurrentPosition).toHaveBeenCalledTimes(1);
    expect(watchPosition).not.toHaveBeenCalled();
  });
});

describe("RMAP-18 readiness labels", () => {
  it("maps server missing codes without inventing rules", () => {
    expect(missingRequirementMessageKey("map_location")).toBe("branches.missing.mapLocation");
    expect(missingRequirementMessageKey("delivery_policy")).toBe("branches.missing.deliveryPolicy");
    expect(missingRequirementMessageKey("delivery_area")).toBe("branches.missing.deliveryArea");
    expect(pickupEnablementLabel({ pickupEnabled: true, pickupReady: true })).toBe("enabled");
    expect(pickupEnablementLabel({ pickupEnabled: false, pickupReady: false })).toBe("notReady");
    expect(deliveryEnablementLabel({ deliveryEnabled: false, deliveryReady: true })).toBe(
      "disabled",
    );
  });

  it("drops reason codes already covered by missing requirements", () => {
    expect(
      filterRedundantReasonCodes(
        ["timezone", "map_location", "branch_address", "delivery_area"],
        [
          "timezone_missing",
          "map_location_missing",
          "branch_address_incomplete",
          "delivery_area_missing",
          "pickup_disabled",
          "delivery_disabled",
          "customer_ordering_disabled",
        ],
      ),
    ).toEqual(["pickup_disabled", "delivery_disabled", "customer_ordering_disabled"]);
  });
});

describe("branch fulfillment toggles", () => {
  it("disables enabling pickup when not ready", () => {
    const decision = resolveFulfillmentToggle({
      channel: "pickup",
      enabled: false,
      ready: false,
      canUseDelivery: true,
    });
    expect(decision.disabled).toBe(true);
    expect(decision.enableBlocked).toBe(true);
    expect(decision.hintKey).toBe("branches.toggle.completeSetupFirst");
  });

  it("allows turning pickup off even when not ready", () => {
    const decision = resolveFulfillmentToggle({
      channel: "pickup",
      enabled: true,
      ready: false,
      canUseDelivery: true,
    });
    expect(decision.checked).toBe(true);
    expect(decision.disabled).toBe(false);
    expect(decision.enableBlocked).toBe(false);
  });

  it("blocks delivery enable when entitlement missing", () => {
    const decision = resolveFulfillmentToggle({
      channel: "delivery",
      enabled: false,
      ready: true,
      canUseDelivery: false,
    });
    expect(decision.disabled).toBe(true);
    expect(decision.hintKey).toBe("branches.toggle.deliveryNotInPlan");
  });

  it("allows enabling delivery when ready and entitled", () => {
    const decision = resolveFulfillmentToggle({
      channel: "delivery",
      enabled: false,
      ready: true,
      canUseDelivery: true,
    });
    expect(decision.disabled).toBe(false);
    expect(decision.enableBlocked).toBe(false);
    expect(decision.hintKey).toBeNull();
  });
});

describe("RMAP-18 hours mapping", () => {
  it("round-trips operating hours DTO", () => {
    const drafts = hoursFromDto([
      {
        dayOfWeek: "Monday",
        isClosed: false,
        isOpen24Hours: false,
        openTime: "09:00:00",
        closeTime: "18:00:00",
      },
    ]);
    const monday = drafts.find((d) => d.dayOfWeek === "Monday")!;
    expect(monday.openTime).toBe("09:00");
    expect(monday.isClosed).toBe(false);
    const request = hoursToRequest(drafts);
    expect(request.find((d) => d.dayOfWeek === "Monday")?.openTime).toBe("09:00");
  });
});

describe("RMAP-18 capabilities", () => {
  it("allows owner/admin and denies cashier/manager-only", () => {
    expect(
      canManageBranchFulfillment({
        productAccessAllowed: true,
        organizationManagementAuthority: true,
        membershipRole: "OrganizationOwner",
        mappedPosRoleCode: "Owner",
        productLocalRoleCode: "Owner",
      }),
    ).toBe(true);
    expect(
      canManageBranchFulfillment({
        productAccessAllowed: true,
        organizationManagementAuthority: false,
        membershipRole: "OrganizationMember",
        mappedPosRoleCode: "StoreManager",
        productLocalRoleCode: "Manager",
      }),
    ).toBe(false);
    expect(
      canManageBranchFulfillment({
        productAccessAllowed: true,
        organizationManagementAuthority: false,
        membershipRole: "OrganizationMember",
        mappedPosRoleCode: "Cashier",
        productLocalRoleCode: "Cashier",
      }),
    ).toBe(false);
  });
});

describe("RMAP-18 branch fulfillment client", () => {
  it("normalizes branch + readiness payloads including setup summary", () => {
    const branch = normalizeOrganizationBranch({
      Id: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      OrganizationId: "11111111-1111-1111-1111-111111111111",
      Code: "MAIN",
      Name: "Main",
      IsPrimary: true,
      Status: "Active",
      Latitude: 14.6,
      Longitude: 120.9,
      PickupEnabled: true,
      DeliveryEnabled: false,
      PickupReady: true,
      DeliveryReady: false,
      CanUseDelivery: true,
      BranchDetailsComplete: true,
      OperatingHoursComplete: true,
      DeliveryLocationComplete: true,
      DeliveryPolicyComplete: false,
      DeliveryAreasComplete: false,
      PickupSectionsComplete: 2,
      PickupSectionsTotal: 2,
      DeliverySectionsComplete: 3,
      DeliverySectionsTotal: 5,
      MissingRequirements: ["delivery_policy", "delivery_area"],
      ContactPhone: "09171234567",
      DeliveryPolicy: {
        BranchId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
        OrganizationId: "11111111-1111-1111-1111-111111111111",
        MinimumOrderAmount: 100,
        BaseDeliveryFee: 40,
        IncludedDistanceKm: 2,
        AdditionalFeePerKm: 10,
        MaximumDeliveryDistanceKm: 8,
        FreeDeliveryThreshold: null,
        CreatedAtUtc: "2026-08-01T00:00:00Z",
        UpdatedAtUtc: "2026-08-01T00:00:00Z",
      },
    });
    expect(branch.latitude).toBe(14.6);
    expect(branch.deliveryPolicy?.baseDeliveryFee).toBe(40);
    expect(branch.pickupReady).toBe(true);
    expect(branch.branchDetailsComplete).toBe(true);
    expect(branch.deliverySectionsComplete).toBe(3);
    expect(branch.missingRequirements).toEqual(["delivery_policy", "delivery_area"]);

    const readiness = normalizeFulfillmentReadiness({
      BranchId: branch.id,
      CanUseCustomerOrdering: true,
      CanUseDelivery: true,
      CustomerOrderingEnabled: false,
      PickupEnabled: false,
      DeliveryEnabled: false,
      OnlineOrdersPaused: false,
      CustomerOrderingReady: false,
      PickupReady: false,
      DeliveryReady: false,
      CustomerOrderingOperational: false,
      PickupOperational: false,
      DeliveryOperational: false,
      MissingRequirements: ["branch_address", "map_location", "delivery_area"],
      ReasonCodes: ["branch_address_incomplete", "delivery_area_missing"],
      StoreIsOpenNow: false,
      BranchDetailsComplete: false,
      DeliveryAreasComplete: false,
      PickupSectionsComplete: 0,
      PickupSectionsTotal: 2,
      DeliverySectionsComplete: 0,
      DeliverySectionsTotal: 5,
    });
    expect(readiness.missingRequirements).toEqual([
      "branch_address",
      "map_location",
      "delivery_area",
    ]);
    expect(readiness.deliveryAreasComplete).toBe(false);
    expect(readiness.pickupSectionsTotal).toBe(2);
  });

  it("normalizes delivery service areas", () => {
    const area = normalizeDeliveryServiceArea({
      Id: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
      OrganizationId: "11111111-1111-1111-1111-111111111111",
      BranchId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      CountryCode: "PH",
      RegionOrProvinceName: "Metro Manila",
      CityMunicipalityName: "Makati",
      NormalizedCityMunicipalityName: "MAKATI",
      IsActive: true,
      CreatedAtUtc: "2026-08-01T00:00:00Z",
      UpdatedAtUtc: "2026-08-01T00:00:00Z",
    });
    expect(area.cityMunicipalityName).toBe("Makati");
    expect(area.regionOrProvinceName).toBe("Metro Manila");
  });

  it("calls Platform branch fulfillment endpoints including delivery areas", async () => {
    const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      const method = init?.method ?? "GET";
      if (url.includes("/delivery-service-areas/") && method === "DELETE") {
        return new Response(
          JSON.stringify({
            branchId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
            canUseCustomerOrdering: true,
            canUseDelivery: true,
            customerOrderingEnabled: true,
            pickupEnabled: true,
            deliveryEnabled: false,
            onlineOrdersPaused: false,
            customerOrderingReady: true,
            pickupReady: true,
            deliveryReady: false,
            customerOrderingOperational: false,
            pickupOperational: false,
            deliveryOperational: false,
            missingRequirements: ["delivery_area"],
            reasonCodes: ["delivery_area_missing"],
            storeIsOpenNow: false,
            deliveryAreasComplete: false,
          }),
          { status: 200, headers: { "Content-Type": "application/json" } },
        );
      }
      if (url.includes("/delivery-service-areas") && method === "POST") {
        return new Response(
          JSON.stringify({
            branchId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
            canUseCustomerOrdering: true,
            canUseDelivery: true,
            customerOrderingEnabled: true,
            pickupEnabled: true,
            deliveryEnabled: false,
            onlineOrdersPaused: false,
            customerOrderingReady: true,
            pickupReady: true,
            deliveryReady: true,
            customerOrderingOperational: false,
            pickupOperational: false,
            deliveryOperational: false,
            missingRequirements: [],
            reasonCodes: [],
            storeIsOpenNow: false,
            deliveryAreasComplete: true,
            deliverySectionsComplete: 5,
            deliverySectionsTotal: 5,
          }),
          { status: 200, headers: { "Content-Type": "application/json" } },
        );
      }
      if (url.includes("/delivery-service-areas") && method === "GET") {
        return new Response(
          JSON.stringify([
            {
              id: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
              organizationId: "11111111-1111-1111-1111-111111111111",
              branchId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
              countryCode: "PH",
              regionOrProvinceName: "Metro Manila",
              cityMunicipalityName: "Makati",
              normalizedCityMunicipalityName: "MAKATI",
              isActive: true,
              createdAtUtc: "2026-08-01T00:00:00Z",
              updatedAtUtc: "2026-08-01T00:00:00Z",
            },
            {
              id: "cccccccc-cccc-cccc-cccc-cccccccccccc",
              organizationId: "11111111-1111-1111-1111-111111111111",
              branchId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
              countryCode: "PH",
              cityMunicipalityName: "Pasig",
              normalizedCityMunicipalityName: "PASIG",
              isActive: false,
              createdAtUtc: "2026-08-01T00:00:00Z",
              updatedAtUtc: "2026-08-01T00:00:00Z",
            },
          ]),
          { status: 200, headers: { "Content-Type": "application/json" } },
        );
      }
      if (
        url.includes("/branches") &&
        method === "GET" &&
        !url.includes("fulfillment") &&
        !url.includes("delivery")
      ) {
        return new Response(
          JSON.stringify([
            {
              id: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
              organizationId: "11111111-1111-1111-1111-111111111111",
              code: "MAIN",
              name: "Main",
              isPrimary: true,
              status: "Active",
              pickupReady: true,
              deliveryReady: false,
              canUseDelivery: true,
              branchDetailsComplete: true,
            },
          ]),
          { status: 200, headers: { "Content-Type": "application/json" } },
        );
      }
      if (url.includes("/fulfillment-settings") && method === "PUT") {
        return new Response(
          JSON.stringify({
            branchId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
            canUseCustomerOrdering: true,
            canUseDelivery: true,
            customerOrderingEnabled: true,
            pickupEnabled: true,
            deliveryEnabled: false,
            onlineOrdersPaused: false,
            customerOrderingReady: true,
            pickupReady: true,
            deliveryReady: false,
            customerOrderingOperational: false,
            pickupOperational: false,
            deliveryOperational: false,
            missingRequirements: ["map_location"],
            reasonCodes: ["map_location_missing"],
            storeIsOpenNow: false,
          }),
          { status: 200, headers: { "Content-Type": "application/json" } },
        );
      }
      if (url.endsWith("/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa") && method === "PUT") {
        return new Response(
          JSON.stringify({
            id: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
            organizationId: "11111111-1111-1111-1111-111111111111",
            code: "MAIN",
            name: "Updated",
            isPrimary: true,
            status: "Active",
            latitude: 14.6,
            longitude: 120.9,
          }),
          { status: 200, headers: { "Content-Type": "application/json" } },
        );
      }
      if (url.includes("/operating-hours") && method === "PUT") {
        return new Response(
          JSON.stringify({
            branchId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
            canUseCustomerOrdering: true,
            canUseDelivery: true,
            customerOrderingEnabled: true,
            pickupEnabled: true,
            deliveryEnabled: false,
            onlineOrdersPaused: false,
            customerOrderingReady: true,
            pickupReady: true,
            deliveryReady: false,
            customerOrderingOperational: false,
            pickupOperational: false,
            deliveryOperational: false,
            missingRequirements: [],
            reasonCodes: [],
            storeIsOpenNow: true,
          }),
          { status: 200, headers: { "Content-Type": "application/json" } },
        );
      }
      if (url.includes("/delivery-policy") && method === "PUT") {
        return new Response(
          JSON.stringify({
            branchId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
            organizationId: "11111111-1111-1111-1111-111111111111",
            minimumOrderAmount: 100,
            baseDeliveryFee: 40,
            includedDistanceKm: 2,
            additionalFeePerKm: 10,
            maximumDeliveryDistanceKm: 8,
            freeDeliveryThreshold: null,
            createdAtUtc: "2026-08-01T00:00:00Z",
            updatedAtUtc: "2026-08-01T00:00:00Z",
          }),
          { status: 200, headers: { "Content-Type": "application/json" } },
        );
      }
      if (url.includes("/antiforgery/token")) {
        return new Response(JSON.stringify({ headerName: "X-XSRF-TOKEN", token: "t" }), {
          status: 200,
          headers: { "Content-Type": "application/json" },
        });
      }
      return new Response("{}", { status: 404 });
    });
    vi.stubGlobal("fetch", fetchMock);

    const listed = await listOrganizationBranchesForFulfillment(
      "11111111-1111-1111-1111-111111111111",
    );
    expect(listed[0]?.name).toBe("Main");
    expect(listed[0]?.pickupReady).toBe(true);

    const areas = await listBranchDeliveryServiceAreas(
      "11111111-1111-1111-1111-111111111111",
      "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    );
    expect(areas).toHaveLength(1);
    expect(areas[0]?.cityMunicipalityName).toBe("Makati");

    const added = await addBranchDeliveryServiceArea(
      "11111111-1111-1111-1111-111111111111",
      "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      {
        countryCode: "PH",
        cityMunicipalityName: "Quezon City",
        regionOrProvinceName: "Metro Manila",
      },
    );
    expect(added.deliveryAreasComplete).toBe(true);

    const removed = await deleteBranchDeliveryServiceArea(
      "11111111-1111-1111-1111-111111111111",
      "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
    );
    expect(removed.missingRequirements).toContain("delivery_area");

    const updated = await updateOrganizationBranch(
      "11111111-1111-1111-1111-111111111111",
      "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      { name: "Updated", latitude: 14.6, longitude: 120.9 },
    );
    expect(updated.name).toBe("Updated");

    const readiness = await updateBranchFulfillmentSettings(
      "11111111-1111-1111-1111-111111111111",
      "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      { pickupEnabled: true },
    );
    expect(readiness.pickupEnabled).toBe(true);

    await upsertBranchOperatingHours(
      "11111111-1111-1111-1111-111111111111",
      "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      {
        days: [
          {
            dayOfWeek: "Monday",
            isClosed: false,
            isOpen24Hours: false,
            openTime: "09:00",
            closeTime: "18:00",
          },
        ],
      },
    );

    const policy = await upsertBranchDeliveryPolicy(
      "11111111-1111-1111-1111-111111111111",
      "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      {
        minimumOrderAmount: 100,
        baseDeliveryFee: 40,
        includedDistanceKm: 2,
        additionalFeePerKm: 10,
        maximumDeliveryDistanceKm: 8,
      },
    );
    expect(policy.maximumDeliveryDistanceKm).toBe(8);
  });
});
