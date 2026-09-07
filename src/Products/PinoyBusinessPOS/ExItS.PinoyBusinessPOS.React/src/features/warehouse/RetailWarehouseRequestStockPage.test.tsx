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

describe("RetailWarehouseRequestStockPage Sell UI parity", () => {
  beforeEach(() => {
    vi.spyOn(window, "matchMedia").mockImplementation((query) => ({
      matches: query.includes("min-width: 900px"),
      media: query,
      onchange: null,
      addListener: vi.fn(),
      removeListener: vi.fn(),
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
      dispatchEvent: vi.fn(),
    }));
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
          productId: "66666666-6666-6666-6666-666666666666",
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
      ],
      totalCount: 4,
      page: 1,
      pageSize: 40,
      supplyWarehouseBranchId: WH_A,
      supplyWarehouseName: "Panay Warehouse",
    });
    vi.spyOn(stockRequestsClient, "createStockRequest").mockResolvedValue({
      stockRequestId: "sr-1",
    } as never);
  });

  it("uses Sell floor layout classes and Sell search placeholder", async () => {
    renderPage();
    const root = await screen.findByTestId("retail-warehouse-request-stock");
    expect(root.className).toMatch(/sell-floor-root/);
    expect(root.className).toMatch(/request-stock-floor/);
    expect(screen.getByTestId("retail-warehouse-browser").className).toMatch(
      /sell-floor-workspace/,
    );
    expect(screen.getByTestId("retail-warehouse-products").className).toMatch(
      /sell-product-grid/,
    );
    expect(screen.getByTestId("retail-warehouse-search")).toHaveAttribute(
      "placeholder",
      "Search by product name, barcode, or SKU",
    );
    expect(screen.getByTestId("sell-categories")).toBeInTheDocument();
  });

  it("product cards have no image media, no Add button, no selling price", async () => {
    renderPage();
    const card = await screen.findByTestId(`retail-warehouse-product-${PRODUCT_A}`);
    expect(card).toHaveClass("sell-product-card--request");
    expect(card.querySelector(".sell-product-card__media")).toBeNull();
    expect(card.querySelector("img")).toBeNull();
    expect(screen.queryByTestId(`retail-warehouse-add-${PRODUCT_A}`)).not.toBeInTheDocument();
    expect(screen.queryByTestId(`retail-warehouse-price-${PRODUCT_A}`)).not.toBeInTheDocument();
    expect(screen.queryByText(/₱18/)).not.toBeInTheDocument();
    expect(screen.getByTestId(`retail-warehouse-cost-${PRODUCT_A}`)).toHaveTextContent(/12\.50/);
    expect(screen.getByTestId(`retail-warehouse-branch-stock-${PRODUCT_A}`)).toHaveTextContent(
      /2\s*pcs/i,
    );
    expect(screen.getByTestId(`retail-warehouse-wh-stock-${PRODUCT_A}`)).toHaveTextContent(
      /40\s*pcs/i,
    );
  });

  it("tap-to-add increments PerItem and keeps Sell cart line pattern across search", async () => {
    renderPage();
    await screen.findByTestId(`retail-warehouse-product-${PRODUCT_A}`);

    fireEvent.click(screen.getByTestId(`retail-warehouse-product-${PRODUCT_A}`));
    fireEvent.click(screen.getByTestId(`retail-warehouse-product-${PRODUCT_A}`));

    const line = screen.getByTestId(`retail-warehouse-basket-line-${PRODUCT_A}`);
    expect(line).toHaveClass("sell-cart-line");
    expect(screen.getByTestId(`retail-warehouse-qty-${PRODUCT_A}`)).toHaveTextContent("2");
    expect(screen.getByTestId("retail-warehouse-submit")).not.toBeDisabled();

    fireEvent.change(screen.getByTestId("retail-warehouse-search"), {
      target: { value: "zzz-no-match" },
    });
    await waitFor(() => {
      expect(stockRequestsClient.listReplenishmentCatalog).toHaveBeenCalled();
    });
    expect(screen.getByTestId(`retail-warehouse-basket-line-${PRODUCT_A}`)).toBeInTheDocument();
  });

  it("opens Sell weight entry for ByWeight and confirms Add to request", async () => {
    renderPage();
    await screen.findByTestId(`retail-warehouse-product-${PRODUCT_W}`);

    fireEvent.click(screen.getByTestId(`retail-warehouse-product-${PRODUCT_W}`));
    expect(await screen.findByTestId("sell-weight-entry")).toBeInTheDocument();
    expect(screen.getByTestId("sell-weight-confirm")).toHaveTextContent(/Add to request/i);

    fireEvent.change(screen.getByTestId("sell-weight-input"), { target: { value: "5.5" } });
    fireEvent.click(screen.getByTestId("sell-weight-confirm"));

    const line = await screen.findByTestId(`retail-warehouse-basket-line-${PRODUCT_W}`);
    expect(
      within(line).getByTestId(`retail-warehouse-edit-weight-${PRODUCT_W}`),
    ).toHaveTextContent(/5\.5\s*kg/i);
    expect(screen.getByTestId(`retail-warehouse-line-cost-${PRODUCT_W}`)).toHaveTextContent(
      /550/,
    );
    expect(within(line).queryByText(/Potential retail|SRP|gross/i)).not.toBeInTheDocument();
  });

  it("footer shows estimated warehouse cost only and mixed UOM product count", async () => {
    renderPage();
    await screen.findByTestId(`retail-warehouse-product-${PRODUCT_A}`);

    fireEvent.click(screen.getByTestId(`retail-warehouse-product-${PRODUCT_A}`));
    fireEvent.click(screen.getByTestId(`retail-warehouse-product-${PRODUCT_B}`));

    const footer = screen.getByTestId("retail-warehouse-basket-footer");
    expect(footer.querySelector(".sell-cart-footer__total-row")).toBeTruthy();
    expect(screen.getByTestId("retail-warehouse-estimated-cost")).toHaveTextContent(/62\.50/);
    expect(screen.getByTestId("retail-warehouse-products-count")).toHaveTextContent(/2 products/i);
    expect(footer).not.toHaveTextContent(/16\.50\s*units/i);
    expect(screen.queryByTestId("retail-warehouse-potential-retail")).not.toBeInTheDocument();
    expect(screen.getByTestId("retail-warehouse-add-note")).toBeInTheDocument();
  });

  it("Pack UOM displays on card and cart line", async () => {
    renderPage();
    await screen.findByTestId(`retail-warehouse-product-${PRODUCT_B}`);
    fireEvent.click(screen.getByTestId(`retail-warehouse-product-${PRODUCT_B}`));
    expect(screen.getByTestId(`retail-warehouse-cost-${PRODUCT_B}`)).toHaveTextContent(/Pack/);
    const line = screen.getByTestId(`retail-warehouse-basket-line-${PRODUCT_B}`);
    expect(line).toHaveTextContent(/Pack/);
  });

  it("blocks zero warehouse stock and caps per-item increments", async () => {
    const OOS = "66666666-6666-6666-6666-666666666666";
    renderPage();
    const oos = await screen.findByTestId(`retail-warehouse-product-${OOS}`);
    expect(oos).toBeDisabled();
    fireEvent.click(oos);
    expect(screen.queryByTestId(`retail-warehouse-basket-line-${OOS}`)).not.toBeInTheDocument();

    fireEvent.click(screen.getByTestId(`retail-warehouse-product-${PRODUCT_B}`));
    fireEvent.click(screen.getByTestId(`retail-warehouse-product-${PRODUCT_B}`));
    expect(screen.getByTestId(`retail-warehouse-qty-${PRODUCT_B}`)).toHaveTextContent("2");
    fireEvent.click(screen.getByTestId(`retail-warehouse-product-${PRODUCT_B}`));
    expect(screen.getByTestId(`retail-warehouse-qty-${PRODUCT_B}`)).toHaveTextContent("2");
  });

  it("rejects weight above warehouse available", async () => {
    renderPage();
    await screen.findByTestId(`retail-warehouse-product-${PRODUCT_W}`);
    fireEvent.click(screen.getByTestId(`retail-warehouse-product-${PRODUCT_W}`));
    expect(await screen.findByTestId("sell-weight-max-available")).toHaveTextContent(
      /Maximum available:\s*55\s*kg/i,
    );
    fireEvent.change(screen.getByTestId("sell-weight-input"), { target: { value: "60" } });
    expect(screen.getByTestId("sell-weight-confirm")).toBeDisabled();
    expect(screen.getByRole("alert")).toHaveTextContent(/Only 55 kg is available/i);
  });

  it("blocks submit when warehouse availability drops without rewriting qty", async () => {
    renderPage();
    await screen.findByTestId(`retail-warehouse-product-${PRODUCT_W}`);
    fireEvent.click(screen.getByTestId(`retail-warehouse-product-${PRODUCT_W}`));
    fireEvent.change(await screen.findByTestId("sell-weight-input"), {
      target: { value: "50" },
    });
    fireEvent.click(screen.getByTestId("sell-weight-confirm"));
    expect(
      within(await screen.findByTestId(`retail-warehouse-basket-line-${PRODUCT_W}`)).getByTestId(
        `retail-warehouse-edit-weight-${PRODUCT_W}`,
      ),
    ).toHaveTextContent(/50/);

    vi.mocked(stockRequestsClient.listReplenishmentCatalog).mockResolvedValue({
      items: [
        {
          productId: PRODUCT_W,
          name: "Banana Lakatan",
          sku: "PH-FRU-BANANA",
          unitOfMeasure: "Kilogram",
          branchOnHandQuantity: 1.2,
          warehouseAvailableQuantity: 40,
          isLowStock: false,
          isTracked: true,
          sellingMode: "ByWeight",
          warehouseUnitCost: 100,
          branchEffectiveSellingPrice: 140,
        },
      ],
      totalCount: 1,
      page: 1,
      pageSize: 20,
    });

    fireEvent.click(screen.getByTestId("retail-warehouse-submit"));
    await waitFor(() => {
      expect(screen.getByTestId("retail-warehouse-submit")).toBeDisabled();
    });
    expect(screen.getByText(/Warehouse stock changed\. Only 40 kg is now available/i)).toBeInTheDocument();
    expect(
      within(screen.getByTestId(`retail-warehouse-basket-line-${PRODUCT_W}`)).getByTestId(
        `retail-warehouse-edit-weight-${PRODUCT_W}`,
      ),
    ).toHaveTextContent(/50/);
    expect(stockRequestsClient.createStockRequest).not.toHaveBeenCalled();
  });
});
