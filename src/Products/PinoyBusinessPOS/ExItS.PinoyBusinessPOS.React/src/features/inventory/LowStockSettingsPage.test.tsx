import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import * as inventoryClient from "@/api/pos/pos-inventory-client";
import * as catalogClient from "@/api/pos/pos-catalog-client";
import { LowStockSettingsPage } from "@/features/inventory/LowStockSettingsPage";

const workspace = {
  organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  branchId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
  branchName: "Main",
};

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => ({
    boundWorkspace: workspace,
    sessionGrant: {
      productAccessAllowed: true,
      mappedPosRoleCode: "Owner",
      productLocalRoleCode: "Owner",
      membershipRole: "OrganizationOwner",
      organizationManagementAuthority: true,
    },
  }),
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

describe("LowStockSettingsPage", () => {
  beforeEach(() => {
    vi.spyOn(inventoryClient, "listInventory").mockResolvedValue({
      items: [
        {
          productId: "p1",
          organizationId: workspace.organizationId,
          name: "Coca-Cola 1.5L",
          unitOfMeasure: "Piece",
          productStatus: "Active",
          isTracked: true,
          onHandQuantity: 24,
          reorderLevel: 10,
          reorderQuantity: 24,
          stockStatus: "InStock",
          isLowStock: false,
          monitoringMode: "BranchDefault",
          sku: "COKE15",
          categoryName: "Drinks",
          createdAtUtc: "2026-01-01T00:00:00Z",
          updatedAtUtc: "2026-01-01T00:00:00Z",
        },
      ],
      totalCount: 1,
      page: 1,
      pageSize: 50,
    });
    vi.spyOn(inventoryClient, "getInventoryReorderDefault").mockResolvedValue({
      organizationId: workspace.organizationId,
      branchId: workspace.branchId,
      reorderLevel: 5,
      reorderQuantity: 10,
    });
    vi.spyOn(catalogClient, "listCatalogCategories").mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 200,
    } as never);
  });

  it("renders branch-scoped table and branch default", async () => {
    render(
      <AppProviders>
        <MemoryRouter initialEntries={["/inventory/low-stock-settings"]}>
          <Routes>
            <Route path="/inventory/low-stock-settings" element={<LowStockSettingsPage />} />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );

    expect(await screen.findByTestId("low-stock-settings-page")).toBeInTheDocument();
    expect(await screen.findByTestId("low-stock-branch-default")).toBeInTheDocument();
    expect(await screen.findByTestId("low-stock-filters")).toBeInTheDocument();
    expect(await screen.findByTestId("edit-branch-default")).toBeInTheDocument();
  });
});
