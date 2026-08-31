import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { BranchFulfillmentEditPage } from "@/features/branches/BranchFulfillmentEditPage";

const updateOrganizationBranch = vi.fn();
const upsertBranchOperatingHours = vi.fn();
const upsertBranchDeliveryPolicy = vi.fn();
const getBranchFulfillmentReadiness = vi.fn();

vi.mock("@/access/pos-capabilities", () => ({
  canManageBranchFulfillment: () => true,
}));

vi.mock("@/i18n/I18nProvider", () => ({
  useI18n: () => ({
    t: (key: string) => key,
  }),
}));

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => ({
    boundWorkspace: { organizationId: "11111111-1111-1111-1111-111111111111" },
    sessionGrant: { productRole: "Owner" },
  }),
}));

vi.mock("@/api/platform/branch-fulfillment-client", () => ({
  listOrganizationBranchesForFulfillment: vi.fn(),
  getBranchFulfillmentReadiness: (...args: unknown[]) => getBranchFulfillmentReadiness(...args),
  getBranchOperatingHours: vi.fn(),
  listBranchDeliveryServiceAreas: vi.fn(),
  updateOrganizationBranch: (...args: unknown[]) => updateOrganizationBranch(...args),
  upsertBranchOperatingHours: (...args: unknown[]) => upsertBranchOperatingHours(...args),
  upsertBranchDeliveryPolicy: (...args: unknown[]) => upsertBranchDeliveryPolicy(...args),
  updateBranchFulfillmentSettings: vi.fn(),
  setBranchOnlineOrdersPaused: vi.fn(),
  addBranchDeliveryServiceArea: vi.fn(),
  deleteBranchDeliveryServiceArea: vi.fn(),
}));

vi.mock("@/features/branches/branch-coordinates", async () => {
  const actual = await vi.importActual<typeof import("@/features/branches/branch-coordinates")>(
    "@/features/branches/branch-coordinates",
  );
  return {
    ...actual,
    isMapProviderConfigured: () => true,
  };
});

const branchId = "22222222-2222-2222-2222-222222222222";
const orgId = "11111111-1111-1111-1111-111111111111";

const branch = {
  id: branchId,
  organizationId: orgId,
  name: "Main Branch",
  addressLine1: "123 Rizal St",
  addressLine2: null,
  city: "Bacolod",
  region: "Negros Occidental",
  postalCode: "6100",
  countryCode: "PH",
  latitude: 10.6765,
  longitude: 122.9509,
  contactPhone: "09171234567",
  timeZoneId: "Asia/Manila",
  status: "Active",
  deliveryPolicy: {
    minimumOrderAmount: -1,
    baseDeliveryFee: 0,
    includedDistanceKm: 0,
    additionalFeePerKm: 0,
    maximumDeliveryDistanceKm: 0,
    freeDeliveryThreshold: null,
  },
};

const readiness = {
  branchId,
  canUseCustomerOrdering: true,
  canUseDelivery: true,
  customerOrderingEnabled: true,
  pickupEnabled: true,
  deliveryEnabled: false,
  onlineOrdersPaused: false,
  onlineOrdersPauseReason: null,
  customerOrderingReady: true,
  pickupReady: true,
  deliveryReady: false,
  customerOrderingOperational: true,
  pickupOperational: true,
  deliveryOperational: false,
  missingRequirements: ["DeliveryPolicyIncomplete"],
  reasonCodes: [] as string[],
  storeOpenStatus: "Open",
  storeIsOpenNow: true,
  storeStatusMessage: null,
  branchDetailsComplete: true,
  operatingHoursComplete: true,
  deliveryLocationComplete: true,
  deliveryPolicyComplete: false,
  deliveryServiceAreasComplete: true,
  deliveryAreasComplete: true,
};

function renderPage(client?: QueryClient) {
  const queryClient =
    client ??
    new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[`/org/branches/${branchId}`]}>
        <Routes>
          <Route path="/org/branches" element={<div data-testid="branch-list">list</div>} />
          <Route path="/org/branches/:branchId" element={<BranchFulfillmentEditPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe("BranchFulfillmentEditPage section saves", () => {
  beforeEach(async () => {
    vi.clearAllMocks();
    const api = await import("@/api/platform/branch-fulfillment-client");
    vi.mocked(api.listOrganizationBranchesForFulfillment).mockResolvedValue([branch as never]);
    vi.mocked(api.getBranchOperatingHours).mockResolvedValue([]);
    vi.mocked(api.listBranchDeliveryServiceAreas).mockResolvedValue([]);
    getBranchFulfillmentReadiness.mockResolvedValue(readiness);
    updateOrganizationBranch.mockResolvedValue({
      ...branch,
      latitude: 10.7,
      longitude: 122.96,
    });
  });

  it("Save location calls only coordinate branch update, not policy or hours", async () => {
    const user = userEvent.setup();
    renderPage();

    await screen.findByTestId("branch-tab-location");
    await user.click(screen.getByTestId("branch-tab-location"));

    const lat = await screen.findByTestId("branch-latitude");
    const lng = screen.getByTestId("branch-longitude");
    await user.clear(lat);
    await user.type(lat, "10.700000");
    await user.clear(lng);
    await user.type(lng, "122.960000");

    await user.click(screen.getByTestId("branch-save"));

    await waitFor(() => {
      expect(updateOrganizationBranch).toHaveBeenCalledTimes(1);
    });

    const [, , body] = updateOrganizationBranch.mock.calls[0];
    expect(body).toMatchObject({
      latitude: 10.7,
      longitude: 122.96,
    });
    expect(body.addressLine1).toBeUndefined();
    expect(body.city).toBeUndefined();
    expect(upsertBranchDeliveryPolicy).not.toHaveBeenCalled();
    expect(upsertBranchOperatingHours).not.toHaveBeenCalled();
    expect(screen.queryByText(/Included distance cannot be negative/i)).toBeNull();
  });

  it("shows Choose on map and contextual Save location label", async () => {
    const user = userEvent.setup();
    renderPage();
    await screen.findByTestId("branch-tab-location");
    await user.click(screen.getByTestId("branch-tab-location"));
    expect(await screen.findByTestId("branch-choose-on-map")).toBeInTheDocument();
    expect(screen.getByTestId("branch-save")).toHaveTextContent("branches.saveLocation");
  });

  it("renders after remount when query data is cached", async () => {
    const client = new QueryClient({
      defaultOptions: { queries: { retry: false, staleTime: Infinity } },
    });
    const { unmount } = renderPage(client);

    await screen.findByTestId("branch-setup-tabs");
    unmount();
    renderPage(client);

    await screen.findByTestId("branch-setup-tabs");
    expect(screen.queryByTestId("branch-fulfillment-not-found")).not.toBeInTheDocument();
  });
});
