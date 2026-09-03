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
        unitOfMeasureCode: "Piece",
        supplierOrderPrice: 12,
        effectiveSupplierOrderPrice: 12,
        isOrderable: true,
        isExposed: true,
        syncVersion: 1,
        createdAtUtc: "2026-09-01T00:00:00Z",
        updatedAtUtc: "2026-09-01T00:00:00Z",
      },
    ],
    totalCount: 1,
    page: 1,
    pageSize: 50,
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
    createPurchaseOrder.mockReset();
    listCatalogProducts.mockReset();
    listSuppliers.mockResolvedValue(linkedSupplier());
    listLinks.mockResolvedValue(readyLinkPayload());
    searchExposedCatalog.mockResolvedValue(readyCatalogPayload());
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
    expect(screen.getByText("Bottled Water 500ml")).toBeInTheDocument();
    expect(screen.queryByText("New products")).not.toBeInTheDocument();
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

  it("shows empty shared-catalog CTA when no ready products", async () => {
    const user = userEvent.setup();
    listLinks.mockResolvedValue([]);
    searchExposedCatalog.mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 50 });
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
