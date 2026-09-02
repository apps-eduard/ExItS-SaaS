import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import * as inventoryClient from "@/api/pos/pos-inventory-client";
import { InventoryDetailPage } from "@/features/inventory/InventoryDetailPage";
import { formatPeso } from "@/lib/format-money";

const productId = "cccccccc-cccc-cccc-cccc-cccccccccccc";
const workspace = {
  organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  branchId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
};
const actorA = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => ({
    boundWorkspace: workspace,
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

vi.mock("@/features/actors/useActorDirectory", () => ({
  useActorDirectory: () => ({
    resolve: (id?: string | null) =>
      id
        ? { actorId: id, displayName: "Maria Santos", actorStatus: "Active" }
        : null,
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

describe("InventoryDetailPage purchase cost history", () => {
  beforeEach(() => {
    vi.spyOn(inventoryClient, "getInventoryProduct").mockResolvedValue({
      productId,
      organizationId: workspace.organizationId,
      name: "Bath Soap",
      unitOfMeasure: "Piece",
      productStatus: "Active",
      isTracked: true,
      onHandQuantity: 72,
      hasOpeningStock: true,
      stockStatus: "InStock",
      isLowStock: false,
      tracksExpiration: false,
      createdAtUtc: "2026-01-01T00:00:00Z",
      updatedAtUtc: "2026-01-01T00:00:00Z",
    } as never);
    vi.spyOn(inventoryClient, "listProductLots").mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 50,
    } as never);
  });

  it("shows unit cost and stock value for OpeningStock and omits cost for ManualIncrease", async () => {
    vi.spyOn(inventoryClient, "listInventoryMovements").mockResolvedValue({
      items: [
        {
          movementId: "11111111-1111-1111-1111-111111111111",
          productId,
          inventoryAccountId: "55555555-5555-5555-5555-555555555555",
          movementType: "OpeningStock",
          quantityEffect: 24,
          reason: "Opening stock",
          sourceType: "Opening",
          recordedAtUtc: "2026-08-28T14:15:00Z",
          recordedBy: actorA,
          unitCost: 18,
          stockValue: 432,
        },
        {
          movementId: "22222222-2222-2222-2222-222222222222",
          productId,
          inventoryAccountId: "55555555-5555-5555-5555-555555555555",
          movementType: "ManualIncrease",
          quantityEffect: 3,
          reason: "Count correction",
          sourceType: "Manual",
          recordedAtUtc: "2026-08-28T15:00:00Z",
          recordedBy: actorA,
          unitCost: null,
          stockValue: null,
        },
        {
          movementId: "33333333-3333-3333-3333-333333333333",
          productId,
          inventoryAccountId: "55555555-5555-5555-5555-555555555555",
          movementType: "PurchaseReceipt",
          quantityEffect: 48,
          reason: "PO receipt",
          sourceType: "PurchaseReceipt",
          recordedAtUtc: "2026-08-28T16:00:00Z",
          recordedBy: actorA,
          unitCost: 10,
          stockValue: 480,
        },
      ],
      totalCount: 3,
      page: 1,
      pageSize: 50,
    } as never);

    renderPage();

    await waitFor(() => {
      expect(screen.getByText("Opening stock")).toBeInTheDocument();
    });
    expect(screen.getByText("PO receipt")).toBeInTheDocument();
    expect(screen.getByText("Stock adjustment — increase")).toBeInTheDocument();
    expect(screen.getByText(formatPeso(18))).toBeInTheDocument();
    expect(screen.getByText(formatPeso(432))).toBeInTheDocument();
    expect(screen.getByText(formatPeso(10))).toBeInTheDocument();
    expect(screen.getByText(formatPeso(480))).toBeInTheDocument();
    expect(screen.queryByText(formatPeso(0))).not.toBeInTheDocument();
    expect(screen.getAllByText("Maria Santos").length).toBeGreaterThanOrEqual(1);
  });
});
