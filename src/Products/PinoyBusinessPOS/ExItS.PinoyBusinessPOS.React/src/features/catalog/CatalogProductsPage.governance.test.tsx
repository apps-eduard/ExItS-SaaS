import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
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
      branchName: "Main branch",
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

const ORG_PRODUCT = {
  productId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  organizationId: "11111111-1111-1111-1111-111111111111",
  name: "Org Soap",
  brandName: null,
  unitOfMeasure: "Piece",
  sellingMode: "PerItem",
  sellingPrice: 28,
  status: "Active",
  canBeSold: true,
  businessUsage: "Resale",
  scope: "OrganizationStandard",
  isOfferedAtBranch: true,
  createdAtUtc: "2026-01-01T00:00:00Z",
  updatedAtUtc: "2026-01-01T00:00:00Z",
};

const LOCAL_PRODUCT = {
  productId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
  organizationId: "11111111-1111-1111-1111-111111111111",
  name: "Local Snack",
  brandName: null,
  unitOfMeasure: "Piece",
  sellingMode: "PerItem",
  sellingPrice: 15,
  status: "Active",
  canBeSold: true,
  businessUsage: "Resale",
  scope: "BranchLocal",
  originBranchId: "22222222-2222-2222-2222-222222222222",
  createdAtUtc: "2026-01-01T00:00:00Z",
  updatedAtUtc: "2026-01-02T00:00:00Z",
};

const NOT_OFFERED = {
  ...ORG_PRODUCT,
  productId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
  name: "Hidden Org Item",
  isOfferedAtBranch: false,
};

function renderPage() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <CatalogProductsPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe("CatalogProductsPage governance", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    sessionGrant.organizationManagementAuthority = true;
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
    listCatalogProducts.mockResolvedValue({
      items: [ORG_PRODUCT, LOCAL_PRODUCT, NOT_OFFERED],
      totalCount: 3,
    });
  });

  it("shows scope badges and not-offered label", async () => {
    renderPage();
    await screen.findByText("Org Soap");
    const badges = screen.getAllByTestId("catalog-product-scope-badge");
    expect(badges.length).toBeGreaterThanOrEqual(2);
    expect(screen.getByText("catalog.governance.branchProductThisBranch")).toBeInTheDocument();
    expect(screen.getByTestId("catalog-product-offering")).toHaveTextContent(
      "catalog.governance.notOfferedAtBranch",
    );
  });

  it("passes scope filter to listCatalogProducts and resets page", async () => {
    const user = userEvent.setup();
    renderPage();
    await screen.findByTestId("catalog-scope-filters");

    await waitFor(() => {
      expect(listCatalogProducts).toHaveBeenCalled();
    });
    expect(listCatalogProducts.mock.calls[0][1].scope).toBeUndefined();

    await user.click(screen.getByTestId("catalog-scope-BranchLocal"));

    await waitFor(() => {
      const last = listCatalogProducts.mock.calls.at(-1)?.[1];
      expect(last?.scope).toBe("BranchLocal");
      expect(last?.page).toBe(1);
    });
  });

  it("hides global/template toolbar for non-govern actors", async () => {
    sessionGrant.organizationManagementAuthority = false;
    sessionGrant.membershipRole = "Member";
    renderPage();
    await screen.findByTestId("catalog-toolbar");
    expect(screen.queryByTestId("catalog-open-templates")).not.toBeInTheDocument();
    expect(screen.queryByTestId("catalog-open-global-catalog")).not.toBeInTheDocument();
    expect(screen.getByTestId("catalog-new-product")).toBeInTheDocument();
  });
});
