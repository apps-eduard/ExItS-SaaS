import { afterEach, describe, expect, it, vi } from "vitest";
import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { App } from "@/app/App";
import * as developmentTools from "@/lib/auth/development-tools";
import { mockAuthenticatedFetch, sampleAuthorization } from "@/test/auth-fixtures";

function stubDesktop(matchesDesktop: boolean) {
  vi.spyOn(window, "matchMedia").mockImplementation((query: string) => {
    const desktop = query.includes("min-width: 1024px") || query.includes("min-width: 768px");
    return {
      matches: matchesDesktop ? desktop : false,
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

function primaryNav() {
  return screen.getByLabelText("Primary");
}

describe("AppNav bulk accordion", () => {
  afterEach(() => {
    vi.restoreAllMocks();
    delete window.__EXITS_PLATFORM_ADMIN_WEB__;
  });

  it("collapses all expandable sections, then expands them again without changing the route", async () => {
    stubDesktop(true);
    vi.spyOn(developmentTools, "areDevelopmentToolsAllowed").mockReturnValue(true);
    mockAuthenticatedFetch({
      permissions: [...sampleAuthorization.permissions, "platform.permission.manage_subscriptions"],
    });
    const user = userEvent.setup();
    window.history.replaceState({}, "", "/admin");
    render(<App />);

    const nav = within(await screen.findByLabelText("Primary"));
    expect(await nav.findByRole("link", { name: "Overview" })).toBeInTheDocument();
    expect(await nav.findByRole("button", { name: /^Billing$/i })).toHaveAttribute(
      "aria-expanded",
      "true",
    );

    const bulk = await screen.findByTestId("nav-bulk-accordion");
    expect(bulk).toHaveAttribute("aria-label", "Collapse all");
    await user.click(bulk);

    expect(bulk).toHaveAttribute("aria-label", "Expand all");
    expect(nav.queryByRole("link", { name: "Overview" })).not.toBeInTheDocument();
    expect(nav.getByRole("button", { name: /^Billing$/i })).toHaveAttribute(
      "aria-expanded",
      "false",
    );
    expect(window.location.pathname).toBe("/admin");

    await user.click(bulk);
    expect(bulk).toHaveAttribute("aria-label", "Collapse all");
    expect(nav.getByRole("link", { name: "Overview" })).toBeInTheDocument();
    expect(nav.getByRole("link", { name: "Payments" })).toBeInTheDocument();
  });

  it("keeps individual section toggles working after bulk collapse", async () => {
    stubDesktop(true);
    vi.spyOn(developmentTools, "areDevelopmentToolsAllowed").mockReturnValue(true);
    mockAuthenticatedFetch();
    const user = userEvent.setup();
    window.history.replaceState({}, "", "/admin");
    render(<App />);

    const nav = within(await screen.findByLabelText("Primary"));
    const bulk = await screen.findByTestId("nav-bulk-accordion");
    await user.click(bulk);
    expect(nav.queryByRole("link", { name: "Overview" })).not.toBeInTheDocument();

    await user.click(nav.getByRole("button", { name: /^Home$/i }));
    expect(nav.getByRole("link", { name: "Overview" })).toBeInTheDocument();
    expect(nav.queryByRole("link", { name: "Payments" })).not.toBeInTheDocument();
  });

  it("hides DEV_TEST_ONLY items when development tools are disallowed", async () => {
    stubDesktop(true);
    vi.spyOn(developmentTools, "areDevelopmentToolsAllowed").mockReturnValue(false);
    mockAuthenticatedFetch({
      permissions: [...sampleAuthorization.permissions, "platform.permission.manage_subscriptions"],
    });
    window.history.replaceState({}, "", "/admin");
    render(<App />);
    const nav = within(await screen.findByLabelText("Primary"));
    expect(nav.getByRole("link", { name: "Overview" })).toBeInTheDocument();
    expect(nav.queryByText("Development")).not.toBeInTheDocument();
    expect(nav.queryByRole("link", { name: "Test Payments" })).not.toBeInTheDocument();
  });

  it("preserves active route highlighting after collapse and expand", async () => {
    stubDesktop(true);
    vi.spyOn(developmentTools, "areDevelopmentToolsAllowed").mockReturnValue(true);
    mockAuthenticatedFetch();
    const user = userEvent.setup();
    window.history.replaceState({}, "", "/admin/payments");
    render(<App />);

    const nav = within(await screen.findByLabelText("Primary"));
    const payments = await nav.findByRole("link", { name: "Payments" });
    expect(payments.className).toMatch(/primary-soft/);

    const bulk = await screen.findByTestId("nav-bulk-accordion");
    await user.click(bulk);
    await user.click(bulk);

    const paymentsAgain = await nav.findByRole("link", { name: "Payments" });
    expect(paymentsAgain.className).toMatch(/primary-soft/);
  });

  it("does not render the bulk control in icon-rail collapsed mode", async () => {
    stubDesktop(true);
    vi.spyOn(developmentTools, "areDevelopmentToolsAllowed").mockReturnValue(true);
    mockAuthenticatedFetch();
    const user = userEvent.setup();
    window.history.replaceState({}, "", "/admin");
    render(<App />);
    expect(await screen.findByTestId("nav-bulk-accordion")).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Collapse sidebar" }));
    expect(screen.getByTestId("nav-bulk-accordion")).toBeInTheDocument();
    expect(primaryNav().querySelector('a[href="/admin"]')).not.toBeNull();
  });

  it("flattens By Product into direct product links in icon-rail mode", async () => {
    stubDesktop(true);
    vi.spyOn(developmentTools, "areDevelopmentToolsAllowed").mockReturnValue(true);
    mockAuthenticatedFetch();
    const user = userEvent.setup();
    window.history.replaceState({}, "", "/admin");
    render(<App />);
    await screen.findByRole("link", { name: "All Organizations" });
    await user.click(screen.getByRole("button", { name: "Collapse sidebar" }));

    const nav = within(primaryNav());
    expect(nav.queryByRole("button", { name: /^By Product$/i })).not.toBeInTheDocument();
    expect(nav.getByRole("link", { name: "Pinoy Business POS" })).toHaveAttribute(
      "href",
      "/admin/organizations?product=pinoy-business-pos",
    );
  });
});
