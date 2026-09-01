import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import * as inventoryClient from "@/api/pos/pos-inventory-client";
import { InventoryDetailPage } from "@/features/inventory/InventoryDetailPage";
import { InventoryListPage } from "@/features/inventory/InventoryListPage";

const orgId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
const mainBranchId = "11111111-1111-1111-1111-111111111111";
const remoteBranchId = "22222222-2222-2222-2222-222222222222";
const productId = "cccccccc-cccc-cccc-cccc-cccccccccccc";

const workspaceMock = {
  boundWorkspace: {
    organizationId: orgId,
    branchId: mainBranchId,
    branchName: "Main Store",
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
      branches: [
        { branchId: mainBranchId, name: "Main Store", isActive: true },
        { branchId: remoteBranchId, name: "Remote Branch", isActive: true },
      ],
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

function account(onHandQuantity: number) {
  return {
    productId,
    organizationId: orgId,
    name: "Coke",
    unitOfMeasure: "Piece",
    productStatus: "Active",
    isTracked: true,
    onHandQuantity,
    stockStatus: "InStock",
    isLowStock: false,
    tracksExpiration: false,
    createdAtUtc: "2026-01-01T00:00:00Z",
    updatedAtUtc: "2026-01-01T00:00:00Z",
  };
}

describe("Inventory branch read authority UX", () => {
  beforeEach(() => {
    workspaceMock.boundWorkspace.branchId = mainBranchId;
    workspaceMock.boundWorkspace.branchName = "Main Store";
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("BINV-UX-02 list displays Main branch quantity", async () => {
    const listSpy = vi.spyOn(inventoryClient, "listInventory").mockResolvedValue({
      items: [account(100) as never],
      totalCount: 1,
      page: 1,
      pageSize: 50,
    });

    render(
      <AppProviders>
        <MemoryRouter initialEntries={["/inventory"]}>
          <Routes>
            <Route path="/inventory" element={<InventoryListPage />} />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );

    await waitFor(() => expect(listSpy).toHaveBeenCalled());
    expect(await screen.findByText("100")).toBeInTheDocument();
    expect(listSpy.mock.calls[0]?.[0]).toMatchObject({
      organizationId: orgId,
      branchId: mainBranchId,
    });
  });

  it("BINV-UX-03 Remote workspace displays Remote quantity", async () => {
    workspaceMock.boundWorkspace.branchId = remoteBranchId;
    workspaceMock.boundWorkspace.branchName = "Remote Branch";

    const listSpy = vi.spyOn(inventoryClient, "listInventory").mockResolvedValue({
      items: [account(25) as never],
      totalCount: 1,
      page: 1,
      pageSize: 50,
    });

    render(
      <AppProviders>
        <MemoryRouter initialEntries={["/inventory"]}>
          <Routes>
            <Route path="/inventory" element={<InventoryListPage />} />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );

    await waitFor(() => expect(listSpy).toHaveBeenCalled());
    expect(await screen.findByText("25")).toBeInTheDocument();
    expect(listSpy.mock.calls[0]?.[0].branchId).toBe(remoteBranchId);
  });

  it("BINV-UX-05 detail labels quantity as selected branch", async () => {
    vi.spyOn(inventoryClient, "getInventoryProduct").mockResolvedValue(account(25) as never);
    vi.spyOn(inventoryClient, "listInventoryMovements").mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 50,
    });

    workspaceMock.boundWorkspace.branchId = remoteBranchId;
    workspaceMock.boundWorkspace.branchName = "Remote Branch";

    render(
      <AppProviders>
        <MemoryRouter initialEntries={[`/inventory/${productId}`]}>
          <Routes>
            <Route path="/inventory/:productId" element={<InventoryDetailPage />} />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );

    const onHand = await screen.findByTestId("inventory-on-hand");
    expect(onHand.textContent).toContain("Remote Branch");
    expect(onHand.textContent).toContain("25");
  });

  it("BINV-UX-01 list query key includes branch", async () => {
    const listSpy = vi.spyOn(inventoryClient, "listInventory").mockResolvedValue({
      items: [account(100) as never],
      totalCount: 1,
      page: 1,
      pageSize: 50,
    });

    render(
      <AppProviders>
        <MemoryRouter initialEntries={["/inventory"]}>
          <Routes>
            <Route path="/inventory" element={<InventoryListPage />} />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );

    await waitFor(() => expect(listSpy).toHaveBeenCalled());
    expect(listSpy.mock.calls[0]?.[0]).toEqual(
      expect.objectContaining({ organizationId: orgId, branchId: mainBranchId }),
    );
  });

  it("BINV-UX-06 detail product query includes branch", async () => {
    const getSpy = vi.spyOn(inventoryClient, "getInventoryProduct").mockResolvedValue(account(25) as never);
    vi.spyOn(inventoryClient, "listInventoryMovements").mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 50,
    });

    workspaceMock.boundWorkspace.branchId = remoteBranchId;

    render(
      <AppProviders>
        <MemoryRouter initialEntries={[`/inventory/${productId}`]}>
          <Routes>
            <Route path="/inventory/:productId" element={<InventoryDetailPage />} />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );

    await waitFor(() => expect(getSpy).toHaveBeenCalled());
    expect(getSpy.mock.calls[0]?.[0]).toEqual(
      expect.objectContaining({ organizationId: orgId, branchId: remoteBranchId }),
    );
  });
});
