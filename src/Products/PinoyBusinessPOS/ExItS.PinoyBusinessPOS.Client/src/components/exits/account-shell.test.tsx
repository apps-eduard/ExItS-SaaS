import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { RouterProvider, createMemoryRouter } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import { appRoutes } from "@/app/router";
import { setPosAccessToken } from "@/api/platform/pos-access-token";
import { setPosSessionGrant } from "@/api/platform/pos-session-grant";
import { clearPlatformAntiforgeryToken } from "@/api/platform/platform-http";

const orgId = "11111111-1111-1111-1111-111111111111";
const branchId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";

function createFetchMock(options: { longNames?: boolean; unbound?: boolean } = {}) {
  const longNames = options.longNames ?? false;
  const unbound = options.unbound ?? false;
  const orgName = longNames
    ? "Very Long Organization Name For Truncation Testing Carenderia"
    : "Kizy Store";
  const displayName = longNames ? "Olivia Extremely Long Mendoza Display" : "Olivia Mendoza";

  return vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const method = init?.method ?? "GET";

    if (url.includes("/api/v1/platform/antiforgery/token")) {
      return {
        ok: true,
        status: 200,
        json: async () => ({ headerName: "X-XSRF-TOKEN", token: "csrf-token" }),
        text: async () => "",
      } as Response;
    }

    if (url.includes("/api/v1/platform/auth/me") && method === "GET") {
      return {
        ok: true,
        status: 200,
        json: async () => ({
          sessionId: "11111111-1111-1111-1111-111111111111",
          username: "olivia",
          displayName,
          email: "olivia@example.com",
          selectedOrganizationId: unbound ? null : orgId,
          accountClass: unbound ? "Personal" : "Organization",
          homeOrganizationId: unbound ? null : orgId,
          organizationContextLocked: !unbound,
        }),
        text: async () => "",
      } as Response;
    }

    if (url.includes("/api/v1/platform/auth/organizations") && method === "GET") {
      return {
        ok: true,
        status: 200,
        json: async () =>
          unbound
            ? []
            : [
                {
                  organizationId: orgId,
                  displayName: orgName,
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
            name: longNames ? "Main Branch With A Very Long Store Title" : "Main Branch",
            isPrimary: true,
            status: "Active",
          },
        ],
        text: async () => "",
      } as Response;
    }

    if (url.includes("/api/v1/platform/auth/organization-context") && method === "PUT") {
      return { ok: true, status: 204, json: async () => null, text: async () => "" } as Response;
    }

    if (url.includes(`/organizations/${orgId}/branch-context`) && method === "PUT") {
      return { ok: true, status: 204, json: async () => null, text: async () => "" } as Response;
    }

    if (url.includes("/api/v1/platform/auth/token") && method === "POST") {
      return {
        ok: true,
        status: 200,
        json: async () => ({
          accessToken: "in-memory-only-access-token",
          productAccessAllowed: true,
          mappedPosRoleCode: "Cashier",
          productLocalRoleCode: "Cashier",
        }),
        text: async () => "",
      } as Response;
    }

    if (url.includes("/api/v1/platform/auth/logout") && method === "POST") {
      return { ok: true, status: 204, json: async () => null, text: async () => "" } as Response;
    }

    return {
      ok: false,
      status: 404,
      json: async () => ({ detail: "not mocked" }),
      text: async () => "",
    } as Response;
  });
}

function renderAt(path: string) {
  const memoryRouter = createMemoryRouter(appRoutes, { initialEntries: [path] });
  return render(
    <AppProviders>
      <RouterProvider router={memoryRouter} />
    </AppProviders>,
  );
}

describe("account shell", () => {
  beforeEach(() => {
    setPosAccessToken("in-memory-only-access-token");
    setPosSessionGrant({
      accessToken: "in-memory-only-access-token",
      productAccessAllowed: true,
      mappedPosRoleCode: "Cashier",
      productLocalRoleCode: "Cashier",
    });
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    clearPlatformAntiforgeryToken();
  });

  it("shows initials, account menu, preferences route, and workspace context", async () => {
    const user = userEvent.setup();
    vi.stubGlobal("fetch", createFetchMock());
    renderAt("/role/cashier");

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: "Cashier home" })).toBeInTheDocument();
    });

    const trigger = screen.getByTestId("account-menu-trigger");
    expect(trigger).toHaveTextContent("OM");
    expect(screen.getByTestId("workspace-context")).toHaveTextContent("Kizy Store");
    const mobileContext = screen.getByTestId("workspace-context-mobile");
    expect(mobileContext).toHaveTextContent("Kizy Store");
    expect(mobileContext).toHaveTextContent("Main Branch");
    expect(screen.queryByRole("button", { name: "Preferences" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Sign out" })).not.toBeInTheDocument();

    await user.click(trigger);
    const menu = await screen.findByRole("menu");
    expect(within(menu).getByText("Olivia Mendoza")).toBeInTheDocument();
    expect(within(menu).getByText("olivia")).toBeInTheDocument();
    expect(within(menu).getByText("Cashier")).toBeInTheDocument();

    await user.click(within(menu).getByRole("menuitem", { name: "Preferences" }));
    await waitFor(() => {
      expect(screen.getByRole("heading", { name: "Preferences" })).toBeInTheDocument();
    });
  });

  it("closes the account menu on Escape", async () => {
    const user = userEvent.setup();
    vi.stubGlobal("fetch", createFetchMock());
    renderAt("/role/cashier");

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: "Cashier home" })).toBeInTheDocument();
      expect(screen.getByTestId("account-menu-trigger")).toBeInTheDocument();
    });

    await user.click(screen.getByTestId("account-menu-trigger"));
    expect(await screen.findByRole("menu")).toBeInTheDocument();
    await user.keyboard("{Escape}");
    await waitFor(() => {
      expect(screen.queryByRole("menu")).not.toBeInTheDocument();
    });
  });

  it("omits no-workspace error copy when unbound on preferences", async () => {
    vi.stubGlobal("fetch", createFetchMock({ unbound: true }));
    renderAt("/settings/preferences");

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: "Preferences" })).toBeInTheDocument();
    });
    expect(screen.queryByText(/No workspace selected/i)).not.toBeInTheDocument();
    expect(screen.getByTestId("account-menu-trigger")).toBeInTheDocument();
  });

  it("truncates long display names in the account trigger title", async () => {
    vi.stubGlobal("fetch", createFetchMock({ longNames: true }));
    renderAt("/role/cashier");

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: "Cashier home" })).toBeInTheDocument();
      expect(screen.getByTestId("account-menu-trigger")).toBeInTheDocument();
      expect(screen.getByTestId("workspace-context")).toBeInTheDocument();
    });
    expect(screen.getByTestId("account-menu-trigger")).toHaveAttribute(
      "title",
      "Olivia Extremely Long Mendoza Display",
    );
    expect(screen.getByTestId("workspace-context")).toHaveAttribute(
      "title",
      expect.stringContaining("Very Long Organization"),
    );
  });
});
