import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { RouterProvider, createMemoryRouter } from "react-router-dom";
import {
  filterMockProducts,
  mockCatalogCategories,
  MOCK_COKE_PRODUCT_ID,
  MOCK_DRINKS_CATEGORY_ID,
  MOCK_MEAT_PRODUCT_ID,
  MOCK_OOS_PRODUCT_ID,
  MOCK_RICE_PRODUCT_ID,
  MOCK_RICE_SACK_UNIT_ID,
} from "@/test/mock-pos-catalog";
import { AppProviders } from "@/app/providers";
import { appRoutes } from "@/app/router";
import {
  createOrganizationSellReadyFetch,
  createPersonalPlatformFetch,
  seedOrganizationSellReadyLocalState,
} from "@/test/session-context";

/** Sell Floor landscape cart (and sell-pay) mounts at min-width 900px. */
function stubViewport(minWidthPx: number) {
  Object.defineProperty(window, "matchMedia", {
    writable: true,
    configurable: true,
    value: vi.fn().mockImplementation((query: string) => {
      const match = String(query).match(/min-width:\s*(\d+)px/);
      const required = match ? Number(match[1]) : 0;
      return {
        matches: minWidthPx >= required,
        media: query,
        onchange: null,
        addListener: vi.fn(),
        removeListener: vi.fn(),
        addEventListener: vi.fn(),
        removeEventListener: vi.fn(),
        dispatchEvent: vi.fn(),
      };
    }),
  });
}

function renderSellFloor(options: { viewportMinWidth?: number } = {}) {
  seedOrganizationSellReadyLocalState({ role: "Cashier" });
  stubViewport(options.viewportMinWidth ?? 1024);
  vi.stubGlobal(
    "fetch",
    createOrganizationSellReadyFetch({
      role: "Cashier",
      catalogCategories: mockCatalogCategories,
      catalogProducts: filterMockProducts,
    }),
  );
  const memoryRouter = createMemoryRouter(appRoutes, { initialEntries: ["/sell"] });
  return render(
    <AppProviders>
      <RouterProvider router={memoryRouter} />
    </AppProviders>,
  );
}

describe("SellFloorPage", () => {
  beforeEach(() => {
    seedOrganizationSellReadyLocalState({ role: "Cashier" });
    stubViewport(1024);
  });

  it("renders sell-floor regions with disabled pay and catalog products", async () => {
    renderSellFloor();

    await waitFor(() => {
      expect(screen.getByTestId("sell-floor")).toBeInTheDocument();
    });

    expect(screen.getByTestId("sell-search")).toBeInTheDocument();
    expect(screen.getByTestId("sell-categories")).toBeInTheDocument();
    expect(screen.getByTestId("sell-category-active")).toHaveTextContent("All");
    expect(screen.getByTestId("sell-products")).toBeInTheDocument();
    expect(screen.getByTestId("sell-cart-landscape")).toBeInTheDocument();
    expect(screen.queryByTestId("sell-cart-bar")).not.toBeInTheDocument();
    expect(screen.queryByTestId("sell-cart-sheet")).not.toBeInTheDocument();
    expect(screen.queryByTestId("checkout-readiness")).not.toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getByTestId(`sell-product-${MOCK_COKE_PRODUCT_ID}`)).toBeInTheDocument();
    });

    const payButtons = screen.getAllByTestId("sell-pay");
    expect(payButtons.length).toBeGreaterThan(0);
    for (const button of payButtons) {
      expect(button).toBeDisabled();
    }
  });

  it("shows floating cart bar after adding a line", async () => {
    const user = userEvent.setup();
    renderSellFloor({ viewportMinWidth: 390 });

    await waitFor(() => {
      expect(screen.getByTestId(`sell-product-${MOCK_COKE_PRODUCT_ID}`)).toBeInTheDocument();
    });

    await user.click(screen.getByTestId(`sell-product-${MOCK_COKE_PRODUCT_ID}`));
    await waitFor(() => {
      expect(screen.getByTestId("sell-cart-bar")).toBeInTheDocument();
    });
  });

  it("adds to cart and keeps lines when switching category", async () => {
    const user = userEvent.setup();
    renderSellFloor();

    await waitFor(() => {
      expect(screen.getByTestId(`sell-product-${MOCK_COKE_PRODUCT_ID}`)).toBeInTheDocument();
    });

    await user.click(screen.getByTestId(`sell-product-${MOCK_COKE_PRODUCT_ID}`));

    const landscapeCart = screen.getByTestId("sell-cart-landscape");
    await waitFor(() => {
      expect(
        within(landscapeCart).getByTestId(`sell-cart-line-${MOCK_COKE_PRODUCT_ID}::base`),
      ).toBeInTheDocument();
    });

    await user.click(screen.getByTestId(`sell-category-${MOCK_DRINKS_CATEGORY_ID}`));

    expect(
      within(landscapeCart).getByTestId(`sell-cart-line-${MOCK_COKE_PRODUCT_ID}::base`),
    ).toBeInTheDocument();
    expect(within(landscapeCart).getByTestId("sell-cart-subtotal")).toHaveTextContent("25");
  });

  it("opens sell-unit picker for multi-UOM products and adds sack line", async () => {
    const user = userEvent.setup();
    renderSellFloor();

    await waitFor(() => {
      expect(screen.getByTestId(`sell-product-${MOCK_RICE_PRODUCT_ID}`)).toBeInTheDocument();
    });

    await user.click(screen.getByTestId(`sell-product-${MOCK_RICE_PRODUCT_ID}`));
    expect(screen.getByTestId("sell-unit-entry")).toBeInTheDocument();
    await user.click(screen.getByTestId(`sell-unit-option-${MOCK_RICE_SACK_UNIT_ID}`));
    await user.click(screen.getByTestId("sell-unit-add"));

    const landscapeCart = screen.getByTestId("sell-cart-landscape");
    await waitFor(() => {
      expect(
        within(landscapeCart).getByTestId(
          `sell-cart-line-${MOCK_RICE_PRODUCT_ID}::${MOCK_RICE_SACK_UNIT_ID}`,
        ),
      ).toBeInTheDocument();
    });
    expect(within(landscapeCart).getByTestId("sell-cart-subtotal")).toHaveTextContent("2,600");
  });

  it("opens weight entry for ByWeight products and clears cart with confirmation", async () => {
    const user = userEvent.setup();
    renderSellFloor();

    await waitFor(() => {
      expect(screen.getByTestId(`sell-product-${MOCK_MEAT_PRODUCT_ID}`)).toBeInTheDocument();
    });

    await user.click(screen.getByTestId(`sell-product-${MOCK_MEAT_PRODUCT_ID}`));
    expect(screen.getByTestId("sell-weight-entry")).toBeInTheDocument();
    await user.clear(screen.getByTestId("sell-weight-input"));
    await user.type(screen.getByTestId("sell-weight-input"), "2");
    await user.click(screen.getByTestId("sell-weight-confirm"));

    const landscapeCart = screen.getByTestId("sell-cart-landscape");
    await waitFor(() => {
      expect(
        within(landscapeCart).getByTestId(`sell-cart-line-${MOCK_MEAT_PRODUCT_ID}::base`),
      ).toBeInTheDocument();
    });
    expect(within(landscapeCart).getByTestId("sell-cart-subtotal")).toHaveTextContent("120");

    await user.click(within(landscapeCart).getByTestId("sell-cart-clear"));
    await user.click(
      within(screen.getByTestId("sell-cart-clear-confirm")).getByRole("button", {
        name: "Clear cart",
      }),
    );
    await waitFor(() => {
      expect(
        within(landscapeCart).queryByTestId(`sell-cart-line-${MOCK_MEAT_PRODUCT_ID}::base`),
      ).not.toBeInTheDocument();
    });
  });

  it("shows New Sale heading and toggles info panel", async () => {
    const user = userEvent.setup();
    renderSellFloor();

    await waitFor(() => {
      expect(screen.getByTestId("sell-floor")).toBeInTheDocument();
    });

    expect(screen.getByRole("heading", { name: "New Sale" })).toBeInTheDocument();
    expect(screen.getByTestId("sell-info-toggle")).toHaveTextContent("Info");
    expect(screen.getByTestId("sell-out-of-stock-toggle")).toHaveTextContent("Out of stock");
    expect(screen.queryByTestId("sell-info-panel")).not.toBeInTheDocument();

    const toggle = screen.getByTestId("sell-info-toggle");
    expect(toggle).toHaveAttribute("aria-expanded", "false");
    await user.click(toggle);
    expect(toggle).toHaveAttribute("aria-expanded", "true");
    expect(screen.getByTestId("sell-info-panel")).toBeInTheDocument();
    expect(
      screen.getByText("Search or select products to start a sale."),
    ).toBeInTheDocument();
    expect(screen.getByText("Open a shift before checkout.")).toBeInTheDocument();
    expect(
      screen.getByText(
        "Quantities in this cart are not reserved. Other registers still see committed on-hand until checkout.",
      ),
    ).toBeInTheDocument();

    await user.click(screen.getByTestId("sell-info-close"));
    expect(toggle).toHaveAttribute("aria-expanded", "false");
    expect(screen.queryByTestId("sell-info-panel")).not.toBeInTheDocument();
  });

  it("hides out-of-stock products until the cashier shows them", async () => {
    const user = userEvent.setup();
    renderSellFloor();

    await waitFor(() => {
      expect(screen.getByTestId(`sell-product-${MOCK_COKE_PRODUCT_ID}`)).toBeInTheDocument();
    });
    expect(screen.queryByTestId(`sell-product-${MOCK_OOS_PRODUCT_ID}`)).not.toBeInTheDocument();
    expect(screen.getByTestId(`sell-product-stock-${MOCK_COKE_PRODUCT_ID}`)).toHaveTextContent(
      "48 Bottle available",
    );

    const oosToggle = screen.getByTestId("sell-out-of-stock-toggle");
    expect(oosToggle).toHaveTextContent("Out of stock");
    expect(oosToggle).toHaveAttribute("aria-pressed", "false");
    await user.click(oosToggle);
    expect(oosToggle).toHaveAttribute("aria-pressed", "true");
    expect(screen.queryByTestId(`sell-product-${MOCK_COKE_PRODUCT_ID}`)).not.toBeInTheDocument();

    const oosCard = screen.getByTestId(`sell-product-${MOCK_OOS_PRODUCT_ID}`);
    expect(oosCard).toBeDisabled();
    expect(screen.getByTestId(`sell-product-stock-${MOCK_OOS_PRODUCT_ID}`)).toHaveTextContent(
      "Out of stock",
    );

    await user.click(oosCard);
    expect(screen.queryByTestId(`sell-cart-line-${MOCK_OOS_PRODUCT_ID}::base`)).not.toBeInTheDocument();

    await user.click(screen.getByTestId("sell-category-all"));
    expect(oosToggle).toHaveAttribute("aria-pressed", "false");
    expect(screen.getByTestId(`sell-product-${MOCK_COKE_PRODUCT_ID}`)).toBeInTheDocument();
    expect(screen.queryByTestId(`sell-product-${MOCK_OOS_PRODUCT_ID}`)).not.toBeInTheDocument();

    await user.click(oosToggle);
    expect(screen.getByTestId(`sell-product-${MOCK_OOS_PRODUCT_ID}`)).toBeInTheDocument();
    await user.click(screen.getByTestId(`sell-category-${MOCK_DRINKS_CATEGORY_ID}`));
    expect(oosToggle).toHaveAttribute("aria-pressed", "false");
    expect(screen.getByTestId(`sell-product-${MOCK_COKE_PRODUCT_ID}`)).toBeInTheDocument();
    expect(screen.queryByTestId(`sell-product-${MOCK_OOS_PRODUCT_ID}`)).not.toBeInTheDocument();
  });

  it("reduces displayed on-hand when this register adds to cart and restores it on remove", async () => {
    const user = userEvent.setup();
    renderSellFloor();

    await waitFor(() => {
      expect(screen.getByTestId(`sell-product-${MOCK_COKE_PRODUCT_ID}`)).toBeInTheDocument();
    });
    expect(screen.getByTestId(`sell-product-stock-${MOCK_COKE_PRODUCT_ID}`)).toHaveTextContent(
      "48 Bottle available",
    );

    await user.click(screen.getByTestId(`sell-product-${MOCK_COKE_PRODUCT_ID}`));
    const landscapeCart = screen.getByTestId("sell-cart-landscape");
    await waitFor(() => {
      expect(
        within(landscapeCart).getByTestId(`sell-cart-line-${MOCK_COKE_PRODUCT_ID}::base`),
      ).toBeInTheDocument();
    });
    expect(screen.getByTestId(`sell-product-stock-${MOCK_COKE_PRODUCT_ID}`)).toHaveTextContent(
      "47 Bottle available",
    );

    await user.click(
      within(landscapeCart).getByTestId(`sell-cart-remove-${MOCK_COKE_PRODUCT_ID}::base`),
    );
    await waitFor(() => {
      expect(
        within(landscapeCart).queryByTestId(`sell-cart-line-${MOCK_COKE_PRODUCT_ID}::base`),
      ).not.toBeInTheDocument();
    });
    expect(screen.getByTestId(`sell-product-stock-${MOCK_COKE_PRODUCT_ID}`)).toHaveTextContent(
      "48 Bottle available",
    );
  });

  it("blocks weight that exceeds available stock", async () => {
    const user = userEvent.setup();
    renderSellFloor();

    await waitFor(() => {
      expect(screen.getByTestId(`sell-product-${MOCK_MEAT_PRODUCT_ID}`)).toBeInTheDocument();
    });

    await user.click(screen.getByTestId(`sell-product-${MOCK_MEAT_PRODUCT_ID}`));
    await user.clear(screen.getByTestId("sell-weight-input"));
    await user.type(screen.getByTestId("sell-weight-input"), "20");
    await user.click(screen.getByTestId("sell-weight-confirm"));

    await waitFor(() => {
      expect(screen.getByTestId("sell-stock-error")).toHaveTextContent(
        "Only 12.50 kg available.",
      );
    });
    expect(
      screen.queryByTestId(`sell-cart-line-${MOCK_MEAT_PRODUCT_ID}::base`),
    ).not.toBeInTheDocument();
  });
});

describe("SellFloorPage account-class gate", () => {
  it("rejects Personal session on Organization Sell route", async () => {
    vi.stubGlobal("fetch", createPersonalPlatformFetch());
    const memoryRouter = createMemoryRouter(appRoutes, { initialEntries: ["/sell"] });
    render(
      <AppProviders>
        <RouterProvider router={memoryRouter} />
      </AppProviders>,
    );

    await waitFor(() => {
      expect(screen.getByTestId("account-class-denied")).toBeInTheDocument();
    });
    expect(screen.queryByTestId("sell-floor")).not.toBeInTheDocument();
  });

  it("allows Organization sell-ready session onto Sell floor", async () => {
    renderSellFloor();
    await waitFor(() => {
      expect(screen.getByTestId("sell-floor")).toBeInTheDocument();
    });
    expect(screen.queryByTestId("account-class-denied")).not.toBeInTheDocument();
  });
});
