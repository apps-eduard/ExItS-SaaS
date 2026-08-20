import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { RouterProvider, createMemoryRouter } from "react-router-dom";
import { setPosSessionGrant } from "@/api/platform/pos-session-grant";
import {
  filterMockProducts,
  mockCatalogCategories,
  MOCK_COKE_PRODUCT_ID,
  MOCK_DRINKS_CATEGORY_ID,
} from "@/test/mock-pos-catalog";
import { AppProviders } from "@/app/providers";
import { appRoutes } from "@/app/router";

const orgId = "11111111-1111-1111-1111-111111111111";
const branchId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";

function mockBoundCashierApis() {
  return vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const method = init?.method ?? "GET";

    if (url.includes("/pos-api/")) {
      if (url.includes("/catalog/categories")) {
        return {
          ok: true,
          status: 200,
          json: async () => mockCatalogCategories,
          text: async () => "",
        } as Response;
      }

      if (url.includes("/catalog/products")) {
        return {
          ok: true,
          status: 200,
          json: async () => filterMockProducts(url),
          text: async () => "",
        } as Response;
      }

      return {
        ok: false,
        status: 404,
        json: async () => ({ detail: "not mocked" }),
        text: async () => "",
      } as Response;
    }

    if (url.includes("/api/v1/platform/auth/me")) {
      return {
        ok: true,
        status: 200,
        json: async () => ({
          sessionId: "11111111-1111-1111-1111-111111111111",
          username: "cashier",
          displayName: "Cashier One",
          selectedOrganizationId: orgId,
        }),
        text: async () => "",
      } as Response;
    }

    if (url.includes("/api/v1/platform/auth/organizations") && method === "GET") {
      return {
        ok: true,
        status: 200,
        json: async () => [
          {
            organizationId: orgId,
            displayName: "Kizy Store",
            slug: "kizy-store",
          },
        ],
        text: async () => "",
      } as Response;
    }

    if (url.includes(`/organizations/${orgId}/branches`) && method === "GET") {
      return {
        ok: true,
        status: 200,
        json: async () => [
          {
            id: branchId,
            organizationId: orgId,
            code: "MAIN",
            name: "Main Branch",
            isPrimary: true,
            status: "Active",
          },
        ],
        text: async () => "",
      } as Response;
    }

    if (url.includes("/api/v1/platform/antiforgery/token")) {
      return {
        ok: true,
        status: 200,
        json: async () => ({ headerName: "X-XSRF-TOKEN", token: "csrf-token" }),
        text: async () => "",
      } as Response;
    }

    return {
      ok: false,
      status: 404,
      json: async () => ({ detail: "not mocked" }),
      text: async () => "",
    } as Response;
  });
}

describe("SellFloorPage", () => {
  beforeEach(() => {
    setPosSessionGrant({
      accessToken: "in-memory-only",
      productAccessAllowed: true,
      mappedPosRoleCode: "Cashier",
      productLocalRoleCode: "Cashier",
    });
    vi.stubGlobal("fetch", mockBoundCashierApis());
  });

  it("renders sell-floor regions with disabled pay and catalog products", async () => {
    const memoryRouter = createMemoryRouter(appRoutes, { initialEntries: ["/sell"] });
    render(
      <AppProviders>
        <RouterProvider router={memoryRouter} />
      </AppProviders>,
    );

    await waitFor(() => {
      expect(screen.getByTestId("sell-floor")).toBeInTheDocument();
    });

    expect(screen.getByTestId("sell-search")).toBeInTheDocument();
    expect(screen.getByTestId("sell-categories")).toBeInTheDocument();
    expect(screen.getByTestId("sell-category-active")).toHaveTextContent("All");
    expect(screen.getByTestId("sell-products")).toBeInTheDocument();
    expect(screen.getByTestId("sell-cart-landscape")).toBeInTheDocument();
    expect(screen.getByTestId("sell-cart-bar")).toBeInTheDocument();
    expect(screen.getByTestId("sell-cart-sheet")).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getByTestId(`sell-product-${MOCK_COKE_PRODUCT_ID}`)).toBeInTheDocument();
    });

    const payButtons = screen.getAllByTestId("sell-pay");
    expect(payButtons.length).toBeGreaterThan(0);
    for (const button of payButtons) {
      expect(button).toBeDisabled();
    }
  });

  it("adds to cart and keeps lines when switching category", async () => {
    const user = userEvent.setup();
    const memoryRouter = createMemoryRouter(appRoutes, { initialEntries: ["/sell"] });
    render(
      <AppProviders>
        <RouterProvider router={memoryRouter} />
      </AppProviders>,
    );

    await waitFor(() => {
      expect(screen.getByTestId(`sell-product-${MOCK_COKE_PRODUCT_ID}`)).toBeInTheDocument();
    });

    await user.click(screen.getByTestId(`sell-product-${MOCK_COKE_PRODUCT_ID}`));

    const landscapeCart = screen.getByTestId("sell-cart-landscape");
    await waitFor(() => {
      expect(
        within(landscapeCart).getByTestId(`sell-cart-line-${MOCK_COKE_PRODUCT_ID}`),
      ).toBeInTheDocument();
    });

    await user.click(screen.getByTestId(`sell-category-${MOCK_DRINKS_CATEGORY_ID}`));

    expect(
      within(landscapeCart).getByTestId(`sell-cart-line-${MOCK_COKE_PRODUCT_ID}`),
    ).toBeInTheDocument();
    expect(within(landscapeCart).getByTestId("sell-cart-subtotal")).toHaveTextContent("25");
  });
});
