import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { RouterProvider, createMemoryRouter } from "react-router-dom";
import {
  filterMockProducts,
  mockCatalogCategories,
  MOCK_COKE_PRODUCT_ID,
} from "@/test/mock-pos-catalog";
import { AppProviders } from "@/app/providers";
import { appRoutes } from "@/app/router";
import {
  createOrganizationSellReadyFetch,
  seedOrganizationSellReadyLocalState,
} from "@/test/session-context";
import {
  getOrgBottomNavHidden,
  setOrgBottomNavHidden,
} from "@/features/sell/sell-org-bottom-nav-chrome";

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

function renderMobileSell() {
  seedOrganizationSellReadyLocalState({ role: "Cashier" });
  stubViewport(390);
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

describe("POS-SELL-MOBILE-CART-UX-IMPROVEMENT", () => {
  beforeEach(() => {
    setOrgBottomNavHidden(false);
    seedOrganizationSellReadyLocalState({ role: "Cashier" });
    stubViewport(390);
  });

  it("SELL-UX-01: mobile Sell entry does not autofocus search", async () => {
    renderMobileSell();
    await waitFor(() => {
      expect(screen.getByTestId("sell-floor")).toBeInTheDocument();
    });
    expect(screen.getByTestId("sell-search")).not.toHaveFocus();
  });

  it("SELL-UX-02/03: floating cart keeps money icon and View cart action", async () => {
    const user = userEvent.setup();
    renderMobileSell();
    await waitFor(() => {
      expect(screen.getByTestId(`sell-product-${MOCK_COKE_PRODUCT_ID}`)).toBeInTheDocument();
    });
    await user.click(screen.getByTestId(`sell-product-${MOCK_COKE_PRODUCT_ID}`));
    const bar = await screen.findByTestId("sell-cart-bar");
    expect(bar.querySelector(".sell-cart-bar__icon")).toBeTruthy();
    expect(within(bar).getByTestId("sell-cart-bar-view")).toHaveTextContent(/View/i);
  });

  it("SELL-UX-04/05: filled floating cart uses high-contrast filled class", async () => {
    const user = userEvent.setup();
    renderMobileSell();
    await waitFor(() => {
      expect(screen.getByTestId(`sell-product-${MOCK_COKE_PRODUCT_ID}`)).toBeInTheDocument();
    });
    await user.click(screen.getByTestId(`sell-product-${MOCK_COKE_PRODUCT_ID}`));
    const bar = await screen.findByTestId("sell-cart-bar");
    expect(bar.className).toMatch(/sell-cart-bar--filled/);
    expect(bar.querySelector(".sell-cart-bar__total")).toBeTruthy();
    expect(bar.querySelector(".sell-cart-bar__action")).toBeTruthy();
  });

  it("SELL-UX-06/07/08: bottom nav chrome hides while cart open and restores on close", async () => {
    const user = userEvent.setup();
    renderMobileSell();
    await waitFor(() => {
      expect(screen.getByTestId(`sell-product-${MOCK_COKE_PRODUCT_ID}`)).toBeInTheDocument();
    });
    expect(getOrgBottomNavHidden()).toBe(false);

    await user.click(screen.getByTestId(`sell-product-${MOCK_COKE_PRODUCT_ID}`));
    await user.click(await screen.findByTestId("sell-cart-bar"));
    await screen.findByTestId("sell-cart-sheet");
    await waitFor(() => expect(getOrgBottomNavHidden()).toBe(true));

    await user.click(screen.getByTestId("sell-cart-sheet-close"));
    await waitFor(() => expect(getOrgBottomNavHidden()).toBe(false));
    expect(screen.queryByTestId("sell-cart-sheet")).not.toBeInTheDocument();
  });

  it("SELL-UX-09/10: sticky footer shows total and payment CTA", async () => {
    const user = userEvent.setup();
    renderMobileSell();
    await waitFor(() => {
      expect(screen.getByTestId(`sell-product-${MOCK_COKE_PRODUCT_ID}`)).toBeInTheDocument();
    });
    await user.click(screen.getByTestId(`sell-product-${MOCK_COKE_PRODUCT_ID}`));
    await user.click(await screen.findByTestId("sell-cart-bar"));
    const sheet = await screen.findByTestId("sell-cart-sheet");
    expect(within(sheet).getByTestId("sell-cart-footer-total")).toBeInTheDocument();
    expect(within(sheet).getByTestId("sell-cart-header-subtotal")).toBeInTheDocument();
    expect(within(sheet).getByTestId("sell-pay")).toBeInTheDocument();
  });

  it("SELL-UX-11/12: cart line and payment control remain available", async () => {
    const user = userEvent.setup();
    renderMobileSell();
    await waitFor(() => {
      expect(screen.getByTestId(`sell-product-${MOCK_COKE_PRODUCT_ID}`)).toBeInTheDocument();
    });
    await user.click(screen.getByTestId(`sell-product-${MOCK_COKE_PRODUCT_ID}`));
    await user.click(await screen.findByTestId("sell-cart-bar"));
    const sheet = await screen.findByTestId("sell-cart-sheet");
    expect(
      within(sheet).getByTestId(`sell-cart-line-${MOCK_COKE_PRODUCT_ID}::base`),
    ).toBeInTheDocument();
    expect(within(sheet).getByTestId("sell-pay")).toBeInTheDocument();
  });
});
