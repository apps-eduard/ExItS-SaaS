import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import { OrgEssentialsPage } from "@/features/role/OrgEssentialsPage";
import { AdminManagementShell } from "@/features/admin/AdminManagementShell";
import * as posReportingClient from "@/api/pos/pos-reporting-client";
import * as branchesClient from "@/api/platform/organization-branches-client";
import * as areasClient from "@/api/platform/organization-areas-client";
import * as membersClient from "@/api/platform/organization-members-client";
import * as devicesClient from "@/api/platform/pos-devices-client";
import { TEST_ORG_A_ID } from "@/test/session-context";

const getManagementOverview = vi.spyOn(posReportingClient, "getManagementOverview");
const listBranchManagementSummaries = vi.spyOn(branchesClient, "listBranchManagementSummaries");
const getBranchCapacity = vi.spyOn(branchesClient, "getBranchCapacity");
const listOrganizationAreas = vi.spyOn(areasClient, "listOrganizationAreas");
const listOrganizationMembers = vi.spyOn(membersClient, "listOrganizationMembers");
const getPosDeviceCapacity = vi.spyOn(devicesClient, "getPosDeviceCapacity");

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => ({
    boundWorkspace: {
      organizationId: TEST_ORG_A_ID,
      organizationDisplayName: "Kizy Store",
      branchId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      branchName: "Main Branch",
      experience: "manage_business" as const,
    },
    sessionGrant: {
      accessToken: "token",
      productAccessAllowed: true,
      mappedPosRoleCode: "Owner",
      productLocalRoleCode: "Owner",
      membershipRole: "OrganizationOwner",
      organizationManagementAuthority: true,
      featureCodes: ["store-area-management", "store-warehouse"],
      grantedFeatureCodes: [],
    },
  }),
}));

vi.mock("@/connectivity/browser-online", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/connectivity/browser-online")>();
  return {
    ...actual,
    useBrowserOnline: () => true,
  };
});

function renderOverview() {
  return render(
    <AppProviders>
      <MemoryRouter>
        <OrgEssentialsPage />
      </MemoryRouter>
    </AppProviders>,
  );
}

function renderShell(path = "/org") {
  return render(
    <AppProviders>
      <MemoryRouter initialEntries={[path]}>
        <AdminManagementShell>
          <div data-testid="shell-child">Child</div>
        </AdminManagementShell>
      </MemoryRouter>
    </AppProviders>,
  );
}

describe("POS-ADMIN-OVERVIEW-V2", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    getManagementOverview.mockResolvedValue({
      businessDate: "2026-09-04",
      todaySalesTotal: 0,
      todaySaleCount: 0,
      todayCashSalesTotal: 0,
      todayUtangSalesTotal: 0,
      todayPaymentsReceived: 0,
      openUtangOutstanding: 0,
      lowStockProductCount: 0,
      expiredLotCount: 0,
      nearExpiryLotCount: 0,
      pendingTransferCount: 0,
      openShiftCount: 1,
      activeRegisterCount: 1,
    });
    listBranchManagementSummaries.mockResolvedValue({
      ok: true,
      value: [
        {
          id: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
          organizationId: TEST_ORG_A_ID,
          code: "MAIN",
          name: "Main",
          branchType: "Retail",
          isPrimary: true,
          status: "Active",
          city: null,
          region: null,
          addressLine1: null,
          pickupEnabled: false,
          deliveryEnabled: false,
          customerOrderingEnabled: false,
          assignedStaffCount: 2,
          activeDeviceCount: 1,
          areaId: null,
          areaName: null,
          pickupSectionsComplete: 0,
          pickupSectionsTotal: 0,
          deliverySectionsComplete: 0,
          deliverySectionsTotal: 0,
        },
        {
          id: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
          organizationId: TEST_ORG_A_ID,
          code: "WH1",
          name: "Warehouse",
          branchType: "Warehouse",
          isPrimary: false,
          status: "Active",
          city: null,
          region: null,
          addressLine1: null,
          pickupEnabled: false,
          deliveryEnabled: false,
          customerOrderingEnabled: false,
          assignedStaffCount: 0,
          activeDeviceCount: 0,
          areaId: null,
          areaName: null,
          pickupSectionsComplete: 0,
          pickupSectionsTotal: 0,
          deliverySectionsComplete: 0,
          deliverySectionsTotal: 0,
        },
      ],
    });
    getBranchCapacity.mockResolvedValue({ ok: true, value: { used: 2, allowed: 10 } });
    listOrganizationAreas.mockResolvedValue({
      ok: true,
      value: {
        areas: [],
        unassignedBranchCount: 0,
        activeAreaCount: 1,
        maxAreas: 3,
      },
    });
    listOrganizationMembers.mockResolvedValue({
      ok: true,
      members: [{ membershipId: "m1" } as never, { membershipId: "m2" } as never],
    });
    getPosDeviceCapacity.mockResolvedValue({ ok: true, value: { used: 2, allowed: 10 } });
  });

  it("renders Today and Plan sections in a shared top grid with Usage card", async () => {
    renderOverview();

    await waitFor(() => {
      expect(screen.getByTestId("org-overview-top")).toBeInTheDocument();
      expect(screen.getByTestId("org-group-today")).toBeInTheDocument();
      expect(screen.getByTestId("org-group-plan")).toBeInTheDocument();
      expect(screen.getByTestId("org-plan-usage")).toBeInTheDocument();
    });

    expect(screen.getByTestId("org-group-today")).toHaveTextContent("Today");
    expect(screen.getByTestId("org-group-plan")).toHaveTextContent("Plan");
    expect(screen.getByTestId("org-plan-usage")).toHaveTextContent("Usage");
    expect(screen.getByTestId("org-plan-usage")).not.toHaveTextContent("Plan usage");
    expect(screen.queryByTestId("org-plan-usage-mobile")).not.toBeInTheDocument();

    expect(screen.getByTestId("org-plan-capacity-branches")).toHaveTextContent("2 / 10");
    expect(screen.getByTestId("org-plan-capacity-areas")).toHaveTextContent("1 / 3");
    expect(screen.getByTestId("org-plan-capacity-devices")).toHaveTextContent("2 / 10");
  });

  it("does not mount shell Plan Usage panel on Overview", async () => {
    render(
      <AppProviders>
        <MemoryRouter initialEntries={["/org"]}>
          <AdminManagementShell>
            <OrgEssentialsPage />
          </AdminManagementShell>
        </MemoryRouter>
      </AppProviders>,
    );

    await waitFor(() => {
      expect(screen.getByTestId("org-plan-usage")).toBeInTheDocument();
    });

    expect(screen.queryByTestId("admin-xl-context")).not.toBeInTheDocument();
    expect(screen.queryByTestId("admin-context-panel")).not.toBeInTheDocument();
  });

  it("keeps shell Plan Usage panel available on manage routes", () => {
    renderShell("/org/manage/branches");
    expect(screen.getByTestId("admin-xl-context")).toBeInTheDocument();
  });

  it("renders command-center overview without duplicated nav tile walls", async () => {
    renderOverview();

    await waitFor(() => {
      expect(screen.getByTestId("org-essentials-page")).toBeInTheDocument();
      expect(screen.getByTestId("org-kpi-today-sales")).toBeInTheDocument();
    });

    expect(screen.getByTestId("org-attention-clear")).toBeInTheDocument();
    expect(screen.getByTestId("org-glance-section")).toBeInTheDocument();
    expect(screen.getByTestId("org-quick-actions")).toBeInTheDocument();
    expect(screen.getByTestId("open-org-dashboard")).toBeInTheDocument();
    expect(screen.getByTestId("open-org-reports")).toBeInTheDocument();
    expect(screen.getByTestId("org-action-branches")).toBeInTheDocument();
    expect(screen.getByTestId("org-action-staff")).toBeInTheDocument();

    expect(screen.queryByTestId("admin-overview-organization")).not.toBeInTheDocument();
    expect(screen.queryByTestId("admin-overview-business")).not.toBeInTheDocument();
    expect(screen.queryByTestId("admin-overview-security")).not.toBeInTheDocument();
    expect(screen.queryByTestId("open-switch-workspace")).not.toBeInTheDocument();
    expect(document.body.textContent).not.toMatch(/Today\?s/);
  });

  it("shows attention items only when actionable", async () => {
    getManagementOverview.mockResolvedValue({
      businessDate: "2026-09-04",
      todaySalesTotal: 1250,
      todaySaleCount: 8,
      todayCashSalesTotal: 900,
      todayUtangSalesTotal: 350,
      todayPaymentsReceived: 100,
      openUtangOutstanding: 500,
      lowStockProductCount: 3,
      expiredLotCount: 1,
      nearExpiryLotCount: 2,
      pendingTransferCount: 0,
      openShiftCount: 1,
      activeRegisterCount: 1,
    });

    renderOverview();

    await waitFor(() => {
      expect(screen.getByTestId("org-attention-low-stock")).toBeInTheDocument();
    });

    expect(screen.getByTestId("org-attention-near-expiry")).toBeInTheDocument();
    expect(screen.getByTestId("org-attention-expired")).toBeInTheDocument();
    expect(screen.getByTestId("org-attention-utang")).toBeInTheDocument();
    expect(screen.queryByTestId("org-attention-clear")).not.toBeInTheDocument();
    expect(screen.getByTestId("org-kpi-today-sales")).toHaveTextContent("1,250");
  });

  it("renders organization glance counts from real APIs", async () => {
    renderOverview();

    await waitFor(() => {
      expect(screen.getByTestId("org-glance-locations")).toHaveTextContent("2");
    });

    expect(screen.getByTestId("org-glance-locations")).toHaveTextContent("1 Retail");
    expect(screen.getByTestId("org-glance-locations")).toHaveTextContent("1 Warehouse");
    expect(screen.getByTestId("org-glance-staff")).toHaveTextContent("2");
    expect(screen.getByTestId("org-glance-devices")).toHaveTextContent("2");
  });

  it("keeps Switch workspace in Admin shell sidebar across admin routes", async () => {
    renderShell("/dashboard");

    expect(screen.getByTestId("admin-sidebar-switch-workspace")).toBeInTheDocument();
    expect(screen.getByTestId("admin-sidebar-switch-workspace")).toHaveAttribute("href", "/workspace");

    renderShell("/org/branches");
    expect(screen.getAllByTestId("admin-sidebar-switch-workspace").length).toBeGreaterThan(0);
  });
});
