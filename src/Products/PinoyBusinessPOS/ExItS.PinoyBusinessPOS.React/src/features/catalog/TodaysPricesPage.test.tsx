import { beforeEach, describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter } from "react-router-dom";
import { ToastProvider } from "@/components/exits/ToastProvider";
import { TodaysPricesPage } from "@/features/catalog/TodaysPricesPage";

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

const { pricesSessionGrant } = vi.hoisted(() => ({
  pricesSessionGrant: {
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
    sessionGrant: pricesSessionGrant,
  }),
}));

vi.mock("@/navigation/page-back-nav", () => ({
  pageBackNav: { catalog: { to: "/catalog", labelKey: "catalog.title" } },
}));

const listCatalogProducts = vi.fn();
const updateCatalogProductPrices = vi.fn();

vi.mock("@/api/pos/pos-catalog-client", () => ({
  listCatalogProducts: (...args: unknown[]) => listCatalogProducts(...args),
  updateCatalogProductPrices: (...args: unknown[]) => updateCatalogProductPrices(...args),
}));

const PRODUCT_A = {
  productId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  organizationId: "11111111-1111-1111-1111-111111111111",
  name: "Bath Soap Bar",
  brandName: null,
  unitOfMeasure: "Piece",
  sellingMode: "PerItem",
  sellingPrice: 28,
  status: "Active",
  scope: "BranchLocal",
  createdAtUtc: "2026-01-01T00:00:00Z",
  updatedAtUtc: "2026-01-01T00:00:00Z",
};

const PRODUCT_B = {
  productId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
  organizationId: "11111111-1111-1111-1111-111111111111",
  name: "Biscuit Pack",
  brandName: null,
  unitOfMeasure: "Piece",
  sellingMode: "PerItem",
  sellingPrice: 15,
  status: "Active",
  scope: "BranchLocal",
  createdAtUtc: "2026-01-01T00:00:00Z",
  updatedAtUtc: "2026-01-02T00:00:00Z",
};

const STANDARD_PRODUCT = {
  productId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
  organizationId: "11111111-1111-1111-1111-111111111111",
  name: "Org Standard Tea",
  brandName: null,
  unitOfMeasure: "Piece",
  sellingMode: "PerItem",
  sellingPrice: 40,
  status: "Active",
  scope: "OrganizationStandard",
  createdAtUtc: "2026-01-01T00:00:00Z",
  updatedAtUtc: "2026-01-01T00:00:00Z",
};

function renderPage() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <ToastProvider>
          <TodaysPricesPage />
        </ToastProvider>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe("TodaysPricesPage per-product save", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    pricesSessionGrant.organizationManagementAuthority = true;
    pricesSessionGrant.membershipRole = "OrganizationOwner";
    listCatalogProducts.mockResolvedValue({ items: [PRODUCT_A, PRODUCT_B], totalCount: 2 });
  });

  it("shows Save only for dirty product and saves one item", async () => {
    const user = userEvent.setup();
    updateCatalogProductPrices.mockResolvedValue({
      results: [
        {
          productId: PRODUCT_A.productId,
          succeeded: true,
          changed: true,
          product: {
            ...PRODUCT_A,
            sellingPrice: 30,
            updatedAtUtc: "2026-01-03T00:00:00Z",
          },
        },
      ],
      succeededCount: 1,
      failedCount: 0,
      changedCount: 1,
    });

    renderPage();
    await screen.findByText("Bath Soap Bar");

    expect(screen.queryByTestId(`price-save-${PRODUCT_A.productId}`)).not.toBeInTheDocument();

    const rowA = screen.getByTestId(`price-row-${PRODUCT_A.productId}`);
    const rowB = screen.getByTestId(`price-row-${PRODUCT_B.productId}`);
    await user.clear(within(rowA).getByRole("textbox", { name: "prices.newPrice" }));
    await user.type(within(rowA).getByRole("textbox", { name: "prices.newPrice" }), "30");

    expect(within(rowA).getByTestId(`price-save-${PRODUCT_A.productId}`)).toBeInTheDocument();
    expect(within(rowB).queryByTestId(`price-save-${PRODUCT_B.productId}`)).not.toBeInTheDocument();
    expect(screen.queryByTestId("prices-save")).not.toBeInTheDocument();
    expect(rowA.querySelector(".truncate")).toBeNull();

    await user.click(within(rowA).getByTestId(`price-save-${PRODUCT_A.productId}`));

    await waitFor(() => {
      expect(updateCatalogProductPrices).toHaveBeenCalledTimes(1);
    });
    expect(updateCatalogProductPrices.mock.calls[0][1]).toEqual({
      items: [
        {
          productId: PRODUCT_A.productId,
          sellingPrice: 30,
          expectedUpdatedAtUtc: PRODUCT_A.updatedAtUtc,
        },
      ],
    });

    await waitFor(() => {
      expect(within(rowA).getByText(/₱30\.00/)).toBeInTheDocument();
    });
    expect(within(rowA).queryByTestId(`price-save-${PRODUCT_A.productId}`)).not.toBeInTheDocument();
    expect(await screen.findByTestId("exits-toast")).toHaveTextContent("prices.updatedToast");
  });

  it("Enter saves focused dirty valid product", async () => {
    const user = userEvent.setup();
    updateCatalogProductPrices.mockResolvedValue({
      results: [
        {
          productId: PRODUCT_A.productId,
          succeeded: true,
          changed: true,
          product: { ...PRODUCT_A, sellingPrice: 29, updatedAtUtc: "2026-01-04T00:00:00Z" },
        },
      ],
      succeededCount: 1,
      failedCount: 0,
      changedCount: 1,
    });

    renderPage();
    const rowA = await screen.findByTestId(`price-row-${PRODUCT_A.productId}`);
    const input = within(rowA).getByRole("textbox", { name: "prices.newPrice" });
    await user.clear(input);
    await user.type(input, "29");
    fireEvent.keyDown(input, { key: "Enter", code: "Enter" });

    await waitFor(() => expect(updateCatalogProductPrices).toHaveBeenCalledTimes(1));
    expect(updateCatalogProductPrices.mock.calls[0][1].items).toHaveLength(1);
    expect(updateCatalogProductPrices.mock.calls[0][1].items[0].productId).toBe(PRODUCT_A.productId);
  });

  it("failure preserves draft and current price", async () => {
    const user = userEvent.setup();
    updateCatalogProductPrices.mockResolvedValue({
      results: [
        {
          productId: PRODUCT_A.productId,
          succeeded: false,
          changed: false,
          errorCode: "pos.catalog.concurrency_conflict",
          errorMessage: "Product was modified by another user.",
        },
      ],
      succeededCount: 0,
      failedCount: 1,
      changedCount: 0,
    });

    renderPage();
    const rowA = await screen.findByTestId(`price-row-${PRODUCT_A.productId}`);
    await user.clear(within(rowA).getByRole("textbox", { name: "prices.newPrice" }));
    await user.type(within(rowA).getByRole("textbox", { name: "prices.newPrice" }), "40");
    await user.click(within(rowA).getByTestId(`price-save-${PRODUCT_A.productId}`));

    await waitFor(() => {
      expect(within(rowA).getByText("prices.staleConflict")).toBeInTheDocument();
    });
    expect(within(rowA).getByRole("textbox", { name: "prices.newPrice" })).toHaveValue("40");
    expect(within(rowA).getByText(/₱28\.00/)).toBeInTheDocument();
    expect(within(rowA).getByTestId(`price-save-${PRODUCT_A.productId}`)).toBeInTheDocument();
  });

  it("saving one product does not wipe another dirty draft after refetch", async () => {
    const user = userEvent.setup();
    updateCatalogProductPrices.mockResolvedValue({
      results: [
        {
          productId: PRODUCT_A.productId,
          succeeded: true,
          changed: true,
          product: { ...PRODUCT_A, sellingPrice: 30, updatedAtUtc: "2026-01-05T00:00:00Z" },
        },
      ],
      succeededCount: 1,
      failedCount: 0,
      changedCount: 1,
    });

    renderPage();
    const rowA = await screen.findByTestId(`price-row-${PRODUCT_A.productId}`);
    const rowB = screen.getByTestId(`price-row-${PRODUCT_B.productId}`);

    await user.clear(within(rowA).getByRole("textbox", { name: "prices.newPrice" }));
    await user.type(within(rowA).getByRole("textbox", { name: "prices.newPrice" }), "30");
    await user.clear(within(rowB).getByRole("textbox", { name: "prices.newPrice" }));
    await user.type(within(rowB).getByRole("textbox", { name: "prices.newPrice" }), "18");

    listCatalogProducts.mockResolvedValue({
      items: [
        { ...PRODUCT_A, sellingPrice: 30, updatedAtUtc: "2026-01-05T00:00:00Z" },
        PRODUCT_B,
      ],
      totalCount: 2,
    });

    await user.click(within(rowA).getByTestId(`price-save-${PRODUCT_A.productId}`));

    await waitFor(() => {
      expect(within(rowA).queryByTestId(`price-save-${PRODUCT_A.productId}`)).not.toBeInTheDocument();
    });
    expect(within(rowB).getByRole("textbox", { name: "prices.newPrice" })).toHaveValue("18");
    expect(within(rowB).getByTestId(`price-save-${PRODUCT_B.productId}`)).toBeInTheDocument();
  });
});

describe("TodaysPricesPage governance", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    pricesSessionGrant.organizationManagementAuthority = false;
    pricesSessionGrant.membershipRole = "Member";
    listCatalogProducts.mockResolvedValue({ items: [STANDARD_PRODUCT, PRODUCT_A], totalCount: 2 });
  });

  it("makes Standard products read-only for non-govern actors", async () => {
    const user = userEvent.setup();
    renderPage();
    const standardRow = await screen.findByTestId(`price-row-${STANDARD_PRODUCT.productId}`);
    expect(within(standardRow).getByTestId(`price-managed-${STANDARD_PRODUCT.productId}`)).toHaveTextContent(
      "prices.managedByOrganization",
    );
    expect(within(standardRow).getByTestId(`price-scope-${STANDARD_PRODUCT.productId}`)).toHaveTextContent(
      "catalog.governance.organizationProduct",
    );
    const input = within(standardRow).getByRole("textbox", { name: "prices.organizationPrice" });
    expect(input).toBeDisabled();
    fireEvent.keyDown(input, { key: "Enter", code: "Enter" });
    expect(updateCatalogProductPrices).not.toHaveBeenCalled();

    const localRow = screen.getByTestId(`price-row-${PRODUCT_A.productId}`);
    await user.clear(within(localRow).getByRole("textbox", { name: "prices.newPrice" }));
    await user.type(within(localRow).getByRole("textbox", { name: "prices.newPrice" }), "31");
    expect(within(localRow).getByTestId(`price-save-${PRODUCT_A.productId}`)).toBeInTheDocument();
  });
});
