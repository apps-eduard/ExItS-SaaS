import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import * as catalogClient from "@/api/pos/pos-catalog-client";
import * as inventoryClient from "@/api/pos/pos-inventory-client";
import { InventoryDetailPage } from "@/features/inventory/InventoryDetailPage";
import { formatPeso } from "@/lib/format-money";

const productId = "cccccccc-cccc-cccc-cccc-cccccccccccc";
const workspace = {
  organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  branchId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
  branchName: "Kalibo Branch",
};

const workspaceState = {
  boundWorkspace: { ...workspace },
  workspaces: [
    {
      organizationId: workspace.organizationId,
      displayName: "mica store",
      branches: [
        {
          branchId: workspace.branchId,
          name: "Kalibo Branch",
          secondaryLine: "",
          isPrimary: true,
          isActive: true,
        },
      ],
    },
  ],
  sessionGrant: {
    productAccessAllowed: true,
    mappedPosRoleCode: "Owner",
    productLocalRoleCode: "Owner",
    membershipRole: "OrganizationOwner",
    organizationManagementAuthority: true,
  },
};

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => workspaceState,
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

vi.mock("@/features/actors/useActorDirectory", () => ({
  useActorDirectory: () => ({
    resolve: () => null,
    isResolving: false,
    sortedIds: [],
    isLoading: false,
    isFetching: false,
    data: [],
  }),
}));

function renderPage() {
  return render(
    <AppProviders>
      <MemoryRouter initialEntries={[`/inventory/${productId}`]}>
        <Routes>
          <Route path="/inventory/:productId" element={<InventoryDetailPage />} />
        </Routes>
      </MemoryRouter>
    </AppProviders>,
  );
}

describe("InventoryDetailPage enable tracking selling price", () => {
  beforeEach(() => {
    workspaceState.boundWorkspace = { ...workspace };
    vi.spyOn(inventoryClient, "getInventoryProduct").mockResolvedValue({
      productId,
      organizationId: workspace.organizationId,
      name: "Bath Soap",
      unitOfMeasure: "Bottle",
      productStatus: "Active",
      isTracked: false,
      onHandQuantity: 0,
      hasOpeningStock: false,
      stockStatus: "OutOfStock",
      isLowStock: false,
      tracksExpiration: false,
      createdAtUtc: "2026-01-01T00:00:00Z",
      updatedAtUtc: "2026-01-01T00:00:00Z",
    } as never);
    vi.spyOn(inventoryClient, "listInventoryMovements").mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 50,
    } as never);
    vi.spyOn(inventoryClient, "listProductLots").mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 50,
    } as never);
  });

  it("shows branch override as current selling price", async () => {
    const getCatalog = vi.spyOn(catalogClient, "getCatalogProduct").mockResolvedValue({
      productId,
      organizationId: workspace.organizationId,
      name: "Bath Soap",
      unitOfMeasure: "Bottle",
      sellingMode: "Each",
      sellingPrice: 12,
      effectiveSellingPrice: 15,
      hasBranchPriceOverride: true,
      status: "Active",
      createdAtUtc: "2026-01-01T00:00:00Z",
      updatedAtUtc: "2026-01-01T00:00:00Z",
    } as never);
    vi.spyOn(catalogClient, "setBranchProductPriceOverride");

    renderPage();

    await waitFor(() => {
      expect(screen.getByTestId("inventory-current-selling-price")).toBeInTheDocument();
    });
    expect(screen.getByTestId("inventory-current-selling-price")).toHaveTextContent(
      `${formatPeso(15)} / Bottle`,
    );
    expect(screen.getByTestId("inventory-selling-price-source")).toHaveTextContent("Branch price");
    expect(getCatalog).toHaveBeenCalledTimes(1);
    expect(catalogClient.setBranchProductPriceOverride).not.toHaveBeenCalled();
  });

  it("shows organization price when no branch override", async () => {
    vi.spyOn(catalogClient, "getCatalogProduct").mockResolvedValue({
      productId,
      organizationId: workspace.organizationId,
      name: "Bath Soap",
      unitOfMeasure: "Bottle",
      sellingMode: "Each",
      sellingPrice: 12,
      effectiveSellingPrice: 12,
      hasBranchPriceOverride: false,
      status: "Active",
      createdAtUtc: "2026-01-01T00:00:00Z",
      updatedAtUtc: "2026-01-01T00:00:00Z",
    } as never);

    renderPage();

    await waitFor(() => {
      expect(screen.getByTestId("inventory-selling-price-source")).toHaveTextContent(
        "Organization price",
      );
    });
    expect(screen.getByTestId("inventory-current-selling-price")).toHaveTextContent(
      `${formatPeso(12)} / Bottle`,
    );
  });

  it("warns when purchase cost exceeds selling price without blocking enable", async () => {
    const user = userEvent.setup();
    vi.spyOn(catalogClient, "getCatalogProduct").mockResolvedValue({
      productId,
      organizationId: workspace.organizationId,
      name: "Bath Soap",
      unitOfMeasure: "Bottle",
      sellingMode: "Each",
      sellingPrice: 12,
      effectiveSellingPrice: 12,
      hasBranchPriceOverride: false,
      status: "Active",
      createdAtUtc: "2026-01-01T00:00:00Z",
      updatedAtUtc: "2026-01-01T00:00:00Z",
    } as never);

    renderPage();
    await waitFor(() => expect(screen.getByTestId("inventory-enable")).toBeInTheDocument());

    await user.clear(screen.getByLabelText(/Opening quantity/i));
    await user.type(screen.getByLabelText(/Opening quantity/i), "10");
    await user.type(screen.getByTestId("inventory-enable-unit-cost"), "15");

    await waitFor(() => {
      expect(screen.getByTestId("inventory-purchase-cost-high-warning")).toBeInTheDocument();
    });
    expect(screen.getByTestId("inventory-enable-stock-value")).toHaveTextContent("150.00");
    expect(screen.getByTestId("inventory-enable")).not.toBeDisabled();
  });
});
