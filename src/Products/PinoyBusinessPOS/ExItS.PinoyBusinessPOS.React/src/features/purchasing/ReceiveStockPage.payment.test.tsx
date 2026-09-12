import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import type { PosCatalogProductDto } from "@/api/pos/pos-catalog-types";
import { ReceiveStockPage } from "@/features/purchasing/ReceiveStockPage";

const orgId = "11111111-1111-1111-1111-111111111111";
const branchId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
const productId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
const productId2 = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2";
const productId3 = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb3";
const categoryCanned = "cccccccc-cccc-cccc-cccc-cccccccanned";
const categoryFruits = "cccccccc-cccc-cccc-cccc-cccccccfruit";
const supplierId = "cccccccc-cccc-cccc-cccc-cccccccccccc";
const receiptId = "dddddddd-dddd-dddd-dddd-dddddddddddd";

const listSuppliers = vi.fn();
const listCatalogProducts = vi.fn();
const listCatalogCategories = vi.fn();
const createDirectPurchaseReceipt = vi.fn();
const listDirectPurchases = vi.fn();

const workspaceMock = {
  boundWorkspace: {
    organizationId: orgId,
    organizationDisplayName: "Kizy Store",
    branchId,
    branchName: "Main Branch",
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

vi.mock("@/api/pos/pos-suppliers-client", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/api/pos/pos-suppliers-client")>();
  return {
    ...actual,
    listSuppliers: (...args: unknown[]) => listSuppliers(...args),
  };
});

vi.mock("@/api/pos/pos-catalog-client", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/api/pos/pos-catalog-client")>();
  return {
    ...actual,
    listCatalogProducts: (...args: unknown[]) => listCatalogProducts(...args),
    listCatalogCategories: (...args: unknown[]) => listCatalogCategories(...args),
  };
});

vi.mock("@/api/pos/pos-direct-purchase-receipts-client", async (importOriginal) => {
  const actual =
    await importOriginal<typeof import("@/api/pos/pos-direct-purchase-receipts-client")>();
  return {
    ...actual,
    createDirectPurchaseReceipt: (...args: unknown[]) => createDirectPurchaseReceipt(...args),
  };
});

vi.mock("@/api/pos/pos-direct-purchases-client", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/api/pos/pos-direct-purchases-client")>();
  return {
    ...actual,
    listDirectPurchases: (...args: unknown[]) => listDirectPurchases(...args),
  };
});

function productDto(overrides: Partial<PosCatalogProductDto> = {}): PosCatalogProductDto {
  return {
    productId,
    organizationId: orgId,
    name: "Rice 25kg",
    sku: "RICE-25",
    barcode: null,
    unitOfMeasure: "bag",
    sellingMode: "Standard",
    sellingPrice: 1200,
    status: "Active",
    createdAtUtc: "2026-08-01T00:00:00Z",
    updatedAtUtc: "2026-08-01T00:00:00Z",
    isTracked: true,
    tracksExpiration: false,
    ...overrides,
  };
}

const canned = () =>
  productDto({
    productId,
    name: "Canned Corned Beef",
    sku: "PH-CAN-CORNEDBEEF",
    categoryId: categoryCanned,
    unitOfMeasure: "Can",
  });
const fruit = () =>
  productDto({
    productId: productId2,
    name: "Apple",
    sku: "PH-FRU-APPLE",
    categoryId: categoryFruits,
    unitOfMeasure: "Kg",
  });
const other = () =>
  productDto({
    productId: productId3,
    name: "Battery AA Pack",
    sku: "PH-GM-BATTERY",
    categoryId: "cccccccc-cccc-cccc-cccc-cccccccgener",
    unitOfMeasure: "Pack",
  });

function renderPage() {
  return render(
    <AppProviders>
      <MemoryRouter initialEntries={["/purchasing/receive-stock"]}>
        <Routes>
          <Route path="/purchasing/receive-stock" element={<ReceiveStockPage />} />
          <Route
            path="/purchasing/direct-purchases/:id"
            element={<div data-testid="direct-detail-redirect" />}
          />
          <Route
            path="/purchasing/direct-purchases"
            element={<div data-testid="direct-list-redirect" />}
          />
        </Routes>
      </MemoryRouter>
    </AppProviders>,
  );
}

async function addLine(
  user: ReturnType<typeof userEvent.setup>,
  opts?: { qty?: string; cost?: string },
) {
  await waitFor(() => {
    expect(screen.getByTestId(`direct-product-${productId}`)).toBeInTheDocument();
  });
  await user.click(screen.getByTestId(`direct-add-${productId}`));
  await waitFor(() => {
    expect(screen.getByTestId(`direct-receipt-line-${productId}`)).toBeInTheDocument();
  });
  const qty = screen.getByTestId(`direct-line-qty-${productId}`);
  const cost = screen.getByTestId(`direct-line-cost-${productId}`);
  await user.clear(qty);
  await user.type(qty, opts?.qty ?? "10");
  await user.clear(cost);
  await user.type(cost, opts?.cost ?? "100");
}

describe("ReceiveStockPage payment at receipt", () => {
  beforeEach(() => {
    listSuppliers.mockResolvedValue({
      items: [
        {
          supplierId,
          organizationId: orgId,
          supplierCode: "SUP1",
          name: "Fresh Farms",
          status: "Active",
          connectionType: "Manual",
          createdAtUtc: "2026-08-01T00:00:00Z",
          updatedAtUtc: "2026-08-01T00:00:00Z",
        },
      ],
      totalCount: 1,
      page: 1,
      pageSize: 100,
    });
    listCatalogCategories.mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 50,
    });
    listCatalogProducts.mockResolvedValue({
      items: [productDto()],
      totalCount: 1,
      page: 1,
      pageSize: 100,
    });
    listDirectPurchases.mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 8,
    });
    createDirectPurchaseReceipt.mockResolvedValue({
      directPurchaseReceiptId: receiptId,
      receiptNumber: "DPR-1",
      organizationId: orgId,
      branchId,
      status: "Posted",
      lines: [],
    });
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  it("allows no supplier + fully paid and sends PaidNow equal to total", async () => {
    const user = userEvent.setup();
    renderPage();
    await addLine(user);
    expect(createDirectPurchaseReceipt).not.toHaveBeenCalled();
    await user.click(screen.getByTestId("direct-review"));
    await waitFor(() => {
      expect(screen.getByTestId("receive-payment-section")).toBeInTheDocument();
    });
    expect(createDirectPurchaseReceipt).not.toHaveBeenCalled();
    await user.click(screen.getByTestId("direct-confirm"));
    await waitFor(() => {
      expect(createDirectPurchaseReceipt).toHaveBeenCalled();
    });
    const body = createDirectPurchaseReceipt.mock.calls[0][1];
    expect(body.paidNow).toBe(1000);
    expect(body.supplierId).toBeNull();
  });

  it("hides supplier credit mode without supplier (blocks no-supplier credit)", async () => {
    const user = userEvent.setup();
    renderPage();
    await addLine(user);
    await user.click(screen.getByTestId("direct-review"));
    await waitFor(() => {
      expect(screen.getByTestId("receive-payment-section")).toBeInTheDocument();
    });
    expect(screen.queryByTestId("receive-payment-mode-credit")).not.toBeInTheDocument();
  });

  it("sends DueDate and PaymentMethodAtReceipt for supplier credit", async () => {
    const user = userEvent.setup();
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId("direct-supplier")).toContainHTML(supplierId);
    });
    await user.selectOptions(screen.getByTestId("direct-supplier"), supplierId);
    await addLine(user);
    await user.click(screen.getByTestId("direct-review"));
    await waitFor(() => {
      expect(screen.getByTestId("receive-payment-mode-credit")).toBeInTheDocument();
    });
    await user.click(screen.getByTestId("receive-payment-mode-credit"));
    const paidInput = screen.getByTestId("receive-payment-paid-now");
    await user.clear(paidInput);
    await user.type(paidInput, "400");
    await user.type(screen.getByTestId("receive-payment-due-date"), "2026-10-01");
    await user.selectOptions(screen.getByTestId("receive-payment-method"), "GCash");
    await user.click(screen.getByTestId("direct-confirm"));
    await waitFor(() => {
      expect(createDirectPurchaseReceipt).toHaveBeenCalled();
    });
    const body = createDirectPurchaseReceipt.mock.calls[0][1];
    expect(body.paidNow).toBe(400);
    expect(body.dueDate).toBe("2026-10-01");
    expect(body.paymentMethodAtReceipt).toBe("GCash");
  });
});

describe("ReceiveStockPage dual table workspace", () => {
  beforeEach(() => {
    listSuppliers.mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 100,
    });
    listCatalogCategories.mockResolvedValue({
      items: [
        {
          categoryId: categoryCanned,
          organizationId: orgId,
          name: "Canned Goods",
          status: "Active",
          createdAtUtc: "2026-08-01T00:00:00Z",
          updatedAtUtc: "2026-08-01T00:00:00Z",
        },
        {
          categoryId: categoryFruits,
          organizationId: orgId,
          name: "Fresh Fruits",
          status: "Active",
          createdAtUtc: "2026-08-01T00:00:00Z",
          updatedAtUtc: "2026-08-01T00:00:00Z",
        },
      ],
      totalCount: 2,
      page: 1,
      pageSize: 50,
    });
    listCatalogProducts.mockImplementation((_ws: unknown, params: { categoryId?: string }) => {
      const all = [canned(), fruit(), other()];
      if (params.categoryId) {
        return Promise.resolve({
          items: all.filter((p) => p.categoryId === params.categoryId),
          totalCount: 1,
          page: 1,
          pageSize: 100,
        });
      }
      return Promise.resolve({
        items: all,
        totalCount: 3,
        page: 1,
        pageSize: 100,
      });
    });
    listDirectPurchases.mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 8,
    });
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  it("shows all eligible products when All is active", async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId(`direct-product-${productId}`)).toBeInTheDocument();
      expect(screen.getByTestId(`direct-product-${productId2}`)).toBeInTheDocument();
      expect(screen.getByTestId(`direct-product-${productId3}`)).toBeInTheDocument();
    });
    const last = listCatalogProducts.mock.calls.at(-1)?.[1];
    expect(last?.categoryId).toBeUndefined();
  });

  it("supports multi-category OR filtering with removable chips", async () => {
    const user = userEvent.setup();
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId("direct-category-multiselect-trigger")).toBeInTheDocument();
    });

    await user.click(screen.getByTestId("direct-category-multiselect-trigger"));
    await user.click(screen.getByTestId(`direct-category-option-${categoryCanned}`));
    await waitFor(() => {
      expect(screen.getByTestId(`direct-product-${productId}`)).toBeInTheDocument();
      expect(screen.queryByTestId(`direct-product-${productId2}`)).not.toBeInTheDocument();
    });

    if (!screen.queryByTestId(`direct-category-option-${categoryFruits}`)) {
      await user.click(screen.getByTestId("direct-category-multiselect-trigger"));
    }
    await user.click(screen.getByTestId(`direct-category-option-${categoryFruits}`));
    await waitFor(() => {
      expect(screen.getByTestId(`direct-product-${productId}`)).toBeInTheDocument();
      expect(screen.getByTestId(`direct-product-${productId2}`)).toBeInTheDocument();
      expect(screen.queryByTestId(`direct-product-${productId3}`)).not.toBeInTheDocument();
    });
    expect(screen.getByTestId(`direct-category-chip-${categoryCanned}`)).toBeInTheDocument();
    expect(screen.getByTestId(`direct-category-chip-${categoryFruits}`)).toBeInTheDocument();

    await user.click(screen.getByTestId(`direct-category-chip-${categoryCanned}`));
    await waitFor(() => {
      expect(screen.queryByTestId(`direct-product-${productId}`)).not.toBeInTheDocument();
      expect(screen.getByTestId(`direct-product-${productId2}`)).toBeInTheDocument();
    });

    await user.click(screen.getByTestId("direct-category-all"));
    await waitFor(() => {
      expect(screen.getByTestId(`direct-product-${productId}`)).toBeInTheDocument();
      expect(screen.getByTestId(`direct-product-${productId2}`)).toBeInTheDocument();
      expect(screen.getByTestId(`direct-product-${productId3}`)).toBeInTheDocument();
    });
  });

  it("moves qty/cost editing to receipt table only", async () => {
    const user = userEvent.setup();
    renderPage();
    expect(screen.getByTestId("direct-receipt-empty")).toBeInTheDocument();
    await waitFor(() => {
      expect(screen.getByTestId(`direct-add-${productId}`)).toBeInTheDocument();
    });
    expect(
      within(screen.getByTestId(`direct-product-${productId}`)).queryByTestId(
        `direct-line-qty-${productId}`,
      ),
    ).not.toBeInTheDocument();

    await user.click(screen.getByTestId(`direct-add-${productId}`));
    await waitFor(() => {
      expect(screen.getByTestId(`direct-receipt-line-${productId}`)).toBeInTheDocument();
      expect(screen.getByTestId(`direct-added-${productId}`)).toBeInTheDocument();
    });
    expect(screen.getByTestId("direct-review")).toBeDisabled();

    await user.clear(screen.getByTestId(`direct-line-qty-${productId}`));
    await user.type(screen.getByTestId(`direct-line-qty-${productId}`), "2");
    await user.type(screen.getByTestId(`direct-line-cost-${productId}`), "45.60");
    expect(screen.getByTestId("direct-review")).not.toBeDisabled();
    expect(createDirectPurchaseReceipt).not.toHaveBeenCalled();

    await user.click(screen.getByTestId(`direct-remove-${productId}`));
    expect(screen.getByTestId("direct-receipt-empty")).toBeInTheDocument();
  });
});
