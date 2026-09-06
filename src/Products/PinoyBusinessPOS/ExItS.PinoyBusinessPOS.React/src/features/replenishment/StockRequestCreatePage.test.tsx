import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import { StockRequestCreatePage } from "@/features/replenishment/StockRequestCreatePage";
import * as supplyRoutesClient from "@/api/pos/pos-supply-routes-client";
import * as inventoryClient from "@/api/pos/pos-inventory-client";
import { TEST_BRANCH_A_ID, TEST_ORG_A_ID } from "@/test/session-context";

const WH_A = "11111111-1111-1111-1111-111111111111";
const RETAIL_B = "22222222-2222-2222-2222-222222222222";
const PRODUCT_ID = "33333333-3333-3333-3333-333333333333";

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => ({
    boundWorkspace: {
      organizationId: TEST_ORG_A_ID,
      organizationDisplayName: "Kizy Store",
      branchId: TEST_BRANCH_A_ID,
      branchName: "Pac Passi",
      branchType: "Retail",
      experience: "operations",
    },
    sessionGrant: {
      accessToken: "token",
      productAccessAllowed: true,
      mappedPosRoleCode: "Owner",
      productLocalRoleCode: "Owner",
    },
    workspaces: [
      {
        organizationId: TEST_ORG_A_ID,
        displayName: "Kizy Store",
        branches: [
          {
            branchId: TEST_BRANCH_A_ID,
            name: "Pac Passi",
            secondaryLine: "",
            isPrimary: true,
            isActive: true,
            branchType: "Retail",
          },
          {
            branchId: WH_A,
            name: "Panay Warehouse",
            secondaryLine: "",
            isPrimary: false,
            isActive: true,
            branchType: "Warehouse",
          },
          {
            branchId: RETAIL_B,
            name: "Other Retail",
            secondaryLine: "",
            isPrimary: false,
            isActive: true,
            branchType: "Retail",
          },
        ],
      },
    ],
  }),
}));

describe("StockRequestCreatePage warehouse-only sources", () => {
  beforeEach(() => {
    vi.spyOn(inventoryClient, "listInventory").mockResolvedValue({
      items: [
        {
          productId: PRODUCT_ID,
          organizationId: TEST_ORG_A_ID,
          name: "Sardines",
          unitOfMeasure: "pcs",
          productStatus: "Active",
          isTracked: true,
          onHandQuantity: 2,
          stockStatus: "Low",
          isLowStock: true,
          createdAtUtc: "2026-01-01T00:00:00Z",
          updatedAtUtc: "2026-01-01T00:00:00Z",
        },
      ],
      page: 1,
      pageSize: 100,
      totalCount: 1,
    });

    vi.spyOn(inventoryClient, "getOrganizationInventorySummary").mockResolvedValue({
      productId: PRODUCT_ID,
      productName: "Sardines",
      unitOfMeasure: "pcs",
      organizationOnHandQuantity: 50,
      organizationReservedQuantity: 0,
      organizationAvailableQuantity: 50,
      branches: [
        {
          branchId: WH_A,
          onHandQuantity: 40,
          reservedQuantity: 0,
          availableQuantity: 40,
        },
      ],
    });

    vi.spyOn(supplyRoutesClient, "listSupplyRoutesByDestination").mockResolvedValue([
      {
        routeId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
        organizationId: TEST_ORG_A_ID,
        sourceLocationId: WH_A,
        destinationLocationId: TEST_BRANCH_A_ID,
        isPreferred: true,
        isActive: true,
        notes: null,
        createdAtUtc: "2026-01-01T00:00:00Z",
        updatedAtUtc: "2026-01-01T00:00:00Z",
      },
      {
        routeId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
        organizationId: TEST_ORG_A_ID,
        sourceLocationId: RETAIL_B,
        destinationLocationId: TEST_BRANCH_A_ID,
        isPreferred: false,
        isActive: true,
        notes: null,
        createdAtUtc: "2026-01-01T00:00:00Z",
        updatedAtUtc: "2026-01-01T00:00:00Z",
      },
    ]);
  });

  it("shows only warehouse sources in Request Stock", async () => {
    render(
      <AppProviders>
        <MemoryRouter>
          <StockRequestCreatePage />
        </MemoryRouter>
      </AppProviders>,
    );

    await waitFor(() => expect(screen.getByTestId("stock-request-source")).toBeInTheDocument());
    const options = Array.from(
      (screen.getByTestId("stock-request-source") as HTMLSelectElement).options,
    ).map((o) => o.textContent ?? "");
    expect(options.some((t) => t.includes("Panay Warehouse"))).toBe(true);
    expect(options.some((t) => t.includes("Other Retail"))).toBe(false);
  });

  it("renders compact qty inputs and desktop grid headers", async () => {
    render(
      <AppProviders>
        <MemoryRouter>
          <StockRequestCreatePage />
        </MemoryRouter>
      </AppProviders>,
    );

    await waitFor(() => expect(screen.getByTestId("stock-request-grid-headers")).toBeInTheDocument());
    expect(screen.getByTestId("stock-request-grid-headers").textContent).toMatch(/Product/i);
    expect(screen.getByTestId("stock-request-grid-headers").textContent).toMatch(/Branch stock/i);
    expect(screen.getByTestId("stock-request-grid-headers").textContent).toMatch(/Warehouse available/i);
    expect(screen.getByTestId("stock-request-grid-headers").textContent).toMatch(/Request qty/i);

    const qty = await screen.findByTestId(`stock-request-qty-${PRODUCT_ID}`);
    expect(qty.className).toMatch(/w-24/);
    expect(qty.className).toMatch(/max-w-\[6rem\]/);
  });
});
