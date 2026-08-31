import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter, Route, Routes, useLocation } from "react-router-dom";
import { BranchFulfillmentListPage } from "@/features/branches/BranchFulfillmentListPage";

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

const listOrganizationBranchesForFulfillment = vi.fn();

vi.mock("@/api/platform/branch-fulfillment-client", () => ({
  listOrganizationBranchesForFulfillment: (...args: unknown[]) =>
    listOrganizationBranchesForFulfillment(...args),
  updateBranchFulfillmentSettings: vi.fn(),
}));

function LocationProbe() {
  const location = useLocation();
  return <div data-testid="location-probe">{location.pathname}</div>;
}

function renderListPage() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });

  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={["/org/branches"]}>
        <Routes>
          <Route path="/org/branches" element={<BranchFulfillmentListPage />} />
          <Route path="/org/branches/:branchId" element={<LocationProbe />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

const branch = {
  id: "22222222-2222-2222-2222-222222222222",
  organizationId: "11111111-1111-1111-1111-111111111111",
  code: "MAIN",
  name: "Main Branch",
  isPrimary: true,
  status: "Active",
  city: "Bacolod",
  pickupEnabled: false,
  deliveryEnabled: false,
  pickupReady: false,
  deliveryReady: false,
  canUseDelivery: true,
  branchDetailsComplete: false,
  operatingHoursComplete: false,
  deliveryLocationComplete: false,
  deliveryPolicyComplete: false,
  deliveryAreasComplete: false,
  pickupSectionsComplete: 0,
  pickupSectionsTotal: 2,
  deliverySectionsComplete: 0,
  deliverySectionsTotal: 4,
};

describe("BranchFulfillmentListPage routing", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("redirects a single branch to overview", async () => {
    listOrganizationBranchesForFulfillment.mockResolvedValue([branch]);

    renderListPage();

    await waitFor(() => {
      expect(screen.getByTestId("location-probe")).toHaveTextContent(
        "/org/branches/22222222-2222-2222-2222-222222222222",
      );
    });
    expect(screen.queryByTestId("branch-fulfillment-list")).not.toBeInTheDocument();
  });

  it("keeps the list for multiple branches", async () => {
    listOrganizationBranchesForFulfillment.mockResolvedValue([
      branch,
      { ...branch, id: "33333333-3333-3333-3333-333333333333", name: "Second Branch", code: "SEC" },
    ]);

    renderListPage();

    expect(await screen.findByTestId("branch-fulfillment-list")).toBeInTheDocument();
    expect(screen.getByTestId("branch-fulfillment-items")).toBeInTheDocument();
    expect(screen.queryByTestId("location-probe")).not.toBeInTheDocument();
  });
});
