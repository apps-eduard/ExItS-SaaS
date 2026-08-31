import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter } from "react-router-dom";
import { BranchManagementListPage } from "@/features/branches/BranchManagementListPage";

vi.mock("@/access/pos-capabilities", () => ({
  canManageBranchFulfillment: () => true,
  canInviteOrganizationStaff: () => true,
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

vi.mock("@/api/platform/organization-branches-client", () => ({
  listBranchManagementSummaries: (...args: unknown[]) => listBranchManagementSummaries(...args),
  getBranchCapacity: (...args: unknown[]) => getBranchCapacity(...args),
}));

function renderPage() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <BranchManagementListPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe("BranchManagementListPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    getBranchCapacity.mockResolvedValue({ ok: true, value: { used: 2, allowed: 3 } });
    listBranchManagementSummaries.mockResolvedValue({
      ok: true,
      value: [
        {
          id: "22222222-2222-2222-2222-222222222222",
          organizationId: "11111111-1111-1111-1111-111111111111",
          code: "MAIN",
          name: "Main Branch",
          isPrimary: true,
          status: "Active",
          city: "Bacolod",
          region: "Negros Occidental",
          addressLine1: "Lacson St",
          pickupEnabled: true,
          deliveryEnabled: false,
          customerOrderingEnabled: false,
          assignedStaffCount: 4,
          activeDeviceCount: 2,
          pickupSectionsComplete: 2,
          pickupSectionsTotal: 2,
          deliverySectionsComplete: 0,
          deliverySectionsTotal: 5,
        },
      ],
    });
  });

  it("renders capacity, primary badge, and no Delete action", async () => {
    renderPage();

    await waitFor(() => {
      expect(screen.getByTestId("branch-mgmt-capacity")).toBeInTheDocument();
    });
    expect(screen.getByTestId("branch-mgmt-capacity-value")).toHaveTextContent(
      "branches.mgmt.capacityOf",
    );
    expect(screen.getByTestId("branch-mgmt-primary-22222222-2222-2222-2222-222222222222")).toHaveTextContent(
      "branches.mgmt.primary",
    );
    expect(screen.queryByText(/Delete/i)).not.toBeInTheDocument();
    expect(screen.getByTestId("branch-mgmt-add")).toBeInTheDocument();
  });
});
