import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
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

type LogoutMode = "success" | "fail" | "already_signed_out";

function createSessionFetchMock(
  options: { logoutMode?: LogoutMode; authenticatedInitially?: boolean } = {},
) {
  const logoutMode = options.logoutMode ?? "success";
  let authenticated = options.authenticatedInitially ?? true;

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
      if (!authenticated) {
        return {
          ok: false,
          status: 401,
          json: async () => ({ errorCode: "application.auth.session_invalid" }),
          text: async () => "",
        } as Response;
      }
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
      expect(new Headers(init?.headers).get("X-XSRF-TOKEN")).toBe("csrf-token");
      if (logoutMode === "fail") {
        return {
          ok: false,
          status: 500,
          json: async () => ({ detail: "logout unavailable" }),
          text: async () => "",
        } as Response;
      }
      if (logoutMode === "already_signed_out") {
        authenticated = false;
        return {
          ok: false,
          status: 401,
          json: async () => ({ detail: "session invalid" }),
          text: async () => "",
        } as Response;
      }
      authenticated = false;
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

  it("does not clear local session when logout fails", async () => {
    const user = userEvent.setup();
    vi.stubGlobal("fetch", createSessionFetchMock({ logoutMode: "fail" }));

    renderSessionProbe();

    await waitFor(() => {
      expect(screen.getByTestId("session-status")).toHaveTextContent("authenticated");
    });

    await user.click(screen.getByRole("button", { name: "Sign out" }));

    await waitFor(() => {
      expect(screen.getByTestId("sign-out-result")).toHaveTextContent(/fail:logout unavailable/i);
    });
    expect(screen.getByTestId("session-status")).toHaveTextContent("authenticated");
    expect(getPosAccessToken()).toBe("in-memory-only-access-token");
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
      expect(screen.getByRole("heading", { name: "Cashier home" })).toBeInTheDocument();
    });
    expect(screen.getByText(/Kizy Store · Main Branch/)).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Sign out" }));

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: "Sign in" })).toBeInTheDocument();
    });
    expect(memoryRouter.state.location.pathname).toBe("/sign-in");
    expect(getPosAccessToken()).toBeNull();
    expect(getPosSessionGrant()).toBeNull();

    await memoryRouter.navigate("/sell");
    await waitFor(() => {
      expect(screen.getByRole("heading", { name: "Sign in" })).toBeInTheDocument();
    });
    expect(memoryRouter.state.location.pathname).toBe("/sign-in");
  });

  it("shell keeps authenticated content and shows an error when logout fails", async () => {
    const user = userEvent.setup();
    vi.stubGlobal("fetch", createSessionFetchMock({ logoutMode: "fail" }));

    renderCashierHome();

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: "Cashier home" })).toBeInTheDocument();
    });

    await user.click(screen.getByRole("button", { name: "Sign out" }));

    await waitFor(() => {
      expect(screen.getByRole("alert")).toHaveTextContent(/logout unavailable|Sign out failed/i);
    });
    expect(screen.getByRole("heading", { name: "Cashier home" })).toBeInTheDocument();
    expect(getPosAccessToken()).toBe("in-memory-only-access-token");
  });
});
