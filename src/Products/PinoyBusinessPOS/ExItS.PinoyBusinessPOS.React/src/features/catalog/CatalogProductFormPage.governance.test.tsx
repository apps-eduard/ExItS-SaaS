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

const getCatalogProduct = vi.fn();
const createCatalogProduct = vi.fn();
const updateCatalogProduct = vi.fn();
const listCatalogCategories = vi.fn();
const listCatalogBrands = vi.fn();
const promoteCatalogProduct = vi.fn();
const getProductBranchAvailability = vi.fn();
const setBranchProductAvailability = vi.fn();
const listOrganizationBranches = vi.fn();

vi.mock("@/api/pos/pos-catalog-client", () => ({
  getCatalogProduct: (...args: unknown[]) => getCatalogProduct(...args),
  createCatalogProduct: (...args: unknown[]) => createCatalogProduct(...args),
  updateCatalogProduct: (...args: unknown[]) => updateCatalogProduct(...args),
  listCatalogCategories: (...args: unknown[]) => listCatalogCategories(...args),
  listCatalogBrands: (...args: unknown[]) => listCatalogBrands(...args),
  promoteCatalogProduct: (...args: unknown[]) => promoteCatalogProduct(...args),
  getProductBranchAvailability: (...args: unknown[]) => getProductBranchAvailability(...args),
  setBranchProductAvailability: (...args: unknown[]) => setBranchProductAvailability(...args),
  checkCatalogProductNameConflict: vi.fn().mockResolvedValue({
    isDuplicate: false,
    canRevealExisting: false,
  }),
  deactivateCatalogProduct: vi.fn(),
  reactivateCatalogProduct: vi.fn(),
  uploadCatalogProductImage: vi.fn(),
  createCatalogCategory: vi.fn(),
  createCatalogBrand: vi.fn(),
}));

vi.mock("@/api/platform/platform-auth-client", () => ({
  listOrganizationBranches: (...args: unknown[]) => listOrganizationBranches(...args),
}));

vi.mock("@/api/pos/pos-inventory-client", () => ({
  enableInventoryTracking: vi.fn(),
}));

const LOCAL_PRODUCT = {
  productId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
  organizationId: "11111111-1111-1111-1111-111111111111",
  name: "Local Snack",
  description: null,
  sku: null,
  barcode: null,
  categoryId: null,
  brandId: null,
  brandName: null,
  unitOfMeasure: "Piece",
  sellingMode: "PerItem",
  sellingPrice: 15,
  status: "Active",
  canBeSold: true,
  businessUsage: "Resale",
  scope: "BranchLocal",
  originBranchId: "22222222-2222-2222-2222-222222222222",
  units: [],
  tracksExpiration: false,
  createdAtUtc: "2026-01-01T00:00:00Z",
  updatedAtUtc: "2026-01-02T00:00:00Z",
};

const STANDARD_PRODUCT = {
  ...LOCAL_PRODUCT,
  productId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  name: "Org Soap",
  scope: "OrganizationStandard",
  originBranchId: null,
  isOfferedAtBranch: true,
};

function renderForm(mode: "create" | "edit", productId = STANDARD_PRODUCT.productId) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  const path = mode === "create" ? "/catalog/products/new" : `/catalog/products/${productId}/edit`;
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[path]}>
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

describe("CatalogProductFormPage governance", () => {
  beforeEach(() => {
    vi.clearAllMocks();
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
    getProductBranchAvailability.mockResolvedValue({
      productId: STANDARD_PRODUCT.productId,
      scope: "OrganizationStandard",
      explicitRows: [],
    });
    getCatalogProduct.mockResolvedValue(STANDARD_PRODUCT);
    createCatalogProduct.mockResolvedValue(STANDARD_PRODUCT);
  });

  it("create form lets owner choose OrganizationStandard by default and sends scope", async () => {
    const user = userEvent.setup();
    renderForm("create");
    await screen.findByTestId("catalog-create-scope");
    expect(screen.getByTestId("catalog-create-scope-OrganizationStandard")).toBeChecked();

    await user.clear(screen.getByRole("textbox", { name: "catalog.name" }));
    await user.type(screen.getByRole("textbox", { name: "catalog.name" }), "New Org Item");
    await user.click(screen.getByTestId("catalog-save"));

    await waitFor(() => expect(createCatalogProduct).toHaveBeenCalledTimes(1));
    expect(createCatalogProduct.mock.calls[0][1].scope).toBe("OrganizationStandard");
    expect(createCatalogProduct.mock.calls[0][1].originBranchId).toBeUndefined();
  });

  it("create form for branch actor is fixed BranchLocal", async () => {
    sessionGrant.organizationManagementAuthority = false;
    sessionGrant.membershipRole = "Member";
    renderForm("create");
    await screen.findByTestId("catalog-create-scope");
    expect(screen.queryByTestId("catalog-create-scope-OrganizationStandard")).not.toBeInTheDocument();
    expect(
      screen.getByText((content) => content.includes("catalog.governance.branchProduct")),
    ).toBeInTheDocument();
  });

  it("edit Standard without governance is read-only", async () => {
    sessionGrant.organizationManagementAuthority = false;
    sessionGrant.membershipRole = "Member";
    getCatalogProduct.mockResolvedValue(STANDARD_PRODUCT);
    renderForm("edit", STANDARD_PRODUCT.productId);
    await screen.findByTestId("catalog-managed-by-organization");
    expect(screen.queryByTestId("catalog-save")).not.toBeInTheDocument();
    expect(screen.queryByTestId("catalog-deactivate")).not.toBeInTheDocument();
    expect(screen.getByRole("textbox", { name: "catalog.name" })).toBeDisabled();
  });

  it("owner can promote BranchLocal via confirmation dialog", async () => {
    const user = userEvent.setup();
    getCatalogProduct.mockResolvedValue(LOCAL_PRODUCT);
    promoteCatalogProduct.mockResolvedValue({
      ...LOCAL_PRODUCT,
      scope: "OrganizationStandard",
    });
    renderForm("edit", LOCAL_PRODUCT.productId);
    await screen.findByTestId("catalog-promote");
    await user.click(screen.getByTestId("catalog-promote"));
    expect(screen.getByTestId("catalog-promote-dialog")).toHaveAttribute("role", "dialog");
    await user.click(screen.getByTestId("catalog-promote-confirm"));
    await waitFor(() => expect(promoteCatalogProduct).toHaveBeenCalledTimes(1));
  });

  it("BranchLocal shows origin-only availability without cross-branch toggles", async () => {
    getCatalogProduct.mockResolvedValue(LOCAL_PRODUCT);
    renderForm("edit", LOCAL_PRODUCT.productId);
    const section = await screen.findByTestId("catalog-branch-availability");
    expect(section).toHaveTextContent("catalog.governance.availableAtOriginOnly");
    expect(screen.queryByTestId(/catalog-availability-/)).not.toBeInTheDocument();
  });

  it("branch actor does not see promote control on Local", async () => {
    sessionGrant.organizationManagementAuthority = false;
    sessionGrant.membershipRole = "Member";
    getCatalogProduct.mockResolvedValue(LOCAL_PRODUCT);
    renderForm("edit", LOCAL_PRODUCT.productId);
    await screen.findByTestId("catalog-edit-scope-summary");
    expect(screen.queryByTestId("catalog-promote")).not.toBeInTheDocument();
  });
});
