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
  createCatalogCategory: vi.fn(),
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

const EXISTING_ORG = {
  productId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  organizationId: "11111111-1111-1111-1111-111111111111",
  name: "Coke 1L",
  description: null,
  sku: null,
  barcode: null,
  categoryId: null,
  brandId: null,
  brandName: null,
  unitOfMeasure: "Piece",
  sellingMode: "PerItem",
  sellingPrice: 45,
  status: "Active",
  canBeSold: true,
  businessUsage: "Resale",
  scope: "OrganizationStandard",
  originBranchId: null,
  isOfferedAtBranch: true,
  units: [],
  tracksExpiration: false,
  createdAtUtc: "2026-01-01T00:00:00Z",
  updatedAtUtc: "2026-01-02T00:00:00Z",
};

function renderCreate() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={["/catalog/products/new"]}>
        <ToastProvider>
          <Routes>
            <Route path="/catalog/products/new" element={<CatalogProductFormPage mode="create" />} />
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

describe("CatalogProductFormPage duplicate name UX", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    onlineState.online = true;
    sessionGrant.organizationManagementAuthority = true;
    sessionGrant.membershipRole = "OrganizationOwner";
    listCatalogCategories.mockResolvedValue({ items: [], totalCount: 0 });
    listCatalogBrands.mockResolvedValue({ items: [], totalCount: 0 });
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
    createCatalogProduct.mockResolvedValue(EXISTING_ORG);
  });

  async function typeProductName(value: string) {
    const user = userEvent.setup();
    renderCreate();
    const nameInput = await screen.findByRole("textbox", { name: "catalog.name" });
    await user.clear(nameInput);
    await user.type(nameInput, value);
    return user;
  }

  it("shows Use existing product when duplicate is visible", async () => {
    checkCatalogProductNameConflict.mockResolvedValue({
      isDuplicate: true,
      canRevealExisting: true,
      existingProduct: EXISTING_ORG,
    });

    await typeProductName("Coke 1L");

    expect(await screen.findByTestId("catalog-name-conflict")).toBeInTheDocument();
    expect(screen.getByTestId("catalog-name-conflict-name")).toHaveTextContent("Coke 1L");
    const useExisting = screen.getByTestId("catalog-name-conflict-use-existing");
    expect(useExisting).toHaveAttribute(
      "href",
      `/catalog/products/${EXISTING_ORG.productId}/edit`,
    );
    expect(useExisting).toHaveTextContent("catalog.duplicate.useExisting");
    expect(screen.getByTestId("catalog-save")).toBeDisabled();
  });

  it("does not offer Create anyway on duplicate", async () => {
    checkCatalogProductNameConflict.mockResolvedValue({
      isDuplicate: true,
      canRevealExisting: true,
      existingProduct: EXISTING_ORG,
    });

    await typeProductName("Coke 1L");

    await screen.findByTestId("catalog-name-conflict");
    expect(screen.queryByText(/create anyway/i)).not.toBeInTheDocument();
    expect(screen.queryByTestId("catalog-name-conflict-create-anyway")).not.toBeInTheDocument();
  });

  it("shows privacy-safe message for hidden foreign Local", async () => {
    checkCatalogProductNameConflict.mockResolvedValue({
      isDuplicate: true,
      canRevealExisting: false,
      existingProduct: null,
    });

    await typeProductName("Fresh Bangus");

    const panel = await screen.findByTestId("catalog-name-conflict");
    expect(panel).toHaveTextContent("catalog.duplicate.hiddenForeign");
    expect(screen.queryByTestId("catalog-name-conflict-use-existing")).not.toBeInTheDocument();
    expect(screen.queryByTestId("catalog-name-conflict-name")).not.toBeInTheDocument();
    expect(screen.getByTestId("catalog-save")).toBeDisabled();
  });

  it("blocks offline create with OnlineRequiredCard", async () => {
    onlineState.online = false;
    renderCreate();

    expect(await screen.findByTestId("online-required")).toBeInTheDocument();
    expect(screen.getByText("online_required.catalog_product_create")).toBeInTheDocument();
    expect(screen.getByTestId("catalog-save")).toBeDisabled();
    expect(checkCatalogProductNameConflict).not.toHaveBeenCalled();
  });

  it("passes excludeProductId undefined on create conflict checks", async () => {
    checkCatalogProductNameConflict.mockResolvedValue({
      isDuplicate: false,
      canRevealExisting: false,
    });

    await typeProductName("Unique Name");

    await waitFor(() => expect(checkCatalogProductNameConflict).toHaveBeenCalled());
    expect(checkCatalogProductNameConflict.mock.calls[0][1]).toEqual({
      name: "Unique Name",
      excludeProductId: undefined,
    });
  });
});
