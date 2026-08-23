import { afterEach, describe, expect, it, vi } from "vitest";
import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { App } from "@/app/App";
import { SETTINGS_BACKEND_API_GAPS } from "@/features/settings/settings-sections";
import { mockAuthenticatedFetch } from "@/test/auth-fixtures";

function stubDesktop() {
  vi.spyOn(window, "matchMedia").mockImplementation((query: string) => {
    return {
      matches: query.includes("min-width: 1024px") || query.includes("min-width: 768px"),
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

describe("platform settings workspace", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  it("exposes a single Settings → Platform Settings nav link to /admin/settings", async () => {
    stubDesktop();
    mockAuthenticatedFetch();
    window.history.replaceState({}, "", "/admin");
    render(<App />);

    const nav = await screen.findByRole("navigation", { name: "Primary" });
    const settingsLink = within(nav).getByRole("link", { name: "Platform Settings" });
    expect(settingsLink).toHaveAttribute("href", "/admin/settings");
    expect(within(nav).queryByRole("link", { name: "General" })).not.toBeInTheDocument();
    expect(within(nav).queryByRole("link", { name: "Email & Notifications" })).not.toBeInTheDocument();
  });

  it("loads workspace local nav and truthful backend-gap panels without fake values", async () => {
    stubDesktop();
    mockAuthenticatedFetch();
    window.history.replaceState({}, "", "/admin/settings");
    const user = userEvent.setup();
    render(<App />);

    expect(await screen.findByRole("heading", { name: "Platform Settings" })).toBeInTheDocument();
    expect(await screen.findByRole("heading", { name: "General" })).toBeInTheDocument();
    expect(screen.getByText("BACKEND_API_GAP:PLATFORM_SETTINGS_GENERAL")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /save/i })).not.toBeInTheDocument();
    expect(screen.queryByLabelText(/smtp password/i)).not.toBeInTheDocument();

    const workspaceNav = screen.getByRole("navigation", { name: "Settings categories" });
    expect(within(workspaceNav).getByRole("link", { name: "General" })).toHaveAttribute(
      "href",
      "/admin/settings/general",
    );
    expect(within(workspaceNav).getByRole("link", { name: "Email & Notifications" })).toHaveAttribute(
      "href",
      "/admin/settings/email",
    );
    expect(within(workspaceNav).getByRole("link", { name: "Security Policies" })).toBeInTheDocument();
    expect(within(workspaceNav).getByRole("link", { name: "Integrations" })).toBeInTheDocument();
    expect(within(workspaceNav).getByRole("link", { name: "Feature Flags" })).toBeInTheDocument();
    expect(within(workspaceNav).getByRole("link", { name: "Regional" })).toBeInTheDocument();
    expect(within(workspaceNav).getByRole("link", { name: "Advanced" })).toBeInTheDocument();

    await user.click(within(workspaceNav).getByRole("link", { name: "Email & Notifications" }));
    expect(await screen.findByRole("heading", { name: "Email & Notifications" })).toBeInTheDocument();
    expect(screen.getByText("BACKEND_API_GAP:PLATFORM_SETTINGS_EMAIL")).toBeInTheDocument();
    expect(screen.getByText(/SMTP passwords/i)).toBeInTheDocument();

    await user.click(within(workspaceNav).getByRole("link", { name: "Feature Flags" }));
    expect(await screen.findByRole("heading", { name: "Feature Flags" })).toBeInTheDocument();
    expect(screen.getByText("BACKEND_API_GAP:PLATFORM_SETTINGS_FEATURE_FLAGS")).toBeInTheDocument();
    expect(
      screen.getByText(/Plans, subscriptions, catalog features, and entitlements → Products & Commercial/i),
    ).toBeInTheDocument();
  });

  it("records every settings category as a backend API gap", () => {
    expect(SETTINGS_BACKEND_API_GAPS).toEqual([
      "BACKEND_API_GAP:PLATFORM_SETTINGS_GENERAL",
      "BACKEND_API_GAP:PLATFORM_SETTINGS_EMAIL",
      "BACKEND_API_GAP:PLATFORM_SETTINGS_SECURITY",
      "BACKEND_API_GAP:PLATFORM_SETTINGS_INTEGRATIONS",
      "BACKEND_API_GAP:PLATFORM_SETTINGS_FEATURE_FLAGS",
      "BACKEND_API_GAP:PLATFORM_SETTINGS_REGIONAL",
      "BACKEND_API_GAP:PLATFORM_SETTINGS_ADVANCED",
    ]);
  });

  it("fail-closes when the actor is not a platform administrator", async () => {
    stubDesktop();
    mockAuthenticatedFetch({ permissions: [] });
    window.history.replaceState({}, "", "/admin/settings/general");
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Page not found" })).toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "Platform Settings" })).not.toBeInTheDocument();
  });

  it("redirects unknown settings sections to general", async () => {
    stubDesktop();
    mockAuthenticatedFetch();
    window.history.replaceState({}, "", "/admin/settings/not-a-real-section");
    render(<App />);
    expect(await screen.findByRole("heading", { name: "General" })).toBeInTheDocument();
    expect(window.location.pathname).toBe("/admin/settings/general");
  });
});
