import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import { RouterProvider, createMemoryRouter } from "react-router-dom";
import { setPosSessionGrant } from "@/api/platform/pos-session-grant";
import { AppProviders } from "@/app/providers";
import { appRoutes } from "@/app/router";

const orgId = "11111111-1111-1111-1111-111111111111";
const branchId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";

function mockBoundCashierPlatformApi() {
  return vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const method = init?.method ?? "GET";

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
    vi.stubGlobal("fetch", mockBoundCashierPlatformApi());
  });

  it("renders sell-floor regions with disabled pay and search field", async () => {
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
    expect(screen.getByTestId("sell-products")).toBeInTheDocument();
    expect(screen.getByTestId("sell-cart-landscape")).toBeInTheDocument();
    expect(screen.getByTestId("sell-cart-bar")).toBeInTheDocument();
    expect(screen.getByTestId("sell-cart-sheet")).toBeInTheDocument();

    const payButtons = screen.getAllByTestId("sell-pay");
    expect(payButtons.length).toBeGreaterThan(0);
    for (const button of payButtons) {
      expect(button).toBeDisabled();
    }
  });
});
