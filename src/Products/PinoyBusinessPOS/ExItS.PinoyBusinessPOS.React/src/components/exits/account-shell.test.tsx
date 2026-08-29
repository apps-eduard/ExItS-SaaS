import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { RouterProvider, createMemoryRouter } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import { appRoutes } from "@/app/router";
import { setPosAccessToken } from "@/api/platform/pos-access-token";
import { clearPlatformAntiforgeryToken } from "@/api/platform/platform-http";
import { jsonResponse } from "@/test/render";
import {
  TEST_BRANCH_A_ID,
  TEST_INSTALL_ID,
  TEST_ORG_A_ID,
  createOrganizationSellReadyFetch,
  createPersonalPlatformFetch,
  seedOrganizationSellReadyLocalState,
} from "@/test/session-context";

const orgId = TEST_ORG_A_ID;
const branchId = TEST_BRANCH_A_ID;
const installId = TEST_INSTALL_ID;

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
  const branchName = longNames ? "Main Branch With A Very Long Store Title" : "Main Branch";

  if (unbound) {
    return createPersonalPlatformFetch({
      displayName,
      username: "olivia",
      email: "olivia@example.com",
    });
  }

  return createOrganizationSellReadyFetch({
    organizationId: orgId,
    organizationName: orgName,
    branchId,
    branchName,
    displayName,
    username: "olivia",
    email: "olivia@example.com",
    role: "Cashier",
    organizationContextLocked: personalSwitchable ? false : true,
    catalogCategories: { items: [], totalCount: 0, page: 1, pageSize: 50 },
    catalogProducts: () => ({ items: [], totalCount: 0, page: 1, pageSize: 50 }),
    onPosRequest: async (url, method) => {
      if (url.includes("/pos-api/api/v1/pos/operational-branch") && method === "PUT") {
        return jsonResponse(200, {
          organizationId: orgId,
          branchId,
          name: "Main Branch",
          deviceMatchesSelectedBranch: false,
          deviceBoundBranchId: null,
          openCashierShiftPresent: false,
        });
      }
      return null;
    },
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
    seedOrganizationSellReadyLocalState({ role: "Cashier" });
    window.localStorage.setItem("exits.pos-client.installation-device-id.v1", installId);
    setPosAccessToken("in-memory-only-access-token");
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
