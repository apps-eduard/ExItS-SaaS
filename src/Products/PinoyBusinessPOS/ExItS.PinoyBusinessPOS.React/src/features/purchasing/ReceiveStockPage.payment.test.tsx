import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { useEffect } from "react";
import { render, screen, waitFor, within, fireEvent } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes, useLocation, useNavigate } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import type { PosCatalogProductDto } from "@/api/pos/pos-catalog-types";
import { useToast } from "@/components/exits/ToastProvider";
import {
  buildReceiveMarginWarningToast,
  type ReceiveMarginWarningFlash,
} from "@/features/purchasing/receive-cost-margin";
import { ReceiveStockPage } from "@/features/purchasing/ReceiveStockPage";
import { useI18n } from "@/i18n/I18nProvider";

const orgId = "11111111-1111-1111-1111-111111111111";
const branchId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
const productId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
const productId2 = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2";
const productId3 = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb3";
const categoryCanned = "cccccccc-cccc-cccc-cccc-cccccccanned";
const categoryFruits = "cccccccc-cccc-cccc-cccc-cccccccfruit";
const categoryEmpty = "cccccccc-cccc-cccc-cccc-cccccccempty";
const supplierId = "cccccccc-cccc-cccc-cccc-cccccccccccc";
const receiptId = "dddddddd-dddd-dddd-dddd-dddddddddddd";

const listSuppliers = vi.fn();
const listCatalogProducts = vi.fn();
const listCatalogCategories = vi.fn();
const createDirectPurchaseReceipt = vi.fn();
const listDirectPurchases = vi.fn();
const getInventoryProduct = vi.fn();
const enableExpirationTracking = vi.fn();

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

vi.mock("@/api/pos/pos-inventory-client", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/api/pos/pos-inventory-client")>();
  return {
    ...actual,
    getInventoryProduct: (...args: unknown[]) => getInventoryProduct(...args),
    enableExpirationTracking: (...args: unknown[]) => enableExpirationTracking(...args),
  };
});

function inventoryAccount(overrides: Record<string, unknown> = {}) {
  return {
    productId,
    organizationId: orgId,
    name: "Rice 25kg",
    unitOfMeasure: "bag",
    productStatus: "Active",
    isTracked: true,
    onHandQuantity: 0,
    stockStatus: "InStock",
    isLowStock: false,
    createdAtUtc: "2026-08-01T00:00:00Z",
    updatedAtUtc: "2026-08-01T00:00:00Z",
    tracksExpiration: false,
    expirationWarningDays: 7,
    ...overrides,
  };
}

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

function DirectPurchaseDetailRedirect() {
  const location = useLocation();
  const navigate = useNavigate();
  const { showToast } = useToast();
  const { t } = useI18n();

  useEffect(() => {
    const flash = (location.state as { receiveMarginWarning?: ReceiveMarginWarningFlash } | null)
      ?.receiveMarginWarning;
    if (!flash || !(flash.count > 0)) {
      return;
    }
    showToast(
      buildReceiveMarginWarningToast({
        count: flash.count,
        productId: flash.productId,
        title: t("purchasing.sellingPriceNeedsReview"),
        detailSingle: t("purchasing.sellingPriceNeedsReviewDetail"),
        detailMany: t("purchasing.sellingPriceNeedsReviewDetailMany"),
        reviewPrice: t("purchasing.reviewPrice"),
        reviewPrices: t("purchasing.reviewPrices"),
      }),
    );
    navigate(location.pathname, { replace: true, state: {} });
  }, [location.pathname, location.state, navigate, showToast, t]);

  return <div data-testid="direct-detail-redirect" />;
}

function renderPage() {
  return render(
    <AppProviders>
      <MemoryRouter initialEntries={["/purchasing/receive-stock"]}>
        <Routes>
          <Route path="/purchasing/receive-stock" element={<ReceiveStockPage />} />
          <Route
            path="/purchasing/direct-purchases/:id"
            element={<DirectPurchaseDetailRedirect />}
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

async function openFinder(user: ReturnType<typeof userEvent.setup>) {
  const trigger = screen.getByTestId("direct-add-products-trigger");
  expect(trigger).toHaveAttribute("aria-expanded", "false");
  await user.click(trigger);
  await waitFor(() => {
    expect(screen.getByTestId("direct-add-products")).toBeInTheDocument();
  });
  expect(trigger).toHaveAttribute("aria-expanded", "true");
}

async function addLine(
  user: ReturnType<typeof userEvent.setup>,
  opts?: { qty?: string; cost?: string },
) {
  await openFinder(user);
  await waitFor(() => {
    expect(screen.getByTestId(`direct-product-${productId}`)).toBeInTheDocument();
  });
  await user.click(screen.getByTestId(`direct-add-${productId}`));
  await waitFor(() => {
    expect(screen.getByTestId(`direct-receipt-line-${productId}`)).toBeInTheDocument();
  });
  expect(screen.getByTestId("direct-add-products")).toBeInTheDocument();
  const qty = screen.getByTestId(`direct-line-qty-${productId}`);
  const cost = screen.getByTestId(`direct-line-cost-${productId}`);
  // Add focuses cost — fill it first, then set qty without fighting autofocus.
  await waitFor(() => {
    expect(cost).toHaveFocus();
  });
  await user.clear(cost);
  await user.type(cost, opts?.cost ?? "100");
  await user.click(qty);
  await user.clear(qty);
  await user.type(qty, opts?.qty ?? "10");
}

/** jsdom date inputs are unreliable with user.type — set value via change. */
function setDateInput(testId: string, value: string) {
  fireEvent.change(screen.getByTestId(testId), { target: { value } });
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
    getInventoryProduct.mockResolvedValue(inventoryAccount());
    enableExpirationTracking.mockResolvedValue({
      productId,
      organizationId: orgId,
      tracksExpiration: true,
      expirationWarningDays: 7,
      isTracked: true,
      onHandQuantity: 0,
      lots: [],
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

describe("ReceiveStockPage receipt-first product dialog", () => {
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
        {
          categoryId: categoryEmpty,
          organizationId: orgId,
          name: "Empty Shelf",
          status: "Active",
          createdAtUtc: "2026-08-01T00:00:00Z",
          updatedAtUtc: "2026-08-01T00:00:00Z",
        },
      ],
      totalCount: 3,
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

  it("starts with receipt visible and find products collapsed", async () => {
    renderPage();
    expect(screen.getByTestId("direct-receipt-items")).toBeInTheDocument();
    expect(screen.getByTestId("direct-receipt-empty")).toBeInTheDocument();
    expect(screen.getByTestId("direct-add-products-trigger")).toHaveAttribute(
      "aria-expanded",
      "false",
    );
    expect(screen.queryByTestId("direct-add-products")).not.toBeInTheDocument();
  });

  it("opens and closes the product picker without changing receipt", async () => {
    const user = userEvent.setup();
    renderPage();
    await openFinder(user);
    expect(screen.getByTestId("direct-receipt-items")).toBeInTheDocument();
    await user.click(screen.getByTestId("direct-add-products-close"));
    await waitFor(() => {
      expect(screen.queryByTestId("direct-add-products")).not.toBeInTheDocument();
    });
    expect(screen.getByTestId("direct-receipt-empty")).toBeInTheDocument();
    expect(createDirectPurchaseReceipt).not.toHaveBeenCalled();
  });

  it("shows all eligible products when no category is selected", async () => {
    const user = userEvent.setup();
    renderPage();
    await openFinder(user);
    await waitFor(() => {
      expect(screen.getByTestId(`direct-product-${productId}`)).toBeInTheDocument();
      expect(screen.getByTestId(`direct-product-${productId2}`)).toBeInTheDocument();
      expect(screen.getByTestId(`direct-product-${productId3}`)).toBeInTheDocument();
    });
    expect(screen.queryByTestId("direct-category-all")).not.toBeInTheDocument();
    const last = listCatalogProducts.mock.calls.at(-1)?.[1];
    expect(last?.categoryId).toBeUndefined();
  });

  it("supports multi-category OR filtering with select all and deselect all", async () => {
    const user = userEvent.setup();
    renderPage();
    await openFinder(user);
    await waitFor(() => {
      expect(screen.getByTestId("direct-category-multiselect")).toBeInTheDocument();
    });

    await user.click(screen.getByTestId("direct-category-multiselect"));
    expect(screen.getByTestId("direct-category-multiselect-search")).toBeInTheDocument();
    // Counts render for every category, including 0.
    expect(
      within(screen.getByTestId(`direct-category-multiselect-option-${categoryCanned}`)).getByText("1"),
    ).toBeInTheDocument();
    expect(
      within(screen.getByTestId(`direct-category-multiselect-option-${categoryEmpty}`)).getByText("0"),
    ).toBeInTheDocument();
    await user.type(screen.getByTestId("direct-category-multiselect-search"), "fruit");
    expect(screen.getByTestId(`direct-category-multiselect-option-${categoryFruits}`)).toBeInTheDocument();
    expect(screen.queryByTestId(`direct-category-multiselect-option-${categoryCanned}`)).not.toBeInTheDocument();
    await user.clear(screen.getByTestId("direct-category-multiselect-search"));
    await user.click(screen.getByTestId(`direct-category-multiselect-option-${categoryCanned}`));
    await waitFor(() => {
      expect(screen.getByTestId(`direct-product-${productId}`)).toBeInTheDocument();
      expect(screen.queryByTestId(`direct-product-${productId2}`)).not.toBeInTheDocument();
    });
    expect(screen.queryByTestId("direct-category-filters")).not.toBeInTheDocument();

    if (!screen.queryByTestId(`direct-category-multiselect-option-${categoryFruits}`)) {
      await user.click(screen.getByTestId("direct-category-multiselect"));
    }
    await user.click(screen.getByTestId(`direct-category-multiselect-option-${categoryFruits}`));
    await waitFor(() => {
      expect(screen.getByTestId(`direct-product-${productId}`)).toBeInTheDocument();
      expect(screen.getByTestId(`direct-product-${productId2}`)).toBeInTheDocument();
      expect(screen.queryByTestId(`direct-product-${productId3}`)).not.toBeInTheDocument();
    });

    await user.click(screen.getByTestId("direct-category-multiselect-select-all"));
    await waitFor(() => {
      expect(screen.getByTestId(`direct-product-${productId}`)).toBeInTheDocument();
      expect(screen.getByTestId(`direct-product-${productId2}`)).toBeInTheDocument();
      // "Other" is outside listed categories, so Select all does not include it.
      expect(screen.queryByTestId(`direct-product-${productId3}`)).not.toBeInTheDocument();
    });
    expect(screen.getByTestId("direct-category-multiselect")).toHaveTextContent(
      "3 selected",
    );

    await user.click(screen.getByTestId("direct-category-multiselect-clear"));
    await waitFor(() => {
      expect(screen.getByTestId(`direct-product-${productId}`)).toBeInTheDocument();
      expect(screen.getByTestId(`direct-product-${productId2}`)).toBeInTheDocument();
      expect(screen.getByTestId(`direct-product-${productId3}`)).toBeInTheDocument();
    });
    expect(screen.getByTestId("direct-category-multiselect")).toHaveTextContent(
      "Select categories",
    );
  });

  it("preserves category filters across reopen", async () => {
    const user = userEvent.setup();
    renderPage();
    await openFinder(user);
    await waitFor(() => {
      expect(screen.getByTestId("direct-category-multiselect")).toBeInTheDocument();
    });
    await user.click(screen.getByTestId("direct-category-multiselect"));
    await user.click(screen.getByTestId(`direct-category-multiselect-option-${categoryFruits}`));
    await waitFor(() => {
      expect(screen.getByTestId("direct-category-multiselect")).toHaveTextContent(
        "1 selected",
      );
    });
    await user.click(screen.getByTestId("direct-add-products-close"));
    await openFinder(user);
    expect(screen.getByTestId("direct-category-multiselect")).toHaveTextContent(
      "1 selected",
    );
  });

  it("keeps the product picker open after add so more products can be selected", async () => {
    const user = userEvent.setup();
    renderPage();
    expect(screen.getByTestId("direct-receipt-empty")).toBeInTheDocument();
    await openFinder(user);
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
      expect(screen.getByTestId("direct-add-products")).toBeInTheDocument();
      expect(screen.queryByTestId(`direct-product-${productId}`)).not.toBeInTheDocument();
    });
    await waitFor(() => {
      expect(screen.getByTestId(`direct-line-cost-${productId}`)).toHaveFocus();
    });
    expect(screen.getByTestId("direct-add-products-trigger")).toHaveAttribute(
      "aria-expanded",
      "true",
    );
    expect(screen.getByTestId(`direct-line-selling-${productId}`)).toBeInTheDocument();
    expect(screen.getByTestId(`direct-line-selling-${productId}`)).toHaveTextContent("1,200.00");
    expect(screen.getByTestId("direct-review")).toBeDisabled();
    expect(screen.getByTestId(`direct-line-cost-${productId}`)).toHaveAttribute(
      "aria-invalid",
      "true",
    );

    await user.clear(screen.getByTestId(`direct-line-qty-${productId}`));
    await user.type(screen.getByTestId(`direct-line-qty-${productId}`), "2");
    await user.type(screen.getByTestId(`direct-line-cost-${productId}`), "45.60");
    await user.tab();
    expect(screen.getByTestId(`direct-line-cost-${productId}`)).toHaveValue("45.60");
    expect(screen.getByTestId("direct-review")).not.toBeDisabled();
    expect(screen.getByTestId(`direct-line-qty-${productId}`)).not.toHaveAttribute(
      "aria-invalid",
    );
    expect(screen.getByTestId(`direct-line-cost-${productId}`)).not.toHaveAttribute(
      "aria-invalid",
    );
    expect(createDirectPurchaseReceipt).not.toHaveBeenCalled();

    await user.click(screen.getByTestId(`direct-remove-${productId}`));
    expect(screen.getByTestId("direct-receipt-empty")).toBeInTheDocument();
    await waitFor(() => {
      expect(screen.getByTestId(`direct-product-${productId}`)).toBeInTheDocument();
      expect(screen.getByTestId(`direct-add-${productId}`)).toBeInTheDocument();
    });
  });

  it("keeps table layout for find products when viewport is wide enough", async () => {
    Object.defineProperty(window, "matchMedia", {
      writable: true,
      configurable: true,
      value: (query: string) => ({
        matches: query.includes("min-width: 768px") || query.includes("min-width: 1024px"),
        media: query,
        onchange: null,
        addListener: vi.fn(),
        removeListener: vi.fn(),
        addEventListener: vi.fn(),
        removeEventListener: vi.fn(),
        dispatchEvent: vi.fn(),
      }),
    });
    try {
      const user = userEvent.setup();
      renderPage();
      await openFinder(user);
      await waitFor(() => {
        expect(screen.getByTestId("direct-product-results")).toHaveAttribute(
          "data-layout",
          "table",
        );
      });
      expect(
        within(screen.getByTestId("direct-product-results")).getByText("Action"),
      ).toBeInTheDocument();
      expect(
        within(screen.getByTestId("direct-product-results")).getByText("Inventory tracking"),
      ).toBeInTheDocument();
    } finally {
      // Restore jsdom default (no matchMedia) so later tests resolve LIST.
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      delete (window as any).matchMedia;
    }
  });

  it("shows tracking status chips and toasts when adding an untracked product", async () => {
    const untrackedId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb4";
    listCatalogProducts.mockResolvedValue({
      items: [
        canned(),
        productDto({
          productId: untrackedId,
          name: "Service Fee",
          sku: "SVC-1",
          isTracked: false,
          unitOfMeasure: "Each",
        }),
      ],
      totalCount: 2,
      page: 1,
      pageSize: 100,
    });
    const user = userEvent.setup();
    renderPage();
    await openFinder(user);
    await waitFor(() => {
      expect(screen.getByTestId(`direct-add-${untrackedId}`)).toBeInTheDocument();
    });
    expect(
      within(screen.getByTestId("direct-product-results")).getAllByText("Category").length,
    ).toBeGreaterThan(0);
    expect(within(screen.getByTestId(`direct-product-${untrackedId}`)).getByText("Not tracked")).toBeInTheDocument();
    expect(screen.getByTestId("direct-tracking-filter-chip")).toHaveTextContent("Tracked only");
    expect(
      within(screen.getByTestId(`direct-product-${untrackedId}`)).getByText("Not tracked"),
    ).toBeInTheDocument();
    expect(
      within(screen.getByTestId(`direct-product-${productId}`)).getByText("Tracked"),
    ).toBeInTheDocument();
    expect(screen.getByTestId("exits-table-page-size")).toBeInTheDocument();

    await user.click(screen.getByTestId(`direct-add-${untrackedId}`));
    expect(screen.queryByTestId(`direct-receipt-line-${untrackedId}`)).not.toBeInTheDocument();
    expect(await screen.findByTestId("exits-toast")).toHaveTextContent(
      "Inventory tracking required",
    );
    expect(screen.getByTestId("exits-toast")).toHaveTextContent("Service Fee");
    const action = screen.getByTestId("exits-toast-action");
    expect(action).toHaveTextContent("Enable tracking");
    expect(action).toHaveAttribute("href", `/inventory/${untrackedId}`);
  });

  it("filters to tracked products only when Tracked only chip is selected", async () => {
    const untrackedId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb4";
    listCatalogProducts.mockResolvedValue({
      items: [
        canned(),
        fruit(),
        productDto({
          productId: untrackedId,
          name: "Service Fee",
          sku: "SVC-1",
          isTracked: false,
          categoryId: categoryCanned,
          unitOfMeasure: "Each",
        }),
      ],
      totalCount: 3,
      page: 1,
      pageSize: 100,
    });
    const user = userEvent.setup();
    renderPage();
    await openFinder(user);
    await waitFor(() => {
      expect(screen.getByTestId(`direct-product-${productId}`)).toBeInTheDocument();
      expect(screen.getByTestId(`direct-product-${untrackedId}`)).toBeInTheDocument();
    });

    const chip = screen.getByTestId("direct-tracking-filter-chip");
    expect(chip).toHaveAttribute("aria-pressed", "false");
    await user.click(chip);
    await waitFor(() => {
      expect(chip).toHaveAttribute("aria-pressed", "true");
      expect(screen.getByTestId(`direct-product-${productId}`)).toBeInTheDocument();
      expect(screen.queryByTestId(`direct-product-${untrackedId}`)).not.toBeInTheDocument();
    });

    await user.click(chip);
    await waitFor(() => {
      expect(chip).toHaveAttribute("aria-pressed", "false");
      expect(screen.getByTestId(`direct-product-${untrackedId}`)).toBeInTheDocument();
    });
  });
});

describe("ReceiveStockPage cost vs selling price margin warning", () => {
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
    listDirectPurchases.mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 8,
    });
    getInventoryProduct.mockResolvedValue(inventoryAccount());
    enableExpirationTracking.mockResolvedValue({
      productId,
      organizationId: orgId,
      tracksExpiration: true,
      expirationWarningDays: 7,
      isTracked: true,
      onHandQuantity: 0,
      lots: [],
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

  async function addPricedLine(
    user: ReturnType<typeof userEvent.setup>,
    product: PosCatalogProductDto,
    cost: string,
  ) {
    listCatalogProducts.mockResolvedValue({
      items: [product],
      totalCount: 1,
      page: 1,
      pageSize: 100,
    });
    renderPage();
    await openFinder(user);
    await waitFor(() => {
      expect(screen.getByTestId(`direct-product-${product.productId}`)).toBeInTheDocument();
    });
    await user.click(screen.getByTestId(`direct-add-${product.productId}`));
    await waitFor(() => {
      expect(screen.getByTestId(`direct-receipt-line-${product.productId}`)).toBeInTheDocument();
    });
    const costInput = screen.getByTestId(`direct-line-cost-${product.productId}`);
    await user.clear(costInput);
    await user.type(costInput, cost);
    await user.tab();
  }

  it("shows no warning when cost is below branch-effective selling price", async () => {
    const user = userEvent.setup();
    await addPricedLine(
      user,
      productDto({
        sellingPrice: 999,
        effectiveSellingPrice: 200,
        hasBranchPriceOverride: true,
      }),
      "150",
    );
    expect(
      screen.queryByTestId(`direct-line-margin-warning-${productId}`),
    ).not.toBeInTheDocument();
    expect(screen.getByTestId("direct-review")).not.toBeDisabled();
  });

  it("warns on zero margin when cost equals effective selling price", async () => {
    const user = userEvent.setup();
    await addPricedLine(
      user,
      productDto({
        sellingPrice: 999,
        effectiveSellingPrice: 200,
        hasBranchPriceOverride: true,
      }),
      "200",
    );
    const warning = screen.getByTestId(`direct-line-margin-warning-${productId}`);
    expect(warning).toHaveAttribute("data-margin", "zeroMargin");
    expect(screen.getByTestId("direct-review")).not.toBeDisabled();
    await user.click(screen.getByTestId("direct-review"));
    expect(await screen.findByTestId("direct-review-margin-warning")).toBeInTheDocument();
    expect(screen.getByTestId("direct-confirm")).not.toBeDisabled();
  });

  it("warns on negative margin when cost exceeds effective selling price", async () => {
    const user = userEvent.setup();
    await addPricedLine(
      user,
      productDto({
        sellingPrice: 999,
        effectiveSellingPrice: 200,
        hasBranchPriceOverride: true,
      }),
      "300",
    );
    expect(screen.getByTestId(`direct-line-margin-warning-${productId}`)).toHaveAttribute(
      "data-margin",
      "negativeMargin",
    );
  });

  it("shows one consolidated post-save toast for multiple affected products", async () => {
    listCatalogProducts.mockResolvedValue({
      items: [
        productDto({
          productId,
          name: "Rice",
          sellingPrice: 100,
          effectiveSellingPrice: 100,
        }),
        productDto({
          productId: productId2,
          name: "Oil",
          sku: "OIL-1",
          sellingPrice: 50,
          effectiveSellingPrice: 50,
        }),
      ],
      totalCount: 2,
      page: 1,
      pageSize: 100,
    });
    const user = userEvent.setup();
    renderPage();
    await openFinder(user);
    await waitFor(() => {
      expect(screen.getByTestId(`direct-product-${productId}`)).toBeInTheDocument();
      expect(screen.getByTestId(`direct-product-${productId2}`)).toBeInTheDocument();
    });
    await user.click(screen.getByTestId(`direct-add-${productId}`));
    await user.click(screen.getByTestId(`direct-add-${productId2}`));
    await waitFor(() => {
      expect(screen.getByTestId(`direct-receipt-line-${productId}`)).toBeInTheDocument();
      expect(screen.getByTestId(`direct-receipt-line-${productId2}`)).toBeInTheDocument();
    });

    for (const id of [productId, productId2]) {
      const costInput = screen.getByTestId(`direct-line-cost-${id}`);
      await user.clear(costInput);
      await user.type(costInput, "200");
    }

    await user.click(screen.getByTestId("direct-review"));
    expect(await screen.findByTestId("direct-review-margin-warning")).toHaveTextContent(
      "2 received products",
    );
    await user.click(screen.getByTestId("direct-confirm"));
    await waitFor(() => {
      expect(createDirectPurchaseReceipt).toHaveBeenCalledTimes(1);
    });
    const toast = await screen.findByTestId("exits-toast");
    expect(toast).toHaveAttribute("data-tone", "warning");
    expect(toast).toHaveTextContent("Selling price needs review");
    expect(toast).toHaveTextContent("2 received products");
    expect(screen.getAllByTestId("exits-toast")).toHaveLength(1);
    const action = screen.getByTestId("exits-toast-action");
    expect(action).toHaveTextContent("Review prices");
    expect(action).toHaveAttribute("href", "/catalog/todays-prices");
  });

  it("links a single-product toast to edit product", async () => {
    const user = userEvent.setup();
    await addPricedLine(
      user,
      productDto({
        sellingPrice: 200,
        effectiveSellingPrice: 200,
      }),
      "200",
    );
    await user.click(screen.getByTestId("direct-review"));
    await user.click(screen.getByTestId("direct-confirm"));
    await waitFor(() => {
      expect(createDirectPurchaseReceipt).toHaveBeenCalled();
    });
    const action = await screen.findByTestId("exits-toast-action");
    expect(action).toHaveTextContent("Review price");
    expect(action).toHaveAttribute("href", `/catalog/products/${productId}/edit`);
  });
});

describe("ReceiveStockPage optional expiry and lot", () => {
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
      pageSize: 100,
    });
    listDirectPurchases.mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 8,
    });
    getInventoryProduct.mockResolvedValue(inventoryAccount());
    enableExpirationTracking.mockResolvedValue({
      productId,
      organizationId: orgId,
      tracksExpiration: true,
      expirationWarningDays: 7,
      isTracked: true,
      onHandQuantity: 0,
      lots: [],
    });
    createDirectPurchaseReceipt.mockResolvedValue({
      directPurchaseReceiptId: receiptId,
      receiptNumber: "DPR-1",
      organizationId: orgId,
      branchId,
      purchaseDate: "2026-09-01",
      supplierId: null,
      sourceName: null,
      referenceNumber: null,
      notes: null,
      status: "Posted",
      totalCost: 1000,
      createdAtUtc: "2026-09-01T00:00:00Z",
      lines: [],
    });
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  it("always shows optional expiry and lot inputs for non-tracked products", async () => {
    const user = userEvent.setup();
    renderPage();
    await addLine(user, { qty: "2", cost: "50" });
    expect(screen.getByTestId(`direct-line-expiry-${productId}`)).toBeInTheDocument();
    expect(screen.getByTestId(`direct-line-lot-${productId}`)).toBeInTheDocument();
    expect(screen.getByTestId(`direct-line-expiry-${productId}`)).not.toHaveAttribute(
      "aria-required",
    );
  });

  it("shows enable-tracking info notice above the receipt table when expiry is entered", async () => {
    const user = userEvent.setup();
    renderPage();
    await addLine(user, { qty: "2", cost: "50" });
    await user.type(screen.getByTestId(`direct-line-expiry-${productId}`), "2027-01-15");
    const hint = screen.getByTestId("direct-receipt-expiry-hint");
    expect(hint).toHaveAttribute("data-tone", "info");
    expect(hint).toHaveTextContent(
      "Expiry tracking will be enabled when this receipt is saved.",
    );
    expect(screen.getByTestId("direct-receipt-table")).toBeInTheDocument();
    expect(screen.getByTestId(`direct-line-expiry-clear-${productId}`)).toBeInTheDocument();
    await user.click(screen.getByTestId("direct-receipt-expiry-hint-close"));
    expect(screen.queryByTestId("direct-receipt-expiry-hint")).not.toBeInTheDocument();
  });

  it("clears expiry from the notice Clear date action and from the date clear control", async () => {
    const user = userEvent.setup();
    renderPage();
    await addLine(user, { qty: "2", cost: "50" });
    await user.type(screen.getByTestId(`direct-line-expiry-${productId}`), "2027-01-15");
    expect(screen.getByTestId(`direct-line-expiry-${productId}`)).toHaveValue("2027-01-15");
    await user.click(screen.getByTestId("direct-receipt-expiry-hint-clear"));
    expect(screen.getByTestId(`direct-line-expiry-${productId}`)).toHaveValue("");
    expect(screen.queryByTestId("direct-receipt-expiry-hint")).not.toBeInTheDocument();

    await user.type(screen.getByTestId(`direct-line-expiry-${productId}`), "2027-02-01");
    expect(screen.getByTestId("direct-receipt-expiry-hint")).toBeInTheDocument();
    await user.click(screen.getByTestId(`direct-line-expiry-clear-${productId}`));
    expect(screen.getByTestId(`direct-line-expiry-${productId}`)).toHaveValue("");
    expect(screen.queryByTestId("direct-receipt-expiry-hint")).not.toBeInTheDocument();
  });

  it("serializes blank expiry/lot as omitted and lot-only without forcing expiry", async () => {
    const user = userEvent.setup();
    renderPage();
    await addLine(user, { qty: "2", cost: "50" });
    await user.type(screen.getByTestId(`direct-line-lot-${productId}`), "LOT-A");
    await user.click(screen.getByTestId("direct-review"));
    await user.click(screen.getByTestId("direct-confirm"));
    await waitFor(() => {
      expect(createDirectPurchaseReceipt).toHaveBeenCalled();
    });
    const body = createDirectPurchaseReceipt.mock.calls[0]![1] as {
      lines: Array<{ expiryDate?: string | null; lotNumber?: string | null }>;
    };
    expect(body.lines[0]?.lotNumber).toBe("LOT-A");
    expect(body.lines[0]?.expiryDate == null || body.lines[0]?.expiryDate === "").toBe(true);
  });

  it("requires expiry for already-tracked products", async () => {
    const user = userEvent.setup();
    listCatalogProducts.mockResolvedValue({
      items: [productDto({ tracksExpiration: true })],
      totalCount: 1,
      page: 1,
      pageSize: 100,
    });
    renderPage();
    await addLine(user, { qty: "2", cost: "50" });
    expect(screen.getByTestId(`direct-line-expiry-${productId}`)).toHaveAttribute(
      "aria-required",
    );
    expect(screen.getByTestId("direct-receipt-expiry-required-notice")).toHaveTextContent(
      "Expiry date is required because this product uses expiry tracking.",
    );
    expect(screen.getByTestId("direct-review")).toBeDisabled();

    await user.click(screen.getByTestId("direct-receipt-expiry-required-notice-close"));
    expect(screen.queryByTestId("direct-receipt-expiry-required-notice")).not.toBeInTheDocument();
    expect(screen.getByTestId("direct-review")).toBeDisabled();
  });

  it("does not open setup dialog when expiry is selected with zero on-hand", async () => {
    const user = userEvent.setup();
    getInventoryProduct.mockResolvedValue(inventoryAccount({ onHandQuantity: 0 }));
    renderPage();
    await addLine(user, { qty: "2", cost: "50" });
    await user.type(screen.getByTestId(`direct-line-expiry-${productId}`), "2027-06-30");
    await waitFor(() => {
      expect(getInventoryProduct).toHaveBeenCalled();
    });
    expect(screen.queryByTestId("receive-stock-expiry-setup-dialog")).not.toBeInTheDocument();
    expect(screen.getByTestId(`direct-line-expiry-${productId}`)).toHaveValue("2027-06-30");
    expect(screen.getByTestId("direct-receipt-expiry-hint")).toBeInTheDocument();
  });

  it("opens in-place setup dialog for existing on-hand and enables tracking without submitting receipt", async () => {
    const user = userEvent.setup();
    getInventoryProduct.mockResolvedValue(
      inventoryAccount({ onHandQuantity: 100, name: "Apple", unitOfMeasure: "kg" }),
    );
    enableExpirationTracking.mockResolvedValue({
      productId,
      organizationId: orgId,
      tracksExpiration: true,
      expirationWarningDays: 7,
      isTracked: true,
      onHandQuantity: 100,
      lots: [],
    });
    renderPage();
    await addLine(user, { qty: "100", cost: "45" });
    expect(screen.getByTestId(`direct-line-cost-${productId}`)).toHaveValue("45.00");

    await user.type(screen.getByTestId(`direct-line-expiry-${productId}`), "2027-06-30");
    await waitFor(() => {
      expect(screen.getByTestId("receive-stock-expiry-setup-dialog")).toBeInTheDocument();
    });

    expect(screen.getByTestId("enable-expiration-current-stock")).toHaveTextContent("100");
    expect(screen.getByTestId("enable-expiration-qty-0")).toHaveValue("100");
    expect(screen.getByTestId(`direct-line-expiry-${productId}`)).toHaveValue("");
    expect(createDirectPurchaseReceipt).not.toHaveBeenCalled();

    await user.clear(screen.getByTestId("enable-expiration-qty-0"));
    await user.type(screen.getByTestId("enable-expiration-qty-0"), "50");
    await user.type(screen.getByTestId("enable-expiration-expiry-0"), "2026-12-31");
    await user.click(screen.getByTestId("enable-expiration-add-row"));
    expect(screen.getByTestId("enable-expiration-qty-1")).toHaveValue("50");
    await user.clear(screen.getByTestId("enable-expiration-qty-1"));
    await user.type(screen.getByTestId("enable-expiration-qty-1"), "25");
    await user.type(screen.getByTestId("enable-expiration-expiry-1"), "2027-01-31");
    await user.click(screen.getByTestId("enable-expiration-add-row"));
    expect(screen.getByTestId("enable-expiration-qty-2")).toHaveValue("25");
    await user.type(screen.getByTestId("enable-expiration-expiry-2"), "2027-03-31");

    expect(screen.getByTestId("enable-expiration-submit")).not.toBeDisabled();
    await user.click(screen.getByTestId("enable-expiration-submit"));

    await waitFor(() => {
      expect(enableExpirationTracking).toHaveBeenCalled();
    });
    const enableBody = enableExpirationTracking.mock.calls[0]![2] as {
      expectedOnHandQuantity: number;
      existingStockLots: Array<{ quantity: number; expiryDate: string }>;
    };
    expect(enableBody.expectedOnHandQuantity).toBe(100);
    expect(enableBody.existingStockLots).toHaveLength(3);
    expect(enableBody.existingStockLots.map((lot) => lot.quantity)).toEqual([50, 25, 25]);

    await waitFor(() => {
      expect(screen.queryByTestId("receive-stock-expiry-setup-dialog")).not.toBeInTheDocument();
    });
    expect(screen.getByText("Expiration tracking enabled.")).toBeInTheDocument();
    expect(screen.getByTestId(`direct-line-expiry-${productId}`)).toHaveAttribute("aria-required");
    expect(screen.getByTestId(`direct-line-expiry-${productId}`)).toHaveValue("2027-06-30");
    await waitFor(() => {
      expect(screen.getByTestId(`direct-line-expiry-${productId}`)).toHaveFocus();
    });
    expect(screen.getByTestId(`direct-line-cost-${productId}`)).toHaveValue("45.00");
    expect(screen.getByTestId(`direct-line-qty-${productId}`)).toHaveValue("100");
    expect(screen.queryByTestId("direct-receipt-expiry-hint")).not.toBeInTheDocument();
    expect(createDirectPurchaseReceipt).not.toHaveBeenCalled();
  });

  it("after setup, clearing new expiry keeps tracking on and blocks review", async () => {
    const user = userEvent.setup();
    getInventoryProduct.mockResolvedValue(inventoryAccount({ onHandQuantity: 40 }));
    enableExpirationTracking.mockResolvedValue({
      productId,
      organizationId: orgId,
      tracksExpiration: true,
      expirationWarningDays: 7,
      isTracked: true,
      onHandQuantity: 40,
      lots: [],
    });
    renderPage();
    await addLine(user, { qty: "2", cost: "50" });
    await user.type(screen.getByTestId(`direct-line-expiry-${productId}`), "2027-06-30");
    await waitFor(() => {
      expect(screen.getByTestId("receive-stock-expiry-setup-dialog")).toBeInTheDocument();
    });
    setDateInput("enable-expiration-expiry-0", "2026-12-31");
    await waitFor(() => {
      expect(screen.getByTestId("enable-expiration-submit")).not.toBeDisabled();
    });
    await user.click(screen.getByTestId("enable-expiration-submit"));
    await waitFor(() => {
      expect(screen.queryByTestId("receive-stock-expiry-setup-dialog")).not.toBeInTheDocument();
    });

    expect(screen.getByTestId(`direct-line-expiry-${productId}`)).toHaveValue("2027-06-30");
    expect(screen.getByTestId("direct-review")).not.toBeDisabled();

    await user.click(screen.getByTestId(`direct-line-expiry-clear-${productId}`));
    expect(screen.getByTestId(`direct-line-expiry-${productId}`)).toHaveValue("");
    expect(screen.getByTestId(`direct-line-expiry-${productId}`)).toHaveAttribute("aria-required");
    expect(screen.getByTestId("direct-receipt-expiry-required-notice")).toHaveTextContent(
      "Expiry date is required because this product uses expiry tracking.",
    );
    expect(screen.getByTestId("direct-review")).toBeDisabled();
    expect(screen.queryByTestId("direct-receipt-expiry-hint")).not.toBeInTheDocument();
    expect(screen.queryByText(/turn off expiry/i)).not.toBeInTheDocument();

    setDateInput(`direct-line-expiry-${productId}`, "2027-07-15");
    await waitFor(() => {
      expect(screen.getByTestId(`direct-line-expiry-${productId}`)).toHaveValue("2027-07-15");
    });
    expect(screen.queryByTestId("direct-receipt-expiry-required-notice")).not.toBeInTheDocument();
    expect(screen.queryByTestId("receive-stock-expiry-setup-dialog")).not.toBeInTheDocument();
    expect(screen.getByTestId("direct-review")).not.toBeDisabled();
  });

  it("cancels setup dialog without mutating tracking or applying expiry", async () => {
    const user = userEvent.setup();
    getInventoryProduct.mockResolvedValue(inventoryAccount({ onHandQuantity: 100 }));
    renderPage();
    await addLine(user, { qty: "2", cost: "50" });
    await user.type(screen.getByTestId(`direct-line-expiry-${productId}`), "2027-06-30");
    await waitFor(() => {
      expect(screen.getByTestId("receive-stock-expiry-setup-dialog")).toBeInTheDocument();
    });
    await user.click(screen.getByTestId("receive-stock-expiry-setup-cancel"));
    await waitFor(() => {
      expect(screen.queryByTestId("receive-stock-expiry-setup-dialog")).not.toBeInTheDocument();
    });
    expect(enableExpirationTracking).not.toHaveBeenCalled();
    expect(screen.getByTestId(`direct-line-expiry-${productId}`)).toHaveValue("");
    expect(screen.getByTestId(`direct-line-expiry-${productId}`)).not.toHaveAttribute(
      "aria-required",
    );
    expect(screen.getByTestId(`direct-receipt-line-${productId}`)).toBeInTheDocument();
  });

  it("keeps setup dialog open on Escape and preserves in-progress allocation rows", async () => {
    const user = userEvent.setup();
    getInventoryProduct.mockResolvedValue(inventoryAccount({ onHandQuantity: 100 }));
    renderPage();
    await addLine(user, { qty: "2", cost: "50" });
    await user.type(screen.getByTestId(`direct-line-expiry-${productId}`), "2027-06-30");
    await waitFor(() => {
      expect(screen.getByTestId("receive-stock-expiry-setup-dialog")).toBeInTheDocument();
    });

    await user.clear(screen.getByTestId("enable-expiration-qty-0"));
    await user.type(screen.getByTestId("enable-expiration-qty-0"), "50");
    await user.type(screen.getByTestId("enable-expiration-expiry-0"), "2026-12-31");
    await user.click(screen.getByTestId("enable-expiration-add-row"));
    expect(screen.getByTestId("enable-expiration-qty-1")).toHaveValue("50");

    await user.keyboard("{Escape}");
    expect(screen.getByTestId("receive-stock-expiry-setup-dialog")).toBeInTheDocument();
    expect(screen.getByTestId("enable-expiration-qty-0")).toHaveValue("50");
    expect(screen.getByTestId("enable-expiration-expiry-0")).toHaveValue("2026-12-31");
    expect(screen.getByTestId("enable-expiration-qty-1")).toHaveValue("50");
    expect(enableExpirationTracking).not.toHaveBeenCalled();
  });

  it("uses organization on-hand for setup allocation when branch display differs", async () => {
    const user = userEvent.setup();
    getInventoryProduct.mockResolvedValue(
      inventoryAccount({
        onHandQuantity: 40,
        organizationOnHandQuantity: 100,
        name: "Apple",
        unitOfMeasure: "kg",
      }),
    );
    enableExpirationTracking.mockResolvedValue({
      productId,
      organizationId: orgId,
      tracksExpiration: true,
      expirationWarningDays: 7,
      isTracked: true,
      onHandQuantity: 100,
      lots: [],
    });
    renderPage();
    await addLine(user, { qty: "2", cost: "50" });
    await user.type(screen.getByTestId(`direct-line-expiry-${productId}`), "2027-06-30");
    await waitFor(() => {
      expect(screen.getByTestId("receive-stock-expiry-setup-dialog")).toBeInTheDocument();
    });
    expect(screen.getByTestId("enable-expiration-current-stock")).toHaveTextContent("100");
    expect(screen.getByTestId("enable-expiration-qty-0")).toHaveValue("100");

    setDateInput("enable-expiration-expiry-0", "2026-12-31");
    await waitFor(() => {
      expect(screen.getByTestId("enable-expiration-submit")).not.toBeDisabled();
    });
    await user.click(screen.getByTestId("enable-expiration-submit"));
    await waitFor(() => {
      expect(enableExpirationTracking).toHaveBeenCalled();
    });
    const enableBody = enableExpirationTracking.mock.calls[0]![2] as {
      expectedOnHandQuantity: number;
    };
    expect(enableBody.expectedOnHandQuantity).toBe(100);
  });
});
