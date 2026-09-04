import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import { RouterProvider, createMemoryRouter } from "react-router-dom";
import {
  filterMockProducts,
  mockCatalogCategories,
} from "@/test/mock-pos-catalog";
import { AppProviders } from "@/app/providers";
import { appRoutes } from "@/app/router";
import {
  createOrganizationSellReadyFetch,
  seedOrganizationSellReadyLocalState,
} from "@/test/session-context";

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

function renderSellFloor(viewportMinWidth = 1366) {
  seedOrganizationSellReadyLocalState({ role: "Cashier" });
  stubViewport(viewportMinWidth);
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

describe("POS-SELL-INDEPENDENT-SCROLL-LAYOUT", () => {
  beforeEach(() => {
    seedOrganizationSellReadyLocalState({ role: "Cashier" });
    stubViewport(1366);
  });

  it("Sell floor root is contained; product scroll excludes search/categories; cart header/footer stay outside lines", async () => {
    renderSellFloor(1366);

    await waitFor(() => {
      expect(screen.getByTestId("sell-floor")).toBeInTheDocument();
    });

    const root = screen.getByTestId("sell-floor");
    expect(root.className).toMatch(/sell-floor-root/);
    expect(root.className).toMatch(/min-h-0/);

    const browse = screen.getByTestId("sell-floor-browse");
    expect(browse.className).toMatch(/min-h-0/);

    const search = screen.getByTestId("sell-search");
    const categories = screen.getByTestId("sell-categories");
    const products = screen.getByTestId("sell-products");

    expect(browse.contains(search)).toBe(true);
    expect(browse.contains(categories)).toBe(true);
    expect(browse.contains(products)).toBe(true);
    expect(products.contains(search)).toBe(false);
    expect(products.contains(categories)).toBe(false);
    expect(products.className).toMatch(/overflow-y-auto/);
    expect(products.className).toMatch(/min-h-0/);

    const cartAside = screen.getByTestId("sell-cart-landscape");
    expect(cartAside.className).toMatch(/min-h-0/);
    expect(cartAside.className).toMatch(/overflow-hidden/);

    await waitFor(() => {
      expect(screen.getByTestId("sell-cart-header")).toBeInTheDocument();
    });
    expect(screen.getByTestId("sell-cart-footer")).toBeInTheDocument();
    expect(screen.queryByTestId("sell-cart-lines")).not.toBeInTheDocument();

    expect(cartAside.contains(screen.getByTestId("sell-cart-header"))).toBe(true);
    expect(cartAside.contains(screen.getByTestId("sell-cart-footer"))).toBe(true);
    expect(document.body.style.overflow).not.toBe("hidden");
  });

  it("mobile sell keeps product pane scrollable with search/categories outside the product scroll region", async () => {
    renderSellFloor(390);

    await waitFor(() => {
      expect(screen.getByTestId("sell-floor")).toBeInTheDocument();
    });

    const products = screen.getByTestId("sell-products");
    expect(products.className).toMatch(/overflow-y-auto/);
    expect(products.contains(screen.getByTestId("sell-search"))).toBe(false);
    expect(products.contains(screen.getByTestId("sell-categories"))).toBe(false);
    expect(screen.getByTestId("sell-cart-landscape").className).toMatch(/hidden/);
  });
});
