import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { BranchManagementDetailPage } from "@/features/branches/BranchManagementDetailPage";

const canViewInventory = vi.fn(() => true);
const canManageInventory = vi.fn(() => true);
const canViewPurchasing = vi.fn(() => true);
const canUseWarehouseBranches = vi.fn(() => true);

vi.mock("@/access/pos-capabilities", () => ({
  canManageBranchFulfillment: () => true,
  canInviteOrganizationStaff: () => true,
  canUseWarehouseBranches: () => canUseWarehouseBranches(),
  canViewInventory: () => canViewInventory(),
  canManageInventory: () => canManageInventory(),
  canViewPurchasing: () => canViewPurchasing(),
}));

vi.mock("@/i18n/I18nProvider", () => ({
  useI18n: () => ({
    t: (key: string, vars?: Record<string, string | number>) => {
      if (!vars) return key;
      return Object.entries(vars).reduce(
        (acc, [k, v]) => acc.replace(`{${k}}`, String(v)),
        key,
      );
    },
  }),
}));

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => ({
    boundWorkspace: {
      organizationId: "11111111-1111-1111-1111-111111111111",
      organizationDisplayName: "Kizy",
    },
    sessionGrant: { productRole: "Owner", organizationManagementAuthority: true },
  }),
}));

const getOrganizationBranch = vi.fn();
const listBranchManagementSummaries = vi.fn();
const listPosDevices = vi.fn();
const updateOrganizationBranchDetails = vi.fn();

vi.mock("@/api/platform/organization-branches-client", () => ({
  getOrganizationBranch: (...args: unknown[]) => getOrganizationBranch(...args),
  listBranchManagementSummaries: (...args: unknown[]) => listBranchManagementSummaries(...args),
  updateOrganizationBranchDetails: (...args: unknown[]) => updateOrganizationBranchDetails(...args),
  suspendOrganizationBranch: vi.fn(),
  reactivateOrganizationBranch: vi.fn(),
  archiveOrganizationBranch: vi.fn(),
  setPrimaryOrganizationBranch: vi.fn(),
}));

vi.mock("@/api/platform/pos-devices-client", () => ({
  listPosDevices: (...args: unknown[]) => listPosDevices(...args),
}));

vi.mock("@/features/branches/BranchStaffAccessPanel", () => ({
  BranchStaffAccessPanel: () => <div data-testid="branch-staff-access-panel" />,
}));

vi.mock("@/features/branches/BranchDetailsForm", () => ({
  BranchDetailsForm: ({ branchType }: { branchType: string }) => (
    <div data-testid="branch-details-form" data-branch-type={branchType} />
  ),
}));

vi.mock("@/features/branches/BranchStorefrontQrPanel", () => ({
  BranchStorefrontQrPanel: () => <div data-testid="branch-storefront-qr-panel" />,
}));

const branchId = "22222222-2222-2222-2222-222222222222";

function retailBranch(overrides: Record<string, unknown> = {}) {
  return {
    id: branchId,
    organizationId: "11111111-1111-1111-1111-111111111111",
    code: "EAST",
    name: "East Branch",
    branchType: "Retail",
    isPrimary: false,
    status: "Active",
    contactPhone: null,
    addressLine1: "Street 1",
    addressLine2: null,
    city: "Talisay",
    region: "Negros Occidental",
    postalCode: "6100",
    countryCode: "PH",
    timeZoneId: "Asia/Manila",
    customerOrderingEnabled: false,
    pickupEnabled: false,
    deliveryEnabled: false,
    onlineOrdersPaused: false,
    ...overrides,
  };
}

function warehouseBranch(overrides: Record<string, unknown> = {}) {
  return retailBranch({
    code: "ILOILO-JARO-WAREHOUSE",
    name: "Iloilo Jaro Warehouse",
    branchType: "Warehouse",
    isPrimary: false,
    ...overrides,
  });
}

function summaryFor(branch: Record<string, unknown>) {
  return {
    id: branch.id,
    assignedStaffCount: 0,
    activeDeviceCount: 0,
    areaName: null,
    pickupEnabled: false,
    deliveryEnabled: false,
    customerOrderingEnabled: false,
    pickupSectionsComplete: 0,
    pickupSectionsTotal: 2,
    deliverySectionsComplete: 0,
    deliverySectionsTotal: 5,
  };
}

function renderDetail(tab = "overview", pathSuffix = "") {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  const entry =
    tab === "overview"
      ? `/org/branches/${branchId}${pathSuffix}`
      : `/org/branches/${branchId}?tab=${tab}${pathSuffix}`;
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[entry]}>
        <Routes>
          <Route path="/org/branches/:branchId" element={<BranchManagementDetailPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe("BranchManagementDetailPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    canViewInventory.mockReturnValue(true);
    canManageInventory.mockReturnValue(true);
    canViewPurchasing.mockReturnValue(true);
    canUseWarehouseBranches.mockReturnValue(true);
    const retail = retailBranch();
    getOrganizationBranch.mockResolvedValue({ ok: true, value: retail });
    listBranchManagementSummaries.mockResolvedValue({
      ok: true,
      value: [summaryFor(retail)],
    });
    listPosDevices.mockResolvedValue({ ok: true, value: [] });
  });

  it("shows overview with secondary/lifecycle and fulfillment link for retail", async () => {
    renderDetail("overview");

    await waitFor(() => {
      expect(screen.getByTestId("branch-detail-code")).toHaveTextContent("EAST");
    });
    expect(screen.getByText("branches.mgmt.secondary")).toBeInTheDocument();
    expect(screen.getByTestId("branch-make-primary")).toBeInTheDocument();
    expect(screen.getByTestId("branch-suspend")).toBeInTheDocument();
    expect(screen.getByTestId("branch-mgmt-configure-fulfillment")).toHaveAttribute(
      "href",
      `/org/branches/${branchId}/fulfillment`,
    );
    expect(screen.getByTestId("branch-storefront-qr-panel")).toBeInTheDocument();
    expect(screen.getByTestId("branch-mgmt-tab-fulfillment")).toBeInTheDocument();
  });

  it("hides suspend/archive on primary retail branch", async () => {
    const primary = retailBranch({ isPrimary: true, code: "MAIN", name: "Main Branch" });
    getOrganizationBranch.mockResolvedValue({ ok: true, value: primary });
    listBranchManagementSummaries.mockResolvedValue({
      ok: true,
      value: [summaryFor(primary)],
    });

    renderDetail("overview");

    await waitFor(() => {
      expect(screen.getByTestId("branch-detail-primary-badge")).toBeInTheDocument();
    });
    expect(screen.queryByTestId("branch-make-primary")).not.toBeInTheDocument();
    expect(screen.queryByTestId("branch-suspend")).not.toBeInTheDocument();
    expect(screen.queryByTestId("branch-archive")).not.toBeInTheDocument();
  });

  it("renders staff access panel on staff tab", async () => {
    renderDetail("staff");
    await waitFor(() => {
      expect(screen.getByTestId("branch-staff-access-panel")).toBeInTheDocument();
    });
  });

  it("uses warehouse terminology and hides retail-only UI", async () => {
    const warehouse = warehouseBranch();
    getOrganizationBranch.mockResolvedValue({ ok: true, value: warehouse });
    listBranchManagementSummaries.mockResolvedValue({
      ok: true,
      value: [summaryFor(warehouse)],
    });

    renderDetail("overview");

    await waitFor(() => {
      expect(screen.getByTestId("branch-mgmt-detail")).toHaveAttribute(
        "data-branch-type",
        "Warehouse",
      );
    });
    expect(screen.getAllByText("branches.detail.overview.warehouse").length).toBeGreaterThan(0);
    expect(screen.getByText("branches.create.name.warehouse")).toBeInTheDocument();
    expect(screen.getByText("branches.detail.codeReadonly.warehouse")).toBeInTheDocument();
    expect(screen.getByText("branches.mgmt.devicesShort")).toBeInTheDocument();
    expect(screen.queryByText("branches.mgmt.primary")).not.toBeInTheDocument();
    expect(screen.queryByText("branches.mgmt.secondary")).not.toBeInTheDocument();
    expect(screen.queryByTestId("branch-mgmt-tab-fulfillment")).not.toBeInTheDocument();
    expect(screen.queryByTestId("branch-mgmt-configure-fulfillment")).not.toBeInTheDocument();
    expect(screen.queryByTestId("branch-storefront-qr-panel")).not.toBeInTheDocument();
    expect(screen.queryByText("branches.mgmt.pickup")).not.toBeInTheDocument();
    expect(screen.queryByText("branches.mgmt.delivery")).not.toBeInTheDocument();
    expect(screen.getByTestId("branch-warehouse-operations")).toBeInTheDocument();
    expect(screen.getByTestId("branch-mgmt-tab-overview")).toHaveTextContent(
      "branches.detail.overview.warehouse",
    );
    expect(screen.getByTestId("branch-mgmt-tab-details")).toHaveTextContent(
      "branches.detail.details.warehouse",
    );
    expect(screen.getByTestId("branch-warehouse-op-inventory")).toHaveAttribute("href", "/inventory");
    expect(screen.getByTestId("branch-warehouse-op-receive")).toHaveAttribute(
      "href",
      "/purchasing/receive-stock",
    );
    expect(screen.getByTestId("branch-warehouse-op-transfers")).toHaveAttribute(
      "href",
      "/inventory/transfers",
    );
    expect(screen.getByTestId("branch-warehouse-op-purchasing")).toHaveAttribute(
      "href",
      "/purchasing",
    );
  });

  it("normalizes warehouse ?tab=fulfillment away from retail fulfillment UI", async () => {
    const warehouse = warehouseBranch();
    getOrganizationBranch.mockResolvedValue({ ok: true, value: warehouse });
    listBranchManagementSummaries.mockResolvedValue({
      ok: true,
      value: [summaryFor(warehouse)],
    });

    renderDetail("fulfillment");

    await waitFor(() => {
      expect(screen.getByTestId("branch-mgmt-overview")).toBeInTheDocument();
    });
    expect(screen.queryByTestId("branch-fulfillment-summary")).not.toBeInTheDocument();
    expect(screen.queryByTestId("branch-storefront-qr-panel")).not.toBeInTheDocument();
  });

  it("hides unauthorized warehouse operation shortcuts", async () => {
    canViewInventory.mockReturnValue(false);
    canManageInventory.mockReturnValue(false);
    canViewPurchasing.mockReturnValue(false);
    const warehouse = warehouseBranch();
    getOrganizationBranch.mockResolvedValue({ ok: true, value: warehouse });
    listBranchManagementSummaries.mockResolvedValue({
      ok: true,
      value: [summaryFor(warehouse)],
    });

    renderDetail("overview");
    await waitFor(() => {
      expect(screen.getByTestId("branch-mgmt-overview")).toBeInTheDocument();
    });
    expect(screen.queryByTestId("branch-warehouse-operations")).not.toBeInTheDocument();
  });
});
