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
const installId = "aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee";
const deviceId = "ffffffff-ffff-4fff-8fff-ffffffffffff";
const shiftId = "cccccccc-cccc-cccc-cccc-cccccccccccc";
const registerId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";

function createFetchMock(
  options: { longNames?: boolean; unbound?: boolean; personalSwitchable?: boolean } = {},
) {
  const longNames = options.longNames ?? false;
  const unbound = options.unbound ?? false;
  const personalSwitchable = options.personalSwitchable ?? false;
  const orgName = longNames
    ? "Very Long Organization Name For Truncation Testing Carenderia"
    : "Kizy Store";
  const displayName = longNames ? "Olivia Extremely Long Mendoza Display" : "Olivia Mendoza";

  return vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const method = init?.method ?? "GET";

    if (url.includes("/pos-devices/authorize") && method === "POST") {
      return {
        ok: true,
        status: 200,
        json: async () => ({
          posDeviceId: deviceId,
          branchId,
          installationDeviceId: installId,
        }),
        text: async () => "",
      } as Response;
    }

    if (url.includes("/cashier-shifts/current") && method === "GET") {
      return {
        ok: true,
        status: 200,
        json: async () => ({
          shiftId,
          organizationId: orgId,
          shiftNumber: "S-1",
          status: "Open",
          actorId: "dddddddd-dddd-dddd-dddd-dddddddddddd",
          registerId,
          registerCode: "REG-1",
          registerName: "Front",
          businessDate: "2026-08-21",
          openingCashAmount: 100,
          openingCashCounted: true,
          effectiveCashCountMode: "Required",
          openedAtUtc: "2026-08-21T01:00:00Z",
          openedBy: "dddddddd-dddd-dddd-dddd-dddddddddddd",
          createdAtUtc: "2026-08-21T01:00:00Z",
          updatedAtUtc: "2026-08-21T01:00:00Z",
        }),
        text: async () => "",
      } as Response;
    }

    if (url.includes("/catalog/categories")) {
      return {
        ok: true,
        status: 200,
        json: async () => ({ items: [], totalCount: 0, page: 1, pageSize: 50 }),
        text: async () => "",
      } as Response;
    }

    if (url.includes("/catalog/products")) {
      return {
        ok: true,
        status: 200,
        json: async () => ({ items: [], totalCount: 0, page: 1, pageSize: 50 }),
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
          organizationContextLocked: personalSwitchable ? false : !unbound,
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

    if (url.includes("/pos-api/api/v1/pos/operational-branch") && method === "PUT") {
      return {
        ok: true,
        status: 200,
        json: async () => ({
          organizationId: orgId,
          branchId,
          name: "Main Branch",
          deviceMatchesSelectedBranch: false,
          deviceBoundBranchId: null,
          openCashierShiftPresent: false,
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
    window.localStorage.setItem("exits.pos-client.installation-device-id.v1", installId);
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
      expect(screen.getByRole("heading", { name: "New Sale" })).toBeInTheDocument();
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
    expect(within(menu).getByTestId("account-menu-role")).toHaveTextContent("Cashier");
    expect(within(menu).queryByText("olivia")).not.toBeInTheDocument();

    await user.click(within(menu).getByRole("menuitem", { name: "Preferences" }));
    await waitFor(() => {
      expect(screen.getByRole("heading", { name: "Preferences" })).toBeInTheDocument();
    });

    await user.click(screen.getByTestId("preferences-close"));
    await waitFor(() => {
      expect(screen.getByRole("heading", { name: "More" })).toBeInTheDocument();
    });
  });

  it("closes the account menu on Escape", async () => {
    const user = userEvent.setup();
    vi.stubGlobal("fetch", createFetchMock());
    renderAt("/role/cashier");

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: "New Sale" })).toBeInTheDocument();
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
      expect(screen.getByRole("heading", { name: "New Sale" })).toBeInTheDocument();
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

  it("shows Switch to Personal in the organization avatar menu for non-locked sessions", async () => {
    const user = userEvent.setup();
    vi.stubGlobal("fetch", createFetchMock({ personalSwitchable: true }));
    renderAt("/role/cashier");

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: "New Sale" })).toBeInTheDocument();
    });

    await user.click(screen.getByTestId("account-menu-trigger"));
    const menu = await screen.findByRole("menu");
    expect(within(menu).getByRole("menuitem", { name: "Switch to Personal" })).toBeInTheDocument();
  });
});
