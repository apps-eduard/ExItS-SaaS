import { beforeEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor, within } from "@testing-library/react";
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

function renderPage() {
  const client = new QueryClient({
    defaultOptions: {
      queries: { retry: false, networkMode: "always" },
      mutations: { networkMode: "always" },
    },
  });
  render(
    <QueryClientProvider client={client}>
      <MemoryRouter initialEntries={["/purchasing/new"]}>
        <Routes>
          <Route path="/purchasing/new" element={<PurchaseOrderCreatePage />} />
          <Route path="/suppliers/:supplierId/connected-catalog" element={<div>catalog</div>} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
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
    listSuppliers.mockResolvedValue(linkedSupplier());
    listLinks.mockResolvedValue(readyLinkPayload());
    searchExposedCatalog.mockResolvedValue(readyCatalogPayload());
    classifyCatalogReadiness.mockResolvedValue(readinessPayload());
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
  });

  it("loads linked shared orderable products without requiring search", async () => {
    const user = userEvent.setup();
    renderPage();
    await waitFor(() => expect(screen.getByRole("option", { name: /Mica Store/i })).toBeInTheDocument());
    await user.selectOptions(screen.getByTestId("po-supplier"), supplierId);
    await waitFor(() =>
      expect(screen.getByTestId(`po-connected-product-${buyerProductId}`)).toBeInTheDocument(),
    );
    expect(listLinks).toHaveBeenCalled();
    expect(searchExposedCatalog).toHaveBeenCalled();
    expect(classifyCatalogReadiness).toHaveBeenCalled();
    expect(screen.getByText("Bottled Water 500ml")).toBeInTheDocument();
    expect(screen.getByTestId("po-readiness-filters")).toBeInTheDocument();
    expect(screen.getByTestId("po-ready-linked")).toHaveAttribute("aria-selected", "true");
    expect(screen.queryByTestId("po-ready-all")).not.toBeInTheDocument();
    expect(screen.getByTestId("po-ready-newProduct")).toHaveTextContent("New products (1)");
  });

  it("lets setup tabs open shared catalog for connecting", async () => {
    const user = userEvent.setup();
    renderPage();
    await waitFor(() => expect(screen.getByRole("option", { name: /Mica Store/i })).toBeInTheDocument());
    await user.selectOptions(screen.getByTestId("po-supplier"), supplierId);
    await waitFor(() => screen.getByTestId("po-ready-newProduct"));
    await user.click(screen.getByTestId("po-ready-newProduct"));
    await waitFor(() => screen.getByTestId(`po-setup-product-${exposureId2}`));
    expect(screen.queryByTestId(`po-connected-product-${buyerProductId}`)).not.toBeInTheDocument();
    expect(screen.getByTestId(`po-connect-${exposureId2}`)).toHaveAttribute(
      "href",
      `/suppliers/${supplierId}/connected-catalog?setup=newProduct`,
    );
  });

  it("filters products, supports add/stepper totals, and updates subtotal", async () => {
    const user = userEvent.setup();
    renderPage();
    await waitFor(() => expect(screen.getByRole("option", { name: /Mica Store/i })).toBeInTheDocument());
    await user.selectOptions(screen.getByTestId("po-supplier"), supplierId);
    await waitFor(() => screen.getByTestId(`po-connected-product-${buyerProductId}`));

    const search = screen.getByTestId("po-product-search");
    await user.type(search, "soap");
    await waitFor(() =>
      expect(screen.queryByTestId(`po-connected-product-${buyerProductId}`)).not.toBeInTheDocument(),
    );
    await user.clear(search);
    await waitFor(() => screen.getByTestId(`po-connected-product-${buyerProductId}`));

    await user.click(screen.getByTestId(`po-add-${buyerProductId}`));
    expect(screen.getByTestId(`po-line-math-${buyerProductId}`)).toHaveTextContent("₱12");
    expect(screen.getByTestId("po-subtotal")).toHaveTextContent("₱12.00");

    const card = screen.getByTestId(`po-connected-product-${buyerProductId}`);
    await user.click(within(card).getByRole("button", { name: "Increase quantity" }));
    expect(screen.getByTestId(`po-qty-${buyerProductId}`)).toHaveTextContent("2");
    expect(screen.getByTestId(`po-line-math-${buyerProductId}`)).toHaveTextContent("₱24");
    expect(screen.getByTestId("po-subtotal")).toHaveTextContent("₱24.00");

    await user.click(within(card).getByRole("button", { name: "Decrease quantity" }));
    await user.click(within(card).getByRole("button", { name: "Decrease quantity" }));
    expect(screen.getByTestId(`po-add-${buyerProductId}`)).toBeInTheDocument();
    expect(screen.getByTestId("po-subtotal")).toHaveTextContent("₱0.00");
  });

  it("shows supplier stock and disables add / + at max", async () => {
    const user = userEvent.setup();
    getConnectedOrderStock.mockResolvedValue({
      relationshipId,
      supplierBranchId: "77777777-7777-4777-8777-777777777777",
      supplierBranchName: "Main Branch",
      items: [
        { supplierProductId, isTracked: true, availableBaseQuantity: 2 },
        {
          supplierProductId: "00000000-0000-4000-8000-000000000099",
          isTracked: true,
          availableBaseQuantity: 0,
        },
      ],
    });
    renderPage();
    await waitFor(() => expect(screen.getByRole("option", { name: /Mica Store/i })).toBeInTheDocument());
    await user.selectOptions(screen.getByTestId("po-supplier"), supplierId);
    await waitFor(() =>
      expect(screen.getByTestId(`po-stock-${buyerProductId}`)).toHaveTextContent("2 available"),
    );
    expect(getConnectedOrderStock).toHaveBeenCalled();

    await user.click(screen.getByTestId(`po-add-${buyerProductId}`));
    const card = screen.getByTestId(`po-connected-product-${buyerProductId}`);
    await user.click(within(card).getByRole("button", { name: "Increase quantity" }));
    expect(screen.getByTestId(`po-qty-${buyerProductId}`)).toHaveTextContent("2");
    expect(within(card).getByRole("button", { name: "Increase quantity" })).toBeDisabled();
  });

  it("disables Add when supplier stock is zero", async () => {
    const user = userEvent.setup();
    getConnectedOrderStock.mockResolvedValue({
      relationshipId,
      supplierBranchId: "77777777-7777-4777-8777-777777777777",
      supplierBranchName: "Main Branch",
      items: [{ supplierProductId, isTracked: true, availableBaseQuantity: 0 }],
    });
    renderPage();
    await waitFor(() => expect(screen.getByRole("option", { name: /Mica Store/i })).toBeInTheDocument());
    await user.selectOptions(screen.getByTestId("po-supplier"), supplierId);
    await waitFor(() =>
      expect(screen.getByTestId(`po-stock-${buyerProductId}`)).toHaveTextContent("Out of stock"),
    );
    expect(screen.getByTestId(`po-add-${buyerProductId}`)).toBeDisabled();
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
    await waitFor(() => expect(screen.getByRole("option", { name: /Mica Store/i })).toBeInTheDocument());
    await user.selectOptions(screen.getByTestId("po-supplier"), supplierId);
    await waitFor(() => expect(screen.getByTestId("po-open-shared-catalog")).toBeInTheDocument());
    expect(screen.getByTestId("po-open-shared-catalog")).toHaveAttribute(
      "href",
      `/suppliers/${supplierId}/connected-catalog`,
    );
  });
});
