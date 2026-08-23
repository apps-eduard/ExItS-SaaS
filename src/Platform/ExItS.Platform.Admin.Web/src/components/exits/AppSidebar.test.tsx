import { afterEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { App } from "@/app/App";
import * as developmentTools from "@/lib/auth/development-tools";
import { UI_PREFERENCES_STORAGE_KEY } from "@/lib/preferences/ui-preferences";
import { mockAuthenticatedFetch } from "@/test/auth-fixtures";

function stubDesktop() {
  vi.spyOn(window, "matchMedia").mockImplementation((query: string) => {
    return {
      matches: query.includes("min-width: 1024px"),
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

describe("AppSidebar hover peek", () => {
  afterEach(() => {
    vi.restoreAllMocks();
    window.localStorage.removeItem(UI_PREFERENCES_STORAGE_KEY);
  });

  it("expands the icon rail on hover without changing the persisted collapsed preference", async () => {
    stubDesktop();
    vi.spyOn(developmentTools, "areDevelopmentToolsAllowed").mockReturnValue(true);
    mockAuthenticatedFetch();
    window.localStorage.setItem(
      UI_PREFERENCES_STORAGE_KEY,
      JSON.stringify({ theme: "system", language: "en", density: "balanced", sidebarCollapsed: true }),
    );
    const user = userEvent.setup();
    window.history.replaceState({}, "", "/admin");
    render(<App />);
    await screen.findByRole("heading", { name: "Overview" });

    const sidebar = screen.getByTestId("app-sidebar");
    expect(sidebar).toHaveAttribute("data-sidebar-peek", "false");
    expect(screen.queryByRole("button", { name: /^Home$/i })).not.toBeInTheDocument();

    await user.hover(sidebar);
    expect(sidebar).toHaveAttribute("data-sidebar-peek", "true");
    expect(await screen.findByRole("button", { name: /^Home$/i })).toBeInTheDocument();

    await user.unhover(sidebar);
    await waitFor(() => expect(sidebar).toHaveAttribute("data-sidebar-peek", "false"));

    expect(
      JSON.parse(window.localStorage.getItem(UI_PREFERENCES_STORAGE_KEY) ?? "{}").sidebarCollapsed,
    ).toBe(true);
  });
});
