import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter } from "react-router-dom";
import { CatalogProductsPage } from "@/features/catalog/CatalogProductsPage";

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
      branchName: "Branch A",
    },
    sessionGrant,
  }),
}));

vi.mock("@/navigation/page-back-nav", () => ({
  pageBackNav: { managerHome: { to: "/manager", labelKey: "nav.manager" } },
}));

const listCatalogProducts = vi.fn();
const listCatalogCategories = vi.fn();
const listCatalogBrands = vi.fn();
const listOrganizationBranches = vi.fn();

vi.mock("@/api/pos/pos-catalog-client", () => ({
  listCatalogProducts: (...args: unknown[]) => listCatalogProducts(...args),
  listCatalogCategories: (...args: unknown[]) => listCatalogCategories(...args),
  listCatalogBrands: (...args: unknown[]) => listCatalogBrands(...args),
}));

vi.mock("@/api/platform/platform-auth-client", () => ({
  listOrganizationBranches: (...args: unknown[]) => listOrganizationBranches(...args),
}));

const PRODUCT_ID = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
const LOCAL_PRODUCT_ID = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";

const STANDARD_PRODUCT = {
  productId: PRODUCT_ID,
  organizationId: "11111111-1111-1111-1111-111111111111",
  name: "Coke",
  brandName: null,
  unitOfMeasure: "Piece",
  sellingMode: "PerItem",
  sellingPrice: 100,
  effectiveSellingPrice: 100,
  hasBranchPriceOverride: false,
  status: "Active",
  canBeSold: true,
  businessUsage: "Resale",
  scope: "OrganizationStandard",
  isOfferedAtBranch: true,
  createdAtUtc: "2026-01-01T00:00:00Z",
  updatedAtUtc: "2026-01-01T00:00:00Z",
};

const LOCAL_PRODUCT = {
  productId: LOCAL_PRODUCT_ID,
  organizationId: "11111111-1111-1111-1111-111111111111",
  name: "Local Snack",
  brandName: null,
  unitOfMeasure: "Piece",
  sellingMode: "PerItem",
  sellingPrice: 15,
  effectiveSellingPrice: 15,
  status: "Active",
  canBeSold: true,
  businessUsage: "Resale",
  scope: "BranchLocal",
  originBranchId: "22222222-2222-2222-2222-222222222222",
  createdAtUtc: "2026-01-01T00:00:00Z",
  updatedAtUtc: "2026-01-02T00:00:00Z",
};

function renderPage(queryClient?: QueryClient) {
  const client =
    queryClient ??
    new QueryClient({
      defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
    });
  return {
    client,
    ...render(
      <QueryClientProvider client={client}>
        <MemoryRouter>
          <CatalogProductsPage />
        </MemoryRouter>
      </QueryClientProvider>,
    ),
  };
}

describe("CatalogProductsPage effective branch price", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    listCatalogCategories.mockResolvedValue({ items: [], totalCount: 0 });
    listCatalogBrands.mockResolvedValue({ items: [], totalCount: 0 });
    listOrganizationBranches.mockResolvedValue({
      ok: true,
      branches: [
        {
          id: "22222222-2222-2222-2222-222222222222",
          organizationId: "11111111-1111-1111-1111-111111111111",
          code: "BR-A",
          name: "Branch A",
          isPrimary: false,
          status: "Active",
        },
      ],
    });
  });

  it("CATPRICE-01 displays effective/org price when no branch override", async () => {
    listCatalogProducts.mockResolvedValue({
      items: [STANDARD_PRODUCT],
      totalCount: 1,
    });
    renderPage();
    const price = await screen.findByTestId(`catalog-product-price-${PRODUCT_ID}`);
    expect(price).toHaveTextContent("₱100.00");
    expect(screen.getByTestId(`catalog-product-price-origin-${PRODUCT_ID}`)).toHaveTextContent(
      "catalog.productListPrice.orgDefault",
    );
  });

  it("CATPRICE-02 displays branch effective price when override exists", async () => {
    listCatalogProducts.mockResolvedValue({
      items: [
        {
          ...STANDARD_PRODUCT,
          effectiveSellingPrice: 120,
          hasBranchPriceOverride: true,
        },
      ],
      totalCount: 1,
    });
    renderPage();
    const price = await screen.findByTestId(`catalog-product-price-${PRODUCT_ID}`);
    expect(price).toHaveTextContent("₱120.00");
  });

  it("CATPRICE-03 does not display organization default as primary when effective differs", async () => {
    listCatalogProducts.mockResolvedValue({
      items: [
        {
          ...STANDARD_PRODUCT,
          sellingPrice: 100,
          effectiveSellingPrice: 120,
          hasBranchPriceOverride: true,
        },
      ],
      totalCount: 1,
    });
    renderPage();
    const price = await screen.findByTestId(`catalog-product-price-${PRODUCT_ID}`);
    expect(price).toHaveTextContent("₱120.00");
    expect(price).not.toHaveTextContent("₱100.00");
  });

  it("CATPRICE-04 shows branch override indicator", async () => {
    listCatalogProducts.mockResolvedValue({
      items: [
        {
          ...STANDARD_PRODUCT,
          effectiveSellingPrice: 120,
          hasBranchPriceOverride: true,
        },
      ],
      totalCount: 1,
    });
    renderPage();
    expect(
      await screen.findByTestId(`catalog-product-price-origin-${PRODUCT_ID}`),
    ).toHaveTextContent("catalog.productListPrice.branchPrice");
  });

  it("CATPRICE-05 shows org default indicator without override", async () => {
    listCatalogProducts.mockResolvedValue({
      items: [STANDARD_PRODUCT],
      totalCount: 1,
    });
    renderPage();
    expect(
      await screen.findByTestId(`catalog-product-price-origin-${PRODUCT_ID}`),
    ).toHaveTextContent("catalog.productListPrice.orgDefault");
  });

  it("CATPRICE-06 preserves BranchLocal price display without org inheritance label", async () => {
    listCatalogProducts.mockResolvedValue({
      items: [LOCAL_PRODUCT],
      totalCount: 1,
    });
    renderPage();
    const price = await screen.findByTestId(`catalog-product-price-${LOCAL_PRODUCT_ID}`);
    expect(price).toHaveTextContent("₱15.00");
    expect(
      screen.queryByTestId(`catalog-product-price-origin-${LOCAL_PRODUCT_ID}`),
    ).not.toBeInTheDocument();
  });

  it("CATPRICE-07 refetches updated effective price after cache invalidation", async () => {
    listCatalogProducts
      .mockResolvedValueOnce({
        items: [
          {
            ...STANDARD_PRODUCT,
            effectiveSellingPrice: 100,
            hasBranchPriceOverride: false,
          },
        ],
        totalCount: 1,
      })
      .mockResolvedValue({
        items: [
          {
            ...STANDARD_PRODUCT,
            effectiveSellingPrice: 120,
            hasBranchPriceOverride: true,
          },
        ],
        totalCount: 1,
      });

    const { client } = renderPage();
    const price = await screen.findByTestId(`catalog-product-price-${PRODUCT_ID}`);
    expect(price).toHaveTextContent("₱100.00");

    await client.invalidateQueries({ queryKey: ["catalog"] });

    await waitFor(() => {
      expect(screen.getByTestId(`catalog-product-price-${PRODUCT_ID}`)).toHaveTextContent(
        "₱120.00",
      );
    });
    expect(screen.getByTestId(`catalog-product-price-origin-${PRODUCT_ID}`)).toHaveTextContent(
      "catalog.productListPrice.branchPrice",
    );
  });
});
