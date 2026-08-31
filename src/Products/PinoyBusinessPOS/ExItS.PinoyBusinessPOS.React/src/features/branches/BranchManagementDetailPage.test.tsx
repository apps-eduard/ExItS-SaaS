import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { BranchManagementDetailPage } from "@/features/branches/BranchManagementDetailPage";

vi.mock("@/access/pos-capabilities", () => ({
  canManageBranchFulfillment: () => true,
  canInviteOrganizationStaff: () => true,
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
    boundWorkspace: { organizationId: "11111111-1111-1111-1111-111111111111" },
    sessionGrant: { productRole: "Owner", organizationManagementAuthority: true },
  }),
}));

const getOrganizationBranch = vi.fn();
const listBranchManagementSummaries = vi.fn();
const listPosDevices = vi.fn();

vi.mock("@/api/platform/organization-branches-client", () => ({
  getOrganizationBranch: (...args: unknown[]) => getOrganizationBranch(...args),
  listBranchManagementSummaries: (...args: unknown[]) => listBranchManagementSummaries(...args),
  updateOrganizationBranchDetails: vi.fn(),
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
  BranchDetailsForm: () => <div data-testid="branch-details-form" />,
}));

const branchId = "22222222-2222-2222-2222-222222222222";

function renderDetail(tab = "overview") {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[`/org/branches/${branchId}?tab=${tab}`]}>
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
    getOrganizationBranch.mockResolvedValue({
      ok: true,
      value: {
        id: branchId,
        organizationId: "11111111-1111-1111-1111-111111111111",
        code: "EAST",
        name: "East Branch",
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
      },
    });
    listBranchManagementSummaries.mockResolvedValue({
      ok: true,
      value: [
        {
          id: branchId,
          assignedStaffCount: 2,
          activeDeviceCount: 1,
          pickupEnabled: false,
          deliveryEnabled: false,
          customerOrderingEnabled: false,
          pickupSectionsComplete: 0,
          pickupSectionsTotal: 2,
          deliverySectionsComplete: 0,
          deliverySectionsTotal: 5,
        },
      ],
    });
    listPosDevices.mockResolvedValue({ ok: true, value: [] });
  });

  it("shows overview with secondary/lifecycle and fulfillment link", async () => {
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
  });

  it("hides suspend/archive on primary branch", async () => {
    getOrganizationBranch.mockResolvedValue({
      ok: true,
      value: {
        id: branchId,
        organizationId: "11111111-1111-1111-1111-111111111111",
        code: "MAIN",
        name: "Main Branch",
        isPrimary: true,
        status: "Active",
        contactPhone: null,
        addressLine1: null,
        addressLine2: null,
        city: null,
        region: null,
        postalCode: null,
        countryCode: "PH",
        timeZoneId: "Asia/Manila",
        customerOrderingEnabled: true,
        pickupEnabled: true,
        deliveryEnabled: false,
        onlineOrdersPaused: false,
      },
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
});
