import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { OrgAreasPage } from "@/features/areas/OrgAreasPage";
import { OrgAreaDetailPage } from "@/features/areas/OrgAreaDetailPage";

vi.mock("@/access/pos-capabilities", () => ({
  canInviteOrganizationStaff: () => true,
  canManageStoreAreas: () => true,
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

const listOrganizationAreas = vi.fn();
const createOrganizationArea = vi.fn();
const updateOrganizationArea = vi.fn();
const archiveOrganizationArea = vi.fn();
const setBranchArea = vi.fn();
const listBranchManagementSummaries = vi.fn();

vi.mock("@/api/platform/organization-areas-client", () => ({
  listOrganizationAreas: (...args: unknown[]) => listOrganizationAreas(...args),
  createOrganizationArea: (...args: unknown[]) => createOrganizationArea(...args),
  updateOrganizationArea: (...args: unknown[]) => updateOrganizationArea(...args),
  archiveOrganizationArea: (...args: unknown[]) => archiveOrganizationArea(...args),
  setBranchArea: (...args: unknown[]) => setBranchArea(...args),
}));

vi.mock("@/api/platform/organization-branches-client", () => ({
  listBranchManagementSummaries: (...args: unknown[]) => listBranchManagementSummaries(...args),
}));

const orgId = "11111111-1111-1111-1111-111111111111";
const areaId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
const retailId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
const warehouseId = "cccccccc-cccc-cccc-cccc-cccccccccccc";

function areaList() {
  return {
    areas: [
      {
        id: areaId,
        organizationId: orgId,
        name: "Panay North",
        code: "PNY-N",
        status: "Active",
        branchCount: 0,
      },
    ],
    unassignedBranchCount: 2,
    activeAreaCount: 1,
    maxAreas: 3,
  };
}

function branches(overrides: { areaId?: string | null } = {}) {
  const assignedAreaId = overrides.areaId === undefined ? null : overrides.areaId;
  return [
    {
      id: retailId,
      organizationId: orgId,
      code: "MAIN",
      name: "Main Branch",
      branchType: "Retail",
      isPrimary: true,
      status: "Active",
      areaId: assignedAreaId,
      areaName: assignedAreaId ? "Panay North" : null,
    },
    {
      id: warehouseId,
      organizationId: orgId,
      code: "WH1",
      name: "Iloilo Warehouse",
      branchType: "Warehouse",
      isPrimary: false,
      status: "Active",
      areaId: assignedAreaId,
      areaName: assignedAreaId ? "Panay North" : null,
    },
  ];
}

function renderList() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={["/org/areas"]}>
        <Routes>
          <Route path="/org/areas" element={<OrgAreasPage />} />
          <Route path="/org/areas/:areaId" element={<div data-testid="area-detail-route" />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

function renderDetail() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[`/org/areas/${areaId}`]}>
        <Routes>
          <Route path="/org/areas/:areaId" element={<OrgAreaDetailPage />} />
          <Route path="/org/areas" element={<div data-testid="areas-list-route" />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe("OrgAreasPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    listOrganizationAreas.mockResolvedValue({ ok: true, value: areaList() });
    listBranchManagementSummaries.mockResolvedValue({ ok: true, value: branches() });
    createOrganizationArea.mockResolvedValue({
      ok: true,
      value: {
        id: "dddddddd-dddd-dddd-dddd-dddddddddddd",
        organizationId: orgId,
        name: "Panay Central",
        code: "PNY-C",
        status: "Active",
        branchCount: 0,
      },
    });
  });

  it("shows location breakdown and opens add-area sheet", async () => {
    const user = userEvent.setup();
    renderList();

    expect(await screen.findByTestId(`org-area-type-breakdown-${areaId}`)).toHaveTextContent(
      "areas.locationBreakdown",
    );
    expect(screen.queryByTestId("org-areas-form")).not.toBeInTheDocument();
    expect(screen.getByTestId("org-areas-unassigned")).toHaveTextContent(
      "areas.unassignedCount",
    );

    await user.click(screen.getByTestId("org-areas-add"));
    const form = await screen.findByTestId("org-areas-form");
    expect(within(form).getByTestId("org-areas-name")).toBeInTheDocument();
    expect(within(form).getByTestId("org-areas-submit")).toHaveTextContent(
      "areas.create.submit",
    );
  });
});

describe("OrgAreaDetailPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    listOrganizationAreas.mockResolvedValue({ ok: true, value: areaList() });
    listBranchManagementSummaries.mockResolvedValue({
      ok: true,
      value: branches({ areaId: null }),
    });
    setBranchArea.mockResolvedValue({ ok: true, value: undefined });
    updateOrganizationArea.mockResolvedValue({
      ok: true,
      value: areaList().areas[0],
    });
  });

  it("uses assign/remove wording and compact edit", async () => {
    const user = userEvent.setup();
    renderDetail();

    expect(await screen.findByTestId("org-area-branches")).toHaveTextContent(
      "areas.detail.assigned",
    );
    expect(screen.getByTestId("org-area-no-branches")).toHaveTextContent(
      "areas.detail.noLocations",
    );
    expect(screen.getByTestId("org-area-available")).toHaveTextContent("areas.detail.available");
    expect(screen.getByTestId(`org-area-add-${retailId}`)).toHaveTextContent(
      "areas.detail.assign",
    );
    expect(screen.getByTestId(`org-area-available-${warehouseId}`)).toHaveTextContent(
      "branches.type.warehouse",
    );

    expect(screen.getByTestId("org-area-summary")).toBeInTheDocument();
    expect(screen.queryByTestId("org-area-save")).not.toBeInTheDocument();

    await user.click(screen.getByTestId("org-area-edit"));
    expect(screen.getByTestId("org-area-save")).toHaveTextContent("areas.save");
  });

  it("confirms assign for unassigned and transfer for already assigned locations", async () => {
    const user = userEvent.setup();
    const otherAreaId = "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee";
    listBranchManagementSummaries.mockResolvedValue({
      ok: true,
      value: [
        {
          id: retailId,
          organizationId: orgId,
          code: "MAIN",
          name: "Main Branch",
          branchType: "Retail",
          isPrimary: true,
          status: "Active",
          areaId: null,
          areaName: null,
        },
        {
          id: warehouseId,
          organizationId: orgId,
          code: "WH1",
          name: "Iloilo Warehouse",
          branchType: "Warehouse",
          isPrimary: false,
          status: "Active",
          areaId: otherAreaId,
          areaName: "Pasi Norte",
        },
      ],
    });
    renderDetail();

    await user.click(await screen.findByTestId(`org-area-add-${retailId}`));
    expect(screen.getByTestId("org-area-assign-confirm")).toBeInTheDocument();
    await user.click(screen.getByTestId("org-area-assign-confirm-confirm"));
    expect(setBranchArea).toHaveBeenCalledWith(orgId, retailId, areaId);

    await user.click(await screen.findByTestId(`org-area-transfer-${warehouseId}`));
    expect(screen.getByTestId("org-area-transfer-confirm")).toBeInTheDocument();
    expect(screen.getByTestId("org-area-transfer-confirm")).toHaveTextContent(
      "areas.detail.transferConfirmTitle",
    );
    await user.click(screen.getByTestId("org-area-transfer-confirm-confirm"));
    expect(setBranchArea).toHaveBeenCalledWith(orgId, warehouseId, areaId);
  });
});
