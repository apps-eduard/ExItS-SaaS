import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
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
const listOrganizationAreas = vi.fn();

vi.mock("@/api/platform/organization-branches-client", () => ({
  listBranchManagementSummaries: (...args: unknown[]) => listBranchManagementSummaries(...args),
  getBranchCapacity: (...args: unknown[]) => getBranchCapacity(...args),
}));

vi.mock("@/api/platform/organization-areas-client", () => ({
  listOrganizationAreas: (...args: unknown[]) => listOrganizationAreas(...args),
}));

const branchId = "22222222-2222-2222-2222-222222222222";

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
    listOrganizationAreas.mockResolvedValue({
      ok: true,
      value: { areas: [{ id: "area-1", name: "North" }] },
    });
    listBranchManagementSummaries.mockResolvedValue({
      ok: true,
      value: [
        {
          id: branchId,
          organizationId: "11111111-1111-1111-1111-111111111111",
          code: "MAIN",
          name: "Main Branch",
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
    expect(screen.getByTestId(`branch-mgmt-primary-${branchId}`)).toHaveTextContent(
      "branches.mgmt.primary",
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
    expect(screen.getByTestId(`branch-mgmt-view-qr-${branchId}`)).toHaveAttribute(
      "href",
      `/org/branches/${branchId}?focus=qr#branch-storefront-qr`,
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
  });
});
