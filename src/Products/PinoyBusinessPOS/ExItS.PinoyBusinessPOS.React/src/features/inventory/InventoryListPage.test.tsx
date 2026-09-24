import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import * as inventoryClient from "@/api/pos/pos-inventory-client";
import type { PosInventoryAccountDto } from "@/api/pos/pos-inventory-client";
import { InventoryListPage } from "@/features/inventory/InventoryListPage";

const orgId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
const branchId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";

const workspaceMock = {
  boundWorkspace: {
    organizationId: orgId,
    branchId,
    branchName: "Main Branch",
    organizationDisplayName: "Test Org",
  },
  sessionGrant: {
    productAccessAllowed: true,
    mappedPosRoleCode: "Owner",
    productLocalRoleCode: "Owner",
    membershipRole: "OrganizationOwner",
    organizationManagementAuthority: true,
  },
  workspaces: [
    {
      organizationId: orgId,
      organizationDisplayName: "Test Org",
      branches: [{ branchId, name: "Main Branch", isActive: true }],
    },
  ],
};

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => workspaceMock,
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

function item(overrides: Partial<PosInventoryAccountDto> = {}): PosInventoryAccountDto {
  return {
    productId: "prod-apple",
    organizationId: orgId,
    name: "Apple",
    unitOfMeasure: "Kilogram",
    productStatus: "Active",
    isTracked: true,
    onHandQuantity: 90,
    availableQuantity: 90,
    reservedQuantity: 0,
    stockStatus: "InStock",
    isLowStock: false,
    tracksExpiration: false,
    createdAtUtc: "2026-09-24T00:00:00Z",
    updatedAtUtc: "2026-09-24T00:00:00Z",
    sellingPrice: 80,
    effectiveSellingPrice: 80,
    hasBranchPriceOverride: false,
    ...overrides,
  };
}

function renderPage() {
  return render(
    <AppProviders>
      <MemoryRouter initialEntries={["/inventory"]}>
        <Routes>
          <Route path="/inventory" element={<InventoryListPage />} />
          <Route
            path="/inventory/:productId"
            element={<div data-testid="inventory-detail-route">detail</div>}
          />
        </Routes>
      </MemoryRouter>
    </AppProviders>,
  );
}

describe("InventoryListPage desktop table", () => {
  beforeEach(() => {
    workspaceMock.sessionGrant.mappedPosRoleCode = "Owner";
    workspaceMock.sessionGrant.productLocalRoleCode = "Owner";
    workspaceMock.sessionGrant.membershipRole = "OrganizationOwner";
    workspaceMock.sessionGrant.organizationManagementAuthority = true;
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("renders cards for small screens and table for large screens", async () => {
    vi.spyOn(inventoryClient, "listInventory").mockResolvedValue({
      items: [item()],
      totalCount: 1,
      page: 1,
      pageSize: 50,
    });
    renderPage();

    const cards = await screen.findByTestId("inventory-list");
    expect(cards.className).toMatch(/lg:hidden/);
    const table = await screen.findByTestId("inventory-list-table");
    expect(table.className).toMatch(/hidden/);
    expect(table.className).toMatch(/lg:block/);
  });

  it("shows tracked row purchase cost and selling price without enable inputs", async () => {
    vi.spyOn(inventoryClient, "listInventory").mockResolvedValue({
      items: [item({ reservedQuantity: 0, unitCost: 45, openingQuantity: 20 })],
      totalCount: 1,
      page: 1,
      pageSize: 50,
    });
    renderPage();

    expect(await screen.findByTestId("inventory-table-tracking-prod-apple")).toHaveTextContent(
      "Tracked",
    );
    expect(screen.getByTestId("inventory-table-unit-prod-apple")).toHaveTextContent("Kilogram");
    expect(screen.queryByTestId("inventory-table-opening-qty-prod-apple")).not.toBeInTheDocument();
    expect(screen.queryByTestId("inventory-table-opening-qty-value-prod-apple")).not.toBeInTheDocument();
    expect(screen.queryByTestId("inventory-table-unit-cost-prod-apple")).not.toBeInTheDocument();
    expect(screen.queryByTestId("inventory-table-reserved-prod-apple")).not.toBeInTheDocument();
    expect(screen.getByTestId("inventory-table-selling-price-prod-apple")).toHaveTextContent(/80/);
    expect(screen.getByTestId("inventory-table-selling-price-prod-apple")).not.toHaveTextContent(
      /Kilogram/,
    );
    expect(screen.getByTestId("inventory-table-available-prod-apple")).toHaveTextContent("90");
    expect(screen.getByTestId("inventory-table-available-prod-apple")).not.toHaveTextContent(
      /Kilogram/,
    );
    expect(screen.getByTestId("inventory-table-unit-cost-value-prod-apple")).toHaveTextContent(/45/);
  });

  it("shows reserved badge only when reservedQty > 0 and opens drawer", async () => {
    const user = userEvent.setup();
    vi.spyOn(inventoryClient, "listInventory").mockResolvedValue({
      items: [
        item({
          productId: "prod-banana",
          name: "Banana Lakatan",
          availableQuantity: 100,
          reservedQuantity: 5,
          sellingPrice: 95,
          effectiveSellingPrice: 95,
        }),
      ],
      totalCount: 1,
      page: 1,
      pageSize: 50,
    });
    vi.spyOn(inventoryClient, "getInventoryProductReservations").mockResolvedValue({
      productId: "prod-banana",
      productName: "Banana Lakatan",
      unitOfMeasure: "Kilogram",
      onHandQuantity: 100,
      reservedQuantity: 5,
      availableQuantity: 95,
      reservations: [],
    } as never);

    renderPage();

    const badge = await screen.findByTestId("inventory-table-reserved-prod-banana");
    expect(badge).toHaveTextContent(/5/);
    await user.click(badge);
    await waitFor(() => {
      expect(screen.getByTestId("inventory-reservations-drawer")).toBeInTheDocument();
    });
  });

  it("prefers branch effective selling price when present", async () => {
    vi.spyOn(inventoryClient, "listInventory").mockResolvedValue({
      items: [
        item({
          sellingPrice: 80,
          effectiveSellingPrice: 75,
          hasBranchPriceOverride: true,
        }),
      ],
      totalCount: 1,
      page: 1,
      pageSize: 50,
    });
    renderPage();
    expect(await screen.findByTestId("inventory-table-selling-price-prod-apple")).toHaveTextContent(
      /75/,
    );
    expect(screen.getByTestId("inventory-table-selling-price-prod-apple")).not.toHaveTextContent(
      /₱80/,
    );
  });

  it("enters enable mode from Untracked with labeled Opening Qty / Purchase Cost and cost warnings", async () => {
    const user = userEvent.setup();
    vi.spyOn(inventoryClient, "listInventory").mockResolvedValue({
      items: [
        item({
          productId: "prod-battery",
          name: "Battery AA Pack",
          isTracked: false,
          onHandQuantity: 0,
          availableQuantity: 0,
          sellingPrice: 45,
          effectiveSellingPrice: 45,
          unitOfMeasure: "Pack",
        }),
      ],
      totalCount: 1,
      page: 1,
      pageSize: 50,
    });
    renderPage();

    await user.click(await screen.findByTestId("inventory-table-untracked-prod-battery"));
    expect(await screen.findByTestId("inventory-table-tracking-prod-battery")).toHaveTextContent(
      "Enable tracking",
    );
    const qty = screen.getByTestId("inventory-table-opening-qty-prod-battery");
    expect(qty).toHaveAccessibleName("Opening Qty");
    const cost = screen.getByTestId("inventory-table-unit-cost-prod-battery");
    expect(cost).toHaveAccessibleName("Purchase Cost");
    await waitFor(() => {
      expect(qty).toHaveFocus();
    });

    await user.type(qty, "20");
    await user.click(cost);
    await waitFor(() => {
      expect(cost).toHaveFocus();
    });

    await user.clear(cost);
    await user.type(cost, "45");
    expect(
      await screen.findByTestId("inventory-table-cost-zero-margin-prod-battery"),
    ).toBeInTheDocument();

    await user.clear(cost);
    await user.type(cost, "50");
    expect(
      await screen.findByTestId("inventory-table-cost-high-warning-prod-battery"),
    ).toBeInTheDocument();
  });

  it("enables stock and refreshes list to Tracked", async () => {
    const user = userEvent.setup();
    const listSpy = vi.spyOn(inventoryClient, "listInventory");
    listSpy.mockResolvedValueOnce({
      items: [
        item({
          productId: "prod-battery",
          name: "Battery AA Pack",
          isTracked: false,
          onHandQuantity: 0,
          availableQuantity: 0,
          sellingPrice: 45,
          effectiveSellingPrice: 45,
          unitOfMeasure: "Pack",
        }),
      ],
      totalCount: 1,
      page: 1,
      pageSize: 50,
    });
    listSpy.mockResolvedValue({
      items: [
        item({
          productId: "prod-battery",
          name: "Battery AA Pack",
          isTracked: true,
          onHandQuantity: 20,
          availableQuantity: 20,
          sellingPrice: 45,
          effectiveSellingPrice: 45,
          unitOfMeasure: "Pack",
        }),
      ],
      totalCount: 1,
      page: 1,
      pageSize: 50,
    });
    vi.spyOn(inventoryClient, "enableInventoryTracking").mockResolvedValue(
      item({
        productId: "prod-battery",
        isTracked: true,
        onHandQuantity: 20,
        availableQuantity: 20,
      }) as never,
    );

    renderPage();
    await user.click(await screen.findByTestId("inventory-table-untracked-prod-battery"));
    await user.type(screen.getByTestId("inventory-table-opening-qty-prod-battery"), "20");
    await user.type(screen.getByTestId("inventory-table-unit-cost-prod-battery"), "30");
    await user.click(screen.getByTestId("inventory-table-enable-stock-prod-battery"));

    await waitFor(() => {
      expect(inventoryClient.enableInventoryTracking).toHaveBeenCalledWith(
        expect.anything(),
        "prod-battery",
        expect.objectContaining({ openingQuantity: 20, unitCost: 30 }),
      );
    });
    await waitFor(() => {
      expect(screen.getByTestId("inventory-table-tracking-prod-battery")).toHaveTextContent(
        "Tracked",
      );
    });
    expect(screen.queryByTestId("inventory-table-opening-qty-prod-battery")).not.toBeInTheDocument();
  });

  it("view icon navigates and desktop row is not clickable as a whole", async () => {
    const user = userEvent.setup();
    vi.spyOn(inventoryClient, "listInventory").mockResolvedValue({
      items: [item()],
      totalCount: 1,
      page: 1,
      pageSize: 50,
    });
    renderPage();

    const row = await screen.findByTestId("inventory-table-row-prod-apple");
    expect(row).not.toHaveAttribute("role", "link");
    expect(row).not.toHaveAttribute("tabIndex");
    await user.click(within(row).getByTestId("inventory-table-view-prod-apple"));
    expect(await screen.findByTestId("inventory-detail-route")).toBeInTheDocument();
  });

  it("view-only user cannot enable untracked products", async () => {
    workspaceMock.sessionGrant.mappedPosRoleCode = "ReportingUser";
    workspaceMock.sessionGrant.productLocalRoleCode = "ReportingUser";
    workspaceMock.sessionGrant.membershipRole = "Staff";
    workspaceMock.sessionGrant.organizationManagementAuthority = false;

    vi.spyOn(inventoryClient, "listInventory").mockResolvedValue({
      items: [
        item({
          productId: "prod-battery",
          isTracked: false,
          onHandQuantity: 0,
          availableQuantity: 0,
        }),
      ],
      totalCount: 1,
      page: 1,
      pageSize: 50,
    });
    renderPage();

    expect(await screen.findByTestId("inventory-table-tracking-prod-battery")).toHaveTextContent(
      "Not tracked",
    );
    expect(screen.queryByTestId("inventory-table-untracked-prod-battery")).not.toBeInTheDocument();
  });

  it("does not add a Reserved column header", async () => {
    vi.spyOn(inventoryClient, "listInventory").mockResolvedValue({
      items: [item()],
      totalCount: 1,
      page: 1,
      pageSize: 50,
    });
    renderPage();
    const table = await screen.findByTestId("inventory-list-table");
    expect(within(table).queryByRole("columnheader", { name: /^Reserved$/i })).not.toBeInTheDocument();
    expect(within(table).getByRole("columnheader", { name: /Available/i })).toBeInTheDocument();
  });
});
