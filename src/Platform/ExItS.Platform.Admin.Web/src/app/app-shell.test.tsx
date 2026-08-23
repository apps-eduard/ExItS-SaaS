import { afterEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { App } from "@/app/App";
import * as developmentTools from "@/lib/auth/development-tools";
import { UI_PREFERENCES_STORAGE_KEY } from "@/lib/preferences/ui-preferences";
import {
  jsonResponse,
  mockAuthenticatedFetch,
  mockUnauthenticatedFetch,
  sampleAuthorization,
  sampleSession,
  textResponse,
} from "@/test/auth-fixtures";

function stubDesktop(desktop: boolean) {
  vi.spyOn(window, "matchMedia").mockImplementation((query: string) => {
    return {
      matches: desktop && query.includes("min-width: 1024px"),
      media: query,
      onchange: null,
      addEventListener: () => undefined,
      removeEventListener: () => undefined,
      addListener: () => undefined,
      removeListener: () => undefined,
      dispatchEvent: () => true,
    } as MediaQueryList;
  });
}

describe("application shell", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  it("redirects unauthenticated /admin to login", async () => {
    mockUnauthenticatedFetch();
    window.history.replaceState({}, "", "/admin");
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Sign In" })).toBeInTheDocument();
    expect(window.location.pathname).toBe("/admin/login");
  });

  it("redirects authenticated / to /admin and renders the shell", async () => {
    stubDesktop(true);
    mockAuthenticatedFetch();
    window.history.replaceState({}, "", "/");
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Overview" })).toBeInTheDocument();
    await waitFor(() => {
      expect(window.location.pathname).toBe("/admin");
    });
    expect(screen.getAllByText("Home").length).toBeGreaterThan(0);
    expect(screen.getByRole("link", { name: "Overview" })).toHaveAttribute("aria-current", "page");
    expect(screen.getByRole("navigation", { name: "Breadcrumb" })).toBeInTheDocument();
    expect(await screen.findByRole("link", { name: "All Organizations" })).toBeInTheDocument();
  });

  it("does not flash privileged navigation while authorization is loading", async () => {
    stubDesktop(true);
    let resolveAuthz: ((value: Response) => void) | undefined;
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL) => {
        const url = String(input);
        if (url.includes("/auth/me")) {
          return jsonResponse(200, sampleSession);
        }
        if (url.includes("/authorization/me")) {
          return await new Promise<Response>((resolve) => {
            resolveAuthz = resolve;
          });
        }
        return jsonResponse(404, {});
      }),
    );
    window.history.replaceState({}, "", "/admin");
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Overview" })).toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "All Organizations" })).not.toBeInTheDocument();
    expect(screen.queryByLabelText("Organizations. Under development")).not.toBeInTheDocument();
    resolveAuthz?.(jsonResponse(200, sampleAuthorization));
    expect(await screen.findByRole("link", { name: "All Organizations" })).toBeInTheDocument();
    expect(screen.queryByLabelText("Organizations. Under development")).not.toBeInTheDocument();
  });

  it("hides unauthorized items after permissions load", async () => {
    stubDesktop(true);
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL) => {
        const url = String(input);
        if (url.includes("/auth/me")) {
          return jsonResponse(200, sampleSession);
        }
        if (url.includes("/authorization/me")) {
          return jsonResponse(200, { ...sampleAuthorization, permissions: [] });
        }
        return jsonResponse(404, {});
      }),
    );
    window.history.replaceState({}, "", "/admin");
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Overview" })).toBeInTheDocument();
    await waitFor(() => {
      expect(screen.queryByRole("link", { name: "All Organizations" })).not.toBeInTheDocument();
      expect(screen.queryByLabelText("Organizations. Under development")).not.toBeInTheDocument();
    });
  });

  it("shows planned items as non-navigable Development status when tools are allowed", async () => {
    stubDesktop(true);
    vi.spyOn(developmentTools, "areDevelopmentToolsAllowed").mockReturnValue(true);
    mockAuthenticatedFetch();
    window.history.replaceState({}, "", "/admin");
    render(<App />);
    expect(await screen.findByLabelText("Event Delivery. Planned")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Platform Settings" })).toHaveAttribute(
      "href",
      "/admin/settings",
    );
    expect(screen.queryByLabelText("Platform Settings. Planned")).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /Event Delivery/i })).not.toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Event Delivery" })).not.toBeInTheDocument();
    expect(screen.getAllByText("Planned").length).toBeGreaterThan(0);
  });

  it("collapses the desktop sidebar and persists the preference", async () => {
    stubDesktop(true);
    mockAuthenticatedFetch();
    window.history.replaceState({}, "", "/admin");
    const user = userEvent.setup();
    const { unmount } = render(<App />);
    await screen.findByRole("heading", { name: "Overview" });
    const aside = document.querySelector("aside");
    expect(aside).toBeTruthy();
    expect(
      within(aside as HTMLElement).queryByRole("button", { name: /sidebar/i }),
    ).not.toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Collapse sidebar" }));
    expect(
      JSON.parse(window.localStorage.getItem(UI_PREFERENCES_STORAGE_KEY) ?? "{}").sidebarCollapsed,
    ).toBe(true);
    unmount();
    render(<App />);
    expect(await screen.findByRole("button", { name: "Expand sidebar" })).toBeInTheDocument();
  });

  it("opens and closes the mobile navigation drawer", async () => {
    stubDesktop(false);
    mockAuthenticatedFetch();
    window.history.replaceState({}, "", "/admin");
    const user = userEvent.setup();
    render(<App />);
    await screen.findByRole("heading", { name: "Overview" });
    expect(screen.getByRole("button", { name: "Open navigation" })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Collapse sidebar" })).not.toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Open navigation" }));
    const dialog = await screen.findByRole("dialog");
    expect(within(dialog).getByRole("link", { name: "Overview" })).toBeInTheDocument();
    await user.keyboard("{Escape}");
    await waitFor(() => {
      expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    });
  });

  it("collapses the desktop sidebar from the keyboard", async () => {
    stubDesktop(true);
    mockAuthenticatedFetch();
    window.history.replaceState({}, "", "/admin");
    const user = userEvent.setup();
    render(<App />);
    const collapse = await screen.findByRole("button", { name: "Collapse sidebar" });
    collapse.focus();
    await user.keyboard("{Enter}");
    expect(await screen.findByRole("button", { name: "Expand sidebar" })).toBeInTheDocument();
  });

  it("integrates language, theme, and density from the shell preferences menu", async () => {
    stubDesktop(true);
    mockAuthenticatedFetch();
    window.history.replaceState({}, "", "/admin");
    const user = userEvent.setup();
    render(<App />);
    await screen.findByRole("heading", { name: "Overview" });
    await user.click(screen.getByRole("button", { name: "Preferences" }));
    await user.click(await screen.findByRole("menuitem", { name: /Filipino/i }));
    expect(document.documentElement.lang).toBe("fil-PH");
    expect(
      await screen.findByRole("heading", { name: "Pangkalahatang-tanaw" }),
    ).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Mga kagustuhan" }));
    await user.click(await screen.findByRole("menuitem", { name: /Madilim/i }));
    expect(document.documentElement.dataset.theme).toBe("dark");
    await user.click(screen.getByRole("button", { name: "Mga kagustuhan" }));
    await user.click(await screen.findByRole("menuitem", { name: /Siksik/i }));
    expect(document.documentElement.dataset.density).toBe("compact");
  });

  it("omits DEV_TEST_ONLY navigation when the frontend mode is disallowed", async () => {
    stubDesktop(true);
    vi.spyOn(developmentTools, "areDevelopmentToolsAllowed").mockReturnValue(false);
    vi.spyOn(developmentTools, "areTestUserToolsPermitted").mockReturnValue(false);
    mockAuthenticatedFetch();
    window.history.replaceState({}, "", "/admin");
    render(<App />);
    await screen.findByRole("heading", { name: "Overview" });
    expect(screen.queryByRole("link", { name: "Test Payments" })).not.toBeInTheDocument();
    expect(screen.queryByLabelText(/Test Payments/)).not.toBeInTheDocument();
  });

  it("shows DEV_TEST_ONLY navigation when development tools are allowed", async () => {
    stubDesktop(true);
    vi.spyOn(developmentTools, "areDevelopmentToolsAllowed").mockReturnValue(true);
    mockAuthenticatedFetch();
    window.history.replaceState({}, "", "/admin");
    render(<App />);
    expect(await screen.findByLabelText("Test Payments. Under development")).toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Test Payments" })).not.toBeInTheDocument();
  });

  it("shows authorized under-development items in-place and Development only for DEV_TEST_ONLY", async () => {
    stubDesktop(true);
    vi.spyOn(developmentTools, "areDevelopmentToolsAllowed").mockReturnValue(true);
    mockAuthenticatedFetch();
    window.history.replaceState({}, "", "/admin");
    render(<App />);
    await screen.findByRole("heading", { name: "Overview" });
    expect(await screen.findByRole("button", { name: "Development" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "All Organizations" })).toBeInTheDocument();
    expect(await screen.findByRole("link", { name: "All Accounts" })).toHaveAttribute(
      "href",
      "/admin/users",
    );
    expect(screen.getByLabelText("Event Delivery. Planned")).toBeInTheDocument();
    expect(screen.getAllByText("Under development").length).toBeGreaterThan(0);
    expect(screen.getByLabelText("Test Payments. Under development")).toBeInTheDocument();
  });

  it("keeps blueprint under-development items without Development tools, and hides DEV_TEST_ONLY", async () => {
    stubDesktop(true);
    vi.spyOn(developmentTools, "areDevelopmentToolsAllowed").mockReturnValue(false);
    vi.spyOn(developmentTools, "areTestUserToolsPermitted").mockReturnValue(false);
    mockAuthenticatedFetch();
    window.history.replaceState({}, "", "/admin");
    render(<App />);
    await screen.findByRole("heading", { name: "Overview" });
    expect(screen.queryByText("Development")).not.toBeInTheDocument();
    expect(await screen.findByRole("link", { name: "All Organizations" })).toBeInTheDocument();
    expect(await screen.findByRole("link", { name: "All Accounts" })).toHaveAttribute(
      "href",
      "/admin/users",
    );
    expect(screen.getByLabelText("Event Delivery. Planned")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Platform Settings" })).toHaveAttribute(
      "href",
      "/admin/settings",
    );
    expect(screen.queryByLabelText("Platform Settings. Planned")).not.toBeInTheDocument();
    expect(screen.queryByText("Test Payments")).not.toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Overview" })).toBeInTheDocument();
  });

  it("renders Under development for known unimplemented routes and Page not found for unknown routes", async () => {
    stubDesktop(true);
    vi.spyOn(developmentTools, "areDevelopmentToolsAllowed").mockReturnValue(true);
    mockAuthenticatedFetch();
    window.history.replaceState({}, "", "/admin/local-validation/test-payments");
    const { unmount } = render(<App />);
    expect(await screen.findByRole("heading", { name: "Under development" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Back to Overview" })).toHaveAttribute(
      "href",
      "/admin",
    );
    expect(screen.queryByRole("button", { name: "Copy error details" })).not.toBeInTheDocument();
    unmount();

    window.history.replaceState({}, "", "/admin/platform-roles");
    const second = render(<App />);
    expect(await screen.findByRole("heading", { name: "Roles & Permissions" })).toBeInTheDocument();
    second.unmount();

    window.history.replaceState({}, "", "/admin/this-route-does-not-exist-xyz");
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Page not found" })).toBeInTheDocument();
  });

  it("does not leak privileged feature names on unauthorized known routes", async () => {
    stubDesktop(true);
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL) => {
        const url = String(input);
        if (url.includes("/auth/me")) {
          return jsonResponse(200, sampleSession);
        }
        if (url.includes("/authorization/me")) {
          return jsonResponse(200, { ...sampleAuthorization, permissions: [] });
        }
        return jsonResponse(404, {});
      }),
    );
    window.history.replaceState({}, "", "/admin/organizations");
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Page not found" })).toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "Under development" })).not.toBeInTheDocument();
  });

  it("does not flash under-development content while authorization is loading", async () => {
    stubDesktop(true);
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL) => {
        const url = String(input);
        if (url.includes("/auth/me")) {
          return jsonResponse(200, sampleSession);
        }
        if (url.includes("/authorization/me")) {
          return await new Promise<Response>(() => undefined);
        }
        return jsonResponse(404, {});
      }),
    );
    window.history.replaceState({}, "", "/admin/organizations");
    render(<App />);
    expect(await screen.findAllByRole("link", { name: "Overview" })).not.toHaveLength(0);
    expect(screen.queryByRole("heading", { name: "Under development" })).not.toBeInTheDocument();
    expect(screen.queryByText("Organizations")).not.toBeInTheDocument();
  });

  it("keeps under-development nav items out of the keyboard tab order", async () => {
    stubDesktop(true);
    vi.spyOn(developmentTools, "areDevelopmentToolsAllowed").mockReturnValue(true);
    mockAuthenticatedFetch();
    window.history.replaceState({}, "", "/admin");
    const user = userEvent.setup();
    render(<App />);
    const overview = await screen.findByRole("link", { name: "Overview" });
    overview.focus();
    await user.tab();
    expect(document.activeElement).not.toBe(screen.getByLabelText("Event Delivery. Planned"));
    expect(screen.getByLabelText("Event Delivery. Planned")).not.toHaveAttribute("href");
  });

  it("localizes the under-development page to Filipino", async () => {
    stubDesktop(true);
    vi.spyOn(developmentTools, "areDevelopmentToolsAllowed").mockReturnValue(true);
    mockAuthenticatedFetch();
    window.history.replaceState({}, "", "/admin/local-validation/test-payments");
    const user = userEvent.setup();
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Under development" })).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Preferences" }));
    await user.click(await screen.findByRole("menuitem", { name: /Filipino/i }));
    expect(
      await screen.findByRole("heading", { name: "Kasalukuyang dinadagdag" }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole("link", { name: "Bumalik sa Pangkalahatang-tanaw" }),
    ).toBeInTheDocument();
  });

  it("renders initials in the account trigger and signs out through the logout endpoint", async () => {
    stubDesktop(true);
    let signedOut = false;
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
        const url = String(input);
        if (url.includes("/auth/logout")) {
          expect(init?.method).toBe("POST");
          expect(new Headers(init?.headers).get("X-XSRF-TOKEN")).toBe("test-antiforgery-token");
          signedOut = true;
          return {
            ok: true,
            status: 204,
            json: async () => null,
            text: async () => "",
          } as Response;
        }
        if (url.includes("/antiforgery/token")) {
          return jsonResponse(200, { headerName: "X-XSRF-TOKEN", token: "test-antiforgery-token" });
        }
        if (url.includes("/auth/me")) {
          if (signedOut) {
            return jsonResponse(401, {
              status: 401,
              errorCode: "application.auth.session_invalid",
            });
          }
          return jsonResponse(200, sampleSession);
        }
        if (url.includes("/authorization/me")) {
          return jsonResponse(200, sampleAuthorization);
        }
        if (
          url.includes("/organizations") ||
          url.includes("/subscriptions") ||
          url.includes("/users") ||
          url.includes("/audit")
        ) {
          return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 1 });
        }
        if (url.includes("/health")) {
          return textResponse(200, "Healthy");
        }
        return jsonResponse(404, {});
      }),
    );
    window.history.replaceState({}, "", "/admin");
    const user = userEvent.setup();
    const { unmount } = render(<App />);
    await screen.findByRole("heading", { name: "Overview" });
    expect(screen.getByText("OM", { exact: true })).toBeInTheDocument();
    expect(screen.queryByRole("menuitem", { name: /Sign out/i })).not.toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Account menu" }));
    expect(await screen.findByText("olivia@example.test")).toBeInTheDocument();
    await user.click(await screen.findByRole("menuitem", { name: /Sign out/i }));
    expect(await screen.findByRole("heading", { name: "Sign In" })).toBeInTheDocument();
    expect(signedOut).toBe(true);
    expect(window.location.pathname).toBe("/admin/login");
    unmount();
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Sign In" })).toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "Overview" })).not.toBeInTheDocument();
  });
});
