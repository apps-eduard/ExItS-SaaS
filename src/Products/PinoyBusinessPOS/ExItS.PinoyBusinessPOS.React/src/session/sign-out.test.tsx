import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { jsonResponse } from "@/test/session-context";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { RouterProvider, createMemoryRouter } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import { appRoutes } from "@/app/router";
import { setPosAccessToken, getPosAccessToken } from "@/api/platform/pos-access-token";
import { getPosSessionGrant, setPosSessionGrant } from "@/api/platform/pos-session-grant";
import { clearPlatformAntiforgeryToken } from "@/api/platform/platform-http";
import { SessionProvider, useSession } from "@/session/SessionProvider";
import { UI_PREFERENCES_STORAGE_KEY } from "@/lib/preferences/ui-preferences";

const orgId = "11111111-1111-1111-1111-111111111111";
const branchId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
const installId = "aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee";
const deviceId = "ffffffff-ffff-4fff-8fff-ffffffffffff";
const shiftId = "cccccccc-cccc-cccc-cccc-cccccccccccc";
const registerId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";

type LogoutMode = "success" | "fail" | "already_signed_out";

function createSessionFetchMock(
  options: { logoutMode?: LogoutMode; authenticatedInitially?: boolean } = {},
) {
  const logoutMode = options.logoutMode ?? "success";
  let authenticated = options.authenticatedInitially ?? true;

  return vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const method = init?.method ?? "GET";

    if (url.includes("/pos-devices/authorize") && method === "POST") {
      return jsonResponse(200, {
          posDeviceId: deviceId,
          branchId,
          installationDeviceId: installId,
        });
    }

    if (url.includes("/cashier-shifts/current") && method === "GET") {
      return jsonResponse(200, {
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
        });
    }

    if (url.includes("/catalog/categories") || url.includes("/catalog/products")) {
      return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 50 });
    }

    if (url.includes("/api/v1/platform/antiforgery/token")) {
      return jsonResponse(200, { headerName: "X-XSRF-TOKEN", token: "csrf-token" });
    }

    if (url.includes("/api/v1/platform/auth/me") && method === "GET") {
      if (!authenticated) {
        return jsonResponse(401, { errorCode: "application.auth.session_invalid" });
      }
      return jsonResponse(200, {
          sessionId: "11111111-1111-1111-1111-111111111111",
          username: "cashier",
          displayName: "Cashier One",
          selectedOrganizationId: orgId,
          accountClass: "Organization",
          homeOrganizationId: orgId,
          organizationContextLocked: true,
        });
    }

    if (url.includes("/api/v1/platform/auth/organizations") && method === "GET") {
      return jsonResponse(200, [
          {
            organizationId: orgId,
            displayName: "Kizy Store",
            slug: "kizy-store",
          },
        ]);
    }

    if (url.includes(`/organizations/${orgId}/branches`) && method === "GET") {
      return jsonResponse(200, [
          {
            id: branchId,
            organizationId: orgId,
            code: "MAIN",
            name: "Main Branch",
            isPrimary: true,
            status: "Active",
          },
        ]);
    }

    if (url.includes("/api/v1/platform/auth/organization-context") && method === "PUT") {
      return jsonResponse(204, null);
    }

    if (url.includes(`/organizations/${orgId}/branch-context`) && method === "PUT") {
      return jsonResponse(204, null);
    }

    if (url.includes("/api/v1/platform/auth/token") && method === "POST") {
      return jsonResponse(200, {
          accessToken: "in-memory-only-access-token",
          productAccessAllowed: true,
          mappedPosRoleCode: "Cashier",
          productLocalRoleCode: "Cashier",
        });
    }

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

    if (url.includes("/api/v1/platform/auth/logout") && method === "POST") {
      expect(new Headers(init?.headers).get("X-XSRF-TOKEN")).toBe("csrf-token");
      if (logoutMode === "fail") {
        return jsonResponse(500, { detail: "logout unavailable" });
      }
      if (logoutMode === "already_signed_out") {
        authenticated = false;
        return jsonResponse(401, { detail: "session invalid" });
      }
      authenticated = false;
      return jsonResponse(204, null);
    }

    return jsonResponse(404, { detail: "not mocked" });
  });
}

function SignOutProbe() {
  const { status, session, signOut } = useSession();
  return (
    <div>
      <p data-testid="session-status">{status}</p>
      <p data-testid="session-user">{session?.username ?? "none"}</p>
      <button
        type="button"
        onClick={() => {
          void signOut().then((result) => {
            const host = document.getElementById("sign-out-result");
            if (host) {
              host.textContent = result.ok ? `ok:${result.reason}` : `fail:${result.detail}`;
            }
          });
        }}
      >
        Sign out
      </button>
      <p id="sign-out-result" data-testid="sign-out-result" />
    </div>
  );
}

function renderSessionProbe() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={client}>
      <SessionProvider>
        <SignOutProbe />
      </SessionProvider>
    </QueryClientProvider>,
  );
}

function renderCashierHome() {
  const memoryRouter = createMemoryRouter(appRoutes, { initialEntries: ["/role/cashier"] });
  return {
    memoryRouter,
    ...render(
      <AppProviders>
        <RouterProvider router={memoryRouter} />
      </AppProviders>,
    ),
  };
}

describe("sign out", () => {
  beforeEach(() => {
    window.localStorage.setItem(
      UI_PREFERENCES_STORAGE_KEY,
      JSON.stringify({ locale: "en", theme: "light" }),
    );
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
    window.localStorage.clear();
  });

  it("POSTs logout with CSRF, clears session artifacts, and keeps UI preferences", async () => {
    const user = userEvent.setup();
    const fetchMock = createSessionFetchMock({ logoutMode: "success" });
    vi.stubGlobal("fetch", fetchMock);

    renderSessionProbe();

    await waitFor(() => {
      expect(screen.getByTestId("session-status")).toHaveTextContent("authenticated");
    });

    await user.click(screen.getByRole("button", { name: "Sign out" }));

    await waitFor(() => {
      expect(screen.getByTestId("sign-out-result")).toHaveTextContent("ok:logged_out");
    });
    expect(screen.getByTestId("session-status")).toHaveTextContent("unauthenticated");
    expect(screen.getByTestId("session-user")).toHaveTextContent("none");
    expect(getPosAccessToken()).toBeNull();
    expect(getPosSessionGrant()).toBeNull();
    expect(
      fetchMock.mock.calls.some(
        ([url, init]) =>
          String(url).includes("/api/v1/platform/auth/logout") &&
          (init as RequestInit | undefined)?.method === "POST",
      ),
    ).toBe(true);

    const preferences = JSON.parse(
      window.localStorage.getItem(UI_PREFERENCES_STORAGE_KEY) ?? "{}",
    ) as { locale?: string; theme?: string };
    expect(preferences.locale).toBe("en");
    expect(preferences.theme).toBe("light");
  });

  it("locks locally when remote logout fails and marks pending remote logout", async () => {
    const user = userEvent.setup();
    vi.stubGlobal("fetch", createSessionFetchMock({ logoutMode: "fail" }));

    renderSessionProbe();

    await waitFor(() => {
      expect(screen.getByTestId("session-status")).toHaveTextContent("authenticated");
    });

    await user.click(screen.getByRole("button", { name: "Sign out" }));

    await waitFor(() => {
      expect(screen.getByTestId("sign-out-result")).toHaveTextContent("ok:logged_out");
    });
    expect(screen.getByTestId("session-status")).toHaveTextContent("unauthenticated");
    expect(getPosAccessToken()).toBeNull();
    expect(window.localStorage.getItem("exits.pos-client.pending-remote-logout.v1")).toContain(
      "markedAtUtc",
    );
  });

  it("clears local session when logout reports an already-expired session", async () => {
    const user = userEvent.setup();
    vi.stubGlobal("fetch", createSessionFetchMock({ logoutMode: "already_signed_out" }));

    renderSessionProbe();

    await waitFor(() => {
      expect(screen.getByTestId("session-status")).toHaveTextContent("authenticated");
    });

    await user.click(screen.getByRole("button", { name: "Sign out" }));

    await waitFor(() => {
      expect(screen.getByTestId("sign-out-result")).toHaveTextContent("ok:already_signed_out");
    });
    expect(screen.getByTestId("session-status")).toHaveTextContent("unauthenticated");
    expect(getPosAccessToken()).toBeNull();
  });

  it("shell Sign out replaces to sign-in and blocks protected routes", async () => {
    const user = userEvent.setup();
    vi.stubGlobal("fetch", createSessionFetchMock({ logoutMode: "success" }));

    const { memoryRouter } = renderCashierHome();

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: "New Sale" })).toBeInTheDocument();
    });
    expect(screen.getByTestId("workspace-context")).toHaveTextContent(/Kizy Store/);
    expect(screen.getByTestId("account-menu-trigger")).toHaveTextContent("CO");

    await user.click(screen.getByTestId("account-menu-trigger"));
    await user.click(screen.getByRole("menuitem", { name: "Sign out" }));

    await waitFor(() => {
      expect(screen.getByTestId("sign-in-page")).toBeInTheDocument();
    });
    expect(memoryRouter.state.location.pathname).toBe("/sign-in");
    expect(getPosAccessToken()).toBeNull();
    expect(getPosSessionGrant()).toBeNull();

    await memoryRouter.navigate("/sell");
    await waitFor(() => {
      expect(screen.getByTestId("sign-in-page")).toBeInTheDocument();
      expect(memoryRouter.state.location.pathname).toBe("/sign-in");
    });
  });

  it("shell locks locally when remote logout fails", async () => {
    const user = userEvent.setup();
    vi.stubGlobal("fetch", createSessionFetchMock({ logoutMode: "fail" }));

    const { memoryRouter } = renderCashierHome();

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: "New Sale" })).toBeInTheDocument();
    });

    await user.click(screen.getByTestId("account-menu-trigger"));
    await user.click(screen.getByRole("menuitem", { name: "Sign out" }));

    await waitFor(() => {
      expect(screen.getByTestId("sign-in-page")).toBeInTheDocument();
    });
    expect(memoryRouter.state.location.pathname).toBe("/sign-in");
    expect(getPosAccessToken()).toBeNull();
  });
});
