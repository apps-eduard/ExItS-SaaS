import { expect, test } from "@playwright/test";
import { assertNoHorizontalOverflow } from "./helpers";
import {
  E2E_BRANCH_ID,
  E2E_ORG_ID,
  chooseOwnerManageBusiness,
  clientNavigate,
  mockBoundCashierSession,
  mockBoundOwnerSession,
  signInAndBindCashier,
  signInAndBindOwner,
} from "./mock-bound-session";

const VIEWPORTS = [
  { width: 375, height: 812 },
  { width: 768, height: 1024 },
  { width: 1024, height: 768 },
  { width: 1440, height: 900 },
] as const;

const UNKNOWN_BRANCH_ID = "cccccccc-cccc-4ccc-8ccc-cccccccccccc";

type BranchState = {
  addressLine1: string;
  city: string;
  latitude: number | null;
  longitude: number | null;
  contactPhone: string;
  timeZoneId: string;
  pickupEnabled: boolean;
  deliveryEnabled: boolean;
  customerOrderingEnabled: boolean;
  onlineOrdersPaused: boolean;
  missing: string[];
  reasonCodes: string[];
  customerOrderingReady: boolean;
  pickupReady: boolean;
  deliveryReady: boolean;
  updates: Array<Record<string, unknown>>;
  fulfillmentUpdates: Array<Record<string, unknown>>;
  hoursPuts: number;
  policyPuts: number;
};

function branchBody(state: BranchState, overrides: Record<string, unknown> = {}) {
  return {
    id: E2E_BRANCH_ID,
    organizationId: E2E_ORG_ID,
    code: "MAIN",
    name: "Main Branch",
    isPrimary: true,
    status: "Active",
    addressLine1: state.addressLine1,
    addressLine2: null,
    city: state.city,
    region: "NCR",
    postalCode: "1000",
    countryCode: "PH",
    latitude: state.latitude,
    longitude: state.longitude,
    pickupEnabled: state.pickupEnabled,
    deliveryEnabled: state.deliveryEnabled,
    customerOrderingEnabled: state.customerOrderingEnabled,
    onlineOrdersPaused: state.onlineOrdersPaused,
    contactPhone: state.contactPhone,
    timeZoneId: state.timeZoneId,
    deliveryPolicy: {
      branchId: E2E_BRANCH_ID,
      organizationId: E2E_ORG_ID,
      minimumOrderAmount: 100,
      baseDeliveryFee: 40,
      includedDistanceKm: 2,
      additionalFeePerKm: 10,
      maximumDeliveryDistanceKm: 8,
      freeDeliveryThreshold: null,
      createdAtUtc: "2026-08-01T00:00:00Z",
      updatedAtUtc: "2026-08-01T00:00:00Z",
    },
    ...overrides,
  };
}

function readinessBody(state: BranchState) {
  return {
    branchId: E2E_BRANCH_ID,
    canUseCustomerOrdering: true,
    canUseDelivery: true,
    customerOrderingEnabled: state.customerOrderingEnabled,
    pickupEnabled: state.pickupEnabled,
    deliveryEnabled: state.deliveryEnabled,
    onlineOrdersPaused: state.onlineOrdersPaused,
    onlineOrdersPauseReason: state.onlineOrdersPaused ? "TooBusy" : null,
    customerOrderingReady: state.customerOrderingReady,
    pickupReady: state.pickupReady,
    deliveryReady: state.deliveryReady,
    customerOrderingOperational: false,
    pickupOperational: false,
    deliveryOperational: false,
    missingRequirements: state.missing,
    reasonCodes: state.reasonCodes,
    storeOpenStatus: "Closed",
    storeIsOpenNow: false,
    storeStatusMessage: "Store is closed for online orders right now.",
  };
}

async function mockBranchFulfillmentApi(
  page: import("@playwright/test").Page,
): Promise<BranchState> {
  const state: BranchState = {
    addressLine1: "123 Rizal St",
    city: "Manila",
    latitude: 14.5995,
    longitude: 120.9842,
    contactPhone: "09171234567",
    timeZoneId: "Asia/Manila",
    pickupEnabled: false,
    deliveryEnabled: false,
    customerOrderingEnabled: true,
    onlineOrdersPaused: false,
    missing: [],
    reasonCodes: ["pickup_disabled", "delivery_disabled"],
    customerOrderingReady: true,
    pickupReady: true,
    deliveryReady: true,
    updates: [],
    fulfillmentUpdates: [],
    hoursPuts: 0,
    policyPuts: 0,
  };

  await page.route("**/platform-api/api/v1/platform/organizations/**/branches**", async (route) => {
    const url = route.request().url();
    const method = route.request().method();
    const pathname = new URL(url).pathname.replace(/\/$/, "");

    if (method === "GET" && pathname.endsWith(`/organizations/${E2E_ORG_ID}/branches`)) {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify([branchBody(state)]),
      });
    }

    if (pathname.includes(`/branches/${UNKNOWN_BRANCH_ID}`)) {
      return route.fulfill({
        status: 404,
        contentType: "application/json",
        body: JSON.stringify({ detail: "Branch was not found.", errorCode: "not_found" }),
      });
    }

    if (method === "GET" && pathname.endsWith(`/branches/${E2E_BRANCH_ID}/fulfillment-readiness`)) {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(readinessBody(state)),
      });
    }

    if (method === "GET" && pathname.endsWith(`/branches/${E2E_BRANCH_ID}/operating-hours`)) {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify([
          {
            dayOfWeek: "Monday",
            isClosed: false,
            isOpen24Hours: false,
            openTime: "09:00:00",
            closeTime: "18:00:00",
          },
          {
            dayOfWeek: "Tuesday",
            isClosed: true,
            isOpen24Hours: false,
            openTime: null,
            closeTime: null,
          },
          {
            dayOfWeek: "Wednesday",
            isClosed: true,
            isOpen24Hours: false,
            openTime: null,
            closeTime: null,
          },
          {
            dayOfWeek: "Thursday",
            isClosed: true,
            isOpen24Hours: false,
            openTime: null,
            closeTime: null,
          },
          {
            dayOfWeek: "Friday",
            isClosed: true,
            isOpen24Hours: false,
            openTime: null,
            closeTime: null,
          },
          {
            dayOfWeek: "Saturday",
            isClosed: true,
            isOpen24Hours: false,
            openTime: null,
            closeTime: null,
          },
          {
            dayOfWeek: "Sunday",
            isClosed: true,
            isOpen24Hours: false,
            openTime: null,
            closeTime: null,
          },
        ]),
      });
    }

    if (method === "PUT" && pathname.endsWith(`/branches/${E2E_BRANCH_ID}`)) {
      const body = route.request().postDataJSON() as Record<string, unknown>;
      state.updates.push(body);
      if (typeof body.addressLine1 === "string") state.addressLine1 = body.addressLine1;
      if (typeof body.city === "string") state.city = body.city;
      if (typeof body.contactPhone === "string") state.contactPhone = body.contactPhone;
      if (typeof body.timeZoneId === "string") state.timeZoneId = body.timeZoneId;
      if (body.clearCoordinates === true) {
        state.latitude = null;
        state.longitude = null;
      } else {
        if (typeof body.latitude === "number") state.latitude = body.latitude;
        if (typeof body.longitude === "number") state.longitude = body.longitude;
      }
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(branchBody(state, { name: String(body.name ?? "Main Branch") })),
      });
    }

    if (method === "PUT" && pathname.endsWith(`/branches/${E2E_BRANCH_ID}/operating-hours`)) {
      state.hoursPuts += 1;
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(readinessBody(state)),
      });
    }

    if (method === "PUT" && pathname.endsWith(`/branches/${E2E_BRANCH_ID}/delivery-policy`)) {
      state.policyPuts += 1;
      const body = route.request().postDataJSON() as Record<string, unknown>;
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          branchId: E2E_BRANCH_ID,
          organizationId: E2E_ORG_ID,
          ...body,
          createdAtUtc: "2026-08-01T00:00:00Z",
          updatedAtUtc: "2026-08-01T00:00:00Z",
        }),
      });
    }

    if (method === "PUT" && pathname.endsWith(`/branches/${E2E_BRANCH_ID}/fulfillment-settings`)) {
      const body = route.request().postDataJSON() as Record<string, unknown>;
      state.fulfillmentUpdates.push(body);
      if (typeof body.pickupEnabled === "boolean") state.pickupEnabled = body.pickupEnabled;
      if (typeof body.deliveryEnabled === "boolean") state.deliveryEnabled = body.deliveryEnabled;
      if (typeof body.customerOrderingEnabled === "boolean") {
        state.customerOrderingEnabled = body.customerOrderingEnabled;
      }
      state.reasonCodes = [];
      if (!state.pickupEnabled) state.reasonCodes.push("pickup_disabled");
      if (!state.deliveryEnabled) state.reasonCodes.push("delivery_disabled");
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(readinessBody(state)),
      });
    }

    return route.fallback();
  });

  return state;
}

async function signInOwnerManageBusiness(page: import("@playwright/test").Page) {
  await signInAndBindOwner(page);
  const manage = page.getByTestId("workspace-destination-manage_business");
  const orgPage = page.getByTestId("org-essentials-page");
  await Promise.race([
    manage.waitFor({ state: "visible", timeout: 15000 }),
    orgPage.waitFor({ state: "visible", timeout: 15000 }),
  ]);
  if (await manage.isVisible().catch(() => false)) {
    await chooseOwnerManageBusiness(page);
  }
  await orgPage.waitFor({ state: "visible", timeout: 15000 });
}

test.describe("RMAP-18 branch fulfillment", () => {
  test("owner configures address, coords, hours, pickup/delivery + readiness", async ({ page }) => {
    await mockBoundOwnerSession(page);
    const state = await mockBranchFulfillmentApi(page);
    await signInOwnerManageBusiness(page);
    await clientNavigate(page, "/org/branches");
    await expect(page.getByTestId("branch-fulfillment-list")).toBeVisible();
    await expect(page.getByText("Main Branch")).toBeVisible();
    await expect(page.getByText("Pickup: Disabled")).toBeVisible();

    await page.getByTestId(`open-branch-fulfillment-${E2E_BRANCH_ID}`).click();
    await expect(page.getByTestId("branch-fulfillment-edit")).toBeVisible();
    await expect(page.getByTestId("branch-map-fallback")).toBeVisible();
    await expect(page.getByTestId("branch-address1")).toHaveValue("123 Rizal St");
    await expect(page.getByTestId("branch-latitude")).toHaveValue("14.5995");
    await expect(page.getByTestId("pickup-status")).toContainText("Pickup: Disabled");
    await expect(page.getByTestId("delivery-status")).toContainText("Delivery: Disabled");

    await page.getByTestId("branch-address1").fill("456 Mabini Ave");
    await page.getByTestId("branch-city").fill("Quezon City");
    await page.getByTestId("branch-latitude").fill("14.65");
    await page.getByTestId("branch-longitude").fill("121.05");
    await page.getByTestId("hours-open-Tuesday").check();
    await page.getByTestId("hours-start-Tuesday").fill("10:00");
    await page.getByTestId("hours-end-Tuesday").fill("19:00");
    await page.getByTestId("policy-maximum-km").fill("12");
    await page.getByTestId("branch-save").click();
    await expect(page.getByTestId("branch-fulfillment-ok")).toBeVisible();
    expect(state.updates.at(-1)?.addressLine1).toBe("456 Mabini Ave");
    expect(state.updates.at(-1)?.latitude).toBe(14.65);
    expect(state.hoursPuts).toBeGreaterThan(0);
    expect(state.policyPuts).toBeGreaterThan(0);

    await page.getByTestId("branch-latitude").fill("99");
    await page.getByTestId("branch-save").click();
    await expect(page.getByTestId("branch-fulfillment-error")).toContainText("Latitude");

    await page.getByTestId("branch-latitude").fill("14.65");
    await page.getByTestId("enable-pickup").click();
    await expect(page.getByTestId("pickup-status")).toContainText("Pickup: Enabled");
    expect(state.fulfillmentUpdates.some((u) => u.pickupEnabled === true)).toBe(true);

    await page.getByTestId("enable-delivery").click();
    await expect(page.getByTestId("delivery-status")).toContainText("Delivery: Enabled");
    expect(state.fulfillmentUpdates.some((u) => u.deliveryEnabled === true)).toBe(true);

    await expect(page.getByTestId("branch-maps-google")).toBeVisible();
    await expect(page.getByTestId("branch-gps-assist")).toBeVisible();
  });

  test("shows server missing-config messaging", async ({ page }) => {
    await mockBoundOwnerSession(page);
    const state = await mockBranchFulfillmentApi(page);
    state.missing = ["branch_address", "map_location", "delivery_policy"];
    state.reasonCodes = ["branch_address_incomplete", "map_location_missing"];
    state.customerOrderingReady = false;
    state.pickupReady = false;
    state.deliveryReady = false;
    state.latitude = null;
    state.longitude = null;
    state.addressLine1 = "";
    state.city = "";
    await signInOwnerManageBusiness(page);
    await clientNavigate(page, `/org/branches/${E2E_BRANCH_ID}`);
    await expect(page.getByTestId("branch-missing-requirements")).toBeVisible();
    await expect(page.getByTestId("branch-missing-requirements")).toContainText("address");
    await expect(page.getByTestId("branch-missing-requirements")).toContainText("coordinates");
    await expect(page.getByTestId("pickup-status")).toContainText("Not ready");
  });

  test("cashier is denied branch fulfillment admin", async ({ page }) => {
    await mockBoundCashierSession(page);
    await mockBranchFulfillmentApi(page);
    await signInAndBindCashier(page);
    await clientNavigate(page, "/org/branches");
    await expect(page.getByTestId("admin-experience-denied")).toBeVisible();
  });

  test("unknown branch shows not found (isolation)", async ({ page }) => {
    await mockBoundOwnerSession(page);
    await mockBranchFulfillmentApi(page);
    await signInOwnerManageBusiness(page);
    await clientNavigate(page, `/org/branches/${UNKNOWN_BRANCH_ID}`);
    await expect(page.getByTestId("branch-fulfillment-not-found")).toBeVisible();
  });

  test("responsive matrix + Filipino locale smoke", async ({ page }) => {
    await mockBoundOwnerSession(page);
    await mockBranchFulfillmentApi(page);
    await signInOwnerManageBusiness(page);

    for (const viewport of VIEWPORTS) {
      await page.setViewportSize(viewport);
      await clientNavigate(page, "/org/branches");
      await expect(page.getByTestId("branch-fulfillment-list")).toBeVisible();
      await assertNoHorizontalOverflow(page);
    }

    await page.getByTestId("account-menu-trigger").click();
    await page.getByRole("menuitem", { name: /Preferences|Mga setting/i }).click();
    await page.getByRole("radio", { name: /Filipino/i }).click();
    await expect(page.locator("html")).toHaveAttribute("lang", "fil-PH");
    await page.getByTestId("preferences-close").click();
    await clientNavigate(page, "/org/branches");
    await expect(page.getByTestId("branch-fulfillment-list")).toBeVisible();
    await expect(page.getByTestId("branch-fulfillment-list")).toContainText(/fulfillment/i);
  });
});
