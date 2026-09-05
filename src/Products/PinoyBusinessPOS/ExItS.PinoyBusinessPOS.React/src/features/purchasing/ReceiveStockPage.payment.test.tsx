import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import type { PosCatalogProductDto } from "@/api/pos/pos-catalog-types";
import { ReceiveStockPage } from "@/features/purchasing/ReceiveStockPage";

const orgId = "11111111-1111-1111-1111-111111111111";
const branchId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
const productId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
const supplierId = "cccccccc-cccc-cccc-cccc-cccccccccccc";

const listSuppliers = vi.fn();
const listCatalogProducts = vi.fn();
const listCatalogCategories = vi.fn();
const createDirectPurchaseReceipt = vi.fn();

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

function productDto(): PosCatalogProductDto {
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
  };
}

async function addLine(user: ReturnType<typeof userEvent.setup>) {
  await user.type(screen.getByTestId("direct-product-search"), "Rice");
  await waitFor(() => {
    expect(screen.getByTestId(`direct-product-${productId}`)).toBeInTheDocument();
  });
  await user.clear(screen.getByTestId(`direct-line-qty-${productId}`));
  await user.type(screen.getByTestId(`direct-line-qty-${productId}`), "10");
  await user.clear(screen.getByTestId(`direct-line-cost-${productId}`));
  await user.type(screen.getByTestId(`direct-line-cost-${productId}`), "100");
  await user.click(screen.getByTestId(`direct-add-${productId}`));
  await waitFor(() => {
    expect(screen.getByTestId(`direct-receipt-line-${productId}`)).toBeInTheDocument();
  });
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
    createDirectPurchaseReceipt.mockResolvedValue({
      directPurchaseReceiptId: "dddddddd-dddd-dddd-dddd-dddddddddddd",
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
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );
  }

  it("allows no supplier + fully paid and sends PaidNow equal to total", async () => {
    const user = userEvent.setup();
    renderPage();
    await addLine(user);
    expect(screen.queryByTestId("receive-payment-section")).not.toBeInTheDocument();
    await user.click(screen.getByTestId("direct-review"));
    await waitFor(() => {
      expect(screen.getByTestId("receive-payment-section")).toBeInTheDocument();
    });
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

describe("ReceiveStockPage compact layout", () => {
  beforeEach(() => {
    listSuppliers.mockResolvedValue({
      items: [],
      totalCount: 0,
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
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  it("shows branch as subtitle, compact details, and hides source until other source", async () => {
    const user = userEvent.setup();
    render(
      <AppProviders>
        <MemoryRouter initialEntries={["/purchasing/receive-stock"]}>
          <Routes>
            <Route path="/purchasing/receive-stock" element={<ReceiveStockPage />} />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );

    expect(screen.getByTestId("page-header-subtitle")).toHaveTextContent("Main Branch");
    expect(screen.getByTestId("direct-purchase-details")).toBeInTheDocument();
    expect(screen.getByTestId("direct-add-products")).toBeInTheDocument();
    expect(screen.queryByTestId("direct-source-name")).not.toBeInTheDocument();

    await user.selectOptions(screen.getByTestId("direct-supplier"), "__other__");
    expect(screen.getByTestId("direct-source-name")).toBeInTheDocument();

    const review = screen.getByTestId("direct-review");
    expect(review).toBeDisabled();
    expect(review.className).not.toMatch(/w-full/);
  });
});
