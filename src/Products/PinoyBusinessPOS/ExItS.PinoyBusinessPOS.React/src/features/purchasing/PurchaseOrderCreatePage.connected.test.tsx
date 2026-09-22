import { beforeEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { catalogs } from "@/i18n/messages";
import { PurchaseOrderCreatePage } from "@/features/purchasing/PurchaseOrderCreatePage";

const orgId = "11111111-1111-1111-1111-111111111111";
const branchId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
const supplierId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
const relationshipId = "cccccccc-cccc-cccc-cccc-cccccccccccc";
const buyerProductId = "dddddddd-dddd-4ddd-8ddd-dddddddddddd";
const supplierProductId = "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee";
const linkId = "ffffffff-ffff-4fff-8fff-ffffffffffff";
const exposureId = "99999999-9999-4999-8999-999999999999";
const buyerProductId2 = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";
const supplierProductId2 = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb";
const linkId2 = "cccccccc-cccc-4ccc-8ccc-cccccccccccc";
const exposureId2 = "12121212-1212-4212-8212-121212121212";

const workspaceMock = {
  boundWorkspace: {
    organizationId: orgId,
    organizationDisplayName: "Paul store",
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
}));

vi.mock("@/i18n/I18nProvider", () => ({
  useI18n: () => ({
    t: (key: keyof typeof catalogs.en) => catalogs.en[key] ?? String(key),
  }),
}));

const listSuppliers = vi.fn();
const listLinks = vi.fn();
const searchExposedCatalog = vi.fn();
const classifyCatalogReadiness = vi.fn();
const getConnectedOrderStock = vi.fn();
const createPurchaseOrder = vi.fn();
const listCatalogProducts = vi.fn();
const createBuyerProductAndLink = vi.fn();
const linkProduct = vi.fn();
const getBusinessCustomerCreditPolicy = vi.fn();
const getBuyerConnectedSupplierCommerceReadiness = vi.fn();

vi.mock("@/api/pos/pos-business-credit-policy-client", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/api/pos/pos-business-credit-policy-client")>();
  return {
    ...actual,
    getBusinessCustomerCreditPolicy: (...args: unknown[]) => getBusinessCustomerCreditPolicy(...args),
  };
});

vi.mock("@/api/pos/pos-suppliers-client", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/api/pos/pos-suppliers-client")>();
  return {
    ...actual,
    listSuppliers: (...args: unknown[]) => listSuppliers(...args),
  };
});

vi.mock("@/api/pos/pos-connected-suppliers-client", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/api/pos/pos-connected-suppliers-client")>();
  return {
    ...actual,
    listLinks: (...args: unknown[]) => listLinks(...args),
    searchExposedCatalog: (...args: unknown[]) => searchExposedCatalog(...args),
    classifyCatalogReadiness: (...args: unknown[]) => classifyCatalogReadiness(...args),
    getConnectedOrderStock: (...args: unknown[]) => getConnectedOrderStock(...args),
    createBuyerProductAndLink: (...args: unknown[]) => createBuyerProductAndLink(...args),
    linkProduct: (...args: unknown[]) => linkProduct(...args),
    getBuyerConnectedSupplierCommerceReadiness: (...args: unknown[]) =>
      getBuyerConnectedSupplierCommerceReadiness(...args),
  };
});

vi.mock("@/api/pos/pos-purchase-orders-client", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/api/pos/pos-purchase-orders-client")>();
  return {
    ...actual,
    createPurchaseOrder: (...args: unknown[]) => createPurchaseOrder(...args),
  };
});

vi.mock("@/api/pos/pos-catalog-client", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/api/pos/pos-catalog-client")>();
  return {
    ...actual,
    listCatalogProducts: (...args: unknown[]) => listCatalogProducts(...args),
  };
});

function linkedSupplier() {
  return {
    items: [
      {
        supplierId,
        organizationId: orgId,
        supplierCode: "SUP-1",
        name: "Mica Store",
        status: "Active",
        connectionType: "ConnectedOrganization",
        contactPerson: null,
        mobileNumber: null,
        telephoneNumber: null,
        email: null,
        addressLine1: null,
        addressLine2: null,
        cityMunicipality: null,
        province: null,
        postalCode: null,
        taxOrRegistrationNumber: null,
        notes: null,
        connectedRelationshipId: relationshipId,
        createdAtUtc: "2026-09-01T00:00:00Z",
        updatedAtUtc: "2026-09-01T00:00:00Z",
        connectedBusinessPublicId: "ORGMICA01",
        supplierBranchName: "Iloilo",
      },
    ],
    totalCount: 1,
    page: 1,
    pageSize: 100,
  };
}

function readyLinkPayload() {
  return [
    {
      linkId,
      relationshipId,
      buyerOrganizationId: orgId,
      supplierOrganizationId: "22222222-2222-4222-8222-222222222222",
      buyerProductId,
      supplierProductId,
      supplierSkuSnapshot: "PH-BEV-WATER-500",
      supplierNameSnapshot: "Bottled Water 500ml",
      unitOfMeasureCode: "Piece",
      lastKnownOrderPrice: 12,
      isActive: true,
      syncVersion: 1,
      createdAtUtc: "2026-09-01T00:00:00Z",
      updatedAtUtc: "2026-09-01T00:00:00Z",
      buyerPurchaseUnitId: null,
      multiplierToBase: 1,
      packageLabel: null,
    },
    {
      linkId: linkId2,
      relationshipId,
      buyerOrganizationId: orgId,
      supplierOrganizationId: "22222222-2222-4222-8222-222222222222",
      buyerProductId: buyerProductId2,
      supplierProductId: supplierProductId2,
      supplierSkuSnapshot: "PH-RICE-1KG",
      supplierNameSnapshot: "Rice 1kg",
      unitOfMeasureCode: "Piece",
      lastKnownOrderPrice: 50,
      isActive: true,
      syncVersion: 1,
      createdAtUtc: "2026-09-01T00:00:00Z",
      updatedAtUtc: "2026-09-01T00:00:00Z",
      buyerPurchaseUnitId: null,
      multiplierToBase: 1,
      packageLabel: null,
    },
  ];
}

function readyCatalogPayload() {
  return {
    items: [
      {
        exposureId,
        supplierOrganizationId: "22222222-2222-4222-8222-222222222222",
        productId: supplierProductId,
        skuSnapshot: "PH-BEV-WATER-500",
        nameSnapshot: "Bottled Water 500ml",
        categoryNameSnapshot: "Beverages",
        unitOfMeasureCode: "Piece",
        supplierOrderPrice: 12,
        effectiveSupplierOrderPrice: 12,
        isOrderable: true,
        isExposed: true,
        syncVersion: 1,
        createdAtUtc: "2026-09-01T00:00:00Z",
        updatedAtUtc: "2026-09-01T00:00:00Z",
      },
      {
        exposureId: exposureId2,
        supplierOrganizationId: "22222222-2222-4222-8222-222222222222",
        productId: supplierProductId2,
        skuSnapshot: "PH-RICE-1KG",
        nameSnapshot: "Rice 1kg",
        categoryNameSnapshot: null,
        unitOfMeasureCode: "Piece",
        supplierOrderPrice: 50,
        effectiveSupplierOrderPrice: 50,
        isOrderable: true,
        isExposed: true,
        syncVersion: 1,
        createdAtUtc: "2026-09-01T00:00:00Z",
        updatedAtUtc: "2026-09-01T00:00:00Z",
      },
    ],
    totalCount: 2,
    page: 1,
    pageSize: 50,
  };
}

function readinessPayload() {
  return {
    relationshipId,
    ready: 1,
    new: 1,
    review: 0,
    conflict: 0,
    items: [
      {
        exposureId,
        supplierProductId,
        supplierName: "Bottled Water 500ml",
        supplierSku: "PH-BEV-WATER-500",
        supplierBarcode: null,
        unitOfMeasureCode: "Piece",
        poPrice: 12,
        status: "Ready",
        canAutoLink: false,
        candidateBuyerProductId: buyerProductId,
        candidateBuyerProductName: "Bottled Water 500ml",
        nameMatched: true,
        skuMatched: true,
        barcodeMatched: false,
        unitCompatible: true,
        matchDetails: null,
        conflictCandidates: [],
      },
      {
        exposureId: exposureId2,
        supplierProductId: supplierProductId2,
        supplierName: "Snack Mix",
        supplierSku: "PH-SNACK-1",
        supplierBarcode: null,
        unitOfMeasureCode: "Piece",
        poPrice: 20,
        status: "New",
        canAutoLink: false,
        candidateBuyerProductId: null,
        candidateBuyerProductName: null,
        nameMatched: false,
        skuMatched: false,
        barcodeMatched: false,
        unitCompatible: true,
        matchDetails: null,
        conflictCandidates: [],
      },
    ],
  };
}

function renderPage(initialEntry = "/purchasing/new") {
  const client = new QueryClient({
    defaultOptions: {
      queries: { retry: false, networkMode: "always" },
      mutations: { networkMode: "always" },
    },
  });
  render(
    <QueryClientProvider client={client}>
      <MemoryRouter initialEntries={[initialEntry]}>
        <Routes>
          <Route path="/purchasing/new" element={<PurchaseOrderCreatePage />} />
          <Route path="/suppliers/:supplierId/connected-catalog" element={<div>catalog</div>} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

async function selectSupplierAndOpenFinder(
  user: ReturnType<typeof userEvent.setup>,
  options?: { waitForProduct?: boolean },
) {
  await waitFor(() => expect(screen.getByRole("option", { name: /Mica Store/i })).toBeInTheDocument());
  await user.selectOptions(screen.getByTestId("po-supplier"), supplierId);
  await user.click(screen.getByTestId("po-add-products-trigger"));
  if (options?.waitForProduct !== false) {
    await waitFor(() =>
      expect(screen.getByTestId(`po-connected-product-${buyerProductId}`)).toBeInTheDocument(),
    );
  } else {
    await waitFor(() => expect(screen.getByTestId("po-add-products")).toBeInTheDocument());
  }
}

describe("PurchaseOrderCreatePage connected product picker", () => {
  beforeEach(() => {
    listSuppliers.mockReset();
    listLinks.mockReset();
    searchExposedCatalog.mockReset();
    classifyCatalogReadiness.mockReset();
    getConnectedOrderStock.mockReset();
    createPurchaseOrder.mockReset();
    listCatalogProducts.mockReset();
    createBuyerProductAndLink.mockReset();
    linkProduct.mockReset();
    getBusinessCustomerCreditPolicy.mockReset();
    getBuyerConnectedSupplierCommerceReadiness.mockReset();
    listSuppliers.mockResolvedValue(linkedSupplier());
    listLinks.mockResolvedValue(readyLinkPayload());
    searchExposedCatalog.mockResolvedValue(readyCatalogPayload());
    classifyCatalogReadiness.mockResolvedValue(readinessPayload());
    getBuyerConnectedSupplierCommerceReadiness.mockResolvedValue({
      relationshipId,
      isReady: true,
      supportedFulfillmentMethods: ["Pickup", "Delivery"],
      requirements: null,
    });
    getBusinessCustomerCreditPolicy.mockResolvedValue({
      status: "Approved",
      creditLimit: 100000,
      availableCredit: 50000,
      outstandingAmount: 0,
      defaultTermDays: 30,
    });
    getConnectedOrderStock.mockResolvedValue({
      relationshipId,
      supplierBranchId: "77777777-7777-4777-8777-777777777777",
      supplierBranchName: "Main Branch",
      items: [
        {
          supplierProductId,
          isTracked: true,
          availableBaseQuantity: 10,
        },
      ],
    });
    createPurchaseOrder.mockResolvedValue({ purchaseOrderId: "po-1" });
    listCatalogProducts.mockResolvedValue({ items: [], totalCount: 0 });
    createBuyerProductAndLink.mockResolvedValue({
      buyerProductId: buyerProductId2,
      createdNewProduct: true,
      alreadyLinked: false,
    });
    linkProduct.mockResolvedValue({ linkId: "l-new" });
  });

  it("loads linked shared orderable products without requiring search", async () => {
    const user = userEvent.setup();
    renderPage();
    await selectSupplierAndOpenFinder(user);
    expect(listLinks).toHaveBeenCalled();
    expect(searchExposedCatalog).toHaveBeenCalled();
    expect(classifyCatalogReadiness).toHaveBeenCalled();
    expect(screen.getByText("Bottled Water 500ml")).toBeInTheDocument();
    expect(screen.getByTestId("po-readiness-filters")).toBeInTheDocument();
    expect(screen.getByTestId("po-ready-linked")).toHaveAttribute("aria-selected", "true");
    expect(screen.queryByTestId("po-ready-all")).not.toBeInTheDocument();
    expect(screen.getByTestId("po-ready-newProduct")).toHaveTextContent("New products (1)");
  });

  it("filters linked products with searchable category multi-select", async () => {
    const user = userEvent.setup();
    renderPage();
    await selectSupplierAndOpenFinder(user);

    expect(screen.getByTestId(`po-category-${buyerProductId}`)).toHaveTextContent("Beverages");
    expect(screen.getByTestId(`po-category-${buyerProductId2}`)).toHaveTextContent("—");

    expect(screen.queryByTestId("po-category-filters")).not.toBeInTheDocument();
    expect(screen.getByTestId("po-category-multiselect")).toBeInTheDocument();
    expect(screen.getByTestId("po-category-multiselect")).toHaveTextContent(/Categories/i);

    await user.click(screen.getByTestId("po-category-multiselect"));
    expect(screen.getByTestId("po-category-multiselect-search")).toBeInTheDocument();
    expect(screen.getByTestId("po-category-multiselect-option-Beverages")).toBeInTheDocument();
    expect(
      screen.getByTestId("po-category-multiselect-option-__po_no_category__"),
    ).toBeInTheDocument();
    expect(screen.queryByTestId("po-category-multiselect-select-all")).not.toBeInTheDocument();

    await user.click(screen.getByTestId("po-category-multiselect-option-Beverages"));
    expect(screen.getByTestId(`po-connected-product-${buyerProductId}`)).toBeInTheDocument();
    expect(screen.queryByTestId(`po-connected-product-${buyerProductId2}`)).not.toBeInTheDocument();
    expect(screen.getByTestId("po-category-multiselect")).toHaveTextContent(/Categories · 1/);

    await user.click(screen.getByTestId("po-category-multiselect-option-__po_no_category__"));
    expect(screen.getByTestId(`po-connected-product-${buyerProductId}`)).toBeInTheDocument();
    expect(screen.getByTestId(`po-connected-product-${buyerProductId2}`)).toBeInTheDocument();
    expect(screen.getByTestId("po-category-multiselect")).toHaveTextContent(/Categories · 2/);

    await user.click(screen.getByTestId("po-category-multiselect-clear"));
    expect(screen.getByTestId(`po-connected-product-${buyerProductId}`)).toBeInTheDocument();
    expect(screen.getByTestId(`po-connected-product-${buyerProductId2}`)).toBeInTheDocument();
  });

  it("preselects supplier from supplierId query when opening new purchase order", async () => {
    const user = userEvent.setup();
    renderPage(`/purchasing/new?supplierId=${supplierId}`);
    await waitFor(() => expect(screen.getByTestId("po-supplier")).toHaveValue(supplierId));
    await user.click(screen.getByTestId("po-add-products-trigger"));
    await waitFor(() =>
      expect(screen.getByTestId(`po-connected-product-${buyerProductId}`)).toBeInTheDocument(),
    );
  });

  it("lets setup tabs connect a specific product or open shared catalog", async () => {
    const user = userEvent.setup();
    renderPage();
    await selectSupplierAndOpenFinder(user, { waitForProduct: false });
    await waitFor(() => screen.getByTestId("po-ready-newProduct"));
    await user.click(screen.getByTestId("po-ready-newProduct"));
    await waitFor(() => screen.getByTestId(`po-setup-product-${exposureId2}`));
    expect(screen.queryByTestId(`po-connected-product-${buyerProductId}`)).not.toBeInTheDocument();
    expect(screen.getByTestId(`po-create-link-${exposureId2}`)).toBeInTheDocument();
    expect(screen.queryByTestId(`po-open-catalog-${exposureId2}`)).not.toBeInTheDocument();
    expect(screen.getByTestId("po-open-shared-catalog-setup-bar")).toHaveAttribute(
      "href",
      `/suppliers/${supplierId}/connected-catalog?setup=newProduct`,
    );

    await user.click(screen.getByTestId(`po-create-link-${exposureId2}`));
    await waitFor(() =>
      expect(createBuyerProductAndLink).toHaveBeenCalledWith(
        expect.anything(),
        relationshipId,
        expect.objectContaining({
          exposureId: exposureId2,
          name: "Snack Mix",
          sellingPrice: 20,
        }),
      ),
    );
  });

  it("bulk-adds selected setup products while keeping per-row connect", async () => {
    const user = userEvent.setup();
    renderPage();
    await selectSupplierAndOpenFinder(user, { waitForProduct: false });
    await waitFor(() => screen.getByTestId("po-ready-newProduct"));
    await user.click(screen.getByTestId("po-ready-newProduct"));
    await waitFor(() => screen.getByTestId(`po-setup-select-${exposureId2}`));

    expect(screen.getByTestId(`po-create-link-${exposureId2}`)).toBeInTheDocument();
    await user.click(screen.getByTestId(`po-setup-select-${exposureId2}`));
    await waitFor(() => screen.getByTestId("po-setup-bulk-bar"));
    await user.click(screen.getByTestId("po-bulk-add-as-new"));
    await waitFor(() =>
      expect(createBuyerProductAndLink).toHaveBeenCalledWith(
        expect.anything(),
        relationshipId,
        expect.objectContaining({ exposureId: exposureId2, sellingPrice: 20 }),
      ),
    );
  });

  it("filters products, supports add/qty edit totals, and updates subtotal", async () => {
    const user = userEvent.setup();
    renderPage();
    await selectSupplierAndOpenFinder(user);

    const search = screen.getByTestId("po-product-search");
    await user.type(search, "soap");
    await waitFor(() =>
      expect(screen.queryByTestId(`po-connected-product-${buyerProductId}`)).not.toBeInTheDocument(),
    );
    await user.clear(search);
    await waitFor(() => screen.getByTestId(`po-connected-product-${buyerProductId}`));

    await user.click(screen.getByTestId(`po-add-${buyerProductId}`));
    // Added product leaves Find products and appears on Purchase order items.
    expect(screen.queryByTestId(`po-connected-product-${buyerProductId}`)).not.toBeInTheDocument();
    expect(screen.getByTestId(`po-connected-selected-${buyerProductId}`)).toBeInTheDocument();
    expect(screen.getByTestId(`po-qty-${buyerProductId}`)).toHaveTextContent("1");
    expect(screen.getByTestId("po-subtotal")).toHaveTextContent("₱12.00");

    await user.click(screen.getByTestId(`po-qty-${buyerProductId}`));
    expect(screen.getByTestId(`po-qty-${buyerProductId}`)).toHaveValue("1");

    await user.clear(screen.getByTestId(`po-qty-${buyerProductId}`));
    await user.type(screen.getByTestId(`po-qty-${buyerProductId}`), "2");
    await user.tab();
    expect(screen.getByTestId(`po-qty-${buyerProductId}`)).toHaveTextContent("2");
    expect(screen.getByTestId("po-subtotal")).toHaveTextContent("₱24.00");

    await user.click(screen.getByTestId(`po-qty-${buyerProductId}`));
    await user.clear(screen.getByTestId(`po-qty-${buyerProductId}`));
    await user.type(screen.getByTestId(`po-qty-${buyerProductId}`), "5");
    await user.tab();
    expect(screen.getByTestId(`po-qty-${buyerProductId}`)).toHaveTextContent("5");
    expect(screen.getByTestId("po-subtotal")).toHaveTextContent("₱60.00");

    await user.click(screen.getByTestId(`po-qty-${buyerProductId}`));
    await user.clear(screen.getByTestId(`po-qty-${buyerProductId}`));
    await user.type(screen.getByTestId(`po-qty-${buyerProductId}`), "4");
    await user.tab();
    expect(screen.getByTestId(`po-qty-${buyerProductId}`)).toHaveTextContent("4");
    await user.click(screen.getByTestId(`po-connected-selected-remove-${buyerProductId}`));
    // Removed from order items → returns to Find products.
    expect(screen.getByTestId(`po-add-${buyerProductId}`)).toBeInTheDocument();
    expect(screen.getByTestId("po-connected-selected-items-empty")).toBeInTheDocument();
    expect(screen.queryByTestId("po-subtotal")).not.toBeInTheDocument();
  });

  it("allows measured Kg decimals on purchase order items via qty text input", async () => {
    const user = userEvent.setup();
    const kgBuyerProductId = buyerProductId2;
    listLinks.mockResolvedValue([
      {
        ...readyLinkPayload()[0],
      },
      {
        ...readyLinkPayload()[1],
        buyerProductId: kgBuyerProductId,
        supplierProductId: supplierProductId2,
        supplierSkuSnapshot: "PH-RICE-KG",
        supplierNameSnapshot: "Loose Rice",
        unitOfMeasureCode: "Kilogram",
        lastKnownOrderPrice: 40,
        packageLabel: "Kilogram",
      },
    ]);
    searchExposedCatalog.mockResolvedValue({
      ...readyCatalogPayload(),
      items: [
        readyCatalogPayload().items[0],
        {
          ...readyCatalogPayload().items[1],
          productId: supplierProductId2,
          skuSnapshot: "PH-RICE-KG",
          nameSnapshot: "Loose Rice",
          unitOfMeasureCode: "Kilogram",
          supplierOrderPrice: 40,
          effectiveSupplierOrderPrice: 40,
        },
      ],
    });
    renderPage();
    await selectSupplierAndOpenFinder(user, { waitForProduct: false });
    await waitFor(() => screen.getByTestId(`po-connected-product-${kgBuyerProductId}`));

    await user.click(screen.getByTestId(`po-add-${kgBuyerProductId}`));
    await user.click(screen.getByTestId(`po-qty-${kgBuyerProductId}`));
    const qty = screen.getByTestId(`po-qty-${kgBuyerProductId}`);
    expect(qty).toHaveValue("1");

    await user.clear(qty);
    await user.type(qty, "0.25");
    await user.tab();
    expect(screen.getByTestId(`po-qty-${kgBuyerProductId}`)).toHaveTextContent("0.25");
    expect(screen.getByTestId("po-subtotal")).toHaveTextContent("₱10.00");

    await user.click(screen.getByTestId(`po-qty-${kgBuyerProductId}`));
    await user.clear(screen.getByTestId(`po-qty-${kgBuyerProductId}`));
    await user.type(screen.getByTestId(`po-qty-${kgBuyerProductId}`), "1.25");
    await user.tab();
    expect(screen.getByTestId(`po-qty-${kgBuyerProductId}`)).toHaveTextContent("1.25");
    expect(screen.getByTestId("po-subtotal")).toHaveTextContent("₱50.00");
  });

  it("shows stock qty label and allows qty above stock with warning", async () => {
    const user = userEvent.setup();
    getConnectedOrderStock.mockResolvedValue({
      relationshipId,
      supplierBranchId: "77777777-7777-4777-8777-777777777777",
      supplierBranchName: "Main Branch",
      items: [
        { supplierProductId, isTracked: true, availableBaseQuantity: 5 },
        {
          supplierProductId: "00000000-0000-4000-8000-000000000099",
          isTracked: true,
          availableBaseQuantity: 0,
        },
      ],
    });
    renderPage();
    await selectSupplierAndOpenFinder(user);
    await waitFor(() =>
      expect(screen.getByTestId(`po-stock-${buyerProductId}`)).toHaveTextContent("5"),
    );
    expect(getConnectedOrderStock).toHaveBeenCalled();

    await user.click(screen.getByTestId(`po-add-${buyerProductId}`));
    await waitFor(() =>
      expect(screen.getByTestId(`po-connected-selected-availability-${buyerProductId}`)).toHaveTextContent(
        "5",
      ),
    );
    expect(
      screen.queryByTestId(`po-connected-selected-over-order-${buyerProductId}`),
    ).not.toBeInTheDocument();

    await user.click(screen.getByTestId(`po-qty-${buyerProductId}`));

    // Type past available (5).
    const qty = screen.getByTestId(`po-qty-${buyerProductId}`);
    await user.clear(qty);
    await user.type(qty, "8");
    await user.tab();
    await waitFor(() =>
      expect(screen.getByTestId(`po-qty-${buyerProductId}`)).toHaveTextContent("8"),
    );
    await waitFor(() =>
      expect(screen.getByTestId(`po-connected-selected-over-order-${buyerProductId}`)).toHaveTextContent(
        /exceeds current available stock/i,
      ),
    );
    await user.click(screen.getByTestId("po-payment-option-Cash"));
    await user.click(screen.getByTestId("po-fulfillment-option-Pickup"));
    expect(screen.getByTestId("po-create-submit")).not.toBeDisabled();
  });

  it("Not added / Added buttons switch product list without a toggle", async () => {
    const user = userEvent.setup();
    renderPage();
    await selectSupplierAndOpenFinder(user);

    const notAddedFilter = screen.getByTestId("po-filter-not-added");
    const addedFilter = screen.getByTestId("po-filter-added");
    expect(notAddedFilter).toHaveAttribute("aria-pressed", "true");
    expect(addedFilter).toHaveAttribute("aria-pressed", "false");
    expect(addedFilter).toHaveTextContent("Added (0)");

    await user.click(addedFilter);
    expect(addedFilter).toHaveAttribute("aria-pressed", "true");
    expect(notAddedFilter).toHaveAttribute("aria-pressed", "false");
    await waitFor(() =>
      expect(screen.getByTestId("po-connected-filter-empty")).toBeInTheDocument(),
    );

    await user.click(notAddedFilter);
    expect(notAddedFilter).toHaveAttribute("aria-pressed", "true");
    expect(addedFilter).toHaveAttribute("aria-pressed", "false");
    await waitFor(() => screen.getByTestId(`po-connected-product-${buyerProductId}`));

    await user.click(screen.getByTestId(`po-add-${buyerProductId}`));
    expect(screen.queryByTestId(`po-connected-product-${buyerProductId}`)).not.toBeInTheDocument();
    expect(addedFilter).toHaveTextContent("Added (1)");

    await user.click(addedFilter);
    await waitFor(() => {
      expect(screen.getByTestId(`po-connected-product-${buyerProductId}`)).toBeInTheDocument();
      expect(screen.getByTestId(`po-remove-${buyerProductId}`)).toBeInTheDocument();
    });
    expect(screen.queryByTestId(`po-add-${buyerProductId}`)).not.toBeInTheDocument();

    await user.click(screen.getByTestId(`po-remove-${buyerProductId}`));
    await waitFor(() => {
      expect(screen.queryByTestId(`po-connected-product-${buyerProductId}`)).not.toBeInTheDocument();
      expect(screen.getByTestId("po-connected-selected-items-empty")).toBeInTheDocument();
    });
    expect(addedFilter).toHaveTextContent("Added (0)");

    await user.click(notAddedFilter);
    await waitFor(() => screen.getByTestId(`po-add-${buyerProductId}`));
  });

  it("allows Add when supplier stock is zero and shows over-order warning", async () => {
    const user = userEvent.setup();
    getConnectedOrderStock.mockResolvedValue({
      relationshipId,
      supplierBranchId: "77777777-7777-4777-8777-777777777777",
      supplierBranchName: "Main Branch",
      items: [{ supplierProductId, isTracked: true, availableBaseQuantity: 0 }],
    });
    renderPage();
    await selectSupplierAndOpenFinder(user);
    await waitFor(() =>
      expect(screen.getByTestId(`po-stock-${buyerProductId}`)).toHaveTextContent("Out of stock"),
    );
    expect(screen.getByTestId(`po-add-${buyerProductId}`)).not.toBeDisabled();
    await user.click(screen.getByTestId(`po-add-${buyerProductId}`));
    await waitFor(() =>
      expect(screen.getByTestId(`po-connected-selected-over-order-${buyerProductId}`)).toHaveTextContent(
        /exceeds current available stock/i,
      ),
    );
  });

  it("shows empty shared-catalog CTA when no ready products", async () => {
    const user = userEvent.setup();
    listLinks.mockResolvedValue([]);
    searchExposedCatalog.mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 50 });
    classifyCatalogReadiness.mockResolvedValue({
      ...readinessPayload(),
      ready: 0,
      items: readinessPayload().items.filter((item) => item.status !== "Ready"),
    });
    renderPage();
    await selectSupplierAndOpenFinder(user, { waitForProduct: false });
    await waitFor(() => expect(screen.getByTestId("po-open-shared-catalog")).toBeInTheDocument());
    expect(screen.getByTestId("po-open-shared-catalog")).toHaveAttribute(
      "href",
      `/suppliers/${supplierId}/connected-catalog`,
    );
  });

  it("blocks draft create until fulfillment is chosen even when supplier commerce is not ready", async () => {
    const user = userEvent.setup();
    getBuyerConnectedSupplierCommerceReadiness.mockResolvedValue({
      relationshipId,
      isReady: false,
      supportedFulfillmentMethods: ["Pickup"],
      requirements: null,
      blockerCategories: ["Catalog"],
    });
    renderPage(`/purchasing/new?supplierId=${supplierId}`);

    const banner = await screen.findByTestId("po-supplier-not-ready-banner");
    expect(banner).toHaveTextContent(/Supplier not ready for purchase orders/i);
    expect(banner).toHaveTextContent(/You can save a draft now/i);
    expect(banner).toHaveTextContent(/Issue:\s*Catalog/i);
    expect(screen.queryByText(/Responsible contact/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/Shared catalog/i)).not.toBeInTheDocument();

    await selectSupplierAndOpenFinder(user);
    await user.click(screen.getByTestId(`po-add-${buyerProductId}`));
    await waitFor(() =>
      expect(screen.getByTestId(`po-qty-${buyerProductId}`)).toBeInTheDocument(),
    );
    await user.click(screen.getByTestId("po-payment-option-Cash"));

    // Single supported method is auto-selected → create remains available for draft.
    await waitFor(() => {
      expect(screen.getByTestId("po-create-submit")).not.toBeDisabled();
    });
  });

  it("keeps create disabled when connected and no fulfillment method is available", async () => {
    const user = userEvent.setup();
    getBuyerConnectedSupplierCommerceReadiness.mockResolvedValue({
      relationshipId,
      isReady: false,
      supportedFulfillmentMethods: [],
      requirements: null,
      blockerCategories: ["Fulfillment"],
    });
    renderPage(`/purchasing/new?supplierId=${supplierId}`);

    await selectSupplierAndOpenFinder(user);
    await user.click(screen.getByTestId(`po-add-${buyerProductId}`));
    await user.click(screen.getByTestId("po-payment-option-Cash"));

    await waitFor(() => {
      expect(screen.getByTestId("po-create-submit")).toBeDisabled();
    });
  });

  it("auto-selects Delivery fulfillment when Pay on Delivery timing is chosen", async () => {
    const user = userEvent.setup();
    getBuyerConnectedSupplierCommerceReadiness.mockResolvedValue({
      relationshipId,
      isReady: true,
      supportedFulfillmentMethods: ["Pickup", "Delivery"],
      allowPayBeforeFulfillment: true,
      allowPayOnDeliveryOrReceipt: true,
      allowSupplierCredit: false,
      defaultPaymentTiming: "PayBeforeFulfillment",
      requirements: null,
    });
    renderPage(`/purchasing/new?supplierId=${supplierId}`);

    await selectSupplierAndOpenFinder(user);
    await waitFor(() => {
      expect(screen.getByTestId("po-fulfillment-option-Pickup")).toHaveAttribute(
        "data-selected",
        "true",
      );
    });

    await user.click(screen.getByTestId("po-payment-timing-select-option-PayOnDeliveryOrReceipt"));

    await waitFor(() => {
      expect(screen.getByTestId("po-fulfillment-option-Delivery")).toHaveAttribute(
        "data-selected",
        "true",
      );
    });
    expect(screen.getByTestId("po-fulfillment-option-Pickup")).toHaveAttribute(
      "data-selected",
      "false",
    );
  });
});
