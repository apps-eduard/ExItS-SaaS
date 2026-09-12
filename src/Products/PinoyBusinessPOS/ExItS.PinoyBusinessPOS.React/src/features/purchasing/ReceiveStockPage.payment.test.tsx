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
const categoryCanned = "cccccccc-cccc-cccc-cccc-cccccccanned";
const categorySnacks = "cccccccc-cccc-cccc-cccc-cccccccsnacks";
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
      pageSize: 20,
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
    expect(screen.queryByTestId("receive-payment-section")).not.toBeInTheDocument();
    expect(createDirectPurchaseReceipt).not.toHaveBeenCalled();
    await user.click(screen.getByTestId("direct-review"));
    await waitFor(() => {
      expect(screen.getByTestId("receive-payment-section")).toBeInTheDocument();
    });
    expect(createDirectPurchaseReceipt).not.toHaveBeenCalled();
    expect(screen.queryByTestId("receive-payment-mode-credit")).not.toBeInTheDocument();
    await user.click(screen.getByTestId("direct-confirm"));
    await waitFor(() => {
      expect(createDirectPurchaseReceipt).toHaveBeenCalled();
    });
    const body = createDirectPurchaseReceipt.mock.calls[0][1];
    expect(body.paidNow).toBe(1000);
    expect(body.dueDate).toBeNull();
    expect(body.paymentMethodAtReceipt).toBe("Cash");
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
    expect(JSON.stringify(body)).not.toMatch(/SupplierPayablePayment/i);
  });
});

describe("ReceiveStockPage active workspace", () => {
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
          sortOrder: 1,
          createdAtUtc: "2026-08-01T00:00:00Z",
          updatedAtUtc: "2026-08-01T00:00:00Z",
        },
        {
          categoryId: categorySnacks,
          organizationId: orgId,
          name: "Snacks",
          status: "Active",
          sortOrder: 2,
          createdAtUtc: "2026-08-01T00:00:00Z",
          updatedAtUtc: "2026-08-01T00:00:00Z",
        },
      ],
      totalCount: 2,
      page: 1,
      pageSize: 50,
    });
    listCatalogProducts.mockImplementation((_ws: unknown, params: { categoryId?: string }) => {
      const canned = productDto({
        productId,
        name: "Canned Corned Beef",
        sku: "PH-CAN-CORNEDBEEF",
      });
      const snack = productDto({
        productId: productId2,
        name: "Chips",
        sku: "PH-SNK-CHIPS",
      });
      if (params.categoryId === categoryCanned) {
        return Promise.resolve({
          items: [canned],
          totalCount: 1,
          page: 1,
          pageSize: 20,
        });
      }
      if (params.categoryId === categorySnacks) {
        return Promise.resolve({
          items: [snack],
          totalCount: 1,
          page: 1,
          pageSize: 20,
        });
      }
      return Promise.resolve({
        items: [canned, snack],
        totalCount: 2,
        page: 1,
        pageSize: 20,
      });
    });
    listDirectPurchases.mockResolvedValue({
      items: [
        {
          sourceId: receiptId,
          sourceType: "Local",
          occurredAtUtc: "2026-09-12T00:00:00Z",
          purchaseDate: "2026-09-12",
          sellerDisplayName: "Fresh Farms",
          referenceNumber: "DPR-1",
          lineCount: 2,
          totalAmount: 500,
          status: "Completed",
        },
      ],
      totalCount: 1,
      page: 1,
      pageSize: 8,
    });
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  it("loads All eligible products without requiring search", async () => {
    renderPage();
    await waitFor(() => {
      expect(listCatalogProducts).toHaveBeenCalled();
    });
    const firstCall = listCatalogProducts.mock.calls[0][1];
    expect(firstCall.categoryId).toBeUndefined();
    await waitFor(() => {
      expect(screen.getByTestId(`direct-product-${productId}`)).toBeInTheDocument();
      expect(screen.getByTestId(`direct-product-${productId2}`)).toBeInTheDocument();
    });
  });

  it("filters by category and restores All", async () => {
    const user = userEvent.setup();
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId("direct-category-all")).toBeInTheDocument();
    });
    await user.click(screen.getByTestId(`direct-category-${categoryCanned}`));
    await waitFor(() => {
      const last = listCatalogProducts.mock.calls.at(-1)?.[1];
      expect(last?.categoryId).toBe(categoryCanned);
    });
    await waitFor(() => {
      expect(screen.getByTestId(`direct-product-${productId}`)).toBeInTheDocument();
      expect(screen.queryByTestId(`direct-product-${productId2}`)).not.toBeInTheDocument();
    });
    await user.click(screen.getByTestId("direct-category-all"));
    await waitFor(() => {
      const last = listCatalogProducts.mock.calls.at(-1)?.[1];
      expect(last?.categoryId).toBeUndefined();
    });
    await waitFor(() => {
      expect(screen.getByTestId(`direct-product-${productId}`)).toBeInTheDocument();
      expect(screen.getByTestId(`direct-product-${productId2}`)).toBeInTheDocument();
    });
  });

  it("adds to receipt with editors on the right and shows Added state", async () => {
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
    expect(screen.getByTestId("direct-review")).toBeDisabled();
  });

  it("shows recent completed direct purchases without duplicating domain", async () => {
    renderPage();
    await waitFor(() => {
      expect(listDirectPurchases).toHaveBeenCalled();
    });
    const params = listDirectPurchases.mock.calls[0][1];
    expect(params.status).toBe("Completed");
    expect(params.pageSize).toBe(8);
    await waitFor(() => {
      expect(screen.getByTestId(`direct-recent-row-${receiptId}`)).toBeInTheDocument();
    });
    expect(screen.getByTestId("direct-view-all-purchases")).toHaveAttribute(
      "href",
      "/purchasing/direct-purchases",
    );
  });
});
