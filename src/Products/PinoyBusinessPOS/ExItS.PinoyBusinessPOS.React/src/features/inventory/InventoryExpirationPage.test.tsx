import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import * as inventoryClient from "@/api/pos/pos-inventory-client";
import { InventoryExpirationPage } from "@/features/inventory/InventoryExpirationPage";

const orgId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
const branchId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
const productId = "cccccccc-cccc-cccc-cccc-cccccccccccc";
const expiredLotId = "11111111-1111-1111-1111-111111111111";
const nearLotId = "22222222-2222-2222-2222-222222222222";

const workspaceMock = {
  boundWorkspace: {
    organizationId: orgId,
    organizationDisplayName: "Kizy Store",
    branchId,
    branchName: "Main",
    experience: "operations" as const,
  },
  sessionGrant: {
    productAccessAllowed: true,
    membershipRole: "OrganizationOwner",
    productLocalRoleCode: "Owner",
    mappedPosRoleCode: "Owner",
  } as Record<string, unknown>,
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

function expiredLot(overrides: Partial<inventoryClient.PosExpiringLotDto> = {}) {
  return {
    lotId: expiredLotId,
    productId,
    productName: "Milk 1L",
    lotNumber: "EXP-001",
    expirationDate: "2026-08-01",
    quantityOnHand: 7,
    expiryStatus: "Expired",
    warningDays: 7,
    branchId,
    ...overrides,
  } satisfies inventoryClient.PosExpiringLotDto;
}

function nearLot() {
  return {
    lotId: nearLotId,
    productId,
    productName: "Milk 1L",
    lotNumber: "NEAR-1",
    expirationDate: "2026-09-06",
    quantityOnHand: 10,
    expiryStatus: "NearExpiry",
    warningDays: 7,
    branchId,
  } satisfies inventoryClient.PosExpiringLotDto;
}

function renderPage() {
  return render(
    <AppProviders>
      <MemoryRouter initialEntries={["/inventory/expiration"]}>
        <Routes>
          <Route path="/inventory/expiration" element={<InventoryExpirationPage />} />
        </Routes>
      </MemoryRouter>
    </AppProviders>,
  );
}

describe("InventoryExpirationPage expired write-off action", () => {
  beforeEach(() => {
    workspaceMock.sessionGrant = {
      productAccessAllowed: true,
      membershipRole: "OrganizationOwner",
      productLocalRoleCode: "Owner",
      mappedPosRoleCode: "Owner",
    };
    vi.spyOn(inventoryClient, "listExpiringLots").mockResolvedValue({
      items: [expiredLot(), nearLot()],
      totalCount: 2,
      page: 1,
      pageSize: 50,
      expiredCount: 1,
      nearExpiryCount: 1,
    });
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("shows View for all lots and Write off only for expired lots with on-hand", async () => {
    renderPage();
    await screen.findByTestId(`expiring-lot-${expiredLotId}`);

    const writeOff = screen.getByTestId(`expiring-lot-write-off-${expiredLotId}`);
    expect(writeOff).toHaveAttribute(
      "href",
      expect.stringContaining(`/inventory/waste-loss/new?`),
    );
    expect(writeOff.getAttribute("href")).toContain(`productId=${productId}`);
    expect(writeOff.getAttribute("href")).toContain(`lotId=${expiredLotId}`);
    expect(writeOff.getAttribute("href")).toContain("reason=Expired");
    expect(writeOff.getAttribute("href")).toContain("source=expiration");
    expect(writeOff.getAttribute("href")).toContain("quantity=7");

    expect(screen.getByTestId(`expiring-lot-view-${expiredLotId}`)).toBeInTheDocument();
    expect(screen.getByTestId(`expiring-lot-view-${nearLotId}`)).toBeInTheDocument();
    expect(screen.queryByTestId(`expiring-lot-write-off-${nearLotId}`)).not.toBeInTheDocument();
  });

  it("hides Write off when ManageInventory is denied", async () => {
    workspaceMock.sessionGrant = {
      productAccessAllowed: true,
      membershipRole: "OrganizationMember",
      productLocalRoleCode: "Cashier",
      mappedPosRoleCode: "Cashier",
    };
    renderPage();
    await screen.findByTestId(`expiring-lot-${expiredLotId}`);
    expect(screen.queryByTestId(`expiring-lot-write-off-${expiredLotId}`)).not.toBeInTheDocument();
    expect(screen.getByTestId(`expiring-lot-view-${expiredLotId}`)).toBeInTheDocument();
  });

  it("hides Write off when expired lot quantity is zero", async () => {
    vi.spyOn(inventoryClient, "listExpiringLots").mockResolvedValue({
      items: [expiredLot({ quantityOnHand: 0 })],
      totalCount: 1,
      page: 1,
      pageSize: 50,
      expiredCount: 1,
      nearExpiryCount: 0,
    });
    renderPage();
    await screen.findByTestId(`expiring-lot-${expiredLotId}`);
    await waitFor(() => {
      expect(screen.queryByTestId(`expiring-lot-write-off-${expiredLotId}`)).not.toBeInTheDocument();
    });
  });
});
