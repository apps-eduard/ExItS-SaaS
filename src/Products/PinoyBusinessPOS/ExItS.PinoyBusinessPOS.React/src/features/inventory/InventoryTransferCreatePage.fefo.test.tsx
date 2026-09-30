import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import * as inventoryClient from "@/api/pos/pos-inventory-client";
import * as transferClient from "@/api/pos/pos-inventory-transfer-client";
import { InventoryTransferCreatePage } from "@/features/inventory/InventoryTransferCreatePage";

const orgId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
const mainId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
const branchBId = "cccccccc-cccc-cccc-cccc-cccccccccccc";
const appleId = "aaaaaaaa-1111-1111-1111-111111111111";
const soapId = "11111111-1111-1111-1111-111111111111";
const lotA = "lot-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
const lotB = "lot-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
const lotC = "lot-cccc-cccc-cccc-cccccccccccc";

const workspaceMock = {
  boundWorkspace: {
    organizationId: orgId,
    organizationDisplayName: "Store",
    branchId: mainId,
    branchName: "Main Branch",
    experience: "operations" as const,
  },
  sessionGrant: {
    productAccessAllowed: true,
    membershipRole: "OrganizationOwner",
    productLocalRoleCode: "Owner",
  },
  workspaces: [
    {
      organizationId: orgId,
      displayName: "Store",
      branches: [
        {
          branchId: mainId,
          name: "Main Branch",
          secondaryLine: "",
          isPrimary: true,
          isActive: true,
        },
        {
          branchId: branchBId,
          name: "Iloilo Branch",
          secondaryLine: "",
          isPrimary: false,
          isActive: true,
        },
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

function account(
  overrides: Partial<inventoryClient.PosInventoryAccountDto> &
    Pick<inventoryClient.PosInventoryAccountDto, "productId" | "name" | "onHandQuantity">,
): inventoryClient.PosInventoryAccountDto {
  return {
    organizationId: orgId,
    unitOfMeasure: "kg",
    productStatus: "Active",
    isTracked: true,
    stockStatus: overrides.onHandQuantity > 0 ? "InStock" : "OutOfStock",
    isLowStock: false,
    createdAtUtc: "2026-08-29T08:00:00Z",
    updatedAtUtc: "2026-08-29T08:00:00Z",
    tracksExpiration: false,
    sku: "PH-FRU-APPLE",
    ...overrides,
  };
}

function lot(
  overrides: Partial<inventoryClient.PosInventoryLotDto> &
    Pick<
      inventoryClient.PosInventoryLotDto,
      "lotId" | "expirationDate" | "quantityOnHand" | "lotNumber"
    >,
): inventoryClient.PosInventoryLotDto {
  return {
    productId: appleId,
    expiryStatus: "Ok",
    createdAtUtc: "2026-01-01T00:00:00Z",
    updatedAtUtc: "2026-01-01T00:00:00Z",
    ...overrides,
  };
}

function renderCreate() {
  return render(
    <AppProviders>
      <MemoryRouter initialEntries={["/inventory/transfers/new"]}>
        <Routes>
          <Route path="/inventory/transfers/new" element={<InventoryTransferCreatePage />} />
          <Route path="/inventory/transfers/:transferId" element={<div>detail</div>} />
        </Routes>
      </MemoryRouter>
    </AppProviders>,
  );
}

async function chooseDestination(user: ReturnType<typeof userEvent.setup>, branchName: string) {
  await user.click(screen.getByTestId("transfer-destination-branch"));
  await user.click(await screen.findByRole("menuitem", { name: new RegExp(branchName, "i") }));
}

async function openProductFinder(user: ReturnType<typeof userEvent.setup>) {
  await user.click(screen.getByTestId("transfer-add-products-trigger"));
  await waitFor(() => {
    expect(screen.getByTestId("transfer-add-products")).toBeInTheDocument();
  });
}

async function setLineQuantity(
  user: ReturnType<typeof userEvent.setup>,
  lineKey: string,
  quantity: string,
) {
  const qty = screen.getByTestId(`transfer-line-qty-${lineKey}`);
  await user.click(qty);
  const input = screen.getByTestId(`transfer-line-qty-${lineKey}`);
  await user.clear(input);
  await user.type(input, quantity);
  await user.tab();
}

async function setChangeLotQuantity(
  user: ReturnType<typeof userEvent.setup>,
  lotId: string,
  quantity: string,
) {
  const input = screen.getByTestId(`transfer-change-lot-qty-${lotId}`);
  await user.click(input);
  await user.clear(input);
  await user.type(input, quantity);
  await user.tab();
}

describe("InventoryTransferCreatePage FEFO allocation", () => {
  beforeEach(() => {
    vi.spyOn(inventoryClient, "listInventory").mockResolvedValue({
      items: [
        account({
          productId: appleId,
          name: "Apple",
          onHandQuantity: 187,
          tracksExpiration: true,
          unitOfMeasure: "kg",
        }),
        account({
          productId: soapId,
          name: "Bath Soap Bar",
          onHandQuantity: 10,
          tracksExpiration: false,
          unitOfMeasure: "Piece",
          sku: "SOAP-1",
        }),
      ],
      totalCount: 2,
      page: 1,
      pageSize: 40,
    });
    vi.spyOn(inventoryClient, "listProductLots").mockResolvedValue({
      items: [
        lot({
          lotId: lotA,
          lotNumber: "LOT-A",
          expirationDate: "2026-09-12",
          quantityOnHand: 50,
        }),
        lot({
          lotId: lotB,
          lotNumber: "LOT-B",
          expirationDate: "2026-09-15",
          quantityOnHand: 37,
        }),
        lot({
          lotId: lotC,
          lotNumber: "LOT-C",
          expirationDate: "2026-09-30",
          quantityOnHand: 100,
        }),
      ],
      totalCount: 3,
      page: 1,
      pageSize: 50,
    });
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("Find Products shows lot count without Select lot dropdown", async () => {
    const user = userEvent.setup();
    renderCreate();
    await openProductFinder(user);

    await waitFor(() => {
      expect(screen.getByTestId(`transfer-picker-available-${appleId}`)).toHaveTextContent(
        /187\/kg · 3 lots · Tracks expiry/i,
      );
    });
    expect(screen.queryByTestId(`transfer-lot-select-${appleId}`)).not.toBeInTheDocument();
    expect(screen.queryByText(/Select lot/i)).not.toBeInTheDocument();
  });

  it("adds expiry product without selecting a lot and defaults to FEFO", async () => {
    const user = userEvent.setup();
    renderCreate();
    await chooseDestination(user, "Iloilo Branch");
    await openProductFinder(user);
    await waitFor(() => screen.getByTestId(`transfer-add-${appleId}`));
    await user.click(screen.getByTestId(`transfer-add-${appleId}`));

    await waitFor(() => {
      expect(screen.getByTestId(`transfer-line-${appleId}`)).toBeInTheDocument();
    });
    expect(screen.getByTestId(`transfer-line-allocation-${appleId}`)).toBeInTheDocument();
    expect(screen.getByTestId(`transfer-line-expiry-mode-${appleId}`)).toHaveTextContent(
      /FEFO automatically selected/i,
    );
    expect(screen.getByTestId(`transfer-line-alloc-slice-${appleId}-${lotA}`)).toHaveTextContent(
      /1 kg/,
    );
  });

  it("qty 60 produces 50 + 10 FEFO allocation", async () => {
    const user = userEvent.setup();
    renderCreate();
    await chooseDestination(user, "Iloilo Branch");
    await openProductFinder(user);
    await user.click(await screen.findByTestId(`transfer-add-${appleId}`));
    await waitFor(() => screen.getByTestId(`transfer-line-${appleId}`));

    await setLineQuantity(user, appleId, "60");

    await waitFor(() => {
      expect(screen.getByTestId(`transfer-line-alloc-slice-${appleId}-${lotA}`)).toHaveTextContent(
        /50 kg/,
      );
      expect(screen.getByTestId(`transfer-line-alloc-slice-${appleId}-${lotB}`)).toHaveTextContent(
        /10 kg/,
      );
    });
  });

  it("increasing and decreasing qty recalculates FEFO in auto mode", async () => {
    const user = userEvent.setup();
    renderCreate();
    await chooseDestination(user, "Iloilo Branch");
    await openProductFinder(user);
    await user.click(await screen.findByTestId(`transfer-add-${appleId}`));
    await waitFor(() => screen.getByTestId(`transfer-line-${appleId}`));

    await setLineQuantity(user, appleId, "90");
    await waitFor(() => {
      expect(screen.getByTestId(`transfer-line-alloc-slice-${appleId}-${lotC}`)).toHaveTextContent(
        /3 kg/,
      );
    });

    await setLineQuantity(user, appleId, "20");
    await waitFor(() => {
      expect(screen.getByTestId(`transfer-line-alloc-slice-${appleId}-${lotA}`)).toHaveTextContent(
        /20 kg/,
      );
      expect(
        screen.queryByTestId(`transfer-line-alloc-slice-${appleId}-${lotB}`),
      ).not.toBeInTheDocument();
    });
  });

  it("Change lots dialog validates allocation and supports Use FEFO", async () => {
    const user = userEvent.setup();
    renderCreate();
    await chooseDestination(user, "Iloilo Branch");
    await openProductFinder(user);
    await user.click(await screen.findByTestId(`transfer-add-${appleId}`));
    await waitFor(() => screen.getByTestId(`transfer-line-${appleId}`));
    await setLineQuantity(user, appleId, "60");

    await user.click(screen.getByTestId(`transfer-change-lots-${appleId}`));
    await waitFor(() => {
      expect(screen.getByTestId("transfer-change-lots-dialog")).toBeInTheDocument();
    });

    const qtyA = screen.getByTestId(`transfer-change-lot-qty-${lotA}`);
    const qtyB = screen.getByTestId(`transfer-change-lot-qty-${lotB}`);
    const qtyC = screen.getByTestId(`transfer-change-lot-qty-${lotC}`);
    expect(qtyA).toBeInTheDocument();
    expect(qtyB).toBeInTheDocument();
    expect(qtyC).toBeInTheDocument();

    await setChangeLotQuantity(user, lotA, "0");
    await setChangeLotQuantity(user, lotB, "20");
    await setChangeLotQuantity(user, lotC, "40");

    await waitFor(() => {
      expect(screen.getByTestId("transfer-change-lots-apply")).not.toBeDisabled();
    });
    await user.click(screen.getByTestId("transfer-change-lots-apply"));

    await waitFor(() => {
      expect(screen.queryByTestId("transfer-change-lots-dialog")).not.toBeInTheDocument();
      expect(screen.getByTestId(`transfer-line-expiry-mode-${appleId}`)).toHaveTextContent(
        /Custom lot allocation/i,
      );
    });

    await user.click(screen.getByTestId(`transfer-change-lots-${appleId}`));
    await waitFor(() => screen.getByTestId("transfer-change-lots-use-fefo"));
    await user.click(screen.getByTestId("transfer-change-lots-use-fefo"));
    await user.click(screen.getByTestId("transfer-change-lots-apply"));

    await waitFor(() => {
      expect(screen.getByTestId(`transfer-line-expiry-mode-${appleId}`)).toHaveTextContent(
        /FEFO automatically selected/i,
      );
      expect(screen.getByTestId(`transfer-line-alloc-slice-${appleId}-${lotA}`)).toHaveTextContent(
        /50 kg/,
      );
    });
  });

  it("manual allocation cannot exceed lot stock", async () => {
    const user = userEvent.setup();
    renderCreate();
    await chooseDestination(user, "Iloilo Branch");
    await openProductFinder(user);
    await user.click(await screen.findByTestId(`transfer-add-${appleId}`));
    await waitFor(() => screen.getByTestId(`transfer-line-${appleId}`));
    await setLineQuantity(user, appleId, "60");
    await user.click(screen.getByTestId(`transfer-change-lots-${appleId}`));
    await waitFor(() => screen.getByTestId("transfer-change-lots"));

    await setChangeLotQuantity(user, lotA, "55");
    await waitFor(() => {
      expect(
        Number(screen.getByTestId(`transfer-change-lot-qty-${lotA}`).getAttribute("value") ?? "0"),
      ).toBeLessThanOrEqual(50);
    });
  });

  it("Use max fills full lot on-hand and Apply sets line quantity to lot total", async () => {
    const user = userEvent.setup();
    renderCreate();
    await chooseDestination(user, "Iloilo Branch");
    await openProductFinder(user);
    await user.click(await screen.findByTestId(`transfer-add-${appleId}`));
    await waitFor(() => screen.getByTestId(`transfer-line-${appleId}`));
    await setLineQuantity(user, appleId, "10");
    await user.click(screen.getByTestId(`transfer-change-lots-${appleId}`));
    await waitFor(() => screen.getByTestId("transfer-change-lots"));

    await setChangeLotQuantity(user, lotA, "5");
    await setChangeLotQuantity(user, lotB, "10");
    await setChangeLotQuantity(user, lotC, "10");
    expect(screen.getByTestId("transfer-change-lots-allocated")).toHaveTextContent(/25/);
    await user.click(screen.getByTestId("transfer-change-lots-apply"));

    await waitFor(() => {
      expect(screen.queryByTestId("transfer-change-lots-dialog")).not.toBeInTheDocument();
    });
    expect(screen.getByTestId(`transfer-line-qty-${appleId}`)).toHaveTextContent("25");
    expect(screen.getByTestId(`transfer-line-alloc-slice-${appleId}-${lotA}`)).toHaveTextContent(
      /5 kg/,
    );
    expect(screen.getByTestId(`transfer-line-alloc-slice-${appleId}-${lotB}`)).toHaveTextContent(
      /10 kg/,
    );
    expect(screen.getByTestId(`transfer-line-alloc-slice-${appleId}-${lotC}`)).toHaveTextContent(
      /10 kg/,
    );
  });

  it("Use max on all lots including expired allows Apply at physical total", async () => {
    const user = userEvent.setup();
    vi.spyOn(inventoryClient, "listProductLots").mockResolvedValue({
      items: [
        lot({
          lotId: lotA,
          lotNumber: "LOT-A",
          expirationDate: "2026-09-12",
          quantityOnHand: 50,
          expiryStatus: "Expired",
        }),
        lot({
          lotId: lotB,
          lotNumber: "LOT-B",
          expirationDate: "2026-09-15",
          quantityOnHand: 37,
          expiryStatus: "Expired",
        }),
        lot({
          lotId: lotC,
          lotNumber: "LOT-C",
          expirationDate: "2026-09-30",
          quantityOnHand: 100,
          expiryStatus: "Ok",
        }),
      ],
      totalCount: 3,
      page: 1,
      pageSize: 50,
    });

    renderCreate();
    await chooseDestination(user, "Iloilo Branch");
    await openProductFinder(user);
    await user.click(await screen.findByTestId(`transfer-add-${appleId}`));
    await waitFor(() => screen.getByTestId(`transfer-line-${appleId}`));
    await user.click(screen.getByTestId(`transfer-change-lots-${appleId}`));
    await waitFor(() => screen.getByTestId("transfer-change-lots"));

    await user.click(screen.getByTestId(`transfer-change-lot-use-max-${lotA}`));
    await user.click(screen.getByTestId(`transfer-change-lot-use-max-${lotB}`));
    await user.click(screen.getByTestId(`transfer-change-lot-use-max-${lotC}`));
    expect(screen.getByTestId("transfer-change-lots-allocated")).toHaveTextContent(/187/);
    expect(screen.queryByTestId("transfer-change-lots-over-available")).not.toBeInTheDocument();
    expect(screen.getByTestId("transfer-change-lots-apply")).not.toBeDisabled();
    await user.click(screen.getByTestId("transfer-change-lots-apply"));
    await waitFor(() => {
      expect(screen.getByTestId(`transfer-line-qty-${appleId}`)).toHaveTextContent("187");
    });
  });

  it("Use FEFO redistributes current line quantity", async () => {
    const user = userEvent.setup();
    renderCreate();
    await chooseDestination(user, "Iloilo Branch");
    await openProductFinder(user);
    await user.click(await screen.findByTestId(`transfer-add-${appleId}`));
    await waitFor(() => screen.getByTestId(`transfer-line-${appleId}`));
    await setLineQuantity(user, appleId, "1");
    await user.click(screen.getByTestId(`transfer-change-lots-${appleId}`));
    await waitFor(() => screen.getByTestId("transfer-change-lots"));

    await setChangeLotQuantity(user, lotA, "0");
    await setChangeLotQuantity(user, lotC, "1");
    await user.click(screen.getByTestId("transfer-change-lots-use-fefo"));
    await waitFor(() => {
      expect(screen.getByTestId(`transfer-change-lot-qty-${lotA}`)).toHaveValue("1");
    });
    expect(screen.getByTestId(`transfer-change-lot-qty-${lotC}`)).toHaveValue("0");
    expect(screen.getByTestId("transfer-change-lots-apply")).not.toBeDisabled();
  });

  it("non-expiry product has no allocation UI", async () => {
    const user = userEvent.setup();
    renderCreate();
    await chooseDestination(user, "Iloilo Branch");
    await openProductFinder(user);
    await user.click(await screen.findByTestId(`transfer-add-${soapId}`));
    await waitFor(() => screen.getByTestId(`transfer-line-${soapId}`));

    expect(screen.queryByTestId(`transfer-line-allocation-${soapId}`)).not.toBeInTheDocument();
    expect(screen.queryByTestId(`transfer-change-lots-${soapId}`)).not.toBeInTheDocument();
  });

  it("payload expands one logical product into multiple SourceLotId lines", async () => {
    const user = userEvent.setup();
    const createSpy = vi.spyOn(transferClient, "createInventoryTransfer").mockResolvedValue({
      transferId: "transfer-1",
      transferNumber: "TR-1",
      status: "Draft",
    } as never);

    renderCreate();
    await chooseDestination(user, "Iloilo Branch");
    await openProductFinder(user);
    await user.click(await screen.findByTestId(`transfer-add-${appleId}`));
    await waitFor(() => screen.getByTestId(`transfer-line-${appleId}`));
    await setLineQuantity(user, appleId, "60");

    // Keep finder closed and confirm soap can coexist after reopening.
    await openProductFinder(user);
    await user.click(await screen.findByTestId(`transfer-add-${soapId}`));
    await waitFor(() => screen.getByTestId(`transfer-line-${soapId}`));
    await setLineQuantity(user, soapId, "2");

    await user.click(screen.getByTestId("transfer-save-draft"));

    await waitFor(() => {
      expect(createSpy).toHaveBeenCalled();
    });
    const payload = createSpy.mock.calls[0]![1] as {
      lines: { productId: string; quantity: number; sourceLotId: string | null }[];
    };
    expect(payload.lines).toEqual([
      { productId: appleId, quantity: 50, sourceLotId: lotA },
      { productId: appleId, quantity: 10, sourceLotId: lotB },
      { productId: soapId, quantity: 2, sourceLotId: null },
    ]);
    await waitFor(() => {
      expect(screen.getByText("detail")).toBeInTheDocument();
    });
  });

  it("mobile picker status shows compact available · lots · Tracks expiry", async () => {
    const user = userEvent.setup();
    renderCreate();
    await openProductFinder(user);
    await waitFor(() => {
      const matches = screen.getAllByTestId(`transfer-picker-available-${appleId}`);
      expect(matches.length).toBeGreaterThan(0);
      expect(matches[0]).toHaveTextContent(/187\/kg · 3 lots · Tracks expiry/i);
    });
    // ProductSelectionView list/table both use the same compact status text.
    expect(within(screen.getByTestId("transfer-product-picker")).queryByRole("combobox")).toBeNull();
  });

  it("excludes expired lot qty from picker availability and lot count", async () => {
    const user = userEvent.setup();
    vi.spyOn(inventoryClient, "listProductLots").mockResolvedValue({
      items: [
        lot({
          lotId: lotA,
          lotNumber: "LOT-A",
          expirationDate: "2026-09-12",
          quantityOnHand: 50,
          expiryStatus: "Expired",
        }),
        lot({
          lotId: lotB,
          lotNumber: "LOT-B",
          expirationDate: "2026-09-25",
          quantityOnHand: 37,
          expiryStatus: "Ok",
        }),
        lot({
          lotId: lotC,
          lotNumber: "LOT-C",
          expirationDate: "2026-09-30",
          quantityOnHand: 100,
          expiryStatus: "Ok",
        }),
      ],
      totalCount: 3,
      page: 1,
      pageSize: 50,
    });
    vi.spyOn(inventoryClient, "listInventory").mockResolvedValue({
      items: [
        account({
          productId: appleId,
          name: "Apple",
          onHandQuantity: 187,
          tracksExpiration: true,
          unitOfMeasure: "kg",
        }),
      ],
      totalCount: 1,
      page: 1,
      pageSize: 40,
    });

    renderCreate();
    await openProductFinder(user);
    await waitFor(() => {
      expect(screen.getByTestId(`transfer-picker-available-${appleId}`)).toHaveTextContent(
        /137\/kg · 2 lots · Tracks expiry/i,
      );
    });
  });

  it("Change lots allows expired lot quantity via stepper and Use FEFO may include it", async () => {
    const user = userEvent.setup();
    vi.spyOn(inventoryClient, "listProductLots").mockResolvedValue({
      items: [
        lot({
          lotId: lotA,
          lotNumber: "LOT-A",
          expirationDate: "2026-09-12",
          quantityOnHand: 50,
          expiryStatus: "Expired",
        }),
        lot({
          lotId: lotB,
          lotNumber: "LOT-B",
          expirationDate: "2026-09-25",
          quantityOnHand: 37,
          expiryStatus: "Ok",
        }),
        lot({
          lotId: lotC,
          lotNumber: "LOT-C",
          expirationDate: "2026-09-30",
          quantityOnHand: 100,
          expiryStatus: "Ok",
        }),
      ],
      totalCount: 3,
      page: 1,
      pageSize: 50,
    });

    renderCreate();
    await chooseDestination(user, "Iloilo Branch");
    await openProductFinder(user);
    await user.click(await screen.findByTestId(`transfer-add-${appleId}`));
    await waitFor(() => screen.getByTestId(`transfer-line-${appleId}`));
    await setLineQuantity(user, appleId, "60");

    await waitFor(() => {
      expect(screen.getByTestId(`transfer-line-alloc-slice-${appleId}-${lotB}`)).toHaveTextContent(
        /37 kg/,
      );
      expect(screen.getByTestId(`transfer-line-alloc-slice-${appleId}-${lotC}`)).toHaveTextContent(
        /23 kg/,
      );
    });

    await user.click(screen.getByTestId(`transfer-change-lots-${appleId}`));
    await waitFor(() => screen.getByTestId("transfer-change-lots"));
    expect(screen.getByTestId(`transfer-change-lot-expired-label-${lotA}`)).toHaveTextContent(
      /Expired/i,
    );
    expect(screen.getByTestId(`transfer-change-lot-qty-${lotA}`)).toBeInTheDocument();
    expect(screen.getByTestId(`transfer-change-lot-qty-${lotB}`)).toBeInTheDocument();

    await user.click(screen.getByTestId("transfer-change-lots-use-fefo"));
    await waitFor(() => {
      expect(screen.getByTestId(`transfer-change-lot-qty-${lotA}`)).toHaveValue("50");
    });
    expect(screen.getByTestId(`transfer-change-lot-qty-${lotB}`)).toHaveValue("10");
  });

  it("all-expired product cannot be added", async () => {
    const user = userEvent.setup();
    vi.spyOn(inventoryClient, "listProductLots").mockResolvedValue({
      items: [
        lot({
          lotId: lotA,
          lotNumber: "LOT-A",
          expirationDate: "2026-01-01",
          quantityOnHand: 100,
          expiryStatus: "Expired",
        }),
      ],
      totalCount: 1,
      page: 1,
      pageSize: 50,
    });
    vi.spyOn(inventoryClient, "listInventory").mockResolvedValue({
      items: [
        account({
          productId: appleId,
          name: "Apple",
          onHandQuantity: 100,
          tracksExpiration: true,
          unitOfMeasure: "kg",
        }),
      ],
      totalCount: 1,
      page: 1,
      pageSize: 40,
    });

    renderCreate();
    await openProductFinder(user);
    await waitFor(() => {
      expect(screen.getByTestId(`transfer-picker-unavailable-${appleId}`)).toBeInTheDocument();
    });
    expect(screen.queryByTestId(`transfer-add-${appleId}`)).not.toBeInTheDocument();
  });
});
