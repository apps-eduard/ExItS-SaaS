import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { BranchProductPricingPanel } from "@/features/catalog/BranchProductPricingPanel";
import { ToastProvider } from "@/components/exits/ToastProvider";
import { readFileSync } from "node:fs";
import { resolve } from "node:path";

vi.mock("@/i18n/I18nProvider", () => ({
  useI18n: () => ({
    t: (key: string) =>
      ({
        "catalog.sellingPrice.title": "Selling price",
        "catalog.sellingPrice.hint":
          "Manage the organization default and this branch's selling price.",
        "catalog.branchPricing.title": "{branch} price",
        "catalog.branchPricing.hint":
          "Use the organization default price or set a custom selling price for {branch}.",
        "catalog.branchPricing.basePrice": "Base unit price",
        "catalog.branchPricing.unitPrice": "Unit: {name}",
        "catalog.branchPricing.organizationDefault": "Organization default",
        "catalog.branchPricing.inheritedByBranches":
          "Used by branches without their own custom price.",
        "catalog.branchPricing.branchSellingPrice": "{branch} selling price",
        "catalog.branchPricing.priceSource": "Price source",
        "catalog.branchPricing.useOrganizationDefaultMode": "Organization default",
        "catalog.branchPricing.customBranchPriceMode": "Custom price",
        "catalog.branchPricing.inheritMode": "Uses organization default",
        "catalog.branchPricing.useOrganizationDefault": "Use organization default",
        "catalog.branchPricing.customPriceInput": "Custom selling price",
        "catalog.branchPricing.effectivePrice": "Effective price",
        "catalog.branchPricing.saveCustom": "Save branch price",
        "catalog.branchPricing.saving": "Saving…",
        "catalog.branchPricing.removing": "Removing…",
        "catalog.branchPricing.saved": "Saved",
        "catalog.branchPricing.removed": "Removed",
        "catalog.organizationPricing.defaultPrice": "Default selling price",
        "catalog.organizationPricing.hint":
          "Used by branches without their own custom price.",
        "catalog.organizationPricing.changeWarning":
          "Organization default. Branches without a custom price will use this price.",
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

const globalsCss = readFileSync(
  resolve(__dirname, "../../styles/globals.css"),
  "utf8",
);

function renderPanel(
  canGovern = true,
  branchName = "Main Branch",
  organizationEditor?: {
    value: string;
    onChange: (value: string) => void;
    warning?: string | null;
  } | null,
) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={client}>
      <ToastProvider>
        <BranchProductPricingPanel
          workspace={workspace}
          productId="prod-1"
          product={orgStandardProduct}
          canGovern={canGovern}
          branchName={branchName}
          organizationEditor={organizationEditor}
        />
      </ToastProvider>
    </QueryClientProvider>,
  );
}

describe("BranchProductPricingPanel / Selling price card", () => {
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

  it("renders one Selling price card without legacy headings", async () => {
    renderPanel(true, "Main Branch", {
      value: "100",
      onChange: () => undefined,
    });
    expect(await screen.findByTestId("catalog-selling-price")).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Selling price" })).toBeInTheDocument();
    expect(await screen.findByRole("heading", { name: "Main Branch" })).toBeInTheDocument();
    expect(screen.queryByText("Organization pricing")).not.toBeInTheDocument();
    expect(screen.queryByText("Branch pricing")).not.toBeInTheDocument();
    expect(screen.queryByText("Base unit price")).not.toBeInTheDocument();
    expect(screen.getByTestId("catalog-organization-pricing")).toBeInTheDocument();
    expect(screen.getByTestId("catalog-organization-default-price")).toBeInTheDocument();
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

  it("shows custom selling price only when Custom is selected", async () => {
    const user = userEvent.setup();
    renderPanel();
    await screen.findByTestId("branch-pricing-base");
    expect(screen.getByTestId("base-mode-inherit")).toHaveAttribute("aria-checked", "true");
    expect(screen.queryByTestId("base-custom-price-input")).not.toBeInTheDocument();
    await user.click(screen.getByTestId("base-mode-custom"));
    expect(screen.getByTestId("base-custom-price-input")).toBeInTheDocument();
    expect(screen.getByTestId("base-custom-price-input")).toHaveAttribute(
      "name",
      "branchOverride-base",
    );
  });

  it("keeps price inputs on field radius under Pill control shape", () => {
    expect(globalsCss).toMatch(
      /\[data-control-shape="pill"\][\s\S]*?--exits-field-radius:\s*var\(--exits-radius-md\)/,
    );
    expect(globalsCss).toContain(".selling-price-card__columns");
  });

  it("BRPRICE-UX-13 hides panel for unauthorized users", () => {
    renderPanel(false);
    expect(screen.queryByTestId("catalog-selling-price")).not.toBeInTheDocument();
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
    expect(screen.queryByTestId("catalog-selling-price")).not.toBeInTheDocument();
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
