import { beforeEach, describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen, waitFor, within } from "@testing-library/react";
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

function renderPage() {
  return render(
    <AppProviders>
      <MemoryRouter>
        <RetailWarehouseRequestStockPage />
      </MemoryRouter>
    </AppProviders>,
  );
}

describe("RetailWarehouseRequestStockPage Sell-like basket", () => {
  beforeEach(() => {
    vi.spyOn(catalogClient, "listCatalogCategories").mockResolvedValue({
      items: [],
      totalCount: 0,
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
      items: [
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
          name: "Noodles",
          sku: "NOO-1",
          unitOfMeasure: "pcs",
          branchOnHandQuantity: 8,
          warehouseAvailableQuantity: 0,
          isLowStock: false,
          isTracked: true,
          sellingMode: "PerItem",
          warehouseUnitCost: 8,
          branchEffectiveSellingPrice: 12,
        },
        {
          productId: PRODUCT_W,
          name: "Tilapia",
          sku: "TIL-1",
          unitOfMeasure: "Kilogram",
          branchOnHandQuantity: 1.2,
          warehouseAvailableQuantity: 15,
          isLowStock: false,
          isTracked: true,
          sellingMode: "ByWeight",
          warehouseUnitCost: 80,
          branchEffectiveSellingPrice: 120,
        },
      ],
      totalCount: 3,
      page: 1,
      pageSize: 40,
      supplyWarehouseBranchId: WH_A,
      supplyWarehouseName: "Panay Warehouse",
    });
  });

  it("tap-to-add uses Sell cart line pattern and keeps basket across search", async () => {
    renderPage();
    await screen.findByTestId(`retail-warehouse-product-${PRODUCT_A}`);

    expect(screen.getByTestId("retail-warehouse-submit")).toBeDisabled();

    fireEvent.click(screen.getByTestId(`retail-warehouse-product-${PRODUCT_A}`));
    const line = screen.getByTestId(`retail-warehouse-basket-line-${PRODUCT_A}`);
    expect(line).toHaveClass("sell-cart-line");
    expect(screen.getByTestId("retail-warehouse-submit")).not.toBeDisabled();
    expect(screen.queryByTestId(`retail-warehouse-add-${PRODUCT_A}`)).not.toBeInTheDocument();

    fireEvent.change(screen.getByTestId("retail-warehouse-search"), {
      target: { value: "zzz-no-match" },
    });
    await waitFor(() => {
      expect(stockRequestsClient.listReplenishmentCatalog).toHaveBeenCalled();
    });
    expect(screen.getByTestId(`retail-warehouse-basket-line-${PRODUCT_A}`)).toBeInTheDocument();

    fireEvent.click(screen.getByTestId(`retail-warehouse-basket-remove-${PRODUCT_A}`));
    expect(screen.queryByTestId(`retail-warehouse-basket-line-${PRODUCT_A}`)).not.toBeInTheDocument();
    expect(screen.getByTestId("retail-warehouse-submit")).toBeDisabled();
  });

  it("shows warehouse cost and OOS without branch selling price on product cards", async () => {
    renderPage();
    await screen.findByTestId(`retail-warehouse-product-${PRODUCT_A}`);

    expect(screen.getByTestId(`retail-warehouse-cost-${PRODUCT_A}`)).toHaveTextContent(/12\.50/);
    expect(screen.queryByTestId(`retail-warehouse-price-${PRODUCT_A}`)).not.toBeInTheDocument();
    expect(screen.getByTestId(`retail-warehouse-oos-${PRODUCT_B}`)).toHaveTextContent(
      /Out of stock at warehouse/i,
    );
  });

  it("opens weight dialog on tap for ByWeight and shows Sell-like weight edit control", async () => {
    renderPage();
    await screen.findByTestId(`retail-warehouse-product-${PRODUCT_W}`);

    fireEvent.click(screen.getByTestId(`retail-warehouse-product-${PRODUCT_W}`));
    expect(await screen.findByTestId("sell-weight-entry")).toBeInTheDocument();

    fireEvent.change(screen.getByTestId("sell-weight-input"), { target: { value: "1.5" } });
    fireEvent.click(screen.getByTestId("sell-weight-confirm"));

    const line = await screen.findByTestId(`retail-warehouse-basket-line-${PRODUCT_W}`);
    expect(line).toHaveClass("sell-cart-line");
    expect(
      within(line).getByTestId(`retail-warehouse-edit-weight-${PRODUCT_W}`),
    ).toHaveTextContent(/1\.5\s*kg/i);
    expect(screen.getByTestId(`retail-warehouse-line-cost-${PRODUCT_W}`)).toHaveTextContent(/120/);
    expect(within(line).queryByText(/Potential retail/i)).not.toBeInTheDocument();
  });

  it("footer shows products count and estimated warehouse cost only", async () => {
    renderPage();
    await screen.findByTestId(`retail-warehouse-product-${PRODUCT_A}`);

    fireEvent.click(screen.getByTestId(`retail-warehouse-product-${PRODUCT_A}`));
    fireEvent.click(screen.getByTestId(`retail-warehouse-product-${PRODUCT_B}`));

    const footer = screen.getByTestId("retail-warehouse-basket-footer");
    expect(within(footer).getByTestId("retail-warehouse-products-count")).toHaveTextContent(
      /2 products/i,
    );
    expect(footer).not.toHaveTextContent(/16\.50\s*units/i);
    expect(footer).not.toHaveTextContent(/\d+\s*qty/i);
    expect(screen.getByTestId("retail-warehouse-estimated-cost")).toHaveTextContent(/20\.50/);
    expect(screen.queryByTestId("retail-warehouse-potential-retail")).not.toBeInTheDocument();
    expect(screen.queryByTestId("retail-warehouse-potential-gross")).not.toBeInTheDocument();
  });

  it("shows mobile view-request control", async () => {
    renderPage();
    await screen.findByTestId(`retail-warehouse-product-${PRODUCT_A}`);
    expect(screen.getByTestId("retail-warehouse-view-request")).toHaveTextContent("View request (0)");
  });
});
