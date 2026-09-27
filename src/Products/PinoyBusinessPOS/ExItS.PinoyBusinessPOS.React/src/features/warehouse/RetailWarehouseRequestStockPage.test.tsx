import { beforeEach, describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import { RetailWarehouseRequestStockPage } from "@/features/warehouse/RetailWarehouseRequestStockPage";
import * as stockRequestsClient from "@/api/pos/pos-stock-requests-client";
import * as catalogClient from "@/api/pos/pos-catalog-client";
import * as supplyRoutesClient from "@/api/pos/pos-supply-routes-client";
import { TEST_BRANCH_A_ID, TEST_ORG_A_ID } from "@/test/session-context";

const WH_A = "11111111-1111-1111-1111-111111111111";
const PRODUCT_A = "33333333-3333-3333-3333-333333333333";
const PRODUCT_B = "44444444-4444-4444-4444-444444444444";
const PRODUCT_W = "55555555-5555-5555-5555-555555555555";
const PRODUCT_OOS = "66666666-6666-6666-6666-666666666666";

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
      organizationManagementAuthority: true,
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
        ],
      },
    ],
  }),
}));

vi.mock("@/features/warehouse/useRetailWarehouseResolve", () => ({
  useRetailWarehouseResolve: () => ({
    workspace: { organizationId: TEST_ORG_A_ID, branchId: TEST_BRANCH_A_ID },
    boundWorkspace: {
      organizationId: TEST_ORG_A_ID,
      branchId: TEST_BRANCH_A_ID,
      branchName: "Pac Passi",
      branchType: "Retail",
    },
    orgBranches: [],
    routesQuery: { data: [], isPending: false, isError: false },
    resolveState: {
      kind: "ready",
      supplyWarehouseId: WH_A,
      supplyWarehouseName: "Panay Warehouse",
      isPreferred: true,
    },
    isLoading: false,
    isError: false,
  }),
}));

const catalogItems = [
  {
    productId: PRODUCT_A,
    name: "Sardines",
    sku: "SAR-1",
    unitOfMeasure: "pcs",
    branchOnHandQuantity: 2,
    warehouseAvailableQuantity: 40,
    isLowStock: true,
    isTracked: true,
    sellingMode: "PerItem",
    warehouseUnitCost: 12.5,
    branchEffectiveSellingPrice: 18,
  },
  {
    productId: PRODUCT_B,
    name: "Battery AA Pack",
    sku: "BAT-AA",
    unitOfMeasure: "Pack",
    branchOnHandQuantity: 0,
    warehouseAvailableQuantity: 2,
    isLowStock: false,
    isTracked: true,
    sellingMode: "PerItem",
    warehouseUnitCost: 50,
    branchEffectiveSellingPrice: 75,
  },
  {
    productId: PRODUCT_W,
    name: "Banana Lakatan",
    sku: "PH-FRU-BANANA",
    unitOfMeasure: "Kilogram",
    branchOnHandQuantity: 1.2,
    warehouseAvailableQuantity: 55,
    isLowStock: false,
    isTracked: true,
    sellingMode: "ByWeight",
    warehouseUnitCost: 100,
    branchEffectiveSellingPrice: 140,
  },
  {
    productId: PRODUCT_OOS,
    name: "Empty Stock Item",
    sku: "EMPTY-1",
    unitOfMeasure: "pcs",
    branchOnHandQuantity: 0,
    warehouseAvailableQuantity: 0,
    isLowStock: true,
    isTracked: true,
    sellingMode: "PerItem",
    warehouseUnitCost: 1,
    branchEffectiveSellingPrice: 2,
  },
];

function renderPage() {
  return render(
    <AppProviders>
      <MemoryRouter>
        <RetailWarehouseRequestStockPage />
      </MemoryRouter>
    </AppProviders>,
  );
}

async function openFinder(user: ReturnType<typeof userEvent.setup>) {
  await user.click(screen.getByTestId("request-add-products-trigger"));
  await waitFor(() => {
    expect(screen.getByTestId("request-add-products")).toBeInTheDocument();
  });
}

describe("RetailWarehouseRequestStockPage transfer-style compose", () => {
  beforeEach(() => {
    vi.spyOn(catalogClient, "listCatalogCategories").mockResolvedValue({
      items: [{ categoryId: "cat-1", name: "Grocery", status: "Active" } as never],
      totalCount: 1,
      page: 1,
      pageSize: 100,
    });
    vi.spyOn(supplyRoutesClient, "listSupplyRoutesByDestination").mockResolvedValue([
      {
        routeId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
        organizationId: TEST_ORG_A_ID,
        sourceLocationId: WH_A,
        destinationLocationId: TEST_BRANCH_A_ID,
        isPreferred: true,
        isActive: true,
        createdAtUtc: "2026-01-01T00:00:00Z",
        updatedAtUtc: "2026-01-01T00:00:00Z",
      },
    ]);
    vi.spyOn(stockRequestsClient, "listReplenishmentCatalog").mockResolvedValue({
      items: catalogItems,
      totalCount: catalogItems.length,
      page: 1,
      pageSize: 40,
      supplyWarehouseBranchId: WH_A,
      supplyWarehouseName: "Panay Warehouse",
    });
    vi.spyOn(stockRequestsClient, "createStockRequest").mockResolvedValue({
      stockRequestId: "sr-1",
    } as never);
  });

  it("uses transfer-style page chrome with locked warehouse and branch", async () => {
    renderPage();
    const root = await screen.findByTestId("retail-warehouse-request-stock");
    expect(root.className).toMatch(/inventory-transfer-create-page/);
    expect(screen.getByTestId("request-stock-details")).toBeInTheDocument();
    expect(screen.getByTestId("retail-warehouse-supply-from")).toHaveTextContent(
      /Panay Warehouse/i,
    );
    expect(screen.getByTestId("retail-warehouse-request-to")).toHaveTextContent(/Pac Passi/i);
    expect(screen.getByTestId("request-selected-items-empty")).toBeInTheDocument();
    expect(screen.queryByTestId("retail-warehouse-browser")).not.toBeInTheDocument();
  });

  it("adds PerItem products from finder and shows estimated cost", async () => {
    const user = userEvent.setup();
    renderPage();
    await openFinder(user);

    await waitFor(() => {
      expect(screen.getByTestId(`request-add-${PRODUCT_A}`)).toBeInTheDocument();
    });
    await user.click(screen.getByTestId(`request-add-${PRODUCT_A}`));
    await user.click(screen.getByTestId(`request-add-${PRODUCT_B}`));

    expect(await screen.findByTestId(`request-line-${PRODUCT_A}`)).toBeInTheDocument();
    expect(screen.getByTestId(`request-line-${PRODUCT_B}`)).toBeInTheDocument();
    expect(screen.getByTestId("retail-warehouse-estimated-cost")).toHaveTextContent(/62\.50/);
    expect(screen.getByTestId("retail-warehouse-submit")).not.toBeDisabled();
  });

  it("adds ByWeight products from finder with quantity stepper (no weight modal)", async () => {
    const user = userEvent.setup();
    renderPage();
    await openFinder(user);
    await waitFor(() => screen.getByTestId(`request-add-${PRODUCT_W}`));
    await user.click(screen.getByTestId(`request-add-${PRODUCT_W}`));

    expect(screen.queryByTestId("sell-weight-entry")).not.toBeInTheDocument();
    const line = await screen.findByTestId(`request-line-${PRODUCT_W}`);
    expect(within(line).getByTestId(`request-line-qty-${PRODUCT_W}`)).toHaveTextContent(/^1$/);
  });

  it("marks out-of-stock warehouse products unavailable in finder", async () => {
    const user = userEvent.setup();
    renderPage();
    await openFinder(user);
    await waitFor(() => screen.getByTestId(`request-picker-unavailable-${PRODUCT_OOS}`));
    expect(screen.queryByTestId(`request-add-${PRODUCT_OOS}`)).not.toBeInTheDocument();
  });

  it("blocks submit when warehouse availability drops without rewriting qty", async () => {
    const user = userEvent.setup();
    renderPage();
    await openFinder(user);
    await waitFor(() => screen.getByTestId(`request-add-${PRODUCT_A}`));
    await user.click(screen.getByTestId(`request-add-${PRODUCT_A}`));
    expect(await screen.findByTestId(`request-line-${PRODUCT_A}`)).toBeInTheDocument();

    vi.mocked(stockRequestsClient.listReplenishmentCatalog).mockResolvedValue({
      items: [
        {
          ...catalogItems[0]!,
          warehouseAvailableQuantity: 0,
        },
      ],
      totalCount: 1,
      page: 1,
      pageSize: 20,
      supplyWarehouseBranchId: WH_A,
      supplyWarehouseName: "Panay Warehouse",
    });

    fireEvent.click(screen.getByTestId("retail-warehouse-submit"));
    await waitFor(() => {
      expect(screen.getByTestId("retail-warehouse-submit")).toBeDisabled();
    });
    expect(
      screen.getByText(/Warehouse stock changed\. Adjust quantities before submitting/i),
    ).toBeInTheDocument();
    expect(stockRequestsClient.createStockRequest).not.toHaveBeenCalled();
  });

  it("submits a stock request for selected lines", async () => {
    const user = userEvent.setup();
    renderPage();
    await openFinder(user);
    await waitFor(() => screen.getByTestId(`request-add-${PRODUCT_A}`));
    await user.click(screen.getByTestId(`request-add-${PRODUCT_A}`));
    expect(await screen.findByTestId(`request-line-${PRODUCT_A}`)).toBeInTheDocument();

    fireEvent.click(screen.getByTestId("retail-warehouse-submit"));
    await waitFor(() => {
      expect(stockRequestsClient.createStockRequest).toHaveBeenCalled();
    });
    const call = vi.mocked(stockRequestsClient.createStockRequest).mock.calls[0]!;
    expect(call[1]).toMatchObject({
      destinationLocationId: TEST_BRANCH_A_ID,
      requestedSourceLocationId: WH_A,
      lines: [{ productId: PRODUCT_A, requestedQuantity: 1 }],
    });
  });
});
