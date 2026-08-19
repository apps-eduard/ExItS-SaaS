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
    expect(screen.getByRole("link", { name: "Organizations" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Overview" })).toHaveAttribute("aria-current", "page");
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
    expect(screen.queryByRole("link", { name: "Organizations" })).not.toBeInTheDocument();
    resolveAuthz?.(jsonResponse(200, sampleAuthorization));
    expect(await screen.findByRole("link", { name: "Organizations" })).toBeInTheDocument();
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
      expect(screen.queryByRole("link", { name: "Organizations" })).not.toBeInTheDocument();
    });
  });

  it("shows planned items as disabled", async () => {
    stubDesktop(true);
    mockAuthenticatedFetch();
    window.history.replaceState({}, "", "/admin");
    render(<App />);
    const planned = await screen.findByRole("button", { name: /Event Delivery/i });
    expect(planned).toBeDisabled();
    expect(screen.getAllByText("Planned").length).toBeGreaterThan(0);
  });

  it("collapses the desktop sidebar and persists the preference", async () => {
    stubDesktop(true);
    mockAuthenticatedFetch();
    window.history.replaceState({}, "", "/admin");
    const user = userEvent.setup();
    const { unmount } = render(<App />);
    await screen.findByRole("heading", { name: "Overview" });
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
    mockAuthenticatedFetch();
    window.history.replaceState({}, "", "/admin");
    render(<App />);
    await screen.findByRole("heading", { name: "Overview" });
    expect(screen.queryByRole("link", { name: "Test Payments" })).not.toBeInTheDocument();
  });

  it("shows DEV_TEST_ONLY navigation when development tools are allowed", async () => {
    stubDesktop(true);
    vi.spyOn(developmentTools, "areDevelopmentToolsAllowed").mockReturnValue(true);
    mockAuthenticatedFetch();
    window.history.replaceState({}, "", "/admin");
    render(<App />);
    expect(await screen.findByRole("link", { name: "Test Payments" })).toBeInTheDocument();
  });
});
