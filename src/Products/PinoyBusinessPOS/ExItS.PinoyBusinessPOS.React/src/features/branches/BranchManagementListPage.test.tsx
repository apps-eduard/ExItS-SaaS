import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { BranchManagementListPage } from "@/features/branches/BranchManagementListPage";

const canUseWarehouseBranches = vi.fn(() => true);

vi.mock("@/access/pos-capabilities", () => ({
  canManageBranchFulfillment: () => true,
  canInviteOrganizationStaff: () => true,
  canUseWarehouseBranches: () => canUseWarehouseBranches(),
}));

vi.mock("@/i18n/I18nProvider", () => ({
  useI18n: () => ({
    t: (key: string) => key,
  }),
}));

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => ({
    boundWorkspace: { organizationId: "11111111-1111-1111-1111-111111111111" },
    sessionGrant: { productRole: "Owner", organizationManagementAuthority: true },
  }),
}));

const listBranchManagementSummaries = vi.fn();
const getBranchCapacity = vi.fn();
const listOrganizationAreas = vi.fn();

vi.mock("@/api/platform/organization-branches-client", () => ({
  listBranchManagementSummaries: (...args: unknown[]) => listBranchManagementSummaries(...args),
  getBranchCapacity: (...args: unknown[]) => getBranchCapacity(...args),
}));

vi.mock("@/api/platform/organization-areas-client", () => ({
  listOrganizationAreas: (...args: unknown[]) => listOrganizationAreas(...args),
}));

const branchId = "22222222-2222-2222-2222-222222222222";
const warehouseId = "33333333-3333-3333-3333-333333333333";

function retailBranch(overrides: Record<string, unknown> = {}) {
  return {
    id: branchId,
    organizationId: "11111111-1111-1111-1111-111111111111",
    code: "MAIN",
    name: "Main Branch",
    branchType: "Retail",
    isPrimary: true,
    status: "Active",
    city: "Bacolod",
    region: "Negros Occidental",
    addressLine1: "Lacson St",
    areaName: "North",
    pickupEnabled: true,
    deliveryEnabled: false,
    customerOrderingEnabled: false,
    assignedStaffCount: 4,
    activeDeviceCount: 2,
    pickupSectionsComplete: 2,
    pickupSectionsTotal: 2,
    deliverySectionsComplete: 0,
    deliverySectionsTotal: 5,
    ...overrides,
  };
}

function warehouseBranch(overrides: Record<string, unknown> = {}) {
  return {
    id: warehouseId,
    organizationId: "11111111-1111-1111-1111-111111111111",
    code: "WH1",
    name: "Main Warehouse",
    branchType: "Warehouse",
    isPrimary: false,
    status: "Active",
    city: null,
    region: null,
    addressLine1: null,
    areaName: null,
    areaId: null,
    pickupEnabled: false,
    deliveryEnabled: false,
    customerOrderingEnabled: false,
    assignedStaffCount: 1,
    activeDeviceCount: 0,
    pickupSectionsComplete: 0,
    pickupSectionsTotal: 0,
    deliverySectionsComplete: 0,
    deliverySectionsTotal: 0,
    ...overrides,
  };
}

function renderPage(initialPath = "/org/branches") {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[initialPath]}>
        <Routes>
          <Route path="/org/branches" element={<BranchManagementListPage />} />
          <Route path="/org/branches/new" element={<div data-testid="create-route" />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe("BranchManagementListPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    canUseWarehouseBranches.mockReturnValue(true);
    getBranchCapacity.mockResolvedValue({ ok: true, value: { used: 2, allowed: 3 } });
    listOrganizationAreas.mockResolvedValue({
      ok: true,
      value: { areas: [{ id: "area-1", name: "North" }] },
    });
    listBranchManagementSummaries.mockResolvedValue({
      ok: true,
      value: [retailBranch()],
    });
  });

  it("uses Branches & Warehouses title and location capacity copy", async () => {
    renderPage();

    await waitFor(() => {
      expect(screen.getByTestId("branch-mgmt-capacity")).toBeInTheDocument();
    });
    expect(screen.getByRole("heading", { name: "branches.mgmt.title" })).toBeInTheDocument();
    expect(screen.getByTestId("branch-mgmt-capacity")).toHaveTextContent("branches.mgmt.capacity");
    expect(screen.getByTestId("branch-mgmt-capacity-value")).toHaveTextContent(
      "branches.mgmt.capacityOf",
    );
    expect(screen.getByTestId("branch-mgmt-capacity-breakdown")).toHaveTextContent(
      "branches.mgmt.capacityBreakdown",
    );
  });

  it("exposes Add location menu with retail and warehouse when entitled", async () => {
    const user = userEvent.setup();
    renderPage();

    await waitFor(() => {
      expect(screen.getByTestId("branch-mgmt-add")).toBeInTheDocument();
    });
    await user.click(screen.getByTestId("branch-mgmt-add"));
    expect(screen.getByTestId("branch-mgmt-add-retail")).toBeInTheDocument();
    expect(screen.getByTestId("branch-mgmt-add-warehouse")).toBeInTheDocument();

    await user.click(screen.getByTestId("branch-mgmt-add-warehouse"));
    await waitFor(() => {
      expect(screen.getByTestId("create-route")).toBeInTheDocument();
    });
  });

  it("locks warehouse entry when not entitled", async () => {
    canUseWarehouseBranches.mockReturnValue(false);
    const user = userEvent.setup();
    renderPage();

    await waitFor(() => {
      expect(screen.getByTestId("branch-mgmt-add")).toBeInTheDocument();
    });
    await user.click(screen.getByTestId("branch-mgmt-add"));
    expect(screen.getByTestId("branch-mgmt-add-retail")).toBeInTheDocument();
    expect(screen.getByTestId("branch-mgmt-add-warehouse-locked")).toBeDisabled();
    expect(screen.queryByTestId("branch-mgmt-warehouse-hint")).not.toBeInTheDocument();
  });

  it("shows zero-warehouse hint when entitled and count is zero", async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId("branch-mgmt-warehouse-hint")).toBeInTheDocument();
    });
    expect(screen.getByTestId("branch-mgmt-warehouse-hint-add")).toHaveAttribute(
      "href",
      "/org/branches/new?type=warehouse",
    );
  });

  it("hides zero-warehouse hint after a warehouse exists", async () => {
    listBranchManagementSummaries.mockResolvedValue({
      ok: true,
      value: [retailBranch(), warehouseBranch()],
    });
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId(`branch-mgmt-type-${warehouseId}`)).toBeInTheDocument();
    });
    expect(screen.queryByTestId("branch-mgmt-warehouse-hint")).not.toBeInTheDocument();
  });

  it("filters by type", async () => {
    const user = userEvent.setup();
    listBranchManagementSummaries.mockResolvedValue({
      ok: true,
      value: [retailBranch(), warehouseBranch()],
    });
    renderPage();

    await waitFor(() => {
      expect(screen.getByTestId(`branch-mgmt-card-${branchId}`)).toBeInTheDocument();
    });
    expect(screen.getByTestId(`branch-mgmt-card-${warehouseId}`)).toBeInTheDocument();

    await user.click(screen.getByTestId("branch-mgmt-type-warehouse"));
    expect(screen.queryByTestId(`branch-mgmt-card-${branchId}`)).not.toBeInTheDocument();
    expect(screen.getByTestId(`branch-mgmt-card-${warehouseId}`)).toBeInTheDocument();

    await user.click(screen.getByTestId("branch-mgmt-type-retail"));
    expect(screen.getByTestId(`branch-mgmt-card-${branchId}`)).toBeInTheDocument();
    expect(screen.queryByTestId(`branch-mgmt-card-${warehouseId}`)).not.toBeInTheDocument();
  });

  it("renders capacity, primary badge, and no Delete action", async () => {
    renderPage();

    await waitFor(() => {
      expect(screen.getByTestId("branch-mgmt-capacity")).toBeInTheDocument();
    });
    expect(screen.getByTestId(`branch-mgmt-primary-${branchId}`)).toHaveTextContent(
      "branches.mgmt.primary",
    );
    expect(screen.getByTestId(`branch-mgmt-type-${branchId}`)).toHaveTextContent(
      "branches.type.retail",
    );
    expect(screen.queryByText(/Delete/i)).not.toBeInTheDocument();
    expect(screen.getByTestId("branch-mgmt-add")).toBeInTheDocument();
    expect(screen.getByTestId(`branch-mgmt-view-qr-${branchId}`)).toHaveAttribute(
      "href",
      `/org/branches/${branchId}?focus=qr#branch-storefront-qr`,
    );
  });

  it("renders compact entity-card structure with identity, meta, and actions", async () => {
    renderPage();

    await waitFor(() => {
      expect(screen.getByTestId(`branch-mgmt-card-${branchId}`)).toBeInTheDocument();
    });

    const card = screen.getByTestId(`branch-mgmt-card-${branchId}`);
    expect(card.className).toContain("exits-entity-card");
    expect(card.className).toContain("exits-entity-card--interactive");
    expect(card.className).not.toMatch(/active-tint|status-active-card|primary-soft-card/);
    expect(card.querySelector(".exits-entity-card__header")).not.toBeNull();
    expect(card.querySelector(".exits-entity-card__identity")).not.toBeNull();
    expect(card.querySelector(".exits-entity-card__meta")).not.toBeNull();
    expect(card.querySelector(".exits-entity-card__actions")).not.toBeNull();

    expect(screen.getByRole("heading", { name: "Main Branch" })).toBeInTheDocument();
    expect(card).toHaveTextContent("MAIN");
    expect(card).toHaveTextContent("Bacolod, Negros Occidental");
    expect(screen.getByTestId(`branch-mgmt-primary-${branchId}`)).toBeInTheDocument();
    expect(card).toHaveTextContent("branches.mgmt.status.active");

    expect(screen.getByTestId(`branch-mgmt-staff-${branchId}`)).toHaveTextContent("4");
    expect(screen.getByTestId(`branch-mgmt-devices-${branchId}`)).toHaveTextContent(
      "branches.mgmt.devicesActive",
    );
    expect(screen.getByTestId(`branch-mgmt-area-${branchId}`)).toHaveTextContent("North");
    expect(screen.getByTestId(`branch-mgmt-pickup-${branchId}`)).toHaveTextContent(
      "branches.mgmt.on",
    );
    expect(screen.getByTestId(`branch-mgmt-delivery-${branchId}`)).toHaveTextContent(
      "branches.mgmt.off",
    );

    expect(screen.getByTestId(`branch-mgmt-open-${branchId}`)).toHaveAttribute(
      "href",
      `/org/branches/${branchId}`,
    );
  });

  it("keeps More action opening the sheet without route change", async () => {
    const user = userEvent.setup();
    renderPage();

    await waitFor(() => {
      expect(screen.getByTestId(`branch-mgmt-more-${branchId}`)).toBeInTheDocument();
    });

    await user.click(screen.getByTestId(`branch-mgmt-more-${branchId}`));
    expect(screen.getByTestId("branch-mgmt-more-panel")).toBeInTheDocument();
    expect(screen.getByTestId(`branch-mgmt-more-fulfillment-${branchId}`)).toHaveAttribute(
      "href",
      `/org/branches/${branchId}/fulfillment`,
    );
    expect(screen.getByTestId(`branch-mgmt-more-staff-${branchId}`)).toHaveAttribute(
      "href",
      `/org/branches/${branchId}?tab=staff`,
    );
    // Primary card actions are not duplicated in the overlay.
    expect(
      within(screen.getByTestId("branch-mgmt-more-panel")).queryByText("branches.mgmt.open"),
    ).not.toBeInTheDocument();
    expect(
      within(screen.getByTestId("branch-mgmt-more-panel")).queryByText("branches.mgmt.viewQr"),
    ).not.toBeInTheDocument();
  });

  it("shows Warehouse type chip and hides retail-only metadata", async () => {
    listBranchManagementSummaries.mockResolvedValue({
      ok: true,
      value: [warehouseBranch()],
    });
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId(`branch-mgmt-type-${warehouseId}`)).toHaveTextContent(
        "branches.type.warehouse",
      );
    });
    const card = screen.getByTestId(`branch-mgmt-card-${warehouseId}`);
    expect(within(card).queryByTestId(`branch-mgmt-pickup-${warehouseId}`)).not.toBeInTheDocument();
    expect(within(card).queryByTestId(`branch-mgmt-delivery-${warehouseId}`)).not.toBeInTheDocument();
    expect(within(card).queryByTestId(`branch-mgmt-view-qr-${warehouseId}`)).not.toBeInTheDocument();
    expect(screen.getByTestId(`branch-mgmt-open-${warehouseId}`)).toHaveTextContent(
      "branches.mgmt.openWarehouse",
    );
  });

  it("warehouse more menu excludes fulfillment and QR", async () => {
    const user = userEvent.setup();
    listBranchManagementSummaries.mockResolvedValue({
      ok: true,
      value: [warehouseBranch()],
    });
    renderPage();

    await waitFor(() => {
      expect(screen.getByTestId(`branch-mgmt-more-${warehouseId}`)).toBeInTheDocument();
    });
    await user.click(screen.getByTestId(`branch-mgmt-more-${warehouseId}`));
    const panel = screen.getByTestId("branch-mgmt-more-panel");
    expect(within(panel).getByTestId(`branch-mgmt-more-staff-${warehouseId}`)).toBeInTheDocument();
    expect(within(panel).getByTestId(`branch-mgmt-more-devices-${warehouseId}`)).toBeInTheDocument();
    expect(
      within(panel).queryByTestId(`branch-mgmt-more-fulfillment-${warehouseId}`),
    ).not.toBeInTheDocument();
    expect(within(panel).queryByText("branches.mgmt.viewQr")).not.toBeInTheDocument();
  });
});
