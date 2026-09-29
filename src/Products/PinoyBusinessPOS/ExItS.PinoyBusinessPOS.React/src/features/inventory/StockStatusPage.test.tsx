import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import * as catalogClient from "@/api/pos/pos-catalog-client";
import * as inventoryClient from "@/api/pos/pos-inventory-client";
import { StockStatusPage } from "@/features/inventory/StockStatusPage";
import * as stockStatusExport from "@/features/inventory/stock-status-export";

const orgId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
const branchId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
const productId = "cccccccc-cccc-cccc-cccc-cccccccccccc";

const workspaceMock = {
  boundWorkspace: {
    organizationId: orgId,
    organizationDisplayName: "Kizy Store",
    branchId,
    branchName: "Main",
    experience: "operations" as const,
  },
  workspaces: [
    {
      organizationId: orgId,
      displayName: "Kizy Store",
      branches: [
        {
          branchId,
          name: "Main",
          secondaryLine: "",
          isPrimary: true,
          isActive: true,
        },
        {
          branchId: "dddddddd-dddd-dddd-dddd-dddddddddddd",
          name: "Second",
          secondaryLine: "",
          isPrimary: false,
          isActive: true,
        },
      ],
    },
  ],
  sessionGrant: {
    productAccessAllowed: true,
    membershipRole: "OrganizationOwner",
    productLocalRoleCode: "Owner",
    mappedPosRoleCode: "Owner",
  } as Record<string, unknown>,
};

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => workspaceMock,
  useOptionalWorkspace: () => workspaceMock,
}));

vi.mock("@/connectivity/browser-online", () => ({
  useBrowserOnline: () => true,
  subscribeBrowserOnline: (onChange: (online: boolean) => void) => {
    onChange(true);
    return () => undefined;
  },
}));

vi.mock("@/offline/organization-offline-context", () => ({
  useOrganizationOfflineContext: () => null,
}));

function sampleRow(
  overrides: Partial<inventoryClient.InventoryStockStatusRowDto> = {},
): inventoryClient.InventoryStockStatusRowDto {
  return {
    productId,
    productName: "Milk 1L",
    sku: "MILK-1",
    categoryId: "cat-1",
    categoryName: "Dairy",
    unitOfMeasure: "Piece",
    branchId,
    branchName: "Main",
    areaId: null,
    areaName: null,
    onHandQuantity: 40,
    sellableQuantity: 36,
    reservedQuantity: 4,
    stockRequestCommittedQuantity: 0,
    availableQuantity: 32,
    damagedQuantity: 0,
    inspectionHoldQuantity: 0,
    pendingReturnQuantity: 0,
    expiredQuantity: 0,
    saleBlockedQuantity: 0,
    inTransitInboundQuantity: 0,
    inTransitOutboundQuantity: 0,
    reorderLevel: 10,
    isLowStock: false,
    ...overrides,
  };
}

function renderPage(entry = "/inventory/stock-status") {
  return render(
    <AppProviders>
      <MemoryRouter initialEntries={[entry]}>
        <Routes>
          <Route path="/inventory/stock-status" element={<StockStatusPage />} />
        </Routes>
      </MemoryRouter>
    </AppProviders>,
  );
}

describe("StockStatusPage", () => {
  beforeEach(() => {
    vi.spyOn(catalogClient, "listCatalogCategories").mockResolvedValue({
      items: [
        {
          categoryId: "cat-1",
          organizationId: orgId,
          name: "Dairy",
          status: "Active",
          createdAtUtc: "",
          updatedAtUtc: "",
        },
      ],
      totalCount: 1,
      page: 1,
      pageSize: 200,
    });
    vi.spyOn(inventoryClient, "getInventoryStockStatus").mockResolvedValue({
      generatedAtUtc: "2026-09-29T08:00:00Z",
      isCurrentOnly: true,
      totalCount: 1,
      rows: [sampleRow()],
    });
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("honors productId query prefilter", async () => {
    renderPage(`/inventory/stock-status?productId=${productId}`);
    await screen.findByTestId("stock-status-page");
    await waitFor(() => {
      expect(inventoryClient.getInventoryStockStatus).toHaveBeenCalled();
    });
    const call = vi.mocked(inventoryClient.getInventoryStockStatus).mock.calls.at(-1);
    expect(call?.[1]?.productId).toBe(productId);
    const banner = await screen.findByTestId("stock-status-product-filter-banner");
    expect(banner).toHaveTextContent(/Showing:\s*Milk 1L/i);
    expect(banner).not.toHaveTextContent(productId);
    expect(screen.getByTestId("stock-status-clear-product-filter")).toHaveTextContent(/^Clear$/i);
  });

  it("applies stock state filter to the query", async () => {
    renderPage();
    await screen.findByTestId("stock-status-page");
    await userEvent.click(screen.getByTestId("stock-status-state-Damaged"));
    await waitFor(() => {
      const call = vi.mocked(inventoryClient.getInventoryStockStatus).mock.calls.at(-1);
      expect(call?.[1]?.stockState).toBe("Damaged");
    });
  });

  it("exports filtered rows from the current result set", async () => {
    const filtered = sampleRow({ productName: "Filtered Milk", damagedQuantity: 2 });
    vi.mocked(inventoryClient.getInventoryStockStatus).mockResolvedValue({
      generatedAtUtc: "2026-09-29T08:00:00Z",
      isCurrentOnly: true,
      totalCount: 1,
      rows: [filtered],
    });
    const csvSpy = vi.spyOn(stockStatusExport, "exportStockStatusCsv").mockReturnValue("csv");
    renderPage();
    await screen.findByTestId("stock-status-data");
    const csvBtn = screen.getAllByRole("button", { name: /export csv/i })[0];
    await userEvent.click(csvBtn!);
    expect(csvSpy).toHaveBeenCalledWith(
      expect.objectContaining({
        rows: [expect.objectContaining({ productName: "Filtered Milk", damagedQuantity: 2 })],
      }),
    );
  });

  it("shows current-only label (no as-of date control)", async () => {
    renderPage();
    await screen.findByTestId("stock-status-current-only");
    expect(screen.queryByLabelText(/as of/i)).not.toBeInTheDocument();
  });

  it("renders category name and sorts by product header", async () => {
    vi.mocked(inventoryClient.getInventoryStockStatus).mockResolvedValue({
      generatedAtUtc: "2026-09-29T08:00:00Z",
      isCurrentOnly: true,
      totalCount: 2,
      rows: [
        sampleRow({ productName: "Banana", categoryName: "Produce", productId: "p1" }),
        sampleRow({ productName: "Apple", categoryName: "Produce", productId: "p2" }),
      ],
    });
    renderPage();
    await screen.findByTestId("stock-status-data");
    expect(screen.getAllByText("Produce").length).toBeGreaterThan(0);

    const productHead = screen.getByRole("button", { name: /product/i });
    await userEvent.click(productHead);
    const rows = screen.getAllByTestId(/stock-status-row-/);
    expect(rows[0]).toHaveTextContent("Apple");
    expect(rows[1]).toHaveTextContent("Banana");
  });

  it("fills missing category name from the category filter list", async () => {
    vi.mocked(inventoryClient.getInventoryStockStatus).mockResolvedValue({
      generatedAtUtc: "2026-09-29T08:00:00Z",
      isCurrentOnly: true,
      totalCount: 1,
      rows: [sampleRow({ categoryId: "cat-1", categoryName: null })],
    });
    renderPage();
    const data = await screen.findByTestId("stock-status-data");
    expect(within(data).getAllByText("Dairy").length).toBeGreaterThan(0);
    expect(screen.getByTestId(`stock-status-row-${productId}-${branchId}`)).toHaveTextContent(
      "Dairy",
    );
  });

  it("renders Committed column", async () => {
    vi.mocked(inventoryClient.getInventoryStockStatus).mockResolvedValue({
      generatedAtUtc: "2026-09-29T08:00:00Z",
      isCurrentOnly: true,
      totalCount: 1,
      rows: [sampleRow({ stockRequestCommittedQuantity: 3 })],
    });
    renderPage();
    await screen.findByTestId("stock-status-data");
    expect(screen.getAllByText("Committed").length).toBeGreaterThan(0);
    expect(screen.getByTestId(`stock-status-committed-${productId}-${branchId}`)).toHaveTextContent(
      "3",
    );
  });
});
