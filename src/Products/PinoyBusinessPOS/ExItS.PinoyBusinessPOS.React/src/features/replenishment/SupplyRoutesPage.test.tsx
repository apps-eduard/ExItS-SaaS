import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import { SupplyRoutesPage } from "@/features/replenishment/SupplyRoutesPage";
import * as branchesClient from "@/api/platform/organization-branches-client";
import * as areasClient from "@/api/platform/organization-areas-client";
import * as supplyRoutesClient from "@/api/pos/pos-supply-routes-client";
import type { BranchManagementSummaryItemDto } from "@/api/platform/organization-branches-client";
import { TEST_ORG_A_ID } from "@/test/session-context";

const WH_A = "11111111-1111-1111-1111-111111111111";
const WH_B = "22222222-2222-2222-2222-222222222222";
const BRANCH_MAIN = "33333333-3333-3333-3333-333333333333";
const BRANCH_PASSI = "44444444-4444-4444-4444-444444444444";
const AREA_NORTE = "55555555-5555-5555-5555-555555555555";
const RETAIL_ONLY = "66666666-6666-6666-6666-666666666666";

function summary(
  partial: Partial<BranchManagementSummaryItemDto> &
    Pick<BranchManagementSummaryItemDto, "id" | "name" | "branchType" | "areaId" | "areaName">,
): BranchManagementSummaryItemDto {
  return {
    organizationId: TEST_ORG_A_ID,
    code: partial.code ?? "X",
    status: "Active",
    isPrimary: false,
    city: null,
    region: null,
    addressLine1: null,
    pickupEnabled: false,
    deliveryEnabled: false,
    customerOrderingEnabled: false,
    assignedStaffCount: 0,
    activeDeviceCount: 0,
    pickupSectionsComplete: 0,
    pickupSectionsTotal: 0,
    deliverySectionsComplete: 0,
    deliverySectionsTotal: 0,
    ...partial,
  };
}

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => ({
    boundWorkspace: {
      organizationId: TEST_ORG_A_ID,
      organizationDisplayName: "Kizy Store",
      branchId: null,
      branchName: null,
      branchType: null,
      experience: "manage_business",
    },
    sessionGrant: {
      accessToken: "token",
      productAccessAllowed: true,
      mappedPosRoleCode: "Owner",
      productLocalRoleCode: "Owner",
    },
  }),
}));

describe("SupplyRoutesPage warehouse-first", () => {
  beforeEach(() => {
    vi.spyOn(branchesClient, "listBranchManagementSummaries").mockResolvedValue({
      ok: true,
      value: [
        summary({
          id: WH_A,
          name: "Panay Warehouse",
          code: "PW",
          branchType: "Warehouse",
          areaId: null,
          areaName: null,
        }),
        summary({
          id: WH_B,
          name: "Central Warehouse",
          code: "CW",
          branchType: "Warehouse",
          areaId: null,
          areaName: null,
        }),
        summary({
          id: BRANCH_MAIN,
          name: "Main Branch",
          code: "MB",
          branchType: "Retail",
          areaId: AREA_NORTE,
          areaName: "Pasi Norte",
          isPrimary: true,
        }),
        summary({
          id: BRANCH_PASSI,
          name: "Pac Passi",
          code: "PP",
          branchType: "Retail",
          areaId: AREA_NORTE,
          areaName: "Pasi Norte",
        }),
        summary({
          id: RETAIL_ONLY,
          name: "Solo Retail",
          code: "SR",
          branchType: "Retail",
          areaId: null,
          areaName: null,
        }),
      ],
    });

    vi.spyOn(areasClient, "listOrganizationAreas").mockResolvedValue({
      ok: true,
      value: {
        areas: [
          {
            id: AREA_NORTE,
            organizationId: TEST_ORG_A_ID,
            name: "Pasi Norte",
            code: "PN",
            status: "Active",
            branchCount: 2,
          },
        ],
        unassignedBranchCount: 1,
        activeAreaCount: 1,
        maxAreas: 50,
      },
    });

    vi.spyOn(supplyRoutesClient, "listSupplyRoutes").mockResolvedValue([
      {
        routeId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
        organizationId: TEST_ORG_A_ID,
        sourceLocationId: WH_A,
        destinationLocationId: BRANCH_MAIN,
        isPreferred: true,
        isActive: true,
        notes: null,
        createdAtUtc: "2026-01-01T00:00:00Z",
        updatedAtUtc: "2026-01-01T00:00:00Z",
      },
    ]);

    vi.spyOn(supplyRoutesClient, "upsertSupplyCoverageBySource").mockResolvedValue([]);
  });

  it("lists warehouses as source cards only and never retail sources", async () => {
    render(
      <AppProviders>
        <MemoryRouter>
          <SupplyRoutesPage />
        </MemoryRouter>
      </AppProviders>,
    );

    await waitFor(() => expect(screen.getByTestId("supply-routes-warehouse-list")).toBeInTheDocument());
    expect(screen.getByTestId(`supply-warehouse-card-${WH_A}`)).toBeInTheDocument();
    expect(screen.getByTestId(`supply-warehouse-card-${WH_B}`)).toBeInTheDocument();
    expect(screen.queryByTestId(`supply-warehouse-card-${BRANCH_MAIN}`)).not.toBeInTheDocument();
    expect(screen.queryByTestId(`supply-warehouse-card-${RETAIL_ONLY}`)).not.toBeInTheDocument();
    expect(screen.queryByText("All types")).not.toBeInTheDocument();
  });

  it("opens manage coverage with area bulk select and individual overrides", async () => {
    const user = userEvent.setup();
    render(
      <AppProviders>
        <MemoryRouter>
          <SupplyRoutesPage />
        </MemoryRouter>
      </AppProviders>,
    );

    await waitFor(() => expect(screen.getByTestId(`supply-manage-coverage-${WH_A}`)).toBeInTheDocument());
    await user.click(screen.getByTestId(`supply-manage-coverage-${WH_A}`));

    await waitFor(() => expect(screen.getByTestId("supply-coverage-manage")).toBeInTheDocument());
    expect(screen.getByTestId("supply-coverage-panel")).toHaveAttribute(
      "data-presentation",
      "sheet-mobile-dialog-desktop",
    );

    const area = screen.getByTestId(`supply-area-${AREA_NORTE}`) as HTMLInputElement;
    expect(area.indeterminate).toBe(true);

    await user.click(area);
    expect((screen.getByTestId(`supply-dest-${BRANCH_MAIN}`) as HTMLInputElement).checked).toBe(true);
    expect((screen.getByTestId(`supply-dest-${BRANCH_PASSI}`) as HTMLInputElement).checked).toBe(true);

    await user.click(screen.getByTestId(`supply-dest-${BRANCH_PASSI}`));
    expect((screen.getByTestId(`supply-dest-${BRANCH_PASSI}`) as HTMLInputElement).checked).toBe(false);
    expect((screen.getByTestId(`supply-dest-${BRANCH_MAIN}`) as HTMLInputElement).checked).toBe(true);

    expect(screen.getByText("Unassigned")).toBeInTheDocument();
    expect(screen.getByTestId(`supply-dest-${RETAIL_ONLY}`)).toBeInTheDocument();
    expect(screen.getByText("Warehouse replenishment")).toBeInTheDocument();
    expect(screen.getByTestId(`supply-wh-dest-${WH_B}`)).toBeInTheDocument();
  });

  it("does not drop hidden selections while searching", async () => {
    const user = userEvent.setup();
    render(
      <AppProviders>
        <MemoryRouter>
          <SupplyRoutesPage />
        </MemoryRouter>
      </AppProviders>,
    );

    await waitFor(() => expect(screen.getByTestId(`supply-manage-coverage-${WH_A}`)).toBeInTheDocument());
    await user.click(screen.getByTestId(`supply-manage-coverage-${WH_A}`));
    await waitFor(() => expect(screen.getByTestId("supply-coverage-search")).toBeInTheDocument());

    await user.click(screen.getByTestId(`supply-dest-${BRANCH_PASSI}`));
    await user.type(screen.getByTestId("supply-coverage-search"), "Solo");
    expect(screen.queryByTestId(`supply-dest-${BRANCH_PASSI}`)).not.toBeInTheDocument();
    await user.clear(screen.getByTestId("supply-coverage-search"));
    await waitFor(() => expect(screen.getByTestId(`supply-dest-${BRANCH_PASSI}`)).toBeInTheDocument());
    expect((screen.getByTestId(`supply-dest-${BRANCH_PASSI}`) as HTMLInputElement).checked).toBe(true);
  });

  it("saves coverage via by-source upsert", async () => {
    const user = userEvent.setup();
    render(
      <AppProviders>
        <MemoryRouter>
          <SupplyRoutesPage />
        </MemoryRouter>
      </AppProviders>,
    );

    await waitFor(() => expect(screen.getByTestId(`supply-manage-coverage-${WH_A}`)).toBeInTheDocument());
    await user.click(screen.getByTestId(`supply-manage-coverage-${WH_A}`));
    await waitFor(() => expect(screen.getByTestId("supply-coverage-save")).toBeInTheDocument());
    await user.click(screen.getByTestId("supply-coverage-save"));

    await waitFor(() =>
      expect(supplyRoutesClient.upsertSupplyCoverageBySource).toHaveBeenCalledWith(
        expect.objectContaining({ organizationId: TEST_ORG_A_ID }),
        WH_A,
        expect.arrayContaining([BRANCH_MAIN]),
        expect.any(Array),
      ),
    );
  });
});
