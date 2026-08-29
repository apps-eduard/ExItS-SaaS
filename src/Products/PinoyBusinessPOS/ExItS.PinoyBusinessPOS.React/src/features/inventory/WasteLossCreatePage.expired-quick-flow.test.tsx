import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import * as catalogClient from "@/api/pos/pos-catalog-client";
import * as inventoryClient from "@/api/pos/pos-inventory-client";
import * as wasteLossClient from "@/api/pos/pos-waste-loss-client";
import { WasteLossCreatePage } from "@/features/inventory/WasteLossCreatePage";

const orgId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
const branchId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
const productId = "cccccccc-cccc-cccc-cccc-cccccccccccc";
const otherProductId = "dddddddd-dddd-dddd-dddd-dddddddddddd";
const expiredLotId = "11111111-1111-1111-1111-111111111111";
const validLotId = "22222222-2222-2222-2222-222222222222";
const wasteLossId = "99999999-9999-9999-9999-999999999999";

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

vi.mock("@/lib/secure-mutation-id", () => ({
  createSecureMutationId: () => ({ ok: true as const, id: wasteLossId }),
}));

function inventoryAccount(overrides: Record<string, unknown> = {}) {
  return {
    productId,
    organizationId: orgId,
    name: "Milk 1L",
    unitOfMeasure: "Piece",
    productStatus: "Active",
    isTracked: true,
    onHandQuantity: 25,
    hasOpeningStock: true,
    stockStatus: "InStock",
    isLowStock: false,
    tracksExpiration: true,
    createdAtUtc: "2026-01-01T00:00:00Z",
    updatedAtUtc: "2026-01-01T00:00:00Z",
    ...overrides,
  };
}

function expiredLot(overrides: Partial<inventoryClient.PosInventoryLotDto> = {}) {
  return {
    lotId: expiredLotId,
    productId,
    branchId,
    lotNumber: "EXP-001",
    expirationDate: "2026-08-01",
    quantityOnHand: 7,
    expiryStatus: "Expired",
    createdAtUtc: "2026-01-01T00:00:00Z",
    updatedAtUtc: "2026-01-01T00:00:00Z",
    ...overrides,
  } satisfies inventoryClient.PosInventoryLotDto;
}

function validLot() {
  return {
    lotId: validLotId,
    productId,
    branchId,
    lotNumber: "OK-001",
    expirationDate: "2026-12-30",
    quantityOnHand: 20,
    expiryStatus: "Ok",
    createdAtUtc: "2026-01-01T00:00:00Z",
    updatedAtUtc: "2026-01-01T00:00:00Z",
  } satisfies inventoryClient.PosInventoryLotDto;
}

function renderQuickFlow(query: string) {
  return render(
    <AppProviders>
      <MemoryRouter initialEntries={[`/inventory/waste-loss/new?${query}`]}>
        <Routes>
          <Route path="/inventory/waste-loss/new" element={<WasteLossCreatePage />} />
          <Route
            path="/inventory/waste-loss/:wasteLossId"
            element={<div data-testid="waste-loss-detail-stub" />}
          />
        </Routes>
      </MemoryRouter>
    </AppProviders>,
  );
}

describe("WasteLossCreatePage expired stock quick flow", () => {
  beforeEach(() => {
    workspaceMock.boundWorkspace.branchId = branchId;
    workspaceMock.sessionGrant = {
      productAccessAllowed: true,
      membershipRole: "OrganizationOwner",
      productLocalRoleCode: "Owner",
      mappedPosRoleCode: "Owner",
    };
    vi.spyOn(inventoryClient, "listInventory").mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 40,
    });
    vi.spyOn(catalogClient, "listCatalogProducts").mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 40,
    } as never);
    vi.spyOn(inventoryClient, "getInventoryProduct").mockResolvedValue(
      inventoryAccount() as never,
    );
    vi.spyOn(catalogClient, "getCatalogProduct").mockResolvedValue({
      productId,
      name: "Milk 1L",
      unitOfMeasure: "Piece",
      tracksExpiration: true,
      updatedAtUtc: "2026-01-01T00:00:00Z",
    } as never);
    vi.spyOn(inventoryClient, "listProductLots").mockResolvedValue({
      items: [expiredLot(), validLot()],
      totalCount: 2,
      page: 1,
      pageSize: 50,
    });
    vi.spyOn(wasteLossClient, "createWasteLoss").mockResolvedValue({
      wasteLossId,
      wasteLossNumber: "WL-20260830-0001",
      reason: "Expired",
      status: "Posted",
    } as never);
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("prefills Expired reason, exact lot, and current lot quantity", async () => {
    renderQuickFlow(
      `productId=${productId}&lotId=${expiredLotId}&reason=Expired&source=expiration&quantity=10`,
    );

    await screen.findByTestId("waste-loss-expired-context");
    expect(screen.getByTestId("waste-loss-reason")).toHaveValue("Expired");
    expect(screen.getByTestId(`waste-loss-line-qty-${productId}`)).toHaveValue(7);
    expect(screen.getByTestId("waste-loss-lot-available")).toHaveTextContent("7");
    expect(screen.getByTestId(`waste-loss-lots-${productId}`)).toBeInTheDocument();
    const selected = document.querySelector(
      `input[name="inventory-lot-picker"][value="${expiredLotId}"]`,
    ) as HTMLInputElement | null;
    expect(selected?.checked).toBe(true);
    expect(
      screen.getAllByTestId(`waste-loss-lot-${productId}-${expiredLotId}`).length,
    ).toBeGreaterThan(0);
  });

  it("uses refreshed lot quantity when query quantity is stale", async () => {
    vi.spyOn(inventoryClient, "listProductLots").mockResolvedValue({
      items: [expiredLot({ quantityOnHand: 6 }), validLot()],
      totalCount: 2,
      page: 1,
      pageSize: 50,
    });
    renderQuickFlow(
      `productId=${productId}&lotId=${expiredLotId}&reason=Expired&source=expiration&quantity=10`,
    );
    await screen.findByTestId("waste-loss-expired-context");
    expect(screen.getByTestId(`waste-loss-line-qty-${productId}`)).toHaveValue(6);
  });

  it("blocks posting when lot quantity is zero", async () => {
    vi.spyOn(inventoryClient, "listProductLots").mockResolvedValue({
      items: [expiredLot({ quantityOnHand: 0 }), validLot()],
      totalCount: 2,
      page: 1,
      pageSize: 50,
    });
    renderQuickFlow(
      `productId=${productId}&lotId=${expiredLotId}&reason=Expired&source=expiration`,
    );
    await screen.findByTestId("waste-loss-lot-zero");
    expect(screen.queryByTestId("waste-loss-submit")).not.toBeInTheDocument();
    expect(screen.getByTestId("waste-loss-back-expiration")).toBeInTheDocument();
  });

  it("fails closed when lot is missing and does not select another lot", async () => {
    vi.spyOn(inventoryClient, "listProductLots").mockResolvedValue({
      items: [validLot()],
      totalCount: 1,
      page: 1,
      pageSize: 50,
    });
    renderQuickFlow(
      `productId=${productId}&lotId=${expiredLotId}&reason=Expired&source=expiration`,
    );
    await screen.findByTestId("waste-loss-lot-unavailable");
    expect(screen.queryByTestId("waste-loss-submit")).not.toBeInTheDocument();
  });

  it("fails closed when lot belongs to a different product", async () => {
    vi.spyOn(inventoryClient, "listProductLots").mockResolvedValue({
      items: [expiredLot({ productId: otherProductId })],
      totalCount: 1,
      page: 1,
      pageSize: 50,
    });
    renderQuickFlow(
      `productId=${productId}&lotId=${expiredLotId}&reason=Expired&source=expiration`,
    );
    await screen.findByTestId("waste-loss-lot-unavailable");
  });

  it("shows notice when lot is no longer expired but keeps review form", async () => {
    vi.spyOn(inventoryClient, "listProductLots").mockResolvedValue({
      items: [expiredLot({ expiryStatus: "Ok", expirationDate: "2026-12-30" }), validLot()],
      totalCount: 2,
      page: 1,
      pageSize: 50,
    });
    renderQuickFlow(
      `productId=${productId}&lotId=${expiredLotId}&reason=Expired&source=expiration`,
    );
    await screen.findByTestId("waste-loss-lot-not-expired-notice");
    expect(screen.getByTestId("waste-loss-reason")).toHaveValue("Expired");
    expect(screen.getByTestId("waste-loss-submit")).toBeEnabled();
  });

  it("allows partial quantity edit then posts exact lot line", async () => {
    const user = userEvent.setup();
    renderQuickFlow(
      `productId=${productId}&lotId=${expiredLotId}&reason=Expired&source=expiration`,
    );
    await screen.findByTestId("waste-loss-submit");
    const qty = screen.getByTestId(`waste-loss-line-qty-${productId}`);
    await user.clear(qty);
    await user.type(qty, "5");
    await user.click(screen.getByTestId("waste-loss-submit"));

    await waitFor(() => {
      expect(wasteLossClient.createWasteLoss).toHaveBeenCalledWith(
        expect.objectContaining({ organizationId: orgId, branchId }),
        expect.objectContaining({
          reason: "Expired",
          wasteLossId,
          lines: [
            expect.objectContaining({
              productId,
              quantity: 5,
              inventoryLotId: expiredLotId,
            }),
          ],
        }),
      );
    });
    await screen.findByTestId("waste-loss-detail-stub");
  });

  it("disables submit while saving to prevent double submit", async () => {
    const user = userEvent.setup();
    let resolveCreate!: (value: wasteLossClient.WasteLossDto) => void;
    vi.spyOn(wasteLossClient, "createWasteLoss").mockImplementation(
      () =>
        new Promise<wasteLossClient.WasteLossDto>((resolve) => {
          resolveCreate = resolve;
        }),
    );
    renderQuickFlow(
      `productId=${productId}&lotId=${expiredLotId}&reason=Expired&source=expiration`,
    );
    const submit = await screen.findByTestId("waste-loss-submit");
    await user.click(submit);
    await waitFor(() => expect(submit).toBeDisabled());
    expect(wasteLossClient.createWasteLoss).toHaveBeenCalledTimes(1);
    resolveCreate({
      wasteLossId,
      organizationId: orgId,
      wasteLossNumber: "WL-20260830-0001",
      occurredAtUtc: "2026-08-30T00:00:00Z",
      reason: "Expired",
      status: "Posted",
      costStatus: "Unavailable",
      createdByUserId: "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee",
      createdAtUtc: "2026-08-30T00:00:00Z",
      lines: [],
    });
    await screen.findByTestId("waste-loss-detail-stub");
  });
});
