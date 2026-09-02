import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { BranchProductPricingPanel } from "@/features/catalog/BranchProductPricingPanel";
import { ToastProvider } from "@/components/exits/ToastProvider";

vi.mock("@/i18n/I18nProvider", () => ({
  useI18n: () => ({
    t: (key: string) =>
      ({
        "catalog.branchPricing.title": "Branch pricing",
        "catalog.branchPricing.hint": "Hint for {branch}",
        "catalog.branchPricing.basePrice": "Base unit price",
        "catalog.branchPricing.unitPrice": "Unit: {name}",
        "catalog.branchPricing.organizationDefault": "Organization default price",
        "catalog.branchPricing.inheritedByBranches": "Inherited by branches without a custom price.",
        "catalog.branchPricing.branchSellingPrice": "{branch} selling price",
        "catalog.branchPricing.useOrganizationDefaultMode": "Use organization default",
        "catalog.branchPricing.customBranchPriceMode": "Custom branch price",
        "catalog.branchPricing.inheritMode": "Uses organization default",
        "catalog.branchPricing.useOrganizationDefault": "Use organization default",
        "catalog.branchPricing.customPriceInput": "Custom branch price",
        "catalog.branchPricing.effectivePrice": "Effective price",
        "catalog.branchPricing.saveCustom": "Save branch price",
        "catalog.branchPricing.saving": "Saving…",
        "catalog.branchPricing.removing": "Removing…",
        "catalog.branchPricing.saved": "Saved",
        "catalog.branchPricing.removed": "Removed",
        "catalog.invalidPrice": "Invalid price",
        "loading.label": "Loading…",
      })[key] ?? key,
  }),
}));

const getBranchProductPricing = vi.fn();
const setBranchProductPriceOverride = vi.fn();
const removeBranchProductPriceOverride = vi.fn();
const updateCatalogProduct = vi.fn();

vi.mock("@/api/pos/pos-catalog-client", () => ({
  getBranchProductPricing: (...args: unknown[]) => getBranchProductPricing(...args),
  setBranchProductPriceOverride: (...args: unknown[]) => setBranchProductPriceOverride(...args),
  removeBranchProductPriceOverride: (...args: unknown[]) =>
    removeBranchProductPriceOverride(...args),
  updateCatalogProduct: (...args: unknown[]) => updateCatalogProduct(...args),
}));

const workspace = {
  organizationId: "11111111-1111-1111-1111-111111111111",
  branchId: "22222222-2222-2222-2222-222222222222",
};

const orgStandardProduct = {
  scope: "OrganizationStandard",
  units: [
    {
      unitId: "u1",
      productId: "prod-1",
      displayName: "Box",
      shortLabel: "Box",
      kind: "Sell",
      multiplierToBase: 1,
      allowsCustomQuantity: false,
      isActive: true,
      sortOrder: 0,
    },
  ],
};

function renderPanel(canGovern = true) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={client}>
      <ToastProvider>
        <BranchProductPricingPanel
          workspace={workspace}
          productId="prod-1"
          product={orgStandardProduct}
          canGovern={canGovern}
          branchName="Branch A"
        />
      </ToastProvider>
    </QueryClientProvider>,
  );
}

describe("BranchProductPricingPanel", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    getBranchProductPricing.mockResolvedValue({
      productId: "prod-1",
      branchId: workspace.branchId,
      basePrice: {
        organizationDefaultPrice: 100,
        branchOverridePrice: null,
        effectivePrice: 100,
        hasBranchPriceOverride: false,
      },
      unitPrices: [
        {
          productUnitId: "u1",
          organizationDefaultPrice: 1000,
          branchOverridePrice: 120,
          effectivePrice: 120,
          hasBranchPriceOverride: true,
        },
      ],
    });
    setBranchProductPriceOverride.mockResolvedValue(undefined);
    removeBranchProductPriceOverride.mockResolvedValue(undefined);
  });

  it("BRPRICE-UX-01 displays organization default separately from branch price", async () => {
    renderPanel();
    expect(await screen.findByTestId("base-organization-default")).toHaveTextContent("₱100.00");
    expect(screen.getByTestId("base-effective-price")).toHaveTextContent("₱100.00");
    expect(screen.getByTestId("u1-organization-default")).toHaveTextContent("₱1,000.00");
    expect(screen.getByTestId("u1-effective-price")).toHaveTextContent("₱120.00");
  });

  it("BRPRICE-UX-02 saves custom branch price via setBranchProductPriceOverride", async () => {
    const user = userEvent.setup();
    renderPanel();
    await screen.findByTestId("branch-pricing-base");
    await user.click(screen.getByTestId("base-mode-custom"));
    await user.clear(screen.getByTestId("base-custom-price-input"));
    await user.type(screen.getByTestId("base-custom-price-input"), "120");
    await user.click(screen.getByTestId("base-save-override"));
    await waitFor(() => {
      expect(setBranchProductPriceOverride).toHaveBeenCalledWith(
        workspace,
        "prod-1",
        expect.objectContaining({
          branchId: workspace.branchId,
          sellingPrice: 120,
          productUnitId: null,
        }),
      );
    });
    expect(updateCatalogProduct).not.toHaveBeenCalled();
  });

  it("BRPRICE-UX-03 does not call canonical product update for branch custom price", async () => {
    const user = userEvent.setup();
    renderPanel();
    await screen.findByTestId("branch-pricing-base");
    await user.click(screen.getByTestId("base-mode-custom"));
    await user.clear(screen.getByTestId("base-custom-price-input"));
    await user.type(screen.getByTestId("base-custom-price-input"), "125");
    await user.click(screen.getByTestId("base-save-override"));
    await waitFor(() => expect(setBranchProductPriceOverride).toHaveBeenCalled());
    expect(updateCatalogProduct).not.toHaveBeenCalled();
  });

  it("BRPRICE-UX-04 removes override via removeBranchProductPriceOverride", async () => {
    const user = userEvent.setup();
    renderPanel();
    await screen.findByTestId("branch-pricing-unit-u1");
    await user.click(screen.getByTestId("u1-use-organization-default"));
    await waitFor(() => {
      expect(removeBranchProductPriceOverride).toHaveBeenCalledWith(
        workspace,
        "prod-1",
        workspace.branchId,
        "u1",
      );
    });
  });

  it("BRPRICE-UX-13 hides panel for unauthorized users", () => {
    renderPanel(false);
    expect(screen.queryByTestId("catalog-branch-pricing")).not.toBeInTheDocument();
    expect(setBranchProductPriceOverride).not.toHaveBeenCalled();
  });

  it("BRPRICE-UX-09 hides panel for branch-local products", () => {
    const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    render(
      <QueryClientProvider client={client}>
        <ToastProvider>
          <BranchProductPricingPanel
            workspace={workspace}
            productId="prod-1"
            product={{ scope: "BranchLocal", units: [] }}
            canGovern
            branchName="Branch A"
          />
        </ToastProvider>
      </QueryClientProvider>,
    );
    expect(screen.queryByTestId("catalog-branch-pricing")).not.toBeInTheDocument();
  });
});

describe("resolveSellUnitPrice on sell floor", () => {
  it("BRPRICE-UX-11 uses effective price for display", async () => {
    const { resolveSellUnitPrice } = await import("@/cart/sell-cart-helpers");
    expect(
      resolveSellUnitPrice(
        {
          productId: "p1",
          organizationId: "o1",
          name: "Coke",
          unitOfMeasure: "Piece",
          sellingMode: "PerItem",
          sellingPrice: 100,
          effectiveSellingPrice: 120,
          status: "Active",
          createdAtUtc: "",
          updatedAtUtc: "",
        },
        null,
      ),
    ).toBe(120);
  });
});
