import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { ToastProvider } from "@/components/exits/ToastProvider";
import { CatalogProductFormPage } from "@/features/catalog/CatalogProductFormPage";

vi.mock("@/i18n/I18nProvider", () => ({
  useI18n: () => ({
    t: (key: string) => key,
  }),
}));

vi.mock("@/workspace/use-pos-workspace-scope", () => ({
  usePosWorkspaceScope: () => ({
    organizationId: "11111111-1111-1111-1111-111111111111",
    branchId: "22222222-2222-2222-2222-222222222222",
  }),
}));

const { sessionGrant } = vi.hoisted(() => ({
  sessionGrant: {
    productAccessAllowed: true,
    organizationManagementAuthority: true,
    membershipRole: "OrganizationOwner",
    productRole: "Owner",
  },
}));

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => ({
    boundWorkspace: {
      organizationId: "11111111-1111-1111-1111-111111111111",
      branchId: "22222222-2222-2222-2222-222222222222",
      branchName: "Main branch",
    },
    sessionGrant,
  }),
}));

vi.mock("@/navigation/page-back-nav", () => ({
  pageBackNav: { catalog: { to: "/catalog", labelKey: "catalog.productsTitle" } },
}));

const { onlineState } = vi.hoisted(() => ({
  onlineState: { online: true },
}));

vi.mock("@/connectivity/browser-online", () => ({
  useBrowserOnline: () => onlineState.online,
}));

const getCatalogProduct = vi.fn();
const createCatalogProduct = vi.fn();
const updateCatalogProduct = vi.fn();
const listCatalogCategories = vi.fn();
const listCatalogBrands = vi.fn();
const createCatalogCategory = vi.fn();
const checkCatalogProductNameConflict = vi.fn();
const listOrganizationBranches = vi.fn();

vi.mock("@/api/pos/pos-catalog-client", () => ({
  getCatalogProduct: (...args: unknown[]) => getCatalogProduct(...args),
  createCatalogProduct: (...args: unknown[]) => createCatalogProduct(...args),
  updateCatalogProduct: (...args: unknown[]) => updateCatalogProduct(...args),
  listCatalogCategories: (...args: unknown[]) => listCatalogCategories(...args),
  listCatalogBrands: (...args: unknown[]) => listCatalogBrands(...args),
  checkCatalogProductNameConflict: (...args: unknown[]) =>
    checkCatalogProductNameConflict(...args),
  deactivateCatalogProduct: vi.fn(),
  reactivateCatalogProduct: vi.fn(),
  uploadCatalogProductImage: vi.fn(),
  createCatalogCategory: (...args: unknown[]) => createCatalogCategory(...args),
  createCatalogBrand: vi.fn(),
  promoteCatalogProduct: vi.fn(),
  getProductBranchAvailability: vi.fn().mockResolvedValue({
    productId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    scope: "OrganizationStandard",
    explicitRows: [],
  }),
  setBranchProductAvailability: vi.fn(),
}));

vi.mock("@/api/platform/platform-auth-client", () => ({
  listOrganizationBranches: (...args: unknown[]) => listOrganizationBranches(...args),
}));

vi.mock("@/api/pos/pos-inventory-client", () => ({
  enableInventoryTracking: vi.fn(),
}));

const PRODUCT = {
  productId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  organizationId: "11111111-1111-1111-1111-111111111111",
  name: "Apple",
  description: null,
  sku: null,
  barcode: null,
  categoryId: null,
  categoryName: null,
  brandId: null,
  brandName: null,
  unitOfMeasure: "Piece",
  sellingMode: "PerItem",
  sellingPrice: 10,
  status: "Active",
  canBeSold: true,
  canBeUsedAsIngredient: false,
  isProduced: false,
  businessUsage: "Resale",
  scope: "OrganizationStandard",
  originBranchId: null,
  isOfferedAtBranch: true,
  units: [],
  tracksExpiration: false,
  expirationWarningDays: null,
  isTracked: false,
  createdAtUtc: "2026-01-01T00:00:00Z",
  updatedAtUtc: "2026-01-02T00:00:00Z",
};

const CREATED_CATEGORY = {
  categoryId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
  organizationId: "11111111-1111-1111-1111-111111111111",
  name: "Fruits",
  status: "Active",
  createdAtUtc: "2026-09-15T00:00:00Z",
  updatedAtUtc: "2026-09-15T00:00:00Z",
};

function renderEdit() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter
        initialEntries={[`/catalog/products/${PRODUCT.productId}/edit`]}
      >
        <ToastProvider>
          <Routes>
            <Route
              path="/catalog/products/:productId/edit"
              element={<CatalogProductFormPage mode="edit" />}
            />
          </Routes>
        </ToastProvider>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe("CatalogProductFormPage quick-add category on save", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    onlineState.online = true;
    sessionGrant.organizationManagementAuthority = true;
    sessionGrant.membershipRole = "OrganizationOwner";
    getCatalogProduct.mockResolvedValue(PRODUCT);
    listCatalogCategories.mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 50 });
    listCatalogBrands.mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 50 });
    listOrganizationBranches.mockResolvedValue({
      ok: true,
      branches: [
        {
          id: "22222222-2222-2222-2222-222222222222",
          organizationId: "11111111-1111-1111-1111-111111111111",
          code: "MAIN",
          name: "Main branch",
          isPrimary: true,
          status: "Active",
        },
      ],
    });
    checkCatalogProductNameConflict.mockResolvedValue({
      isDuplicate: false,
      canRevealExisting: false,
    });
    createCatalogCategory.mockResolvedValue(CREATED_CATEGORY);
    updateCatalogProduct.mockResolvedValue({
      ...PRODUCT,
      categoryId: CREATED_CATEGORY.categoryId,
      categoryName: CREATED_CATEGORY.name,
      updatedAtUtc: "2026-09-15T12:00:00Z",
    });
  });

  it("creates pending category name on Save without clicking +", async () => {
    const user = userEvent.setup();
    renderEdit();

    const categoryInput = await screen.findByRole("textbox", {
      name: "catalog.newCategoryPlaceholder",
    });
    await user.type(categoryInput, "Fruits");

    await user.click(screen.getByTestId("catalog-save"));

    await waitFor(() => expect(createCatalogCategory).toHaveBeenCalledTimes(1));
    expect(createCatalogCategory).toHaveBeenCalledWith(
      expect.objectContaining({
        organizationId: "11111111-1111-1111-1111-111111111111",
      }),
      { name: "Fruits" },
    );

    await waitFor(() => expect(updateCatalogProduct).toHaveBeenCalledTimes(1));
    expect(updateCatalogProduct.mock.calls[0][2]).toEqual(
      expect.objectContaining({
        categoryId: CREATED_CATEGORY.categoryId,
      }),
    );
  });

  it("selects existing category when typed name already exists", async () => {
    listCatalogCategories.mockResolvedValue({
      items: [CREATED_CATEGORY],
      totalCount: 1,
      page: 1,
      pageSize: 50,
    });
    const user = userEvent.setup();
    renderEdit();

    const categoryInput = await screen.findByRole("textbox", {
      name: "catalog.newCategoryPlaceholder",
    });
    await user.type(categoryInput, "Fruits");
    await user.click(screen.getByTestId("catalog-save"));

    await waitFor(() => expect(updateCatalogProduct).toHaveBeenCalledTimes(1));
    expect(createCatalogCategory).not.toHaveBeenCalled();
    expect(updateCatalogProduct.mock.calls[0][2]).toEqual(
      expect.objectContaining({
        categoryId: CREATED_CATEGORY.categoryId,
      }),
    );
  });
});
